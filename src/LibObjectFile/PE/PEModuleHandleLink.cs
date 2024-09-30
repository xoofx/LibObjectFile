// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// A link to a module handle.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct PEModuleHandleLink(PESection? Container, RVO RVO) : IPELink<PESection>
{
    /// <inheritdoc />
    public override string ToString() => this.ToDisplayTextWithRVA();
}