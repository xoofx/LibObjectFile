using System.IO;

namespace LibObjectFile.Elf
{
    internal sealed class ElfReaderDirect : ElfReader<ElfDecoderDirect>
    {
        public ElfReaderDirect(ElfObjectFile elfObjectFile, Stream stream) : base(elfObjectFile, stream)
        {
        }
    }
}