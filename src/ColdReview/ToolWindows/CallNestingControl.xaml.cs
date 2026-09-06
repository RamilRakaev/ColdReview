using System.Windows.Controls;
using ColdReview.Debugger;

namespace ColdReview.ToolWindows
{
    public partial class CallNestingControl : UserControl
    {
        private readonly CallNestingViewModel _viewModel = new CallNestingViewModel();

        public CallNestingControl()
        {
            InitializeComponent();
            DataContext = _viewModel;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _viewModel.Attach();
            _viewModel.Apply(CallStackMonitor.Current);
        }

        private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _viewModel.Detach();
        }
    }
}
