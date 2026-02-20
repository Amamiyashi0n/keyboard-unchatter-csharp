using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace keyboard_unchatter_csharp
{
    public partial class ToastWindow : Window
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private bool _closing;

        public ToastWindow(string title, string message)
        {
            InitializeComponent();
            AppNameText.Text = GetAppName();
            TitleText.Text = title ?? "";
            MessageText.Text = message ?? "";

            Loaded += OnLoaded;
            MouseLeftButtonUp += (s, e) => Close();

            _timer.Interval = TimeSpan.FromMilliseconds(2200);
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                Close();
            };
        }

        public static void ShowToast(string title, string message)
        {
            try
            {
                var toast = new ToastWindow(title, message);
                toast.Show();
            }
            catch
            {
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            PositionToBottomRight();
            PlayInAnimation();
            _timer.Start();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_closing)
            {
                base.OnClosing(e);
                return;
            }
            e.Cancel = true;
            _closing = true;
            _timer.Stop();
            PlayOutAnimation();
        }

        private void PositionToBottomRight()
        {
            try
            {
                var work = SystemParameters.WorkArea;
                Left = work.Right - Width - 16;
                Top = work.Bottom - Height - 16;
            }
            catch
            {
            }
        }

        private void PlayInAnimation()
        {
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));
            BeginAnimation(OpacityProperty, fade);

            var slide = new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(220));
            SlideTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slide);
        }

        private void PlayOutAnimation()
        {
            var fade = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(160));
            fade.Completed += (s, e) =>
            {
                try
                {
                    Close();
                }
                catch
                {
                }
            };
            BeginAnimation(OpacityProperty, fade);

            var slide = new DoubleAnimation(0, 12, TimeSpan.FromMilliseconds(160));
            SlideTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slide);
        }

        private string GetAppName()
        {
            try
            {
                if (Application.Current != null && Application.Current.MainWindow != null && !string.IsNullOrEmpty(Application.Current.MainWindow.Title))
                {
                    return Application.Current.MainWindow.Title;
                }
            }
            catch
            {
            }

            try
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            }
            catch
            {
            }

            return "应用";
        }
    }
}

