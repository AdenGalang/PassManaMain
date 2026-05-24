using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PassManaAlpha.MVVM.Model
{
    public class VaultInfo : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        private string _masterKey = string.Empty;

        public string MasterKey
        {
            get => _masterKey;
            set { _masterKey = value; OnPropertyChanged(); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private bool _isUnlocked;
        public bool IsUnlocked
        {
            get => _isUnlocked;
            set { _isUnlocked = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusLabel)); }
        }

        public string StatusLabel => _isUnlocked ? "🔓 Unlocked" : "🔒 Locked";
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
