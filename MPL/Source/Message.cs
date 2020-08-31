using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace MPL
{
    public static class MessageType
    {
        public const int DEFAULT = 0;
        public const int DISCONNECT = -1;
        public const int STOP_SENDING = -2;
        public const int STRING = -3;
        public const int BINARY = -4;
    }

    public class MSGHEADER
    {
        private UInt32 data = 0;

        public MSGHEADER(uint l, uint t)
        {
            // store "length" in upper half of the word,
            // store "type" in the lower half   
            data = (l << 16) | (0x0000FFFF & t);
        }

        public Int16 Type()
        {
            return (Int16)(data & 0x0000FFFF);
        }

        public int Len()
        {
            return (int) (data & 0xFFFF0000) >> 16;
        }

        public MSGHEADER(byte[] raw_hdr, bool read_from_network_socket)
        {
            // https://docs.microsoft.com/en-us/dotnet/api/system.bitconverter?view=netcore-3.1
            // if the desination (this) architecture is little endian, and the source of the serialized header
            // is from the network, then need to convert serialized header from big endian to little
            // endian before it is deserialized into a MSGHEADER object
            if (BitConverter.IsLittleEndian && read_from_network_socket)
            {
               Array.Reverse(raw_hdr);
            }

            data = BitConverter.ToUInt32(raw_hdr, 0);
        }

        public byte[] ToNetworkByteOrder()
        {
            return SerializeHeader(this, true);
        }

        public static byte[] SerializeHeader(MSGHEADER hdr, bool write_to_network_socket)
        {
            byte[] bytes = BitConverter.GetBytes(hdr.data);
            // https://docs.microsoft.com/en-us/dotnet/api/system.bitconverter?view=netcore-3.1
            // if the source architecture is liitle endian, and the destination is a network
            // socket, then need to serialize the header in big endian order prior to writing
            // into the socket
            if (BitConverter.IsLittleEndian && write_to_network_socket)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        // call on send operation to ensure data is written to the network in beg endian
        // call on receive operation to ensure data (coming in from the network) is coverted back the host ordering
        public void ConvertToAppropriateByteOrder()
        {
            byte[] bytes = BitConverter.GetBytes(data);
            ConvertToAppropriateByteOrder(bytes);
        }

        public void ConvertToAppropriateByteOrder(byte[] raw_hdr)
        {
            // https://docs.microsoft.com/en-us/dotnet/api/system.bitconverter?view=netcore-3.1
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(raw_hdr);
                data = BitConverter.ToUInt32(raw_hdr, 0);
            }
        }

        public byte[] ToBytes()
        {
            return BitConverter.GetBytes(data);
        }

        public static int SIZE
        {
            get
            {
                return sizeof(uint);
            }
        }
    }

    public class Message
    {
        public Message(byte[] data, int type)
        {
            data_ = data;
            if(data_ != null)
               hdr = new MSGHEADER((uint)data.Length, (uint)type);
            else
               hdr = new MSGHEADER(0, (uint)type);
        }
 
        public Message(string str, int type): this(Encoding.ASCII.GetBytes(str), type)
        { }


        public Message(int type)
        {
            hdr = new MSGHEADER(0, (uint)type);
            data_ = null;
        }

        public int Type => hdr.Type();

        public byte[] GetData => data_;

        public int Length => hdr.Len();

        public MSGHEADER GetHeader => hdr;

        public override string ToString()
        {
            if(data_ != null)
              return Encoding.ASCII.GetString(data_);
            return string.Empty;
        }

        private byte[] data_;
        private MSGHEADER hdr;       
    }
}

/* 
 * save (code example syntax)
 * 
 *  // make message large enough for the data and the fixed size header
            raw_msg_ = new byte[data_.Length + MSGHEADER.SIZE];

            // store a local copy of the hdr (logical immutability), to save from excessive endianess conversions
            shadow_hdr = new MSGHEADER((uint)data_.Length, (uint) type);

            //serialize the shadow header and copy into the message envelope
            byte[] serialized_hdr = shadow_hdr.ToBytes();
            serialized_hdr.CopyTo(raw_msg_, 0);

            //copy the message data to message envelope
            data_.CopyTo(raw_msg_, serialized_hdr.Length); 

        Message (byte[] data, bool read_from_network_socket)
        {
            // make message large enough for the data and the fixed size header
            raw_msg_ = data;

            // extract the raw header and 
            ArraySegment<byte> raw_hdr = new ArraySegment<byte>(data, 0, MSGHEADER.SIZE);
            shadow_hdr = new MSGHEADER(raw_hdr.Array);
            
            
                
            byte[] reversArray = hdr.Array;
           
            if(endianness)
              Array.Reverse(reversArray);
           

            Buffer.BlockCopy(data, 0, hdr_bytes, 0, (int)MSGHEADER.SIZE);


            ArraySegment<byte> hdr = new ArraySegment<byte>(data, 0, MSGHEADER.SIZE);
            
            shadow_hdr = new MSGHEADER(hdr.Array, endianness);


            ArraySegment<byte>  = new ArraySegment<byte>(data, 0, MSGHEADER.SIZE);

            // create buffer to store the message header
            byte[] hdr_bytes = new byte[MSGHEADER.SIZE];

            Buffer.BlockCopy(data, 0, hdr_bytes, 0, (int)MSGHEADER.SIZE);
            
        }

 */