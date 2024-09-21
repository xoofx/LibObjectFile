// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

#pragma warning disable CS0649
public readonly struct PEExportFunctionEntry
{
    private readonly PEVirtualObject? _container;
    private readonly uint _offset;
    private readonly bool _isForwarderRVA;

    public PEExportFunctionEntry(PEFunctionAddressLink exportRVA)
    {
        _container = exportRVA.Container;
        _offset = exportRVA.Offset;
        _isForwarderRVA = false;
    }

    public PEExportFunctionEntry(PEAsciiStringLink forwarderRVA)
    {
        _container = forwarderRVA.StreamSectionData;
        _offset = forwarderRVA.Offset;
        _isForwarderRVA = true;
    }

    public bool IsForwarderRVA => _isForwarderRVA;
    
    public PEFunctionAddressLink ExportRVA => IsForwarderRVA ? default : new(_container, _offset);

    public PEAsciiStringLink ForwarderRVA => IsForwarderRVA ? new(_container as PEStreamSectionData, _offset) : default;

    public override string ToString() => ForwarderRVA.IsNull() ? $"{ExportRVA}" : $"{ExportRVA}, ForwarderRVA = {ForwarderRVA}";
}