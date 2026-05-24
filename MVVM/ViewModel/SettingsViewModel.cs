using PassManaAlpha.Core;
using System.IO;
using System.Windows.Input;

namespace PassManaAlpha.MVVM.ViewModel
{
    public class SettingsViewModel : ForkObject
    {
        private readonly PasswordViewModel _passwordVM;
        private readonly VaultManagerViewModel _vaultManager;
        private string? _lastBackedUpPath;
        private DateTime? _lastBackedUpFileTime;
        private string _notificationMessage = string.Empty;

        public string NotificationMessage
        {
            get => _notificationMessage;
            set { _notificationMessage = value; OnPropertyChanged(); }
        }

        public Action<string>? TriggerNotificationAnimation { get; set; }
        public ICommand ManualBackupCommand { get; }
        public ICommand DeleteActiveVaultCommand { get; }
        public SettingsViewModel(PasswordViewModel passwordVM, VaultManagerViewModel vaultManager)
        {
            _passwordVM = passwordVM;
            _vaultManager = vaultManager;

            ManualBackupCommand = new RelayCommand(o => ExecuteManualBackup());
            DeleteActiveVaultCommand = new RelayCommand(o => ExecuteRecycleVault());
        }

        private void ShowAnimatedFeedback(string message)
        {
            
            TriggerNotificationAnimation?.Invoke(message);
        }

        private void ExecuteManualBackup()
        {
            string? currentVaultPath = _vaultManager.ActiveVaultPath;
            if (string.IsNullOrEmpty(currentVaultPath) || !File.Exists(currentVaultPath))
            {
                _passwordVM.Log("Backup failed: No active vault file found.");
                ShowAnimatedFeedback("No Active Vault Found");
                return;
            }

            try
            {
                DateTime currentFileTime = File.GetLastWriteTime(currentVaultPath);

                if (currentVaultPath == _lastBackedUpPath && currentFileTime == _lastBackedUpFileTime)
                {
                    _passwordVM.Log("Backup skipped: Vault file has not changed since the last backup.");
                    ShowAnimatedFeedback("Already Up To Date");
                    return;
                }

                _passwordVM.BackupVault();

                _lastBackedUpPath = currentVaultPath;
                _lastBackedUpFileTime = currentFileTime;

                _passwordVM.Log("Backup successfully completed.");
                ShowAnimatedFeedback("Backup Created");
            }
            catch (Exception ex)
            {
                _passwordVM.Log($"Backup validation error: {ex.Message}");
                ShowAnimatedFeedback("Backup Failed");
            }
        }

        private void ExecuteRecycleVault()
        {
            var targetVault = _vaultManager.SelectedVault;
            string? filePath = _vaultManager.ActiveVaultPath;

            if (targetVault == null || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _passwordVM.Log("Delete failed: No active vault target loaded.");
                ShowAnimatedFeedback("No Vault Selected");
                return;
            }

            try
            {
                string vanishedVaultName = targetVault.Name;
                _passwordVM.Entries.Clear();
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    filePath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin
                );

                _passwordVM.Log($"Vault '{vanishedVaultName}' was successfully moved to the Recycle Bin.");
                ShowAnimatedFeedback($"Deleted {vanishedVaultName}");
                _vaultManager.SelectedVault = null;
                _vaultManager.RemoveVault(targetVault);
            }
            catch (Exception ex)
            {
                _passwordVM.Log($"Delete operational error: {ex.Message}");
                ShowAnimatedFeedback("Deletion Failed");
            }
        }
    }
}