using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Collections.Specialized;

namespace fastJSON
{
	internal class JsonDeserializer
	{
		static readonly RevertJsonValue[] _revertMethods = RegisterMethods ();

		readonly JSONParameters _params;
		readonly SerializationManager _manager;
		readonly Dictionary<object, int> _circobj = new Dictionary<object, int> ();
		readonly Dictionary<int, object> _cirrev = new Dictionary<int, object> ();
		bool _usingglobals = false;
		JsonDict globaltypes;

		private static RevertJsonValue[] RegisterMethods () {
			var r = new RevertJsonValue[Enum.GetNames (typeof (JsonDataType)).Length];
			r[(int)JsonDataType.Array] = RevertArray;
			r[(int)JsonDataType.Bool] = RevertPrimitive;
			r[(int)JsonDataType.ByteArray] = RevertByteArray;
			r[(int)JsonDataType.Custom] = RevertCustom;
			r[(int)JsonDataType.DataSet] = RevertDataSet;
			r[(int)JsonDataType.DataTable] = RevertDataTable;
			r[(int)JsonDataType.DateTime] = RevertDateTime;
			r[(int)JsonDataType.Dictionary] = RevertDictionary;
			r[(int)JsonDataType.Double] = RevertPrimitive;
			r[(int)JsonDataType.Enum] = RevertEnum;
			r[(int)JsonDataType.List] = RevertList;
			r[(int)JsonDataType.Guid] = RevertGuid;
			r[(int)JsonDataType.Hashtable] = RevertHashTable;
			r[(int)JsonDataType.Int] = RevertInt32;
			r[(int)JsonDataType.Long] = RevertPrimitive;
			r[(int)JsonDataType.MultiDimensionalArray] = RevertMultiDimensionalArray;
			r[(int)JsonDataType.NameValue] = RevertNameValueCollection;
			r[(int)JsonDataType.Object] = RevertUndefined;
			r[(int)JsonDataType.Single] = RevertSingle;
			r[(int)JsonDataType.String] = RevertPrimitive;
			r[(int)JsonDataType.StringDictionary] = RevertStringDictionary;
			r[(int)JsonDataType.StringKeyDictionary] = RevertStringKeyDictionary;
			r[(int)JsonDataType.TimeSpan] = RevertTimeSpan;
			r[(int)JsonDataType.Undefined] = RevertUndefined;
			return r;
		}

		public JsonDeserializer (JSONParameters param, SerializationManager manager) {
			_params = param;
			_manager = manager;
		}

		public T ToObject<T>(string json) {
			Type t = typeof (T);
			var o = ToObject (json, t);

			//if (t.IsArray) {
			//	if ((o as ICollection).Count == 0) // edge case for "[]" -> T[]
			//	{
			//		Type tt = t.GetElementType ();
			//		object oo = Array.CreateInstance (tt, 0);
			//		return (T)oo;
			//	}
			//	else
			//		return (T)o;
			//}
			//else
				return (T)o;
		}

		public object ToObject (string json) {
			return ToObject (json, null);
		}

		public object ToObject (string json, Type type) {
			ReflectionCache c = null;
			RevertJsonValue m = null;
			if (type != null) {
				c = _manager.GetReflectionCache (type);
				if (c.CommonType == ComplexType.Dictionary
					|| c.CommonType == ComplexType.List) {
					_usingglobals = false;
				}
				m = c.DeserializeMethod;
			}
			else {
				_usingglobals = _params.UsingGlobalTypes;
			}

			object o = new JsonParser (json).Decode ();
			if (o == null)
				return null;
			if (m != null) {
				return m (this, o, type);
			}

