using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SchoolTesting.Models;
using SchoolTesting.ViewModels.Base;

namespace SchoolTesting.ViewModels
{
    public class QuestionEditViewModel : ViewModelBase
    {
        private readonly Question originalQuestion;
        public event Action<bool, Question> CloseRequested;

        public ObservableCollection<QuestionType> QuestionTypes { get; } = new ObservableCollection<QuestionType>();

        private QuestionType selectedType;
        public QuestionType SelectedType
        {
            get => selectedType;
            set
            {
                if (Set(ref selectedType, value))
                {
                    OnPropertyChanged(nameof(IsMultipleChoice));
                    OnPropertyChanged(nameof(IsTextInput));
                    OnPropertyChanged(nameof(IsNumberInput));
                }
            }
        }

        public bool IsMultipleChoice => SelectedType == QuestionType.Варианты;
        public bool IsTextInput => SelectedType == QuestionType.ВводТекста;
        public bool IsNumberInput => SelectedType == QuestionType.ВводЧислового;

        private string questionText;
        public string QuestionText { get => questionText; set => Set(ref questionText, value); }

        private int score = 1;
        public int Score { get => score; set => Set(ref score, value); }

        public ObservableCollection<OptionItem> Options { get; } = new ObservableCollection<OptionItem>();
        private string correctAnswer;
        public string CorrectAnswer { get => correctAnswer; set => Set(ref correctAnswer, value); }
        private double correctValue;
        public double CorrectValue { get => correctValue; set => Set(ref correctValue, value); }
        private double tolerance = 0.001;
        public double Tolerance { get => tolerance; set => Set(ref tolerance, value); }

        public ICommand AddOptionCommand { get; }
        public ICommand RemoveOptionCommand { get; }
        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public QuestionEditViewModel(Question question = null)
        {
            foreach (QuestionType t in Enum.GetValues(typeof(QuestionType)))
                QuestionTypes.Add(t);

            if (question == null)
            {
                originalQuestion = new MultipleChoiceQuestion();
                SelectedType = QuestionType.Варианты;
            }
            else
            {
                originalQuestion = question;
                QuestionText = question.Text;
                Score = question.Score;
                SelectedType = question.Type;

                switch (question)
                {
                    case MultipleChoiceQuestion mc:
                        for (int i = 0; i < mc.Options.Count; i++)
                            Options.Add(new OptionItem { Text = mc.Options[i], IsSelected = i == mc.CorrectOptionIndex });
                        break;
                    case TextInputQuestion ti:
                        CorrectAnswer = ti.CorrectAnswer;
                        break;
                    case NumberInputQuestion ni:
                        CorrectValue = ni.CorrectValue;
                        Tolerance = ni.Tolerance;
                        break;
                }
            }

            AddOptionCommand = new RelayCommand(_ => Options.Add(new OptionItem { Text = "Новый вариант" }));
            RemoveOptionCommand = new RelayCommand(p => { if (p is OptionItem o) Options.Remove(o); });
            OkCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(false, null));
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(QuestionText))
            {
                MessageBox.Show("Введите текст вопроса");
                return;
            }

            Question result;
            switch (SelectedType)
            {
                case QuestionType.Варианты:
                    if (Options.Count == 0) { MessageBox.Show("Добавьте варианты"); return; }
                    int correctIndex = -1;
                    for (int i = 0; i < Options.Count; i++)
                        if (Options[i].IsSelected) { correctIndex = i; break; }
                    if (correctIndex == -1) { MessageBox.Show("Выберите правильный вариант"); return; }
                    result = new MultipleChoiceQuestion
                    {
                        Text = QuestionText,
                        Score = Score,
                        Options = Options.Select(o => o.Text).ToList(),
                        CorrectOptionIndex = correctIndex
                    };
                    break;
                case QuestionType.ВводТекста:
                    if (string.IsNullOrWhiteSpace(CorrectAnswer)) { MessageBox.Show("Введите правильный ответ"); return; }
                    result = new TextInputQuestion { Text = QuestionText, Score = Score, CorrectAnswer = CorrectAnswer };
                    break;
                case QuestionType.ВводЧислового:
                    result = new NumberInputQuestion { Text = QuestionText, Score = Score, CorrectValue = CorrectValue, Tolerance = Tolerance };
                    break;
                default:
                    return;
            }
            CloseRequested?.Invoke(true, result);
        }
    }

    
}