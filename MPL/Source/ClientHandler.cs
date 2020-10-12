using SWTools;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MPL
{
    public abstract class ClientHandler
    {
        public ClientHandler()
        {
            sendQ_ = new BlockingQueue<Message>();
            recvQ_ = new BlockingQueue<Message>();

            isReceiving = new AtomicBool();
            isSending = new AtomicBool();

            service_ep_ = null;

            client_socket = null;
        }

        public abstract ClientHandler Clone();
        public abstract void AppProc();

        public EndPoint RemoteEP => client_socket.RemoteEndPoint;

        public void SetServiceEndPoint(EndPoint ep) => service_ep_ = ep;

        public EndPoint GetServiceEndPoint => service_ep_;

        public void SetSocket(Socket sock)
        {
            client_socket = sock;
        }

        public void Close()
        {
            client_socket.Close();
        }

        public Socket GetDataSocket()
        {
            return client_socket;
        }

        public void PostMessage(Message msg)
        {
            sendQ_.enQ(msg);
        }

        public void SendMessage(Message m)
        {
            SendSocketMessage(m);
        }

        protected virtual void SendSocketMessage(Message msg)
        {
             // send the header
            SocketUtils.SendAll(client_socket, msg.get_header_bytes(), SocketFlags.None);

            // send the data
            SocketUtils.SendAll(client_socket, msg.get_content_bytes(), SocketFlags.None);
        }

        protected virtual void SendProc()
        {
            try
            {
                Message msg = sendQ_.deQ();
                while (msg.get_type() != MessageType.STOP_SENDING)
                {
                    // serialize the message into the socket
                    SendSocketMessage(msg);

                    msg = sendQ_.deQ();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("ClientHandler::Sendproc() " + ex.Message);
            }
        }

           
        public void StartSending()
        {
            if (!isSending.get())
            {
                isSending.set(true);
                //sendTask = Task.Run(SendProc);
                sendThread = new Thread(SendProc);
                sendThread.Start();
            }
        }

        public void StopSending()
        {
            try
            {
                if (isSending.get())
                {
                    PostMessage(new Message(MessageType.STOP_SENDING));
                    sendThread.Join();
                    //sendTask.Wait();
                    isSending.set(false);
                }
            }
            catch
            {
                isSending.set(false);
            } 
        }

        public void ShutdownSend()
        {
            client_socket.Shutdown(SocketShutdown.Send);
        }
        
        public void ShutdownRecv()
        {
            client_socket.Shutdown(SocketShutdown.Receive);
        }

        protected virtual void RecvProc()
        {
            Message msg = null;
            try
            {        
                do
                {
                    msg = RecvSocketMessage();
                    recvQ_.enQ(msg);
                }
                while (msg.get_type() != MessageType.DISCONNECT);
            }
            catch(Exception ex)
            {
                Console.WriteLine("ClientHandler::RecvProc(): {0} Message len: {1}", ex.Message, msg.get_content_len());
            }
        }

        public void StartReceiving()
        {
            if (!isReceiving.get())
            {
                isReceiving.set(true);
                //recvTask = Task.Run(RecvProc);
                recvThread = new Thread(RecvProc);
                recvThread.Start();
            }
        }

        public void StopReceiving()
        {
            try
            {
                if (isReceiving.get())
                {
                    //recvTask.Wait();
                    recvThread.Join();
                    isReceiving.set(false);
                }
            }
            catch
            {
                isReceiving.set(false);
            }
        }


        public Message GetMessage()
        {
            return recvQ_.deQ();
        }

        public Message ReceiveMessage()
        {
            return RecvSocketMessage();
        }

       
        // serialize the message header and message and write them into the socket
        protected virtual Message RecvSocketMessage()
        {    
            byte[] hdr_bytes = new byte[Message.get_header_len()];
            int recv_bytes;

            // receive fixed size message header (see wire protocol in Message.h)
            if ((recv_bytes = SocketUtils.RecvAll(client_socket, hdr_bytes, SocketFlags.None)) == (int) Message.get_header_len())
            {
                // construct a Message using the Message header read from the socket channel
                // *** critical that mhdr is host byte order (e.g. Intel CPU == little endian) ***
                Message msg = new Message(hdr_bytes);

                // recv message data
                if (SocketUtils.RecvAll(client_socket, msg.get_content_bytes(), SocketFlags.None) != (int) msg.get_content_len())
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
            

        private Socket client_socket;
        //private Task sendTask;
        //private Task recvTask;
        private Thread sendThread;
        private Thread recvThread;
        private BlockingQueue<Message> sendQ_;
        private BlockingQueue<Message> recvQ_;
        private AtomicBool isSending;
        private AtomicBool isReceiving;
        EndPoint service_ep_;
    }

    ////////////////////////////////////////
    //  fix size message client handler   //
    ///////////////////////////////////////
    public abstract class FixedSizeMsgClientHander : ClientHandler
    {
        public FixedSizeMsgClientHander(int msg_size)
        {
            msg_size_ = msg_size;
        }

        protected override Message RecvSocketMessage()
        {
             Message r_msg = new Message((ulong)msg_size_);

            int recv_bytes = SocketUtils.RecvAll(GetDataSocket(), r_msg.get_raw_ref(), SocketFlags.None);
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

        protected override void SendSocketMessage(Message msg)
        {
            SocketUtils.SendAll(GetDataSocket(), msg.get_raw_ref(), SocketFlags.None);
        }

        public int GetMessageSize()
        {
            return msg_size_;
        }

        private int msg_size_;
        // redefine socket level processing for fixed message handling 
        // only one send and recv system call
    };
}
