using PassManaAlpha.MVVM.Model;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace PassManaAlpha.Core
{
    /// <summary>
    /// Owns the list of known vaults and tracks which one is active.
    /// Injected into HomeViewModel (display) and PasswordViewModel (I/O).
    /// </summary>
    public class VaultManagerViewModel : ForkObject
    {
        private readonly AppConfig _config;

        public ObservableCollection<VaultInfo> Vaults { get; } = new();

        // ── Active / selected vault ──────────────────────────────────────────
        private VaultInfo? _selectedVault;
        public VaultInfo? SelectedVault
        {
            get => _selectedVault;
            set
            {
                if (_selectedVault == value) return;

                // Clear previous selection highlight
                if (_selectedVault != null)
                    _selectedVault.IsSelected = false;

                _selectedVault = value;

                if (_selectedVault != null)
                    _selectedVault.IsSelected = true;

                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(ActiveVaultPath));
                OnPropertyChanged(nameof(ActiveMasterKey));

                // Persist last active vault path
                _config.LastActiveVault = _selectedVault?.FilePath;
                _config.Save();
            }
        }

        public bool HasSelection => _selectedVault != null;
        public string? ActiveVaultPath => _selectedVault?.FilePath;
        public string? ActiveMasterKey => _selectedVault?.MasterKey;

        // ── Inline key input shown when a vault is selected ──────────────────
        private string _inlineKey = string.Empty;
        public string InlineKey
        {
            get => _inlineKey;
            set { _inlineKey = value; OnPropertyChanged(); }
        }

        // Called by the view's PasswordBox.PasswordChanged (can't bind directly)
        public void SetInlineKey(string key)
        {
            InlineKey = key;
            if (_selectedVault != null)
                _selectedVault.MasterKey = key;
            OnPropertyChanged(nameof(ActiveMasterKey));
        }

        // ── Commands ─────────────────────────────────────────────────────────
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

        // ── Constructor ──────────────────────────────────────────────────────
        public VaultManagerViewModel()
        {
            _config = AppConfig.Load();
            LoadKnownVaults();
        }

        // ── Private helpers ──────────────────────────────────────────────────
        private void LoadKnownVaults()
        {
            foreach (var path in _config.KnownVaults)
            {
                if (!File.Exists(path)) continue; // skip deleted files silently
                var vi = new VaultInfo
                {
                    Name = Path.GetFileNameWithoutExtension(path),
                    FilePath = path
                };
                Vaults.Add(vi);

                // Re-select the last active vault (locked — key is never persisted)
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

            // Create empty file if it doesn't exist yet
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
            // Don't duplicate
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

        private void RemoveVault(VaultInfo v)
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
