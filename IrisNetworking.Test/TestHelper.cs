using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IrisNetworking;
using IrisNetworking.Internal;
using System.Threading;

namespace IrisNetworking.Test
{
    public class TestIrisDedicatedServer
    {
        public Thread updateThread;
        public IrisDedicatedServer server;
        public bool isAlive = true;

        public TestIrisDedicatedServer(Thread updateThread, IrisDedicatedServer server)
        {
            this.updateThread = updateThread;
            this.server = server;
        }

        public void Destroy()
        {
            this.updateThread = null;

            this.server.Stop();
        }
    }

    public class TestHelper
    {
        public static TestIrisDedicatedServer CreateDedicatedServer(string ip, short port, IrisMaster master)
        {
            IrisDedicatedServer server = new IrisDedicatedServer(ip, port, 1337, master);
            TestIrisDedicatedServer test = new TestIrisDedicatedServer(null, server);

            Thread updateThread = new Thread(() =>
            {
                while (test.isAlive)
                {
                    server.Update();

                    Thread.Sleep(10);
                }
            });

            updateThread.Start();

            test.updateThread = updateThread;

            return test;
        }
    }
}
