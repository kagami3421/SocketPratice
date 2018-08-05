using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace SocketPratice.Core
{
    public class NetworkManager
    {
        public event ClientDelegate OnClientConnected;
        public event ClientDelegate OnClientDisconnected;

        public event ClientDelegate OnServerConnected;
        public event ClientDelegate OnServerDisconnected;

        public bool IsListening { get; private set; }
        public bool IsConnecting { get; private set; }
        public bool IsConnected { get; private set; }

        private TcpListener mListener;

        private TcpClient mTcpClient;

        private Client mClinet;

        private List<Client> mConnections = new List<Client>();

        private Dictionary<short, List<NetworkMessageDelegate>> messageHandlers = new Dictionary<short, List<NetworkMessageDelegate>>();

        #region Server 

        public void ServerStart(IPAddress address, int port)
        {
            if (IsListening)
                return;

            if (mListener != null)
            {
                mListener.Stop();
                mListener = null;
            }

            mListener = new TcpListener(address, port);
            mListener.Start();

            IsListening = true;

            mListener.BeginAcceptTcpClient(OnClientConnect, mListener);
        }

        public void ServerStop()
        {
            if (!IsListening)
                return;

            IsListening = false;
            mListener.Stop();
            mListener = null;
        }

        public void SendToClient(int clientID, short msgType, MessageBase msg)
        {
            if (!IsListening)
                return;

            if (mConnections[clientID] == null)
                return;

            mConnections[clientID].Send(msgType, msg);
        }

        public void SendToAll(short msgType, MessageBase msg)
        {
            if (!IsListening)
                return;

            for (int i = 0; i < mConnections.Count; i++)
            {
                if (mConnections[i] == null)
                    continue;

                mConnections[i].Send(msgType, msg);
            }
        }

        private void OnClientConnect(IAsyncResult ar)
        {
            if (!IsListening)
                return;

            TcpListener _Listener = (TcpListener)ar.AsyncState;
            TcpClient _TcpClient = _Listener.EndAcceptTcpClient(ar);

            Client _Client = new Client(_TcpClient);

            //加入至玩家列表
            int _EmptyIndex = mConnections.IndexOf(null);
            if (_EmptyIndex >= 0)
            {
                mConnections[_EmptyIndex] = _Client;
            }
            else
            {
                mConnections.Add(_Client);
                _EmptyIndex = mConnections.IndexOf(_Client);
            }

            _Client.ClientID = _EmptyIndex;
            _Client.OnDisconnect += OnClientDisconnect;
            _Client.OnMessage += OnMessageHandler;

            if (OnServerConnected != null)
                OnServerConnected.Invoke(_Client);

            mListener.BeginAcceptTcpClient(OnClientConnect, mListener);
        }

        private void OnClientDisconnect(Client sender)
        {
            sender.OnDisconnect -= OnClientDisconnect;
            sender.OnMessage -= OnMessageHandler;

            int _Index = mConnections.IndexOf(sender);
            if (_Index < 0)
                return;

            mConnections[_Index] = null;

            if (OnServerDisconnected != null)
                OnServerDisconnected.Invoke(sender);
        }

        #endregion

        #region Client

        public void StartConnect(IPAddress address, int port)
        {
            if (IsConnecting || IsConnected)
                return;

            mTcpClient = new TcpClient();
            mTcpClient.BeginConnect(address, port, OnConnectFinish, mTcpClient);
        }

        public void Disconnect()
        {
            if (!IsConnected)
                return;

            if (mTcpClient == null)
                return;

            mTcpClient.Close();
            IsConnected = false;

            if (OnClientDisconnected != null)
                OnClientDisconnected.Invoke(null);
        }

        private void OnConnectFinish(IAsyncResult ar)
        {
            IsConnecting = false;

            if (mTcpClient.Connected)
            {
                IsConnected = true;
                mClinet = new Client(mTcpClient);
                mClinet.OnDisconnect += OnClientDisconnectHandler;
                mClinet.OnMessage += OnMessageHandler;

                if (OnClientConnected != null)
                    OnClientConnected.Invoke(mClinet);
            }
            else
            {
                IsConnected = false;
                if (OnClientDisconnected != null)
                    OnClientDisconnected.Invoke(null);
            }
        }

        private void OnClientDisconnectHandler(Client sender)
        {
            IsConnected = false;
            if (OnClientDisconnected != null)
                OnClientDisconnected.Invoke(null);
        }

        private void OnMessageHandler(Client sender, NetworkMessage msg)
        {
            var _Callbacks = GetCallbacks(msg.msgType);
            if (_Callbacks == null)
                return;

            foreach (var _Callback in _Callbacks)
            {
                _Callback.Invoke(sender.ClientID, msg);
            }
        }

        public void RegistHandler(short msgType, NetworkMessageDelegate callback)
        {
            List<NetworkMessageDelegate> _HandlerList;
            if (!messageHandlers.TryGetValue(msgType, out _HandlerList))
            {
                _HandlerList = new List<NetworkMessageDelegate>();
                messageHandlers.Add(msgType, _HandlerList);
            }

            if (!_HandlerList.Contains(callback))
            {
                _HandlerList.Add(callback);
            }
        }

        public void UnRegisterHandler(short msgType)
        {
            if (messageHandlers.ContainsKey(msgType))
            {
                messageHandlers.Remove(msgType);
            }
        }

        public void UnRegisterHandler(short msgType, NetworkMessageDelegate callback)
        {
            List<NetworkMessageDelegate> _HandlerList;
            if (!messageHandlers.TryGetValue(msgType, out _HandlerList))
            {
                _HandlerList = new List<NetworkMessageDelegate>();
                messageHandlers.Add(msgType, _HandlerList);
            }

            if (_HandlerList.Contains(callback))
            {
                _HandlerList.Remove(callback);
            }
        }

        public void SendToServer(short msgType, MessageBase msg)
        {
            if (!IsConnected)
                return;

            mClinet.Send(msgType, msg);
        }

        public void Dispose()
        {
            if (mClinet != null)
            {
                mClinet.TcpClient.Close();
            }
        }

        #endregion

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
