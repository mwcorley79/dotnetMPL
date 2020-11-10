using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace MPL
{
    using usize = UInt64;
    using u8 = Byte;

    public static class MessageType
    {
        public const u8 DEFAULT = 0;
        public const u8 DISCONNECT = 8;
        public const u8 STOP_SENDING = 32;
        public const u8 STRING = 64;
        public const u8 BINARY = 128;
    }

    public class Message
    {
        const int TYPE_SIZE = 1;
        const int TYPE_INDEX = 0;
        const int CONTENT_SIZE = sizeof(usize);
        const int CONTENT_SIZE_INDEX = 1;
        const int HEADER_SIZE = TYPE_SIZE + CONTENT_SIZE;

        public Message(usize sz, u8 mtype = MessageType.DEFAULT)
        {
            br = new u8[sz];
            set_type(mtype);
            set_content_len(0);
        }

        public Message(usize sz,
              u8[] content_buf,
              usize content_len,
              u8 mtype) : this(sz, mtype)
        {
            // if(br_len_ < HEADER_SIZE)
            //   throw std::invalid_argument("message size must be greater than equal to the header size");    
            set_type(mtype);
            set_content_bytes(content_buf);
        }

        // constructs a default (empty content) message with min HEADER_SIZE
        public Message(u8 mtype = MessageType.DEFAULT) : this(HEADER_SIZE, mtype)
        {
        }

        public Message(usize sz,
               string str,
               u8 mtype) : this(sz, Encoding.ASCII.GetBytes(str), (usize)str.Length, mtype)
        { }

        bool is_empty()
        {
            return (br.Length == 0);
        }

        public u8 get_type()
        {
            return br[TYPE_INDEX];
        }

        public void set_type(u8 mt)
        {
            br[TYPE_INDEX] = mt;
        }

        private usize GetHeaderLen(u8[] header)
        {
            // extract the content size region of the header
            ArraySegment<byte> content_size_segment =
                            new ArraySegment<byte>(header, TYPE_SIZE, CONTENT_SIZE);

            byte[] content_size_bytes = content_size_segment.Array;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(content_size_bytes, 0, CONTENT_SIZE);
            }

            return BitConverter.ToUInt64(content_size_bytes, 0);
        }


        public Message(u8[] header)
        {
            usize br_len = HEADER_SIZE + GetHeaderLen(header);
            br = new u8[br_len];
            Buffer.BlockCopy(header, 0, br, 0, HEADER_SIZE);
        }

        public uint raw_len()
        {
            return (uint)br.Length;
        }

        public uint max_content_len()
        {
            return (raw_len() - HEADER_SIZE);
        }

        public usize get_content_len()
        {
            return GetHeaderLen(br);
        }

        public void set_content_len(usize sz)
        {
            u8[] content_bytes = BitConverter.GetBytes(sz);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(content_bytes, 0, content_bytes.Length);
            }
            Buffer.BlockCopy(content_bytes, 0, br, CONTENT_SIZE_INDEX, content_bytes.Length);
        }

        /*-------------------------------------------
      Set message content from buff and set
      content size to length of buff
   */
        public void set_content_bytes(u8[] buf)
        {
            usize clen = max_content_len();
            if (buf.Length < (int)clen)
            {
                Buffer.BlockCopy(buf, 0, br, HEADER_SIZE, buf.Length);
                set_content_len((usize)buf.Length);
            }
            else
            {
                Buffer.BlockCopy(buf, 0, br, HEADER_SIZE, (int)clen);
                set_content_len(clen);
            }
        }

        public void set_content_str(string str)
        {
            set_content_bytes(ASCIIEncoding.ASCII.GetBytes(str));
        }

        public static usize get_header_len()
        {
            return HEADER_SIZE;
        }

        public u8[] get_content_bytes()
        {
            // extract the content size region of the header
            ArraySegment<byte> content_size_segment =
                            new ArraySegment<byte>(br, HEADER_SIZE, (int) get_content_len());
            return content_size_segment.Array;
        }


        public u8[] get_header_bytes()
        {
            // extract the content size region of the header
            ArraySegment<byte> header_segment =
                            new ArraySegment<byte>(br, 0, HEADER_SIZE);
            return header_segment.Array;
        }

        
        public void init_content(u8 val = 0)
        {
            uint clen = max_content_len();
            for (uint i = 0; i < clen; ++i)
            {
                br[HEADER_SIZE + i] = val;
            }

            set_content_len(max_content_len());
        }

        public u8[] get_raw_ref()
        {
            return br;
        }


        private u8[] br;
    }
}

/* 
 public Message(string str, int type) : this(Encoding.ASCII.GetBytes(str), type)
  { }

  public Message(MSGHEADER mhdr)
  {
      hdr = mhdr;
      data_ = new byte[mhdr.Len()];
  }

  public Message(MSGHEADER mhdr, byte[] data)
  {
      hdr = mhdr;
      data_ = data;
  }

  public Message(int type)
  {
      hdr = new MSGHEADER(0, (uint)type);
      data_ = null;
  }

  public int Type => hdr.Type();

  public byte[] GetData => data_;

  public byte[] GetInternalDataBuf()
  {
      return data_;
  }

  public int Length => hdr.Len();

  public MSGHEADER GetHeader => hdr;


  public override string ToString()
  {
      if (data_ != null)
          return Encoding.ASCII.GetString(data_, 0, Length);
      return string.Empty;
  }

  public Message(int size, byte[] buf, int type)
  {
      if (buf != null)
      {
          data_ = new byte[size + MSGHEADER.SIZE];
          hdr = new MSGHEADER((uint) buf.Length, (uint)type);

          Buffer.BlockCopy(hdr.ToNetworkByteOrder(), 0, data_, 0, MSGHEADER.SIZE);
          Buffer.BlockCopy(buf, 0, data_, MSGHEADER.SIZE, buf.Length);
      }

  }

  private byte[] data_;
  private MSGHEADER hdr;
}
*/


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

 /* public class MSGHEADER
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
                return sizeof(UInt32);
            }
        }
    }
    */