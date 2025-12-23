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
        private Stream _stream;
        private bool _is32;

        public Stream Stream
        {
            get => _stream;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _stream = value;
                Size = (ulong)_stream.Length;
            }
        }

        public List<ElfDynamic> Entries { get; }

        public ElfDynamicLinkingTable() : base(ElfSectionType.DynamicLinking)
        {
            Name = ElfSectionSpecialType.Dynamic.GetDefaultName();
            Flags = ElfSectionSpecialType.Dynamic.GetSectionFlags();
            _stream = new MemoryStream();

            Entries = [];
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