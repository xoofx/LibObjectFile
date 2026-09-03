// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.MachO;

/// <summary>
/// A thread state load command (<c>LC_THREAD</c> or <c>LC_UNIXTHREAD</c>).
/// </summary>
/// <remarks>
/// Before <c>LC_MAIN</c>, an executable specified its entry point by handing the kernel a whole
/// register state to restore, with the program counter set to the entry. Which register holds
/// the program counter depends on the architecture and the flavour, so the register words are
/// kept as they are rather than being interpreted here.
/// <para>
/// <c>LC_UNIXTHREAD</c> also asks the kernel to set up a stack, which is the difference from
/// <c>LC_THREAD</c>.
/// </para>
/// </remarks>
public sealed class MachOThreadCommand : MachOLoadCommand
{
    /// <summary>
    /// Gets the thread states carried by this command. The format allows more than one, though
    /// a linker emits a single state.
    /// </summary>
    public List<MachOThreadState> States { get; } = [];

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context)
    {
        uint total = 8;
        foreach (var state in States)
        {
            total += 8 + (uint)state.Registers.Length * 4;
        }
        Size = total;
    }

    /// <inheritdoc />
    public override void Read(MachOReader reader)
    {
        var commandPosition = reader.Position;
        reader.ReadU32();
        reader.ReadU32();

        States.Clear();
        var end = commandPosition + Size;
        while (reader.Position + 8 <= end)
        {
            var flavor = reader.ReadU32();
            var count = reader.ReadU32();

            if (reader.Position + (ulong)count * 4 > end)
            {
                reader.Diagnostics.Error(
                    DiagnosticId.MACHO_ERR_TruncatedLoadCommand,
                    $"The thread state at 0x{commandPosition:X} claims {count} registers, which do not fit in the command");
                return;
            }

            var registers = new uint[count];
            for (var i = 0; i < count; i++)
            {
                registers[i] = reader.ReadU32();
            }

            States.Add(new MachOThreadState(flavor, registers));
        }
    }

    /// <inheritdoc />
    public override void Write(MachOWriter writer)
    {
        writer.WriteU32((uint)Type);
        writer.WriteU32((uint)Size);

        foreach (var state in States)
        {
            writer.WriteU32(state.Flavor);
            writer.WriteU32((uint)state.Registers.Length);
            foreach (var register in state.Registers)
            {
                writer.WriteU32(register);
            }
        }
    }

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Type = {Type}, States = {States.Count}");
        return true;
    }
}

/// <summary>
/// One register state inside a <see cref="MachOThreadCommand"/>.
/// </summary>
public sealed class MachOThreadState
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="flavor">The architecture-specific flavour identifying the register layout.</param>
    /// <param name="registers">The register words.</param>
    /// <exception cref="ArgumentNullException"><paramref name="registers"/> is null.</exception>
    public MachOThreadState(uint flavor, uint[] registers)
    {
        ArgumentNullException.ThrowIfNull(registers);
        Flavor = flavor;
        Registers = registers;
    }

    /// <summary>
    /// Gets or sets the flavour, which says which register layout the words follow. The values
    /// are architecture-specific, so the same number means different things on x86 and ARM.
    /// </summary>
    public uint Flavor { get; set; }

    /// <summary>
    /// Gets or sets the register words, counted in 32-bit units even on a 64-bit architecture
    /// where each register spans two of them.
    /// </summary>
    public uint[] Registers { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"{nameof(MachOThreadState)} {{ Flavor = {Flavor}, Registers = {Registers.Length} }}";
}
