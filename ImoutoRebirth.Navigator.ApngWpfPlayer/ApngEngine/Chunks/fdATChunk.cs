using System.IO;

namespace ImoutoRebirth.Navigator.ApngWpfPlayer.ApngEngine.Chunks
{
    internal class FdAtChunk : Chunk
    {
        public FdAtChunk(byte[] bytes)
            : base(bytes)
        {
        }

        public FdAtChunk(MemoryStream ms)
            : base(ms)
        {
        }

        public FdAtChunk(Chunk chunk)
            : base(chunk)
        {
        }

        public uint SequenceNumber { get; private set; }

        public byte[] FrameData { get; private set; }

        protected override void ParseData(MemoryStream ms)
        {
            SequenceNumber = Helper.ConvertEndian(ms.ReadUInt32());
            FrameData = ms.ReadBytes((int)Length - 4);
        }

        public IdatChunk ToIdatChunk()
        {
            uint newCrc;
            using (var msCrc = new MemoryStream())
            {
                msCrc.WriteBytes(new[] {(byte)'I', (byte)'D', (byte)'A', (byte)'T'});
                msCrc.WriteBytes(FrameData);

                newCrc = CrcHelper.Calculate(msCrc.ToArray());
            }

            using (var ms = new MemoryStream())
            {
                ms.WriteUInt32(Helper.ConvertEndian(Length - 4));
                ms.WriteBytes(new[] {(byte)'I', (byte)'D', (byte)'A', (byte)'T'});
                ms.WriteBytes(FrameData);
                ms.WriteUInt32(Helper.ConvertEndian(newCrc));
                ms.Position = 0;

                return new IdatChunk(ms);
            }
        }
    }
}