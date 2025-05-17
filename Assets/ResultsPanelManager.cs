using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ResultsPanelManager : MonoBehaviour
{
    public List<TextMeshProUGUI> questionTexts;
    public List<TextMeshProUGUI> responseTexts;
    public List<TextMeshProUGUI> confidenceTexts;
    public List<TextMeshProUGUI> correctnessTexts;
    public List<TextMeshProUGUI> feedbackTexts;

    public GameObject resultsPanel;

    void Start()
    {
        resultsPanel.SetActive(false); // Hide the panel initially
        StartCoroutine(PollForCompletion());
    }

    IEnumerator PollForCompletion()
    {
        while (true)
        {
            string currentId = InterviewSession.InterviewId;
            string url = $"http://localhost:5000/get_results/{currentId}";
            UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            string json = request.downloadHandler.text;
            Debug.Log("✅ Raw JSON:\n" + json);

            if (json.Contains("\"status\": \"completed\"")) // ✅ This matches the actual JSON
            {
                InterviewResult result = JsonUtility.FromJson<InterviewResult>(json);

                if (result != null && result.questions != null)
                {
                    Debug.Log("✅ Completed interview received with " + result.questions.Length + " questions.");
                    PopulateUI(result);
                    break;
                }
                else
                {
                    Debug.Log("❌ Parsed result is null or missing questions.");
                }
            }
            else
            {
                Debug.Log("⏳ Interview not completed yet.");
            }

            //if (request.result == UnityWebRequest.Result.Success)
            //{
            //    Debug.Log("✅ Raw JSON:\n" + request.downloadHandler.text);

            //    InterviewResult result = JsonUtility.FromJson<InterviewResult>(request.downloadHandler.text);

            //    if (result != null && result.status == "completed" && result.questions != null)
            //    {
            //        Debug.Log("✅ Completed interview received with " + result.questions.Length + " questions.");
            //        PopulateUI(result);
            //        break;
            //    }
            //    else
            //    {
            //        Debug.Log("❌ Parsed result is null or missing questions.");
            //    }
            //}
            //else
            //{
            //    Debug.LogWarning("Waiting for completion…");
            //}

            yield return new WaitForSeconds(3f);
        }
    }

    void PopulateUI(InterviewResult result)
    {
        resultsPanel.SetActive(true); // Show the results panel

        for (int i = 0; i < result.questions.Length && i < 6; i++)
        {
            var q = result.questions[i];
            questionTexts[i].text = "Q: " + q.question_enonce;
            responseTexts[i].text = "Your Answer: " + q.response_text;
            confidenceTexts[i].text = "Confidence: " + (q.confidence * 100f).ToString("F1") + "%";
            correctnessTexts[i].text = "Correct: " + (q.correctness == 1 ? "Yes" : "No");
            feedbackTexts[i].text = "Feedback: " + q.feedback;
        }
    }
}