using System;
using System.Net;
using System.Threading.Tasks;
using MPL;

namespace TCPConnectorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            TCPConnector connector = new TCPConnector();
            connector.UseRecvQueue(false);
            connector.UseSendQueue(false);
            //IPHostEntry host = Dns.GetHostByAddress(IPAddress.Parse("192.168.37.146"));
            //IPAddress ipAddress = host.AddressList[0];
            EndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6060);
            //connector.Connect(ep);
            //if(connector.IsConnected)
            string name = "test 1";
            int num_messages = 10;

            if(connector.ConnectPersist(ep, 10, 1,1) < 10)
            {
                for (int i = 0; i < num_messages; ++i)
                {
                    Message msg = new Message(name + " [ Message #: " + (i + 1).ToString() + " ]", MessageType.DEFAULT);
                    Console.WriteLine("Sending: " + msg);
                    connector.SendMessage(msg);
                    Message reply = connector.RecvMessage();
                    Console.WriteLine(reply);
                    
                    Task.Delay(1000).Wait();
                }
                connector.Close();
            }

            

        }
    }
}
