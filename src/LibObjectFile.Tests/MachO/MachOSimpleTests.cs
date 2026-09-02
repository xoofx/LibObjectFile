// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.MachO.Internal;

namespace LibObjectFile.Tests.MachO;

[TestClass]
public class MachOSimpleTests
{
    /// <summary>
    /// The raw structures are copied by value straight out of the file, so a wrong size or an
    /// unexpected padding byte would silently shift every field after it.
    /// </summary>
    [TestMethod]
    public unsafe void RawStructuresMatchTheOnDiskLayout()
    {
        Assert.AreEqual(28, sizeof(RawMachHeader32));
        Assert.AreEqual(32, sizeof(RawMachHeader64));
        Assert.AreEqual(8, sizeof(RawLoadCommand));
        Assert.AreEqual(56, sizeof(RawSegmentCommand32));
        Assert.AreEqual(72, sizeof(RawSegmentCommand64));
        Assert.AreEqual(68, sizeof(RawSection32));
        Assert.AreEqual(80, sizeof(RawSection64));
        Assert.AreEqual(8, sizeof(RawFatHeader));
        Assert.AreEqual(20, sizeof(RawFatArch));
        Assert.AreEqual(32, sizeof(RawFatArch64));
    }
}
