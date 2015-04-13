using System;

namespace fastJSON
{
	/// <summary>
	/// An exception thrown during serialization or deserialization.
	/// </summary>
	[Serializable]
	public class JsonSerializationException : Exception
	{
		public JsonSerializationException () { }
		public JsonSerializationException (string message) : base (message) { }
		public JsonSerializationException (string message, Exception inner) : base (message, inner) { }
		protected JsonSerializationException (
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base (info, context) { }
	}
}
