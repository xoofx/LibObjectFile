namespace LibObjectFile
{
    public struct DiagnosticMessage
    {
        public DiagnosticMessage(DiagnosticKind kind, string message)
        {
            Kind = kind;
            Context = null;
            Message = message;
        }

        public DiagnosticMessage(DiagnosticKind kind, string message, object context)
        {
            Kind = kind;
            Context = context;
            Message = message;
        }

        public DiagnosticKind Kind { get; }

        public object Context { get; }

        public string Message { get; }
        
        public override string ToString()
        {
            return $"{Kind}: {Message}";
        }
    }
}