using PassManaAlpha.Core;
using PassManaAlpha.MVVM.Model;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace PassManaAlpha.MVVM.View
{
    public partial class UserHomeView : UserControl
    {
        public UserHomeView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        // ── Maid easter egg ──────────────────────────────────────────────────
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            kazusa.BeginAnimation(UIElement.OpacityProperty, null);
            kazusa.Opacity = 1;
            Storyboard sb = (Storyboard)FindResource("FadeOutStoryboard");
            sb.Stop();
            kazusa.Visibility = Visibility.Visible;
            kazusa.Opacity = 1;
            sb.Begin();
        }

        // ── Vault card selection ─────────────────────────────────────────────
        private void VaultCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is VaultInfo vault)
            {
                VaultManager?.SelectVaultCommand.Execute(vault);

                // Clear the key box so it matches the newly selected vault
                VaultKeyBox.Clear();
            }
        }

        // ── Inline key input ─────────────────────────────────────────────────
        private void VaultKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Live-update the in-memory key on the VaultInfo as the user types
            VaultManager?.SetInlineKey(VaultKeyBox.Password);
        }

        private void ConfirmKey_Click(object sender, RoutedEventArgs e)
        {
            var vm = VaultManager;
            if (vm?.SelectedVault == null) return;

            vm.SelectedVault.MasterKey = VaultKeyBox.Password;
            vm.SelectedVault.IsUnlocked = !string.IsNullOrWhiteSpace(VaultKeyBox.Password);
        }

        // ── DataContext helper ───────────────────────────────────────────────
        private VaultManagerViewModel? VaultManager =>
            (DataContext as PassManaAlpha.MVVM.ViewModel.HomeViewModel)?.VaultManager;

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Clear key box whenever the whole DataContext is swapped (navigation)
            VaultKeyBox?.Clear();
        }
    }
}
