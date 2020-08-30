using SWTools;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MPL
{
    public class TCPConnector
    {
        public TCPConnector()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sendQueue = new BlockingQueue<Message>();
        }

        public bool IsConnected => socket.Connected;
       

        public void Connect(EndPoint ep)
        {
            socket.Connect(ep);
            Start();
        }

        public uint ConnectPersist(EndPoint ep, uint retries, uint wtime_secs, uint vlevel)
        {
            uint runAttempts = 0;
            while (runAttempts++ < retries) 
            {
                try
                {
                    if (vlevel > 0)
                    {
                        Console.WriteLine($"Connection attempt # {runAttempts} runAttempts of {retries} to {ep.ToString()}");
                    }

                    Connect(ep);
                    break;
                }
                catch (Exception)
                {
                    if (vlevel > 0)
                    {
                        Console.WriteLine($"Failed Attempt: {runAttempts}");
                    }
                    Task.Delay((int) wtime_secs * 1000);
                }
            }
            return runAttempts;
        }

        public void PostMessage(Message msg)
        {
            sendQueue.enQ(msg);
        }

        public void SendMessage(Message msg)
        {
            SendSocketMessage(msg);
        }

        public virtual void SendSocketMessage(Message msg)
        {
            // send the header
            socket.Send(msg.GetHeader.ToNetworkByteOrder());

            // send the data
            if (msg.GetData != null)
                socket.Send(msg.GetData);
        }

        private void StartSending()
        {
            if (!isSending)
            {
                isSending = true;
                sendTask = Task.Run(() =>
                {
                    try
                    {
                       
                        while (isSending)
                        {
                            // deque the next message
                            Message msg = this.sendQueue.deQ();

                            // if this is the stop sending message, signal
                            // the send thread to shutdown
                            if (msg.Type == MessageType.STOP_SENDING)
                            {
                                isSending = false;
                            }
                            else
                            {
                                // serialize the message into the socket
                                SendSocketMessage(msg);
                            }
                        }
                    }
                    catch
                    {
                        isSending = false;
                    }
                });
            }
        }

        private void Start()
        {
            if (IsConnected)
            {
                
                StartSending();
            }   
        }

        void StopSending()
        {
            if (isSending)
            {
                //note: only gets deposited into queue if IsSending is true
                Message StopMsg = new Message(MessageType.STOP_SENDING);
                sendQueue.enQ(StopMsg);
            }
        }

        public void Close()
        {
            if (IsConnected)
            {
                StopSending();
                sendTask.Wait();
             }

           // socket.Shutdown(SocketShutdown.Send);
            socket.Shutdown(SocketShutdown.Both);

            socket.Close();
        }

        Socket socket;
        BlockingQueue<Message> sendQueue;
        Task sendTask;
      
        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/volatile
        volatile bool isSending;

    }
}
