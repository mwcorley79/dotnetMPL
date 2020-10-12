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

            isReceiving = new AtomicBool(false);
            isSending = new AtomicBool(false);

            useRecvQueue = new AtomicBool(true);
            useSendQueue = new AtomicBool(true);
        }

        public bool IsSending => isSending.get();

        public bool IsReceiving => isReceiving.get();

        public void UseRecvQueue(bool val) => useRecvQueue.set(val);

        public void UseSendQueue(bool val) => useSendQueue.set(val);

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
                    Task.Delay((int)wtime_secs * 1000).Wait();
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

        private bool Start()
        {
            if (IsConnected)
            {
                if (useSendQueue.get())
                    StartSending();

                if (useRecvQueue.get())
                    StartReceiving();

                return true;
            }

            return false;
        }

        private void StartSending()
        {
            if (!isSending.get())
            {
                isSending.set(true);
                sendTask = Task.Run(SendProc);
            }
        }

        protected virtual void SendProc()
        {
            try
            {
                //isSending.set(true);

                Message msg = sendQueue.deQ();
                // if this is the stop sending message, signal
                // the send thread to shutdown
                while (msg.get_type() != MessageType.STOP_SENDING)
                {
                    // serialize the message into the socket
                    SendSocketMessage(msg);

                    // deque the next message
                    msg = sendQueue.deQ();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TCPConnector::SendProc: " + ex.Message); // IsSending(false);
            }
        }

        protected virtual void SendSocketMessage(Message msg)
        {
            // send the header
            SocketUtils.SendAll(socket, msg.get_header_bytes(), SocketFlags.None);

            // send the data
            SocketUtils.SendAll(socket, msg.get_content_bytes(), SocketFlags.None);
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
            if (!isReceiving.get())
            {
                isReceiving.set(true);
                recvTask = Task.Run(RecvProc);
            }
        }

        protected virtual void RecvProc()
        {
            try
            {
                Message msg;
                do
                {
                    msg = RecvSocketMessage();
                    recvQueue.enQ(msg);
                }
                while (msg.get_type() != MessageType.DISCONNECT);
            }
            catch (Exception ex)
            {
                Console.WriteLine("TCPConnector::RecvProc: " + ex.Message);
            }
        }

        // serialize the message header and message and write them into the socket
        protected virtual Message RecvSocketMessage()
        {   
            byte[] hdr_bytes = new byte[Message.get_header_len()];
            int recv_bytes;

            // receive fixed size message header (see wire protocol in Message.h)
            if ((recv_bytes = SocketUtils.RecvAll(socket, hdr_bytes, SocketFlags.None)) == (int) Message.get_header_len())
            {
                // construct a Message using the Message header read from the socket channel
                // *** critical that mhdr is host byte order (e.g. Intel CPU == little endian) ***
                Message msg = new Message(hdr_bytes);

                // recv message data
                if (SocketUtils.RecvAll(socket, msg.get_content_bytes(), SocketFlags.None) != (int) msg.get_content_len())
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

        private void StopSending()
        {
            try
            {
                if (isSending.get())
                {
                    PostMessage(new Message(MessageType.STOP_SENDING));
                    sendTask.Wait();
                    isSending.set(false);
                }
            }
            catch
            {
                isSending.set(false);
            }
        }

        private void StopReceiving()
        {
            try
            {
                if (isReceiving.get())
                {
                    recvTask.Wait();
                    isReceiving.set(false);
                }
            }
            catch
            {
                isReceiving.set(false);
            }
        }

        public void Close(Thread listener = null)
        {

            if (IsConnected)
            {
                if (useSendQueue.get())
                    StopSending();
                socket.Shutdown(SocketShutdown.Send);

                if (listener != null)
                    listener.Join();

                if (useRecvQueue.get())
                    StopReceiving();
                socket.Shutdown(SocketShutdown.Receive);

                socket.Close();
            }
        }


        protected Socket socket;
        BlockingQueue<Message> sendQueue;
        BlockingQueue<Message> recvQueue;
        Task sendTask;
        Task recvTask;

        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/volatile
        AtomicBool isSending;
        AtomicBool isReceiving;
        AtomicBool useSendQueue;
        AtomicBool useRecvQueue;
    }

    ////////////////////////////////////////
    //  fix size message connector       //
    ///////////////////////////////////////
    public class FixedSizeMsgConnector : TCPConnector
    {
        public FixedSizeMsgConnector(int msg_size)
        {
            msg_size_ = msg_size;
        }
        public int GetMessageSize()
        {
            return msg_size_;
        }

        // redefine socket level processing for fixed message handling 
        // only one send and recv system call
        protected override void SendSocketMessage(Message msg)
        {
            SocketUtils.SendAll(socket, msg.get_raw_ref(), SocketFlags.None);
        }

        protected override Message RecvSocketMessage()
        {   
            Message r_msg = new Message((ulong)msg_size_);

            int recv_bytes = SocketUtils.RecvAll(socket, r_msg.get_raw_ref(), SocketFlags.None);
            if(recv_bytes == msg_size_)
            {
                return r_msg;    
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
        private int msg_size_;
    }
}
