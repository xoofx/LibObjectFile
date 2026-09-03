// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using LibObjectFile.MachO;

namespace LibObjectFile.Tests.MachO;

[TestClass]
public class MachOEditingTests : MachOTestBase
{
    /// <summary>
    /// The reference fixture is the same input run through Apple's <c>install_name_tool</c>, so
    /// this pins the encoding against the tool the platform actually ships rather than against
    /// this library's own idea of what a runpath command looks like.
    /// </summary>
    [TestMethod]
    public void AddRPathMatchesInstallNameTool()
    {
        var file = LoadMachO("unixthread_i386");
        file.AddRPath("@executable_path/../Frameworks");

        var expected = File.ReadAllBytes(GetFile("unixthread_i386_rpath"));
        ByteArrayAssert.AreEqual(expected, WriteToArray(file), "Adding a runpath does not match install_name_tool");
    }

    /// <summary>
    /// The whole point of editing in place is that nothing moves. A section that shifted would
    /// invalidate every address and relocation already baked into the image.
    /// </summary>
    [TestMethod]
    public void AddingCommandsLeavesEverySectionWhereItWas()
    {
        var before = LoadMachO("unixthread_i386");
        var originalLength = new FileInfo(GetFile("unixthread_i386")).Length;
        var originalOffsets = before.Segments.SelectMany(s => s.Sections).Select(s => (s.Name, s.FileOffset)).ToArray();

        var file = LoadMachO("unixthread_i386");
        file.AddLoadDylib("@executable_path/../Frameworks/injected.dylib");
        file.AddRPath("@executable_path/../Frameworks");
        var written = WriteToArray(file);

        Assert.AreEqual(originalLength, written.Length, "the file changed size");

        var reread = MachOFile.Read(new MemoryStream(written));
        var newOffsets = reread.Segments.SelectMany(s => s.Sections).Select(s => (s.Name, s.FileOffset)).ToArray();
        CollectionAssert.AreEqual(originalOffsets, newOffsets, "a section moved");

        foreach (var segment in reread.Segments)
        {
            var original = before.FindSegment(segment.Name);
            Assert.IsNotNull(original);
            Assert.AreEqual(original.FileOffset, segment.FileOffset, $"segment {segment.Name} moved");
            Assert.AreEqual(original.VmAddress, segment.VmAddress, $"segment {segment.Name} was remapped");
        }
    }

    /// <summary>
    /// dyld refers to a library by its position in the command list, so a new dependency has to
    /// go last or every existing binding would point at the wrong library.
    /// </summary>
    [TestMethod]
    public void AddLoadDylibAppendsAfterTheExistingLibraries()
    {
        var file = LoadMachO("unixthread_i386");
        var before = file.LinkedLibraries.Select(d => d.Name).ToArray();

        file.AddLoadDylib("@rpath/injected.dylib");

        var after = file.LinkedLibraries.Select(d => d.Name).ToArray();
        CollectionAssert.AreEqual(before, after.Take(before.Length).ToArray(), "existing libraries were renumbered");
        Assert.AreEqual("@rpath/injected.dylib", after[^1]);
        Assert.AreSame(file.LoadCommands[^1], file.LinkedLibraries.Last());

        // A weak dependency and a runpath added alongside have to survive being written out.
        file.AddLoadDylib("@rpath/weak.dylib", MachOLoadCommandType.LoadWeakDylib);
        file.AddRPath("@loader_path/../lib");
        var reread = MachOFile.Read(new MemoryStream(WriteToArray(file)));
        Assert.IsTrue(reread.LinkedLibraries.Single(d => d.Name == "@rpath/weak.dylib").IsWeak);
        Assert.AreEqual("@loader_path/../lib", reread.RunPaths.Single().Path);
    }

