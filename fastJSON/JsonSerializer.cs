using System;
using System.Collections;
using System.Collections.Generic;
#if !SILVERLIGHT
using System.Data;
#endif
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Specialized;

namespace fastJSON
{
	internal sealed class JSONSerializer
	{
		StringBuilder _output = new StringBuilder ();
		StringBuilder _before = new StringBuilder ();
		readonly int _MAX_DEPTH = 20;
		int _current_depth = 0;
		readonly Dictionary<string, int> _globalTypes = new Dictionary<string, int> ();
		readonly Dictionary<object, int> _cirobj = new Dictionary<object, int> ();
		readonly JSONParameters _params;
		readonly bool _useEscapedUnicode = false;
		readonly SerializationManager _manager;

		internal JSONSerializer (JSONParameters param, SerializationManager manager) {
			_params = param;
			_useEscapedUnicode = _params.UseEscapedUnicode;
			_MAX_DEPTH = _params.SerializerMaxDepth;
			_manager = manager;
		}

		internal string ConvertToJSON (object obj) {
			WriteValue (obj);

			if (_params.UsingGlobalTypes && _globalTypes != null && _globalTypes.Count > 0) {
				var sb = _before;
				sb.Append ("\"$types\":{");
				var pendingSeparator = false;
				foreach (var kv in _globalTypes) {
					if (pendingSeparator) sb.Append (',');
					pendingSeparator = true;
					sb.Append ('\"');
					sb.Append (kv.Key);
					sb.Append ("\":\"");
					sb.Append (kv.Value);
					sb.Append ('\"');
				}
				sb.Append ("},");
				sb.Append (_output.ToString ());
				return sb.ToString ();
			}
			else
				return _output.ToString ();
		}

		private void WriteValue (object obj) {
			if (obj == null || obj is DBNull)
				_output.Append ("null");

			else if (obj is string || obj is char) {
				if (_useEscapedUnicode) {
					WriteStringEscapeUnicode (_output, obj.ToString ());
				}
				else {
					WriteString (_output, obj.ToString ());
				}
			}
			else if (obj is bool)
				_output.Append (((bool)obj) ? "true" : "false"); // conform to standard
			else if (obj is int) {
				_output.Append (Int32ToString ((int)obj));
			}
			else if (
				obj is long || obj is double ||
				obj is decimal || obj is float ||
				obj is byte || obj is short ||
				obj is sbyte || obj is ushort ||
				obj is uint || obj is ulong
			)
				_output.Append (((IConvertible)obj).ToString (NumberFormatInfo.InvariantInfo));

			else if (obj is DateTime)
				WriteDateTime (this, obj);

			else if (obj is Guid)
				WriteGuid (this, obj);

			else if (obj is TimeSpan) {
				WriteTimeSpan (this, obj);
			}
			else if (_params.KVStyleStringDictionary == false && obj is IDictionary &&
				obj.GetType ().IsGenericType && obj.GetType ().GetGenericArguments ()[0] == typeof(string))

				WriteStringDictionary ((IDictionary)obj);
#if NET_40_OR_GREATER
			else if (_params.KVStyleStringDictionary == false && obj is System.Dynamic.ExpandoObject)
				WriteStringDictionary ((IDictionary<string, object>)obj);
#endif
			else if (obj is IDictionary)
				WriteDictionary ((IDictionary)obj);
#if !SILVERLIGHT
			else if (obj is DataSet)
				WriteDataset ((DataSet)obj);

			else if (obj is DataTable)
				WriteDataTable ((DataTable)obj);
#endif
			else if (obj is byte[])
				WriteBytes ((byte[])obj);

			else if (obj is StringDictionary)
				WriteSD ((StringDictionary)obj);

			else if (obj is NameValueCollection) {
				WriteNameValueCollection ((NameValueCollection)obj);
			}

			else if (obj is IEnumerable)
				WriteArray (this, obj);

			else if (obj is Enum)
				WriteEnum (this, obj);

			else if (_manager.IsTypeRegistered (obj.GetType ()))
				WriteCustom (obj);

			else
				WriteObject (obj);
		}

