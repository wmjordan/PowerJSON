using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace fastJSON.BonusPack
{
	/// <summary>
	/// Turns <see cref="IEnumerable{T}"/> collection into an <see cref="EnumerableDataReader"/>.
	/// </summary>
	public class EnumerableDataReader
	{
		/// <summary>
		/// Creates an <see cref="EnumerableDataReader&lt;T&gt;"/> instance from a given <see cref="IEnumerable&lt;T&gt;"/> instance.
		/// </summary>
		/// <typeparam name="T">The type of the data.</typeparam>
		/// <param name="collection">The data to be read.</param>
		/// <returns>An <see cref="EnumerableDataReader&lt;T&gt;"/> instance.</returns>
		public static EnumerableDataReader<T> Create<T> (IEnumerable<T> collection) {
			return new EnumerableDataReader<T> (collection);
		}
	}

	/// <summary>
	/// Experimental Feature:
	/// Converts <see cref="IEnumerable&lt;T&gt;"/> instances into <see cref="IDataReader"/> for <see cref="System.Data.SqlClient.SqlBulkCopy.WriteToServer(IDataReader)"/>.
	/// </summary>
	/// <remarks>References:
	/// 1) https://github.com/matthewschrager/Repository/blob/master/Repository.EntityFramework/EntityDataReader.cs;
	/// 2) http://www.codeproject.com/Articles/876276/Bulk-Insert-Into-SQL-From-Csharp</remarks>
	/// <typeparam name="T">The data type in the data source.</typeparam>
	public class EnumerableDataReader<T> : IDataReader
	{
		static Dictionary<Type, byte> _scalarTypes = InitScalarTypes ();

		private static Dictionary<Type, byte> InitScalarTypes () {
			return new Dictionary<Type, byte> () {
				{ typeof(string), 0 },

				{ typeof(byte), 0 },
				{ typeof(short), 0 },
				{ typeof(ushort), 0 },
				{ typeof(bool), 0 },
				{ typeof(int), 0 },
				{ typeof(uint), 0 },
				{ typeof(long), 0 },
				{ typeof(ulong), 0 },
				{ typeof(char), 0 },
				{ typeof(float), 0 },
				{ typeof(double), 0 },
				{ typeof(decimal), 0 },
				{ typeof(Guid), 0 },
				{ typeof(DateTime), 0 },
				{ typeof(TimeSpan), 0 },

				{ typeof(byte?), 0 },
				{ typeof(short?), 0 },
				{ typeof(ushort?), 0 },
				{ typeof(bool?), 0 },
				{ typeof(int?), 0 },
				{ typeof(uint?), 0 },
				{ typeof(long?), 0 },
				{ typeof(ulong?), 0 },
				{ typeof(char?), 0 },
				{ typeof(float?), 0 },
				{ typeof(double?), 0 },
				{ typeof(decimal?), 0 },
				{ typeof(Guid?), 0 },
				{ typeof(DateTime?), 0 },
				{ typeof(TimeSpan?), 0 }
			};
		}

		IEnumerator<T> _enumerator;
		int _fieldCount;
		string[] _memberNames;
		Getters[] _accessors;

		public EnumerableDataReader (IEnumerable<T> collection) : this (collection, false) { }
		public EnumerableDataReader (IEnumerable<T> collection, bool showReadOnlyValues) {
			var t = typeof(T);
			if (_scalarTypes.ContainsKey (t)) {
				throw new NotSupportedException (t.FullName + " is not supported.");
			}
			_enumerator = collection.GetEnumerator ();
			var p = SerializationManager.Instance.GetDefinition (t).Getters;
			_fieldCount = p.Length;
			_memberNames = new string[_fieldCount];
			_accessors = new Getters[_fieldCount];
			int c = 0;
			for (int i = 0; i < _fieldCount; i++) {
				var g = p[i];
				if (showReadOnlyValues == false && g.Serializable == TriState.False) {
					continue;
				}
				_memberNames[c] = p[i].SerializedName;
				_accessors[c] = p[i];
				++c;
			}
			if (c < _fieldCount) {
				_fieldCount = c;
				Array.Resize (ref _memberNames, c);
				Array.Resize (ref _accessors, c);
			}
		}

		public void Close () {
			_enumerator.Dispose ();
		}

		public int Depth {
			get { return 0; }
		}

		public DataTable GetSchemaTable () {
			DataTable t = new DataTable ();
			for (int i = 0; i < _fieldCount; i++) {
				DataRow row = t.NewRow ();
				row["ColumnName"] = this.GetName (i);
				row["ColumnOrdinal"] = i;
				Type type = this.GetFieldType (i);
				if (Reflection.Instance.IsNullable (type)) {
					type = Reflection.Instance.GetGenericArguments (type)[0];
				}
				row["DataType"] = this.GetFieldType (i);
				row["DataTypeName"] = this.GetDataTypeName (i);
				row["ColumnSize"] = -1;
				t.Rows.Add (row);
			}
			return t;
		}

		public bool IsClosed {
			get { return _enumerator == null; }
		}

		public bool NextResult () {
			return false;
		}

		public bool Read () {
			if (_enumerator == null) {
				throw new ObjectDisposedException ("EnumerableDataReader");
			}

			return _enumerator.MoveNext ();
		}

		public int RecordsAffected {
			get { return -1; }
		}

		public void Dispose () {
			if (_enumerator != null) {
				_enumerator.Dispose ();
				_enumerator = null;
			}
		}

		public int FieldCount {
			get { return _fieldCount; }
		}

		public bool GetBoolean (int i) {
			return Convert.ToBoolean (GetValue (i));
		}

		public byte GetByte (int i) {
			return Convert.ToByte (GetValue (i));
		}

		public long GetBytes (int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) {
			var buf = (byte[])GetValue (i);
			int bytes = Math.Min (length, buf.Length - (int)fieldOffset);
			Buffer.BlockCopy (buf, (int)fieldOffset, buffer, bufferoffset, bytes);
			return bytes;
		}

		public char GetChar (int i) {
			return Convert.ToChar (GetValue (i));
		}

		public long GetChars (int i, long fieldoffset, char[] buffer, int bufferoffset, int length) {
			string s = GetString (i);
			int chars = Math.Min (length, s.Length - (int)fieldoffset);
			s.CopyTo ((int)fieldoffset, buffer, bufferoffset, chars);
			return chars;
		}

		public IDataReader GetData (int i) {
			throw new NotImplementedException ();
		}

		public string GetDataTypeName (int i) {
			var a = _accessors[i];
			return a.MemberType.Name;
		}

		public DateTime GetDateTime (int i) {
			return Convert.ToDateTime (GetValue (i));
		}

		public decimal GetDecimal (int i) {
			return Convert.ToDecimal (GetValue (i));
		}

		public double GetDouble (int i) {
			return Convert.ToDouble (GetValue (i));
		}

		public Type GetFieldType (int i) {
			var a = _accessors[i];
			return a.MemberType;
		}

		public float GetFloat (int i) {
			return Convert.ToSingle (GetValue (i));
		}

		public Guid GetGuid (int i) {
			var v = GetValue (i);
			if (v is Guid || v  is Guid?) {
				return (Guid)v;
			}
			if (v is string) {
				return new Guid ((string)v);
			}
			if (v is byte[]) {
				return new Guid ((byte[])v);
			}
			return Guid.Empty;
		}

		public short GetInt16 (int i) {
			return Convert.ToInt16 (GetValue (i));
		}

		public int GetInt32 (int i) {
			return Convert.ToInt32 (GetValue (i));
		}

		public long GetInt64 (int i) {
			return Convert.ToInt64 (GetValue (i));
		}

		public string GetName (int i) {
			return _memberNames[i];
		}

		public int GetOrdinal (string name) {
			return Array.IndexOf (_memberNames, name);
		}

		public string GetString (int i) {
			return Convert.ToString (GetValue (i));
		}

		public object GetValue (int i) {
			return _accessors[i].Getter (_enumerator.Current);
		}

		public int GetValues (object[] values) {
			var vl = values.Length;
			var l = _fieldCount > vl ? vl : _fieldCount;
			for (int i = l - 1; i >= 0; i--) {
				values[i] = GetValue (i);
			}
			return l;
		}

		public bool IsDBNull (int i) {
			return Convert.IsDBNull (GetValue (i));
		}

		public object this[string name] {
			get { return GetValue (Array.IndexOf (_memberNames, name)); }
		}

		public object this[int i] {
			get { return GetValue (i); }
		}
	}
}
