// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace LibObjectFile.PE;

/// <summary>
/// Base class for data contained in a <see cref="PESection"/>.
/// </summary>
public abstract class PESectionData : PEObject, IVirtualAddressable
{
    /// <summary>
    /// Gets the parent <see cref="PESection"/> of this section data.
    /// </summary>
    public new PESection? Parent
    {
        get => (PESection?)base.Parent;

        internal set => base.Parent = value;
    }

    /// <summary>
    /// Gets the virtual address of this section data.
    /// </summary>
    /// <remarks>
    /// This property is updated dynamically based on the previous section data.
    /// </remarks>
    public RVA VirtualAddress
    {
        get;
        internal set;
    }

    /// <summary>
    /// Checks if the specified virtual address is contained by this instance.
    /// </summary>
    /// <param name="virtualAddress">The virtual address to check if it belongs to this instance.</param>
    /// <returns><c>true</c> if the specified virtual address is contained by this instance; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsVirtual(RVA virtualAddress) => VirtualAddress <= virtualAddress && virtualAddress < VirtualAddress + Size;

    public virtual int ReadAt(uint offset, Span<byte> destination)
    {
        throw new NotSupportedException($"The read operation is not supported for {this.GetType().FullName}");
    }

    public virtual void WriteAt(uint offset, ReadOnlySpan<byte> source)
    {
        throw new NotSupportedException($"The write operation is not supported for {this.GetType().FullName}");
    }

    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"VirtualAddress: {VirtualAddress}, Size = 0x{Size:X4}");
        return true;
    }
}



internal sealed class PESectionDataTemp : PESectionData
{
    public static readonly PESectionDataTemp Instance = new ();
    
    protected override void Read(PEImageReader reader) => throw new NotSupportedException();

    protected override void Write(PEImageWriter writer) => throw new NotSupportedException();

    public override void UpdateLayout(DiagnosticBag diagnostics) => throw new NotSupportedException();
}