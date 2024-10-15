// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;
using LibObjectFile.Elf;

namespace LibObjectFile.Bench;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Loading files into memory");
        var clock = Stopwatch.StartNew();
        var streams = new List<MemoryStream>();
        int biggestCapacity = 0;
        foreach (var file in GetLinuxBins())
        {
            using var stream = File.OpenRead((string)file[0]);
            if (ElfFile.IsElf(stream))
            {
                stream.Position = 0;
                var localStream = new MemoryStream((int)stream.Length);
                stream.CopyTo(localStream);
                localStream.Position = 0;
                streams.Add(localStream);
                if (localStream.Capacity > biggestCapacity)
                {
                    biggestCapacity = localStream.Capacity;
                }
            }
        }

        clock.Stop();
        Console.WriteLine($"End reading in {clock.Elapsed.TotalMilliseconds}ms");
        Console.ReadLine();

        Console.WriteLine("Processing");
        var memoryStream = new MemoryStream(biggestCapacity);
        clock.Restart();
        //SuperluminalPerf.Initialize();
        for (int i = 0; i < 10; i++)
        {
            //SuperluminalPerf.BeginEvent($"Round{i}");
            foreach (var stream in streams)
            {
                stream.Position = 0;
                var elf = ElfFile.Read(stream);
                memoryStream.SetLength(0);
                elf.Write(memoryStream);
            }
            //SuperluminalPerf.EndEvent();
        }
        clock.Stop();
        Console.WriteLine($"{clock.Elapsed.TotalMilliseconds}ms");
    }
    
    public static IEnumerable<object[]> GetLinuxBins()
    {
        var wslDirectory = @"\\wsl$\Ubuntu\usr\bin";
        if (OperatingSystem.IsLinux())
        {
            foreach (var file in Directory.EnumerateFiles(@"/usr/bin"))
            {
                yield return new object[] { file };
            }
        }
        else if (OperatingSystem.IsWindows() && Directory.Exists(wslDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(wslDirectory))
            {
                var fileInfo = new FileInfo(file);
                // Skip symbolic links as loading them will fail
                if ((fileInfo.Attributes & FileAttributes.ReparsePoint) == 0)
                {
                    yield return new object[] { file };
                }
            }
        }
        else
        {
            yield return new object[] { string.Empty };
        }
    }
}