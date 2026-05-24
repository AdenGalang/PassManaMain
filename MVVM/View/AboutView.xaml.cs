using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using PassManaAlpha.MVVM.ViewModel;

namespace PassManaAlpha.MVVM.View
{
    public partial class AboutView : UserControl
    {
        private CancellationTokenSource? _terminalCts;
        private readonly SolidColorBrush _successColor = new SolidColorBrush(Color.FromRgb(140, 185, 170));
        private readonly SolidColorBrush _systemColor = new SolidColorBrush(Color.FromRgb(180, 155, 210));
        private readonly Random _rand = new Random();

        public AboutView()
        {
            InitializeComponent();
        }

        private async void AboutView_Loaded(object sender, RoutedEventArgs e)
        {
            _terminalCts?.Cancel();
            _terminalCts = new CancellationTokenSource();

            var token = _terminalCts.Token;

            TerminalLogContainer.Children.Clear();

            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            double ramUsage = currentProcess.WorkingSet64 / 1024.0 / 1024.0;
            int cpuThreads = Environment.ProcessorCount;

            string vaultName = "None";
            bool isUnlocked = false;
            int credentialCount = 0;
            bool hasMasterKey = false;

            if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            {
                var selectedVault = mainVm.HomeVM?.VaultManager?.SelectedVault;
                if (selectedVault != null)
                {
                    vaultName = selectedVault.Name;
                    isUnlocked = selectedVault.IsUnlocked;
                    hasMasterKey = !string.IsNullOrWhiteSpace(selectedVault.MasterKey);
                }
                credentialCount = mainVm.PasswordVM?.Entries?.Count ?? 0;
            }
  
            var liveDiagnosticLogs = new List<string>
            {
               "INIT: Launching PassMana Core Security Deck...",
               $"HOST: System utilizing {cpuThreads} logical CPU processor cores.",
               $"SYSTEM: Current application memory workspace: {ramUsage:F2} MB",
               $"VAULT: Active target database mapping -> [{vaultName}.dat]"
            };
            if (vaultName == "None")
            {
                liveDiagnosticLogs.Add("WARN: No database file context selected yet.");
                liveDiagnosticLogs.Add("[OK] Core operational loop idle. Awaiting user workspace interaction...");
            }
            else
            {
                if (hasMasterKey)
                {
                    liveDiagnosticLogs.Add("[PASS] Crypto key context detected in memory stream.");
                }
                else
                {
                    liveDiagnosticLogs.Add("WARN: Target database mapped, but master verification key is missing.");
                }

                if (isUnlocked)
                {
                    liveDiagnosticLogs.Add($"[OK] HakoHelper: Master key validated via Argon2id + AES-256.");
                    liveDiagnosticLogs.Add($"[PASS] Vault decrypted successfully. {credentialCount} credential entries currently cached in memory.");
                }
                else
                {
                    liveDiagnosticLogs.Add("STATUS: Vault is securely locked. Core structural arrays remain isolated.");
                }
            }

            try
            {
                foreach (var log in liveDiagnosticLogs)
                {
                    if (token.IsCancellationRequested) break;
                    AppendLogToUI(log);
                    TerminalScroll.ScrollToEnd();
                    await Task.Delay(_rand.Next(60, 150), token);
                }
            }
            catch (TaskCanceledException) { }
        }

        private void AboutView_Unloaded(object sender, RoutedEventArgs e)
        {
            _terminalCts?.Cancel();
        }

        private void AppendLogToUI(string log)
        {
            var lineBlock = new TextBlock
            {
                Text = log,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9.5,
                Margin = new Thickness(0, 0, 0, 3),
                TextWrapping = TextWrapping.Wrap
            };

            if (log.StartsWith("[OK]") || log.StartsWith("[PASS]"))
            {
                lineBlock.Foreground = _successColor;
                lineBlock.FontWeight = FontWeights.Bold;
            }
            else
            {
                lineBlock.Foreground = _systemColor;
            }

            TerminalLogContainer.Children.Add(lineBlock);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}