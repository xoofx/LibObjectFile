// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE.Internal;

#pragma warning disable CS0649
internal struct RawImageResourceDirectoryEntry
{
    public uint NameOrId;
    public uint OffsetToDataOrDirectoryEntry;
}

internal struct RawImageResourceDataEntry
{
    public RVA OffsetToData;
    public uint Size;
    public uint CodePage;
    public uint Reserved;
}

internal struct RawImageResourceDirectory
{
    public uint Characteristics;
    public uint TimeDateStamp;
    public ushort MajorVersion;
    public ushort MinorVersion;
    public ushort NumberOfNamedEntries;
    public ushort NumberOfIdEntries;
    //  IMAGE_RESOURCE_DIRECTORY_ENTRY DirectoryEntries[];
}
