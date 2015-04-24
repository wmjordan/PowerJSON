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
		StringBuilder _output = new StringBuilder();
		StringBuilder _before = new StringBuilder();
		readonly int _MAX_DEPTH = 20;
		int _current_depth = 0;
		readonly Dictionary<string, int> _globalTypes = new Dictionary<string, int>();
		readonly Dictionary<object, int> _cirobj = new Dictionary<object, int>();
		readonly JSONParameters _params;
		readonly bool _useEscapedUnicode = false;
		readonly SerializationManager _manager;

		internal JSONSerializer(JSONParameters param, SerializationManager manager)
		{
			_params = param;
			_useEscapedUnicode = _params.UseEscapedUnicode;
			_MAX_DEPTH = _params.SerializerMaxDepth;
			_manager = manager;
		}

		internal string ConvertToJSON(object obj)
		{
			WriteValue(obj);

			string str = "";
			if (_params.UsingGlobalTypes && _globalTypes != null && _globalTypes.Count > 0)
			{
				StringBuilder sb = _before;
				sb.Append("\"$types\":{");
				bool pendingSeparator = false;
				foreach (var kv in _globalTypes)
				{
					if (pendingSeparator) sb.Append(',');
					pendingSeparator = true;
					sb.Append('\"');
					sb.Append(kv.Key);
					sb.Append("\":\"");
					sb.Append(kv.Value);
					sb.Append('\"');
				}
				sb.Append("},");
				sb.Append(_output.ToString());
				str = sb.ToString();
			}
			else
				str = _output.ToString();

			return str;
		}

		private void WriteValue(object obj)
		{
			if (obj == null || obj is DBNull)
				_output.Append ("null");

			else if (obj is string || obj is char)
				WriteString (obj.ToString ());

			else if (obj is Guid)
				WriteGuid ((Guid)obj);

			else if (obj is bool)
				_output.Append (((bool)obj) ? "true" : "false"); // conform to standard

			else if (
				obj is int || obj is long || obj is double ||
				obj is decimal || obj is float ||
				obj is byte || obj is short ||
				obj is sbyte || obj is ushort ||
				obj is uint || obj is ulong
			)
				_output.Append (((IConvertible)obj).ToString (NumberFormatInfo.InvariantInfo));

			else if (obj is DateTime)
				WriteDateTime ((DateTime)obj);

			else if (obj is TimeSpan) {
				WriteTimeSpan ((TimeSpan)obj);
			}
			else if (_params.KVStyleStringDictionary == false && obj is IDictionary &&
				obj.GetType ().IsGenericType && obj.GetType ().GetGenericArguments ()[0] == typeof (string))

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
				WriteArray ((IEnumerable)obj);

			else if (obj is Enum)
				WriteEnum ((Enum)obj);

			else if (Reflection.Instance.IsTypeRegistered (obj.GetType ()))
				WriteCustom (obj);

			else
				WriteObject (obj);
		}

		private void WriteSD(StringDictionary stringDictionary)
		{
			_output.Append('{');

			bool pendingSeparator = false;

			foreach (DictionaryEntry entry in stringDictionary)
			{
				if (_params.SerializeNullValues == false && entry.Value == null)
				{
				}
				else
				{
					if (pendingSeparator) _output.Append(',');

					WritePair (_params.NamingStrategy.Rename ((string)entry.Key), entry.Value);
					pendingSeparator = true;
				}
			}
			_output.Append('}');
		}

		private void WriteCustom(object obj)
		{
			Serialize s;
			Reflection.Instance._customSerializer.TryGetValue(obj.GetType(), out s);
			WriteStringFast(s(obj));
		}

		private void WriteEnum (Enum e) {
			// TODO : optimize enum write
			if (_params.UseValuesOfEnums) {
				_output.Append(Convert.ToInt64 (e).ToString(NumberFormatInfo.InvariantInfo));
				return;
			}
			var n = _manager.GetEnumName (e);
			if (n != null) {
				WriteStringFast (n);
			}
			else {
				_output.Append (Convert.ToInt64 (e).ToString (NumberFormatInfo.InvariantInfo));
			}
		}

		private void WriteGuid(Guid g)
		{
			if (_params.UseFastGuid == false)
				WriteStringFast(g.ToString());
			else
				WriteBytes(g.ToByteArray());
		}

		private void WriteBytes(byte[] bytes)
		{
#if !SILVERLIGHT
			WriteStringFast(Convert.ToBase64String(bytes, 0, bytes.Length, Base64FormattingOptions.None));
#else
			WriteStringFast(Convert.ToBase64String(bytes, 0, bytes.Length));
#endif
		}

		private void WriteTimeSpan (TimeSpan timeSpan) {
			WriteStringFast (timeSpan.ToString ());
		}
		private void WriteDateTime(DateTime dateTime)
		{
			// datetime format standard : yyyy-MM-dd HH:mm:ss
			DateTime dt = dateTime;
			if (_params.UseUTCDateTime)
				dt = dateTime.ToUniversalTime();

			_output.Append('\"');
			_output.Append(dt.Year.ToString("0000", NumberFormatInfo.InvariantInfo));
			_output.Append('-');
			_output.Append(dt.Month.ToString("00", NumberFormatInfo.InvariantInfo));
			_output.Append('-');
			_output.Append(dt.Day.ToString("00", NumberFormatInfo.InvariantInfo));
			_output.Append('T'); // strict ISO date compliance 
			_output.Append(dt.Hour.ToString("00", NumberFormatInfo.InvariantInfo));
			_output.Append(':');
			_output.Append(dt.Minute.ToString("00", NumberFormatInfo.InvariantInfo));
			_output.Append(':');
			_output.Append(dt.Second.ToString("00", NumberFormatInfo.InvariantInfo));
			if (_params.DateTimeMilliseconds)
			{
				_output.Append('.');
				_output.Append(dt.Millisecond.ToString("000", NumberFormatInfo.InvariantInfo));
			}
			if (_params.UseUTCDateTime)
				_output.Append('Z');

			_output.Append('\"');
		}

