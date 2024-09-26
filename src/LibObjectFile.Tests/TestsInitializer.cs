using System.Globalization;
using System.Runtime.CompilerServices;
using VerifyMSTest;
using VerifyTests;
using VerifyTests.DiffPlex;

namespace LibObjectFile.Tests;

internal static class TestsInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyDiffPlex.Initialize(OutputType.Compact);
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Verifier.UseProjectRelativeDirectory("Verified");
        DiffEngine.DiffRunner.Disabled = true;
        VerifierSettings.DontScrubSolutionDirectory();
    }
}