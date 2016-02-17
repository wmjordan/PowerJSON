using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace PowerJson.Extensions
{

	/// <summary>
	/// A serializer that directly writes out JSON from <see cref="IDataReader"/>.
	/// </summary>
	public static class DataReaderSerializer
	{
		/// <summary>
		/// Writes the JSON array representation from an <see cref="IDataReader"/> to the output <paramref name="target" />.
		/// </summary>
		/// <param name="data">The data to be serialized.</param>
		/// <param name="target">The output target.</param>
		/// <param name="manager">The <see cref="SerializationManager"/> to control advanced JSON serialization.</param>
		public static void WriteAsDataArray (this IDataReader data, TextWriter target, SerializationManager manager) {
			var js = new JsonSerializer (manager, target);
			target.Write (JsonSerializer.StartArray);
			var l = data.FieldCount;
			var fs = new WriteJsonValue[l];
			for (int i = l - 1; i >= 0; i--) {
				fs[i] = manager.GetSerializationInfo (data.GetFieldType (i)).SerializeMethod;
			}
			var c = -1;
			object v;
			while (data.Read ()) {
				if (++c > 0) {
					target.Write (JsonSerializer.Separator);
				}
				target.Write (JsonSerializer.StartArray);
				for (int i = 0; i < l; i++) {
					if (i > 0) {
						target.Write (JsonSerializer.Separator);
					}
					v = data.GetValue (i);
					if (v == null) {
						target.Write (JsonSerializer.Null);
					}
					else {
						fs[i] (js, v);
					}
				}
				target.Write (JsonSerializer.EndArray);
			}
			target.Write (JsonSerializer.EndArray);
		}
	}
}
