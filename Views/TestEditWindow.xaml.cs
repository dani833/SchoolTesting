using System.Windows;
using SchoolTesting.Models;
using SchoolTesting.ViewModels;

namespace SchoolTesting.Views
{
    public partial class TestEditWindow : Window
    {
        public TestEditWindow(Test t = null)
        {
            InitializeComponent();
            var vm = new TestEditViewModel(t);
            DataContext = vm;
            vm.Close += (r) => { DialogResult = r; Close(); };
        }
    }
}