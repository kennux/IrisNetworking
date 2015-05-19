using System;
using System.Collections.Generic;
using System.Text;
using IrisNetworking;
using IrisNetworking.Sockets;
using IrisNetworking.Internal;

namespace IrisNetworking.Test
{
	public class TestManager : IrisMaster
    {
        private IrisPlayer player;

        /// <summary>
        /// The players dictionary used to keep track of all currently connected players.
        /// </summary>
        private Dictionary<int, IrisPlayer> players = new Dictionary<int, IrisPlayer>();
        private object playersLockObject = new object();

        /// <summary>
        /// The views dictionary used to keep track of all currently spawned views.
        /// </summary>
        private Dictionary<int, IrisView> views = new Dictionary<int, IrisView>();
        private object viewsLockObject = new object();

        public void RegisterView(int viewId, IrisView view)
        {
            IrisConsole.Log(IrisConsole.MessageType.INFO, "TestManager", "Registered view " + view + " with id " + viewId);

            lock (this.viewsLockObject)
            {
                if (views.ContainsKey(viewId))
                {
                    IrisConsole.Log(IrisConsole.MessageType.ERROR, "TestManager", "Tried to register and already existing iris view id = " + viewId);
                    return;
                }
            }

            views.Add(viewId, view);
        }

        /// <summary>
        /// Removes the given viewId from registered view.
        /// Announcements, etc. will get handled by iris.
        /// </summary>
        /// <param name="viewId"></param>
        public void RemoveView(int viewId)
        {
            lock (this.viewsLockObject)
            {
                if (views.ContainsKey(viewId))
                    views.Remove(viewId);
                else
                    IrisConsole.Log(IrisConsole.MessageType.ERROR, "TestManager", "Tried to remove non-existing view object.");
            }
        }

        public List<IrisView> GetViews()
        {
            lock (this.viewsLockObject)
            {
                return new List<IrisView>(views.Values);
            }
        }

        public IrisView FindView(int viewId)
        {
            lock (this.viewsLockObject)
            {
                IrisConsole.Log(IrisConsole.MessageType.DEBUG, "TestManager", "Tried to find view " + viewId);

                if (!views.ContainsKey(viewId))
                {
                    IrisConsole.Log(IrisConsole.MessageType.ERROR, "TestManager", "Tried to find view " + viewId + " but this view is not existing!");
                    return null;
                }

                return views[viewId];
            }
        }

        /// <summary>
        /// Gets the current iris player set by SetPlayer.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public IrisPlayer GetLocalPlayer()
        {
            lock (this.playersLockObject)
            {
                return this.player;
            }
        }

        /// <summary>
        /// Sets the current iris player.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public void SetLocalPlayer(IrisPlayer player)
        {
            lock (this.playersLockObject)
            {
                this.player = player;
                this.SetPlayer(player.PlayerId, player);
            }
        }

        /// <summary>
        /// Gets the iris player set by SetPlayer.
        /// Returns null if the player for the given id was not found.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public IrisPlayer GetPlayer(int playerId)
        {
            lock (this.playersLockObject)
            {
                try
                {
                    return this.players[playerId];
                }
                catch (KeyNotFoundException e)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Sets the iris player for the given id.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public void SetPlayer(int playerId, IrisPlayer player)
        {
            lock (this.playersLockObject)
            {
                if (player != null)
                    player.SetMaster(this);
                else
                {
                    if (this.players.ContainsKey(playerId))
                        this.players.Remove(playerId);
                    return;
                }

                // Set player reference
                if (this.players.ContainsKey(playerId))
                    this.players[playerId] = player;
                else
                    this.players.Add(playerId, player);
            }
        }

        public List<IrisPlayer> GetPlayers()
        {
            lock (this.playersLockObject)
            {
                return new List<IrisPlayer>(this.players.Values);
            }
        }

        /// <summary>
        /// Gets called if a view creation packet arrives.
        /// </summary>
        /// <param name="objectName">Object name.</param>
        /// <param name="viewId">View identifier.</param>
        /// <param name="owner">Owner.</param>
        public IrisView SpawnObject(string objectName, int viewId, IrisPlayer owner, byte[] initialState)
        {
            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "TestManager", "Spawned object " + objectName + " with view id " + viewId + " and owner " + owner.PlayerId + "|" + owner.Name + " with data (hexa-bytes): " + BitConverter.ToString(initialState).Replace("-", " "));

            // Create internal iris view, which is a very basic iris view implementation.
            TestIrisView irisView = new TestIrisView(owner, viewId, objectName);

            // Perform initial deserialization
            IrisStream stream = new IrisStream(this, initialState);
            irisView.Serialize(stream);

            this.RegisterView(viewId, irisView);

            return irisView;
        }
    }
}
