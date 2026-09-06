namespace ColdReview.Debugger
{
    internal sealed class CallStackSnapshot
    {
        public static readonly CallStackSnapshot Empty = new CallStackSnapshot(
            Array.Empty<CallFrameInfo>(),
            debuggerActive: false,
            inBreakMode: false,
            status: "Отладчик не запущен");

        public CallStackSnapshot(
            IReadOnlyList<CallFrameInfo> frames,
            bool debuggerActive,
            bool inBreakMode,
            string status)
        {
            Frames = frames ?? Array.Empty<CallFrameInfo>();
            DebuggerActive = debuggerActive;
            InBreakMode = inBreakMode;
            Status = status ?? string.Empty;
        }

        public IReadOnlyList<CallFrameInfo> Frames { get; }
        public bool DebuggerActive { get; }
        public bool InBreakMode { get; }
        public string Status { get; }

        public string Breadcrumb
        {
            get
            {
                if (Frames.Count == 0)
                {
                    return string.Empty;
                }

                return string.Join("  →  ", Frames.Reverse().Select(f => f.ShortName));
            }
        }
    }
}
