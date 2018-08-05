using System;

namespace SocketPratice.Core
{
    public class NetworkMessage
    {
        public short msgType;
        public Client client;
        public byte[] data;

        public TMsg ReadMessage<TMsg>() where TMsg : MessageBase, new()
        {
            TMsg result = Activator.CreateInstance<TMsg>();
            result.Deserialize(data);
            return result;
        }

        public void ReadMessage<TMsg>(TMsg msg) where TMsg : MessageBase
        {
            msg.Deserialize(data);
        }
    }
}
