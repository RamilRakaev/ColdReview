using System.Runtime.InteropServices;
using System.Threading;
using ColdReview.Debugger;
using ColdReview.ToolWindows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace ColdReview
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.ColdReviewPackageString)]
    [ProvideToolWindow(typeof(CallNestingWindow.Pane), Style = VsDockStyle.Tabbed, Orientation = ToolWindowOrientation.Right, Window = ToolWindowGuids80.SolutionExplorer)]
    [ProvideToolWindowVisibility(typeof(CallNestingWindow.Pane), VSConstants.UICONTEXT.Debugging_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.Debugging_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideBindingPath]
    public sealed class ColdReviewPackage : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await this.RegisterCommandsAsync();
            this.RegisterToolWindows();
            CallStackMonitor.Initialize(this);
            CallStackMonitor.Instance?.Refresh();

            if (CallStackMonitor.Current.InBreakMode)
            {
                await CallNestingWindow.ShowAsync();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CallStackMonitor.Instance?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
