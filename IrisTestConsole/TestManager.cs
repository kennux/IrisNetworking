using System;
using System.Collections.Generic;
using System.Text;
using IrisNetworking;
using IrisNetworking.Sockets;
using IrisNetworking.Internal;

namespace IrisTestConsole
{
	public class TestManager : IrisMaster
    {
        private IrisPlayer player;

        /// <summary>
        /// The players dictionary used to keep track of all currently connected players.
        /// </summary>
        private Dictionary<int, IrisPlayer> players = new Dictionary<int, IrisPlayer>();

        /// <summary>
        /// The views dictionary used to keep track of all currently spawned views.
        /// </summary>
        private Dictionary<int, IrisView> views = new Dictionary<int, IrisView>();

        public void RegisterView(int viewId, IrisView view)
        {
            IrisConsole.Log(IrisConsole.MessageType.INFO, "TestManager", "Registered view " + view + " with id " + viewId);

            if (views.ContainsKey(viewId))
            {
                IrisConsole.Log(IrisConsole.MessageType.ERROR, "TestManager", "Tried to register and already existing iris view id = " + viewId);
                return;
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
            if (views.ContainsKey(viewId))
                views.Remove(viewId);
            else
                IrisConsole.Log(IrisConsole.MessageType.ERROR, "TestManager", "Tried to remove non-existing view object.");
        }

        public List<IrisView> GetViews()
        {
            return new List<IrisView>(views.Values);
        }

        public IrisView FindView(int viewId)
        {
            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "TestManager", "Tried to find view " + viewId);

            if (!views.ContainsKey(viewId))
            {
                IrisConsole.Log(IrisConsole.MessageType.ERROR, "TestManager", "Tried to find view " + viewId + " but this view is not existing!");
                return null;
            }

            return views[viewId];
        }

        /// <summary>
        /// Gets the current iris player set by SetPlayer.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public IrisPlayer GetLocalPlayer()
        {
            return this.player;
        }

        /// <summary>
        /// Sets the current iris player.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public void SetLocalPlayer(IrisPlayer player)
        {
            this.player = player;
            this.SetPlayer(player.PlayerId, player);
        }

        /// <summary>
        /// Gets the iris player set by SetPlayer.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public IrisPlayer GetPlayer(int playerId)
        {
            return this.players[playerId];
        }

        /// <summary>
        /// Sets the iris player for the given id.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public void SetPlayer(int playerId, IrisPlayer player)
        {
            if (player != null)
                player.SetMaster(this);

            // Set player reference
            if (this.players.ContainsKey(playerId))
                this.players[playerId] = player;
            else
                this.players.Add(playerId, player);
        }

        public List<IrisPlayer> GetPlayers()
        {
            return new List<IrisPlayer>(this.players.Values);
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
