using System.Windows;
using SchoolTesting.Models;
using SchoolTesting.ViewModels;

namespace SchoolTesting.Views
{
    public partial class TestResultWindow : Window
    {
        public TestResultWindow(TestResult r, Test t)
        {
            InitializeComponent();
            DataContext = new TestResultViewModel(r, t);
        }
    }
}