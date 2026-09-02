// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.MachO;

/// <summary>
/// Base class for all elements of a <see cref="MachOFile"/>.
/// </summary>
public abstract class MachOObject : ObjectFileElement<MachOVisitorContext, MachOVisitorContext, MachOReader, MachOWriter>
{
}
