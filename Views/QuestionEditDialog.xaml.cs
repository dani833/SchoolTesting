using System.Windows;
using SchoolTesting.Models;
using SchoolTesting.ViewModels;

namespace SchoolTesting.Views
{
    public partial class QuestionEditDialog : Window
    {
        public Question ResultQuestion { get; private set; }

        public QuestionEditDialog(Question question = null)
        {
            InitializeComponent();
            var vm = new QuestionEditViewModel(question);
            DataContext = vm;
            vm.CloseRequested += (ok, q) =>
            {
                ResultQuestion = q;
                DialogResult = ok;
                Close();
            };
        }
    }
}