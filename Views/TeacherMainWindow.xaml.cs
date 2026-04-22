using System.Windows;
using SchoolTesting.Models;
using SchoolTesting.ViewModels;

namespace SchoolTesting.Views
{
    public partial class TeacherMainWindow : Window
    {
        public TeacherMainWindow(User u)
        {
            InitializeComponent();
            DataContext = new TeacherMainViewModel(u);
            ((TeacherMainViewModel)DataContext).Close += () => Close();
        }
    }
}