		private void WriteSD (StringDictionary stringDictionary) {
			_output.Append ('{');

			var pendingSeparator = false;

			foreach (DictionaryEntry entry in stringDictionary) {
				if (_params.SerializeNullValues == false && entry.Value == null) {
				}
				else {
					if (pendingSeparator) _output.Append (',');

					WritePair (_params.NamingStrategy.Rename ((string)entry.Key), entry.Value);
					pendingSeparator = true;
				}
			}
			_output.Append ('}');
		}

		private void WriteCustom (object obj) {
			Serialize s = _manager.GetCustomSerializer (obj.GetType ());
			WriteStringFast (s (obj));
		}

		private void WriteBytes (byte[] bytes) {
#if !SILVERLIGHT
			WriteStringFast (Convert.ToBase64String (bytes, 0, bytes.Length, Base64FormattingOptions.None));
#else
			WriteStringFast(Convert.ToBase64String(bytes, 0, bytes.Length));
#endif
		}

#if !SILVERLIGHT
		private DatasetSchema GetSchema (DataTable ds) {
			if (ds == null) return null;

			var m = new DatasetSchema
			{
				Info = new List<string> (),
				Name = ds.TableName
			};

			foreach (DataColumn c in ds.Columns) {
				m.Info.Add (ds.TableName);
				m.Info.Add (c.ColumnName);
				m.Info.Add (c.DataType.ToString ());
			}
			// FEATURE : serialize relations and constraints here

			return m;
		}

		private DatasetSchema GetSchema (DataSet ds) {
			if (ds == null) return null;

			var m = new DatasetSchema
			{
				Info = new List<string> (),
				Name = ds.DataSetName
			};

			foreach (DataTable t in ds.Tables) {
				foreach (DataColumn c in t.Columns) {
					m.Info.Add (t.TableName);
					m.Info.Add (c.ColumnName);
					m.Info.Add (c.DataType.ToString ());
				}
			}
			// FEATURE : serialize relations and constraints here

			return m;
		}

		private string GetXmlSchema (DataTable dt) {
			using (var writer = new StringWriter ()) {
				dt.WriteXmlSchema (writer);
				return dt.ToString ();
			}
		}

		private void WriteDataset (DataSet ds) {
			_output.Append ('{');
			if (_params.UseExtensions) {
				WritePair ("$schema", _params.UseOptimizedDatasetSchema ? (object)GetSchema (ds) : ds.GetXmlSchema ());
				_output.Append (',');
			}
			var tablesep = false;
			foreach (DataTable table in ds.Tables) {
				if (tablesep) _output.Append (',');
				tablesep = true;
				WriteDataTableData (table);
			}
			// end dataset
			_output.Append ('}');
		}

		private void WriteDataTableData (DataTable table) {
			_output.Append ('\"');
			_output.Append (table.TableName);
			_output.Append ("\":[");
			var cols = table.Columns;
			var rowseparator = false;
			foreach (DataRow row in table.Rows) {
				if (rowseparator) _output.Append (',');
				rowseparator = true;
				_output.Append ('[');

				var pendingSeperator = false;
				foreach (DataColumn column in cols) {
					if (pendingSeperator) _output.Append (',');
					WriteValue (row[column]);
					pendingSeperator = true;
				}
				_output.Append (']');
			}

			_output.Append (']');
		}

		void WriteDataTable (DataTable dt) {
			_output.Append ('{');
			if (_params.UseExtensions) {
				WritePair ("$schema", _params.UseOptimizedDatasetSchema ? (object)GetSchema (dt) : GetXmlSchema (dt));
				_output.Append (',');
			}

			WriteDataTableData (dt);

			_output.Append ('}');
		}
#endif

