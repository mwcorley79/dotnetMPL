using System;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BQTests
{
    public class BQueueTests
    {
        [Fact]
        public void Test1()
        {
            Console.Write("\n  Testing Monitor-Based Blocking Queue");
            Console.Write("\n ======================================");

            SWTools.BlockingQueue<string> q = new SWTools.BlockingQueue<string>();
            int count = 0; 
            Task t = new Task(() =>
            {
                string msg;
                while (true)
                { 
                    msg = q.deQ(); Console.Write("\n  child thread received {0}", msg);
                   // Assert.True(msg == "msg #" + count.ToString());
                    count++;
                    if (msg == "quit") break;
                }

                //Assert.True(count == 21);
            });
            t.Start();

            string sendMsg = "msg #";
            for (int i = 0; i < 20; ++i)
            {
                string temp = sendMsg + i.ToString();
                Console.Write("\n  main thread sending {0}", temp);
                q.enQ(temp);
            }
            q.enQ("quit");
            t.Wait();
            Console.Write("\n\n");
        }
    }
}
