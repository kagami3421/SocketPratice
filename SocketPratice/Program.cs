using SocketPratice.Core;
using System;
using System.Net;

namespace SocketPratice
{
    public class Program
    {
        private static string mCommand = "";

        private static NetworkManager networkManager;

        private static bool IsServer = false;

        public static void Main(string[] args)
        {
            networkManager = new NetworkManager();

            Console.WriteLine("Socket Pratice by Kagami");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("1. Be A Server");
            Console.WriteLine("2. Be A Client");

            mCommand = Console.ReadLine();

            switch (mCommand)
            {
                case "1":
                    {
                        networkManager.OnServerConnected += OnServerConnected;
                        networkManager.ServerStart(IPAddress.Parse("127.0.0.1"), 9487);

                        IsServer = true;
                        Console.WriteLine("Server Successfully Initialize!");
                    }
                    break;
                case "2":
                    {
                        Console.WriteLine("Enter IP:");
                        string _ip = Console.ReadLine();

                        networkManager.OnClientConnected += OnClientConnected;
                        networkManager.StartConnect(IPAddress.Parse(_ip), 9487);

                        IsServer = false;
                    }
                    break;
            }

            while (true)
            {
                if (IsServer)
                {
                    Console.WriteLine("Send Something To Client:");
                    string _message = Console.ReadLine();

                    networkManager.SendToAll(1001, new StringMessage(_message));
                }
            }
        }

        private static void OnClientConnected(Client sender)
        {
            Console.WriteLine("Client Successfully Connected!");

            networkManager.RegistHandler(1001, OnClientReceiveMessage);
        }

        private static void OnClientReceiveMessage(int clientID, NetworkMessage netMsg)
        {
            StringMessage a = netMsg.ReadMessage<StringMessage>();

            Console.WriteLine(string.Format("Received String: {0}" ,a.Value));
        }

        private static void OnServerConnected(Client sender)
        {
            Console.WriteLine(string.Format("Client {0} Connected !" , sender.ClientID));
        }
    }
}
