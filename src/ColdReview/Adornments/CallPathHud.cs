using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ColdReview.Debugger;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;

namespace ColdReview.Adornments
{
    internal sealed class CallPathHud
    {
        private readonly IWpfTextView _view;
        private readonly IAdornmentLayer _layer;
        private readonly Border _root;
        private readonly TextBlock _text;
        private bool _attached;

        public CallPathHud(IWpfTextView view)
        {
            _view = view;
            _layer = view.GetAdornmentLayer(CallPathAdornmentTextViewCreationListener.LayerName);

            _text = new TextBlock
            {
                FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New"),
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xFF, 0xFB)),
                TextWrapping = TextWrapping.Wrap
            };

            _root = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0xEE, 0x12, 0x2A, 0x2E)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0xC4, 0xB6)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 6, 10, 6),
                Child = _text,
                Visibility = Visibility.Collapsed,
                MaxWidth = 860,
                IsHitTestVisible = false
            };

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
            if (_attached)
            {
                return;
            }

            _layer.AddAdornment(
                AdornmentPositioningBehavior.ViewportRelative,
                null,
                null,
                _root,
                (tag, element) => _attached = false);

            Canvas.SetTop(_root, 8);
            Canvas.SetLeft(_root, 12);
            _attached = true;
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
            Hide();
        }
    }
}