			var d = o as JsonDict;
			if (d != null) {
				if (type != null) {
#if !SILVERLIGHT
					if (c.JsonDataType == JsonDataType.DataSet)
						return CreateDataSet (d);

					if (c.JsonDataType == JsonDataType.DataTable)
						return CreateDataTable (d);
#endif
					if (c.CommonType == ComplexType.Dictionary) // deserialize a dictionary
						return RootDictionary (o, type);
				}
				// deserialize an object
				return ParseDictionary (d, type, null);
			}
			var a = o as JsonArray;
			if (a != null) {
				if (type != null) {
					if (c.CommonType == ComplexType.Dictionary) // k/v format
						return RootDictionary (o, type);

					if (c.CommonType == ComplexType.List) // deserialize to generic list
						return RootList (o, type);

					if (c.JsonDataType == JsonDataType.Hashtable)
						return RootHashTable (a);

					if (c.CommonType == ComplexType.Array) {
						return CreateArray (a, type);
					}
				}
				return a.ToArray ();
			}

			if (type != null && o.GetType ().Equals (type) == false)
				return ChangeType (o, type);

			return o;
		}

		RevertJsonValue GetRevertMethod (Type type) {
			if (type == null) {
				return RevertUndefined;
			}
			var c = _manager.GetReflectionCache (type);
			return c.DeserializeMethod;
		}

		internal static RevertJsonValue GetReadJsonMethod (Type type) {
			if (type == null) {
				return RevertUndefined;
			}
			var d = Reflection.GetJsonDataType (type);
			if (d != JsonDataType.Primitive) {
				return GetRevertMethod (d);
			}
			return typeof (byte).Equals (type) ? RevertByte
				: typeof (decimal).Equals (type) ? RevertDecimal
				: typeof (char).Equals (type) ? RevertChar
				: typeof (sbyte).Equals (type) ? RevertSByte
				: typeof (short).Equals (type) ? RevertShort
				: typeof (ushort).Equals (type) ? RevertUShort
				: typeof (uint).Equals (type) ? RevertUInt32
				: typeof (ulong).Equals (type) ? RevertUInt64
				: (RevertJsonValue)RevertUndefined;
		}

		#region [   p r i v a t e   m e t h o d s   ]
		private object RootHashTable (JsonArray o) {
			Hashtable h = new Hashtable ();

			foreach (JsonDict values in o) {
				object key = values["k"];
				object val = values["v"];
				if (key is JsonDict)
					key = ParseDictionary ((JsonDict)key, typeof (object), null);

				if (val is JsonDict)
					val = ParseDictionary ((JsonDict)val, typeof (object), null);

				h.Add (key, val);
			}

			return h;
		}

	private object RootList (object parse, Type type) {
		var c = _manager.GetReflectionCache (type);
		var et = c.ArgumentTypes[0];
		var m = GetRevertMethod (Reflection.GetJsonDataType (et));
		IList o = (IList)c.Instantiate ();
		foreach (var k in (IList)parse) {
			_usingglobals = false;
			object v = m (this, k, et);
			o.Add (v);
		}
		return o;
	}

	private object RootDictionary (object parse, Type type) {
		var c = _manager.GetReflectionCache (type);
			Type[] gtypes = c.ArgumentTypes;
			Type t1 = null;
			Type t2 = null;
			if (gtypes != null) {
				t1 = gtypes[0];
				t2 = gtypes[1];
			}
			var mk = GetRevertMethod (t1);
			var m = GetRevertMethod (t2);
			var d = parse as JsonDict;
			if (d != null) {
				IDictionary o = (IDictionary)c.Instantiate ();

				foreach (var kv in d) {
					o.Add (mk (this, kv.Key, t1), m (this, kv.Value, t2));
				}

				return o;
			}
			var a = parse as JsonArray;
			if (a != null)
				return CreateDictionary (a, type);

			return null;
		}

		internal object ParseDictionary (JsonDict data, Type type, object input) {
			//if (typeof (NameValueCollection).Equals (type))
			//	return CreateNameValueCollection (data);
			//if (typeof (StringDictionary).Equals (type))
			//	return CreateStringDictionary (data);

			if (data.RefIndex > 0) {
				object v = null;
				_cirrev.TryGetValue (data.RefIndex, out v);
				return v;
			}