    /// <summary>
    /// Once the padding runs out the only way to continue would be to move content, so the edit
    /// has to fail rather than silently invalidate the image.
    /// </summary>
    [TestMethod]
    public void AddingBeyondTheAvailableSpaceThrows()
    {
        var file = LoadMachO("unixthread_i386");
        var space = file.AvailableLoadCommandSpace;

        // Each of these costs 12 bytes of header plus the padded path.
        var path = new string('a', 100);
        while (file.AvailableLoadCommandSpace >= 120)
        {
            file.AddRPath("/" + path + file.LoadCommands.Count);
        }

        Assert.IsTrue(file.AvailableLoadCommandSpace < space);

        // Sized against the space actually left rather than the loop's threshold. A fixed path
        // could still fit whatever padding the fill happened to stop on.
        var tooLong = new string('a', (int)file.AvailableLoadCommandSpace + 16);
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => file.AddRPath("/" + tooLong));
        StringAssert.Contains(exception.Message, "free before the first section");
    }

    /// <summary>
    /// An edit that does not fit has to leave the image exactly as it was. Assigning the new name
    /// before checking there is room would leave the command renamed but not resized, describing
    /// a string longer than it has space for.
    /// </summary>
    [TestMethod]
    public void AnEditThatDoesNotFitChangesNothing()
    {
        var file = LoadMachO("unixthread_i386");

        // Use up the room so that any growth fails.
        while (file.AvailableLoadCommandSpace >= 64)
        {
            file.AddRPath("/" + new string('a', 40) + file.LoadCommands.Count);
        }

        var before = WriteToArray(file);
        var namesBefore = file.LinkedLibraries.Select(d => d.Name).ToArray();
        var sizesBefore = file.LinkedLibraries.Select(d => d.Size).ToArray();
        var space = file.AvailableLoadCommandSpace;

        var longName = "/" + new string('b', 200);
        Assert.ThrowsExactly<InvalidOperationException>(() => file.ChangeDylibName("/usr/lib/libSystem.B.dylib", longName));

        CollectionAssert.AreEqual(namesBefore, file.LinkedLibraries.Select(d => d.Name).ToArray(), "a name was changed by a failed edit");
        CollectionAssert.AreEqual(sizesBefore, file.LinkedLibraries.Select(d => d.Size).ToArray(), "a size was changed by a failed edit");
        Assert.AreEqual(space, file.AvailableLoadCommandSpace);
        ByteArrayAssert.AreEqual(before, WriteToArray(file), "a failed edit changed the image");
    }

    [TestMethod]
    public void ChangeDylibNameRepointsTheReference()
    {
        var file = LoadMachO("unixthread_i386");

        Assert.AreEqual(1, file.ChangeDylibName("/usr/lib/libstdc++.6.dylib", "@rpath/libstdc++.6.dylib"));
        Assert.AreEqual(0, file.ChangeDylibName("/nothing/here.dylib", "/other.dylib"));

        var reread = MachOFile.Read(new MemoryStream(WriteToArray(file)));
        CollectionAssert.Contains(reread.LinkedLibraries.Select(d => d.Name).ToArray(), "@rpath/libstdc++.6.dylib");
        CollectionAssert.DoesNotContain(reread.LinkedLibraries.Select(d => d.Name).ToArray(), "/usr/lib/libstdc++.6.dylib");

        // The same rewrite applied to a dylib's own name, which lives in a different command.
        var dylib = LoadMachO("libhelloworld_x86_64.dylib");
        Assert.AreEqual("/usr/local/lib/libhelloworld.dylib", dylib.IdDylib!.Name);
        dylib.SetInstallName("@rpath/libhelloworld.dylib");
        Assert.AreEqual("@rpath/libhelloworld.dylib", MachOFile.Read(new MemoryStream(WriteToArray(dylib))).IdDylib!.Name);

        // An executable has no install name to rewrite.
        Assert.ThrowsExactly<InvalidOperationException>(() => LoadMachO("unixthread_i386").SetInstallName("@rpath/whatever.dylib"));
    }

    [TestMethod]
    public void RemoveRPathDropsOnlyTheMatchingRunPath()
    {
        var file = LoadMachO("unixthread_i386");
        file.AddRPath("/one");
        file.AddRPath("/two");

        Assert.IsTrue(file.RemoveRPath("/one"));
        Assert.IsFalse(file.RemoveRPath("/one"));

        var reread = MachOFile.Read(new MemoryStream(WriteToArray(file)));
        CollectionAssert.AreEqual(new[] { "/two" }, reread.RunPaths.Select(r => r.Path).ToArray());
    }
}