		bool _TypesWritten = false;
		private void WriteObject (object obj) {
			var ci = 0;
			if (_cirobj.TryGetValue (obj, out ci) == false)
				_cirobj.Add (obj, _cirobj.Count + 1);
			else {
				if (_current_depth > 0 && _params.InlineCircularReferences == false) {
					//_circular = true;
					_output.Append ("{\"$i\":");
					_output.Append (Int32ToString (ci));
					_output.Append ("}");
					return;
				}
			}
			var def = _manager.GetDefinition (obj.GetType ());
			var si = def.Interceptor;
			if (si != null && si.OnSerializing (obj) == false) {
				return;
			}
			if (_params.UsingGlobalTypes == false)
				_output.Append ('{');
			else {
				if (_TypesWritten == false) {
					_output.Append ('{');
					_before = _output;
					_output = new StringBuilder ();
				}
				else
					_output.Append ('{');
			}
			_TypesWritten = true;
			_current_depth++;
			if (_current_depth > _MAX_DEPTH)
				throw new JsonSerializationException ("Serializer encountered maximum depth of " + _MAX_DEPTH);


			var map = new Dictionary<string, string> ();
			var append = false;
			if (_params.UseExtensions) {
				if (_params.UsingGlobalTypes == false)
					WritePairFast ("$type", def.AssemblyName);
				else {
					var dt = 0;
					var ct = def.AssemblyName;
					if (_globalTypes.TryGetValue (ct, out dt) == false) {
						dt = _globalTypes.Count + 1;
						_globalTypes.Add (ct, dt);
					}
					WritePairFast ("$type", dt.ToString ());
				}
				append = true;
			}

			var g = def.Getters;
			var c = g.Length;
			var rp = _params.ShowReadOnlyProperties;
			for (int ii = 0; ii < c; ii++) {
				var p = g[ii];
				if (p.Serializable == TriState.False) {
					continue;
				}
				if (p.Serializable == TriState.Default) {
					if (p.IsStatic && _params.SerializeStaticMembers == false
						|| p.IsReadOnly && (p.IsProperty && rp == false || p.IsProperty == false && _params.ShowReadOnlyFields == false)) {
						continue;
					}
				}
				var o = p.Getter (obj);
				var n = p.MemberName;
				if (si != null && si.OnSerializing (obj, ref n, ref o) == false) {
					continue;
				}
				if (p.Converter != null) {
					o = p.Converter.SerializationConvert (n, o);
				}
				if (p.ItemConverter != null && o is IEnumerable) {
					var ol = new List<object> ();
					foreach (var item in (o as IEnumerable)) {
						ol.Add (p.ItemConverter.SerializationConvert (n, item));
					}
					o = ol;
				}
				if (p.SpecificName) {
					if (o == null || p.TypedNames == null || p.TypedNames.TryGetValue (o.GetType (), out n) == false) {
						n = p.SerializedName;
					}
				}
				else {
					n = p.SerializedName;
				}
				if (_params.SerializeNullValues == false && (o == null || o is DBNull)) {
					continue;
				}
				if (p.HasDefaultValue && Equals (o, p.DefaultValue)) {
					// ignore fields with default value
					continue;
				}
				if (p.IsCollection && _params.SerializeEmptyCollections == false && o is ICollection && (o as ICollection).Count == 0) {
					continue;
				}
				if (append)
					_output.Append (',');

				if (p.SpecificName == false) {
					n = _params.NamingStrategy.Rename (p.SerializedName);
				}
				if (p.WriteValue != null && p.Converter == null) {
					WriteStringFast (n);
					_output.Append (':');
					p.WriteValue (this, o);
				}
				else {
					WritePair (n, o);
				}

				if (o != null && _params.UseExtensions) {
					var tt = o.GetType ();
					if (tt == typeof(object))
						map.Add (p.SerializedName, tt.ToString ());
				}
				append = true;
			}
			if (map.Count > 0 && _params.UseExtensions) {
				_output.Append (",\"$map\":");
				WriteStringDictionary (map);
				append = true;
			}
			if (si != null) {
				var ev = si.SerializeExtraValues (obj);
				if (ev != null) {
					foreach (var item in ev) {
						if (append)
							_output.Append (',');
						WritePair (item.Key, item.Value);
						append = true;
					}
				}
				si.OnSerialized (obj);
			}
			_output.Append ('}');
			_current_depth--;
		}


		private void WritePairFast (string name, string value) {
			WriteStringFast (name);

			_output.Append (':');

			WriteStringFast (value);
		}

		private void WritePair (string name, object value) {
			WriteStringFast (name);

			_output.Append (':');

			WriteValue (value);
		}

