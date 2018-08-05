namespace SocketPratice.Core
{
    public delegate void ClientDelegate(Client sender);
    public delegate void ClientMessageDelegate(Client sender, NetworkMessage msg);
}
