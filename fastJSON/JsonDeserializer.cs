using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Collections.Specialized;

namespace fastJSON
{
	internal class JSONDeserializer
	{
		readonly JSONParameters _params;
		readonly SerializationManager _manager;
		readonly Dictionary<object, int> _circobj = new Dictionary<object, int> ();
		readonly Dictionary<int, object> _cirrev = new Dictionary<int, object> ();
		bool _usingglobals = false;

		public JSONDeserializer (JSONParameters param, SerializationManager manager) {
			_params = param;
			_manager = manager;
		}

		public T ToObject<T>(string json) {
			Type t = typeof(T);
			var o = ToObject (json, t);

			if (t.IsArray) {
				if ((o as ICollection).Count == 0) // edge case for "[]" -> T[]
				{
					Type tt = t.GetElementType ();
					object oo = Array.CreateInstance (tt, 0);
					return (T)oo;
				}
				else
					return (T)o;
			}
			else
				return (T)o;
		}

		public object ToObject (string json) {
			return ToObject (json, null);
		}

		public object ToObject (string json, Type type) {
			//_params = Parameters;
			_params.FixValues ();

			ReflectionCache c = null;
			if (type != null) {
				c = _manager.GetDefinition (type);
				if (c.CommonType == ComplexType.Dictionary
					|| c.CommonType == ComplexType.List) {
					_params.UsingGlobalTypes = false;
				}
			}
			_usingglobals = _params.UsingGlobalTypes;

			object o = new JsonParser (json).Decode ();
			if (o == null)
				return null;
#if !SILVERLIGHT
			if (type != null && type == typeof(DataSet))
				return CreateDataset (o as Dictionary<string, object>, null);

			if (type != null && type == typeof(DataTable))
				return CreateDataTable (o as Dictionary<string, object>, null);
#endif
			if (o is IDictionary) {
				if (type != null && c.CommonType == ComplexType.Dictionary)	// deserialize a dictionary
					return RootDictionary (o, type);
				else // deserialize an object
					return ParseDictionary (o as Dictionary<string, object>, null, type, null);
			}

			if (o is List<object>) {
				if (type != null) {
					if (c.CommonType == ComplexType.Dictionary)	// k/v format
						return RootDictionary (o, type);

					if (c.CommonType == ComplexType.List) // deserialize to generic list
						return RootList (o, type);

					if (type == typeof(Hashtable))
						return RootHashTable ((List<object>)o);

					if (type.IsArray) {
						return CreateArray ((List<object>)o, type, null);
					}
				}
				return (o as List<object>).ToArray ();
			}

			if (type != null && o.GetType () != type)
				return ChangeType (o, type);

			return o;
		}

		internal static RevertJsonValue GetReadJsonMethod (Type type) {
			return type == typeof(int) ? RevertInt32
				: type == typeof(long) ? RevertPrimitive
				: type == typeof(string) ? RevertPrimitive
				: type == typeof(bool) ? RevertPrimitive
				: type == typeof(double) ? RevertPrimitive
				: type == typeof(float) ? RevertSingle
				: type == typeof(byte) ? RevertByte
				: (RevertJsonValue)null;
		}

		#region RevertJsonValue delegate methods
		static object RevertPrimitive (JSONDeserializer deserializer, object value) {
			return value;
		}
		static object RevertInt32 (JSONDeserializer deserializer, object value) {
			return (int)(long)value;
		}
		static object RevertByte (JSONDeserializer deserializer, object value) {
			return (byte)(long)value;
		}
		static object RevertSingle (JSONDeserializer deserializer, object value) {
			return (float)(double)value;
		}
		#endregion

		#region [   p r i v a t e   m e t h o d s   ]
		private object RootHashTable (List<object> o) {
			Hashtable h = new Hashtable ();

			foreach (Dictionary<string, object> values in o) {
				object key = values["k"];
				object val = values["v"];
				if (key is Dictionary<string, object>)
					key = ParseDictionary ((Dictionary<string, object>)key, null, typeof(object), null);

				if (val is Dictionary<string, object>)
					val = ParseDictionary ((Dictionary<string, object>)val, null, typeof(object), null);

				h.Add (key, val);
			}

