// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;
using System.IO;

namespace LibObjectFile;

/// <summary>
/// Base class used for layout-ing an object file.
/// </summary>
/// <param name="file">The root object file.</param>
/// <param name="diagnostics">The diagnostics.</param>
public abstract class VisitorContextBase(ObjectFileElement file, DiagnosticBag diagnostics)
{
    public ObjectFileElement File { get; } = file;


    public DiagnosticBag Diagnostics { get; } = diagnostics;


    public bool HasErrors => Diagnostics.HasErrors;

    public TextWriter? DebugLog { get; set; }
}

/// <summary>
/// Base class used for layout-ing an object file.
/// </summary>
/// <typeparam name="TFile">The type of the object file.</typeparam>
/// <param name="file">The root object file.</param>
/// <param name="diagnostics">The diagnostics.</param>
public abstract class VisitorContextBase<TFile>(TFile file, DiagnosticBag diagnostics) : VisitorContextBase(file, diagnostics)
    where TFile : ObjectFileElement
{
    public new TFile File => (TFile)base.File;
}