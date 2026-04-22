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

namespace SchoolTesting.ViewModels
{
    // ---------- MainViewModel ----------
    public class MainViewModel : ViewModelBase
    {
        private readonly JsonDataService ds = new JsonDataService();
        private string login, password, error;
        public string Login { get => login; set => Set(ref login, value); }
        public string Password { get => password; set => Set(ref password, value); }
        public string ErrorMessage { get => error; set => Set(ref error, value); }

        public ICommand LoginTeacher { get; }
        public ICommand LoginStudent { get; }
        public event Action<User> LoginSuccess;

        public MainViewModel()
        {
            LoginTeacher = new RelayCommand(_ => Auth(UserRole.Teacher));
            LoginStudent = new RelayCommand(_ => Auth(UserRole.Student));
            EnsureTestUsers();
        }

        private void Auth(UserRole role)
        {
            ErrorMessage = "";
            var user = ds.LoadUsers().FirstOrDefault(u => u.Login == Login && u.Role == role);
            if (user == null) { ErrorMessage = "Пользователь не найден"; return; }
            if (HashPassword(Password) != user.PasswordHash) { ErrorMessage = "Неверный пароль"; return; }
            LoginSuccess?.Invoke(user);
        }

        private void EnsureTestUsers()
        {
            var users = ds.LoadUsers();
            bool changed = false;
            if (!users.Any(u => u.Login == "teacher"))
            {
                users.Add(new User { Id = 1, Login = "teacher", PasswordHash = HashPassword("123"), Role = UserRole.Teacher, FullName = "Иванов И.И." });
                changed = true;
            }
            if (!users.Any(u => u.Login == "student"))
            {
                users.Add(new User { Id = 2, Login = "student", PasswordHash = HashPassword("123"), Role = UserRole.Student, FullName = "Петров П.П." });
                changed = true;
            }
            if (changed) ds.SaveUsers(users);
        }

