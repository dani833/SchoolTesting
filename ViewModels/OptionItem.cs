using SchoolTesting.ViewModels.Base;

namespace SchoolTesting.ViewModels
{
    public class OptionItem : ViewModelBase
    {
        private string text;
        public string Text
        {
            get => text;
            set => Set(ref text, value);
        }

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set => Set(ref isSelected, value);
        }
    }
}