			if (data.Types != null && data.Types.Count > 0) {
				_usingglobals = true;
				globaltypes = new JsonDict ();
				foreach (var kv in data.Types) {
					globaltypes.Add ((string)kv.Value, kv.Key);
				}
			}

			var tn = data.Type;
			bool found = (tn != null && tn.Length > 0);
#if !SILVERLIGHT
			if (found == false && typeof (object).Equals (type)) {
				return data;    // CreateDataset(data, globaltypes);
			}
#endif
			if (found) {
				if (_usingglobals) {
					object tname = "";
					if (globaltypes != null && globaltypes.TryGetValue (data.Type, out tname))
						tn = (string)tname;
				}
				type = Reflection.Instance.GetTypeFromCache (tn);
			}

			if (type == null)
				throw new JsonSerializationException ("Cannot determine type");

			object o = input;
			var c = _manager.GetReflectionCache (type);
			if (o == null) {
				if (_params.ParametricConstructorOverride)
					o = System.Runtime.Serialization.FormatterServices.GetUninitializedObject (type);
				else
					o = c.Instantiate ();
			}
			int circount = 0;
			if (_circobj.TryGetValue (o, out circount) == false) {
				circount = _circobj.Count + 1;
				_circobj.Add (o, circount);
				_cirrev.Add (circount, o);
			}

			var si = c.Interceptor;
			if (si != null) {
				si.OnDeserializing (o);
			}
			Dictionary<string, myPropInfo> props = c.Properties;
			//TODO: Candidate to removal of unknown use of map
			//if (data.Map != null) {
			//	ProcessMap (o, props, data.Map);
			//}
			foreach (var kv in data) {
				var n = kv.Key;
				var v = kv.Value;
				myPropInfo pi;
				if (props.TryGetValue (n, out pi) == false || pi.CanWrite == false && pi.JsonDataType != JsonDataType.List)
					continue;
				var ji = new JsonItem (n, v, false);
				bool converted = false;
				if (v is IList && pi.ItemConverter != null) {
					converted = ConvertItems (pi, ji);
				}
				if (pi.Converter != null) {
					ConvertProperty (o, pi, ji);
				}

				object oset = null;
				// use the converted value
				if (converted || ReferenceEquals (ji._Value, v) == false) {
					if (pi.CanWrite == false && pi.JsonDataType == JsonDataType.List) {
						ji._Value = CreateList ((JsonArray)ji._Value, pi.MemberType, pi.Getter (o));
					}
					if (ji._Value != null || pi.IsClass || pi.IsNullable) {
						oset = ji._Value;
						goto SET_VALUE;
					}
					continue;
				}
				// process null value
				if (ji._Value == null) {
					var i = new JsonItem (n, null);
					if (si != null && si.OnDeserializing (o, i) == false) {
						continue;
					}
					if (i.Value != null || pi.IsClass || pi.IsNullable) {
						o = pi.Setter (o, i.Value);
					}
					continue;
				}
				v = ji._Value;
				// set member value
				switch (pi.JsonDataType) {
					case JsonDataType.Undefined: goto default;
					case JsonDataType.Int: oset = (int)(long)v; break;
					case JsonDataType.String:
					case JsonDataType.Bool:
					case JsonDataType.Long:
					case JsonDataType.Double: oset = v; break;
					case JsonDataType.Single: oset = (float)(double)v; break;
					case JsonDataType.DateTime: oset = CreateDateTime (this, v); break;
					case JsonDataType.Enum: oset = CreateEnum (v, pi.MemberType); break;
					case JsonDataType.Guid: oset = CreateGuid (v); break;
					case JsonDataType.TimeSpan: oset = CreateTimeSpan (v); break;

					case JsonDataType.List:
						oset = CreateList ((JsonArray)v, pi.MemberType, pi.CanWrite == false ? pi.Getter (o) : null);
						break;
					case JsonDataType.Array:
						if (!pi.IsValueType)
							oset = CreateArray ((JsonArray)v, pi.MemberType);
						// what about 'else'?
						break;
					case JsonDataType.MultiDimensionalArray:
						oset = CreateMultiDimensionalArray ((JsonArray)v, pi.MemberType);
						break;
					case JsonDataType.ByteArray: oset = Convert.FromBase64String ((string)v); break;
#if !SILVERLIGHT
					case JsonDataType.DataSet: oset = CreateDataSet ((JsonDict)v); break;
					case JsonDataType.DataTable: oset = CreateDataTable ((JsonDict)v); break;
					case JsonDataType.Hashtable: // same case as Dictionary
#endif
					case JsonDataType.Dictionary:
						oset = CreateDictionary ((JsonArray)v, pi.MemberType);
						break;
					case JsonDataType.StringKeyDictionary:
						oset = CreateStringKeyDictionary ((JsonDict)v, pi.MemberType);
						break;
					case JsonDataType.NameValue:
						oset = CreateNameValueCollection ((JsonDict)v);
						break;
					case JsonDataType.StringDictionary:
						oset = CreateStringDictionary ((JsonDict)v);
						break;
					case JsonDataType.Custom:
						oset = _manager.CreateCustom ((string)v, pi.MemberType);
						break;
					case JsonDataType.Object: oset = v; break;
					default:
						if ((pi.IsClass || pi.IsStruct) && v is JsonDict)
							oset = ParseDictionary ((JsonDict)v, pi.MemberType, pi.Getter (o));

						else if (v is JsonArray)
							oset = CreateArray ((JsonArray)v, typeof (object[]));

						else if (pi.IsValueType)
							oset = ChangeType (v, pi.ChangeType);

						else
							oset = v;

						break;
				}
				SET_VALUE:
				ji.Value = oset;
				if (si != null) {
					if (si.OnDeserializing (o, ji) == false) {
						continue;
					}
				}
				if (pi.Setter != null) {
					o = pi.Setter (o, ji.Value);
				}
			}
			if (si != null) {
				si.OnDeserialized (o);
			}
			return o;
		}

