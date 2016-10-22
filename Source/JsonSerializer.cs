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

namespace PowerJson
{

	sealed class JsonSerializer
	{
		public const char StartObject = '{';
		public const char EndObject = '}';
		public const char Separator = ',';
		public const char StartArray = '[';
		public const char EndArray = ']';
		public const string Null = "null";

		delegate void WritePredefined ();
		delegate void WriteSingleChar (char character);
		delegate void WriteCharArray (char[] characters, int index, int count);
		delegate void WriteText (string text);

		static readonly WriteJsonValue[] _convertMethods = RegisterMethods ();
		static readonly char[] __numericChars = "0123456789".ToCharArray ();
		static readonly TwoDigitCharPair[] __twoDigitChars = InitTwoDigitChars ();
		readonly int _maxDepth = 20;
		int _currentDepth;
		Dictionary<object, int> _cirobj;
		char[] _intSlot, _dateSlot;
		readonly SerializationManager _manager;
		readonly bool _useExtensions, _showReadOnlyProperties, _showReadOnlyFields;
		readonly WriteSingleChar OutputChar;
		readonly WriteCharArray OutputCharArray;
		readonly WriteText OutputText;
		readonly WriteText OutputName;
		readonly WriteText OutputString;

		JsonSerializer (SerializationManager manager) {
			_manager = manager;
			_maxDepth = manager.SerializerMaxDepth;
			if (manager.EnableAnonymousTypes) {
				_useExtensions = false;
				_showReadOnlyFields = _showReadOnlyProperties = true;
			}
			else {
				_useExtensions = manager.UseExtensions;
				_showReadOnlyProperties = manager.SerializeReadOnlyProperties;
				_showReadOnlyFields = manager.SerializeReadOnlyFields;
			}
			switch (manager.NamingConvention) {
				case NamingConvention.Default:
					OutputName = WriteNameUnchanged;
					break;
				case NamingConvention.LowerCase:
					OutputName = WriteNameLowerCase;
					break;
				case NamingConvention.CamelCase:
					OutputName = WriteNameCamelCase;
					break;
				case NamingConvention.UpperCase:
					OutputName = WriteNameUpperCase;
					break;
				default:
					OutputName = WriteNameUnchanged;
					break;
			}
		}
		internal JsonSerializer (SerializationManager manager, JsonStringWriter writer) : this(manager) {
			OutputChar = writer.Write;
			OutputCharArray = writer.Write;
			OutputText = writer.Write;
			OutputString = manager.UseEscapedUnicode ? writer.WriteStringEscapeUnicode : (WriteText)writer.WriteString;
		}
		internal JsonSerializer (SerializationManager manager, TextWriter writer) : this (manager) {
			OutputChar = writer.Write;
			OutputCharArray = writer.Write;
			OutputText = writer.Write;
			OutputString = manager.UseEscapedUnicode ? WriteStringEscapeUnicode : (WriteText)WriteString;
		}

		internal void ConvertToJSON (object obj) {
			var c = _manager.GetSerializationInfo (obj.GetType ());
			var cv = c.Converter;
			if (cv != null) {
				obj = cv.SerializationConvert (obj);
				if (obj == null) {
					OutputText ("null");
				}
				c = _manager.GetSerializationInfo (obj.GetType ());
			}
			var m = c.SerializeMethod;
			if (m != null) {
				if (c.CollectionName != null) {
					WriteObject (obj, c);
				}
				else {
					m (this, obj);
				}
			}
			else {
				WriteValue (obj);
			}
		}

		static TwoDigitCharPair[] InitTwoDigitChars () {
			var d = new TwoDigitCharPair[100];
			for (int i = 0; i < 10; i++) {
				for (int j = 0; j < 10; j++) {
					d[i*10 + j] = new TwoDigitCharPair { First = __numericChars[i], Second = __numericChars[j] };
				}
			}
			return d;
		}

