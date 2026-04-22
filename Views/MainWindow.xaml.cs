using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SchoolTesting.ViewModels;
using SchoolTesting.Models;

namespace SchoolTesting.Views
{
    public partial class MainWindow : Window
    {
        MainViewModel vm;
        public MainWindow()
        {
            InitializeComponent();
            vm = new MainViewModel();
            DataContext = vm;
            vm.LoginSuccess += OnLogin;
        }
        private void OnLogin(User u)
        {
            if (u.Role == UserRole.Teacher) new TeacherMainWindow(u).Show();
            else new StudentMainWindow(u).Show();
            Close();
        }
        private void PwdChanged(object sender, RoutedEventArgs e) => vm.Password = ((PasswordBox)sender).Password;
        private void CloseClick(object sender, RoutedEventArgs e) => Close();
        private void Drag(object sender, MouseButtonEventArgs e) => DragMove();
    }
}