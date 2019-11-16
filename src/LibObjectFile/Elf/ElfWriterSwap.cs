using System.IO;

namespace LibObjectFile.Elf
{
    internal sealed class ElfWriterSwap : ElfWriter<ElfEncoderSwap>
    {
        public ElfWriterSwap(ElfObjectFile elfObjectFile, Stream stream) : base(elfObjectFile, stream)
        {
        }
    }
}