using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public class QuestionAudioDownloader : MonoBehaviour

{
    public QuestionManager questionManager;
    public string elevenLabsApiKey = "sk_34418568025e2b5eee03ef6e9449490731419dc0e69c90f0";
    public string voiceId = "nPczCjzI2devNBz1zQrb";
    public string outputFolder = "Assets/StreamingAssets/AudioQuestions/";

    void Start()
    {
        StartCoroutine(WaitForQuestionsThenDownload());
    }

    IEnumerator WaitForQuestionsThenDownload()
    {
        // ⏳ Attente jusqu’à ce que les questions soient chargées
        while (QuestionStorage.Questions == null || QuestionStorage.Questions.Length == 0)
        {
            Debug.Log("⏳ En attente des questions...");
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("✅ Questions chargées, début du téléchargement audio...");
        yield return StartCoroutine(DownloadAllAudio());
    }

    IEnumerator DownloadAllAudio()
    {
        Question[] questions = QuestionStorage.Questions;

        for (int i = 0; i < questions.Length; i++)
        {
            yield return StartCoroutine(GenerateAndSaveAudio(questions[i].question, $"question_{i + 1}.mp3"));
        }

        Debug.Log("✅ Tous les fichiers audio ont été téléchargés !");

        questionManager.StartFirstQuestion();

    }

    IEnumerator GenerateAndSaveAudio(string text, string fileName)
    {
        string ttsUrl = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}";
        string jsonBody = $@"
    {{
        ""text"": ""{CleanForJson(text)}"",
        ""model_id"": ""eleven_monolingual_v1"",
        ""voice_settings"": {{
            ""stability"": 0.5,
            ""similarity_boost"": 0.5
        }}
    }}";

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(ttsUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("xi-api-key", elevenLabsApiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            byte[] audioData = request.downloadHandler.data;

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            string fullPath = Path.Combine(outputFolder, fileName);
            File.WriteAllBytes(fullPath, audioData);
            Debug.Log($"✅ Fichier audio sauvegardé : {fullPath}");
        }
        else
        {
            Debug.LogError("❌ Erreur TTS : " + request.error);
            Debug.LogError("🛠 Code HTTP : " + request.responseCode);
            Debug.LogError("🛠 Réponse brute : " + request.downloadHandler.text);
        }
    }

    string CleanForJson(string input)
    {
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

}
