using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using Microsoft.VisualStudio.Imaging;

namespace ColdReview.ToolWindows
{
    public class CallNestingWindow : BaseToolWindow<CallNestingWindow>
    {
        public override string GetTitle(int toolWindowId) => "Cold Review";

        public override Type PaneType => typeof(Pane);

        public override async System.Threading.Tasks.Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return new CallNestingControl();
        }

        [Guid("c3d8a14f-7e92-4b61-8f0a-1d5c9e2b7a44")]
        public class Pane : ToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.CallStackWindow;
            }
        }
    }
}