		private void ConvertProperty (object o, myPropInfo pi, JsonItem ji) {
			var pc = pi.Converter;
			var rt = pc.GetReversiveType (ji);
			var xv = ji._Value;
			if (xv != null && rt != null && typeof (object).Equals (rt) == false && pi.MemberType.Equals (xv.GetType ()) == false) {
				var jt = Reflection.GetJsonDataType (rt);
				if (jt != JsonDataType.Undefined) {
					var m = GetRevertMethod (rt);
					xv = m (this, xv, rt);
				}
				else if (xv is JsonDict) {
					xv = ParseDictionary ((JsonDict)xv, rt, pi.Getter (o));
				}
			}
			ji._Value = xv;
			pc.DeserializationConvert (ji);
		}

		private bool ConvertItems (myPropInfo pi, JsonItem ji) {
			var vl = ji._Value as IList;
			var l = vl.Count;
			var converted = false;
			var ai = new JsonItem (ji._Name, null);
			for (int i = 0; i < l; i++) {
				var vi = vl[i];
				ai._Value = vi;
				pi.ItemConverter.DeserializationConvert (ai);
				if (ReferenceEquals (vi, ai._Value) == false) {
					vl[i] = ai._Value;
					converted = true;
				}
			}
			if (converted) {
				if (pi.JsonDataType == JsonDataType.Array) {
					ji._Value = Array.CreateInstance (pi.ElementType, l);
					vl.CopyTo ((Array)ji._Value, 0);
				}
				else if (pi.JsonDataType == JsonDataType.List) {
					ji._Value = _manager.GetReflectionCache (pi.MemberType).Instantiate ();
					var gl = ji._Value as IList;
					for (int i = 0; i < l; i++) {
						gl.Add (vl[i]);
					}
				}
			}

			return converted;
		}

		private static StringDictionary CreateStringDictionary (JsonDict d) {
			StringDictionary nv = new StringDictionary ();

			foreach (var o in d)
				nv.Add (o.Key, (string)o.Value);

			return nv;
		}