		static void WriteArray (JSONSerializer serializer, object value) {
			IEnumerable array = value as IEnumerable;
			if (array == null) {
				serializer._output.Append ("null");
				return;
			}
			//if (_params.SerializeEmptyCollections == false) {
			//	var c = array as ICollection;
			//	if (c.Count == 0) {
			//		return;
			//	}
			//}
			serializer._output.Append ('[');

			var list = array as IList;
			if (list != null) {
				var c = list.Count;
				if (c == 0) {
					goto EXIT;
				}

				var w = serializer._manager.GetDefinition (list.GetType ()).ItemSerializer;
				if (w != null) {
					w (serializer, list[0]);
					for (int i = 1; i < c; i++) {
						serializer._output.Append (',');
						w (serializer, list[i]);
					}
					goto EXIT;
				}

				serializer.WriteValue (list[0]);
				for (int i = 1; i < c; i++) {
					serializer._output.Append (',');
					serializer.WriteValue (list[i]);
				}
				goto EXIT;
			}

			var pendingSeperator = false;

			foreach (object obj in array) {
				if (pendingSeperator) serializer._output.Append (',');

				serializer.WriteValue (obj);

				pendingSeperator = true;
			}
			EXIT:
			serializer._output.Append (']');
		}

		private void WriteStringDictionary (IDictionary dic) {
			_output.Append ('{');

			var pendingSeparator = false;

			foreach (DictionaryEntry entry in dic) {
				if (_params.SerializeNullValues == false && entry.Value == null) {
					continue;
				}
				if (pendingSeparator) _output.Append (',');
				WritePair (_params.NamingStrategy.Rename ((string)entry.Key), entry.Value);
				pendingSeparator = true;
			}
			_output.Append ('}');
		}

		private void WriteNameValueCollection (NameValueCollection collection) {
			_output.Append ('{');
			var pendingSeparator = false;
			var length = collection.Count;
			string n;
			for (int i = 0; i < length; i++) {
				var v = collection.GetValues (i);
				if (v == null && _params.SerializeNullValues == false) {
					continue;
				}
				if (pendingSeparator) _output.Append (',');
				pendingSeparator = true;
				n = _params.NamingStrategy.Rename (collection.GetKey (i));
				_output.Append ('\"');
				_output.Append (n);
				_output.Append ("\":");
				if (v == null) {
					_output.Append ("null");
					continue;
				}
				var vl = v.Length;
				if (vl == 0) {
					_output.Append ("\"\"");
					continue;
				}
				if (vl == 1) {
					if (_useEscapedUnicode) {
						WriteStringEscapeUnicode (_output, v[0]);
					}
					else {
						WriteString (_output, v[0]);
					}
				}
				else {
					_output.Append ('[');
					if (_useEscapedUnicode) {
						WriteStringEscapeUnicode (_output, v[0]);
					}
					else {
						WriteString (_output, v[0]);
					}
					for (int vi = 1; vi < vl; vi++) {
						_output.Append (',');
						if (_useEscapedUnicode) {
							WriteStringEscapeUnicode (_output, v[vi]);
						}
						else {
							WriteString (_output, v[vi]);
						}
					}
					_output.Append (']');
				}
			}
			_output.Append ('}');
		}

		private void WriteStringDictionary (IDictionary<string, object> dic) {
			_output.Append ('{');
			var pendingSeparator = false;
			foreach (KeyValuePair<string, object> entry in dic) {
				if (_params.SerializeNullValues == false && (entry.Value == null)) {
					continue;
				}
				if (pendingSeparator) _output.Append (',');
				WritePair (_params.NamingStrategy.Rename (entry.Key), entry.Value);
				pendingSeparator = true;
			}
			_output.Append ('}');
		}

		private void WriteDictionary (IDictionary dic) {
			_output.Append ('[');

			var pendingSeparator = false;

			foreach (DictionaryEntry entry in dic) {
				if (pendingSeparator) _output.Append (',');
				_output.Append ('{');
				WritePair ("k", entry.Key);
				_output.Append (",");
				WritePair ("v", entry.Value);
				_output.Append ('}');

				pendingSeparator = true;
			}
			_output.Append (']');
		}

		private void WriteStringFast (string s) {
			_output.Append ('\"');
			_output.Append (s);
			_output.Append ('\"');
		}

