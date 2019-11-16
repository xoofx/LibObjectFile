using System.IO;

namespace LibObjectFile.Elf
{
    internal sealed class ElfWriterDirect : ElfWriter<ElfEncoderDirect>
    {
        public ElfWriterDirect(ElfObjectFile elfObjectFile, Stream stream) : base(elfObjectFile, stream)
        {
        }
    }
}