using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PassManaAlpha.Core
{
    public class ForkObject : INotifyPropertyChanged

    {
        public event PropertyChangedEventHandler? PropertyChanged; //yes
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
