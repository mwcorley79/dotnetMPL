
using System;
using System.IO;
using System.Text;
using MPL;
using Xunit; // https://xunit.net/

namespace MessageTests
{  
    public class MessageTest
    {
        // reference: https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test
        //            https://xunit.net/docs/getting-started/netcore/cmdline
        // ** test 1 of 7 ***

        // Facts are tests which are always true. They test invariant conditions.

        // Theories are tests which are only true for a particular set of data.

        [Fact]  //The [Fact] attribute declares a test method that's run by the test runner. From the Message.Tests folder
        public void testStringCtor()
        {
            Console.WriteLine("Testing Message ");
            Message msg1 = new Message(200,"This is a simple message from Mike", MessageType.BINARY);

            // test to string function
          //  Assert.True(msg1.ToString() == "This is a simple message from Mike");
           
            //test type accessor 
            Assert.True(msg1.get_type() == MessageType.BINARY);

            //test lengtth accessor
           // Assert.True(msg1.get_content_len() == (ulong) msg1.ToString().Length);
        }

        // ** test 2,3,4 of 7 ***
        // [Theory] represents a suite of tests that execute the same code but have different input arguments.
        // [InlineData] attribute specifies values for those inputs.
        [Theory]
        [InlineData("This is a simple message from Mike")]
        [InlineData("Another string")]
        [InlineData("word")]
        public void testByteCtor(string value)
        {
            Console.WriteLine("Testing byte[] parameterized constuctor...");
            Message msg2 = new Message(200, value, MessageType.DEFAULT);
            byte[] b = Encoding.ASCII.GetBytes(value);

            // Assert.True(b.Length == (int) msg2.get_content_len());

            //use a func delegate bound to lambda to do byte by byte comparison test
            Func<bool> testCtor = new Func<bool>(() =>
           {
               byte[] br = msg2.get_raw_ref();
               
               for (int i = 0; i < b.Length; ++i)
                   if (b[i] != br[i])
                       return false;
               return true;
           });

            //invoke the delegate
           // Assert.True(testCtor.Invoke());

            // perform the same test as above, but convert byte[] to string and compare 
            Assert.True(value == ASCIIEncoding.ASCII.GetString(b));
        }

        // *** test 5,6, 7 of 7 *** 
        [Theory]
        [InlineData(1024)]
        [InlineData(4096)]
       // [InlineData(65535)]
        public void TestFixedSizeMessage(int value)
        {
            //test fixed size message 
            byte[] buf = new byte[value];
            for(int i = 0; i < buf.Length; ++i)
            {
                buf[i] = ASCIIEncoding.ASCII.GetBytes("m")[0];
            }
 /* public Message(usize sz,
              u8[] content_buf,
              usize content_len,
              u8 mtype) : this(sz, mtype) */

            Message m = new Message( (UInt64) value, buf, (UInt64) buf.Length, (byte) MessageType.DEFAULT);
            //Message msg3 = new Message(value, new byte[value], MessageType.DEFAULT);
            Assert.True(m.raw_len() == value);
        }
    }
}