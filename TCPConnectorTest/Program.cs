using System;
using System.Net;
using MPL;

namespace TCPConnectorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            TCPConnector connector = new TCPConnector();
            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = host.AddressList[1];
            EndPoint ep = new IPEndPoint(ipAddress, 6060);
            connector.Connect(ep);
            if(connector.IsConnected)
            {
                connector.PostMessage(new Message("Hello C++ C++ and Rust, from .Net Core", MessageType.DEFAULT));
            }

            connector.Close();

        }
    }
}
