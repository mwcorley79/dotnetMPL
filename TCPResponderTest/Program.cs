using System;
using System.Net;
using MPL;

namespace TCPResponderTest
{
    class TestClientHandler : ClientHandler
    {
        // this is a creational function: you must implement it
        // the TCPResponder creates an instance of this class (it's how the server polymorphism works)
        public override ClientHandler Clone()
        {
            return new TestClientHandler();
        }

        // this is where you define the custom server processing: you must implement it
        public override void AppProc()
        {
            // while there are messages in the blocking queue, and you have seen the disconnect
            // message, pull messages out and display them.

            Message msg;

            // when UseReceiveQueue == false, then we must ReceiveMessage instead of GetMessage()
            while ((msg = GetMessage()).Type != MessageType.DISCONNECT)
            {
                Console.WriteLine("Got a message this is devops CI/CD 7: " + msg + " from:  " + RemoteEP);
                PostMessage(new Message("Reply from server: " + GetServiceEndPoint , MessageType.DEFAULT));
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Test TCP Responder");

            EndPoint ep = new IPEndPoint(IPAddress.Any, 6060);
            TCPResponder responder = new TCPResponder(ep);
            responder.UseClientRecvQueue(true);
            responder.UseClientSendQueue(true);

            TestClientHandler th = new TestClientHandler();
            responder.RegisterClientHandler(th);
            responder.NumClients = 1;
            responder.Start();
            Console.WriteLine("Server Listing on: {0} ", ep);
          
            responder.Stop();            
        }
    }
}
