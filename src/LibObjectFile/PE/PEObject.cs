// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace LibObjectFile.PE;

[DebuggerDisplay("{ToString(),nq}")]
public abstract class PEObject : ObjectFileNode
{
    internal void ReadInternal(PEImageReader reader) => Read(reader);

    internal void WriteInternal(PEImageWriter writer) => Write(writer);


    protected abstract void Read(PEImageReader reader);

    protected abstract void Write(PEImageWriter writer);

    // TODO: move PrintName and PrintMembers to ObjectFileNode

    public sealed override string ToString()
    {
        var builder = new StringBuilder();
        PrintName(builder);
        builder.Append(" { ");
        if (PrintMembers(builder))
        {
            builder.Append(' ');
        }
        builder.Append('}');
        return builder.ToString();
    }

    protected virtual void PrintName(StringBuilder builder)
    {
        builder.Append(GetType().Name);
    }

    protected virtual bool PrintMembers(StringBuilder builder)
    {
        return false;
    }
}