#if !SILVERLIGHT
		private DatasetSchema GetSchema(DataTable ds)
		{
			if (ds == null) return null;

			DatasetSchema m = new DatasetSchema();
			m.Info = new List<string>();
			m.Name = ds.TableName;

			foreach (DataColumn c in ds.Columns)
			{
				m.Info.Add(ds.TableName);
				m.Info.Add(c.ColumnName);
				m.Info.Add(c.DataType.ToString());
			}
			// FEATURE : serialize relations and constraints here

			return m;
		}

		private DatasetSchema GetSchema(DataSet ds)
		{
			if (ds == null) return null;

			DatasetSchema m = new DatasetSchema();
			m.Info = new List<string>();
			m.Name = ds.DataSetName;

			foreach (DataTable t in ds.Tables)
			{
				foreach (DataColumn c in t.Columns)
				{
					m.Info.Add(t.TableName);
					m.Info.Add(c.ColumnName);
					m.Info.Add(c.DataType.ToString());
				}
			}
			// FEATURE : serialize relations and constraints here

			return m;
		}

		private string GetXmlSchema(DataTable dt)
		{
			using (var writer = new StringWriter())
			{
				dt.WriteXmlSchema(writer);
				return dt.ToString();
			}
		}

		private void WriteDataset(DataSet ds)
		{
			_output.Append('{');
			if (_params.UseExtensions)
			{
				WritePair("$schema", _params.UseOptimizedDatasetSchema ? (object)GetSchema(ds) : ds.GetXmlSchema());
				_output.Append(',');
			}
			bool tablesep = false;
			foreach (DataTable table in ds.Tables)
			{
				if (tablesep) _output.Append(',');
				tablesep = true;
				WriteDataTableData(table);
			}
			// end dataset
			_output.Append('}');
		}

		private void WriteDataTableData(DataTable table)
		{
			_output.Append('\"');
			_output.Append(table.TableName);
			_output.Append("\":[");
			DataColumnCollection cols = table.Columns;
			bool rowseparator = false;
			foreach (DataRow row in table.Rows)
			{
				if (rowseparator) _output.Append(',');
				rowseparator = true;
				_output.Append('[');

				bool pendingSeperator = false;
				foreach (DataColumn column in cols)
				{
					if (pendingSeperator) _output.Append(',');
					WriteValue(row[column]);
					pendingSeperator = true;
				}
				_output.Append(']');
			}

			_output.Append(']');
		}

		void WriteDataTable(DataTable dt)
		{
			_output.Append('{');
			if (_params.UseExtensions)
			{
				WritePair("$schema", _params.UseOptimizedDatasetSchema ? (object)GetSchema(dt) : GetXmlSchema(dt));
				_output.Append(',');
			}

			WriteDataTableData(dt);

			_output.Append('}');
		}
