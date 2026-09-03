// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using LibObjectFile.MachO.CodeSign;
using LibObjectFile.Utils;

namespace LibObjectFile.MachO;

partial class MachOFile
{
    /// <summary>
    /// Replaces this image's code signature with an ad-hoc one, adding the signature and its load
    /// command if the image is unsigned.
    /// </summary>
    /// <param name="identifier">
    /// The signing identity to record. Apple's tooling uses the binary's file name, and this is
    /// the string the kernel reports as the code's identity.
    /// </param>
    /// <remarks>
    /// An ad-hoc signature carries no certificate; it asserts only that the image matches its own
    /// hashes. Apple Silicon refuses to execute an unsigned image, so an edited arm64 binary has
    /// to be re-signed to stay runnable. Any change to the image invalidates the signature, so
    /// this has to be the last thing done before writing.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="identifier"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="identifier"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">
    /// The image has no <c>__LINKEDIT</c> or <c>__TEXT</c> segment, has content past the point
    /// the signature would occupy, or has no room left for the signature load command.
    /// </exception>
    public void AdHocSign(string identifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        var linkEdit = FindSegment("__LINKEDIT")
            ?? throw new InvalidOperationException("The image has no __LINKEDIT segment, so there is nowhere to put a signature.");
        var text = FindSegment("__TEXT")
            ?? throw new InvalidOperationException("The image has no __TEXT segment, so the signature cannot describe its executable range.");

        // Everything below can fail, and a half-signed image is worse than an unsigned one, so
        // what changes is remembered first and put back if it does. The state is captured before
        // the command is added, since adding one is itself a change to undo.
        var previousFileSize = linkEdit.FileSize;
        var previousVmSize = linkEdit.VmSize;
        var previousStale = IsCodeSignatureStale;
        var added = new List<MachOContent>();
        MachOContent? removed = null;
        var removedIndex = -1;
        MachOLinkEditDataCommand? addedCommand = null;

        var command = CodeSignature;
        var previousOffset = command?.DataOffset ?? 0;
        var previousSize = command?.DataSize ?? 0;

        try
        {
            if (command is null)
            {
                command = new MachOLinkEditDataCommand
                {
                    Type = MachOLoadCommandType.CodeSignature,
                    Size = MachOLinkEditDataCommand.CommandSize,
                };
                AppendCommand(command);
                addedCommand = command;
            }
            // A signature covers everything before it, so it has to be the last thing in the file.
            // Whatever occupied that place before, including a previous signature, is replaced.
            if (command.DataOffset != 0)
            {
                for (var i = Content.Count - 1; i >= 0; i--)
                {
                    if (Content[i].Position == command.DataOffset)
                    {
                        removed = Content[i];
                        removedIndex = i;
                        Content.RemoveAt(i);
                        break;
                    }
                }
            }

            var contentEnd = ComputeFileSize();

            // LC_CODE_SIGNATURE records where the signature is in a 32-bit dataoff, so an image
            // this large cannot carry one. Casting would point the offset back into the image.
            if (contentEnd > uint.MaxValue)
            {
                throw new InvalidOperationException($"The image is 0x{contentEnd:X} bytes, too large for the 32-bit offset LC_CODE_SIGNATURE records.");
            }

            var signatureOffset = AlignHelper.AlignUp((uint)contentEnd, (uint)MachOCodeSignatureConstants.SignatureAlignment);

            var builder = new MachOAdHocSignatureBuilder(identifier)
            {
                CodeLimit = signatureOffset,
                ExecSegmentBase = text.FileOffset,
                ExecSegmentLimit = text.FileSize,
                ExecSegmentFlags = FileType == MachOFileType.Execute ? MachOCodeSignatureConstants.ExecSegMainBinary : 0,
            };
            var signatureSize = builder.ComputeSize();

            command.DataOffset = signatureOffset;
            command.DataSize = signatureSize;

            linkEdit.FileSize = signatureOffset + signatureSize - linkEdit.FileOffset;
            linkEdit.VmSize = Math.Max(linkEdit.VmSize, linkEdit.FileSize);

            // Aligning the signature can leave a gap, and every byte of the file has to belong to
            // some content, so the padding is added rather than left as a hole in the list.
            if (signatureOffset > contentEnd)
            {
                var gap = new MachOStreamContent(new MemoryStream(new byte[signatureOffset - contentEnd]))
                {
                    Position = contentEnd,
                };
                Content.Add(gap);
                added.Add(gap);
            }

            var placeholder = new MachOStreamContent(new MemoryStream(new byte[signatureSize])) { Position = signatureOffset };
            Content.Add(placeholder);
            added.Add(placeholder);

            // Signing is what makes the image match its signature again, so the edit is settled here
            // rather than after the digests are taken. Writing checks this, and the digests are taken
            // by writing the image out.
            IsCodeSignatureStale = false;

            // The digests cover the image as it will finally be written, so the header, the command
            // table and the signature's own offsets all have to be settled before they are taken.
            var image = new MemoryStream();
            Write(image);
            placeholder.Content = new MemoryStream(builder.Build(image.GetBuffer().AsSpan(0, (int)signatureOffset)));
        }
        catch
        {
            foreach (var content in added)
            {
                Content.Remove(content);
            }

            if (removed is not null)
            {
                Content.Insert(removedIndex, removed);
            }

            if (addedCommand is not null)
            {
                LoadCommands.Remove(addedCommand);
            }
            else if (command is not null)
            {
                command.DataOffset = previousOffset;
                command.DataSize = previousSize;
            }

            linkEdit.FileSize = previousFileSize;
            linkEdit.VmSize = previousVmSize;
            IsCodeSignatureStale = previousStale;
            throw;
        }
    }

}
