// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO.Internal;

namespace LibObjectFile.MachO;

/// <summary>
/// A load command naming a dynamic library, covering <c>LC_LOAD_DYLIB</c>, <c>LC_ID_DYLIB</c>,
/// <c>LC_LOAD_WEAK_DYLIB</c>, <c>LC_REEXPORT_DYLIB</c>, <c>LC_LOAD_UPWARD_DYLIB</c> and
/// <c>LC_LAZY_LOAD_DYLIB</c>.
/// </summary>
/// <remarks>
/// The order of these commands is what dyld reports as the library ordinal, so inserting one
/// anywhere other than after the existing ones renumbers the bindings of every command following
/// it. <see cref="MachOFile"/> appends rather than inserts for that reason.
/// </remarks>
public sealed class MachODylibCommand : MachOPathLoadCommand
{
    /// <summary>
    /// Gets or sets the path of the library, which is its install name rather than a file path
    /// and may start with <c>@rpath</c>, <c>@executable_path</c> or <c>@loader_path</c>.
    /// </summary>
    public string Name
    {
        get => Value;
        set => Value = value;
    }

    /// <summary>
    /// Gets or sets the build timestamp of the library.
    /// </summary>
    public uint Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the current version of the library, packed as 16.8.8 bits.
    /// </summary>
    public uint CurrentVersion { get; set; }

    /// <summary>
    /// Gets or sets the oldest version of the library this image is compatible with, packed as 16.8.8 bits.
    /// </summary>
    public uint CompatibilityVersion { get; set; }

    /// <summary>
    /// Gets a value indicating whether a missing library is tolerated at load time.
    /// </summary>
    public bool IsWeak => Type == MachOLoadCommandType.LoadWeakDylib;

    /// <inheritdoc />
    protected override unsafe uint FixedSize => (uint)sizeof(RawDylibCommand);

    /// <inheritdoc />
    public override unsafe void Read(MachOReader reader)
    {
        var commandPosition = reader.Position;
        if (!reader.TryReadData(sizeof(RawDylibCommand), out RawDylibCommand raw))
        {
            reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated dylib command at 0x{commandPosition:X}");
            return;
        }

        Timestamp = raw.Timestamp;
        CurrentVersion = raw.CurrentVersion;
        CompatibilityVersion = raw.CompatibilityVersion;
        ReadValue(reader, commandPosition, raw.NameOffset);
    }

    /// <inheritdoc />
    public override unsafe void Write(MachOWriter writer)
    {
        writer.Write(new RawDylibCommand
        {
            Cmd = (uint)Type,
            CmdSize = (uint)Size,
            NameOffset = (uint)sizeof(RawDylibCommand),
            Timestamp = Timestamp,
            CurrentVersion = CurrentVersion,
            CompatibilityVersion = CompatibilityVersion,
        });
        WriteValue(writer);
    }
}
