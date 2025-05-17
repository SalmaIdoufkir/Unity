using UnityEngine;

public class QuestionStorage : MonoBehaviour
{
    public static Question[] Questions;
}

[System.Serializable]
public class Question
{
    public int id;
    public string question;
    public string answer;
    public string correctness;
}
