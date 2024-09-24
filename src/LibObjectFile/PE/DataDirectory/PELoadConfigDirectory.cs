// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;
using System;
using System.Runtime.InteropServices;
using LibObjectFile.Utils;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the Load Configuration Directory for a PE file.
/// </summary>
public abstract class PELoadConfigDirectory : PERawDataDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PELoadConfigDirectory"/> class.
    /// </summary>
    private protected PELoadConfigDirectory(int minSize) : base(PEDataDirectoryKind.LoadConfig, minSize)
    {
    }

    /// <summary>
    /// Gets the config size.
    /// </summary>
    /// <remarks>
    /// Use <see cref="SetConfigSize"/> to set the config size.
    /// </remarks>
    public sealed override int RawDataSize => (int)MemoryMarshal.Read<uint>(RawData);
    
    /// <summary>
    /// Sets the config size.
    /// </summary>
    /// <param name="value">The new config size.</param>
    /// <remarks>
    /// The underlying buffer <see cref="RawData"/> will be resized if necessary.
    /// </remarks>
    public sealed override void SetRawDataSize(int value)
    {
        base.SetRawDataSize(value);

        // Write back the configuration size
        MemoryMarshal.Write(RawData, value);
    }
}