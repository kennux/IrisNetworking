using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking
{
    /// <summary>
    /// Connection failed exception will get thrown if a socket connection establishment failed.
    /// </summary>
    public class ConnectionFailedException : Exception
    {

        public override String Message
        {
            get { return this._Message; }
        }
        private String _Message;

        public ConnectionFailedException(String ip, short port)
        {
            this._Message = "Connection to " + ip + ":" + port + " failed!";
        }
    }
}
