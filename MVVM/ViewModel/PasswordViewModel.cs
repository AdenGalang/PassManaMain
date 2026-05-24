using Microsoft.VisualBasic.FileIO;
using PassManaAlpha.Core;
using PassManaAlpha.Core.Scurity;
using PassManaAlpha.MVVM.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace PassManaAlpha.MVVM.ViewModel
{
    public class PasswordViewModel : ForkObject
    {
        private readonly VaultManagerViewModel _vaultManager;
        private string? VaultPath => _vaultManager.ActiveVaultPath;
        internal string MasterKey => _vaultManager.ActiveMasterKey ?? string.Empty;
        public string? InputTitle { get; set; }
        public string? InputUsername { get; set; }
        public string? InputPassword { get; set; }
        public Action? OnVaultLoaded { get; set; }

        private string? _consoleLog;
        public string? ConsoleLog
        {
            get => _consoleLog;
            set { _consoleLog = value; OnPropertyChanged(nameof(ConsoleLog)); }
        }

        public void Log(string message) =>
            ConsoleLog += $"[{DateTime.Now:HH:mm:ss}] {message}\n";

        private bool _isLoadEnabled = true;
        public bool IsLoadEnabled
        {
            get => _isLoadEnabled;
            set { _isLoadEnabled = value; OnPropertyChanged(nameof(IsLoadEnabled)); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ObservableCollection<PasswordEntry> Entries { get; set; }
        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged();
                FilteredEntries.Refresh();
            }
        }

        public ICollectionView FilteredEntries { get; }
        public PasswordViewModel(VaultManagerViewModel vaultManager)
        {
            _vaultManager = vaultManager;
            Entries = new ObservableCollection<PasswordEntry>();
            FilteredEntries = CollectionViewSource.GetDefaultView(Entries);
            FilteredEntries.Filter = o =>
                o is PasswordEntry e &&
                (string.IsNullOrWhiteSpace(SearchQuery) ||
                 (e.Title?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                 (e.Username?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false));

            _vaultManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(VaultManagerViewModel.SelectedVault))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Entries.Clear();
                        IsLoadEnabled = true;
                        Log($"Vault changed to: {_vaultManager.SelectedVault?.Name ?? "(none)"}");
                    });
                }
            };
        }

        public ICommand ReloadCommand => new RelayCommand(o =>
        {
            Entries.Clear();
            IsLoadEnabled = true;
            Log("Vault entries cleared.");
        });

        public ICommand ClearConsoleCommand => new RelayCommand(o =>
            ConsoleLog = string.Empty);

        public ICommand SaveCommand => new RelayCommand(o =>
        {
            if (!CheckVaultReady()) return;

            BackupVault();

            if (string.IsNullOrWhiteSpace(InputTitle) ||
                string.IsNullOrWhiteSpace(InputUsername) ||
                string.IsNullOrWhiteSpace(InputPassword))
            {
                Log("Please fill in all fields before saving.");
                return;
            }

            var entry = new PasswordEntry
            {
                Title = InputTitle,
                Username = InputUsername,
                Password = InputPassword
            };

            string json = JsonSerializer.Serialize(entry);
            string encrypted;

            try { encrypted = HakoHelper.Encrypt(json, MasterKey); }
            catch (Exception ex) { Log($"Encryption failed: {ex.Message}"); return; }

            try
            {
                File.AppendAllText(VaultPath!, encrypted + Environment.NewLine);
                Entries.Add(entry);
                InputTitle = InputUsername = InputPassword = string.Empty;
                OnPropertyChanged(nameof(InputTitle));
                OnPropertyChanged(nameof(InputUsername));
                OnPropertyChanged(nameof(InputPassword));
                Log($"Entry saved to \"{_vaultManager.SelectedVault?.Name}\".");
            }
            catch (Exception ex) { Log($"Failed to save entry: {ex.Message}"); }
        });

        public ICommand ImportCsvCommand => new RelayCommand(async o =>
        {
            if (!CheckVaultReady()) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Firefox CSV",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = ".csv"
            };

            if (dialog.ShowDialog() != true) return;

            IsLoading = true;

            try
            {
                var (entries, imported, skipped) = await Task.Run(() =>
                {
                    int i = 0, s = 0;
                    var list = new List<(string title, string username, string password)>();

                    var lines = File.ReadAllLines(dialog.FileName);
                    if (lines.Length < 2) return (list, i, s);

                    using var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(dialog.FileName);
                    parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                    parser.SetDelimiters(",");
                    parser.HasFieldsEnclosedInQuotes = true;
                    parser.ReadLine();

                    while (!parser.EndOfData)
                    {
                        var cols = parser.ReadFields();
                        if (cols == null || cols.Length < 3) { s++; continue; }

                        string url = cols[0];
                        string username = cols[1];
                        string password = cols[2];

                        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                        { s++; continue; }

                        string title = url;
                        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                            title = uri.Host;

                        list.Add((title, username, password));
                        i++;
                    }

                    return (list, i, s);
                });

                string vaultPath = VaultPath!;
                string masterKey = MasterKey;

                foreach (var (title, username, password) in entries)
                {
                    var entry = new PasswordEntry { Title = title, Username = username, Password = password };
                    string json = JsonSerializer.Serialize(entry);
                    string encrypted = HakoHelper.Encrypt(json, masterKey);
                    File.AppendAllText(vaultPath, encrypted + Environment.NewLine);
                    Entries.Add(entry);
                }

                Log($"Imported {imported} entr{(imported == 1 ? "y" : "ies")}." +
                    (skipped > 0 ? $" {skipped} skipped (empty/malformed)." : ""));
            }
            catch (Exception ex)
            {
                Log($"Import failed: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        });

        public ICommand LoadCommand => new Core.AsyncRelayCommand(async o =>
        {
            if (!CheckVaultReady()) return;

            string vaultPath = VaultPath!;
            string masterKey = MasterKey;

            if (!File.Exists(vaultPath))
            {
                Log("Vault file not found.");
                return;
            }

            IsLoading = true;
            IsLoadEnabled = false;
            try
            {
                var result = await Task.Run(() =>
                {
                    var lines = File.ReadAllLines(vaultPath);
                    var loadedList = new List<PasswordEntry>();
                    int loaded = 0;
                    int skipped = 0;

                    if (lines.Length == 0)
                        return (loadedList, loaded, skipped, empty: true);

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        try
                        {
                            string? decrypted = HakoHelper.Decrypt(line, masterKey);
                            if (decrypted == null) { skipped++; continue; }

                            var entry = JsonSerializer.Deserialize<PasswordEntry>(decrypted);
                            if (entry != null) { loadedList.Add(entry); loaded++; }
                        }
                        catch { skipped++; }
                    }

                    return (loadedList, loaded, skipped, empty: false);
                });

                if (result.empty)
                {
                    Log("Vault is empty.");
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Entries.Clear();
                    foreach (var e in result.loadedList) Entries.Add(e);
                });

                Log($"Loaded {result.loaded} entr{(result.loaded == 1 ? "y" : "ies")} from \"{_vaultManager.SelectedVault?.Name}\"." +
                    (result.skipped > 0 ? $" {result.skipped} entr{(result.skipped == 1 ? "y belongs" : "ies belong")} to a different key." : ""));
            }
            finally
            {
                IsLoading = false;
                IsLoadEnabled = true;
                var config = AppConfig.Load();
                config.LastAccessed = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
                config.Save();
                OnVaultLoaded?.Invoke();
            }
        });

        private static readonly string BackupFolder = "backups";
        internal void BackupVault()
        {
            if (VaultPath == null || !File.Exists(VaultPath)) return;
            Directory.CreateDirectory(BackupFolder);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string vaultName = Path.GetFileNameWithoutExtension(VaultPath);
            string backupPath = Path.Combine(BackupFolder, $"{vaultName}_{timestamp}.dat");
            File.Copy(VaultPath, backupPath);

            var dirInfo = new DirectoryInfo(BackupFolder);
            var existingBackups = dirInfo.GetFiles($"{vaultName}_*.dat")
                                         .OrderBy(f => f.CreationTime) 
                                         .ToList();

            while (existingBackups.Count > 10)
            {
                var oldestFile = existingBackups[0];
                try
                {
                    oldestFile.Delete();
                    existingBackups.RemoveAt(0);
                }
                catch (IOException ex)
                {
                    Log($"Backup cleanup error: {ex.Message}");
                    break;
                }
            }
        }

        private bool CheckVaultReady()
        {
            if (VaultPath == null)
            {
                Log("No vault selected. Go to Home and select or create a vault first.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(MasterKey))
            {
                Log("No key set for this vault. Go to Home, select the vault and enter its key.");
                return false;
            }
            return true;
        }
    }
}
