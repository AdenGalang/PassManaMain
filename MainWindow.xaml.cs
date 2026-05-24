using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using PassManaAlpha.MVVM.ViewModel;

namespace PassManaAlpha
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _startupTimer;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            GlobalBgVideo.MediaEnded += GlobalBgVideo_MediaEnded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _startupTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _startupTimer.Tick += StartupTimer_Tick;
            _startupTimer.Start();

            if (DataContext is MainViewModel mainVm)
            {
                mainVm.PropertyChanged += MainVm_PropertyChanged;
            }
        }

        private void StartupTimer_Tick(object? sender, EventArgs e)
        {
            _startupTimer?.Stop();

            GlobalBgVideo.Play();

            if (DataContext is MainViewModel mainVm &&
                mainVm.CurrentViewTypeName != "AboutViewModel" &&
                mainVm.CurrentViewTypeName != "SettingsViewModel")
            {
                GlobalBgVideo.Pause();
            }
            else
            {
                GlobalBgVideo.Visibility = Visibility.Visible;
            }
        }

        private void MainVm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentView))
            {
                if (DataContext is MainViewModel mainVm)
                {
                    if (mainVm.CurrentViewTypeName == "AboutViewModel" || mainVm.CurrentViewTypeName == "SettingsViewModel")
                    {
                        GlobalBgVideo.Visibility = Visibility.Visible;
                        GlobalBgVideo.Play();
                    }
                    else
                    {
                        GlobalBgVideo.Pause();
                        GlobalBgVideo.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void GlobalBgVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            GlobalBgVideo.Stop();
            GlobalBgVideo.Position = TimeSpan.Zero;
            GlobalBgVideo.Play();
        }
    }
}