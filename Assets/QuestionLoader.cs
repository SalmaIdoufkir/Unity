using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

public class QuestionLoader : MonoBehaviour
{
    public TMP_Text[] questionTexts;
    public TMP_Text[] answerTexts; // Ajouter un tableau de TMP_Text pour les réponses

    void Start()
    {
        StartCoroutine(GetQuestions());
    }

    IEnumerator GetQuestions()
    {
        UnityWebRequest www = UnityWebRequest.Get("http://localhost:5000/questions");
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string json = www.downloadHandler.text;
            Debug.Log("JSON reçu : " + json);

            Question[] questions = JsonHelper.FromJson<Question>(json);
            QuestionStorage.Questions = questions;

            int displayCount = Mathf.Min(questions.Length, questionTexts.Length);
            for (int i = 0; i < displayCount; i++)
            {
                questionTexts[i].text = questions[i].question;
                answerTexts[i].text = questions[i].answer; // Afficher la réponse associée à la question
                Debug.Log("Question " + i + ": " + questions[i].question + " | Answer: " + questions[i].answer);
            }
        }
        else
        {
            Debug.LogError("Erreur : " + www.error);
        }
    }
}
