// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.PE;

/// <summary>
/// Control Flow Guard related flags.
/// </summary>
[Flags]
public enum PEGuardFlags : uint
{
    /// <summary>
    /// Module performs control flow integrity checks using system-supplied support.
    /// </summary>
    Instrumented = 0x00000100,

    /// <summary>
    /// Module performs control flow and write integrity checks.
    /// </summary>
    CfwInstrumented = 0x00000200,

    /// <summary>
    /// Module contains valid control flow target metadata.
    /// </summary>
    FunctionTablePresent = 0x00000400,

    /// <summary>
    /// Module does not make use of the /GS security cookie.
    /// </summary>
    SecurityCookieUnused = 0x00000800,

    /// <summary>
    /// Module supports read-only delay load IAT.
    /// </summary>
    ProtectDelayLoadIAT = 0x00001000,

    /// <summary>
    /// Delayload import table in its own .didat section (with nothing else in it) that can be freely reprotected.
    /// </summary>
    DelayLoadIATInItsOwnSection = 0x00002000,

    /// <summary>
    /// Module contains suppressed export information. This also infers that the address taken
    /// IAT table is also present in the load config.
    /// </summary>
    CfExportSuppressionInfoPresent = 0x00004000,

    /// <summary>
    /// Module enables suppression of exports.
    /// </summary>
    CfEnableExportSuppression = 0x00008000,

    /// <summary>
    /// Module contains longjmp target information.
    /// </summary>
    LongJumpTablePresent = 0x00010000,

    /// <summary>
    /// Module contains return flow instrumentation and metadata.
    /// </summary>
    RfInstrumented = 0x00020000,

    /// <summary>
    /// Module requests that the OS enable return flow protection.
    /// </summary>
    RfEnable = 0x00040000,

    /// <summary>
    /// Module requests that the OS enable return flow protection in strict mode.
    /// </summary>
    RfStrict = 0x00080000,

    /// <summary>
    /// Module was built with retpoline support.
    /// </summary>
    RetpolinePresent = 0x00100000,

    // DO_NOT_USE (Was EHCont flag on VB (20H1))
    // DO NOT USE

    /// <summary>
    /// Module contains EH continuation target information.
    /// </summary>
    EhContinuationTablePresent = 0x00400000,

    /// <summary>
    /// Module was built with xfg.
    /// </summary>
    XfgEnabled = 0x00800000,

    /// <summary>
    /// Module has CastGuard instrumentation present.
    /// </summary>
    CastGuardPresent = 0x01000000,

    /// <summary>
    /// Module has Guarded Memcpy instrumentation present.
    /// </summary>
    MemcpyPresent = 0x02000000
}