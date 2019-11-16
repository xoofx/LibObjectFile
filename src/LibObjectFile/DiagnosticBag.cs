// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

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

        public void CopyTo(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            foreach (var diagnosticMessage in Messages)
            {
                diagnostics.Log(diagnosticMessage);
            }
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

        public void Error(DiagnosticId id, string message, object context = null)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            Log(new DiagnosticMessage(DiagnosticKind.Error, id, message, context));
        }

        public void Warning(DiagnosticId id, string message, object context = null)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            Log(new DiagnosticMessage(DiagnosticKind.Warning, id, message, context));
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