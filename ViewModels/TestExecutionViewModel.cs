using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SchoolTesting.Models;
using SchoolTesting.Services;
using SchoolTesting.ViewModels.Base;
using SchoolTesting.Views;
using System.Diagnostics;
namespace SchoolTesting.ViewModels
{



    public class TestExecutionViewModel : ViewModelBase
    {
        private readonly JsonDataService ds = new JsonDataService();
        private readonly User student;
        private readonly Test test;
        private readonly DispatcherTimer timer;
        private TimeSpan remaining;
        private int idx = 0;
        private ObservableCollection<QuestionVMBase> qvms;

        public event Action<TestResult> Completed;
        public event Action Close;

        public TestExecutionViewModel(User s, Test t)
        {
            student = s; test = t;
            remaining = TimeSpan.FromMinutes(t.TimeLimitMinutes);
            qvms = new ObservableCollection<QuestionVMBase>();
            foreach (var q in t.Questions) qvms.Add(CreateVM(q));

            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += Tick;
            timer.Start();

            NextCommand = new RelayCommand(_ => MoveNext(), _ => idx < qvms.Count - 1);
            PreviousCommand = new RelayCommand(_ => MovePrev(), _ => idx > 0);
            FinishCommand = new RelayCommand(_ => FinishTest());
        }

        public string TestTitle => test.Title;
        public string ProgressText => $"Вопрос {idx + 1} из {qvms.Count}";
        public double ProgressPercent => (double)(idx + 1) / qvms.Count * 100;
        public string RemainingTime => remaining.ToString(@"mm\:ss");
        public string TimerColor => remaining.TotalMinutes < 5 ? "#E74C3C" : "White";
        public string CurrentQuestionText => test.Questions[idx].Text;
        public QuestionVMBase CurrentQuestionViewModel => qvms[idx];

        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand FinishCommand { get; }

        private void Tick(object s, EventArgs e)
        {
            if (remaining.TotalSeconds > 0)
            {
                remaining = remaining.Subtract(TimeSpan.FromSeconds(1));
                OnPropertyChanged(nameof(RemainingTime));
                OnPropertyChanged(nameof(TimerColor));
            }
            else { timer.Stop(); FinishTest(true); }
        }

        private void MoveNext() { if (idx < qvms.Count - 1) { idx++; UpdateNav(); } }
        private void MovePrev() { if (idx > 0) { idx--; UpdateNav(); } }
        private void UpdateNav()
        {
            OnPropertyChanged(nameof(CurrentQuestionText));
            OnPropertyChanged(nameof(CurrentQuestionViewModel));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(ProgressPercent));
            (NextCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PreviousCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void FinishTest(object _ = null) => FinishTest(false);
        private void FinishTest(bool timeExpired)
        {
            timer.Stop();
            if (!timeExpired && MessageBox.Show("Завершить тест?", "", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            { timer.Start(); return; }

            int earned = 0;
            var answers = new System.Collections.Generic.List<StudentAnswer>();
            for (int i = 0; i < test.Questions.Count; i++)
            {
                var q = test.Questions[i];
                var vm = qvms[i];
                bool ok = vm.IsCorrect(q);
                int pts = ok ? q.Score : 0;
                earned += pts;
                answers.Add(new StudentAnswer { QuestionId = q.Id, GivenAnswer = vm.GetAnswer(), IsCorrect = ok, PointsEarned = pts });
            }
            var result = new TestResult { TestId = test.Id, StudentId = student.Id, DateTime = DateTime.Now, ScoreEarned = earned, MaxScore = test.MaxScore, Answers = answers, IsCompleted = true };
            ds.SaveResult(result);
            new TestResultWindow(result, test).ShowDialog();
            Completed?.Invoke(result);
            Close?.Invoke();
        }

        private QuestionVMBase CreateVM(Question q)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Создание VM для вопроса типа {q.Type}, текст: {q.Text}");

            if (q is MultipleChoiceQuestion mc)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG]   Исходных вариантов: {mc.Options.Count}");
                foreach (var opt in mc.Options)
                    System.Diagnostics.Debug.WriteLine($"[DEBUG]     - {opt}");

                var vm = new MultipleChoiceQuestionVM(mc);
                System.Diagnostics.Debug.WriteLine($"[DEBUG]   Вариантов в VM: {vm.Options.Count}");
                return vm;
            }
            if (q is TextInputQuestion ti) return new TextInputQuestionVM(ti);
            if (q is NumberInputQuestion ni) return new NumberInputQuestionVM(ni);
            throw new NotSupportedException();
        }
    }

    public abstract class QuestionVMBase : ViewModelBase
    {
        public abstract bool IsCorrect(Question q);
        public abstract string GetAnswer();
    }

    public class MultipleChoiceQuestionVM : QuestionVMBase
    {
        public ObservableCollection<OptionItem> Options { get; } = new ObservableCollection<OptionItem>();
        public MultipleChoiceQuestionVM(MultipleChoiceQuestion q)
        {
            foreach (var o in q.Options)
                Options.Add(new OptionItem { Text = o });
        }
        public override bool IsCorrect(Question q) =>
            q is MultipleChoiceQuestion mc && Options.Select((o, i) => new { o, i }).FirstOrDefault(x => x.o.IsSelected)?.i == mc.CorrectOptionIndex;
        public override string GetAnswer() => Options.FirstOrDefault(o => o.IsSelected)?.Text ?? "";
    }

    public class TextInputQuestionVM : QuestionVMBase
    {
        private string answer;
        public string Answer { get => answer; set => Set(ref answer, value); }
        public TextInputQuestionVM(TextInputQuestion q) { }
        public override bool IsCorrect(Question q) =>
            q is TextInputQuestion ti && string.Equals(Answer?.Trim() ?? "", ti.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
        public override string GetAnswer() => Answer ?? "";
    }

    public class NumberInputQuestionVM : QuestionVMBase
    {
        private string answer;
        public string Answer { get => answer; set => Set(ref answer, value); }
        public NumberInputQuestionVM(NumberInputQuestion q) { }
        public override bool IsCorrect(Question q) =>
            q is NumberInputQuestion ni && double.TryParse(Answer, out double v) && Math.Abs(v - ni.CorrectValue) <= ni.Tolerance;
        public override string GetAnswer() => Answer ?? "";
    }

   
}