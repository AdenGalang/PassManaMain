using PassManaAlpha.MVVM.Model;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace PassManaAlpha.Core
{
    public class VaultManagerViewModel : ForkObject
    {
        private readonly AppConfig _config;
        public ObservableCollection<VaultInfo> Vaults { get; } = new();
        private VaultInfo? _selectedVault;
        public VaultInfo? SelectedVault
        {
            get => _selectedVault;
            set
            {
                if (_selectedVault == value) return;

                if (_selectedVault != null)
                    _selectedVault.IsSelected = false;

                _selectedVault = value;

                if (_selectedVault != null)
                    _selectedVault.IsSelected = true;

                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(ActiveVaultPath));
                OnPropertyChanged(nameof(ActiveMasterKey));

                _config.LastActiveVault = _selectedVault?.FilePath;
                _config.Save();
            }
        }

        public bool HasSelection => _selectedVault != null;
        public string? ActiveVaultPath => _selectedVault?.FilePath;
        public string? ActiveMasterKey => _selectedVault?.MasterKey;
        private string _inlineKey = string.Empty;
        public string InlineKey
        {
            get => _inlineKey;
            set { _inlineKey = value; OnPropertyChanged(); }
        }

        public void SetInlineKey(string key)
        {
            InlineKey = key;
            if (_selectedVault != null)
                _selectedVault.MasterKey = key;
            OnPropertyChanged(nameof(ActiveMasterKey));
        }

        public ICommand NewVaultCommand => new RelayCommand(o => CreateNewVault());
        public ICommand OpenVaultCommand => new RelayCommand(o => OpenExistingVault());
        public ICommand SelectVaultCommand => new RelayCommand(o =>
        {
            if (o is VaultInfo v)
                SelectedVault = v;
        });
        public ICommand RemoveVaultCommand => new RelayCommand(o =>
        {
            if (o is VaultInfo v)
                RemoveVault(v);
        });

        public VaultManagerViewModel()
        {
            _config = AppConfig.Load();
            LoadKnownVaults();
        }
        public void LockActiveVault()
        {
            if (SelectedVault != null)
            {
                SelectedVault.MasterKey = string.Empty;
                SelectedVault.IsUnlocked = false;
                InlineKey = string.Empty;
            }
        }

        private void LoadKnownVaults()
        {
            foreach (var path in _config.KnownVaults)
            {
                if (!File.Exists(path)) continue; 
                var vi = new VaultInfo
                {
                    Name = Path.GetFileNameWithoutExtension(path),
                    FilePath = path
                };
                Vaults.Add(vi);

                if (path == _config.LastActiveVault)
                    _selectedVault = vi;
            }

            if (_selectedVault != null)
            {
                _selectedVault.IsSelected = true;
                OnPropertyChanged(nameof(SelectedVault));
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(ActiveVaultPath));
            }
        }

        private void CreateNewVault()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Create New Vault",
                Filter = "Vault files (*.dat)|*.dat|All files (*.*)|*.*",
                DefaultExt = ".dat",
                FileName = "vault"
            };

            if (dialog.ShowDialog() != true) return;

            string path = dialog.FileName;

            if (!File.Exists(path))
                File.WriteAllText(path, string.Empty);

            AddOrSelectVault(path);
        }

        private void OpenExistingVault()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Open Vault File",
                Filter = "Vault files (*.dat)|*.dat|All files (*.*)|*.*",
                DefaultExt = ".dat"
            };

            if (dialog.ShowDialog() != true) return;

            AddOrSelectVault(dialog.FileName);
        }

        private void AddOrSelectVault(string path)
        {
            var existing = FindByPath(path);
            if (existing != null)
            {
                SelectedVault = existing;
                return;
            }

            var vi = new VaultInfo
            {
                Name = Path.GetFileNameWithoutExtension(path),
                FilePath = path
            };
            Vaults.Add(vi);

            if (!_config.KnownVaults.Contains(path))
            {
                _config.KnownVaults.Add(path);
                _config.Save();
            }

            SelectedVault = vi;
        }

        internal void RemoveVault(VaultInfo v)
        {
            Vaults.Remove(v);
            _config.KnownVaults.Remove(v.FilePath);
            _config.Save();

            if (SelectedVault == v)
                SelectedVault = Vaults.Count > 0 ? Vaults[0] : null;
        }

        private VaultInfo? FindByPath(string path) =>
            Vaults.FirstOrDefault(v => string.Equals(v.FilePath, path, StringComparison.OrdinalIgnoreCase));
    }
}
