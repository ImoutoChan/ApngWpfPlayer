using System.IO;

namespace ImoutoRebirth.Navigator.ApngWpfPlayer.ApngEngine.Chunks
{
    internal class IendChunk : Chunk
    {
        public IendChunk(byte[] bytes)
            : base(bytes)
        {
        }

        public IendChunk(MemoryStream ms)
            : base(ms)
        {
        }

        public IendChunk(Chunk chunk)
            : base(chunk)
        {
        }
    }
}