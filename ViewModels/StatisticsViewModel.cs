using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SchoolTesting.Models;
using SchoolTesting.Services;
using SchoolTesting.ViewModels.Base;

namespace SchoolTesting.ViewModels
{
    public class StatisticsViewModel : ViewModelBase
    {
        private readonly JsonDataService ds = new JsonDataService();

        public ObservableCollection<User> Students { get; } = new ObservableCollection<User>();
        public ObservableCollection<TestResult> Results { get; } = new ObservableCollection<TestResult>();

        private User selectedStudent;
        public User SelectedStudent
        {
            get => selectedStudent;
            set { Set(ref selectedStudent, value); LoadResults(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand ExportPdfCommand { get; }

        public StatisticsViewModel()
        {
            LoadStudents();
            RefreshCommand = new RelayCommand(_ => LoadResults());
            ExportExcelCommand = new RelayCommand(_ => ExportToExcel());
            ExportPdfCommand = new RelayCommand(_ => ExportToPdf());
        }

        private void LoadStudents()
        {
            var users = ds.LoadUsers().Where(u => u.Role == UserRole.Student).ToList();
            Students.Clear();
            foreach (var u in users) Students.Add(u);
            SelectedStudent = Students.FirstOrDefault();
        }

        private void LoadResults()
        {
            Results.Clear();
            if (SelectedStudent == null) return;
            var results = ds.LoadResults(SelectedStudent.Id);
            foreach (var r in results) Results.Add(r);
        }

        private void ExportToExcel()
        {
            if (!Results.Any())
            {
                System.Windows.MessageBox.Show("Нет данных для экспорта.", "Информация", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"results_{SelectedStudent?.Login}.csv"
            };
            if (dlg.ShowDialog() == true)
            {
                using (var writer = new System.IO.StreamWriter(dlg.FileName, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("Дата;Тест;Баллы;Макс.;%");
                    foreach (var r in Results)
                    {
                        var test = ds.LoadAllTests().FirstOrDefault(t => t.Id == r.TestId);
                        writer.WriteLine($"{r.DateTime:dd.MM.yyyy HH:mm};{test?.Title ?? "?"};{r.ScoreEarned};{r.MaxScore};{r.Percentage:F1}");
                    }
                }
                System.Windows.MessageBox.Show("Экспорт завершён.", "Готово", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private void ExportToPdf()
        {
            System.Windows.MessageBox.Show("Экспорт в PDF в разработке.", "Информация", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}