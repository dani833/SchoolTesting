using SchoolTesting.Models;
using SchoolTesting.Services;
using SchoolTesting.ViewModels.Base;
using SchoolTesting.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SchoolTesting.ViewModels
{
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
        public Test Selected
        {
            get => selected;
            set
            {
                Set(ref selected, value);
                (Start as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string status = "Готово";
        public string Status { get => status; set => Set(ref status, value); }

        public ICommand Logout { get; }
        public ICommand Start { get; }

        private void Load()
        {
            var all = ds.LoadAllTests();
            var completed = ds.LoadResults(user.Id).Where(r => r.IsCompleted).Select(r => r.TestId).ToList();
            Tests.Clear();
            foreach (var t in all)
                if (!completed.Contains(t.Id)) Tests.Add(t);
            Status = $"Доступно: {Tests.Count}";
            // ВРЕМЕННО: создаём тестовый тест для отладки
            if (Tests.Count == 0)
            {
                var test = new Test
                {
                    Title = "Отладочный тест",
                    Subject = "Отладка",
                    TimeLimitMinutes = 5,
                    Questions = new List<Question>
        {
            new MultipleChoiceQuestion
            {
                Text = "Какой цвет неба?",
                Options = new List<string> { "Синий", "Зелёный", "Красный" },
                CorrectOptionIndex = 0,
                Score = 1
            }
        }
                };
                ds.SaveTest(test);
                Tests.Add(test);
            }
        }

        private void StartTest(Test t)
        {
            if (t == null) return;
            var w = new TestExecutionWindow(user, t);
            w.Owner = Application.Current.MainWindow;
            w.ShowDialog();
            Load();
        }
    }
}