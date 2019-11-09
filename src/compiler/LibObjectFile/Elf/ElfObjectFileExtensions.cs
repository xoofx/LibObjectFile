using System.IO;

namespace LibObjectFile.Elf
{
    public static class ElfObjectFileExtensions
    {
        public static void Write(this ElfObjectFile objectFile, Stream stream)
        {
            var elfWriter = ElfWriter.Create(objectFile, stream);
            elfWriter.Write();
        }
    }
}