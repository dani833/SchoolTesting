using System;
using System.Collections.Generic;

namespace SchoolTesting.Models
{
    public class TestResult
    {
        public string TestId { get; set; }
        public int StudentId { get; set; }
        public DateTime DateTime { get; set; }
        public int ScoreEarned { get; set; }
        public int MaxScore { get; set; }
        public double Percentage => MaxScore == 0 ? 0 : (double)ScoreEarned / MaxScore * 100;
        public List<StudentAnswer> Answers { get; set; } = new List<StudentAnswer>();
        public bool IsCompleted { get; set; }
    }

    public class StudentAnswer
    {
        public string QuestionId { get; set; }
        public string GivenAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }
    }
}