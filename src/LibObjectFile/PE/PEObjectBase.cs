// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// Base class for all Portable Executable (PE) objects.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class PEObjectBase : ObjectFileElement<PELayoutContext, PEVisitorContext, PEImageReader, PEImageWriter>;