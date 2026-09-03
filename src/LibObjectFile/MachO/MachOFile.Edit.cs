// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace LibObjectFile.MachO;

partial class MachOFile
{
    /// <summary>
    /// Adds a dependency on <paramref name="name"/>, so dyld loads that library at startup.
    /// </summary>
    /// <param name="name">
    /// The install name of the library. It is resolved by dyld rather than opened as a path, so
    /// it may start with <c>@rpath</c>, <c>@executable_path</c> or <c>@loader_path</c>.
    /// </param>
    /// <returns>The command that was added.</returns>
    /// <remarks>
    /// The command is appended, never inserted, because dyld numbers libraries by their command
    /// order and every binding in the symbol table refers to a library by that number. Appending
    /// leaves the existing numbering alone.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty, or the type is not a dylib command.</exception>
    /// <exception cref="InvalidOperationException">
    /// The image has no room left before its first section. The message states the shortfall.
    /// </exception>
    public MachODylibCommand AddLoadDylib(string name)
        => AddLoadDylib(name, MachOLoadCommandType.LoadDylib);

    /// <inheritdoc cref="AddLoadDylib(string)"/>
    /// <param name="name">The install name to record, as it will appear to dyld.</param>
    /// <param name="type">
    /// Which dylib command to add. <see cref="MachOLoadCommandType.LoadWeakDylib"/> makes the
    /// library optional at runtime.
    /// </param>
    public MachODylibCommand AddLoadDylib(string name, MachOLoadCommandType type)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (type is not (MachOLoadCommandType.LoadDylib
            or MachOLoadCommandType.LoadWeakDylib
            or MachOLoadCommandType.ReexportDylib
            or MachOLoadCommandType.LoadUpwardDylib
            or MachOLoadCommandType.LazyLoadDylib))
        {
            throw new ArgumentException($"{type} is not a dylib load command", nameof(type));
        }

        var command = new MachODylibCommand
        {
            Type = type,
            Is64Bit = Is64Bit,
            Name = name,
            Timestamp = 0,
            // A dependency the loader has no version expectations of. dyld only compares these
            // against the target library's own values, and 1.0.0 always satisfies the comparison.
            CurrentVersion = 0x10000,
            CompatibilityVersion = 0x10000,
        };

        AppendCommand(command);
        return command;
    }

    /// <summary>
    /// Adds a runpath that <c>@rpath</c> expands to when resolving dependencies.
    /// </summary>
    /// <param name="path">The directory to add, commonly relative to <c>@executable_path</c>.</param>
    /// <returns>The command that was added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">
    /// The image has no room left before its first section. The message states the shortfall.
    /// </exception>
    public MachOPathCommand AddRPath(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var command = new MachOPathCommand
        {
            Type = MachOLoadCommandType.RPath,
            Is64Bit = Is64Bit,
            Path = path,
        };

        AppendCommand(command);
        return command;
    }

    /// <summary>
    /// Removes a runpath.
    /// </summary>
    /// <param name="path">The runpath to remove.</param>
    /// <returns><c>true</c> if a matching runpath was removed; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Unlike the dylib commands, runpaths are unnumbered, so dropping one changes nothing but
    /// the search order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    public bool RemoveRPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var command = RunPaths.FirstOrDefault(c => c.Path == path);
        if (command is null) return false;

        LoadCommands.Remove(command);
        MarkCodeSignatureStale();
        return true;
    }

    /// <summary>
    /// Repoints every reference to <paramref name="oldName"/> at <paramref name="newName"/>,
    /// which is what <c>install_name_tool -change</c> does.
    /// </summary>
    /// <param name="oldName">The install name currently referenced.</param>
    /// <param name="newName">The install name to reference instead.</param>
    /// <returns>The number of commands that were repointed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="oldName"/> or <paramref name="newName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="newName"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">
    /// The longer name does not fit in the room left before the first section.
    /// </exception>
    public int ChangeDylibName(string oldName, string newName)
    {
        ArgumentNullException.ThrowIfNull(oldName);
        ArgumentException.ThrowIfNullOrEmpty(newName);

        var matches = LoadCommands.OfType<MachODylibCommand>().Where(c => c.Name == oldName).ToArray();
        if (matches.Length == 0) return 0;

        // Work out what the new name costs before assigning it, so an edit that does not fit
        // leaves the image exactly as it was rather than half renamed.
        long extra = 0;
        foreach (var command in matches)
        {
            extra += Math.Max(0, (long)command.ComputeMinimumSize(newName) - (long)command.Size);
        }

        EnsureLoadCommandSpace(extra);

        foreach (var command in matches)
        {
            command.Name = newName;
            command.Size = Math.Max(command.Size, command.MinimumSize);
        }

        MarkCodeSignatureStale();
        return matches.Length;
    }

    /// <summary>
    /// Sets this image's own install name, which is what <c>install_name_tool -id</c> does.
    /// </summary>
    /// <param name="name">The install name to record.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">
    /// This image is not a dylib and so has no install name, or the longer name does not fit.
    /// </exception>
    public void SetInstallName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var command = IdDylib
            ?? throw new InvalidOperationException("This image has no LC_ID_DYLIB command, so it is not a dylib and has no install name.");

        EnsureLoadCommandSpace(Math.Max(0, (long)command.ComputeMinimumSize(name) - (long)command.Size));

        command.Name = name;
        command.Size = Math.Max(command.Size, command.MinimumSize);
        MarkCodeSignatureStale();
    }

    private void AppendCommand(MachOLoadCommand command)
    {
        command.Size = command is MachOPathLoadCommand path ? path.MinimumSize : command.Size;
        EnsureLoadCommandSpace((long)command.Size);

        // Only once the command is known to fit, since a signature is not stale if nothing changed.
        LoadCommands.Add(command);
        MarkCodeSignatureStale();
    }

    /// <summary>
    /// Records that the image no longer matches its signature, if it has one.
    /// </summary>
    private void MarkCodeSignatureStale()
    {
        if (CodeSignature is not null)
        {
            IsCodeSignatureStale = true;
        }
    }

    /// <summary>
    /// Checks that the command table can grow by <paramref name="additionalBytes"/> without
    /// pushing into the first section.
    /// </summary>
    private void EnsureLoadCommandSpace(long additionalBytes)
    {
        if (additionalBytes <= AvailableLoadCommandSpace) return;

        throw new InvalidOperationException(
            $"The load commands need {additionalBytes} more bytes but only {AvailableLoadCommandSpace} are free before the first section at 0x{ContentStartOffset:X}. " +
            "Making room would mean moving content, which would invalidate the addresses in this image.");
    }

}
