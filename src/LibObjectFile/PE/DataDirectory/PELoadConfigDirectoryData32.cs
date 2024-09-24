// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// A structure representing the Load Configuration Directory for a PE32 file.
/// </summary>
public struct PELoadConfigDirectoryData32
{
    internal uint SizeInternal;

    public uint Size => SizeInternal;

    public uint TimeDateStamp;
    public ushort MajorVersion;
    public ushort MinorVersion;
    public uint GlobalFlagsClear;
    public uint GlobalFlagsSet;
    public uint CriticalSectionDefaultTimeout;
    public uint DeCommitFreeBlockThreshold;
    public uint DeCommitTotalFreeThreshold;
    public VA32 LockPrefixTable;                // VA
    public uint MaximumAllocationSize;
    public uint VirtualMemoryThreshold;
    public uint ProcessHeapFlags;
    public uint ProcessAffinityMask;
    public ushort CSDVersion;
    public ushort DependentLoadFlags;
    public VA32 EditList;                       // VA
    public VA32 SecurityCookie;                 // VA
    public VA32 SEHandlerTable;                 // VA
    public uint SEHandlerCount;
    public VA32 GuardCFCheckFunctionPointer;    // VA
    public VA32 GuardCFDispatchFunctionPointer; // VA
    public VA32 GuardCFFunctionTable;           // VA
    public uint GuardCFFunctionCount;
    
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
    public VA32 GuardAddressTakenIatEntryTable; // VA
    public uint GuardAddressTakenIatEntryCount;
    public VA32 GuardLongJumpTargetTable;       // VA
    public uint GuardLongJumpTargetCount;
    public VA32 DynamicValueRelocTable;         // VA
    public uint CHPEMetadataPointer;
    public VA32 GuardRFFailureRoutine;          // VA
    public VA32 GuardRFFailureRoutineFunctionPointer; // VA
    public uint DynamicValueRelocTableOffset;
    public ushort DynamicValueRelocTableSection;
    public ushort Reserved2;
    public VA32 GuardRFVerifyStackPointerFunctionPointer; // VA
    public uint HotPatchTableOffset;
    public uint Reserved3;
    public VA32 EnclaveConfigurationPointer;    // VA
    public VA32 VolatileMetadataPointer;        // VA
    public VA32 GuardEHContinuationTable;       // VA
    public uint GuardEHContinuationCount;
    public VA32 GuardXFGCheckFunctionPointer;   // VA
    public VA32 GuardXFGDispatchFunctionPointer; // VA
    public VA32 GuardXFGTableDispatchFunctionPointer; // VA
    public VA32 CastGuardOsDeterminedFailureMode; // VA
    public VA32 GuardMemcpyFunctionPointer;     // VA
}