			return h;
		}

		private object RootList (object parse, Type type) {
			var c = _manager.GetDefinition (type);
			Type[] gtypes = c.ArgumentTypes;
			IList o = (IList)c.Instantiate ();
			foreach (var k in (IList)parse) {
				_usingglobals = false;
				object v = k;
				if (k is Dictionary<string, object>)
					v = ParseDictionary (k as Dictionary<string, object>, null, gtypes[0], null);
				// supports List<array>
				else if (k is List<object>) {
					v = CreateArray ((List<object>)k, gtypes[0], null);
				}
				else
					v = ChangeType (k, gtypes[0]);

				o.Add (v);
			}
			return o;
		}

		private object RootDictionary (object parse, Type type) {
			var c = _manager.GetDefinition (type);
			Type[] gtypes = c.ArgumentTypes;
			Type t1 = null;
			Type t2 = null;
			if (gtypes != null) {
				t1 = gtypes[0];
				t2 = gtypes[1];
			}
			if (parse is Dictionary<string, object>) {
				IDictionary o = (IDictionary)c.Instantiate ();

				foreach (var kv in (Dictionary<string, object>)parse) {
					object k = ChangeType (kv.Key, t1);
					object v = kv.Value;
					o.Add (k, ConvertObjectType (v, t2));
				}

				return o;
			}
			if (parse is List<object>)
				return CreateDictionary (parse as List<object>, type, gtypes, null);

			return null;
		}

		/// <summary>
		/// Converts parsed object to <paramref name="targetType"/>.
		/// </summary>
		/// <param name="source">The object created from <see cref="JsonParser"/>.</param>
		/// <param name="targetType">The type to convert to.</param>
		/// <returns>An object having <paramref name="targetType"/>.</returns>
		private object ConvertObjectType (object source, Type targetType) {
			if (source is Dictionary<string, object>)
				return ParseDictionary (source as Dictionary<string, object>, null, targetType, null);

			var c = _manager.GetDefinition (targetType);
			if (c.CommonType == ComplexType.Array) {
				return CreateArray ((List<object>)source, targetType, null);
			}
			if (c.CommonType == ComplexType.List) {
				return CreateGenericList ((List<object>)source, targetType, null);
			}
			// NOTE: replaced by the above three lines, t1: see RootDictionary
			//else if (v is IList)
			//	return CreateGenericList ((List<object>)v, t2, t1, null);
			return ChangeType (source, targetType);
		}

		internal object ParseDictionary (Dictionary<string, object> data, Dictionary<string, object> globaltypes, Type type, object input) {
			object tn = "";
			if (type == typeof(NameValueCollection))
				return CreateNameValueCollection (data);
			if (type == typeof(StringDictionary))
				return CreateStringDictionary (data);

			if (data.TryGetValue ("$i", out tn)) {
				object v = null;
				_cirrev.TryGetValue ((int)(long)tn, out v);
				return v;
			}

			if (data.TryGetValue ("$types", out tn)) {
				_usingglobals = true;
				globaltypes = new Dictionary<string, object> ();
				foreach (var kv in (Dictionary<string, object>)tn) {
					globaltypes.Add ((string)kv.Value, kv.Key);
				}
			}

			bool found = data.TryGetValue ("$type", out tn);
#if !SILVERLIGHT
			if (found == false && type == typeof(object)) {
				return data;	// CreateDataset(data, globaltypes);
			}
#endif
			if (found) {
				if (_usingglobals) {
					object tname = "";
					if (globaltypes != null && globaltypes.TryGetValue ((string)tn, out tname))
						tn = tname;
				}
				type = Reflection.Instance.GetTypeFromCache ((string)tn);
			}

			if (type == null)
				throw new JsonSerializationException ("Cannot determine type");

			object o = input;
			var def = _manager.GetDefinition (type);
			if (o == null) {
				if (_params.ParametricConstructorOverride)
					o = System.Runtime.Serialization.FormatterServices.GetUninitializedObject (type);
				else
					o = def.Instantiate ();
			}
			var si = def.Interceptor;
			if (si != null) {
				si.OnDeserializing (o);
			}
			int circount = 0;
			if (_circobj.TryGetValue (o, out circount) == false) {
				circount = _circobj.Count + 1;
				_circobj.Add (o, circount);
				_cirrev.Add (circount, o);
			}

