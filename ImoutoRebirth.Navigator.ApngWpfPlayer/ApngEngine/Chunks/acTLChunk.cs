using System.IO;

namespace ImoutoRebirth.Navigator.ApngWpfPlayer.ApngEngine.Chunks
{
    internal class AcTlChunk : Chunk
    {
        public AcTlChunk(byte[] bytes)
            : base(bytes)
        {
        }

        public AcTlChunk(MemoryStream ms)
            : base(ms)
        {
        }

        public AcTlChunk(Chunk chunk)
            : base(chunk)
        {
        }

        public uint NumFrames { get; private set; }

        public uint NumPlays { get; private set; }

        protected override void ParseData(MemoryStream ms)
        {
            NumFrames = Helper.ConvertEndian(ms.ReadUInt32());
            NumPlays = Helper.ConvertEndian(ms.ReadUInt32());
        }
    }
}