        private string HashPassword(string p)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(p)));
        }
    }

    // ---------- TeacherMainViewModel ----------
    public class TeacherMainViewModel : ViewModelBase
    {
        private readonly JsonDataService ds = new JsonDataService();
        private readonly User user;
        public string FullName => user.FullName;
        public event Action Close;

        public TeacherMainViewModel(User u)
        {
            user = u;
            Load();
            Logout = new RelayCommand(_ => Close?.Invoke());
            Create = new RelayCommand(_ => CreateTest());
            Edit = new RelayCommand(_ => EditTest(), _ => Selected != null);
            Delete = new RelayCommand(_ => DeleteTest(), _ => Selected != null);
        }

        private ObservableCollection<Test> tests = new ObservableCollection<Test>();
        public ObservableCollection<Test> Tests { get => tests; set => Set(ref tests, value); }
        private Test selected;
        public Test Selected { get => selected; set { Set(ref selected, value); (Edit as RelayCommand)?.RaiseCanExecuteChanged(); (Delete as RelayCommand)?.RaiseCanExecuteChanged(); } }
        private string status = "Готово";
        public string Status { get => status; set => Set(ref status, value); }

        public ICommand Logout { get; }
        public ICommand Create { get; }
        public ICommand Edit { get; }
        public ICommand Delete { get; }

        private void Load()
        {
            Tests.Clear();
            foreach (var t in ds.LoadAllTests()) Tests.Add(t);
            Status = $"Загружено тестов: {Tests.Count}";
        }
        private void CreateTest()
        {
            var w = new TestEditWindow();
            w.Owner = App.Current.MainWindow;
            if (w.ShowDialog() == true) Load();
        }
        private void EditTest()
        {
            if (Selected == null) return;
            var w = new TestEditWindow(Selected);
            w.Owner = App.Current.MainWindow;
            if (w.ShowDialog() == true) Load();
        }
        private void DeleteTest()
        {
            if (Selected == null) return;
            if (MessageBox.Show($"Удалить '{Selected.Title}'?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ds.DeleteTest(Selected.Id);
                Tests.Remove(Selected);
                Selected = null;
                Status = "Тест удалён";
            }
        }
    }

    // ---------- StudentMainViewModel ----------
    public class StudentMainViewModel : ViewModelBase
    {
        private readonly JsonDataService ds = new JsonDataService();
        private readonly User user;
        public string FullName => user.FullName;
        public event Action Close;

        public StudentMainViewModel(User u)
        {
            user = u;
            Load();
            Logout = new RelayCommand(_ => Close?.Invoke());
            Start = new RelayCommand(p => StartTest(p as Test), p => p is Test);
        }

        private ObservableCollection<Test> tests = new ObservableCollection<Test>();
        public ObservableCollection<Test> Tests { get => tests; set => Set(ref tests, value); }
        private Test selected;
        public Test Selected { get => selected; set { Set(ref selected, value); (Start as RelayCommand)?.RaiseCanExecuteChanged(); } }
        private string status = "Готово";
        public string Status { get => status; set => Set(ref status, value); }

        public ICommand Logout { get; }
        public ICommand Start { get; }

        private void Load()
        {
            var all = ds.LoadAllTests();
            var completed = ds.LoadResults(user.Id).Where(r => r.IsCompleted).Select(r => r.TestId).ToList();
            Tests.Clear();
            foreach (var t in all) if (!completed.Contains(t.Id)) Tests.Add(t);
            Status = $"Доступно: {Tests.Count}";
        }
        private void StartTest(Test t)
        {
            if (t == null) return;
            var w = new TestExecutionWindow(user, t);
            w.Owner = App.Current.MainWindow;
            w.ShowDialog();
            Load();
        }
    }

    // ---------- TestEditViewModel ----------
    public class TestEditViewModel : ViewModelBase
    {
        private readonly JsonDataService ds = new JsonDataService();
        private readonly Test test;
        public event Action<bool> Close;

        public TestEditViewModel(Test t = null)
        {
            test = t ?? new Test();
            Title = test.Title;
            Subject = test.Subject;
            Time = test.TimeLimitMinutes;
            Questions = new ObservableCollection<Question>(test.Questions);

            Save = new RelayCommand(_ => SaveTest());
            Cancel = new RelayCommand(_ => Close?.Invoke(false));
            Add = new RelayCommand(_ => AddQuestion());
            Edit = new RelayCommand(_ => EditQuestion(), _ => Selected != null);
            Delete = new RelayCommand(_ => DeleteQuestion(), _ => Selected != null);
        }

        private string title, subject;
        private int time;
        public string Title { get => title; set => Set(ref title, value); }
        public string Subject { get => subject; set => Set(ref subject, value); }
        public int Time { get => time; set => Set(ref time, value); }
        private ObservableCollection<Question> questions;
        public ObservableCollection<Question> Questions { get => questions; set => Set(ref questions, value); }
        private Question selected;
        public Question Selected { get => selected; set { Set(ref selected, value); (Edit as RelayCommand)?.RaiseCanExecuteChanged(); (Delete as RelayCommand)?.RaiseCanExecuteChanged(); } }

        public ICommand Save { get; }
        public ICommand Cancel { get; }
        public ICommand Add { get; }
        public ICommand Edit { get; }
        public ICommand Delete { get; }

        private void SaveTest()
        {
            if (string.IsNullOrWhiteSpace(Title)) { MessageBox.Show("Введите название"); return; }
            test.Title = Title;
            test.Subject = Subject;
            test.TimeLimitMinutes = Time;
            test.Questions = Questions.ToList();
            ds.SaveTest(test);
            Close?.Invoke(true);
        }
        private void AddQuestion()
        {
            var dlg = new QuestionEditDialog();
            dlg.Owner = App.Current.MainWindow;
            if (dlg.ShowDialog() == true && dlg.Result != null)
                Questions.Add(dlg.Result);
        }
        private void EditQuestion()
        {
            if (Selected == null) return;
            var dlg = new QuestionEditDialog(Selected);
            dlg.Owner = App.Current.MainWindow;
            if (dlg.ShowDialog() == true && dlg.Result != null)
            {
                int idx = Questions.IndexOf(Selected);
                Questions[idx] = dlg.Result;
                Selected = dlg.Result;
            }
        }
        private void DeleteQuestion()
        {
            if (Selected == null) return;
            if (MessageBox.Show("Удалить вопрос?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Questions.Remove(Selected);
                Selected = null;
            }
        }
    }

    // ---------- QuestionEditViewModel и вспомогательные VM ----------
    public class QuestionEditViewModel : ViewModelBase
    {
        private Question orig;
        public event Action<bool, Question> Close;

        public QuestionEditViewModel(Question q = null)
        {
            orig = q ?? new MultipleChoiceQuestion();
            Text = orig.Text;
            Type = orig.Type;
            Types = new ObservableCollection<QuestionType>(Enum.GetValues(typeof(QuestionType)).Cast<QuestionType>());
            CreateContent(Type);
            Ok = new RelayCommand(_ => Save());
            CancelCmd = new RelayCommand(_ => Close?.Invoke(false, null));
        }

        private string text;
        public string Text { get => text; set => Set(ref text, value); }
        private QuestionType type;
        public QuestionType Type { get => type; set { if (Set(ref type, value)) CreateContent(value); } }
        public ObservableCollection<QuestionType> Types { get; }
        private object content;
        public object Content { get => content; set => Set(ref content, value); }

        public ICommand Ok { get; }
        public ICommand CancelCmd { get; }

        private void CreateContent(QuestionType t)
        {
            switch (t)
            {
                case QuestionType.MultipleChoice:
                    var mc = new MultipleChoiceVM();
                    if (orig is MultipleChoiceQuestion m)
                    {
                        mc.Options = new ObservableCollection<OptionItem>();
                        for (int i = 0; i < m.Options.Count; i++)
                            mc.Options.Add(new OptionItem { Text = m.Options[i], IsSelected = i == m.CorrectOptionIndex });
                        mc.Score = m.Score;
                    }
                    Content = mc;
                    break;
                case QuestionType.TextInput:
                    var ti = new TextInputVM();
                    if (orig is TextInputQuestion txt) { ti.Answer = txt.CorrectAnswer; ti.Score = txt.Score; }
                    Content = ti;
                    break;
                case QuestionType.NumberInput:
                    var ni = new NumberInputVM();
                    if (orig is NumberInputQuestion num) { ni.Value = num.CorrectValue; ni.Tol = num.Tolerance; ni.Score = num.Score; }
                    Content = ni;
                    break;
            }
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(Text)) { MessageBox.Show("Введите текст"); return; }
            Question result = null;
            switch (Type)
            {
                case QuestionType.MultipleChoice:
                    var mc = Content as MultipleChoiceVM;
                    if (mc.Options.Count == 0) { MessageBox.Show("Добавьте варианты"); return; }
                    int idx = -1;
                    for (int i = 0; i < mc.Options.Count; i++) if (mc.Options[i].IsSelected) { idx = i; break; }
                    if (idx == -1) { MessageBox.Show("Выберите правильный вариант"); return; }
                    result = new MultipleChoiceQuestion { Text = Text, Score = mc.Score, Options = mc.Options.Select(o => o.Text).ToList(), CorrectOptionIndex = idx };
                    break;
                case QuestionType.TextInput:
                    var ti = Content as TextInputVM;
                    if (string.IsNullOrWhiteSpace(ti.Answer)) { MessageBox.Show("Введите ответ"); return; }
                    result = new TextInputQuestion { Text = Text, Score = ti.Score, CorrectAnswer = ti.Answer };
                    break;
                case QuestionType.NumberInput:
                    var ni = Content as NumberInputVM;
                    result = new NumberInputQuestion { Text = Text, Score = ni.Score, CorrectValue = ni.Value, Tolerance = ni.Tol };
                    break;
            }
            Close?.Invoke(true, result);
        }
    }

    // Вспомогательные VM для редактора вопроса
    public class OptionItem : ViewModelBase
    {
        private string text;
        public string Text { get => text; set => Set(ref text, value); }
        private bool sel;
        public bool IsSelected { get => sel; set => Set(ref sel, value); }
    }

    public class MultipleChoiceVM : ViewModelBase
    {
        public ObservableCollection<OptionItem> Options { get; set; } = new ObservableCollection<OptionItem>();
        private int score = 1;
        public int Score { get => score; set => Set(ref score, value); }
        public ICommand Add => new RelayCommand(_ => Options.Add(new OptionItem { Text = "Новый вариант" }));
        public ICommand Remove => new RelayCommand(p => { if (p is OptionItem o) Options.Remove(o); });
    }

    public class TextInputVM : ViewModelBase
    {
        private string ans;
        public string Answer { get => ans; set => Set(ref ans, value); }
        private int score = 1;
        public int Score { get => score; set => Set(ref score, value); }
    }

    public class NumberInputVM : ViewModelBase
    {
        private double val;
        public double Value { get => val; set => Set(ref val, value); }
        private double tol = 0.001;
        public double Tol { get => tol; set => Set(ref tol, value); }
        private int score = 1;
        public int Score { get => score; set => Set(ref score, value); }
    }

    // ---------- TestExecutionViewModel и вспомогательные VM ----------
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

            Next = new RelayCommand(_ => MoveNext(), _ => idx < qvms.Count - 1);
            Prev = new RelayCommand(_ => MovePrev(), _ => idx > 0);
            Finish = new RelayCommand(_ => FinishTest());
        }

        public string TestTitle => test.Title;
        public string Progress => $"Вопрос {idx + 1} из {qvms.Count}";
        public double Percent => (double)(idx + 1) / qvms.Count * 100;
        public string TimeLeft => remaining.ToString(@"mm\:ss");
        public string TimerColor => remaining.TotalMinutes < 5 ? "#E74C3C" : "White";
        public string CurrentText => test.Questions[idx].Text;
        public QuestionVMBase CurrentVM => qvms[idx];

        public ICommand Next { get; }
        public ICommand Prev { get; }
        public ICommand Finish { get; }

        private void Tick(object s, EventArgs e)
        {
            if (remaining.TotalSeconds > 0)
            {
                remaining = remaining.Subtract(TimeSpan.FromSeconds(1));
                OnPropertyChanged(nameof(TimeLeft));
                OnPropertyChanged(nameof(TimerColor));
            }
            else { timer.Stop(); FinishTest(true); }
        }

        private void MoveNext() { if (idx < qvms.Count - 1) { idx++; UpdateNav(); } }
        private void MovePrev() { if (idx > 0) { idx--; UpdateNav(); } }
        private void UpdateNav()
        {
            OnPropertyChanged(nameof(CurrentText)); OnPropertyChanged(nameof(CurrentVM));
            OnPropertyChanged(nameof(Progress)); OnPropertyChanged(nameof(Percent));
            (Next as RelayCommand)?.RaiseCanExecuteChanged(); (Prev as RelayCommand)?.RaiseCanExecuteChanged();
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
            if (q is MultipleChoiceQuestion mc) return new MultipleChoiceQuestionVM(mc);
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
            foreach (var o in q.Options) Options.Add(new OptionItem { Text = o });
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

    // ---------- TestResultViewModel ----------
    public class TestResultViewModel : ViewModelBase
    {
        public string Title { get; }
        public string ScoreText { get; }
        public string PercentageText { get; }
        public System.Collections.Generic.List<AnswerItem> Answers { get; }

        public TestResultViewModel(TestResult r, Test t)
        {
            Title = t.Title;
            ScoreText = $"{r.ScoreEarned} из {r.MaxScore}";
            PercentageText = $"{r.Percentage:F1}%";
            Answers = new System.Collections.Generic.List<AnswerItem>();
            foreach (var a in r.Answers)
            {
                var q = t.Questions.FirstOrDefault(x => x.Id == a.QuestionId);
                Answers.Add(new AnswerItem
                {
                    QuestionText = q?.Text ?? "?",
                    YourAnswer = a.GivenAnswer,
                    CorrectAnswer = GetCorrect(q),
                    IsCorrect = a.IsCorrect
                });
            }
        }

        private string GetCorrect(Question q)
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