// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the Base Relocation Directory in a Portable Executable (PE) file.
/// </summary>
public sealed class PEBaseRelocationDirectory : PEDataDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEBaseRelocationDirectory"/> class.
    /// </summary>
    public PEBaseRelocationDirectory() : base(PEDataDirectoryKind.BaseRelocation)
    {
    }

    /// <inheritdoc/>
    protected override uint ComputeHeaderSize(PELayoutContext context) => 0;

    /// <inheritdoc/>
    public override void Read(PEImageReader reader)
    {
        reader.Position = Position;
        var size = (long)Size;

        while (size > 0)
        {
            var block = new PEBaseRelocationBlock()
            {
                Position = reader.Position
            };

            block.Read(reader);

            size -= (uint)block.Size;

            // Add the block to the content
            Content.Add(block);
        }
    }

    public override void Verify(PEVerifyContext context)
    {
        foreach (var block in Content)
        {
            if (block is not PEBaseRelocationBlock relocationBlock)
            {
                context.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidBaseRelocationBlock, $"Invalid block found in BaseRelocationDirectory: {block}. Only PEBaseRelocationBlock are allowed.");
            }

            block.Verify(context);
        }

        base.Verify(context);
    }
}
