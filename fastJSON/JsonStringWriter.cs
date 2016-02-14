using System;
using System.IO;
using System.Text;

namespace PowerJson
{
	/// <summary>
	/// This class serves as a lightweight text appender based on an internal <see cref="StringBuilder"/>.
	/// </summary>
	struct JsonStringWriter
	{
		StringBuilder _output;
		public JsonStringWriter (StringBuilder output) {
			_output = output;
		}
		public void WriteStartObject () {
			_output.Append ('{');
		}
		public void WriteEndObject () {
			_output.Append ('}');
		}
		public void WriteStartArray () {
			_output.Append ('[');
		}
		public void WriteEndArray () {
			_output.Append (']');
		}
		public void WriteColon () {
			_output.Append (':');
		}
		public void WriteNull () {
			_output.Append ("null");
		}
		public void WriteEmptyArray () {
			_output.Append ("[]");
		}
		public void WriteQuotation () {
			_output.Append ('"');
		}
		public void Write (char value) {
			_output.Append (value);
		}
		public void Write (string value) {
			_output.Append (value);
		}
		public void Write (char[] buffer, int index, int count) {
			_output.Append (buffer, index, count);
		}
		internal void WriteStringEscapeUnicode (string s) {
			_output.Append ('\"');

			var runIndex = -1;
			var l = s.Length;
			for (var index = 0; index < l; ++index) {
				var c = s[index];
				if (c >= ' ' && c < 128 && c != '\"' && c != '\\') {
					if (runIndex == -1) {
						runIndex = index;
					}
					continue;
				}

				if (runIndex != -1) {
					_output.Append (s, runIndex, index - runIndex);
					runIndex = -1;
				}

				switch (c) {
					case '\t': _output.Append ("\\t"); break;
					case '\r': _output.Append ("\\r"); break;
					case '\n': _output.Append ("\\n"); break;
					case '"':
					case '\\':
						_output.Append ('\\'); _output.Append (c); break;
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
						_output.Append (u, 0, 6);
						continue;
				}
			}

			if (runIndex != -1) {
				_output.Append (s, runIndex, s.Length - runIndex);
			}

			_output.Append ('\"');
		}

		internal void WriteString (string s) {
			_output.Append ('\"');

			var runIndex = -1;
			var l = s.Length;
			for (var index = 0; index < l; ++index) {
				var c = s[index];
				if (c >= ' ' && c < 128 && c != '\"' && c != '\\') {
					if (runIndex == -1) {
						runIndex = index;
					}
					continue;
				}

				if (runIndex != -1) {
					_output.Append (s, runIndex, index - runIndex);
					runIndex = -1;
				}

				switch (c) {
					case '\t': _output.Append ("\\t"); break;
					case '\r': _output.Append ("\\r"); break;
					case '\n': _output.Append ("\\n"); break;
					case '"':
					case '\\':
						_output.Append ('\\'); _output.Append (c); break;
					default:
						_output.Append (c);
						break;
				}
			}

			if (runIndex != -1) {
				_output.Append (s, runIndex, s.Length - runIndex);
			}

			_output.Append ('\"');
		}

		public override string ToString () {
			return _output.ToString ();
		}
	}
}
