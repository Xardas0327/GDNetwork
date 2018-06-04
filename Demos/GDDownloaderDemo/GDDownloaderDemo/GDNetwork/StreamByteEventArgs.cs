using System;

namespace GDNetwork
{
    public class StreamByteEventArgs : EventArgs
    {
        public long Bytes { get; private set; }

        public StreamByteEventArgs(long streamByte)
        {
            Bytes = streamByte;
        }
    }
}
