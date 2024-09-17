// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE.Internal;

internal struct RawImportDirectoryEntry
{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

    /// <summary>
    /// The RVA of the import lookup table. This table contains a name or ordinal for each import. (The name "Characteristics" is used in Winnt.h, but no longer describes this field.)
    /// </summary>
    public RVA ImportLookupTableRVA;

    /// <summary>
    /// The stamp that is set to zero until the image is bound. After the image is bound, this field is set to the time/data stamp of the DLL.
    ///
    /// 0 if not bound,
    /// -1 if bound, and real date\time stamp in IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT (new BIND)
    /// O.W. date/time stamp of DLL bound to (Old BIND)
    /// </summary>
    public uint TimeDateStamp;

    /// <summary>
    /// The index of the first forwarder reference. -1 if no forwarders
    /// </summary>
    public uint ForwarderChain;

    /// <summary>
    /// The address of an ASCII string that contains the name of the DLL. This address is relative to the image base.
    /// </summary>
    public RVA NameRVA;

    /// <summary>
    /// The RVA of the import address table. The contents of this table are identical to the contents of the import lookup table until the image is bound.
    /// </summary>
    public RVA ImportAddressTableRVA;
}