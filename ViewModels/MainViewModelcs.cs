using System;
using System.Linq;
using System.Windows.Input;
using SchoolTesting.Models;
using SchoolTesting.Services;
using SchoolTesting.ViewModels.Base;

namespace SchoolTesting.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly JsonDataService ds = new JsonDataService();
        private string login, password, error;
        public string Login { get => login; set => Set(ref login, value); }
        public string Password { get => password; set => Set(ref password, value); }
        public string ErrorMessage { get => error; set => Set(ref error, value); }

        public ICommand LoginTeacher { get; }
        public ICommand LoginStudent { get; }
        public event Action<User> LoginSuccess;

        public MainViewModel()
        {
            LoginTeacher = new RelayCommand(_ => Auth(UserRole.Teacher));
            LoginStudent = new RelayCommand(_ => Auth(UserRole.Student));
            EnsureTestUsers();
        }

        private void Auth(UserRole role)
        {
            ErrorMessage = "";
            var user = ds.LoadUsers().FirstOrDefault(u => u.Login == Login && u.Role == role);
            if (user == null) { ErrorMessage = "Пользователь не найден"; return; }
            if (!BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
            {
                ErrorMessage = "Неверный пароль";
                return;
            }
            LoginSuccess?.Invoke(user);
        }

        private void EnsureTestUsers()
        {
            var users = ds.LoadUsers();
            bool changed = false;
            if (!users.Any(u => u.Login == "Учитель"))
            {
                users.Add(new User
                {
                    Id = 1,
                    Login = "Учитель",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
                    Role = UserRole.Teacher,
                    FullName = "Иванов И.И."
                });
                changed = true;
            }
            if (!users.Any(u => u.Login == "Ученик"))
            {
                users.Add(new User
                {
                    Id = 2,
                    Login = "Ученик",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
                    Role = UserRole.Student,
                    FullName = "Петров П.П."
                });
                changed = true;
            }
            if (changed) ds.SaveUsers(users);
        }
    }
}