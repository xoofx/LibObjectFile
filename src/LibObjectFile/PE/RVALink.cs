// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

// ReSharper disable once InconsistentNaming
public interface RVALink
{
    public PEObject? Container { get; }

    public RVO RVO { get; }
}

public interface RVALink<out TData> : RVALink
{
    public TData Resolve();
}