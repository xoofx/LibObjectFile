using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LibObjectFile
{
    [DebuggerDisplay("Count = {Messages.Count}, HasErrors = {" + nameof(HasErrors) + "}")]
    public class DiagnosticBag
    {
        private readonly List<DiagnosticMessage> _messages;

        public DiagnosticBag()
        {
            _messages = new List<DiagnosticMessage>();
        }

        public IReadOnlyList<DiagnosticMessage> Messages => _messages;

        public bool HasErrors { get; private set; }

        public void Clear()
        {
            _messages.Clear();
            HasErrors = false;
        }

        public void Log(DiagnosticMessage message)
        {
            if (message.Message == null) throw new InvalidOperationException($"{nameof(DiagnosticMessage)}.{nameof(DiagnosticMessage.Message)} cannot be null");
            _messages.Add(message);
            if (message.Kind == DiagnosticKind.Error)
            {
                HasErrors = true;
            }
        }

        public void Error(string message, object context = null)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            Log(new DiagnosticMessage(DiagnosticKind.Error, message, context));
        }

        public void Warning(string message, object context = null)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            Log(new DiagnosticMessage(DiagnosticKind.Warning, message, context));
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var diagnosticMessage in Messages)
            {
                builder.AppendLine(diagnosticMessage.ToString());
            }

            return builder.ToString();
        }
    }
}