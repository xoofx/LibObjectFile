// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text;

namespace LibObjectFile.Tests;

/// <summary>
/// Helper class to assert two byte arrays are equal.
/// </summary>
public static class ByteArrayAssert
{
    public static void AreEqual(byte[] expected, byte[] actual, string? message = null)
    {
        if (expected.Length != actual.Length)
        {
            throw new AssertFailedException($"The expected array `{expected.Length}` is not equal to the actual array `{actual.Length}`. {message}");
        }

        for (int i = 0; i < expected.Length; i++)
        {
            if (expected[i] != actual[i])
            {
                StringBuilder builder = new();

                var minLength = Math.Min(expected.Length, actual.Length);

                const int maxLines = 5;

                int j = Math.Max(i - maxLines, 0);

                var minText = $"[{j} / 0x{j:X}]".Length;

                builder.AppendLine("```");
                builder.AppendLine($"{new(' ',minText - 2)} Expected == Actual");
                
                for (; j < Math.Min(minLength, i + maxLines + 1); j++)
                {
                    if (expected[j] != actual[j])
                    {
                        builder.AppendLine($"[{j} / 0x{j:X}] 0x{expected[j]:X2}   != 0x{actual[j]:X2} <---- Actual value is not matching expecting");
                    }
                    else
                    {
                        builder.AppendLine($"[{j} / 0x{j:X}] 0x{expected[j]:X2}");
                    }
                }
                builder.AppendLine("```");

                throw new AssertFailedException($"The expected array is not equal to the actual array at index {i}. {message}{Environment.NewLine}{Environment.NewLine}{builder}");
            }
        }
    }
}