// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

#pragma warning disable CS0649
public readonly struct PEExportFunctionEntry
{
    private readonly PEObject? _container;
    private readonly uint _offset;
    private readonly bool _isForwarderRVA;

    public PEExportFunctionEntry(PEFunctionAddressLink exportRVA)
    {
        _container = exportRVA.Container;
        _offset = exportRVA.RVO;
        _isForwarderRVA = false;
    }

    public PEExportFunctionEntry(PEAsciiStringLink forwarderRVA)
    {
        _container = forwarderRVA.Container;
        _offset = forwarderRVA.RVO;
        _isForwarderRVA = true;
    }

    public bool IsForwarderRVA => _isForwarderRVA;

    public bool IsEmpty => _container is null && _offset == 0;

    public PEFunctionAddressLink ExportRVA => IsForwarderRVA ? default : new(_container, _offset);

    public PEAsciiStringLink ForwarderRVA => IsForwarderRVA ? new(_container as PEStreamSectionData, _offset) : default;

    public override string ToString() => IsEmpty ? "null" : ForwarderRVA.IsNull() ? $"{ExportRVA}" : $"{ExportRVA}, ForwarderRVA = {ForwarderRVA}";


    internal void Verify(PEVerifyContext context, PEExportAddressTable parent, int index)
    {
        context.VerifyObject(_container, parent, $"the object pointed by the {nameof(PEExportFunctionEntry)} at #{index}", false);
    }
}