using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ColdReview.Debugger;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;

namespace ColdReview.Adornments
{
    internal sealed class CallPathHud
    {
        private static bool _dismissed;
        private static double _savedLeft = 12;
        private static double _savedTop = 8;

        private readonly IWpfTextView _view;
        private readonly IAdornmentLayer _layer;
        private readonly Border _root;
        private readonly TextBlock _text;
        private readonly Button _close;
        private bool _attached;
        private bool _dragging;
        private Point _dragStart;
        private double _dragOriginLeft;
        private double _dragOriginTop;

        public CallPathHud(IWpfTextView view)
        {
            _view = view;
            _layer = view.GetAdornmentLayer(CallPathAdornmentTextViewCreationListener.LayerName);

            _text = new TextBlock
            {
                FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New"),
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xFF, 0xFB)),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            _close = new Button
            {
                Content = "✕",
                Width = 20,
                Height = 20,
                Padding = new Thickness(0),
                Margin = new Thickness(4, 0, 0, 0),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xFF, 0xFB)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                ToolTip = "Close",
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            _close.Click += OnCloseClicked;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(_text, 0);
            Grid.SetColumn(_close, 1);
            grid.Children.Add(_text);
            grid.Children.Add(_close);

            _root = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0xEE, 0x12, 0x2A, 0x2E)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0xC4, 0xB6)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 6, 6, 6),
                Child = grid,
                Visibility = Visibility.Collapsed,
                MaxWidth = 860,
                Cursor = Cursors.SizeAll,
                IsHitTestVisible = true
            };

            _root.MouseLeftButtonDown += OnBarMouseDown;
            _root.MouseMove += OnBarMouseMove;
            _root.MouseLeftButtonUp += OnBarMouseUp;
            _root.LostMouseCapture += OnLostCapture;

            _view.Closed += OnClosed;
            _view.ViewportWidthChanged += OnViewportWidthChanged;
            CallStackMonitor.Changed += OnStackChanged;
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                Apply(CallStackMonitor.Current);
            }
            catch (Exception)
            {
            }
        }

        private void OnStackChanged(object sender, CallStackSnapshot snapshot)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Apply(snapshot);
            }).FileAndForget("ColdReview/HudUpdate");
        }

        private void Apply(CallStackSnapshot snapshot)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_layer == null)
            {
                return;
            }

            if (snapshot == null || !snapshot.InBreakMode || snapshot.Frames.Count == 0)
            {
                _dismissed = false;
                Hide();
                return;
            }

            if (_dismissed)
            {
                Hide();
                return;
            }

            var userFrames = snapshot.Frames.Where(f => f.IsUserCode).ToList();
            if (userFrames.Count == 0)
            {
                userFrames = snapshot.Frames.ToList();
            }

            _text.Text = string.Join("  →  ", userFrames.AsEnumerable().Reverse().Select(Highlight));
            _root.Visibility = Visibility.Visible;
            _root.MaxWidth = Math.Max(240, _view.ViewportWidth - 28);
            EnsureAttached();
        }

        private void EnsureAttached()
        {
            if (!_attached)
            {
                _layer.AddAdornment(
                    AdornmentPositioningBehavior.ViewportRelative,
                    null,
                    null,
                    _root,
                    (tag, element) => _attached = false);
                _attached = true;
            }

            Canvas.SetLeft(_root, _savedLeft);
            Canvas.SetTop(_root, _savedTop);
        }

        private void Hide()
        {
            _root.Visibility = Visibility.Collapsed;
            if (_attached)
            {
                _layer.RemoveAdornment(_root);
                _attached = false;
            }
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            _dismissed = true;
            Hide();
            e.Handled = true;
        }

        private void OnBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsCloseSource(e.OriginalSource))
            {
                return;
            }

            _dragging = true;
            _dragStart = e.GetPosition(_view.VisualElement);
            _dragOriginLeft = GetCanvasLeft();
            _dragOriginTop = GetCanvasTop();
            _root.CaptureMouse();
            e.Handled = true;
        }

        private void OnBarMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point now = e.GetPosition(_view.VisualElement);
            double left = Clamp(_dragOriginLeft + now.X - _dragStart.X, 4, Math.Max(4, _view.ViewportWidth - 48));
            double top = Clamp(_dragOriginTop + now.Y - _dragStart.Y, 4, Math.Max(4, _view.ViewportHeight - 32));
            Canvas.SetLeft(_root, left);
            Canvas.SetTop(_root, top);
            _savedLeft = left;
            _savedTop = top;
            e.Handled = true;
        }

        private void OnBarMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_dragging)
            {
                return;
            }

            StopDragging();
            e.Handled = true;
        }

        private void OnLostCapture(object sender, MouseEventArgs e)
        {
            StopDragging();
        }

        private void StopDragging()
        {
            if (!_dragging)
            {
                return;
            }

            _dragging = false;
            if (_root.IsMouseCaptured)
            {
                _root.ReleaseMouseCapture();
            }
        }

        private bool IsCloseSource(object source)
        {
            var current = source as DependencyObject;
            while (current != null)
            {
                if (ReferenceEquals(current, _close))
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private double GetCanvasLeft()
        {
            double value = Canvas.GetLeft(_root);
            return double.IsNaN(value) ? _savedLeft : value;
        }

        private double GetCanvasTop()
        {
            double value = Canvas.GetTop(_root);
            return double.IsNaN(value) ? _savedTop : value;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static string Highlight(CallFrameInfo frame)
        {
            return frame.IsCurrent ? "[" + frame.ShortName + "]" : frame.ShortName;
        }

        private void OnViewportWidthChanged(object sender, EventArgs e)
        {
            _root.MaxWidth = Math.Max(240, _view.ViewportWidth - 28);
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _view.Closed -= OnClosed;
            _view.ViewportWidthChanged -= OnViewportWidthChanged;
            CallStackMonitor.Changed -= OnStackChanged;
            _root.MouseLeftButtonDown -= OnBarMouseDown;
            _root.MouseMove -= OnBarMouseMove;
            _root.MouseLeftButtonUp -= OnBarMouseUp;
            _root.LostMouseCapture -= OnLostCapture;
            _close.Click -= OnCloseClicked;
            Hide();
        }
    }
}
