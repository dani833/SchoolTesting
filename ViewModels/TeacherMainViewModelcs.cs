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
            LoadStudents();
            CheckForWarnings();

            Logout = new RelayCommand(_ => Close?.Invoke());
            Create = new RelayCommand(_ => CreateTest());
            Edit = new RelayCommand(_ => EditTest(), _ => Selected != null);
            Delete = new RelayCommand(_ => DeleteTest(), _ => Selected != null);
            RefreshStatisticsCommand = new RelayCommand(_ => LoadStatisticsResults());
            ExportExcelCommand = new RelayCommand(_ => ExportToExcel(), _ => StatisticsResults.Any());
            ExportPdfCommand = new RelayCommand(_ => ExportToPdf(), _ => StatisticsResults.Any());
        }

        // ------------------ Тесты ------------------
        private ObservableCollection<Test> tests = new ObservableCollection<Test>();
        public ObservableCollection<Test> Tests { get => tests; set => Set(ref tests, value); }

        private Test selected;
        public Test Selected
        {
            get => selected;
            set
            {
                Set(ref selected, value);
                (Edit as RelayCommand)?.RaiseCanExecuteChanged();
                (Delete as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string status = "Готово";
        public string Status { get => status; set => Set(ref status, value); }

        // ------------------ Статистика ------------------
        public ObservableCollection<User> Students { get; } = new ObservableCollection<User>();
        public ObservableCollection<StatisticsItem> StatisticsResults { get; } = new ObservableCollection<StatisticsItem>();

        private User selectedStudent;
        public User SelectedStudent
        {
            get => selectedStudent;
            set { Set(ref selectedStudent, value); LoadStatisticsResults(); }
        }

        // ------------------ Команды ------------------
        public ICommand Logout { get; }
        public ICommand Create { get; }
        public ICommand Edit { get; }
        public ICommand Delete { get; }
        public ICommand RefreshStatisticsCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand ExportPdfCommand { get; }

        // ------------------ Методы ------------------
        private void Load()
        {
            Tests.Clear();
            foreach (var t in ds.LoadAllTests()) Tests.Add(t);
            Status = $"Загружено тестов: {Tests.Count}";
        }

        private void LoadStudents()
        {
            var allUsers = ds.LoadUsers();
            Students.Clear();
            foreach (var u in allUsers.Where(x => x.Role == UserRole.Student))
                Students.Add(u);
            SelectedStudent = Students.FirstOrDefault();
        }

        private void LoadStatisticsResults()
        {
            StatisticsResults.Clear();
            if (SelectedStudent == null) return;

            var results = ds.LoadResults(SelectedStudent.Id);
            var allTests = ds.LoadAllTests();

            foreach (var r in results.Where(x => x.IsCompleted))
            {
                var test = allTests.FirstOrDefault(t => t.Id == r.TestId);
                StatisticsResults.Add(new StatisticsItem
                {
                    DateTime = r.DateTime,
                    TestTitle = test?.Title ?? "Неизвестный тест",
                    ScoreEarned = r.ScoreEarned,
                    MaxScore = r.MaxScore,
                    Percentage = r.Percentage
                });
            }
        }

        private void CheckForWarnings()
        {
            var allResults = ds.LoadAllResults();
            var lowResults = allResults.Where(r => r.IsCompleted && r.Percentage < 40).ToList();
            var incomplete = allResults.Where(r => !r.IsCompleted).ToList();

            if (lowResults.Any() || incomplete.Any())
            {
                string msg = "Внимание!\n";
                if (lowResults.Any()) msg += $"- Низкие результаты (<40%): {lowResults.Count}\n";
                if (incomplete.Any()) msg += $"- Незавершённые тесты: {incomplete.Count}\n";
                MessageBox.Show(msg, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CreateTest()
        {
            var w = new TestEditWindow();
            w.Owner = Application.Current.Windows.OfType<TeacherMainWindow>().FirstOrDefault();
            if (w.ShowDialog() == true) Load();
        }

        private void EditTest()
        {
            if (Selected == null) return;
            var w = new TestEditWindow(Selected);
            w.Owner = Application.Current.Windows.OfType<TeacherMainWindow>().FirstOrDefault();
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

        private void ExportToExcel()
        {
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
                    foreach (var r in StatisticsResults)
                        writer.WriteLine($"{r.DateTime:dd.MM.yyyy HH:mm};{r.TestTitle};{r.ScoreEarned};{r.MaxScore};{r.Percentage:F1}");
                }
                MessageBox.Show("Экспорт завершён.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportToPdf()
        {
            if (!StatisticsResults.Any())
            {
                MessageBox.Show("Нет данных для экспорта.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"results_{SelectedStudent?.Login}.pdf"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using (var fs = new System.IO.FileStream(dlg.FileName, System.IO.FileMode.Create))
                {
                    var document = new iTextSharp.text.Document();
                    var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, fs);
                    document.Open();

                    var titleFont = iTextSharp.text.FontFactory.GetFont("Arial", 16, iTextSharp.text.Font.BOLD);
                    document.Add(new iTextSharp.text.Paragraph($"Результаты ученика: {SelectedStudent?.FullName}", titleFont));
                    document.Add(new iTextSharp.text.Paragraph($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}"));
                    document.Add(new iTextSharp.text.Paragraph("\n"));

                    var table = new iTextSharp.text.pdf.PdfPTable(5);
                    table.WidthPercentage = 100;
                    table.SetWidths(new float[] { 2f, 3f, 1.5f, 1.5f, 1.5f });

                    var headerFont = iTextSharp.text.FontFactory.GetFont("Arial", 12, iTextSharp.text.Font.BOLD);
                    table.AddCell(new iTextSharp.text.Phrase("Дата", headerFont));
                    table.AddCell(new iTextSharp.text.Phrase("Тест", headerFont));
                    table.AddCell(new iTextSharp.text.Phrase("Баллы", headerFont));
                    table.AddCell(new iTextSharp.text.Phrase("Макс.", headerFont));
                    table.AddCell(new iTextSharp.text.Phrase("%", headerFont));

                    var cellFont = iTextSharp.text.FontFactory.GetFont("Arial", 10);
                    foreach (var r in StatisticsResults)
                    {
                        table.AddCell(new iTextSharp.text.Phrase(r.DateTime.ToString("dd.MM.yyyy HH:mm"), cellFont));
                        table.AddCell(new iTextSharp.text.Phrase(r.TestTitle, cellFont));
                        table.AddCell(new iTextSharp.text.Phrase(r.ScoreEarned.ToString(), cellFont));
                        table.AddCell(new iTextSharp.text.Phrase(r.MaxScore.ToString(), cellFont));
                        table.AddCell(new iTextSharp.text.Phrase(r.Percentage.ToString("F1"), cellFont));
                    }

                    document.Add(table);
                    document.Close();
                }

                MessageBox.Show("PDF-отчёт сохранён.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании PDF: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class StatisticsItem
    {
        public DateTime DateTime { get; set; }
        public string TestTitle { get; set; }
        public int ScoreEarned { get; set; }
        public int MaxScore { get; set; }
        public double Percentage { get; set; }
    }
}