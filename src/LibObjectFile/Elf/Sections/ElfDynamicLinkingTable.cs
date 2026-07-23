// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibObjectFile.Diagnostics;
using LibObjectFile.IO;

namespace LibObjectFile.Elf
{
    public class ElfDynamicLinkingTable : ElfSection
    {
        private bool _is32;

        public List<ElfDynamic> Entries { get; }

        public ElfDynamicLinkingTable() : base(ElfSectionType.DynamicLinking)
        {
            Name = ElfSectionSpecialType.Dynamic.GetDefaultName();
            Flags = ElfSectionSpecialType.Dynamic.GetSectionFlags();

            Entries = [];
        }

        /// <summary>
        /// Gets the dynamic string table (<c>.dynstr</c>) referenced by <see cref="ElfSection.Link"/>,
        /// or <c>null</c> when the link does not point to an <see cref="ElfStringTable"/>. Tags such as
        /// <see cref="ElfDynamicTag.Needed"/> store an offset into this table rather than the string itself.
        /// </summary>
        public ElfStringTable? StringTable => Link.Section as ElfStringTable;

        /// <summary>
        /// Returns <c>true</c> when the value of an entry with the given <paramref name="tag"/> is an offset
        /// into the linked string table (<see cref="StringTable"/>) rather than an address, size, or flag set.
        /// </summary>
        public static bool IsStringValueTag(ElfDynamicTag tag) => tag switch
        {
            ElfDynamicTag.Needed
                or ElfDynamicTag.SoName
                or ElfDynamicTag.RPath
                or ElfDynamicTag.RunPath
                or ElfDynamicTag.Config
                or ElfDynamicTag.DepAudit
                or ElfDynamicTag.Audit => true,
            _ => false,
        };

        /// <summary>
        /// Resolves the string referenced by a string-valued entry (e.g. <see cref="ElfDynamicTag.Needed"/>)
        /// through the linked string table. Returns <c>false</c> when the entry is not string-valued or the
        /// linked string table is missing.
        /// </summary>
        public bool TryGetString(ElfDynamic entry, out string value)
        {
            if (IsStringValueTag(entry.TagType) && StringTable is { } stringTable)
            {
                return stringTable.TryGetString((uint)entry.Value, out value);
            }

            value = string.Empty;
            return false;
        }

        /// <summary>
        /// Enumerates the shared library names referenced by <see cref="ElfDynamicTag.Needed"/> entries,
        /// resolved through the linked string table. Yields nothing when the string table is missing.
        /// </summary>
        public IEnumerable<string> GetNeededLibraries()
        {
            if (StringTable is not { } stringTable)
            {
                yield break;
            }

            foreach (var entry in Entries)
            {
                if (entry.TagType == ElfDynamicTag.Needed && stringTable.TryGetString((uint)entry.Value, out var name))
                {
                    yield return name;
                }
            }
        }

        /// <summary>
        /// Appends a <see cref="ElfDynamicTag.Needed"/> entry for <paramref name="libraryName"/>, adding the
        /// name to the linked string table. The entry is inserted before any trailing
        /// <see cref="ElfDynamicTag.Null"/> terminator so the table stays valid.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="ElfSection.Link"/> is not an <see cref="ElfStringTable"/>.</exception>
        public void AddNeededLibrary(string libraryName)
        {
            ArgumentNullException.ThrowIfNull(libraryName);
            var stringTable = StringTable ?? throw new InvalidOperationException($"The {nameof(Link)} of this dynamic section must be set to an {nameof(ElfStringTable)} before adding a needed library");

            var offset = stringTable.GetOrCreateString(libraryName);

            int insertIndex = Entries.Count;
            while (insertIndex > 0 && Entries[insertIndex - 1].TagType == ElfDynamicTag.Null)
            {
                insertIndex--;
            }

            Entries.Insert(insertIndex, new ElfDynamic { Tag = (long)ElfDynamicTag.Needed, Value = offset });
        }

        /// <summary>
        /// Serializes <see cref="Entries"/> as raw <c>Elf32_Dyn</c>/<c>Elf64_Dyn</c> records to <paramref name="stream"/>,
        /// using the file class and byte order of the parent <see cref="ElfFile"/>. Standalone counterpart to the
        /// full-file write path, mirroring <see cref="ElfFile.Write(Stream)"/> at the section level.
        /// </summary>
        /// <exception cref="InvalidOperationException">The section is not attached to an <see cref="ElfFile"/>.</exception>
        public void Write(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            var elf = Parent ?? throw new InvalidOperationException($"The {nameof(ElfDynamicLinkingTable)} must be attached to an {nameof(ElfFile)} to determine the file class and byte order");

            var writer = ElfWriter.Create(elf, stream);
            Write(writer);
        }

        /// <summary>
        /// Replaces <see cref="Entries"/> by reading raw <c>Elf32_Dyn</c>/<c>Elf64_Dyn</c> records from the whole of
        /// <paramref name="stream"/>, using the file class and byte order of the parent <see cref="ElfFile"/>.
        /// Standalone counterpart to the full-file read path, mirroring <see cref="ElfFile.Read(Stream, ElfReaderOptions)"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The section is not attached to an <see cref="ElfFile"/>.</exception>
        public void Read(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            var elf = Parent ?? throw new InvalidOperationException($"The {nameof(ElfDynamicLinkingTable)} must be attached to an {nameof(ElfFile)} to determine the file class and byte order");

            var reader = ElfReader.Create(elf, stream, new ElfReaderOptions());
            reader.Position = 0;

            var entrySize = _is32 ? Unsafe.SizeOf<ElfNative.Elf32_Dyn>() : Unsafe.SizeOf<ElfNative.Elf64_Dyn>();
            var numberOfEntries = (int)((ulong)stream.Length / (ulong)entrySize);

            Entries.Clear();
            CollectionsMarshal.SetCount(Entries, numberOfEntries);

            if (_is32)
                Read32(reader, numberOfEntries);
            else
                Read64(reader, numberOfEntries);
        }

