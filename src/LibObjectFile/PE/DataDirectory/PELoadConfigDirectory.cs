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
    /// Change the recorded size of the Load Configuration Directory.
    /// </summary>
    /// <param name="size"></param>
    public void SetConfigSize(uint size)
    {
        SetRawDataSize(size);

        // Write back the configuration size
        MemoryMarshal.Write(RawData, size);
    }
}