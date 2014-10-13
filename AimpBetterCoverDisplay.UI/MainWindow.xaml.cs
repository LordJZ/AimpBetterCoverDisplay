using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
    }
}