#endif

		bool _TypesWritten = false;
		private void WriteObject(object obj)
		{
			int i = 0;
			if (_cirobj.TryGetValue(obj, out i) == false)
				_cirobj.Add(obj, _cirobj.Count + 1);
			else
			{
				if (_current_depth > 0 && _params.InlineCircularReferences == false)
				{
					//_circular = true;
					_output.Append("{\"$i\":" + i + "}");
					return;
				}
			}
			ReflectionCache def = _manager.GetDefinition (obj.GetType ());
			var si = def.Interceptor;
			if (si != null && si.OnSerializing (obj) == false) {
				return;
			}
			if (_params.UsingGlobalTypes == false)
				_output.Append('{');
			else
			{
				if (_TypesWritten == false)
				{
					_output.Append('{');
					_before = _output;
					_output = new StringBuilder();
				}
				else
					_output.Append('{');
			}
			_TypesWritten = true;
			_current_depth++;
			if (_current_depth > _MAX_DEPTH)
				throw new JsonSerializationException ("Serializer encountered maximum depth of " + _MAX_DEPTH);


			Dictionary<string, string> map = new Dictionary<string, string>();
			bool append = false;
			if (_params.UseExtensions)
			{
				if (_params.UsingGlobalTypes == false)
					WritePairFast("$type", def.AssemblyName);
				else
				{
					int dt = 0;
					string ct = def.AssemblyName;
					if (_globalTypes.TryGetValue(ct, out dt) == false)
					{
						dt = _globalTypes.Count + 1;
						_globalTypes.Add(ct, dt);
					}
					WritePairFast("$type", dt.ToString());
				}
				append = true;
			}

			Getters[] g = def.Getters;
			int c = g.Length;
			var rp = _params.ShowReadOnlyProperties;
			for (int ii = 0; ii < c; ii++)
			{
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
				object o = p.Getter(obj);
				string n = p.MemberName;
				if (p.Converter != null) {
					o = p.Converter.SerializationConvert (n, o);
				}
				if (si != null && si.OnSerializing (obj, ref n, ref o) == false) {
					continue;
				}
				if (p.SpecificName) {
					if (o == null || p.TypedNames == null || p.TypedNames.TryGetValue (o.GetType (), out n) == false) {
						n = p.SerializedName;
					}
				}
				else {
					n = p.SerializedName;
				}
				if (_params.SerializeNullValues == false && (o == null || o is DBNull))
				{
					//append = false;
					continue;
				}
				if (p.HasDefaultValue && Equals (o, p.DefaultValue)) {
					// ignore fields with default value
					continue;
				}
				if (p.SpecificName == false) {
					n = _params.NamingStrategy.Rename (p.SerializedName);
				}
				if (append)
					_output.Append(',');

				WritePair (n, o);

				if (o != null && _params.UseExtensions)
				{
					Type tt = o.GetType ();
					if (tt == typeof(object))
						map.Add(p.SerializedName, tt.ToString());
				}
				append = true;
			}
			if (map.Count > 0 && _params.UseExtensions)
			{
				_output.Append(",\"$map\":");
				WriteStringDictionary(map);
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
			_output.Append('}');
			_current_depth--;
		}

		private void WritePairFast(string name, string value)
		{
			WriteStringFast(name);

			_output.Append(':');

			WriteStringFast(value);
		}

		private void WritePair(string name, object value)
		{
			WriteStringFast(name);

			_output.Append(':');

			WriteValue(value);
		}

		private void WriteArray(IEnumerable array)
		{
			_output.Append('[');

			var list = array as IList;
			if (list != null) {
				if (list.Count == 0) {
					_output.Append (']');
					return;
				}
				WriteValue (list[0]);
				var length = list.Count;
				if (length > 1) {
					for (int i = 1; i < length; i++) {
						_output.Append (',');
						WriteValue (list[i]);
					}
				}
			}
			else {
				bool pendingSeperator = false;

				foreach (object obj in array)
				{
					if (pendingSeperator) _output.Append(',');

					WriteValue(obj);

					pendingSeperator = true;
				}
			}
			_output.Append(']');
		}

		private void WriteStringDictionary(IDictionary dic)
		{
			_output.Append('{');

			bool pendingSeparator = false;

			foreach (DictionaryEntry entry in dic)
			{
				if (_params.SerializeNullValues == false && (entry.Value == null))
				{
					continue;
				}
				if (pendingSeparator) _output.Append(',');
				WritePair (_params.NamingStrategy.Rename ((string)entry.Key), entry.Value);
				pendingSeparator = true;
			}
			_output.Append('}');
		}

		private void WriteNameValueCollection (NameValueCollection collection) {
			_output.Append ('{');
			bool pendingSeparator = false;
			var length = collection.Count;
			string n;
			for (int i = 0; i < length; i++) {
				var v = collection.GetValues (i);
				if (v == null && _params.SerializeNullValues == false) {
					continue;
				}
				if (pendingSeparator) _output.Append (',');
				n = _params.NamingStrategy.Rename (collection.GetKey (i));
				_output.Append ('\"');
				_output.Append (n);
				_output.Append ("\":");
				if (v == null) {
					_output.Append ("null");
				}
				else if (v.Length == 0) {
					_output.Append ("\"\"");
				}
				else if (v.Length == 1) {
					WriteString (v[0]);
				}
				else {
					WriteArray (v);
				}
				pendingSeparator = true;
			}
			_output.Append ('}');
		}

		private void WriteStringDictionary (IDictionary<string, object> dic)
		{
			_output.Append('{');
			bool pendingSeparator = false;
			foreach (KeyValuePair<string, object> entry in dic)
			{
				if (_params.SerializeNullValues == false && (entry.Value == null))
				{
					continue;
				}
				if (pendingSeparator) _output.Append(',');
				WritePair (_params.NamingStrategy.Rename (entry.Key), entry.Value);
				pendingSeparator = true;
			}
			_output.Append('}');
		}

		private void WriteDictionary(IDictionary dic)
		{
			_output.Append('[');

			bool pendingSeparator = false;

			foreach (DictionaryEntry entry in dic)
			{
				if (pendingSeparator) _output.Append(',');
				_output.Append('{');
				WritePair("k", entry.Key);
				_output.Append(",");
				WritePair("v", entry.Value);
				_output.Append('}');

				pendingSeparator = true;
			}
			_output.Append(']');
		}

		private void WriteStringFast(string s)
		{
			_output.Append('\"');
			_output.Append(s);
			_output.Append('\"');
		}

		private void WriteString(string s)
		{
			_output.Append('\"');

			int runIndex = -1;
			int l = s.Length;
			for (var index = 0; index < l; ++index)
			{
				var c = s[index];

				if (_useEscapedUnicode)
				{
					if (c >= ' ' && c < 128 && c != '\"' && c != '\\')
					{
						if (runIndex == -1)
							runIndex = index;

						continue;
					}
				}
				else
				{
					if (c != '\t' && c != '\n' && c != '\r' && c != '\"' && c != '\\')// && c != ':' && c!=',')
					{
						if (runIndex == -1)
							runIndex = index;

						continue;
					}
				}

				if (runIndex != -1)
				{
					_output.Append(s, runIndex, index - runIndex);
					runIndex = -1;
				}

				switch (c)
				{
					case '\t': _output.Append("\\t"); break;
					case '\r': _output.Append("\\r"); break;
					case '\n': _output.Append("\\n"); break;
					case '"':
					case '\\': _output.Append('\\'); _output.Append(c); break;
					default:
						if (_useEscapedUnicode)
						{
							_output.Append("\\u");
							_output.Append(((int)c).ToString("X4", NumberFormatInfo.InvariantInfo));
						}
						else
							_output.Append(c);

						break;
				}
			}

			if (runIndex != -1)
				_output.Append(s, runIndex, s.Length - runIndex);

			_output.Append('\"');
		}
	}
}
