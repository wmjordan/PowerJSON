using System;

namespace fastJSON
{
	/// <summary>
	/// An exception thrown during serialization or deserialization.
	/// </summary>
	[Serializable]
	public class JsonSerializationException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JsonSerializationException"/> class.
		/// </summary>
		public JsonSerializationException () { }
		/// <summary>
		/// Initializes a new instance of the <see cref="JsonSerializationException"/> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public JsonSerializationException (string message) : base (message) { }
		/// <summary>
		/// Initializes a new instance of the <see cref="JsonSerializationException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="inner">The inner exception.</param>
		public JsonSerializationException (string message, Exception inner) : base (message, inner) { }
		/// <summary>
		/// Initializes a new instance of the <see cref="JsonSerializationException"/> class.
		/// </summary>
		/// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
		protected JsonSerializationException (
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base (info, context) { }
	}

	/// <summary>
	/// An exception thrown when parser encounters malform JSON string.
	/// </summary>
	[Serializable]
	public class JsonParseException : JsonSerializationException
	{
		/// <summary>
		/// Gets the index where parse error occurs.
		/// </summary>
		public int Position { get; private set; }
		/// <summary>
		/// Gets the context text around the error position.
		/// </summary>
		public string ContextText { get; private set; }

		internal JsonParseException (string reason, int index, string context) : base (
			String.Concat (reason, index, ": ", context)
			) {
			Position = index;
			ContextText = context;
		}
	}
}
