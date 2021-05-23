using System.IO;

namespace ImoutoRebirth.Navigator.ApngWpfPlayer.ApngEngine.Chunks
{
    internal class IdatChunk : Chunk
    {
        public IdatChunk(byte[] bytes)
            : base(bytes)
        {
        }

        public IdatChunk(MemoryStream ms)
            : base(ms)
        {
        }

        public IdatChunk(Chunk chunk)
            : base(chunk)
        {
        }
    }
}