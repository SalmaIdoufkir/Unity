using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.Networking;

public class AudioRecorder : MonoBehaviour
{
    private string filePath;
    private AudioClip audioClip;
    private int currentQuestionIndex;
    private bool isRecordingAllowed = false;
    private string interviewId; // ID unique de l'entretien


    public QuestionManager questionManager; // Drag & drop

    void Start()
    {
        filePath = UnityEngine.Application.persistentDataPath + "/recorded_audio.wav";
        interviewId = System.Guid.NewGuid().ToString(); // Génère un ID unique pour l'entretien

        InterviewSession.InterviewId = interviewId; // Share globally


    }

    public void EnableRecording(int questionIndex)
    {
        currentQuestionIndex = questionIndex;
        isRecordingAllowed = true;
    }

    public void StartRecording()
    {
        if (!isRecordingAllowed)
        {
            Debug.LogWarning("⛔ L'enregistrement n'est pas encore autorisé !");
            return;
        }

        audioClip = Microphone.Start(null, false, 300, 44100);
        Debug.Log("🎙️ Enregistrement démarré pour question " + currentQuestionIndex);
    }

    public void StopRecording()
    {
        if (!isRecordingAllowed || !Microphone.IsRecording(null))
        {
            Debug.LogWarning("⛔ Aucun enregistrement actif.");
            return;
        }

        int position = Microphone.GetPosition(null);
        Microphone.End(null);

        if (position <= 0)
        {
            Debug.LogError("❌ Rien enregistré.");
            return;
        }

        float[] soundData = new float[position * audioClip.channels];
        audioClip.GetData(soundData, 0);
        AudioClip trimmedClip = AudioClip.Create("trimmed", position, audioClip.channels, audioClip.frequency, false);
        trimmedClip.SetData(soundData, 0);
        audioClip = trimmedClip;

        SaveAudioFile();
        isRecordingAllowed = false;
    }

    private void SaveAudioFile()
    {
        var wavData = WavUtility.FromAudioClip(audioClip);
        File.WriteAllBytes(filePath, wavData);
        StartCoroutine(UploadAudioFile());
    }

    private IEnumerator UploadAudioFile()
    {
        var question = QuestionStorage.Questions[currentQuestionIndex];

        var form = new WWWForm();
        form.AddField("question_enonce", question.question);
        form.AddField("interview_id", interviewId); // facultatif si tu veux suivre l'entretien
        form.AddBinaryData("audio", File.ReadAllBytes(filePath), "audio.wav", "audio/wav");

        using (UnityWebRequest www = UnityWebRequest.Post("http://localhost:5000/submit-response", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Réponse envoyée !");
                questionManager.OnRecordingAndUploadFinished();
            }
            else
            {
                Debug.LogError("❌ Erreur d'envoi : " + www.error);
            }
        }
    }

    public void DownloadSummaryPDF()
    {
        string url = "http://localhost:5000/interviews/" + interviewId + "/summary/pdf";
        Application.OpenURL(url);
    }

}

