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
        var clock = Stopwatch.StartNew();
        //var memoryStream = new MemoryStream();
        foreach (var file in GetLinuxBins())
        {
            //memoryStream.SetLength(0);
            using var stream = File.OpenRead((string)file[0]);
            //stream.CopyTo(memoryStream);

            if (ElfFile.IsElf(stream))
            {
                ElfFile.Read(stream);
            }
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