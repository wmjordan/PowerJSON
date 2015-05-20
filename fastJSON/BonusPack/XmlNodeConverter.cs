using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace fastJSON.BonusPack
{
	/// <summary>
	/// A <see cref="IJsonConverter"/> to convert <see cref="XmlElement"/> to JSON strings. Currently deserialization has not yet been implemented.
	/// </summary>
	/// <remarks>
	/// <para>Elements will be serialized as a dictionary which contains the following three items:
	/// <list type="ordered">
	/// <item>Element name: the element qualified name surrounded with &lt; and &gt;, the value is its namespace URL.</item>
	/// <item>Attributes: the attribute name prefixed with an "@" character, and the value of the attribute.</item>
	/// <item>Nodes: an array contains the child nodes of the element.</item>
	/// </list>
	/// <para>Text, CDATA, Entity reference nodes will be serialized as a text in the nodes array.</para>
	/// <para>Processing Instruction, XML declaration nodes will be serialized as a dictionary containing one name-value pair.</para>
	/// <para>Comment nodes will be serialized </para>
	/// </para>
	/// </remarks>
	public class XmlNodeConverter : IJsonConverter
	{
		void IJsonConverter.DeserializationConvert (JsonItem item) {
			throw new NotImplementedException ();
		}

		Type IJsonConverter.GetReversiveType (JsonItem item) {
			throw new NotImplementedException ();
		}

		void IJsonConverter.SerializationConvert (JsonItem item) {
			var v = item.Value as XmlNode;
			if (v == null)
				return;
			var nt = v.NodeType;
			if (nt == XmlNodeType.Element) {
				item.Value = ConvertElement ((XmlElement)v);
			}
			else if (nt == XmlNodeType.Document || nt == XmlNodeType.DocumentFragment) {
				item.Value = ConvertNode (v);
			}
		}

		Dictionary<string, object> ConvertElement (XmlElement element) {
			Dictionary<string, object> d = new Dictionary<string, object> ();
			d.Add (String.Concat ("<", element.Name, ">"), element.NamespaceURI);
			foreach (XmlAttribute attr in element.Attributes) {
				d.Add ("@" + attr.Name, attr.Value);
			}
			var n = ConvertNode (element);
			if (n != null) {
				d.Add ("nodes", n);
			}
			return d;
		}

		List<object> ConvertNode (XmlNode v) {
			if (v.HasChildNodes == false) {
				return null;
			}
			var cn = v.ChildNodes;
			var l = cn.Count;
			var n = new List<object> (l);
			foreach (XmlNode node in v.ChildNodes) {
				switch (node.NodeType) {
					case XmlNodeType.Element:
						n.Add (ConvertElement ((XmlElement)node));
						break;
					case XmlNodeType.Text:
					case XmlNodeType.CDATA:
					case XmlNodeType.EntityReference:
					case XmlNodeType.SignificantWhitespace:
					case XmlNodeType.Whitespace:
						n.Add (node.Value);
						break;
					case XmlNodeType.XmlDeclaration:
						var xd = (XmlDeclaration)node;
						n.Add (new KeyValuePair<string, string> ("?" + xd.Name, xd.Value));
						break;
					case XmlNodeType.ProcessingInstruction:
						var pi = (XmlProcessingInstruction)node;
						n.Add (new KeyValuePair<string, string> ("?" + pi.Target, pi.Value));
						break;
					case XmlNodeType.Comment:
						n.Add (new KeyValuePair<string, string> ("!", node.Value));
                        break;
				}
			}
			return n;
		}
	}
}
