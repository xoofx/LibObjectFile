// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile;

/// <summary>
/// Defines a node in an object file with its layout that can be updated.
/// </summary>
public abstract class ObjectFileNode : ObjectFileNodeBase
{
    /// <summary>
    /// Updates the size of this node.
    /// </summary>
    /// <param name="diagnostics">The diagnostics.</param>
    public abstract void UpdateLayout(DiagnosticBag diagnostics);
}