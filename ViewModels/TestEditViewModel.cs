using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SchoolTesting.Models;
using SchoolTesting.Services;
using SchoolTesting.ViewModels.Base;
using SchoolTesting.Views;

namespace SchoolTesting.ViewModels
{
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
        public Question Selected
        {
            get => selected;
            set
            {
                Set(ref selected, value);
                (Edit as RelayCommand)?.RaiseCanExecuteChanged();
                (Delete as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

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
            dlg.Owner = Application.Current.MainWindow;
            if (dlg.ShowDialog() == true && dlg.ResultQuestion != null)
                Questions.Add(dlg.ResultQuestion);
        }

        private void EditQuestion()
        {
            if (Selected == null) return;
            var dlg = new QuestionEditDialog(Selected);
            dlg.Owner = Application.Current.MainWindow;
            if (dlg.ShowDialog() == true && dlg.ResultQuestion != null)
            {
                int idx = Questions.IndexOf(Selected);
                Questions[idx] = dlg.ResultQuestion;
                Selected = dlg.ResultQuestion;
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
}