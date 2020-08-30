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
                sendTask = Task.Run(() =>
                {
                    try
                    {
                        //IsSending(true);
                        while (isSending)
                        {
                            // deque the next message
                            Message msg = this.sendQueue.deQ();

                            // if this is the stop sending message, signal
                            // the send thread to shutdown
                            if (msg.Type == MessageType.STOP_SENDING)
                            {
                                isSending = true;
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

            socket.Shutdown(SocketShutdown.Send);

              
            socket.Close();
        }

        Socket socket;
        BlockingQueue<Message> sendQueue;
        Task sendTask;
      
        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/volatile
        volatile bool isSending;

    }
}
