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
            recvQueue = new BlockingQueue<Message>();

            isSendingMutex = new object();
            isReceivingMutex = new object();
            useSendQueueMutex = new object();
            useRecvQueueMutex = new object();
            isSending = isReceiving = false;
            useRecvQueue = useSendQueue = true;
        }

       
        public bool IsConnected  => socket.Connected;
        
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
                    Task.Delay((int) wtime_secs * 1000).Wait();
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
            if (!IsSending())
            {
                IsSending(true);

                sendTask = Task.Run(() =>
                {
                    try
                    {       
                        while (IsSending())
                        {
                            // deque the next message
                            Message msg = this.sendQueue.deQ();

                            // if this is the stop sending message, signal
                            // the send thread to shutdown
                            if (msg.Type == MessageType.STOP_SENDING)
                            {
                                IsSending(false);
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
                         IsSending(false);
                    }
                });
            }
        }

        public Message GetMessage()
        {
            return recvQueue.deQ();
        }

        public Message RecvMessage()
        {
            return RecvSocketMessage();
        }

        void StartReceiving()
        {
            if (!IsReceiving())
            {
                IsReceiving(true);

                recvTask = Task.Run(() =>
                {
                    try
                    {
                        while (IsReceiving())
                        {
                            Message msg = RecvSocketMessage();
                            recvQueue.enQ(msg);
                            if (msg.Type == MessageType.DISCONNECT)
                            {
                                IsReceiving(false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        IsReceiving(false);
                    }

                });
            }
        }

         
        // serialize the message header and message and write them into the socket
        Message RecvSocketMessage()
        {
            byte[] hdr_bytes = new byte[MSGHEADER.SIZE];
            int recv_bytes;

            // receive fixed size message header (see wire protocol in Message.h)
            if ((recv_bytes = socket.Receive(hdr_bytes)) == MSGHEADER.SIZE)
            {
                // construct a Message using the Message header read from the socket channel
                // *** critical that mhdr is host byte order (e.g. Intel CPU == little endian) ***
                Message msg = new Message(new MSGHEADER(hdr_bytes, true));

                // recv message data
                if (socket.Receive(msg.GetInternalDataBuf()) != msg.Length)
                    throw new SocketException();

                return msg;
            }

            // if read zero bytes, then this is the zero length message signaling client shutdown
            if (recv_bytes == 0)
            {
                return new Message(MessageType.DISCONNECT);
            }
            else
            {
                throw new SocketException();
            }
        }


        private bool Start()
        {
            if (IsConnected)
            {
                if (UseSendQueue())
                    StartSending();

                if (UseRecvQueue())
                    StartReceiving();

                return true;
            }
            return false;
        }

        void StopSending()
        {
            if(!IsSending())
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
                if (UseSendQueue())
                {
                    StopSending();
                    sendTask.Wait();
                }
            }

            socket.Shutdown(SocketShutdown.Send);

            if(UseRecvQueue())
            {
                recvTask.Wait();
            }
            socket.Shutdown(SocketShutdown.Receive);
            socket.Close();
        }

        public bool IsSending()
        {
            lock (isSendingMutex)
            {
                return isSending;
            }
        }
        public void IsSending(bool val)
        {
            lock (isSendingMutex)
            {
                isSending = val;
            }
        }

        public bool IsReceiving()
        {
            lock (isReceivingMutex)
            {
                return isReceiving;
            }
        }
        public void IsReceiving(bool val)
        {
            lock (isReceivingMutex)
            {
                isReceiving = val;
            }
        }

        public bool UseSendQueue()
        {
            lock (useSendQueueMutex)
            {
                return useSendQueue;
            }
        }
        public void UseSendQueue(bool val)
        {
            lock (useSendQueueMutex)
            {
                useSendQueue = val;
            }
        }

        public bool UseRecvQueue()
        {
            lock (useRecvQueueMutex)
            {
                return useRecvQueue;
            }
        }
        public void UseRecvQueue(bool val)
        {
            lock (useRecvQueueMutex)
            {
                useRecvQueue = val;
            }
        }

        Socket socket;
        BlockingQueue<Message> sendQueue;
        BlockingQueue<Message> recvQueue;
        Task sendTask;
        Task recvTask;
      
        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/volatile
        volatile bool isSending;
        object isSendingMutex;

        volatile bool isReceiving;
        object isReceivingMutex;

        volatile bool useSendQueue;
        object useSendQueueMutex;

        volatile bool useRecvQueue;
        object useRecvQueueMutex;



    }
}
