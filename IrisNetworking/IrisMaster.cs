using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking
{
    /// <summary>
    /// The iris master interface.
    /// The iris master handles view creation, registration, updating, deletion and rpc's.
    /// </summary>
    public interface IrisMaster
    {
        /// <summary>
        /// Registers an iris view to an iris master.
        /// </summary>
        void RegisterView(int viewId, IrisView view);

        /// <summary>
        /// Removes the given viewId from registered view.
        /// Announcements, etc. will get handled by iris.
        /// </summary>
        /// <param name="viewId"></param>
        void RemoveView(int viewId);

        /// <summary>
        /// Finds a view for the given id.
        /// Returns null if the view was not found.
        /// </summary>
        /// <param name="viewId"></param>
        /// <returns></returns>
		IrisView FindView(int viewId);

		/// <summary>
		/// Gets all currently registered views.
		/// </summary>
		/// <returns>The views.</returns>
        List<IrisView> GetViews();

        /// <summary>
        /// Gets the current iris player set by SetLocalPlayer.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        IrisPlayer GetLocalPlayer();

        /// <summary>
        /// Sets the current iris player.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        void SetLocalPlayer(IrisPlayer player);

        /// <summary>
        /// Gets the iris player set by SetPlayer.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        IrisPlayer GetPlayer(int playerId);

        /// <summary>
        /// Sets the iris player for the given id.
        /// Setting a player to null means he disconnected, got kicked, what ever. null means he is removed from the game.
        /// You need to call player.SetMaster(this) in here.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        void SetPlayer(int playerId, IrisPlayer player);

        /// <summary>
        /// Returns all currently connected players.
        /// </summary>
        /// <returns></returns>
        List<IrisPlayer> GetPlayers();

        /// <summary>
        /// Gets called if a view creation packet arrives or a server directly creates a view.
        /// You must return the created view object from here.
        /// </summary>
        /// <param name="objectName">Object name.</param>
        /// <param name="viewId">View identifier.</param>
        /// <param name="owner">Owner.</param>
        IrisView SpawnObject(string objectName, int viewId, IrisPlayer owner, byte[] initialState);
    }
}
