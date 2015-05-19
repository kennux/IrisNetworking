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
            this.isAlive = false;

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

                    // Prepare frame update packet by first collecting all view information
                    List<IrisViewUpdate> updates = new List<IrisViewUpdate>();
                    List<IrisView> views = master.GetViews();
                    IrisStream stream = new IrisStream(master);

                    foreach (IrisView v in views)
                    {
                        // Create update
                        IrisViewUpdate update = new IrisViewUpdate();
                        update.viewId = v.GetViewId();

                        // Get state
                        v.Serialize(stream);

                        // Write state
                        update.state = stream.GetBytes();

                        // Clear stream again
                        stream.Clear();

                        updates.Add(update);
                    }

                    IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisNetwork", "Server / Master sent frame update with " + views.Count + " view updates");

                    // Now, let's cull out view updates for each players.
                    foreach (IrisPlayer p in master.GetPlayers())
                    {
                        // Perform the culling
                        // Every user will just get updates for views which aren't owned by him
                        IrisViewUpdate[] viewUpdates = updates.FindAll((u) => IrisNetwork.FindView(u.viewId).GetOwner() != p).ToArray();

                        server.SendMessageToPlayer(p, new IrisFrameUpdateMessage(master.GetLocalPlayer(), viewUpdates, master));
                    }

                    Thread.Sleep(10);
                }
            });

            updateThread.Start();
            test.updateThread = updateThread;

            return test;
        }
    }
}
