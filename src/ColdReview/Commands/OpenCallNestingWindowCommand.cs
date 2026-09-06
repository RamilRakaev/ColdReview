using ColdReview.ToolWindows;

namespace ColdReview.Commands
{
    [Command(PackageGuids.ColdReviewCmdSetString, PackageIds.OpenCallNestingWindow)]
    internal sealed class OpenCallNestingWindowCommand : BaseCommand<OpenCallNestingWindowCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await CallNestingWindow.ShowAsync();
        }
    }

    [Command(PackageGuids.ColdReviewCmdSetString, PackageIds.OpenCallNestingWindowTools)]
    internal sealed class OpenCallNestingWindowToolsCommand : BaseCommand<OpenCallNestingWindowToolsCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await CallNestingWindow.ShowAsync();
        }
    }

    [Command(PackageGuids.ColdReviewCmdSetString, PackageIds.OpenCallNestingWindowDebug)]
    internal sealed class OpenCallNestingWindowDebugCommand : BaseCommand<OpenCallNestingWindowDebugCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await CallNestingWindow.ShowAsync();
        }
    }
}