		static WriteJsonValue[] RegisterMethods () {
			var r = new WriteJsonValue[Enum.GetNames (typeof (JsonDataType)).Length];
			r[(int)JsonDataType.Array] = WriteArray;
			r[(int)JsonDataType.Bool] = WriteBoolean;
			r[(int)JsonDataType.ByteArray] = WriteByteArray;
			r[(int)JsonDataType.DataSet] = WriteDataSet;
			r[(int)JsonDataType.DataTable] = WriteDataTable;
			r[(int)JsonDataType.DateTime] = WriteDateTime;
			r[(int)JsonDataType.Dictionary] = WriteDictionary;
			r[(int)JsonDataType.Double] = WriteDouble;
			r[(int)JsonDataType.Enum] = WriteEnum;
			r[(int)JsonDataType.List] = WriteArray;
			r[(int)JsonDataType.Guid] = WriteGuid;
			r[(int)JsonDataType.Hashtable] = WriteDictionary;
			r[(int)JsonDataType.Int] = WriteInt32;
			r[(int)JsonDataType.Long] = WriteInt64;
			r[(int)JsonDataType.MultiDimensionalArray] = WriteMultiDimensionalArray;
			r[(int)JsonDataType.NameValue] = WriteNameValueCollection;
			r[(int)JsonDataType.Object] = WriteUnknown;
			r[(int)JsonDataType.Single] = WriteSingle;
			r[(int)JsonDataType.String] = WriteString;
			r[(int)JsonDataType.StringDictionary] = WriteStringDictionary;
			r[(int)JsonDataType.StringKeyDictionary] = WriteDictionary;
			r[(int)JsonDataType.TimeSpan] = WriteTimeSpan;
			r[(int)JsonDataType.Undefined] = WriteObject;
			return r;
		}
		void WriteValue (object obj) {
			if (obj == null)
				OutputText ("null");

			else if (obj is string || obj is char) {
				OutputString (obj.ToString ());
			}
			else if (obj is bool)
				OutputText (((bool)obj) ? "true" : "false"); // conform to standard
			else if (obj is int) {
				WriteInt32 ((int)obj);
			}
			else if (obj is long) {
				OutputText (ValueConverter.Int64ToString ((long)obj));
			}
			else if (obj is double || obj is float || obj is decimal || obj is byte)
				OutputText (((IConvertible)obj).ToString (NumberFormatInfo.InvariantInfo));

			else if (obj is DateTime)
				WriteDateTime (this, obj);

			else if (obj is Guid)
				WriteGuid (this, obj);

			else {
				var t = obj.GetType ();
				var c = _manager.GetSerializationInfo (t);
				if (c.SerializeMethod != null) {
					if (c.CollectionName != null) {
						WriteObject (obj, c);
					}
					else {
						c.SerializeMethod (this, obj);
					}
				}
				else {
					WriteObject (obj, c);
				}
			}
		}

		void WriteSD (StringDictionary stringDictionary) {
			OutputChar ('{');

			var pendingSeparator = false;

			foreach (DictionaryEntry entry in stringDictionary) {
				if (_manager.SerializeNullValues == false && entry.Value == null) {
				}
				else {
					if (pendingSeparator) OutputChar (',');

					OutputName ((string)entry.Key);
					WriteString ((string)entry.Value);
					pendingSeparator = true;
				}
			}
			OutputChar ('}');
		}

