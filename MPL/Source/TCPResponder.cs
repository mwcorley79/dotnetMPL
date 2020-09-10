using SWTools;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MPL
{
    public class TCPResponder
    {
        public TCPResponder(EndPoint ep)
        {
            listenSocket_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listen_ep_ = ep;
            listenSocket_.Bind(listen_ep_);
            service_queue = new BlockingQueue<Task<bool>>();
            ch_ = null;
            isListening = new AtomicBool(false);
            numClients = new Atomic<int>(-1);
            useClientRecvQueue = new AtomicBool(true);
            useClientSendQueue = new AtomicBool(true);
        }

        public void UseClientRecvQueue(bool val) => useClientRecvQueue.set(val);

        public void UseClientSendQueue(bool val) => useClientSendQueue.set(val);

        public void UseClientSendReceiveQueues(bool val)
        {
            UseClientRecvQueue(val);
            UseClientSendQueue(val);
        }

        public int NumClients
        {
            get
            {
                return numClients.get();
            }
            set
            {
                numClients.set(value);
            }
        }

        public void RegisterClientHandler(ClientHandler ch)
        {
            ch_ = ch;
        }


        void ServiceClient(ClientHandler ch)
        {
            try
            {
                //start the client processing thread, if use specifies to 
                if (useClientRecvQueue.get())
                    ch.StartReceiving();

                // start the send thread
                if (useClientSendQueue.get())
                    ch.StartSending();

                // recycle this (current) thread to run the user define AppProc
                ch.AppProc();
            }
            catch (Exception ex)
            {
                Console.WriteLine("TCPResponder::ServiceClient 1 : " + ex.Message);
            }

            try
            {
               
                //wait for the receive thread to shutdown
                if (useClientRecvQueue.get())
                    ch.StopReceiving();
                ch.ShutdownRecv();

                // stop the send thread
                if (useClientSendQueue.get())
                    ch.StopSending();
                ch.ShutdownSend();

                // signal client to shutdown
                ch.Close();
            }
            catch (Exception ex )
            {
                Console.WriteLine("TCPResponder::ServiceClient 2 : " + ex.Message);
            }
        }

        public void Start(int backlog=20)
        {
            if (!isListening.get())
            {
                isListening.set(true);

                //listenTask = Task.Run(() =>
                listenThread = new Thread(() =>
               {
                   int client_count = 0;
                   listenSocket_.Listen(backlog);
                   List<Thread> serviceQ_ = new List<Thread>();
                   while (isListening.get() && (client_count++ < NumClients || NumClients == -1))
                   {
                       Socket client_socket = listenSocket_.Accept();

                       if (client_socket != null)
                       {
                           if (ch_ != null)
                           {
                               ClientHandler ch = ch_.Clone();

                               ch.SetSocket(client_socket);

                               ch.SetServiceEndPoint(listen_ep_);

                            // same principle effect as Dr. Fawcett's C++ ThreadPool
                            // (see the C++ version, TCPResponder.cpp)
                            // each client is service on a separate thread pool task (a C# future)
                            //here we use the blocking queue to signal when to exit;

                            /* service_queue.enQ(Task.Run(() =>
                            {
                                ServiceClient(ch);
                                return true;
                            }));
                            */

                            // wait for all service threads to complete 
                            Thread serviceClient = new Thread(() => ServiceClient(ch));
                               serviceQ_.Add(serviceClient);
                               serviceClient.Start();
                           }
                       }
                   }

                // wait for all service tasks to complete 
                for (int i = 0; i < serviceQ_.Count; ++i)
                       serviceQ_[i].Join();


                // use to null to signal complete 
                /* service_queue.enQ(Task.Run(() => false));
                Task<bool> client_task;
                
                // wait for all service threads to complete 
                do
                {
                    client_task = service_queue.deQ();
                    client_task.Wait();
                }
                while (client_task.Result == true);
                */

               });
                listenThread.Start();
            }
        }

        public void Stop()
        {
            // if user started the listener, then shut it all down
            if (isListening.get())
            {
                try
                {
                    //listenTask.Wait();
                    listenThread.Join();
                   
                    listenSocket_.Close();
                    isListening.set(false);
                }
                catch
                {
                    isListening.set(true);
                }
            }
        }


        private Socket listenSocket_;
        private EndPoint listen_ep_;
        //private Task listenTask;
        private Thread listenThread;
        private AtomicBool isListening;
        private AtomicBool useClientSendQueue;
        private AtomicBool useClientRecvQueue;
        private BlockingQueue<Task<bool>> service_queue;
        private ClientHandler ch_;
        private Atomic<int> numClients;

        
    }
}
