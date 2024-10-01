// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE.Internal;

#pragma warning disable CS0649
/// <summary>
/// Represents a delay load descriptor in a Portable Executable (PE) image.
/// </summary>
internal struct RawDelayLoadDescriptor
{
    /// <summary>
    /// The attributes that must be zero.
    /// </summary>
    public uint Attributes;

    /// <summary>
    /// The RVA of the name of the DLL to delay load.
    /// </summary>
    public RVA NameRVA;

    /// <summary>
    /// The RVA to the HMODULE caching location (PHMODULE)
    /// </summary>
    public RVA ModuleHandleRVA;
    
    /// <summary>
    /// The RVA of the delay load import address table, which contains the RVAs of the delay load import name table and the delay load import module.
    /// </summary>
    public RVA DelayLoadImportAddressTableRVA;

    /// <summary>
    /// The RVA of the delay load import name table, which contains the names of the delay load imports.
    /// </summary>
    public RVA DelayLoadImportNameTableRVA;

    /// <summary>
    /// The RVA of the bound delay load import address table. This table contains the actual addresses to use for the delay load imports.
    /// </summary>
    public RVA BoundDelayLoadImportAddressTableRVA;

    /// <summary>
    /// The RVA of the unload delay load import address table.
    /// </summary>
    public RVA UnloadDelayLoadImportAddressTableRVA;

    /// <summary>
    /// The time/date stamp of the delay load import table.
    /// </summary>
    public uint TimeDateStamp;
}