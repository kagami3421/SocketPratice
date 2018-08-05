using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocketPratice.Core
{
    public class StringMessage : MessageBase
    {
        public string Value;

        public StringMessage()
        {

        }

        public StringMessage(string value)
        {
            Value = value;
        }

        public override void Deserialize(byte[] byteArray)
        {
            Value = Encoding.UTF8.GetString(byteArray);
        }

        public override byte[] Serialize()
        {
            return Encoding.UTF8.GetBytes(Value);
        }
    }

    public class EmptyMessage : MessageBase
    {
        public override void Deserialize(byte[] byteArray)
        {
            base.Deserialize(byteArray);
        }

        public override byte[] Serialize()
        {
            return base.Serialize();
        }
    }

    public abstract class MessageBase
    {
        public virtual byte[] Serialize()
        {
            return new byte[0];
        }

        public virtual void Deserialize(byte[] byteArray)
        {
            return;
        }
    }
}
