using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using LordJZ.WinAPI;

namespace AimpBetterCoverDisplay.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            WindowPlacement placement = Config.Instance.Placement;
            if (placement != null)
            {
                NativeWindow window = this.NativeWindow;
                window.Placement = placement;

                // adjust again after dpi settings have been applied
                Dispatcher.InvokeAsync(() => window.Placement = placement);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.LeftButton == MouseButtonState.Pressed && !(e.OriginalSource is Thumb))
                DragMove();
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            this.CloseButton.Visibility = Visibility.Visible;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            this.CloseButton.Visibility = Visibility.Collapsed;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            ModifierKeys mods = Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift);
            if (mods == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.Left:
                        AdjustPlacement(left: -1);
                        break;
                    case Key.Up:
                        AdjustPlacement(top: -1);
                        break;
                    case Key.Right:
                        AdjustPlacement(right: -1);
                        break;
                    case Key.Down:
                        AdjustPlacement(bottom: -1);
                        break;
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            this.UpdateConfig();

            Config.Instance.Save();
        }

        void AdjustPlacement(int left = 0, int top = 0, int right = 0, int bottom = 0)
        {
            NativeWindow window = this.NativeWindow;
            WindowPlacement placement = window.Placement;

            NativeRect position = placement.NormalPosition;
            position.Left += left;
            position.Top += top;
            position.Right += right;
            position.Bottom += bottom;
            placement.NormalPosition = position;

            window.Placement = placement;
        }

        void UpdateConfig()
        {
            Config.Instance.Placement = this.NativeWindow.Placement;
        }

        public void SetCover(ImageSource image)
        {
            Image control = this.ImageControl;

            control.Visibility = image == null ? Visibility.Collapsed : Visibility.Visible;
            control.Source = image;

            UpdateStretch();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        void UpdateStretch()
        {
            Image control = this.ImageControl;
            if (control.Visibility != Visibility.Visible)
                return;

            bool fit = FitImage();
            if (fit)
                control.StretchDirection = StretchDirection.Both;
            else
                control.StretchDirection = StretchDirection.DownOnly;
        }

        bool FitImage()
        {
            ImageSource source = this.ImageControl.Source;
            double w = source.Width;
            double h = source.Height;

            Size size = PointToScreenAbsolute(new Size(this.ActualWidth, this.ActualHeight));

            if (w >= size.Width || h >= size.Height)
                return false;

            double ratioW = (size.Width - w) / size.Width;
            double ratioH = (size.Height - w) / size.Height;
            if (Math.Min(ratioW, ratioH) > .4)
                return false;

            return true;
        }

        void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateStretch();
        }
    }
}
