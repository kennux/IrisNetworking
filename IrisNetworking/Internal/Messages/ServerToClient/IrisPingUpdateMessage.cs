using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// This is the ping update message.
    /// It will get broadcasted on the server by the ping thread.
    /// It conatins all currently connected player ids and their ping's.
    /// 
    /// PacketID = 11
    /// </summary>
    class IrisPingUpdateMessage : IrisServerToClientMessage
	{
        public int[] playerIds;

        public int[] playerPings;

        public IrisPingUpdateMessage(IrisPlayer sender, IrisPlayer[] players)
            : base(sender)
        {
            if (players == null)
                return;

            List<int> playerIds = new List<int>();
            List<int> playerPings = new List<int>();

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null)
                {
                    playerIds.Add(players[i].PlayerId);
                    playerPings.Add(players[i].Ping);
                }
            }

            this.playerIds = playerIds.ToArray();
            this.playerPings = playerPings.ToArray();
        }

        public override byte GetPacketId()
        {
            return (byte)11;
        }

        public override void Serialize(IrisStream stream)
        {
            stream.Serialize(ref this.playerIds);
            stream.Serialize(ref this.playerPings);
        }
    }
}
