// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using LibObjectFile.IO;

namespace LibObjectFile.Tests.IO;

/// <summary>
/// Tests for <see cref="BatchDataReader{T}"/> and <see cref="BatchDataWriter{T}"/>.
/// </summary>
[TestClass]
public class TestBatchDataReaderWriter
{
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(100)]
    [DataRow(1000)]
    [DataRow(1024)]
    [DataRow(1025)]
    public void TestRead(int count)
    {
        var stream = new MemoryStream();
        stream.Write(MemoryMarshal.AsBytes(Enumerable.Range(0, count).ToArray().AsSpan()));
        stream.Position = 0;

        using var reader = new BatchDataReader<int>(stream, count);
        int i = 0;
        while (reader.HasNext())
        {
            Assert.AreEqual(i, reader.Read(), $"Invalid value at index {i}");
            i++;
        }
        Assert.AreEqual(count, i);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(100)]
    [DataRow(1000)]
    [DataRow(1024)]
    [DataRow(1025)]
    public void TestWrite(int count)
    {
        var stream = new MemoryStream();
        int i = 0;
        {
            using var writer = new BatchDataWriter<int>(stream, count);
            {
                while (writer.HasNext())
                {
                    writer.Write(i);
                    i++;
                }
            }
        }
        Assert.AreEqual(count * sizeof(int), stream.Length);

        stream.Position = 0;
        using var reader = new BatchDataReader<int>(stream, count);
        i = 0;
        while (reader.HasNext())
        {
            Assert.AreEqual(i, reader.Read(), $"Invalid value at index {i}");
            i++;
        }
        Assert.AreEqual(count, i);
    }
}