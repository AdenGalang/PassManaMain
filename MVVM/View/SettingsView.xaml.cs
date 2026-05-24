using PassManaAlpha.Core;
using PassManaAlpha.MVVM.ViewModel;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PassManaAlpha.MVVM.View
{
    public partial class SettingsView : UserControl
    {
        private CancellationTokenSource? _animationCts;
        private readonly SolidColorBrush _decryptedColor = new SolidColorBrush(Color.FromRgb(240, 240, 240));    //shiro                                                                                             // A washed-out, cool mint/gray slate that feels completely analytical                                                                                                                                                                                                  // A soft, low-saturation cosmic lilac that looks like encrypted energy
        private readonly SolidColorBrush _symbolColor = new SolidColorBrush(Color.FromRgb(180, 155, 210));       //whatever this is

        public SettingsView()
        {
            InitializeComponent();
            DataContextChanged += SettingsView_DataContextChanged;
        }

        private void SettingsView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is SettingsViewModel vm)
            {
                vm.TriggerNotificationAnimation = async (message) =>
                {
                    _animationCts?.Cancel();
                    _animationCts = new CancellationTokenSource();
                    var token = _animationCts.Token;

                    try
                    {
                        NotificationContainer.BeginAnimation(OpacityProperty, null);
                        MaskStop1.BeginAnimation(GradientStop.OffsetProperty, null);
                        MaskStop2.BeginAnimation(GradientStop.OffsetProperty, null);
                        MaskStop1.Offset = 0;
                        MaskStop2.Offset = 0;
                        NotificationText.Inlines.Clear();
                        for (int i = 0; i < message.Length; i++)
                        {
                            NotificationText.Inlines.Add(new Run(" ") { FontWeight = FontWeights.Bold });
                        }

                        var expoEase = new ExponentialEase { Exponent = 3, EasingMode = EasingMode.EaseOut };
                        TimeSpan animDuration = TimeSpan.FromMilliseconds(message.Length * 50); // base duration scales w/ message length

                        var fadeInAnim = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200)); // fade in
                        NotificationContainer.BeginAnimation(OpacityProperty, fadeInAnim);

                        var maskAnim1 = new DoubleAnimation(0.0, 1.0, animDuration) { EasingFunction = expoEase };
                        var maskAnim2 = new DoubleAnimation(0.0, 1.1, animDuration) { EasingFunction = expoEase };
                        MaskStop1.BeginAnimation(GradientStop.OffsetProperty, maskAnim1);
                        MaskStop2.BeginAnimation(GradientStop.OffsetProperty, maskAnim2);

                        await DecryptTextHelper.AnimateTextComplexAsync(message, (index, character, isDecrypted) =>
                        {
                            if (token.IsCancellationRequested || index >= NotificationText.Inlines.Count) return;

                            if (NotificationText.Inlines.ElementAtOrDefault(index) is Run targetRun)
                            {
                                targetRun.Text = character.ToString();

                                if (isDecrypted)
                                {
                                    targetRun.Foreground = _decryptedColor;
                                    targetRun.FontWeight = FontWeights.Light;
                                }
                                else
                                {
                                    double timeFactor = DateTime.Now.Ticks / 5000000.0;
                                    byte r = (byte)(145 + Math.Sin(timeFactor + index) * 25);
                                    byte g = (byte)(160 + Math.Cos(timeFactor + index) * 20);
                                    byte b = (byte)(190 + Math.Sin(timeFactor + index + 1) * 15);
                                    targetRun.Foreground = new SolidColorBrush(Color.FromRgb(r, g, b));
                                    targetRun.FontWeight = FontWeights.Bold;
                                }
                            }
                        }, token);

                        await Task.Delay(500, token); // final pause // ms
                        var fadeOutAnim = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(200)); // fade out
                        NotificationContainer.BeginAnimation(OpacityProperty, fadeOutAnim);
                    }
                    catch (TaskCanceledException) { }
                };
            }
        }
        private Color HslToRgb(double h, double s, double l) // h: 0-360, s: 0-1, l: 0-1 // optional
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = l - c / 2;
            double r = 0, g = 0, b = 0;

            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }

            return Color.FromRgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
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
    
