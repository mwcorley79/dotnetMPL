using System;
using MPL;

namespace MessageTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Test Message Class");

            Console.WriteLine("Test parameterized Message constructor for string");
            Message msg1 = new Message("This is a simple message from Mike", MessageType.DEFAULT);
            Console.WriteLine("The string is: {0}, Message type is: {1}", msg1, msg1.Type);

            Message StopMsg = new Message(MessageType.STOP_SENDING);

        }
    }
}
