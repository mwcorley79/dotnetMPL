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
            //IPHostEntry host = Dns.GetHostByAddress(IPAddress.Parse("192.168.37.146"));
            //IPAddress ipAddress = host.AddressList[0];
            EndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6060);
            //connector.Connect(ep);
            //if(connector.IsConnected)
            if(connector.ConnectPersist(ep, 10, 1,1) < 10)
            {
                for (int i = 0; i < 100000; ++i)
                {
                    connector.PostMessage(new Message("Hello C++ and Rust, from .Net Core", MessageType.DEFAULT));
                }
                connector.Close();
            }

            

        }
    }
}
