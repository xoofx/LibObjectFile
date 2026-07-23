// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;

namespace LibObjectFile.Tests;

internal sealed class FlushTrackingStream : MemoryStream
{
    public bool WasFlushed { get; private set; }

    public override void Flush()
    {
        WasFlushed = true;
        base.Flush();
    }
}
