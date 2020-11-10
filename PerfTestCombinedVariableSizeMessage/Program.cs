/////////////////////////////////////////////////////////////////
// C++ (MPL) Comm - Test Communication library                 //
//                                                             //
// Mike Corley, https://github.com/mwcorley79, 07 Sept. 2020   //
/////////////////////////////////////////////////////////////////

/*
   Demo:
   Test message rate and throughput
   - start Listener component
   - start Connector component
   - start post_message thread
   - start recv_message thread
   - send a fixed number of messages
   - send END message to exit client handler
   - eval elapsed time
   - send QUIT message to shut down Listener
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MPL
{
    public class Program
    {
        /*---------------------------------------------------------
           Perf test - client does not wait for reply before posting
         */
        public static byte[] memset(char[] array, char ch)
        {
            for(int i = 0; i < array.Length; ++i)
            {
                array[i] = ch;
            }

            return Encoding.ASCII.GetBytes(array);
        }

        static void client_no_wait_for_reply(EndPoint addr,    // endpoint (address, port)
                                      string name,      // test name
                                      uint num_msgs,    // number of mesages to send
                                      uint sz_bytes     // body size in bytes
            )
        {

            Console.Write("\n -- " + name + ": " + num_msgs + " msgs," + (sz_bytes + Message.get_header_len().ToString()) + " bytes per msg");
            
            TCPConnector conn = new TCPConnector();

            conn.ConnectPersist(addr, 10, 1, 0);

            if (conn.IsConnected)
            {
               Thread  handle =  new Thread( () =>
                // Task handle = Task.Run(() =>
                {
                    int count = 0;
                    Message msg;
                    while ((msg = conn.GetMessage()).get_type() != MessageType.DISCONNECT)
                    {
                       count++;
                        //  Console.Write("\n received msg: " + msg.Length);
                    }

                    //Console.WriteLine("Received " + count + " messages");
                });

                handle.Start();
                

                // construct message of sz_bytes (pertains to message body, not including header)
                char[] body = new char[sz_bytes];

                // build a message with sz_bytes count of null characters 
                // Message msg = new Message(memset(body, '\0'), MessageType.DEFAULT);
                Message msg = new Message(sz_bytes, MessageType.DEFAULT);

                for (uint _i = 0; _i < num_msgs; ++_i)
                {
                    //std::cout << "\n posting msg " << name << " of size " << sz_bytes;
                    conn.PostMessage(msg);
                }
               

                conn.Close(handle); // handle); // handle);      
            }
        }


        /*-------------------------------------------------------
   Multiple clients running client_no_wait_for_reply
*/
        public static void multiple_clients(int nc,
                              EndPoint addr,    // endpoint (address, port)
                              string name,   // test name
                              uint num_msgs,       // number of mesages to send
                              uint sz_bytes        // body size in bytes
        )
        {
            Console.WriteLine("\n  number of clients:  " + nc);
            Stopwatch tmr = new Stopwatch();
            tmr.Start();

            // List<Task> handles = new List<Task>();
            List<Thread> handles = new List<Thread>();

            for (int _i = 0; _i < nc; ++_i)
            {
                Thread th = new Thread(() => client_no_wait_for_reply(addr, name, num_msgs, sz_bytes));
                handles.Add(th);
                th.Start();
               
                // handles.Add(Task.Factory.StartNew(() => client_no_wait_for_reply(addr, name, num_msgs, sz_bytes)));   
            }

            /*-- wait for all replies --*/
            foreach (var handle in handles)
            {
                handle.Join();
                //handle.Wait();
            }
            tmr.Stop();

            var et = tmr.Elapsed.TotalMilliseconds * 1000;
            var nm = (uint)nc * num_msgs;
            var tp = ((uint)(nm * sz_bytes)) / et;

            Console.Write("\n elapsed microseconds: " + et);
            Console.Write("\n number messages: " + nm);
            Console.WriteLine("\n throughput MB/S: " + tp);
        }


        public class PerfClientHandler : ClientHandler
        {
            // sz_bytes not a fixed size msg, its the chosen size to use for the given instance
            public PerfClientHandler(int sz_bytes)
            {
                sz_bytes_ = sz_bytes;
            }

            // this is a creational function: you must implement it
            // the TCPResponder creates an instance of this class (it's how the server polymorphism works)
            public override ClientHandler Clone()
            {
                return new PerfClientHandler(sz_bytes_);
            }

            // this is where you define the custom server processing: you must implement it
            public override void AppProc()
            {
                // construct message of sz_bytes (pertains to message body, not including header)
                char[] body = new char[sz_bytes_];

                // build a message with sz_bytes count of null characters 
               // Message msgSend = new Message(memset(body, '\0'), MessageType.DEFAULT);
                Message msgSend = new Message((ulong) sz_bytes_,  MessageType.DEFAULT);
                Message msg;
                //no use of queue
                while ((msg = ReceiveMessage()).get_type() != MessageType.DISCONNECT)
                {
                    // PostMessage(msg); // post to send queue
                    SendMessage(msgSend); //direct send                       
                }
            }

            private int sz_bytes_;
        };


        static void Main(string[] args)
        {
            //readPool.SetMaxThreads(100, 100);

            // specify the server Endpoint we wish to connect
            const int MSG_SIZE = 4096;
            const int NUM_CLIENTS =16;
            const int NUM_MSGS = 1000;
            //const int NUM_THREAD_POOL_THREADS = 8;
            const string TEST_NAME = "test4";

            EndPoint addr = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);
            PerfClientHandler ph = new PerfClientHandler(MSG_SIZE);

            // define instance of the TCPResponder (server host)
            TCPResponder responder = new TCPResponder(addr);

            // set number of clients for the server process to service before exiting (-1 runs indefinitely)
            responder.NumClients = NUM_CLIENTS;

            responder.UseClientSendReceiveQueues(false);  // uncomment if you use SendMessage/ReceiveMessage

            // register the custom client handler with TCPResponder instance
            responder.RegisterClientHandler(ph);

            // start the server listening thread
            responder.Start();

            // give the server a change to start
            Task.Delay(100).Wait();

            // std::thread test3 = std::thread(client_wait_for_reply, addr, "test3", 1000, 1024);
            // test3.join();

            Console.Write("\n  -- test4 (variable size message): C#_comm -- \n");
            int nt = 8;
            Console.Write("\n  num thrdpool thrds: " + nt);
            
            Program.multiple_clients(NUM_CLIENTS, addr, TEST_NAME, NUM_MSGS, MSG_SIZE);

            // std::cin.get();

            // stop the listener and quit
            responder.Stop();
           
        }
    }
}