			Dictionary<string, myPropInfo> props = def.Properties;
			foreach (string n in data.Keys) {
				if (n == "$map") {
					ProcessMap (o, props, (Dictionary<string, object>)data[n]);
					continue;
				}
				myPropInfo pi;
				if (props.TryGetValue (n, out pi) == false || pi.CanWrite == false)
					continue;
				object v = data[n];

				if (pi.Converter != null) {
					var tc = pi.Converter as ITypeConverter;
					var xv = v;
					if (tc != null && xv != null) {
						var st = tc.SerializedType;
						if (st != null && st != typeof(object)) {
							if (v is Dictionary<string, object>) {
								xv = ParseDictionary ((Dictionary<string, object>)xv, globaltypes, st, pi.Getter (o));
							}
							else if (v is List<object>) {
								xv = CreateGenericList ((List<object>)xv, st, globaltypes);
							}
							else if (pi.MemberType.Equals (xv.GetType ()) == false) {
								xv = ChangeType (xv, st);
							}
						}
					}
					var cv = pi.Converter.DeserializationConvert (n, xv);
					if (ReferenceEquals (cv, xv) == false) {
						// use the converted value
						if (cv != null || pi.IsClass || pi.IsNullable) {
							if (si != null && si.OnDeserializing (o, n, ref cv) == false) {
								continue;
							}
							o = pi.Setter (o, cv);
						}
						continue;
					}
				}
				if (v == null) {
					if (si != null && si.OnDeserializing (o, n, ref v) == false) {
						continue;
					}
					if (pi.IsClass || pi.IsNullable) {
						o = pi.Setter (o, v);
					}
					continue;
				}

				object oset = null;

				switch (pi.JsonDataType) {
					case JsonDataType.Undefined: goto default;
					case JsonDataType.Int: oset = (int)((long)v); break;
					case JsonDataType.String:
					case JsonDataType.Bool:
					case JsonDataType.Long:
					case JsonDataType.Double: oset = v; break;
					case JsonDataType.Single: oset = (float)((double)v); break;
					case JsonDataType.DateTime: oset = CreateDateTime ((string)v); break;
					case JsonDataType.Enum: oset = CreateEnum (pi.MemberType, v); break;
					case JsonDataType.Guid: oset = CreateGuid ((string)v); break;
					case JsonDataType.TimeSpan: oset = CreateTimeSpan ((string)v); break;

					case JsonDataType.Array:
						if (!pi.IsValueType)
							oset = CreateArray ((List<object>)v, pi.MemberType, globaltypes);
						// what about 'else'?
						break;
					case JsonDataType.ByteArray: oset = Convert.FromBase64String ((string)v); break;
#if !SILVERLIGHT
					case JsonDataType.DataSet: oset = CreateDataset ((Dictionary<string, object>)v, globaltypes); break;
					case JsonDataType.DataTable: oset = CreateDataTable ((Dictionary<string, object>)v, globaltypes); break;
					case JsonDataType.Hashtable: // same case as Dictionary
#endif
					case JsonDataType.Dictionary: oset = CreateDictionary ((List<object>)v, pi.MemberType, pi.GenericTypes, globaltypes); break;
					case JsonDataType.StringKeyDictionary: oset = CreateStringKeyDictionary ((Dictionary<string, object>)v, pi.MemberType, pi.GenericTypes, globaltypes); break;
					case JsonDataType.NameValue: oset = CreateNameValueCollection ((Dictionary<string, object>)v); break;
					case JsonDataType.StringDictionary: oset = CreateStringDictionary ((Dictionary<string, object>)v); break;
					case JsonDataType.Custom: oset = _manager.CreateCustom ((string)v, pi.MemberType); break;
					default:
						if (pi.IsGenericType && pi.IsValueType == false && v is List<object>)
							oset = CreateGenericList ((List<object>)v, pi.MemberType, globaltypes);

						else if ((pi.IsClass || pi.IsStruct) && v is Dictionary<string, object>)
							oset = ParseDictionary ((Dictionary<string, object>)v, globaltypes, pi.MemberType, pi.Getter (o));

						else if (v is List<object>)
							oset = CreateArray ((List<object>)v, typeof(object[]), globaltypes);

						else if (pi.IsValueType)
							oset = ChangeType (v, pi.ChangeType);

						else
							oset = v;

						break;
				}

				if (si != null && si.OnDeserializing (o, n, ref oset) == false) {
					continue;
				}
				o = pi.Setter (o, oset);
			}
			if (si != null) {
				si.OnDeserialized (o);
			}
			return o;
		}

