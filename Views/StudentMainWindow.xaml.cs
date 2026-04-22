using System.Windows;
using SchoolTesting.Models;
using SchoolTesting.ViewModels;

namespace SchoolTesting.Views
{
    public partial class StudentMainWindow : Window
    {
        public StudentMainWindow(User u)
        {
            InitializeComponent();
            DataContext = new StudentMainViewModel(u);
            ((StudentMainViewModel)DataContext).Close += () => Close();
        }
    }
}