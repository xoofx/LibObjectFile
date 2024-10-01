// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

public class PEImageWriterOptions
{
    public static readonly PEImageWriterOptions Default = new();
    
    public bool EnableStackTrace { get; init; }

    public bool EnableChecksum { get; init; }
}