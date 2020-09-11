
using System;
using System.IO;
using System.Text;
using MPL;
using Xunit; // https://xunit.net/

namespace MessageTests
{  
    public class MessageTest
    {
        [Fact]  //The [Fact] attribute declares a test method that's run by the test runner. From the Message.Tests folder
        public void testStringCtor()
        {
            Console.Write("Testing Message constructor for string messages");
            Message msg1 = new Message("This is a simple message from Mike", MessageType.BINARY);

            Assert.True(msg1.ToString() == "This is a simple message from Mike");
            Assert.True(msg1.Type == MessageType.BINARY);                  
        }

    }
}