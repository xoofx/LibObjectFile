// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the Load Configuration Directory for a PE32 file.
/// </summary>
public class PELoadConfigDirectory32 : PELoadConfigDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PELoadConfigDirectory32"/> class.
    /// </summary>
    public unsafe PELoadConfigDirectory32() : base(sizeof(PELoadConfigDirectoryData32))
    {
        SetConfigSize((uint)sizeof(PELoadConfigDirectoryData32));
    }

    public uint ConfigSize => Data.Size;

    public uint TimeDateStamp
    {
        get => Data.TimeDateStamp;
        set => Data.TimeDateStamp = value;
    }

    public ushort MajorVersion
    {
        get => Data.MajorVersion;
        set => Data.MajorVersion = value;
    }

    public ushort MinorVersion
    {
        get => Data.MinorVersion;
        set => Data.MinorVersion = value;
    }

    public uint GlobalFlagsClear
    {
        get => Data.GlobalFlagsClear;
        set => Data.GlobalFlagsClear = value;
    }

    public uint GlobalFlagsSet
    {
        get => Data.GlobalFlagsSet;
        set => Data.GlobalFlagsSet = value;
    }

    public uint CriticalSectionDefaultTimeout
    {
        get => Data.CriticalSectionDefaultTimeout;
        set => Data.CriticalSectionDefaultTimeout = value;
    }

    public uint DeCommitFreeBlockThreshold
    {
        get => Data.DeCommitFreeBlockThreshold;
        set => Data.DeCommitFreeBlockThreshold = value;
    }

    public uint DeCommitTotalFreeThreshold
    {
        get => Data.DeCommitTotalFreeThreshold;
        set => Data.DeCommitTotalFreeThreshold = value;
    }

    public VA32 LockPrefixTable
    {
        get => Data.LockPrefixTable;
        set => Data.LockPrefixTable = value;
    }

    public uint MaximumAllocationSize
    {
        get => Data.MaximumAllocationSize;
        set => Data.MaximumAllocationSize = value;
    }

    public uint VirtualMemoryThreshold
    {
        get => Data.VirtualMemoryThreshold;
        set => Data.VirtualMemoryThreshold = value;
    }

    public uint ProcessHeapFlags
    {
        get => Data.ProcessHeapFlags;
        set => Data.ProcessHeapFlags = value;
    }

    public uint ProcessAffinityMask
    {
        get => Data.ProcessAffinityMask;
        set => Data.ProcessAffinityMask = value;
    }

    public ushort CSDVersion
    {
        get => Data.CSDVersion;
        set => Data.CSDVersion = value;
    }

    public ushort DependentLoadFlags
    {
        get => Data.DependentLoadFlags;
        set => Data.DependentLoadFlags = value;
    }

    public VA32 EditList
    {
        get => Data.EditList;
        set => Data.EditList = value;
    }

    public VA32 SecurityCookie
    {
        get => Data.SecurityCookie;
        set => Data.SecurityCookie = value;
    }

    public VA32 SEHandlerTable
    {
        get => Data.SEHandlerTable;
        set => Data.SEHandlerTable = value;
    }

    public uint SEHandlerCount
    {
        get => Data.SEHandlerCount;
        set => Data.SEHandlerCount = value;
    }

    public VA32 GuardCFCheckFunctionPointer
    {
        get => Data.GuardCFCheckFunctionPointer;
        set => Data.GuardCFCheckFunctionPointer = value;
    }

    public VA32 GuardCFDispatchFunctionPointer
    {
        get => Data.GuardCFDispatchFunctionPointer;
        set => Data.GuardCFDispatchFunctionPointer = value;
    }

    public VA32 GuardCFFunctionTable
    {
        get => Data.GuardCFFunctionTable;
        set => Data.GuardCFFunctionTable = value;
    }

    public uint GuardCFFunctionCount
    {
        get => Data.GuardCFFunctionCount;
        set => Data.GuardCFFunctionCount = value;
    }

    public PEGuardFlags GuardFlags
    {
        get => Data.GuardFlags;
        set => Data.GuardFlags = value;
    }

    public int TableSizeShift
    {
        get => Data.TableSizeShift;
        set => Data.TableSizeShift = value;
    }

    public PELoadConfigCodeIntegrity CodeIntegrity
    {
        get => Data.CodeIntegrity;
        set => Data.CodeIntegrity = value;
    }

    public VA32 GuardAddressTakenIatEntryTable
    {
        get => Data.GuardAddressTakenIatEntryTable;
        set => Data.GuardAddressTakenIatEntryTable = value;
    }

    public uint GuardAddressTakenIatEntryCount
    {
        get => Data.GuardAddressTakenIatEntryCount;
        set => Data.GuardAddressTakenIatEntryCount = value;
    }

    public VA32 GuardLongJumpTargetTable
    {
        get => Data.GuardLongJumpTargetTable;
        set => Data.GuardLongJumpTargetTable = value;
    }

    public uint GuardLongJumpTargetCount
    {
        get => Data.GuardLongJumpTargetCount;
        set => Data.GuardLongJumpTargetCount = value;
    }

    public VA32 DynamicValueRelocTable
    {
        get => Data.DynamicValueRelocTable;
        set => Data.DynamicValueRelocTable = value;
    }

    public uint CHPEMetadataPointer
    {
        get => Data.CHPEMetadataPointer;
        set => Data.CHPEMetadataPointer = value;
    }

    public VA32 GuardRFFailureRoutine
    {
        get => Data.GuardRFFailureRoutine;
        set => Data.GuardRFFailureRoutine = value;
    }

    public VA32 GuardRFFailureRoutineFunctionPointer
    {
        get => Data.GuardRFFailureRoutineFunctionPointer;
        set => Data.GuardRFFailureRoutineFunctionPointer = value;
    }

    public uint DynamicValueRelocTableOffset
    {
        get => Data.DynamicValueRelocTableOffset;
        set => Data.DynamicValueRelocTableOffset = value;
    }

    public ushort DynamicValueRelocTableSection
    {
        get => Data.DynamicValueRelocTableSection;
        set => Data.DynamicValueRelocTableSection = value;
    }

    public ushort Reserved2
    {
        get => Data.Reserved2;
        set => Data.Reserved2 = value;
    }

    public VA32 GuardRFVerifyStackPointerFunctionPointer
    {
        get => Data.GuardRFVerifyStackPointerFunctionPointer;
        set => Data.GuardRFVerifyStackPointerFunctionPointer = value;
    }

    public uint HotPatchTableOffset
    {
        get => Data.HotPatchTableOffset;
        set => Data.HotPatchTableOffset = value;
    }

    public uint Reserved3
    {
        get => Data.Reserved3;
        set => Data.Reserved3 = value;
    }

    public VA32 EnclaveConfigurationPointer
    {
        get => Data.EnclaveConfigurationPointer;
        set => Data.EnclaveConfigurationPointer = value;
    }

    public VA32 VolatileMetadataPointer
    {
        get => Data.VolatileMetadataPointer;
        set => Data.VolatileMetadataPointer = value;
    }

    public VA32 GuardEHContinuationTable
    {
        get => Data.GuardEHContinuationTable;
        set => Data.GuardEHContinuationTable = value;
    }

    public uint GuardEHContinuationCount
    {
        get => Data.GuardEHContinuationCount;
        set => Data.GuardEHContinuationCount = value;
    }

    public VA32 GuardXFGCheckFunctionPointer
    {
        get => Data.GuardXFGCheckFunctionPointer;
        set => Data.GuardXFGCheckFunctionPointer = value;
    }

    public VA32 GuardXFGDispatchFunctionPointer
    {
        get => Data.GuardXFGDispatchFunctionPointer;
        set => Data.GuardXFGDispatchFunctionPointer = value;
    }

    public VA32 GuardXFGTableDispatchFunctionPointer
    {
        get => Data.GuardXFGTableDispatchFunctionPointer;
        set => Data.GuardXFGTableDispatchFunctionPointer = value;
    }

    public VA32 CastGuardOsDeterminedFailureMode
    {
        get => Data.CastGuardOsDeterminedFailureMode;
        set => Data.CastGuardOsDeterminedFailureMode = value;
    }

    public VA32 GuardMemcpyFunctionPointer
    {
        get => Data.GuardMemcpyFunctionPointer;
        set => Data.GuardMemcpyFunctionPointer = value;
    }

    /// <summary>
    /// Gets the 32-bit Load Configuration Directory.
    /// </summary>
    public ref PELoadConfigDirectoryData32 Data
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.As<byte, PELoadConfigDirectoryData32>(ref MemoryMarshal.GetArrayDataReference(RawData));
    }
}