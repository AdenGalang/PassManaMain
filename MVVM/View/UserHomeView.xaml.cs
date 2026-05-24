using PassManaAlpha.Core;
using PassManaAlpha.MVVM.Model;
using PassManaAlpha.MVVM.ViewModel;
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            kazusa.BeginAnimation(UIElement.OpacityProperty, null);
            kazusa.Opacity = 0.5;
            if (FindResource("FadeOutStoryboard") is Storyboard sb)
            {
                sb.Stop();
                kazusa.Visibility = Visibility.Visible;
                sb.Begin();
            }

            var vm = VaultManager;
            if (vm != null)
            {
                vm.LockActiveVault();
                VaultKeyBox?.Clear(); 

                if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
                {
                    mainVm.PasswordVM?.Entries?.Clear();
                    mainVm.PasswordVM?.Log("Flush initiated. Memory buffers cleared.");
                }
            }
        }

        private void VaultCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is VaultInfo vault)
            {
                VaultManager?.SelectVaultCommand.Execute(vault);
                VaultKeyBox.Clear();
            }
        }

        private void VaultKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            VaultManager?.SetInlineKey(VaultKeyBox.Password);
        }

        private void ConfirmKey_Click(object sender, RoutedEventArgs e)
        {
            var vm = VaultManager;
            if (vm?.SelectedVault == null) return;

            vm.SelectedVault.MasterKey = VaultKeyBox.Password;
            vm.SelectedVault.IsUnlocked = !string.IsNullOrWhiteSpace(VaultKeyBox.Password);
        }

        private VaultManagerViewModel? VaultManager =>
            (DataContext as PassManaAlpha.MVVM.ViewModel.HomeViewModel)?.VaultManager;

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            VaultKeyBox?.Clear();
        }
    }
}
