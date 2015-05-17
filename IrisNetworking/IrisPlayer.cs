using System;
using System.Collections.Generic;
using System.Text;
using IrisNetworking.Internal;

namespace IrisNetworking
{
    /// <summary>
    /// Represents an connected player.
    /// Will get used in IrisDedicatedServer.
    /// </summary>
    public class IrisPlayer : IrisSerializable
    {
        /// <summary>
        /// The player id of this iris player instance.
        /// </summary>
        public int PlayerId
        {
            get
            {
                return this.playerId;
            }
        }
        private int playerId;

        /// <summary>
        /// The player's name.
        /// </summary>
        public string Name;

        /// <summary>
        /// Ping in milliseconds.
        /// Will get set from the server if the ping refresh thread is running.
        /// </summary>
        public int Ping;

        /// <summary>
        /// Is this player the master client?
        /// </summary>
        public bool isMaster
        {
            get
            {
                return (this.master != null && this.master is IrisDedicatedServer) ? ((this.playerId == 0) ? true : false) : false;
            }
        }

        /// <summary>
        /// The iris master object.
        /// </summary>
        private IrisMaster master;

        /// <summary>
        /// Serialization constructor.
        /// This will construct an empty iris player object with id = 0.
        /// </summary>
        public IrisPlayer()
        {

        }

        /// <summary>
        /// Constructs a new iris player object.
        /// </summary>
        /// <param name="playerId"></param>
        public IrisPlayer (int playerId)
        {
            this.playerId = playerId;
        }

        /// <summary>
        /// Sets the iris master reference.
        /// Should get called after IrisMaster.SetPlayer() was called.
        /// </summary>
        /// <param name="master"></param>
        public void SetMaster(IrisMaster master)
        {
            this.master = master;
        }

        public override bool Equals(object obj)
        {
            if (obj is IrisPlayer)
            {
                return ((IrisPlayer)obj).PlayerId == this.PlayerId;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        public void Serialize(IrisStream stream)
        {
            stream.Serialize(ref this.playerId);
            stream.Serialize(ref this.Name);
        }

        public override string ToString()
        {
            return this.playerId + " " + this.Name;
        }
    }
}
