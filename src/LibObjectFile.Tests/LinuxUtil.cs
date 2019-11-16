// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace LibObjectFile.Tests
{
    public static class LinuxUtil
    {
        public static string ReadElf(string file, string arguments = "-W -a")
        {
            return RunLinuxExe("readelf", $"{file} -W -a");
        }

        public static string RunLinuxExe(string exe, string arguments, string distribution = "Ubuntu-18.04")
        {
            if (exe == null) throw new ArgumentNullException(nameof(exe));
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));
            if (distribution == null) throw new ArgumentNullException(nameof(distribution));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                arguments = $"-d {distribution} {exe} {arguments}";
                exe = "wsl.exe";
            }

            StringBuilder errorBuilder = null;

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo(exe, arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                },
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (errorBuilder == null)
                {
                    errorBuilder = new StringBuilder();
                }
                errorBuilder.Append(args.Data);
            };

            process.Start();
            process.BeginErrorReadLine();
            var result = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Error while running command `{exe} {arguments}`: {errorBuilder}");
            }
            return result;
        }
    }
}