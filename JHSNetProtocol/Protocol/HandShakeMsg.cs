namespace JHSNetProtocol
{
    public class HandShakeMsg : JHSMessageBase
    {
        public uint Version = 0;
        public byte OP = 0;
        public override void Deserialize(JHSNetworkReader reader)
        {
            Version = reader.ReadPackedUInt32();
            OP = reader.ReadByte();
        }
        public override void Serialize(JHSNetworkWriter writer)
        {
            writer.WritePackedUInt32(Version);
            writer.Write(OP);
        }
    }
}
