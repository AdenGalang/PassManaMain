using PassManaAlpha.Core;

namespace PassManaAlpha.MVVM.ViewModel
{
    public class AboutViewModel : ForkObject
    {
        public string AppName => "PassMana";
        public string Version => "v1.0.1 Final Build";
        public string Description => "A not very lightweight encrypted password manager built with WPF and .NET 8.";
        public string Author => "Minamorin_SmiCondctr5200";
        public string WaifuImage => "/assets/kazusa_r.jpg";
        public string WaifuName => "Kazusa Kyouyama";
        public string WaifuSeries => "杏山カズサ";
        public List<string> TerminalLogs { get; } = new List<string>();
        public bool IsBootstrapped { get; set; } = false;
        
    }
}