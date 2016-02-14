#if NET_40_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;

namespace PowerJson
{
	internal class DynamicJson : DynamicObject
	{
		private IDictionary<string, object> _dictionary { get; set; }
		private List<object> _list { get; set; }

		public DynamicJson (string json) {
			var parse = PowerJson.Json.Parse (json);

			var dd = parse as JsonDict;
			if (dd != null) {
				_dictionary = dd;
			}
			else {
				var l = parse as JsonArray;
				if (l != null) {
					_list = l;
				}
			}
		}

		private DynamicJson (object dictionary) {
			var dd = dictionary as IDictionary<string, object>;
			if (dd != null) {
				_dictionary = dd;
			}
		}

		public override IEnumerable<string> GetDynamicMemberNames () {
			return _dictionary.Keys.ToList ();
		}

		public override bool TryGetIndex (GetIndexBinder binder, object[] indexes, out object result) {
			if (indexes == null || indexes.Length == 0) {
				result = null;
				return false;
			}
			var index = indexes[0];
			if (index is int) {
				result = _list[(int)index];
			}
			else {
				result = _dictionary[(string)index];
			}
			if (result is IDictionary<string, object>) {
				result = new DynamicJson (result as IDictionary<string, object>);
			}
			return true;
		}

		public override bool TryGetMember (GetMemberBinder binder, out object result) {
			if (_dictionary.TryGetValue (binder.Name, out result) == false)
				if (_dictionary.TryGetValue (binder.Name.ToLower (), out result) == false)
					return false;// throw new Exception("property not found " + binder.Name);

			if (result is IDictionary<string, object>) {
				result = new DynamicJson (result as IDictionary<string, object>);
			}
			else if (result is List<object>) {
				List<object> list = new List<object> ();
				foreach (object item in (List<object>)result) {
					if (item is IDictionary<string, object>)
						list.Add (new DynamicJson (item as IDictionary<string, object>));
					else
						list.Add (item);
				}
				result = list;
			}

			return _dictionary.ContainsKey (binder.Name);
		}
	}
}
#endif