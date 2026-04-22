namespace SchoolTesting.Models
{
    public enum UserRole { Teacher, Student }

    public class User
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string PasswordHash { get; set; }
        public UserRole Role { get; set; }
        public string FullName { get; set; }
    }
}