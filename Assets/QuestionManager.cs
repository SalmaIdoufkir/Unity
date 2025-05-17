using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class QuestionManager : MonoBehaviour
{
    public AudioSource audioSource;
    public Animator avatarAnimator;

    public string[] questionAudioNames = { "question_1", "question_2", "question_3", "question_4", "question_5", "question_6", "question_7", "question_8" };
    public bool[] requiresResponse = { false, true, true, true, false, true, true, true }; // Pour couvrir toutes les 7 questions
 // Correspond à chaque audio
    private int currentQuestionIndex = 0;

    public AudioRecorder recorder;

    public void StartFirstQuestion()
    {
        currentQuestionIndex = 0;
        PlayCurrentQuestion();
    }

    public void PlayCurrentQuestion()
    {
        StartCoroutine(PlayQuestionAndWait());
    }

    IEnumerator PlayQuestionAndWait()
{
    if (currentQuestionIndex >= questionAudioNames.Length)
    {
        Debug.Log("✅ Toutes les questions ont été posées !");
        yield break;
    }

    string filePath = Path.Combine(UnityEngine.Application.streamingAssetsPath, "AudioQuestions", questionAudioNames[currentQuestionIndex] + ".mp3");
    using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG))
    {
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Erreur de chargement : " + www.error);
            yield break;
        }

        AudioClip questionClip = DownloadHandlerAudioClip.GetContent(www);
        audioSource.clip = questionClip;
        avatarAnimator.SetBool("isTalking", true);
        audioSource.Play();

        yield return new WaitWhile(() => audioSource.isPlaying);
        avatarAnimator.SetBool("isTalking", false);

        if (requiresResponse[currentQuestionIndex])
        {
            Debug.Log("🎤 L'enregistrement peut commencer !");
            recorder.EnableRecording(currentQuestionIndex);
        }
        else
        {
            Debug.Log("ℹ️ Pas de réponse attendue pour cette question.");

            // ✅ Attente spécifique après le message de bienvenue
            yield return new WaitForSeconds(2f); // Délai de 2 secondes (ajuste comme tu veux)

            currentQuestionIndex++;
            PlayCurrentQuestion(); // Continue avec la prochaine question
        }
    }
}


    public void OnRecordingAndUploadFinished()
    {
        currentQuestionIndex++;
        PlayCurrentQuestion();
    }
}
