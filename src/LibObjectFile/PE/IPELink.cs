// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

// ReSharper disable once InconsistentNaming


public interface IPELink;

public interface IPELink<out TObject> : IPELink where TObject: PEObjectBase
{
    public TObject? Container { get; }

    public RVO RVO { get; }
}


public interface IPELink<out TObject, out TData> : IPELink<TObject> where TObject : PEObjectBase
{
    public TData Resolve();
}