		private StringDictionary CreateStringDictionary (Dictionary<string, object> d) {
			StringDictionary nv = new StringDictionary ();

			foreach (var o in d)
				nv.Add (o.Key, (string)o.Value);

			return nv;
		}

		private NameValueCollection CreateNameValueCollection (Dictionary<string, object> d) {
			NameValueCollection nv = new NameValueCollection ();

			foreach (var o in d) {
				var k = o.Key;
				var ov = o.Value;
				if (ov == null) {
					nv.Add (k, null);
					continue;
				}
				var s = ov as string;
				if (s != null) {
					nv.Add (k, s);
					continue;
				}
				var sa = ov as IList;
				if (sa != null) {
					foreach (string item in sa) {
						nv.Add (k, item);
					}
					continue;
				}
				nv.Add (k, ov.ToString ());
			}

			return nv;
		}

		private void ProcessMap (object obj, Dictionary<string, myPropInfo> props, Dictionary<string, object> dic) {
			foreach (KeyValuePair<string, object> kv in dic) {
				myPropInfo p = props[kv.Key];
				object o = p.Getter (obj);
				Type t = Type.GetType ((string)kv.Value);
				if (t == typeof(Guid))
					p.Setter (obj, CreateGuid ((string)o));
			}
		}

		private object ChangeType (object value, Type conversionType) {
			if (conversionType == typeof(int))
				return (int)((long)value);

			if (conversionType == typeof(long))
				return (long)value;

			if (conversionType == typeof(string))
				return value;

			if (conversionType == typeof(double)) {
				return (double)value;
			}
			if (conversionType == typeof(float)) {
				return (float)(double)value;
			}
			if (conversionType.IsEnum)
				return CreateEnum (conversionType, value);

			if (conversionType == typeof(DateTime))
				return CreateDateTime ((string)value);

			if (_manager.IsTypeRegistered (conversionType))
				return _manager.CreateCustom ((string)value, conversionType);

			// 8-30-2014 - James Brooks - Added code for nullable types.
			if (conversionType.IsGenericType) {
				var c = _manager.GetDefinition (conversionType);
				if (c.CommonType == ComplexType.Nullable) {
					if (value == null) {
						return value;
					}
					conversionType = c.ArgumentTypes[0];
				}
			}

			// 8-30-2014 - James Brooks - Nullable Guid is a special case so it was moved after the "IsNullable" check.
			if (conversionType == typeof(Guid))
				return CreateGuid ((string)value);

			return Convert.ChangeType (value, conversionType, CultureInfo.InvariantCulture);
		}

		private object CreateEnum (Type enumType, object value) {
			var s = value as string;
			if (s != null) {
				return _manager.GetEnumValue (enumType, s);
			}
			else {
				return Enum.ToObject (enumType, value);
			}
		}

		private static Guid CreateGuid (string s) {
			if (s.Length > 30)
				return new Guid (s);
			else
				return new Guid (Convert.FromBase64String (s));
		}

		private DateTime CreateDateTime (string value) {
			bool utc = false;
			//                   0123456789012345678 9012 9/3
			// datetime format = yyyy-MM-ddTHH:mm:ss .nnn  Z
			int year;
			int month;
			int day;
			int hour;
			int min;
			int sec;
			int ms = 0;

			year = CreateInteger (value, 0, 4);
			month = CreateInteger (value, 5, 2);
			day = CreateInteger (value, 8, 2);
			hour = CreateInteger (value, 11, 2);
			min = CreateInteger (value, 14, 2);
			sec = CreateInteger (value, 17, 2);
			if (value.Length > 21 && value[19] == '.')
				ms = CreateInteger (value, 20, 3);

			if (value[value.Length - 1] == 'Z')
				utc = true;

			if (_params.UseUTCDateTime == false || utc == false)
				return new DateTime (year, month, day, hour, min, sec, ms);
			else
				return new DateTime (year, month, day, hour, min, sec, ms, DateTimeKind.Utc).ToLocalTime ();
		}

