using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ColdReview.Debugger;
using Microsoft.VisualStudio.Threading;

namespace ColdReview.ToolWindows
{
    internal sealed class CallNestingViewModel : INotifyPropertyChanged
    {
        private string _status = "Поставьте breakpoint и запустите отладку — Cold Review покажет, кто вызвал текущий метод.";
        private string _breadcrumb = string.Empty;
        private bool _inBreakMode;
        private bool _userCodeOnly = true;
        private CallFrameInfo _selectedFrame;

        public CallNestingViewModel()
        {
            Frames = new ObservableCollection<CallFrameInfo>();
            ActivateFrameCommand = new RelayCommand(OnActivateFrame, _ => InBreakMode);
            CopyPathCommand = new RelayCommand(_ => CopyPath(), _ => Frames.Count > 0);
            RefreshCommand = new RelayCommand(_ => Refresh());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<CallFrameInfo> Frames { get; }

        public ICommand ActivateFrameCommand { get; }
        public ICommand CopyPathCommand { get; }
        public ICommand RefreshCommand { get; }

        public string Status
        {
            get => _status;
            set => Set(ref _status, value);
        }

        public string Breadcrumb
        {
            get => _breadcrumb;
            set => Set(ref _breadcrumb, value);
        }

        public bool InBreakMode
        {
            get => _inBreakMode;
            set => Set(ref _inBreakMode, value);
        }

        public bool UserCodeOnly
        {
            get => _userCodeOnly;
            set
            {
                if (Set(ref _userCodeOnly, value))
                {
                    Apply(CallStackMonitor.Current);
                }
            }
        }

        public CallFrameInfo SelectedFrame
        {
            get => _selectedFrame;
            set
            {
                if (Set(ref _selectedFrame, value) && value != null && InBreakMode)
                {
                    OnActivateFrame(value);
                }
            }
        }

        public void Attach()
        {
            CallStackMonitor.Changed += OnChanged;
            Apply(CallStackMonitor.Current);
        }

        public void Detach()
        {
            CallStackMonitor.Changed -= OnChanged;
        }

        public void Apply(CallStackSnapshot snapshot)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            InBreakMode = snapshot.InBreakMode;
            Status = snapshot.Status;

            IEnumerable<CallFrameInfo> source = snapshot.Frames;
            if (UserCodeOnly)
            {
                source = source.Where(f => f.IsUserCode);
            }

            var visible = source.Reverse().ToList();
            for (int i = 0; i < visible.Count; i++)
            {
                visible[i].ShowCallerConnector = i < visible.Count - 1;
            }

            Frames.Clear();
            foreach (CallFrameInfo frame in visible)
            {
                Frames.Add(frame);
            }

            Breadcrumb = visible.Count == 0
                ? string.Empty
                : string.Join("  →  ", visible.Select(f => f.ShortName));

            _selectedFrame = visible.FirstOrDefault(f => f.IsCurrent) ?? visible.FirstOrDefault();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedFrame)));
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnChanged(object sender, CallStackSnapshot snapshot)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Apply(snapshot);
            }).FileAndForget("ColdReview/ApplySnapshot");
        }

        private void OnActivateFrame(object parameter)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (parameter is CallFrameInfo frame)
            {
                CallStackMonitor.Instance?.ActivateFrame(frame.Index);
            }
        }

        private void CopyPath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            CallStackMonitor.Instance?.CopyPathToClipboard(UserCodeOnly);
        }

        private void Refresh()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            CallStackMonitor.Instance?.Refresh();
        }

        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            return true;
        }
    }
}
