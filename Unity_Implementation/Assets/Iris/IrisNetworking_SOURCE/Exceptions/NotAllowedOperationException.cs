using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking
{
    /// <summary>
    /// Gets thrown by IrisNetworking if you call any api functions without initializing the system.
    /// </summary>
    public class NotAllowedOperationException : Exception
    {

        public override String Message
        {
            get { return this._Message; }
        }
        private String _Message;

        public NotAllowedOperationException(String message)
        {
            this._Message = message;
        }
    }
}
