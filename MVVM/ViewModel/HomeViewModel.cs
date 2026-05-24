using PassManaAlpha.Core;
using System;
using System.Windows.Threading;

namespace PassManaAlpha.MVVM.ViewModel
{
    class HomeViewModel : ForkObject
    {
        private readonly PasswordViewModel? _passwordVM;

        public VaultManagerViewModel VaultManager { get; }

        public int VaultCount => _passwordVM?.Entries.Count ?? 0;

        private string _currentTime = string.Empty;
        public string CurrentTime
        {
            get => _currentTime;
            set { _currentTime = value; OnPropertyChanged(); }
        }

        private string _currentDate = string.Empty;
        public string CurrentDate
        {
            get => _currentDate;
            set { _currentDate = value; OnPropertyChanged(); }
        }

        private string _lastAccessed = "Never";
        public string LastAccessed
        {
            get => _lastAccessed;
            set { _lastAccessed = value; OnPropertyChanged(); }
        }

        public void RefreshLastAccessed()
        {
            var config = AppConfig.Load();
            LastAccessed = config.LastAccessed == "Never" ? "Never" : config.LastAccessed;
        }

        public HomeViewModel(PasswordViewModel passwordVM, VaultManagerViewModel vaultManager)
        {
            _passwordVM = passwordVM;
            VaultManager = vaultManager;

            _passwordVM.Entries.CollectionChanged += (s, e) => OnPropertyChanged(nameof(VaultCount));

            var config = AppConfig.Load();
            _lastAccessed = config.LastAccessed == "Never" ? "Never" : config.LastAccessed;

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => UpdateTime();
            timer.Start();
            UpdateTime();
        }

        private void UpdateTime()
        {
            var now = DateTime.Now;
            var jc = new System.Globalization.JapaneseCalendar();
            int year = jc.GetYear(now);
            int era = jc.GetEra(now);
            string eraName = era == 5 ? "令和" : "平成";

            CurrentDate = $"{eraName}{year}年{now.Month}月{now.Day}日";
            CurrentTime = $"{now.Hour}時{now.Minute:D2}分{now.Second:D2}秒";
        }
    }
}
