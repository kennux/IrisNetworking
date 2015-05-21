using System;

namespace IrisNetworking
{
	public class SerializationException : Exception
	{
		public override String Message
		{
			get { return this._Message; }
		}
		private String _Message;

		public SerializationException(String message)
		{
			this._Message = message;
		}
	}
}

