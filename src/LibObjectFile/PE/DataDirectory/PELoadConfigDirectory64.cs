// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the Load Configuration Directory for a PE64 file.
/// </summary>
public class PELoadConfigDirectory64 : PELoadConfigDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PELoadConfigDirectory64"/> class.
    /// </summary>
    public unsafe PELoadConfigDirectory64() : base(sizeof(PELoadConfigDirectoryData64))
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        SetRawDataSize(sizeof(PELoadConfigDirectoryData64));
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

    public ulong DeCommitFreeBlockThreshold
    {
        get => Data.DeCommitFreeBlockThreshold;
        set => Data.DeCommitFreeBlockThreshold = value;
    }

    public ulong DeCommitTotalFreeThreshold
    {
        get => Data.DeCommitTotalFreeThreshold;
        set => Data.DeCommitTotalFreeThreshold = value;
    }

    public VA64 LockPrefixTable
    {
        get => Data.LockPrefixTable;
        set => Data.LockPrefixTable = value;
    }

    public ulong MaximumAllocationSize
    {
        get => Data.MaximumAllocationSize;
        set => Data.MaximumAllocationSize = value;
    }

    public ulong VirtualMemoryThreshold
    {
        get => Data.VirtualMemoryThreshold;
        set => Data.VirtualMemoryThreshold = value;
    }

    public ulong ProcessAffinityMask
    {
        get => Data.ProcessAffinityMask;
        set => Data.ProcessAffinityMask = value;
    }

    public uint ProcessHeapFlags
    {
        get => Data.ProcessHeapFlags;
        set => Data.ProcessHeapFlags = value;
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

    public VA64 EditList
    {
        get => Data.EditList;
        set => Data.EditList = value;
    }

    public VA64 SecurityCookie
    {
        get => Data.SecurityCookie;
        set => Data.SecurityCookie = value;
    }

    public VA64 SEHandlerTable
    {
        get => Data.SEHandlerTable;
        set => Data.SEHandlerTable = value;
    }

    public ulong SEHandlerCount
    {
        get => Data.SEHandlerCount;
        set => Data.SEHandlerCount = value;
    }

    public VA64 GuardCFCheckFunctionPointer
    {
        get => Data.GuardCFCheckFunctionPointer;
        set => Data.GuardCFCheckFunctionPointer = value;
    }

    public VA64 GuardCFDispatchFunctionPointer
    {
        get => Data.GuardCFDispatchFunctionPointer;
        set => Data.GuardCFDispatchFunctionPointer = value;
    }

    public VA64 GuardCFFunctionTable
    {
        get => Data.GuardCFFunctionTable;
        set => Data.GuardCFFunctionTable = value;
    }

    public ulong GuardCFFunctionCount
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

    public VA64 GuardAddressTakenIatEntryTable
    {
        get => Data.GuardAddressTakenIatEntryTable;
        set => Data.GuardAddressTakenIatEntryTable = value;
    }

    public ulong GuardAddressTakenIatEntryCount
    {
        get => Data.GuardAddressTakenIatEntryCount;
        set => Data.GuardAddressTakenIatEntryCount = value;
    }

    public VA64 GuardLongJumpTargetTable
    {
        get => Data.GuardLongJumpTargetTable;
        set => Data.GuardLongJumpTargetTable = value;
    }

    public ulong GuardLongJumpTargetCount
    {
        get => Data.GuardLongJumpTargetCount;
        set => Data.GuardLongJumpTargetCount = value;
    }

    public VA64 DynamicValueRelocTable
    {
        get => Data.DynamicValueRelocTable;
        set => Data.DynamicValueRelocTable = value;
    }

    public VA64 CHPEMetadataPointer
    {
        get => Data.CHPEMetadataPointer;
        set => Data.CHPEMetadataPointer = value;
    }

    public VA64 GuardRFFailureRoutine
    {
        get => Data.GuardRFFailureRoutine;
        set => Data.GuardRFFailureRoutine = value;
    }

    public VA64 GuardRFFailureRoutineFunctionPointer
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

    public VA64 GuardRFVerifyStackPointerFunctionPointer
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

    public VA64 EnclaveConfigurationPointer
    {
        get => Data.EnclaveConfigurationPointer;
        set => Data.EnclaveConfigurationPointer = value;
    }

    public VA64 VolatileMetadataPointer
    {
        get => Data.VolatileMetadataPointer;
        set => Data.VolatileMetadataPointer = value;
    }

    public VA64 GuardEHContinuationTable
    {
        get => Data.GuardEHContinuationTable;
        set => Data.GuardEHContinuationTable = value;
    }

    public ulong GuardEHContinuationCount
    {
        get => Data.GuardEHContinuationCount;
        set => Data.GuardEHContinuationCount = value;
    }

    public VA64 GuardXFGCheckFunctionPointer
    {
        get => Data.GuardXFGCheckFunctionPointer;
        set => Data.GuardXFGCheckFunctionPointer = value;
    }

    public VA64 GuardXFGDispatchFunctionPointer
    {
        get => Data.GuardXFGDispatchFunctionPointer;
        set => Data.GuardXFGDispatchFunctionPointer = value;
    }

    public VA64 GuardXFGTableDispatchFunctionPointer
    {
        get => Data.GuardXFGTableDispatchFunctionPointer;
        set => Data.GuardXFGTableDispatchFunctionPointer = value;
    }

    public VA64 CastGuardOsDeterminedFailureMode
    {
        get => Data.CastGuardOsDeterminedFailureMode;
        set => Data.CastGuardOsDeterminedFailureMode = value;
    }

    public VA64 GuardMemcpyFunctionPointer
    {
        get => Data.GuardMemcpyFunctionPointer;
        set => Data.GuardMemcpyFunctionPointer = value;
    }

    /// <summary>
    /// Gets the 64-bit Load Configuration Directory.
    /// </summary>
    public ref PELoadConfigDirectoryData64 Data
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.As<byte, PELoadConfigDirectoryData64>(ref MemoryMarshal.GetArrayDataReference(RawData));
    }
}