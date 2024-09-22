// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// A structure representing the Load Configuration Directory for a PE64 file.
/// </summary>
public struct PELoadConfigDirectory64
{
    public uint Size;
    public uint TimeDateStamp;
    public ushort MajorVersion;
    public ushort MinorVersion;
    public uint GlobalFlagsClear;
    public uint GlobalFlagsSet;
    public uint CriticalSectionDefaultTimeout;
    public ulong DeCommitFreeBlockThreshold;
    public ulong DeCommitTotalFreeThreshold;
    public VA64 LockPrefixTable;                // VA
    public ulong MaximumAllocationSize;
    public ulong VirtualMemoryThreshold;
    public ulong ProcessAffinityMask;
    public uint ProcessHeapFlags;
    public ushort CSDVersion;
    public ushort DependentLoadFlags;
    public VA64 EditList;                       // VA
    public VA64 SecurityCookie;                 // VA
    public VA64 SEHandlerTable;                 // VA
    public ulong SEHandlerCount;
    public VA64 GuardCFCheckFunctionPointer;    // VA
    public VA64 GuardCFDispatchFunctionPointer; // VA
    public VA64 GuardCFFunctionTable;           // VA
    public ulong GuardCFFunctionCount;

    private uint _rawGuardFlags;

    public PEGuardFlags GuardFlags
    {
        get => (PEGuardFlags)(_rawGuardFlags & ~0xF000_0000U);
        set => _rawGuardFlags = (_rawGuardFlags & 0xF000_0000U) | (uint)value;
    }

    public int TableSizeShift
    {
        get => (int)(_rawGuardFlags >>> 28);
        set => _rawGuardFlags = (_rawGuardFlags & 0x0FFF_FFFFU) | ((uint)value << 28);
    }

    public PELoadConfigCodeIntegrity CodeIntegrity;
    public VA64 GuardAddressTakenIatEntryTable; // VA
    public ulong GuardAddressTakenIatEntryCount;
    public VA64 GuardLongJumpTargetTable;       // VA
    public ulong GuardLongJumpTargetCount;
    public VA64 DynamicValueRelocTable;         // VA
    public VA64 CHPEMetadataPointer;            // VA
    public VA64 GuardRFFailureRoutine;          // VA
    public VA64 GuardRFFailureRoutineFunctionPointer; // VA
    public uint DynamicValueRelocTableOffset;
    public ushort DynamicValueRelocTableSection;
    public ushort Reserved2;
    public VA64 GuardRFVerifyStackPointerFunctionPointer; // VA
    public uint HotPatchTableOffset;
    public uint Reserved3;
    public VA64 EnclaveConfigurationPointer;    // VA
    public VA64 VolatileMetadataPointer;        // VA
    public VA64 GuardEHContinuationTable;       // VA
    public ulong GuardEHContinuationCount;
    public VA64 GuardXFGCheckFunctionPointer;   // VA
    public VA64 GuardXFGDispatchFunctionPointer; // VA
    public VA64 GuardXFGTableDispatchFunctionPointer; // VA
    public VA64 CastGuardOsDeterminedFailureMode; // VA
    public VA64 GuardMemcpyFunctionPointer;     // VA
}