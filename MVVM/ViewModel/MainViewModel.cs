using PassManaAlpha.Core;

namespace PassManaAlpha.MVVM.ViewModel
{
    class MainViewModel : ForkObject
    {
        public RelayCommand HomeViewCommand { get; set; }
        public RelayCommand PasswordViewCommand { get; set; }
        public RelayCommand SettingsViewCommand { get; set; }
        public RelayCommand AboutViewCommand { get; set; }
        public HomeViewModel HomeVM { get; set; }
        public PasswordViewModel PasswordVM { get; set; }
        public SettingsViewModel SettingsVM { get; set; }
        public AboutViewModel AboutVM { get; set; }

        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentViewTypeName));
            }
        }

        public string CurrentViewTypeName => CurrentView?.GetType().Name ?? string.Empty;

        public string WindowTitle =>
            HomeVM?.VaultManager.SelectedVault?.Name is string name
                ? $"{name}"
                : "PassMana";

        public MainViewModel()
        {
            var vaultManager = new VaultManagerViewModel();
            vaultManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(VaultManagerViewModel.SelectedVault))
                    OnPropertyChanged(nameof(WindowTitle));
            };

            PasswordVM = new PasswordViewModel(vaultManager);
            SettingsVM = new SettingsViewModel(PasswordVM, vaultManager);
            HomeVM = new HomeViewModel(PasswordVM, vaultManager);
            AboutVM = new AboutViewModel();

            _currentView = HomeVM;

            HomeViewCommand = new RelayCommand(o => CurrentView = HomeVM);
            PasswordViewCommand = new RelayCommand(o => CurrentView = PasswordVM);
            SettingsViewCommand = new RelayCommand(o => CurrentView = SettingsVM);
            AboutViewCommand = new RelayCommand(o => CurrentView = AboutVM);

            PasswordVM.OnVaultLoaded = () => HomeVM.RefreshLastAccessed();
        }
    }
}