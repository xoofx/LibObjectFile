// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile
{
    public readonly struct DiagnosticMessage
    {
        public DiagnosticMessage(DiagnosticKind kind, DiagnosticId id, string message)
        {
            Kind = kind;
            Id = id;
            Context = null;
            Message = message;
        }

        public DiagnosticMessage(DiagnosticKind kind, DiagnosticId id, string message, object context)
        {
            Kind = kind;
            Id = id;
            Context = context;
            Message = message;
        }

        public DiagnosticKind Kind { get; }

        public DiagnosticId Id { get; }

        public object Context { get; }

        public string Message { get; }
        
        public override string ToString()
        {
            return $"{Kind}: {Message}";
        }
    }
}