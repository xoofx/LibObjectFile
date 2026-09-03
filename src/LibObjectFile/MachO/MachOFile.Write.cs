// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.MachO;

partial class MachOFile
{
    /// <summary>
    /// Writes this image to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <exception cref="ObjectFileException">
    /// The load commands no longer fit in the space before the content after them. See
    /// <see cref="AvailableLoadCommandSpace"/>.
    /// </exception>
    public void Write(Stream stream)
    {
        if (!TryWrite(stream, out var diagnostics))
        {
            throw new ObjectFileException($"Unexpected error while writing the Mach-O file", diagnostics);
        }
    }

    /// <summary>
    /// Tries to write this image to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="diagnostics">The diagnostics collected while writing.</param>
    /// <returns><c>true</c> if the image was written without errors; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    public bool TryWrite(Stream stream, out DiagnosticBag diagnostics)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var writer = new MachOWriter(this, stream, new DiagnosticBag());
        diagnostics = writer.Diagnostics;

        Verify(writer.VisitorContext);
        if (diagnostics.HasErrors)
        {
            return false;
        }

        UpdateLayout(writer.VisitorContext);
        if (diagnostics.HasErrors)
        {
            return false;
        }

        Write(writer);
        stream.Flush();

        return !diagnostics.HasErrors;
    }

    /// <inheritdoc />
    public override void Write(MachOWriter writer)
    {
        if (IsCodeSignatureStale)
        {
            writer.Diagnostics.Error(
                DiagnosticId.MACHO_ERR_StaleCodeSignature,
                "The image has been edited since it was signed, so its signature no longer covers what it contains. Sign it again before writing.");
            return;
        }

        if (LoadCommandsEndOffset > ContentStartOffset)
        {
            writer.Diagnostics.Error(
                DiagnosticId.MACHO_ERR_NoRoomForLoadCommands,
                $"The load commands need {LoadCommandsEndOffset} bytes but the content after them starts at {ContentStartOffset}. The image has {AvailableLoadCommandSpace} bytes of space left.");
            return;
        }

        var basePosition = writer.Position;

        foreach (var content in Content)
        {
            writer.Position = basePosition + content.Position;
            content.WriteContent(writer);
            if (writer.Diagnostics.HasErrors) return;
        }
    }
}
