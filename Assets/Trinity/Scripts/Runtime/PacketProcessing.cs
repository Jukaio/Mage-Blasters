namespace Alteruna.Trinity
{
    /// <summary>
    /// Class <c>Writer</c> is used to write data to a <c>IPacketProcessor</c>.
    /// </summary>
    /// <seealso cref="IPacketProcessor"/>
    /// 
    public class Writer
    {
        private IPacketProcessor mProcessor;

        public Writer(IPacketProcessor processor)
        {
            mProcessor = processor;
        }

        public void Write(int value)
        {
            mProcessor.Put(value);
        }

        public void Write(float value)
        {
            mProcessor.Put(value);
        }

        public void Write(bool value)
        {
            mProcessor.Put(value);
        }

        public void Write(string value)
        {
            mProcessor.Put(value);
        }

        public void Write(uint value)
        {
            mProcessor.Put(value);
        }

        public void Write(byte value)
        {
            mProcessor.Put(value);
        }

        public void Write(ushort value)
        {
            mProcessor.Put(value);
        }

        public void Write(byte[] value)
        {
            mProcessor.Put(value);
        }
    }

    /// <summary>
    /// Class <c>Writer</c> is used to read data from a <c>IPacketProcessor</c>.
    /// </summary>
    /// <seealso cref="IPacketProcessor"/>
    /// 
    public class Reader
    {
        private IPacketProcessor mProcessor;

        public Reader(IPacketProcessor processor)
        {
            mProcessor = processor;
        }

        public int ReadInt()
        {
            return mProcessor.GetInt();
        }

        public float ReadFloat()
        {
            return mProcessor.GetFloat();
        }

        public bool ReadBool()
        {
            return mProcessor.GetBool();
        }

        public string ReadString()
        {
            return mProcessor.GetString();
        }

        public uint ReadUint()
        {
            return mProcessor.GetUInt();
        }

        public byte ReadByte()
        {
            return mProcessor.GetByte();
        }

        public ushort ReadUshort()
        {
            return mProcessor.GetUShort();
        }

        public byte[] ReadByteArray()
        {
            return mProcessor.GetByteArray();
        }
    }
}