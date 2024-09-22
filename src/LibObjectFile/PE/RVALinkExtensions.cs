// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

public static class RVALinkExtensions
{
    public static bool IsNull<TRVALink>(this TRVALink link) where TRVALink : RVALink => link.Container is null;

    public static RVA RVA<TRVALink>(this TRVALink link) where TRVALink : RVALink => link.Container is not null ? link.Container.RVA + link.RVO : 0;

    public static string ToDisplayText<TRVALink>(this TRVALink link) where TRVALink : RVALink => link.Container is not null ? $"{link.Container}, Offset = {link.RVO}" : $"<empty>";
}