using System.Windows;
using SchoolTesting.Models;
using SchoolTesting.ViewModels;

namespace SchoolTesting.Views
{
    public partial class TestExecutionWindow : Window
    {
        public TestExecutionWindow(User student, Test test)
        {
            InitializeComponent();
            var vm = new TestExecutionViewModel(student, test);
            DataContext = vm;
            vm.Completed += (result) => { DialogResult = true; Close(); };
            vm.Close += () => Close();
        }
    }
}