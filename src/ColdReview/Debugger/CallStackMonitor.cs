using System.Reflection;
using System.Runtime.InteropServices;
using ColdReview.ToolWindows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace ColdReview.Debugger
{
    internal sealed class CallStackMonitor : IDisposable
    {
        private readonly AsyncPackage _package;
        private EnvDTE.DebuggerEvents _debuggerEvents;
        private DTE2 _dte;
        private bool _autoOpened;
        private bool _disposed;

        private CallStackMonitor(AsyncPackage package)
        {
            _package = package;
        }

        public static CallStackMonitor Instance { get; private set; }

        public static CallStackSnapshot Current { get; private set; } = CallStackSnapshot.Empty;

        public static event EventHandler<CallStackSnapshot> Changed;

        public static void Initialize(AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Instance?.Dispose();
            var monitor = new CallStackMonitor(package);
            monitor.Hook();
            Instance = monitor;
        }

        public void Refresh()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Publish(Capture());
        }

        public void ActivateFrame(int index)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_dte?.Debugger?.CurrentThread == null)
            {
                return;
            }

            try
            {
                StackFrames frames = _dte.Debugger.CurrentThread.StackFrames;
                if (frames == null || index < 0 || index >= frames.Count)
                {
                    return;
                }

                _dte.Debugger.CurrentStackFrame = frames.Item(index + 1);
                CallFrameInfo info = Current.Frames.ElementAtOrDefault(index);
                if (info != null && !string.IsNullOrEmpty(info.FileName))
                {
                    NavigateTo(info.FileName, info.LineNumber);
                }

                Refresh();
            }
            catch (COMException)
            {
            }
        }

        public void CopyPathToClipboard(bool userCodeOnly)
        {
            IEnumerable<CallFrameInfo> frames = Current.Frames;
            if (userCodeOnly)
            {
                frames = frames.Where(f => f.IsUserCode);
            }

            var ordered = frames.Reverse().ToList();
            string text = string.Join(
                Environment.NewLine,
                ordered.Select((f, i) => new string(' ', i * 2) + (i == 0 ? string.Empty : "→ ") + f.ShortName + FormatArgs(f)));

            if (!string.IsNullOrWhiteSpace(text))
            {
                System.Windows.Clipboard.SetText(text);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Unhook();
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        private void Hook()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = Package.GetGlobalService(typeof(SDTE)) as DTE2
                ?? Package.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            if (_dte == null)
            {
                return;
            }

            // Keep the events sink alive. EnvDTE event COM wrappers are collected otherwise.
            _debuggerEvents = _dte.Events.DebuggerEvents;
            _debuggerEvents.OnEnterBreakMode += OnEnterBreakMode;
            _debuggerEvents.OnEnterRunMode += OnEnterRunMode;
            _debuggerEvents.OnEnterDesignMode += OnEnterDesignMode;
            _debuggerEvents.OnContextChanged += OnContextChanged;
        }

        private void Unhook()
        {
            if (_debuggerEvents == null)
            {
                return;
            }

            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                _debuggerEvents.OnEnterBreakMode -= OnEnterBreakMode;
                _debuggerEvents.OnEnterRunMode -= OnEnterRunMode;
                _debuggerEvents.OnEnterDesignMode -= OnEnterDesignMode;
                _debuggerEvents.OnContextChanged -= OnContextChanged;
            }
            catch (COMException)
            {
            }

            _debuggerEvents = null;
        }

        private void OnEnterBreakMode(dbgEventReason reason, ref dbgExecutionAction action)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Publish(Capture());
            MaybeOpenWindow();
        }

        private void OnEnterRunMode(dbgEventReason reason)
        {
            Publish(new CallStackSnapshot(Array.Empty<CallFrameInfo>(), debuggerActive: true, inBreakMode: false, status: "Идёт выполнение…"));
        }

        private void OnEnterDesignMode(dbgEventReason reason)
        {
            _autoOpened = false;
            Publish(CallStackSnapshot.Empty);
        }

        private void OnContextChanged(Process newProcess, Program newProgram, Thread newThread, StackFrame newStackFrame)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_dte?.Debugger?.CurrentMode == dbgDebugMode.dbgBreakMode)
            {
                Publish(Capture());
            }
        }

        private void MaybeOpenWindow()
        {
            if (_autoOpened)
            {
                return;
            }

            _autoOpened = true;
            _package.JoinableTaskFactory.RunAsync(async () =>
            {
                await _package.JoinableTaskFactory.SwitchToMainThreadAsync();
                await CallNestingWindow.ShowAsync();
                await VS.StatusBar.ShowMessageAsync("Cold Review: цепочка вызовов обновлена");
            }).FileAndForget("ColdReview/AutoOpen");
        }

        private CallStackSnapshot Capture()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                EnvDTE.Debugger debugger = _dte?.Debugger;
                if (debugger == null)
                {
                    return CallStackSnapshot.Empty;
                }

                if (debugger.CurrentMode != dbgDebugMode.dbgBreakMode)
                {
                    bool active = debugger.CurrentMode == dbgDebugMode.dbgRunMode;
                    return new CallStackSnapshot(
                        Array.Empty<CallFrameInfo>(),
                        debuggerActive: active,
                        inBreakMode: false,
                        status: active ? "Идёт выполнение…" : "Отладчик не запущен");
                }

                if (debugger.CurrentThread?.StackFrames == null)
                {
                    return new CallStackSnapshot(Array.Empty<CallFrameInfo>(), true, true, "Нет стека вызовов");
                }

                var frames = new List<CallFrameInfo>();
                int currentIndex = 0;
                try
                {
                    StackFrame current = debugger.CurrentStackFrame;
                    if (current != null)
                    {
                        int i = 0;
                        foreach (StackFrame candidate in debugger.CurrentThread.StackFrames)
                        {
                            if (ReferenceEquals(candidate, current) || NamesEqual(candidate, current))
                            {
                                currentIndex = i;
                                break;
                            }

                            i++;
                        }
                    }
                }
                catch (COMException)
                {
                }

                int index = 0;
                foreach (StackFrame frame in debugger.CurrentThread.StackFrames)
                {
                    frames.Add(ReadFrame(frame, index, index == currentIndex));
                    index++;
                }

                if (frames.Count == 0)
                {
                    return new CallStackSnapshot(frames, true, true, "Стек пуст");
                }

                string currentName = frames.FirstOrDefault(f => f.IsCurrent)?.ShortName ?? frames[0].ShortName;
                string caller = frames.Skip(1).FirstOrDefault(f => f.IsUserCode)?.ShortName;
                string status = caller == null
                    ? "Вы в " + currentName
                    : caller + "  →  " + currentName;

                return new CallStackSnapshot(frames, true, true, status);
            }
            catch (COMException)
            {
                return new CallStackSnapshot(Array.Empty<CallFrameInfo>(), true, true, "Не удалось прочитать стек");
            }
        }

        private static bool NamesEqual(StackFrame left, StackFrame right)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                return left != null && right != null && left.FunctionName == right.FunctionName;
            }
            catch (COMException)
            {
                return false;
            }
        }

        private static CallFrameInfo ReadFrame(StackFrame frame, int index, bool isCurrent)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string functionName = SafeString(() => frame.FunctionName);
            CallFrameInfo info = CallFrameInfo.Parse(functionName);
            info.Index = index;
            info.IsCurrent = isCurrent;
            info.Module = SafeString(() => frame.Module);
            info.Language = SafeString(() => frame.Language);
            info.ReturnType = SafeString(() => frame.ReturnType);
            info.FileName = GetComProperty(frame, "FileName", string.Empty);
            info.LineNumber = GetComProperty(frame, "LineNumber", 0);
            bool userFlag = GetComProperty(frame, "UserCode", true);
            info.IsUserCode = userFlag && !IsExternal(functionName, info.Module);
            info.Arguments = ReadArguments(frame);
            return info;
        }

        private static IReadOnlyList<CallArgument> ReadArguments(StackFrame frame)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var list = new List<CallArgument>();
            try
            {
                Expressions args = frame.Arguments;
                if (args == null)
                {
                    return list;
                }

                foreach (Expression expression in args)
                {
                    list.Add(new CallArgument(
                        SafeString(() => expression.Name),
                        SafeString(() => expression.Type),
                        SafeString(() => expression.Value)));
                }
            }
            catch (COMException)
            {
            }

            return list;
        }

        private static bool IsExternal(string functionName, string module)
        {
            if (string.IsNullOrEmpty(functionName) || functionName.IndexOf("[External Code]", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string[] prefixes =
            {
                "System.", "Microsoft.", "MS.Internal.", "Windows.", "PresentationFramework",
                "PresentationCore", "WindowsBase", "mscorlib", "netstandard", "StdLib"
            };

            foreach (string prefix in prefixes)
            {
                if (functionName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(module) && module.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void NavigateTo(string fileName, int lineNumber)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                Window window = _dte.ItemOperations.OpenFile(fileName);
                if (lineNumber > 0 && window?.Document?.Selection is TextSelection selection)
                {
                    selection.GotoLine(lineNumber, Select: true);
                }
            }
            catch (COMException)
            {
            }
        }

        private void Publish(CallStackSnapshot snapshot)
        {
            Current = snapshot;
            Changed?.Invoke(this, snapshot);
        }

        private static string FormatArgs(CallFrameInfo frame)
        {
            return string.IsNullOrEmpty(frame.ArgumentsSummary) ? string.Empty : "(" + frame.ArgumentsSummary + ")";
        }

        private static string SafeString(Func<string> getter)
        {
            try
            {
                return getter() ?? string.Empty;
            }
            catch (COMException)
            {
                return string.Empty;
            }
        }

        private static T GetComProperty<T>(object target, string name, T fallback)
        {
            try
            {
                object value = target.GetType().InvokeMember(
                    name,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty,
                    null,
                    target,
                    null);
                if (value is T typed)
                {
                    return typed;
                }

                if (value != null)
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            catch (Exception)
            {
            }

            return fallback;
        }
    }
}