		internal static void WriteStringEscapeUnicode (StringBuilder output, string s) {
			output.Append ('\"');

			var runIndex = -1;
			var l = s.Length;
			for (var index = 0; index < l; ++index) {
				var c = s[index];
				if (c >= ' ' && c < 128 && c != '\"' && c != '\\') {
					if (runIndex == -1)
						runIndex = index;

					continue;
				}

				if (runIndex != -1) {
					output.Append (s, runIndex, index - runIndex);
					runIndex = -1;
				}

				switch (c) {
					case '\t': output.Append ("\\t"); break;
					case '\r': output.Append ("\\r"); break;
					case '\n': output.Append ("\\n"); break;
					case '"':
					case '\\': output.Append ('\\'); output.Append (c); break;
					default:
						output.Append ("\\u");
						// hard-code this line to improve performance:
						// output.Append (((int)c).ToString ("X4", NumberFormatInfo.InvariantInfo));
						var n = (c >> 12) & 0x0F;
						output.Append ((char)(n > 9 ? n + ('A' - 10) : n + '0'));
						n = (c >> 8) & 0x0F;
						output.Append ((char)(n > 9 ? n + ('A' - 10) : n + '0'));
						n = (c >> 4) & 0x0F;
						output.Append ((char)(n > 9 ? n + ('A' - 10) : n + '0'));
						n = c & 0x0F;
						output.Append ((char)(n > 9 ? n + ('A' - 10) : n + '0'));
						break;
				}
			}

			if (runIndex != -1)
				output.Append (s, runIndex, s.Length - runIndex);

			output.Append ('\"');
		}

		internal static void WriteString (StringBuilder output, string s) {
			output.Append ('\"');

			var runIndex = -1;
			var l = s.Length;
			for (var index = 0; index < l; ++index) {
				var c = s[index];
				if (c != '\t' && c != '\n' && c != '\r' && c != '\"' && c != '\\')// && c != ':' && c!=',')
				{
					if (runIndex == -1)
						runIndex = index;

					continue;
				}

				if (runIndex != -1) {
					output.Append (s, runIndex, index - runIndex);
					runIndex = -1;
				}

				switch (c) {
					case '\t': output.Append ("\\t"); break;
					case '\r': output.Append ("\\r"); break;
					case '\n': output.Append ("\\n"); break;
					case '"':
					case '\\': output.Append ('\\'); output.Append (c); break;
					default:
						output.Append (c);
						break;
				}
			}

			if (runIndex != -1)
				output.Append (s, runIndex, s.Length - runIndex);

			output.Append ('\"');
		}

		static string ToFixedWidthString (int value, int digits) {
			var chs = new char[digits];
			for (int i = chs.Length - 1; i >= 0; i--) {
				chs[i] = (char)('0' + (value % 10));
				value /= 10;
			}
			return new string (chs);
		}

		static string Int64ToString (long value) {
			var n = false;
			var d = 20;
			if (value < 0) {
				if (value == Int64.MinValue) {
					return "-9223372036854775808";
				}
				n = true;
				value = -value;
			}
			if (value < 10L) {
				d = 2;
			}
			else if (value < 1000L) {
				d = 4;
			}
			else if (value < 1000000L) {
				d = 7;
			}
			var chs = new char[d];
			var i = d;
			while (--i > 0) {
				chs[i] = (char)('0' + (value % 10L));
				value /= 10L;
				if (value == 0) {
					break;
				}
			}
			if (n) {
				chs[--i] = '-';
			}
			return new string (chs, i, d - i);
		}
		static string Int32ToString (int value) {
			var n = false;
			var d = 11;
			if (value < 0) {
				if (value == Int32.MinValue) {
					return "-2147483648";
				}
				n = true;
				value = -value;
			}
			if (value < 10) {
				d = 2;
			}
			else if (value < 1000) {
				d = 4;
			}
			var chs = new char[d];
			var i = d;
			while (--i > 0) {
				chs[i] = (char)('0' + (value % 10));
				value /= 10;
				if (value == 0) {
					break;
				}
			}
			if (n) {
				chs[--i] = '-';
			}
			return new string (chs, i, d - i);
		}