		private static int CreateInteger (string s, int index, int count) {
			int num = 0;
			bool neg = false;
			for (int x = 0; x < count; x++, index++) {
				char cc = s[index];

				if (cc == '-')
					neg = true;
				else if (cc == '+')
					neg = false;
				else {
					num *= 10;
					num += (cc - '0');
				}
			}
			if (neg) num = -num;

			return num;
		}

		private object CreateTimeSpan (string value) {
			// TODO: Optimize TimeSpan
			return TimeSpan.Parse (value);
		}

		private object CreateArray (List<object> data, Type arrayType, Dictionary<string, object> globalTypes) {
			var l = data.Count;
			var c = _manager.GetDefinition (arrayType);
			Type et = c.ArgumentTypes[0];
			Array col = Array.CreateInstance (et, l);
			var r = c.ItemDeserializer;
			if (r != null) {
				for (int i = 0; i < l; i++) {
					object ob = data[i];
					if (ob == null) {
						continue;
					}
					col.SetValue (r (this, ob), i);
				}
				return col;
			}

			// creates an array of objects
			for (int i = 0; i < l; i++) {
				object ob = data[i];
				if (ob == null) {
					continue;
				}
				if (ob is IDictionary)
					col.SetValue (ParseDictionary ((Dictionary<string, object>)ob, globalTypes, et, null), i);
				// Supports multi-dimensional array
				else if (ob is ICollection) {
					col.SetValue (CreateArray ((List<object>)ob, et, globalTypes), i);
				}
				else
					col.SetValue (ChangeType (ob, et), i);
			}

			return col;
		}

		private object CreateGenericList (List<object> data, Type listType, Dictionary<string, object> globalTypes) {
			var c = _manager.GetDefinition (listType);
			IList col = (IList)c.Instantiate ();
			var r = c.ItemDeserializer;
			if (r != null) {
				foreach (var item in data) {
					if (item == null) {
						continue;
					}
					col.Add (r (this, item));
				}
				return col;
			}

			// create an array of objects
			foreach (object ob in data) {
				if (ob is IDictionary)
					col.Add (ParseDictionary ((Dictionary<string, object>)ob, globalTypes, c.ArgumentTypes[0], null));

				else if (ob is List<object>) {
					if (c.ArgumentTypes[0].IsGenericType)
						col.Add (ob);//).ToArray());
					else
						col.Add (((List<object>)ob).ToArray ());
				}
				else
					col.Add (ChangeType (ob, c.ArgumentTypes[0]));
			}
			return col;
		}

		private object CreateStringKeyDictionary (Dictionary<string, object> reader, Type pt, Type[] types, Dictionary<string, object> globalTypes) {
			var col = (IDictionary)_manager.GetDefinition (pt).Instantiate ();
			Type t1 = null;
			Type t2 = null;
			if (types != null) {
				t1 = types[0];
				t2 = types[1];
			}

			foreach (KeyValuePair<string, object> values in reader) {
				var key = values.Key;
				object val = null;

				if (values.Value is Dictionary<string, object>)
					val = ParseDictionary ((Dictionary<string, object>)values.Value, globalTypes, t2, null);

				else if (types != null && t2.IsArray)
					val = CreateArray ((List<object>)values.Value, t2, globalTypes);

				else if (values.Value is IList)
					val = CreateGenericList ((List<object>)values.Value, t2, globalTypes);

				else
					val = ChangeType (values.Value, t2);

				col.Add (key, val);
			}

			return col;
		}

		private object CreateDictionary (List<object> reader, Type pt, Type[] types, Dictionary<string, object> globalTypes) {
			IDictionary col = (IDictionary)_manager.GetDefinition (pt).Instantiate ();
			Type t1 = null;
			Type t2 = null;
			if (types != null) {
				t1 = types[0];
				t2 = types[1];
			}

