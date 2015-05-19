using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// This is a very special packet.
    /// This packet gets sent by IrisNetwork to send out a partial frame update.
    /// 
    /// This will only get used by iris internally, never send such packets on your own.
    /// 
    /// PacketID = 3
    /// </summary>
    public class IrisPartialFrameUpdateMessage : IrisNetworkMessage
    {
        public IrisViewUpdate[] ViewUpdates
        {
            get { return this.viewUpdates; }
        }

        private IrisViewUpdate[] viewUpdates;

        /// <summary>
        /// Used for compression and encryption (TODO).
        /// </summary>
        private IrisMaster master;

        public IrisPartialFrameUpdateMessage() { }

        public IrisPartialFrameUpdateMessage(IrisPlayer sender, IrisViewUpdate[] viewUpdates, IrisMaster master)
            : base(sender)
        {
            this.viewUpdates = viewUpdates;
            this.master = master;
        }

        public override byte GetPacketId()
        {
            return (byte)3;
        }

        public override void Serialize(IrisStream stream)
        {
            if (stream.IsWriting)
            {
                // Write all view updates to the temporary stream
                IrisStream temporaryStream = new IrisStream(this.master);
                temporaryStream.SerializeObject<IrisViewUpdate>(ref this.viewUpdates);

                byte[] d = temporaryStream.GetBytes();

                // Check if compression is enabled
                if (IrisNetwork.Compression != IrisCompression.NONE)
                {
                    switch (IrisNetwork.Compression)
                    {
                        case IrisCompression.GOOGLE_SNAPPY:
                            // Compress using google snappy
                            Snappy.Sharp.SnappyCompressor compressor = new Snappy.Sharp.SnappyCompressor();
                            byte[] compressed = new byte[compressor.MaxCompressedLength(d.Length)];
                            int compressedLength = compressor.Compress(d, 0, d.Length, compressed);

                            d = new byte[compressedLength];

                            Array.Copy(compressed, d, d.Length);

                            // Output statistics
                            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisNetwork", "Client sent out network frame with compression. Uncompressed size: " + compressed.Length + ", Compressed size: " + d.Length);
                            break;
                    }
                }

                stream.Serialize(ref d);
            }
            else
            {
                // Read data
                byte[] data = null;
                stream.Serialize(ref data);

                // Check if compression is enabled
                if (IrisNetwork.Compression != IrisCompression.NONE)
                {
                    switch (IrisNetwork.Compression)
                    {
                        case IrisCompression.GOOGLE_SNAPPY:
                            // Decompress using google snappy
                            Snappy.Sharp.SnappyDecompressor decompressor = new Snappy.Sharp.SnappyDecompressor();
                            byte[] decompressed = decompressor.Decompress(data, 0, data.Length);
                            data = decompressed;

                            break;
                    }
                }

                IrisStream temporaryStream = new IrisStream(this.master, data);
                temporaryStream.SerializeObject<IrisViewUpdate>(ref this.viewUpdates);
            }
        }
    }
}
