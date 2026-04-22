using System.Collections.Generic;
using Newtonsoft.Json;

namespace SchoolTesting.Models
{
    public class Test
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Subject { get; set; }
        public int TimeLimitMinutes { get; set; }
        public List<Question> Questions { get; set; }

        public Test()
        {
            Id = System.Guid.NewGuid().ToString();
            Questions = new List<Question>();
        }

        [JsonIgnore]
        public int MaxScore
        {
            get
            {
                int sum = 0;
                foreach (var q in Questions)
                    sum += q.Score;
                return sum;
            }
        }
    }

    [JsonObject]
    public abstract class Question
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public int Score { get; set; }

        public Question() => Id = System.Guid.NewGuid().ToString();
        public abstract QuestionType Type { get; }
    }

    public enum QuestionType { MultipleChoice, TextInput, NumberInput }

    public class MultipleChoiceQuestion : Question
    {
        public override QuestionType Type => QuestionType.MultipleChoice;
        public List<string> Options { get; set; } = new List<string>();
        public int CorrectOptionIndex { get; set; }
    }

    public class TextInputQuestion : Question
    {
        public override QuestionType Type => QuestionType.TextInput;
        public string CorrectAnswer { get; set; }
    }

    public class NumberInputQuestion : Question
    {
        public override QuestionType Type => QuestionType.NumberInput;
        public double CorrectValue { get; set; }
        public double Tolerance { get; set; } = 0.001;
    }
}