			foreach (Dictionary<string, object> values in reader) {
				object key = values["k"];
				object val = values["v"];

				if (key is Dictionary<string, object>)
					key = ParseDictionary ((Dictionary<string, object>)key, globalTypes, t1, null);
				else
					key = ChangeType (key, t1);

				if (val is Dictionary<string, object>)
					val = ParseDictionary ((Dictionary<string, object>)val, globalTypes, t2, null);
				else
					val = ChangeType (val, t2);

				col.Add (key, val);
			}

			return col;
		}

#if !SILVERLIGHT
		private DataSet CreateDataset (Dictionary<string, object> reader, Dictionary<string, object> globalTypes) {
			DataSet ds = new DataSet ();
			ds.EnforceConstraints = false;
			ds.BeginInit ();

			// read dataset schema here
			var schema = reader["$schema"];

			if (schema is string) {
				TextReader tr = new StringReader ((string)schema);
				ds.ReadXmlSchema (tr);
			}
			else {
				DatasetSchema ms = (DatasetSchema)ParseDictionary ((Dictionary<string, object>)schema, globalTypes, typeof(DatasetSchema), null);
				ds.DataSetName = ms.Name;
				for (int i = 0; i < ms.Info.Count; i += 3) {
					if (ds.Tables.Contains (ms.Info[i]) == false)
						ds.Tables.Add (ms.Info[i]);
					ds.Tables[ms.Info[i]].Columns.Add (ms.Info[i + 1], Type.GetType (ms.Info[i + 2]));
				}
			}

			foreach (KeyValuePair<string, object> pair in reader) {
				if (pair.Key == "$type" || pair.Key == "$schema") continue;

				List<object> rows = (List<object>)pair.Value;
				if (rows == null) continue;

				DataTable dt = ds.Tables[pair.Key];
				ReadDataTable (rows, dt);
			}

			ds.EndInit ();

			return ds;
		}

		private void ReadDataTable (List<object> rows, DataTable dt) {
			dt.BeginInit ();
			dt.BeginLoadData ();
			List<int> guidcols = new List<int> ();
			List<int> datecol = new List<int> ();

			foreach (DataColumn c in dt.Columns) {
				if (c.DataType == typeof(Guid) || c.DataType == typeof(Guid?))
					guidcols.Add (c.Ordinal);
				if (_params.UseUTCDateTime && (c.DataType == typeof(DateTime) || c.DataType == typeof(DateTime?)))
					datecol.Add (c.Ordinal);
			}

			foreach (List<object> row in rows) {
				object[] v = new object[row.Count];
				row.CopyTo (v, 0);
				foreach (int i in guidcols) {
					string s = (string)v[i];
					if (s != null && s.Length < 36)
						v[i] = new Guid (Convert.FromBase64String (s));
				}
				if (_params.UseUTCDateTime) {
					foreach (int i in datecol) {
						string s = (string)v[i];
						if (s != null)
							v[i] = CreateDateTime (s);
					}
				}
				dt.Rows.Add (v);
			}

			dt.EndLoadData ();
			dt.EndInit ();
		}

		DataTable CreateDataTable (Dictionary<string, object> reader, Dictionary<string, object> globalTypes) {
			var dt = new DataTable ();

			// read dataset schema here
			var schema = reader["$schema"];

			if (schema is string) {
				TextReader tr = new StringReader ((string)schema);
				dt.ReadXmlSchema (tr);
			}
			else {
				var ms = (DatasetSchema)ParseDictionary ((Dictionary<string, object>)schema, globalTypes, typeof(DatasetSchema), null);
				dt.TableName = ms.Info[0];
				for (int i = 0; i < ms.Info.Count; i += 3) {
					dt.Columns.Add (ms.Info[i + 1], Type.GetType (ms.Info[i + 2]));
				}
			}

			foreach (var pair in reader) {
				if (pair.Key == "$type" || pair.Key == "$schema")
					continue;

				var rows = (List<object>)pair.Value;
				if (rows == null)
					continue;

				if (!dt.TableName.Equals (pair.Key, StringComparison.InvariantCultureIgnoreCase))
					continue;

				ReadDataTable (rows, dt);
			}

			return dt;
		}
#endif
		#endregion
	}
}
