using System;

namespace LibObjectFile
{
    public class ObjectFileException : Exception
    {
        public ObjectFileException(string message, DiagnosticBag diagnostics) : base(message)
        {
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public override string Message => base.Message + Environment.NewLine + Diagnostics;
        
        public DiagnosticBag Diagnostics { get; }
    }
}