		#region WriteJsonValue delegate methods
		internal static WriteJsonValue GetWriteJsonMethod (Type type) {
			return type == typeof(int) ? WriteInt32
					: type == typeof(long) ? WriteInt64
					: type == typeof(string) ? WriteString
					: type == typeof(double) ? WriteDouble
					: type == typeof(float) ? WriteSingle
					: type == typeof(decimal) ? WriteDecimal
					: type == typeof(bool) ? WriteBoolean
					: type == typeof(byte) ? WriteByte
					: type == typeof(DateTime) ? WriteDateTime
					: type == typeof(TimeSpan) ? WriteTimeSpan
					: type == typeof(Guid) ? WriteGuid
					: type.IsSubclassOf (typeof(Enum)) ? WriteEnum
					: type.IsSubclassOf (typeof(Array)) && type != typeof(byte[]) ? WriteArray
					: (WriteJsonValue)null;
		}

		static void WriteByte (JSONSerializer serializer, object value) {
			serializer._output.Append (Int32ToString ((byte)value));
		}
		static void WriteInt32 (JSONSerializer serializer, object value) {
			serializer._output.Append (Int32ToString ((int)value));
		}
		static void WriteInt64 (JSONSerializer serializer, object value) {
			serializer._output.Append (Int32ToString ((int)(long)value));
		}
		static void WriteSingle (JSONSerializer serializer, object value) {
			serializer._output.Append (((float)value).ToString (NumberFormatInfo.InvariantInfo));
		}
		static void WriteDouble (JSONSerializer serializer, object value) {
			serializer._output.Append (((double)value).ToString (NumberFormatInfo.InvariantInfo));
		}
		static void WriteDecimal (JSONSerializer serializer, object value) {
			serializer._output.Append (((decimal)value).ToString (NumberFormatInfo.InvariantInfo));
		}
		static void WriteBoolean (JSONSerializer serializer, object value) {
			serializer._output.Append ((bool)value ? "true" : "false");
		}
		static void WriteDateTime (JSONSerializer serializer, object value) {
			// datetime format standard : yyyy-MM-dd HH:mm:ss
			var dt = (DateTime)value;
			var parameter = serializer._params;
			var output = serializer._output;
			if (parameter.UseUTCDateTime)
				dt = dt.ToUniversalTime ();

			output.Append ('\"');
			output.Append (ToFixedWidthString (dt.Year, 4));
			output.Append ('-');
			output.Append (ToFixedWidthString (dt.Month, 2));
			output.Append ('-');
			output.Append (ToFixedWidthString (dt.Day, 2));
			output.Append ('T'); // strict ISO date compliance
			output.Append (ToFixedWidthString (dt.Hour, 2));
			output.Append (':');
			output.Append (ToFixedWidthString (dt.Minute, 2));
			output.Append (':');
			output.Append (ToFixedWidthString (dt.Second, 2));
			if (parameter.DateTimeMilliseconds) {
				output.Append ('.');
				output.Append (ToFixedWidthString (dt.Millisecond, 3));
			}
			if (parameter.UseUTCDateTime)
				output.Append ('Z');

			output.Append ('\"');
		}
		static void WriteTimeSpan (JSONSerializer serializer, object timeSpan) {
			serializer.WriteStringFast ((((TimeSpan)timeSpan).ToString ()));
		}
		static void WriteString (JSONSerializer serializer, object value) {
			if (value == null) {
				serializer._output.Append ("null");
				return;
			}
			var s = (string)value;
			if (s.Length == 0) {
				serializer._output.Append ("\"\"");
				return;
			}
			if (serializer._params.UseEscapedUnicode) {
				WriteStringEscapeUnicode (serializer._output, s);
			}
			else {
				WriteString (serializer._output, s);
			}
		}
		static void WriteGuid (JSONSerializer serializer, object guid) {
			if (serializer._params.UseFastGuid == false)
				serializer.WriteStringFast (((Guid)guid).ToString ());
			else
				serializer.WriteBytes (((Guid)guid).ToByteArray ());
		}

		static void WriteEnum (JSONSerializer serializer, object value) {
			Enum e = (Enum)value;
			// TODO : optimize enum write
			if (serializer._params.UseValuesOfEnums) {
				serializer._output.Append (Convert.ToInt64 (e).ToString (NumberFormatInfo.InvariantInfo));
				return;
			}
			var n = serializer._manager.GetEnumName (e);
			if (n != null) {
				serializer.WriteStringFast (n);
			}
			else {
				serializer._output.Append (Convert.ToInt64 (e).ToString (NumberFormatInfo.InvariantInfo));
			}
		}

		#endregion
	}
}
