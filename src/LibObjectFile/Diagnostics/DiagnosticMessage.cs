// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace LibObjectFile.Diagnostics;

/// <summary>
/// A diagnostic message.
/// </summary>
public readonly struct DiagnosticMessage
{
    public DiagnosticMessage(DiagnosticKind kind, DiagnosticId id, string message)
    {
        Kind = kind;
        Id = id;
        Context = null;
        Message = message;
    }

    public DiagnosticMessage(DiagnosticKind kind, DiagnosticId id, string message, object? context)
    {
        Kind = kind;
        Id = id;
        Context = context;
        Message = message;
    }

    /// <summary>
    /// Gets the kind of this message.
    /// </summary>
    public DiagnosticKind Kind { get; }

    /// <summary>
    /// Gets the id of this message.
    /// </summary>
    public DiagnosticId Id { get; }

    /// <summary>
    /// Gets the context of this message.
    /// </summary>
    public object? Context { get; }

    /// <summary>
    /// Gets the associated text of this message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the associated stack trace of this message.
    /// </summary>
    public StackTrace? StackTrace { get; init; }

    public override string ToString()
    {
        if (StackTrace is not null)
        {
            return $"{Kind} LB{(uint)Id:0000}: {Message}\n{StackTrace}";
        }
        else
        {
            return $"{Kind} LB{(uint)Id:0000}: {Message}";
        }
    }
}