		private static NameValueCollection CreateNameValueCollection (JsonDict d) {
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

		// TODO: Candidate to removal of unknown use of map
		//private static void ProcessMap (object obj, Dictionary<string, myPropInfo> props, JsonDict dic) {
		//	foreach (KeyValuePair<string, object> kv in dic) {
		//		myPropInfo p = props[kv.Key];
		//		object o = p.Getter (obj);
		//		Type t = Type.GetType ((string)kv.Value);
		//		if (typeof (Guid).Equals (t))
		//			p.Setter (obj, CreateGuid (o));
		//	}
		//}

		private object ChangeType (object value, Type conversionType) {
			// 8-30-2014 - James Brooks - Added code for nullable types.
			if (conversionType.IsGenericType) {
				var c = _manager.GetReflectionCache (conversionType);
				if (c.CommonType == ComplexType.Nullable) {
					if (value == null) {
						return value;
					}
					conversionType = c.ArgumentTypes[0];
				}
			}

			// 8-30-2014 - James Brooks - Nullable Guid is a special case so it was moved after the "IsNullable" check.
			if (typeof (Guid).Equals (conversionType))
				return CreateGuid (value);

			return Convert.ChangeType (value, conversionType, CultureInfo.InvariantCulture);
		}

		private object CreateEnum (object value, Type enumType) {
			var s = value as string;
			if (s != null) {
				return _manager.GetEnumValue (enumType, s);
			}
			else {
				return Enum.ToObject (enumType, value);
			}
		}

		private object CreateArray (JsonArray data, Type arrayType) {
			var l = data.Count;
			var c = _manager.GetReflectionCache (arrayType);
			Type et = c.ArgumentTypes[0];
			Array col = Array.CreateInstance (et, l);
			var r = c.ItemDeserializer;
			if (r != null) {
				for (int i = 0; i < l; i++) {
					var ob = data[i];
					if (ob == null) {
						continue;
					}
					col.SetValue (r (this, ob, et), i);
				}
				return col;
			}

			// TODO: revert multi-dimensional array
			// TODO: candidate of code clean-up
			// create an array of objects
			for (int i = 0; i < l; i++) {
				var ob = data[i];
				if (ob == null) {
					continue;
				}
				if (ob is IDictionary)
					col.SetValue (ParseDictionary ((JsonDict)ob, et, null), i);
				// support jagged array
				else if (ob is ICollection) {
					col.SetValue (CreateArray ((JsonArray)ob, et), i);
				}
				else
					col.SetValue (ChangeType (ob, et), i);
			}

			return col;
		}

		private object CreateMultiDimensionalArray (JsonArray data, Type arrayType) {
			var c = _manager.GetReflectionCache (arrayType);
			Type et = c.ArgumentTypes[0];
			var ar = arrayType.GetArrayRank ();
			var ub = new int[ar];
			var d = data;
			// get upper bounds
			for (int i = 0; i < ar; i++) {
				var l = d.Count;
				ub[i] = l;
				if (i == ar - 1) {
					break;
				}
				JsonArray a = null;
				for (int j = 0; j < l; j++) {
					a = d[j] as JsonArray;
					if (d != null) {
						d = a;
						break;
					}
				}
				if (a == null) {
					throw new JsonSerializationException ("The rank of the multi-dimensional array does not match.");
				}
			}
			var mdi = new int[ar];
			Array col = Array.CreateInstance (et, ub);
			var m = c.ItemDeserializer;
			var ri = 0;
			SetMultiDimensionalArrayValue (data, et, ub, mdi, col, m, ri);
			return col;
		}

		private void SetMultiDimensionalArrayValue (JsonArray data, Type et, int[] ub, int[] mdi, Array col, RevertJsonValue m, int ri) {
			if (ri + 1 == ub.Length) {
				foreach (var item in data) {
					col.SetValue (m (this, item, et), mdi);
					++mdi[ri];
				}
				return;
			}
			for (int i = 0; i < ub[ri]; i++) {
				var ob = data[mdi[ri]] as JsonArray;
				if (ob == null) {
					continue;
				}

				else {
					for (int j = mdi.Length - 1; j > ri; j--) {
						mdi[j] = 0;
					}
					SetMultiDimensionalArrayValue (ob, et, ub, mdi, col, m, ri + 1);
					++mdi[ri];
				}
			}
		}

		private object CreateList (JsonArray data, Type listType, object input) {
			var c = _manager.GetReflectionCache (listType);
			IList col = input as IList ?? (IList)c.Instantiate ();
			Type et = c.ArgumentTypes != null ? c.ArgumentTypes[0] : null;
			var r = c.ItemDeserializer;
			if (r != null) {
				foreach (var item in data) {
					if (item == null) {
						continue;
					}
					col.Add (r (this, item, et));
				}
				return col;
			}

			// TODO: candidate of code clean-up.
			// create an array of objects
			foreach (var ob in data) {
				if (ob is IDictionary)
					col.Add (ParseDictionary ((JsonDict)ob, et, null));

				else if (ob is JsonArray) {
					if (et.IsGenericType)
						col.Add (ob);//).ToArray());
					else
						col.Add (((JsonArray)ob).ToArray ());
				}
				else
					col.Add (ChangeType (ob, et));
			}
			return col;
		}

		private object CreateStringKeyDictionary (JsonDict reader, Type pt) {
			var c = _manager.GetReflectionCache (pt);
			var types = c.ArgumentTypes;
			var col = (IDictionary)c.Instantiate ();
			//Type t1 = null; // not used
			Type et = types != null ? types[1] : null;
			var m = GetRevertMethod (et);
			foreach (KeyValuePair<string, object> values in reader) {
				col.Add (values.Key, m (this, values.Value, et));
			}
			return col;
		}

		private object CreateDictionary (JsonArray reader, Type pt) {
			var c = _manager.GetReflectionCache (pt);
			var types = c.ArgumentTypes;
			IDictionary col = (IDictionary)c.Instantiate ();
			Type t1 = null;
			Type t2 = null;
			if (types != null) {
				t1 = types[0];
				t2 = types[1];
			}
			var mk = GetRevertMethod (t1);
			var mv = GetRevertMethod (t2);
			foreach (JsonDict values in reader) {
				col.Add (mk (this, values["k"], t1), mv (this, values["v"], t2));
			}

			return col;
		}

#if !SILVERLIGHT
		private DataSet CreateDataSet (JsonDict reader) {
			DataSet ds = new DataSet ();
			ds.EnforceConstraints = false;
			ds.BeginInit ();

			// read dataset schema here
			var schema = reader.Schema;

			if (schema is string) {
				TextReader tr = new StringReader ((string)schema);
				ds.ReadXmlSchema (tr);
			}
			else {
				DatasetSchema ms = (DatasetSchema)ParseDictionary ((JsonDict)schema, typeof (DatasetSchema), null);
				ds.DataSetName = ms.Name;
				for (int i = 0; i < ms.Info.Count; i += 3) {
					if (ds.Tables.Contains (ms.Info[i]) == false)
						ds.Tables.Add (ms.Info[i]);
					ds.Tables[ms.Info[i]].Columns.Add (ms.Info[i + 1], Type.GetType (ms.Info[i + 2]));
				}
			}

			foreach (KeyValuePair<string, object> pair in reader) {
				//if (pair.Key == "$type" || pair.Key == "$schema") continue;

				JsonArray rows = (JsonArray)pair.Value;
				if (rows == null) continue;

				DataTable dt = ds.Tables[pair.Key];
				ReadDataTable (rows, dt);
			}

			ds.EndInit ();

			return ds;
		}

		private void ReadDataTable (JsonArray rows, DataTable dt) {
			dt.BeginInit ();
			dt.BeginLoadData ();
			List<int> guidcols = new List<int> ();
			List<int> datecol = new List<int> ();

			foreach (DataColumn c in dt.Columns) {
				if (typeof (Guid).Equals (c.DataType) || typeof (Guid?).Equals (c.DataType))
					guidcols.Add (c.Ordinal);
				if (_params.UseUTCDateTime && (typeof (DateTime).Equals (c.DataType) || typeof (DateTime?).Equals (c.DataType)))
					datecol.Add (c.Ordinal);
			}
			var gc = guidcols.Count > 0;
			var dc = datecol.Count > 0;

			foreach (JsonArray row in rows) {
				object[] v = new object[row.Count];
				row.CopyTo (v, 0);
				if (gc) {
					foreach (int i in guidcols) {
						string s = (string)v[i];
						if (s != null && s.Length < 36)
							v[i] = new Guid (Convert.FromBase64String (s));
					}
				}
				if (dc && _params.UseUTCDateTime) {
					foreach (int i in datecol) {
						var s = v[i];
						if (s != null)
							v[i] = CreateDateTime (this, s);
					}
				}
				dt.Rows.Add (v);
			}

			dt.EndLoadData ();
			dt.EndInit ();
		}

		DataTable CreateDataTable (JsonDict reader) {
			var dt = new DataTable ();

			// read dataset schema here
			var schema = reader.Schema;

			if (schema is string) {
				TextReader tr = new StringReader ((string)schema);
				dt.ReadXmlSchema (tr);
			}
			else {
				var ms = (DatasetSchema)ParseDictionary ((JsonDict)schema, typeof (DatasetSchema), null);
				dt.TableName = ms.Info[0];
				for (int i = 0; i < ms.Info.Count; i += 3) {
					dt.Columns.Add (ms.Info[i + 1], Type.GetType (ms.Info[i + 2]));
				}
			}

			foreach (var pair in reader) {
				//if (pair.Key == "$type" || pair.Key == "$schema")
				//	continue;

				var rows = (JsonArray)pair.Value;
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

		internal static RevertJsonValue GetRevertMethod (JsonDataType type) {
			return _revertMethods[(int)type];
		}
		#region RevertJsonValue delegate methods
		internal static object RevertPrimitive (JsonDeserializer deserializer, object value, Type type) {
			return value;
		}
		internal static object RevertInt32 (JsonDeserializer deserializer, object value, Type type) {
			return (int)(long)value;
		}
		internal static object RevertByte (JsonDeserializer deserializer, object value, Type type) {
			return (byte)(long)value;
		}
		internal static object RevertSByte (JsonDeserializer deserializer, object value, Type type) {
			return (sbyte)(long)value;
		}
		internal static object RevertShort (JsonDeserializer deserializer, object value, Type type) {
			return (short)(long)value;
		}
		internal static object RevertUShort (JsonDeserializer deserializer, object value, Type type) {
			return (ushort)(long)value;
		}
		internal static object RevertUInt32 (JsonDeserializer deserializer, object value, Type type) {
			return (uint)(long)value;
		}
		internal static object RevertUInt64 (JsonDeserializer deserializer, object value, Type type) {
			return (ulong)(long)value;
		}
		internal static object RevertSingle (JsonDeserializer deserializer, object value, Type type) {
			return (float)(double)value;
		}
		internal static object RevertDecimal (JsonDeserializer deserializer, object value, Type type) {
			return (decimal)(double)value;
		}
		internal static object RevertChar (JsonDeserializer deserializer, object value, Type type) {
			var s = value as string;
			return s.Length > 0 ? s[0] : '\0';
		}
		internal static object RevertGuid (JsonDeserializer deserializer, object value, Type type) {
			return CreateGuid (value);
		}
		private static object CreateGuid (object value) {
			var s = (string)value;
			return s.Length > 30 ? new Guid (s) : new Guid (Convert.FromBase64String (s));
		}
		internal static object RevertTimeSpan (JsonDeserializer deserializer, object value, Type type) {
			return CreateTimeSpan (value);
		}
		private static object CreateTimeSpan (object value) {
			// TODO: Optimize TimeSpan
			return TimeSpan.Parse ((string)value);
		}
		internal static object RevertByteArray (JsonDeserializer deserializer, object value, Type type) {
			return Convert.FromBase64String ((string)value);
		}
		internal static object RevertDateTime (JsonDeserializer deserializer, object value, Type type) {
			return CreateDateTime (deserializer, value);
		}
		internal static object CreateDateTime (JsonDeserializer deserializer, object value) {
			string t = (string)value;
			//                   0123456789012345678 9012 9/3
			// datetime format = yyyy-MM-ddTHH:mm:ss .nnn  Z
			int year = CreateInteger (t, 0, 4);
			int month = CreateInteger (t, 5, 2);
			int day = CreateInteger (t, 8, 2);
			int hour = CreateInteger (t, 11, 2);
			int min = CreateInteger (t, 14, 2);
			int sec = CreateInteger (t, 17, 2);
			int ms = (t.Length > 21 && t[19] == '.') ? CreateInteger (t, 20, 3) : 0;
			bool utc = (t[t.Length - 1] == 'Z');

			if (deserializer._params.UseUTCDateTime == false || utc == false)
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
					num = (num << 3) + (num << 1); // num *= 10;
					num += (cc - '0');
				}
			}
			if (neg) num = -num;

			return num;
		}
		internal static object RevertUndefined (JsonDeserializer deserializer, object value, Type type) {
			if (value == null) return null;
			var d = value as JsonDict;
			if (d != null) {
				return deserializer.ParseDictionary (d, type, null);
			}
			var a = value as JsonArray;
			if (a != null) {
				return deserializer.CreateList (a, type, null);
			}
			return d;
		}
		internal static object RevertArray (JsonDeserializer deserializer, object value, Type type) {
			if (value == null) return null;
			return deserializer.CreateArray ((JsonArray)value, type);
		}
		internal static object RevertMultiDimensionalArray (JsonDeserializer deserializer, object value, Type type) {
			if (value == null) return null;
			return deserializer.CreateMultiDimensionalArray ((JsonArray)value, type);
		}
		internal static object RevertList (JsonDeserializer deserializer, object value, Type type) {
			if (value == null) return null;
			return deserializer.CreateList ((JsonArray)value, type, null);
		}
		internal static object RevertDataSet (JsonDeserializer deserializer, object value, Type type) {
			return deserializer.CreateDataSet ((JsonDict)value);
		}
		internal static object RevertDataTable (JsonDeserializer deserializer, object value, Type type) {
			return deserializer.CreateDataTable ((JsonDict)value);
		}
		internal static object RevertHashTable (JsonDeserializer deserializer, object value, Type type) {
			return deserializer.RootHashTable ((JsonArray)value);
		}
		internal static object RevertDictionary (JsonDeserializer deserializer, object value, Type type) {
			return deserializer.RootDictionary (value, type);
		}
		internal static object RevertNameValueCollection (JsonDeserializer deserializer, object value, Type type) {
			return CreateNameValueCollection ((JsonDict)value);
		}
		internal static object RevertStringDictionary (JsonDeserializer deserializer, object value, Type type) {
			return CreateStringDictionary ((JsonDict)value);
		}
		internal static object RevertStringKeyDictionary (JsonDeserializer deserializer, object value, Type type) {
			return deserializer.CreateStringKeyDictionary ((JsonDict)value, type);
		}
		internal static object RevertEnum (JsonDeserializer deserializer, object value, Type type) {
			return deserializer.CreateEnum (value, type);
		}
		internal static object RevertCustom (JsonDeserializer deserializer, object value, Type type) {
			return deserializer._manager.CreateCustom ((string)value, type);
		}
		//internal static object ChangeType (JsonDeserializer deserializer, object value, Type type) {
		//	return deserializer.ChangeType (value, type);
		//}
		#endregion
	}
}
