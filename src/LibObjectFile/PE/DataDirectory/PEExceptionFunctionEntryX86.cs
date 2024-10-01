// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Represents an exception function entry for the x86 architecture in a Portable Executable (PE) file.
/// </summary>
public sealed class PEExceptionFunctionEntryX86 : PEExceptionFunctionEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEExceptionFunctionEntryX86"/> class with the specified begin address, end address, and unwind info address.
    /// </summary>
    /// <param name="beginAddress">The begin address of the exception function entry.</param>
    /// <param name="endAddress">The end address of the exception function entry.</param>
    /// <param name="unwindInfoAddress">The unwind info address of the exception function entry.</param>
    public PEExceptionFunctionEntryX86(PESectionDataLink beginAddress, PESectionDataLink endAddress, PESectionDataLink unwindInfoAddress) : base(beginAddress)
    {
        EndAddress = endAddress;
        UnwindInfoAddress = unwindInfoAddress;
    }

    /// <summary>
    /// Gets or sets the end address of the exception function entry.
    /// </summary>
    public PESectionDataLink EndAddress { get; set; }

    /// <summary>
    /// Gets or sets the unwind info address of the exception function entry.
    /// </summary>
    public PESectionDataLink UnwindInfoAddress { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{nameof(BeginAddress)} = {BeginAddress.RVA()}, {nameof(EndAddress)} = {EndAddress.RVA()}, {nameof(UnwindInfoAddress)} = {UnwindInfoAddress.RVA()}";
    }

    /// <inheritdoc />
    internal override void Verify(PEVerifyContext context, PEExceptionDirectory exceptionDirectory, int index)
    {
        base.Verify(context, exceptionDirectory, index);
        context.VerifyObject(EndAddress.Container, exceptionDirectory, $"the {nameof(EndAddress)} of the {nameof(PEExceptionFunctionEntryX86)} at #{index}", false);
        context.VerifyObject(UnwindInfoAddress.Container, exceptionDirectory, $"the {nameof(UnwindInfoAddress)} of the {nameof(PEExceptionFunctionEntryX86)} at #{index}", false);
    }
}
