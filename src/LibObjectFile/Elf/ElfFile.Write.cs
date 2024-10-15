// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.Elf;

partial class ElfFile
{
    public override void Write(ElfWriter writer)
    {
        writer.Position = 0;
        var contentList = Content.UnsafeList;

        // We write the content all sections including shadows
        for (var i = 0; i < contentList.Count; i++)
        {
            var content = contentList[i];
            if (content is ElfSection section && section.Type == ElfSectionType.NoBits)
            {
                continue;
            }

            if (content.Position > writer.Position)
            {
                writer.WriteZero((int)(content.Position - writer.Position));
            }
            content.Write(writer);
        }

        // Write trailing zeros
        if (writer.Position < Layout.TotalSize)
        {
            writer.WriteZero((int)(Layout.TotalSize - writer.Position));
        }
    }

}