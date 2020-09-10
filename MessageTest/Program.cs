using System;
using System.IO;
using System.Text;
using MPL;

namespace MPL_Tests
{  
    public class Program
    {
        public static bool test()
        {
            try
            {
                Console.WriteLine("Testing Message class");

                Console.Write("Testing string parameterized constuctor...");
                Message msg1 = new Message("This is a simple message from Mike", MessageType.BINARY);
                if (msg1.ToString() != "This is a simple message from Mike")
                {
                    Console.WriteLine("Failed");
                    return false;
                }
                else
                    Console.WriteLine("Passed!");

                Console.Write("Testing byte[] parameterized constuctor...");
                Message msg2 = new Message("This is a simple message from Mike", MessageType.DEFAULT);
                byte[] b = Encoding.ASCII.GetBytes("This is a simple message from Mike");

                if (b.Length != msg2.Length)
                {
                    Console.WriteLine("Failed");
                    return false;
                }
                
                for(int i = 0; i < b.Length; ++i)    
                {
                    if (b[i] != msg2.GetData[i])
                    {
                        Console.WriteLine("Failed");
                        return false;
                    }
                }
               
                Console.WriteLine("Passed!");


                Console.Write("Testing Type Accessor function...");
                if (msg1.Type != MessageType.BINARY || msg2.Type != MessageType.DEFAULT)
                {
                    Console.WriteLine("Failed");
                    return false;
                }
                else
                    Console.WriteLine("Passed!");

                Console.WriteLine("All tests passed!");
            }
            catch
            {
                return false;
            }

            return true;
        }


        static void Main(string[] args)
        {
            Program.test();
        }
    }
}