		void WriteBytes (byte[] bytes) {
#if !SILVERLIGHT
			WriteStringFast (Convert.ToBase64String (bytes, 0, bytes.Length, Base64FormattingOptions.None));
#else
			WriteStringFast(Convert.ToBase64String(bytes, 0, bytes.Length));
#endif
		}

#if !SILVERLIGHT
		static DatasetSchema GetSchema (DataTable ds) {
			if (ds == null) return null;

			var m = new DatasetSchema {
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

		static DatasetSchema GetSchema (DataSet ds) {
			if (ds == null) return null;

			var m = new DatasetSchema {
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

		static string GetXmlSchema (DataTable dt) {
			using (var writer = new StringWriter ()) {
				dt.WriteXmlSchema (writer);
				return dt.ToString ();
			}
		}

		void WriteDataset (DataSet ds) {
			OutputChar ('{');
			if (_useExtensions) {
				WriteField (JsonDict.ExtSchema, _manager.UseOptimizedDatasetSchema ? (object)GetSchema (ds) : ds.GetXmlSchema ());
				OutputChar (',');
			}
			var tablesep = false;
			foreach (DataTable table in ds.Tables) {
				if (tablesep) OutputChar (',');
				tablesep = true;
				WriteDataTableData (table);
			}
			// end dataset
			OutputChar ('}');
		}

		void WriteDataTableData (DataTable table) {
			OutputChar ('"');
			OutputText (table.TableName);
			OutputText ("\":[");
			var cols = table.Columns;
			var rowseparator = false;
			var cl = cols.Count;
			var w = new WriteJsonValue[cl];
			if (table.Rows.Count > 3) {
				for (int i = w.Length - 1; i >= 0; i--) {
					w[i] = GetWriteJsonMethod (cols[i].DataType);
				}
			}
			else {
				w = null;
			}
			foreach (DataRow row in table.Rows) {
				if (rowseparator) OutputChar (',');
				rowseparator = true;
				OutputChar ('[');

				for (int j = 0; j < cl; j++) {
					if (j > 0) {
						OutputChar (',');
					}
					if (w != null) {
						w[j] (this, row[j]);
					}
					else {
						WriteValue (row[j]);
					}
				}
				OutputChar (']');
			}

			OutputChar (']');
		}

		void WriteDataTable (DataTable dt) {
			OutputChar ('{');
			if (_useExtensions) {
				WriteField (JsonDict.ExtSchema, _manager.UseOptimizedDatasetSchema ? (object)GetSchema (dt) : GetXmlSchema (dt));
				OutputChar (',');
			}

			WriteDataTableData (dt);

			OutputChar ('}');
		}
#endif

		// HACK: This is a very long function, individual parts in regions are made inline for better performance
		internal void WriteObject (object obj, SerializationInfo info) {
			var def = info ?? _manager.GetSerializationInfo (obj.GetType ());
			#region Detect Circular Reference
			if (def.Reflection.CircularReferencable) {
				var ci = 0;
				if (_cirobj == null) {
					_cirobj = new Dictionary<object, int> ();
				}
				if (_cirobj.TryGetValue (obj, out ci) == false)
					_cirobj.Add (obj, _cirobj.Count + 1);
				else {
					if (_currentDepth > 0 && _useExtensions && _manager.InlineCircularReferences == false) {
						//_circular = true;
						OutputText ("{\"" + JsonDict.ExtRefIndex + "\":");
						OutputText (ValueConverter.Int32ToString (ci));
						OutputChar ('}');
						return;
					}
				}
			}
			#endregion
			var si = def.Interceptor;
			if (si != null && si.OnSerializing (obj) == false) {
				return;
			}

			OutputChar ('{');

			_currentDepth++;
			if (_currentDepth > _maxDepth) {
				throw new JsonSerializationException ("Serializer encountered maximum depth of " + _maxDepth + ". Last object name on stack: " + def.Reflection.AssemblyName);
			}

			var append = false;
			#region Write Type Reference
			if (def.Reflection.IsAbstract || def.Alias != null) {
				WritePairFast (JsonDict.ExtType, def.Alias ?? def.Reflection.TypeName);
				append = true;
			}
			else if (_useExtensions && info == null && def.Reflection.IsAnonymous == false
				|| def.Reflection.Type.Equals (obj.GetType()) == false) {
				WritePairFast (JsonDict.ExtType, def.Reflection.TypeName);
				append = true;
			}
			#endregion

			var g = def.Getters;
			var c = g.Length;
			for (int ii = 0; ii < c; ii++) {
				var p = g[ii];
				var m = p.Member;
				#region Skip Members Not For Serialization
				if (p.Serializable == TriState.False) {
					continue;
				}
				if (p.Serializable == TriState.Default) {
					if (m.IsStatic && _manager.SerializeStaticMembers == false
						|| m.IsReadOnly && p.TypeInfo.Reflection.AppendItem == null
							&& (m.IsProperty && _showReadOnlyProperties == false || m.IsProperty == false && _showReadOnlyFields == false)) {
						continue;
					}
				}
				#endregion
				var ji = new JsonItem (m.MemberName, m.Getter (obj), true);
				if (si != null && si.OnSerializing (obj, ji) == false) {
					continue;
				}
				var cv = p.Converter ?? p.TypeInfo.Converter;
				if (cv != null) {
					ji._Value = cv.SerializationConvert (ji._Value);
				}
				#region Convert Items
				var ic = p.ItemConverter;
				if (ic == null && p.TypeInfo.TypeParameters != null && p.Member.IsNullable == false) {
					var it = p.TypeInfo.TypeParameters[0]; // item type
					ic = it.Converter;
				}
				if (ic != null) {
					var ev = ji._Value as IEnumerable;
					if (ev != null) {
						var ol = new List<object> ();
						foreach (var item in ev) {
							ol.Add (ic.SerializationConvert (item));
						}
						ji._Value = ol;
					}
				}
				#endregion

				#region Determine Serialized Field Name
				if (p.SpecificName) {
					if (ji._Value == null || p.TypedNames == null || p.TypedNames.TryGetValue (ji._Value.GetType (), out ji._Name) == false) {
						ji._Name = p.SerializedName;
					}
				}
				else {
					ji._Name = p.SerializedName;
				}
				#endregion

				#region Skip Null, Default Value or Empty Collection
				if (_manager.SerializeNullValues == false && (ji._Value == null || ji._Value is DBNull)) {
					continue;
				}
				if (p.HasNonSerializedValue && Array.IndexOf (p.NonSerializedValues, ji._Value) != -1) {
					// ignore fields with default value
					continue;
				}
				if (m.IsCollection && _manager.SerializeEmptyCollections == false) {
					var vc = ji._Value as ICollection;
					if (vc != null && vc.Count == 0) {
						continue;
					}
				}
				#endregion
				if (append)
					OutputChar (',');

				#region Write Name
				if (p.SpecificName) {
					WriteStringFast (ji._Name);
					OutputChar (':');
				}
				else {
					OutputName (ji._Name);
				}
				#endregion

				#region Write Value
				if (p.SerializeMethod != null && cv == null) {
					var v = ji._Value;
					if (v == null || v is DBNull) {
						OutputText ("null");
					}
					else if (p.TypeInfo.CollectionName != null) {
						WriteObject (v, p.TypeInfo);
					}
					else {
						p.SerializeMethod (this, v);
					}
				}
				else {
					WriteValue (ji._Value);
				}
				#endregion

				append = true;
			}
			#region Write Inherited Collection
			if (def.CollectionName != null && def.SerializeMethod != null) {
				if (append)
					OutputChar (',');
				WriteStringFast (def.CollectionName);
				OutputChar (':');
				def.SerializeMethod (this, obj);
				append = true;
			}
			#endregion

			#region Write Extra Values
			if (si != null) {
				var ev = si.SerializeExtraValues (obj);
				if (ev != null) {
					foreach (var item in ev) {
						if (append)
							OutputChar (',');
						WriteField (item._Name, item._Value);
						append = true;
					}
				}
				si.OnSerialized (obj);
			}
			#endregion

			_currentDepth--;
			OutputChar ('}');
		}

		void WritePairFast (string name, string value) {
			WriteStringFast (name);
			OutputChar (':');
			WriteStringFast (value);
		}

		public void WriteField (string name, object value) {
			WriteStringFast (name);
			OutputChar (':');
			WriteValue (value);
		}

		static void WriteArray (JsonSerializer serializer, object value) {
			IEnumerable array = value as IEnumerable;
			if (array == null) {
				serializer.OutputText ("null");
				return;
			}
			//if (_params.SerializeEmptyCollections == false) {
			//	var c = array as ICollection;
			//	if (c.Count == 0) {
			//		return;
			//	}
			//}

			var list = array as IList;
			if (list != null) {
				var c = list.Count;
				if (c == 0) {
					serializer.OutputText ("[]");
					return;
				}

				var t = list.GetType ();
				if (t.IsArray && t.GetArrayRank () > 1) {
					WriteMultiDimensionalArray (serializer, list);
					return;
				}
				var d = serializer._manager.GetSerializationInfo (t);
				var w = d.Reflection.ItemSerializer;
				var m = d.TypeParameters[0];
				var ic = m.Converter;
				if (w != null) {
					serializer.OutputChar ('[');
					var v = list[0];
					if (ic != null) {
						v = ic.SerializationConvert (v);
						WriteUnknown (serializer, v);
					}
					else if (v == null) {
						serializer.OutputText ("null");
					}
					else {
						w (serializer, v);
					}
					for (int i = 1; i < c; i++) {
						serializer.OutputChar (',');
						v = list[i];
						if (ic != null) {
							v = ic.SerializationConvert (v);
							WriteUnknown (serializer, v);
							continue;
						}
						if (v == null) {
							serializer.OutputText ("null");
						}
						else {
							w (serializer, v);
						}
					}
					serializer.OutputChar (']');
					return;
				}

				serializer.OutputChar ('[');
				serializer.WriteValue (list[0]);
				for (int i = 1; i < c; i++) {
					serializer.OutputChar (',');
					serializer.WriteValue (list[i]);
				}
				serializer.OutputChar (']');
				return;
			}

			var pendingSeperator = false;
			serializer.OutputChar ('[');
			foreach (object obj in array) {
				if (pendingSeperator) serializer.OutputChar (',');

				serializer.WriteValue (obj);

				pendingSeperator = true;
			}
			serializer.OutputChar (']');
		}

		static void WriteMultiDimensionalArray (JsonSerializer serializer, object value) {
			var a = value as Array;
			if (a == null) {
				serializer.OutputText ("null");
				return;
			}
			var m = serializer._manager.GetReflectionCache (a.GetType ().GetElementType ()).SerializeMethod;
			serializer.WriteMultiDimensionalArray (m, a);
		}

		void WriteMultiDimensionalArray (WriteJsonValue m, Array md) {
			var r = md.Rank;
			var lb = new int[r];
			var ub = new int[r];
			var mdi = new int[r];
			for (int i = 0; i < r; i++) {
				lb[i] = md.GetLowerBound (i);
				ub[i] = md.GetUpperBound (i) + 1;
			}
			Array.Copy (lb, 0, mdi, 0, r);
			WriteMultiDimensionalArray (m, md, r, lb, ub, mdi, 0);
		}

		void WriteMultiDimensionalArray (WriteJsonValue m, Array array, int rank, int[] lowerBounds, int[] upperBounds, int[] indexes, int rankIndex) {
			var u = upperBounds[rankIndex];
			if (rankIndex < rank - 1) {
				OutputChar ('[');
				bool s = false;
				var d = rankIndex;
				do {
					if (s) {
						OutputChar (',');
					}
					Array.Copy (lowerBounds, d + 1, indexes, d + 1, rank - d - 1);
					WriteMultiDimensionalArray (m, array, rank, lowerBounds, upperBounds, indexes, ++d);
					d = rankIndex;
					s = true;
				} while (++indexes[rankIndex] < u);
				OutputChar (']');
			}
			else if (rankIndex == rank - 1) {
				OutputChar ('[');
				bool s = false;
				do {
					if (s) {
						OutputChar (',');
					}
					var v = array.GetValue (indexes);
					if (v == null || v is DBNull) {
						OutputText ("null");
					}
					else {
						m (this, v);
					}
					s = true;
				} while (++indexes[rankIndex] < u);
				OutputChar (']');
			}
		}

		void WriteStringDictionary (IDictionary dic) {
			OutputChar ('{');
			var pendingSeparator = false;
			foreach (DictionaryEntry entry in dic) {
				if (_manager.SerializeNullValues == false && entry.Value == null) {
					continue;
				}
				if (pendingSeparator) OutputChar (',');
				OutputName ((string)entry.Key);
				WriteValue (entry.Value);
				pendingSeparator = true;
			}
			OutputChar ('}');
		}

		void WriteNameValueCollection (NameValueCollection collection) {
			OutputChar ('{');
			var pendingSeparator = false;
			var length = collection.Count;
			for (int i = 0; i < length; i++) {
				var v = collection.GetValues (i);
				if (v == null && _manager.SerializeNullValues == false) {
					continue;
				}
				if (pendingSeparator) OutputChar (',');
				pendingSeparator = true;
				OutputName (collection.GetKey (i));
				if (v == null) {
					OutputText ("null");
					continue;
				}
				var vl = v.Length;
				if (vl == 0) {
					OutputText ("\"\"");
					continue;
				}
				if (vl == 1) {
					OutputString (v[0]);
				}
				else {
					OutputChar ('[');
					OutputString (v[0]);
					for (int vi = 1; vi < vl; vi++) {
						OutputChar (',');
						OutputString (v[vi]);
					}
					OutputChar (']');
				}
			}
			OutputChar ('}');
		}

		void WriteStringDictionary (IDictionary<string, object> dic) {
			OutputChar ('{');
			var pendingSeparator = false;
			foreach (KeyValuePair<string, object> entry in dic) {
				if (_manager.SerializeNullValues == false && entry.Value == null) {
					continue;
				}
				if (pendingSeparator) OutputChar (',');
				OutputName (entry.Key);
				WriteValue (entry.Value);
				pendingSeparator = true;
			}
			OutputChar ('}');
		}

		void WriteKvStyleDictionary (IDictionary dic) {
			OutputChar ('[');

			var pendingSeparator = false;

			foreach (DictionaryEntry entry in dic) {
				if (pendingSeparator) OutputChar (',');
				OutputChar ('{');
				WriteField ("k", entry.Key);
				OutputChar (',');
				WriteField ("v", entry.Value);
				OutputChar ('}');

				pendingSeparator = true;
			}
			OutputChar (']');
		}

		void WriteStringFast (string s) {
			OutputChar ('"');
			OutputText (s);
			OutputChar ('"');
		}

		public void WriteStartArray () {
			OutputChar ('[');
		}
		public void WriteEndArray () {
			OutputChar (']');
		}
		public void WriteSeparator () {
			OutputChar (',');
		}
		public void WriteStartObject () {
			OutputChar ('{');
		}
		public void WriteEndObject () {
			OutputChar ('}');
		}
		internal void WriteStringEscapeUnicode (string s) {
			OutputChar ('\"');

			var runIndex = -1;
			var a = s.ToCharArray ();
			var l = a.Length;
			for (var index = 0; index < l; ++index) {
				var c = a[index];
				if (c >= ' ' && c < 128 && c != '\"' && c != '\\') {
					if (runIndex == -1) {
						runIndex = index;
					}
					continue;
				}

				if (runIndex != -1) {
					OutputCharArray (a, runIndex, index - runIndex);
					runIndex = -1;
				}

				switch (c) {
					case '\t': OutputText ("\\t"); break;
					case '\r': OutputText ("\\r"); break;
					case '\n': OutputText ("\\n"); break;
					case '"':
					case '\\':
						OutputChar ('\\'); OutputChar (c); break;
					default:
						var u = new char[6];
						u[0] = '\\';
						u[1] = 'u';
						// hard-code this line to improve performance:
						// output.Append (((int)c).ToString ("X4", NumberFormatInfo.InvariantInfo));
						var n = (c >> 12) & 0x0F;
						u[2] = ((char)(n > 9 ? n + ('A' - 10) : n + '0'));
						n = (c >> 8) & 0x0F;
						u[3] = ((char)(n > 9 ? n + ('A' - 10) : n + '0'));
						n = (c >> 4) & 0x0F;
						u[4] = ((char)(n > 9 ? n + ('A' - 10) : n + '0'));
						n = c & 0x0F;
						u[5] = ((char)(n > 9 ? n + ('A' - 10) : n + '0'));
						OutputCharArray (u, 0, 6);
						continue;
					}
				}

			if (runIndex != -1) {
				OutputCharArray (a, runIndex, a.Length - runIndex);
			}

			OutputChar ('\"');
		}

		internal void WriteString (string s) {
			OutputChar ('\"');

			var runIndex = -1;
			var a = s.ToCharArray ();
			var l = a.Length;
			for (var index = 0; index < l; ++index) {
				var c = a[index];
				if (c >= ' ' && c < 128 && c != '\"' && c != '\\') {
					if (runIndex == -1) {
						runIndex = index;
					}
					continue;
				}

				if (runIndex != -1) {
					OutputCharArray (a, runIndex, index - runIndex);
					runIndex = -1;
				}

				switch (c) {
					case '\t': OutputText ("\\t"); break;
					case '\r': OutputText ("\\r"); break;
					case '\n': OutputText ("\\n"); break;
					case '"':
					case '\\':
						OutputChar ('\\'); OutputChar (c); break;
					default:
						OutputChar (c);
						break;
				}
			}

			if (runIndex != -1) {
				OutputCharArray (a, runIndex, a.Length - runIndex);
			}

			OutputChar ('\"');
		}

		#region OutputName delegate methods
		void WriteNameUnchanged (string name) {
			OutputChar ('"');
			OutputText (name);
			OutputText ("\":");
		}
		void WriteNameLowerCase(string name) {
			OutputChar ('"');
			OutputText (name.ToLowerInvariant ());
			OutputText ("\":");
		}
		void WriteNameUpperCase(string name) {
			OutputChar ('"');
			OutputText (name.ToUpperInvariant ());
			OutputText ("\":");
		}
		void WriteNameCamelCase(string name) {
			OutputChar ('"');
			var l = name.Length;
			if (l > 0) {
				var c = name[0];
				if (c > 'A' - 1 && c < 'Z' + 1) {
					var b = name.ToCharArray ();
					b[0] = ((char)(c - ('A' - 'a')));
					OutputCharArray (b, 0, b.Length);
				}
				else {
					OutputText (name);
				}
			}
			OutputText ("\":");
		}
		#endregion
		#region WriteJsonValue delegate methods
		void WriteInt32 (int value) {
			if (value == 0) {
				OutputChar ('0');
				return;
			}
			if (_intSlot == null) {
				_intSlot = new char[11];
				_intSlot[0] = '-';
			}
			var n = false;
			if (value < 0) {
				if (value == Int32.MinValue) {
					OutputText ("-2147483648");
					return;
				}
				n = true;
				value = -value;
			}

			int i, d;
			if (value < 10000) {
				if (value < 10) {
					if (n) {
						_intSlot[1] = __numericChars[value % 10];
						OutputCharArray (_intSlot, 0, 2);
					}
					else {
						OutputChar (__numericChars[value % 10]);
					}
					return;
				}
				d = i = value < 100 ? 3 : value < 1000 ? 4 : 5;
			}
			else {
				d = i = value < 100000 ? 6 : value < 1000000 ? 7 : value < 10000000 ? 8 : value < 100000000 ? 9 : value < 1000000000 ? 10 : 11;
			}
			while (--i > 0) {
				_intSlot[i] = __numericChars[value % 10];
				value /= 10;
			}
			if (n) {
				OutputCharArray (_intSlot, 0, d);
			}
			else {
				OutputCharArray (_intSlot, 1, --d);
			}
		}

		internal static WriteJsonValue GetWriteJsonMethod (Type type) {
			var t = Reflection.GetJsonDataType (type);
			if (t == JsonDataType.Primitive) {
				return typeof (decimal).Equals (type) ? WriteDecimal
						: typeof (byte).Equals (type) ? WriteByte
						: typeof (sbyte).Equals (type) ? WriteSByte
						: typeof (short).Equals (type) ? WriteInt16
						: typeof (ushort).Equals (type) ? WriteUInt16
						: typeof (uint).Equals (type) ? WriteUInt32
						: typeof (ulong).Equals (type) ? WriteUInt64
						: typeof (char).Equals (type) ? WriteChar
						: (WriteJsonValue)WriteUnknown;
			}
			else if (t == JsonDataType.Undefined) {
				return type.IsSubclassOf (typeof (Array)) && type.GetArrayRank () > 1 ? WriteMultiDimensionalArray
					: type.IsSubclassOf (typeof (Array)) && typeof (byte[]).Equals (type) == false ? WriteArray
					: typeof (KeyValuePair<string, object>).Equals (type) ? WriteKeyObjectPair
					: typeof (KeyValuePair<string, string>).Equals (type) ? WriteKeyValuePair
					: (WriteJsonValue)new TypedSerializer (type).Serialize;
			}
			else {
				return _convertMethods[(int)t];
			}
		}

		static void WriteByte (JsonSerializer serializer, object value) {
			serializer.OutputText (ValueConverter.Int32ToString ((byte)value));
		}
		static void WriteSByte (JsonSerializer serializer, object value) {
			serializer.OutputText (ValueConverter.Int32ToString ((sbyte)value));
		}
		static void WriteInt16 (JsonSerializer serializer, object value) {
			serializer.OutputText (ValueConverter.Int32ToString ((short)value));
		}
		static void WriteUInt16 (JsonSerializer serializer, object value) {
			serializer.OutputText (ValueConverter.Int32ToString ((ushort)value));
		}
		static void WriteInt32 (JsonSerializer serializer, object value) {
			serializer.WriteInt32 ((int)value);
		}
		static void WriteUInt32 (JsonSerializer serializer, object value) {
			serializer.OutputText (ValueConverter.Int64ToString ((uint)value));
		}
		static void WriteInt64 (JsonSerializer serializer, object value) {
			serializer.OutputText (ValueConverter.Int64ToString ((long)value));
		}
		static void WriteUInt64 (JsonSerializer serializer, object value) {
			serializer.OutputText (ValueConverter.UInt64ToString ((ulong)value));
		}
		static void WriteSingle (JsonSerializer serializer, object value) {
			serializer.OutputText (((float)value).ToString (NumberFormatInfo.InvariantInfo));
		}
		static void WriteDouble (JsonSerializer serializer, object value) {
			serializer.OutputText (((double)value).ToString (NumberFormatInfo.InvariantInfo));
		}
		static void WriteDecimal (JsonSerializer serializer, object value) {
			serializer.OutputText (((decimal)value).ToString (NumberFormatInfo.InvariantInfo));
		}
		static void WriteBoolean (JsonSerializer serializer, object value) {
			serializer.OutputText ((bool)value ? "true" : "false");
		}
		static void WriteChar (JsonSerializer serializer, object value) {
			WriteString (serializer, ((char)value).ToString ());
		}

		static void WriteDateTime (JsonSerializer serializer, object value) {
			var d = serializer._dateSlot;
			if (d == null) {
			// datetime format standard : yyyy-MM-ddTHH:mm:ss[.sss][Z]
				d = new char[26];
				d[0] = d[25] = '"';
				d[5] = d[8] = '-';
				d[11] = 'T';
				d[14] = d[17] = ':';
				serializer._dateSlot = d;
			}
			var dt = (DateTime)value;
			var parameter = serializer._manager;
			if (parameter.UseUniversalTime)
				dt = dt.ToUniversalTime ();

			int n = dt.Year;
			TwoDigitCharPair p;
			if (n > 1999 && n < 2100) {
				d[1] = '2'; d[2] = '0';
				n -= 2000;
				p = __twoDigitChars[n];
				d[3] = p.First; d[4] = p.Second;
			}
			else if (n > 1899 && n < 2000) {
				d[1] = '1'; d[2] = '9';
				n -= 1900;
				p = __twoDigitChars[n];
				d[3] = p.First; d[4] = p.Second;
			}
			else {
				p = __twoDigitChars[n / 100];
				d[1] = p.First; d[2] = p.Second;
				p = __twoDigitChars[n % 100];
				d[3] = p.First; d[4] = p.Second;
			}
			p = __twoDigitChars[dt.Month];
			d[6] = p.First; d[7] = p.Second;
			p = __twoDigitChars[dt.Day];
			d[9] = p.First; d[10] = p.Second;
			p = __twoDigitChars[dt.Hour];
			d[12] = p.First; d[13] = p.Second;
			p = __twoDigitChars[dt.Minute];
			d[15] = p.First; d[16] = p.Second;
			p = __twoDigitChars[dt.Second];
			d[18] = p.First; d[19] = p.Second;

			if (parameter.DateTimeMilliseconds) {
				d[20] = '.';
				n = dt.Millisecond;
				d[21] = (char)('0' + (n / 100));
				p = __twoDigitChars[n % 100];
				d[22] = p.First; d[23] = p.Second;
				n = 23;
			}
			else {
				n = 19;
			}
			if (parameter.UseUniversalTime) {
				d[++n] = 'Z';
			}
			d[++n] = '"';
			serializer.OutputCharArray (d, 0, ++n);
		}

		static void WriteTimeSpan (JsonSerializer serializer, object timeSpan) {
			serializer.WriteStringFast ((((TimeSpan)timeSpan).ToString ()));
		}

		static void WriteString (JsonSerializer serializer, object value) {
			if (value == null) {
				serializer.OutputText ("null");
				return;
			}
			var s = (string)value;
			if (s.Length == 0) {
				serializer.OutputText ("\"\"");
				return;
			}
			serializer.OutputString (s);
		}

		static void WriteGuid (JsonSerializer serializer, object guid) {
			if (serializer._manager.UseFastGuid == false)
				serializer.WriteStringFast (((Guid)guid).ToString ());
			else
				serializer.WriteBytes (((Guid)guid).ToByteArray ());
		}

		static void WriteEnum (JsonSerializer serializer, object value) {
			Enum e = (Enum)value;
			// TODO : optimize enum write
			if (serializer._manager.UseValuesOfEnums) {
				serializer.OutputText (Convert.ToInt64 (e).ToString (NumberFormatInfo.InvariantInfo));
				return;
			}
			var n = serializer._manager.GetEnumName (e);
			if (n != null) {
				serializer.WriteStringFast (n);
			}
			else {
				serializer.OutputText (Convert.ToInt64 (e).ToString (NumberFormatInfo.InvariantInfo));
			}
		}
		static void WriteByteArray (JsonSerializer serializer, object value) {
			serializer.WriteStringFast (Convert.ToBase64String ((byte[])value));
		}
		static void WriteDataSet (JsonSerializer serializer, object value) {
			serializer.WriteDataset ((DataSet)value);
		}
		static void WriteDataTable (JsonSerializer serializer, object value) {
			serializer.WriteDataTable ((DataTable)value);
		}
		static void WriteDictionary (JsonSerializer serializer, object value) {
			if (serializer._manager.KVStyleStringDictionary == false) {
				if (value is IDictionary<string, object>) {
					serializer.WriteStringDictionary ((IDictionary<string, object>)value);
					return;
				}
				else if (value is IDictionary
					&& value.GetType ().IsGenericType
					&& typeof (string).Equals (value.GetType ().GetGenericArguments ()[0])) {
					serializer.WriteStringDictionary ((IDictionary)value);
					return;
				}
#if NET_40_OR_GREATER
				else if (value is System.Dynamic.ExpandoObject) {
					serializer.WriteStringDictionary ((IDictionary<string, object>)value);
					return;
				}
#endif
			}
			if (value is IDictionary)
				serializer.WriteKvStyleDictionary ((IDictionary)value);
		}
		static void WriteStringDictionary (JsonSerializer serializer, object value) {
			serializer.WriteSD ((StringDictionary)value);
		}
		static void WriteNameValueCollection (JsonSerializer serializer, object value) {
			serializer.WriteNameValueCollection ((NameValueCollection)value);
		}
		static void WriteKeyObjectPair (JsonSerializer serializer, object value) {
			var p = (KeyValuePair<string, object>)value;
			serializer.OutputChar ('{');
			serializer.WriteStringFast (p.Key);
			serializer.OutputChar (':');
			serializer.WriteObject (p.Value, null);
			serializer.OutputChar ('}');
		}
		static void WriteKeyValuePair (JsonSerializer serializer, object value) {
			var p = (KeyValuePair<string, string>)value;
			serializer.OutputChar ('{');
			serializer.WriteStringFast (p.Key);
			serializer.OutputChar (':');
			WriteString (serializer, p.Value);
			serializer.OutputChar ('}');
		}
		static void WriteObject (JsonSerializer serializer, object value) {
			serializer.WriteObject (value, null);
		}
		static void WriteUnknown (JsonSerializer serializer, object value) {
			serializer.WriteValue (value);
		}
		struct TwoDigitCharPair
		{
			public char First, Second;
		}

		struct TypedSerializer
		{
			readonly Type Type;
			public TypedSerializer (Type type) {
				Type = type;
			}
			internal void Serialize (JsonSerializer serializer, object value) {
				serializer.WriteObject (value, null);
			}
		}

		#endregion
	}
}
