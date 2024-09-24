// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.
namespace LibObjectFile.PE;

/// <summary>
/// Represents the Thread Local Storage (TLS) directory in a PE file.
/// </summary>
public abstract class PETlsDirectory : PERawDataDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PETlsDirectory"/> class.
    /// </summary>
    private protected PETlsDirectory(int minSize) : base(PEDataDirectoryKind.Tls, minSize)
    {
    }
}