# interview_ui

Added route @app.route('/get_results/<interview_id>', methods=['GET'])

got interview id from AudioRecorder.cs        
InterviewSession.InterviewId = interviewId; // Share globally

Added it into InterviewSession.cs static class

Created InterviewResultData.cs

Created the logic in ResultsPanelManager.cs

# Fichier app.py

```python
from flask import Flask, jsonify, request
from pymongo import MongoClient
import random
import os
import whisper
import subprocess
import tempfile
from werkzeug.utils import secure_filename
import datetime

from flask import send_file
import os
from pdf_generator import generate_pdf_summary

app = Flask(__name__)

# Connexion à MongoDB
client = MongoClient("mongodb://localhost:27017")
db = client["interviewDB"]
question_collection = db["questions"]
interview_collection = db["interviews"]

# Nombre de questions dynamiques
EXPECTED_DYNAMIC_QUESTIONS = 3

# Charger le modèle Whisper
model = whisper.load_model("base")

# Message de bienvenue
WELCOME_MESSAGE = {
    "id": "welcome",
    "question": "Welcome to your interview. Please answer the following questions clearly and confidently.",
    "skip_response": True
}

# Questions statiques
STATIC_QUESTIONS = [
    {"id": "static_1", "question": "Can you introduce yourself?"},
    {"id": "static_2", "question": "Why are you interested in this position?"},
    {"id": "static_3", "question": "What are your greatest strengths?"}
]

# Message de transition vers les questions techniques
TRANSITION_MESSAGE = {
    "id": "transition",
    "question": "Now we will move on to the technical questions.",
    "skip_response": True
}

@app.route("/questions", methods=["GET"])
def get_random_questions():
    all_questions = list(question_collection.find())

    if len(all_questions) < EXPECTED_DYNAMIC_QUESTIONS:
        return jsonify({"error": "Not enough questions in the database"}), 400

    selected_questions = random.sample(all_questions, EXPECTED_DYNAMIC_QUESTIONS)

    # Formatage des questions dynamiques
    for q in selected_questions:
        q["_id"] = str(q["_id"])
        q["answer"] = q.get("answer", "No answer provided.")

    # Liste finale : accueil + statiques + transition + dynamiques
    full_list = (
        [WELCOME_MESSAGE] +
        STATIC_QUESTIONS +
        [TRANSITION_MESSAGE] +
        [{"id": q["_id"], "question": q["question"], "answer": q["answer"]} for q in selected_questions]
    )

    return jsonify(full_list)

@app.route("/submit-response", methods=["POST"])
def submit_response():
    question_enonce = request.form.get("question_enonce")
    interview_id = request.form.get("interview_id")
    audio_file = request.files.get("audio")

    if not question_enonce or not interview_id or not audio_file:
        return jsonify({"error": "Missing data"}), 400

    # Ignorer les messages sans réponse attendue
    if question_enonce in [WELCOME_MESSAGE["question"], TRANSITION_MESSAGE["question"]]:
        return jsonify({"message": "No response required for this message."}), 200

    # Sauvegarde temporaire du fichier audio
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as temp:
        audio_file.save(temp.name)
        temp_filepath = temp.name

    try:
        result = model.transcribe(temp_filepath)
        response_text = result["text"]
    except Exception as e:
        return jsonify({"error": f"Transcription failed: {str(e)}"}), 500
    finally:
        os.remove(temp_filepath)  # Supprimer le fichier temporaire

    question_data = {
        "question_enonce": question_enonce,
        "response_text": response_text
    }

    existing = interview_collection.find_one({"interview_id": interview_id})

    if existing:
        interview_collection.update_one(
            {"interview_id": interview_id},
            {"$push": {"questions": question_data}}
        )
    else:
        interview_doc = {
            "interview_id": interview_id,
            "questions": [question_data],
            "expected_count": len(STATIC_QUESTIONS) + EXPECTED_DYNAMIC_QUESTIONS,
            "created_at": datetime.datetime.utcnow()
        }
        interview_collection.insert_one(interview_doc)

    updated = interview_collection.find_one({"interview_id": interview_id})
    total_answered = len(updated.get("questions", []))
    total_expected = updated.get("expected_count", 6)

    if total_answered >= total_expected:
        try:
            print(f"Launching testBert for interview {interview_id}")
            subprocess.run(["python", "testBert.py", interview_id], check=True)

            print(f"Launching modelOllama.py for feedback")
            subprocess.run(["python", "modelOllama.py", interview_id], check=True)

            # Mark interview as complete
            interview_collection.update_one(
                {"interview_id": interview_id},
                {"$set": {"status": "completed", "completed_at": datetime.datetime.utcnow()}}
            )

        except Exception as e:
            print(f"Error running scripts: {e}")

    return jsonify({
        "message": "Response saved",
        "transcription": response_text
    }), 200


@app.route('/get_results/<interview_id>', methods=['GET'])
def get_results_by_id(interview_id):
    interview = db.interviews.find_one({"interview_id": interview_id}, {'_id': 0})
    return jsonify(interview)

@app.route('/interviews/<interview_id>/summary/pdf', methods=['GET'])
def get_summary_pdf(interview_id):
    interview = interview_collection.find_one({"interview_id": interview_id})
    if not interview:
        return jsonify({'error': 'Interview not found'}), 404

    pdf_buffer = generate_pdf_summary(interview)

    return send_file(
        pdf_buffer,
        as_attachment=True,
        download_name=f"interview_{interview_id}_summary.pdf",
        mimetype='application/pdf'
    )


if __name__ == "__main__":
    app.run(debug=True)



