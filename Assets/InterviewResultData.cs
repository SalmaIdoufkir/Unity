using System;

[Serializable]
public class InterviewResult
{
    public string interview_id;
    public QuestionFeedback[] questions;
    public int expected_count;
    public string created_at;
    public string completed_at;
    public string status;
}

[Serializable]
public class QuestionFeedback
{
    public string question_enonce;
    public string response_text;
    public float confidence;
    public int correctness;
    public string feedback;
}
