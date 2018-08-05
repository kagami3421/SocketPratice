using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace SocketPratice.Core
{
    public class Client
    {
        public int ClientID { get; internal set; }

        public TcpClient TcpClient { get; private set; }

        public event ClientDelegate OnDisconnect;

        public event ClientMessageDelegate OnMessage;

        private Thread mProcessThread;

        private Dictionary<short, List<NetworkMessageDelegate>> messageHandlers = new Dictionary<short, List<NetworkMessageDelegate>>();

        public Client(TcpClient tcpClient)
        {
            this.TcpClient = tcpClient;
            mProcessThread = new Thread(new ThreadStart(ReceiveProcess));
            mProcessThread.IsBackground = true;
            mProcessThread.Start();
        }

        public void Send(short msgType, MessageBase msg)
        {
            if (TcpClient == null)
                return;

            if (!TcpClient.Connected)
                return;

            byte[] _ByteArray = msg.Serialize();

            try
            {
                TcpClient.Client.Send(BitConverter.GetBytes((short)9487));
                TcpClient.Client.Send(BitConverter.GetBytes(msgType));
                TcpClient.Client.Send(BitConverter.GetBytes((uint)_ByteArray.Length));
                if (_ByteArray.Length > 0)
                    TcpClient.Client.Send(_ByteArray);
            }
            catch (Exception)
            {

            }
        }

        private void ReceiveProcess()
        {
            if (TcpClient == null)
                return;

            if (!TcpClient.Connected)
                return;

            try
            {
                byte[] _HeadBuffer = new byte[2];
                byte[] _MsgTypeBuffer = new byte[2];
                byte[] _LengthBuffer = new byte[4];
                NetworkStream _Stream = this.TcpClient.GetStream();

                int _Bytes = -1;

                while (TcpClient != null && TcpClient.Connected)
                {
                    int _HeadBytes = _Stream.Read(_HeadBuffer, 0, _HeadBuffer.Length);
                    if (_HeadBytes != 2)
                        continue;

                    short _Head = BitConverter.ToInt16(_HeadBuffer, 0);
                    if (_Head != 9487)
                        continue;

                    int _MsgTypeBytes = _Stream.Read(_MsgTypeBuffer, 0, _MsgTypeBuffer.Length);
                    if (_MsgTypeBytes != 2)
                        continue;

                    short _MsgType = BitConverter.ToInt16(_MsgTypeBuffer, 0);

                    int _LengthBytes = _Stream.Read(_LengthBuffer, 0, _LengthBuffer.Length);
                    if (_LengthBytes != 4)
                        continue;

                    uint _Length = BitConverter.ToUInt32(_LengthBuffer, 0);

                    byte[] _Buffer = new byte[_Length];
                    if (_Length > 0)
                    {
                        _Bytes = _Stream.Read(_Buffer, 0, _Buffer.Length);

                        if (_Bytes != _Length)
                            continue;
                    }

                    NetworkMessage _Msg = new NetworkMessage();
                    _Msg.client = this;
                    _Msg.msgType = _MsgType;
                    _Msg.data = _Buffer;

                    if (OnMessage != null)
                    {
                        OnMessage.Invoke(this, _Msg);
                    }

                    var _Callbacks = GetCallbacks(_MsgType);
                    if (_Callbacks == null)
                        continue;

                    foreach (var _Callback in _Callbacks)
                    {
                        _Callback.Invoke(ClientID, _Msg);
                    }
                }
            }
            catch (Exception)
            {
                if (TcpClient.Connected)
                    TcpClient.Close();
            }

            if (OnDisconnect != null)
                OnDisconnect.Invoke(this);
        }

        private List<NetworkMessageDelegate> GetCallbacks(short msgType)
        {
            List<NetworkMessageDelegate> _HandlerList;
            if (!messageHandlers.TryGetValue(msgType, out _HandlerList))
            {
                return null;
            }
            return _HandlerList;
        }
    }
}
