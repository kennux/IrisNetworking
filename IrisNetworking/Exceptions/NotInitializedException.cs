using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking
{
    /// <summary>
    /// Gets thrown by IrisNetworking if you call any api functions without initializing the system.
    /// </summary>
    public class NotInitializedException : Exception
    {

        public override String Message
        {
            get { return this._Message; }
        }
        private String _Message;

        public NotInitializedException(String message)
        {
            this._Message = message;
        }
    }
}
