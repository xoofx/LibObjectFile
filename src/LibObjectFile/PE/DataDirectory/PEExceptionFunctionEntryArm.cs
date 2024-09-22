// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Represents an ARM exception function entry in a Portable Executable (PE) file.
/// </summary>
public sealed class PEExceptionFunctionEntryArm : PEExceptionFunctionEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEExceptionFunctionEntryArm"/> class with the specified begin address and unwind data.
    /// </summary>
    /// <param name="beginAddress">The begin address of the exception function entry.</param>
    /// <param name="unwindData">The unwind data.</param>
    public PEExceptionFunctionEntryArm(PESectionDataLink beginAddress, uint unwindData) : base(beginAddress)
    {
        UnwindData = unwindData;
    }

    /// <summary>
    /// Gets or sets the unwind data.
    /// </summary>
    public uint UnwindData { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{nameof(BeginAddress)} = {BeginAddress.RVA()}, {nameof(UnwindData)} = 0x{UnwindData:X}";
    }
}