        protected override void UpdateLayoutCore(ElfVisitorContext context)
        {
            base.UpdateLayoutCore(context);

            var numberOfEntries = Entries.Count;

            base.Size = (ulong)numberOfEntries * base.TableEntrySize;
        }

        public override void Read(ElfReader reader)
        {
            reader.Position = Position;
            Entries.Clear();

            var numberOfEntries = (int)(base.Size / base.TableEntrySize);
            var entries = Entries;
            CollectionsMarshal.SetCount(entries, numberOfEntries);

            if (_is32)
                Read32(reader, numberOfEntries);
            else
                Read64(reader, numberOfEntries);
        }

        private void Read32(ElfReader reader, int numberOfEntries)
        {
            using var batch = new BatchDataReader<ElfNative.Elf32_Dyn>(reader.Stream, numberOfEntries);
            var span = CollectionsMarshal.AsSpan(Entries);
            ref var entry = ref MemoryMarshal.GetReference(span);
            while (batch.HasNext())
            {
                ref var dyn = ref batch.Read();

                entry.Tag = reader.Decode(dyn.d_tag);
                entry.Value = reader.Decode(dyn.d_un.d_val);

                entry = ref Unsafe.Add(ref entry, 1);
            }
        }

        private void Read64(ElfReader reader, int numberOfEntries)
        {
            using var batch = new BatchDataReader<ElfNative.Elf64_Dyn>(reader.Stream, numberOfEntries);
            var span = CollectionsMarshal.AsSpan(Entries);
            ref var entry = ref MemoryMarshal.GetReference(span);
            while (batch.HasNext())
            {
                ref var dyn = ref batch.Read();

                entry.Tag = reader.Decode(dyn.d_tag);
                entry.Value = reader.Decode(dyn.d_un.d_val);

                entry = ref Unsafe.Add(ref entry, 1);
            }
        }

        public override void Write(ElfWriter writer)
        {
            if (_is32)
                Write32(writer);
            else
                Write64(writer);
        }

        private void Write32(ElfWriter writer)
        {
            var entries = CollectionsMarshal.AsSpan(Entries);
            using var batch = new BatchDataWriter<ElfNative.Elf32_Dyn>(writer.Stream, entries.Length);
            var dyn = new ElfNative.Elf32_Dyn();
            for (int i = 0; i < entries.Length; i++)
            {
                ref var entry = ref entries[i];

                writer.Encode(out dyn.d_tag, (int)entry.Tag);
                writer.Encode(out dyn.d_un.d_val, (uint)entry.Value);

                batch.Write(dyn);
            }
        }

        private void Write64(ElfWriter writer)
        {
            var entries = CollectionsMarshal.AsSpan(Entries);
            using var batch = new BatchDataWriter<ElfNative.Elf64_Dyn>(writer.Stream, entries.Length);
            var dyn = new ElfNative.Elf64_Dyn();
            for (int i = 0; i < entries.Length; i++)
            {
                ref var entry = ref entries[i];

                writer.Encode(out dyn.d_tag, entry.Tag);
                writer.Encode(out dyn.d_un.d_val, entry.Value);

                batch.Write(dyn);
            }
        }

        protected override unsafe void ValidateParent(ObjectElement parent)
        {
            base.ValidateParent(parent);

            var elf = (ElfFile)parent;
            _is32 = elf.FileClass == ElfFileClass.Is32;

            BaseTableEntrySize = (uint)(_is32 ? sizeof(ElfNative.Elf32_Dyn) : sizeof(ElfNative.Elf64_Dyn));
            AdditionalTableEntrySize = 0;
        }

        internal override unsafe void InitializeEntrySizeFromRead(DiagnosticBag diagnostics, ulong entrySize, bool is32)
        {
            _is32 = is32;

            if (is32)
            {
                if (entrySize != (ulong)sizeof(ElfNative.Elf32_Dyn))
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionEntrySize, $"Invalid size [{entrySize}] for dynamic entry. Expecting to be equal to [{sizeof(ElfNative.Elf32_Dyn)}] bytes.");
                else
                {
                    BaseTableEntrySize = (uint)sizeof(ElfNative.Elf32_Dyn);
                    AdditionalTableEntrySize = (uint)(entrySize - AdditionalTableEntrySize);
                }
            }
            else
            {
                if (entrySize != (ulong)sizeof(ElfNative.Elf64_Dyn))
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionEntrySize, $"Invalid size [{entrySize}] for dynamic entry. Expecting to be equal to [{sizeof(ElfNative.Elf64_Dyn)}] bytes.");
                else
                {
                    BaseTableEntrySize = (uint)sizeof(ElfNative.Elf64_Dyn);
                    AdditionalTableEntrySize = (uint)(entrySize - AdditionalTableEntrySize);
                }
            }
        }
    }
}