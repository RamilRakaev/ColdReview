using System.IO;

namespace ColdReview.Debugger
{
    internal sealed class CallFrameInfo
    {
        public int Index { get; set; }
        public string FunctionName { get; set; } = string.Empty;
        public string NamespaceName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public bool IsUserCode { get; set; } = true;
        public bool IsCurrent { get; set; }
        public string ReturnType { get; set; } = string.Empty;
        public IReadOnlyList<CallArgument> Arguments { get; set; } = Array.Empty<CallArgument>();
        public bool ShowCallerConnector { get; set; }

        public string ShortName
        {
            get
            {
                if (!string.IsNullOrEmpty(ClassName) && !string.IsNullOrEmpty(MethodName))
                {
                    return ClassName + "." + MethodName;
                }

                return string.IsNullOrEmpty(MethodName) ? FunctionName : MethodName;
            }
        }

        public string FileLabel
        {
            get
            {
                if (string.IsNullOrEmpty(FileName))
                {
                    return string.Empty;
                }

                string name = Path.GetFileName(FileName);
                return LineNumber > 0 ? name + ":" + LineNumber : name;
            }
        }

        public string ArgumentsSummary
        {
            get
            {
                if (Arguments == null || Arguments.Count == 0)
                {
                    return string.Empty;
                }

                return string.Join(", ", Arguments.Select(a => a.Display));
            }
        }

        public string CalledByLabel => IsCurrent ? "вы здесь" : "вызвал";

        public static CallFrameInfo Parse(string functionName)
        {
            var info = new CallFrameInfo
            {
                FunctionName = functionName ?? string.Empty
            };

            string raw = functionName ?? string.Empty;
            int paren = raw.IndexOf('(');
            if (paren >= 0)
            {
                raw = raw.Substring(0, paren);
            }

            string[] parts = raw.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                info.MethodName = functionName ?? string.Empty;
                return info;
            }

            info.MethodName = parts[parts.Length - 1];
            if (parts.Length >= 2)
            {
                info.ClassName = parts[parts.Length - 2];
                info.NamespaceName = string.Join(".", parts.Take(parts.Length - 2));
            }

            return info;
        }
    }
}
