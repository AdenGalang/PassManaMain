using System.Windows;
using System.Windows.Controls;

namespace PassManaAlpha.MVVM.View
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void OpenExeFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = System.AppContext.BaseDirectory,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch { }
        }

        private void OpenBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var backup = System.IO.Path.Combine(System.AppContext.BaseDirectory, "backups");
                if (!System.IO.Directory.Exists(backup))
                    System.IO.Directory.CreateDirectory(backup);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = backup,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch { }
        }
    }
}
