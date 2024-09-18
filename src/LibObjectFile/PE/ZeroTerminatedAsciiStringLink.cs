// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

public record struct ZeroTerminatedAsciiStringLink(RVALink<PESectionData> Link)
{
    public bool IsNull => Link.IsNull;

    public string? ToText() => Link.Element?.ReadZeroTerminatedAsciiString(Link.OffsetInElement);
}