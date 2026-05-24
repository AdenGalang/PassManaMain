using PassManaAlpha.Core;
using System.Windows;
using System.Windows.Input;

namespace PassManaAlpha.MVVM.ViewModel
{
    public class SettingsViewModel : ForkObject
    {
        // Master key is now per-vault (stored on VaultInfo, not here).
        // Settings page keeps general app preferences.

        public SettingsViewModel() { }
    }
}
