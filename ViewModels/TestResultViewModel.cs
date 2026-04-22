using System.Collections.Generic;
using System.Linq;
using SchoolTesting.Models;
using SchoolTesting.ViewModels.Base;

namespace SchoolTesting.ViewModels
{
    public class TestResultViewModel : ViewModelBase
    {
        public string Title { get; }
        public string ScoreText { get; }
        public string PercentageText { get; }
        public List<AnswerItem> Answers { get; }

        public TestResultViewModel(TestResult result, Test test)
        {
            Title = test.Title;
            ScoreText = $"Набрано баллов: {result.ScoreEarned} из {result.MaxScore}";
            PercentageText = $"Процент выполнения: {result.Percentage:F1}%";

            Answers = new List<AnswerItem>();
            for (int i = 0; i < result.Answers.Count; i++)
            {
                var ans = result.Answers[i];
                var question = test.Questions.FirstOrDefault(q => q.Id == ans.QuestionId);
                Answers.Add(new AnswerItem
                {
                    QuestionText = question?.Text ?? "Вопрос не найден",
                    YourAnswer = ans.GivenAnswer,
                    CorrectAnswer = GetCorrectAnswer(question),
                    IsCorrect = ans.IsCorrect
                });
            }
        }

        private string GetCorrectAnswer(Question q)
        {
            if (q is MultipleChoiceQuestion mc) return mc.Options[mc.CorrectOptionIndex];
            if (q is TextInputQuestion ti) return ti.CorrectAnswer;
            if (q is NumberInputQuestion ni) return ni.CorrectValue.ToString();
            return "";
        }
    }

    public class AnswerItem
    {
        public string QuestionText { get; set; }
        public string YourAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public bool IsCorrect { get; set; }
    }
}