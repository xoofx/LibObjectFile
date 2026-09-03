// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.MachO;

/// <summary>
/// The platform an image was built for, as recorded by <c>LC_BUILD_VERSION</c>.
/// </summary>
/// <remarks>
/// This replaced the per-platform <c>LC_VERSION_MIN_*</c> commands, which could not express
/// targets such as Catalyst or a simulator without inventing a command for each.
/// </remarks>
public enum MachOPlatform : uint
{
    /// <summary>No platform recorded.</summary>
    Unknown = 0,
    /// <summary>macOS (<c>PLATFORM_MACOS</c>).</summary>
    MacOS = 1,
    /// <summary>iOS (<c>PLATFORM_IOS</c>).</summary>
    IOS = 2,
    /// <summary>tvOS (<c>PLATFORM_TVOS</c>).</summary>
    TvOS = 3,
    /// <summary>watchOS (<c>PLATFORM_WATCHOS</c>).</summary>
    WatchOS = 4,
    /// <summary>The bridgeOS of a T2 coprocessor (<c>PLATFORM_BRIDGEOS</c>).</summary>
    BridgeOS = 5,
    /// <summary>An iOS app running on macOS (<c>PLATFORM_MACCATALYST</c>).</summary>
    MacCatalyst = 6,
    /// <summary>The iOS simulator (<c>PLATFORM_IOSSIMULATOR</c>).</summary>
    IOSSimulator = 7,
    /// <summary>The tvOS simulator (<c>PLATFORM_TVOSSIMULATOR</c>).</summary>
    TvOSSimulator = 8,
    /// <summary>The watchOS simulator (<c>PLATFORM_WATCHOSSIMULATOR</c>).</summary>
    WatchOSSimulator = 9,
    /// <summary>A driver extension (<c>PLATFORM_DRIVERKIT</c>).</summary>
    DriverKit = 10,
    /// <summary>visionOS (<c>PLATFORM_VISIONOS</c>).</summary>
    VisionOS = 11,
    /// <summary>The visionOS simulator (<c>PLATFORM_VISIONOSSIMULATOR</c>).</summary>
    VisionOSSimulator = 12,
}
