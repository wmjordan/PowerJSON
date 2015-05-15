using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace fastJSON
{
    /// <summary>
    /// This class encodes and decodes JSON strings.
    /// Spec. details, see http://www.json.org/
    /// </summary>
    internal sealed class JsonParser
    {
        enum Token
        {
            None,           // Used to denote no Lookahead available
            Curly_Open,
            Curly_Close,
            Squared_Open,
            Squared_Close,
            Colon,
            Comma,
            String,
            Number,
            True,
            False,
            Null
        }
		readonly static Token[] _CharTokenMap = InitCharTokenMap ();
        readonly string _json;
        readonly StringBuilder _sb = new StringBuilder();
        Token _lookAheadToken = Token.None;
        int _index;

		static Token[] InitCharTokenMap () {
			var t = new Token[0x7F];
			t['{'] = Token.Curly_Open;
			t['}'] = Token.Curly_Close;
			t[':'] = Token.Colon;
			t[','] = Token.Comma;
			t['.'] = Token.Number;
			t['['] = Token.Squared_Open;
			t[']'] = Token.Squared_Close;
			t['\"'] = Token.String;
			for (int i = '0'; i <= '9'; i++) {
				t[(char)i] = Token.Number;
			}
			t['-'] = t['+'] = t['.'] = Token.Number;
			return t;
		}
        internal JsonParser(string json)
        {
           _json = json;
        }

        public object Decode()
        {
            return ParseValue();
        }

        private JsonDict ParseObject()
        {
            var table = new JsonDict ();

            ConsumeToken(); // {

            while (true)
            {
                switch (LookAhead())
                {

                    case Token.Comma:
                        ConsumeToken();
                        break;

                    case Token.Curly_Close:
                        ConsumeToken();
                        return table;

                    default:
                        {
                            // name
                            string name = ParseString();

                            // :
                            if (NextToken() != Token.Colon)
                            {
								throw new JsonParserException ("Expected colon at index ", _index, GetContextText ());
                            }

                            // value
                            object value = ParseValue();

							if (name.Length == 0) {
								// ignores unnamed item
								continue;
							}
							if (name[0] == '$') {
								switch (name) {
									case JsonDict.ExtTypes: table.Types = (JsonDict)value; continue;
									case JsonDict.ExtType: table.Type = (string)value; continue;
									case JsonDict.ExtRefIndex: table.RefIndex = (int)(long)value; continue;
									//TODO: Candidate to removal of unknown use of map
									//case JsonDict.ExtMap: table.Map = (JsonDict)value; continue;
									case JsonDict.ExtSchema: table.Schema = value; continue;
									default:
										break;
								}
							}
                            table[name] = value;
                        }
                        break;
                }
            }
        }

		private string GetContextText () {
			const int ContextLength = 20;
			var s = _index < ContextLength ? _index : ContextLength;
			var e = _index + ContextLength > _json.Length ? _json.Length - _index : ContextLength;
			return string.Concat (_json.Substring (_index - s, s), "^ERROR^", _json.Substring (_index, e));
		}

		private JsonArray ParseArray()
        {
            var array = new JsonArray();
            ConsumeToken(); // [

            while (true)
            {
                switch (LookAhead())
                {
                    case Token.Comma:
                        ConsumeToken();
                        break;

                    case Token.Squared_Close:
                        ConsumeToken();
                        return array;

                    default:
                        array.Add(ParseValue());
                        break;
                }
            }
        }

        private object ParseValue()
        {
            switch (LookAhead())
            {
                case Token.Number:
                    return ParseNumber();

                case Token.String:
                    return ParseString();

                case Token.Curly_Open:
                    return ParseObject();

                case Token.Squared_Open:
                    return ParseArray();

                case Token.True:
                    ConsumeToken();
                    return true;

                case Token.False:
                    ConsumeToken();
                    return false;

                case Token.Null:
                    ConsumeToken();
                    return null;
            }

			throw new JsonParserException ("Unrecognized token at index ", _index, GetContextText ());
        }

        private string ParseString()
        {
            ConsumeToken(); // "

            _sb.Length = 0;

            int runIndex = -1;

            while (_index < _json.Length)
            {
                var c = _json[_index++];

                if (c == '"')
                {
                    if (runIndex != -1)
                    {
                        if (_sb.Length == 0)
                            return _json.Substring(runIndex, _index - runIndex - 1);

                        _sb.Append(_json, runIndex, _index - runIndex - 1);
                    }
                    return _sb.ToString();
                }

                if (c != '\\')
                {
                    if (runIndex == -1)
                        runIndex = _index - 1;

                    continue;
                }

                if (_index == _json.Length) break;

                if (runIndex != -1)
                {
                    _sb.Append(_json, runIndex, _index - runIndex - 1);
                    runIndex = -1;
                }

                switch (_json[_index++])
                {
                    case '"':
                        _sb.Append('"');
                        break;

                    case '\\':
                        _sb.Append('\\');
                        break;

                    case '/':
                        _sb.Append('/');
                        break;

                    case 'b':
                        _sb.Append('\b');
                        break;

                    case 'f':
                        _sb.Append('\f');
                        break;

                    case 'n':
                        _sb.Append('\n');
                        break;

                    case 'r':
                        _sb.Append('\r');
                        break;

                    case 't':
                        _sb.Append('\t');
                        break;

                    case 'u':
                        {
                            int remainingLength = _json.Length - _index;
                            if (remainingLength < 4) break;

                            // parse the 32 bit hex into an integer codepoint
                            // skip 4 chars
                            _sb.Append(ParseUnicode (_json[_index], _json[++_index], _json[++_index], _json[++_index]));
                            ++_index;
                        }
                        break;
                }
            }

			throw new JsonParserException ("Unexpectedly reached end of string", _json.Length, GetContextText ());
        }

		private static int ParseSingleChar (char c1)
        {
            if (c1 >= '0' && c1 <= '9')
                return (c1 - '0');
            else if (c1 >= 'A' && c1 <= 'F')
                return ((c1 - 'A') + 10);
            else if (c1 >= 'a' && c1 <= 'f')
                return  ((c1 - 'a') + 10);
            return 0;
        }

		private static char ParseUnicode (char c1, char c2, char c3, char c4)
        {
            return (char)((ParseSingleChar(c1) << 12)
				+ (ParseSingleChar(c2) << 8)
				+ (ParseSingleChar(c3) << 4)
				+ ParseSingleChar(c4));
        }

		private static long CreateLong (string s, int index, int count) {
			long num = 0;
			bool neg = false;
			for (int x = 0; x < count; x++, index++) {
				char cc = s[index];

				if (cc == '-')
					neg = true;
				else if (cc == '+')
					neg = false;
				else {
					num = (num << 3) + (num << 1); // num *= 10
					num += (cc - '0');
				}
			}
			if (neg) num = -num;

			return num;
		}

        private object ParseNumber()
        {
            ConsumeToken();

            // Need to start back one place because the first digit is also a token and would have been consumed
            var startIndex = _index - 1;
            bool dec = false;
            do
            {
                if (_index == _json.Length)
                    break;
                var c = _json[_index];

                if ((c >= '0' && c <= '9') || c == '.' || c == '-' || c == '+' || c == 'e' || c == 'E')
                {
                    if (c == '.' || c == 'e' || c == 'E')
                        dec = true;
                    if (++_index == _json.Length)
                        break;//throw new Exception("Unexpected end of string whilst parsing number");
                    continue;
                }
                break;
            } while (true);

			if (dec)
			{
				string s = _json.Substring(startIndex, _index - startIndex);
				return double.Parse(s, NumberFormatInfo.InvariantInfo);
			}
			return CreateLong(_json, startIndex, _index - startIndex);
        }

        private Token LookAhead()
        {
            if (_lookAheadToken != Token.None) return _lookAheadToken;

            return _lookAheadToken = NextTokenCore();
        }

        private void ConsumeToken()
        {
            _lookAheadToken = Token.None;
        }

        private Token NextToken()
        {
            var result = _lookAheadToken != Token.None ? _lookAheadToken : NextTokenCore();

            _lookAheadToken = Token.None;

            return result;
        }

        private Token NextTokenCore()
        {
            char c;

            // Skip past whitespace
            while (_index < _json.Length)
            {
                c = _json[_index];

                if (c > ' ') break;
                if (c != ' ' && c != '\t' && c != '\n' && c != '\r') break;

				++_index;
            }

            if (_index == _json.Length)
            {
				throw new JsonParserException ("Reached end of string unexpectedly", _json.Length, GetContextText ());
            }

            c = _json[_index];

            _index++;

			var t = _CharTokenMap[c];
			if (t != Token.None) {
				return t;
			}

			switch (c) {
				case 'f':
					if (_json.Length - _index >= 4 &&
						_json[_index + 0] == 'a' &&
						_json[_index + 1] == 'l' &&
						_json[_index + 2] == 's' &&
						_json[_index + 3] == 'e') {
						_index += 4;
						return Token.False;
					}
					break;
				case 't':
					if (_json.Length - _index >= 3 &&
						_json[_index + 0] == 'r' &&
						_json[_index + 1] == 'u' &&
						_json[_index + 2] == 'e') {
						_index += 3;
						return Token.True;
					}
					break;
				case 'n':
					if (_json.Length - _index >= 3 &&
						_json[_index + 0] == 'u' &&
						_json[_index + 1] == 'l' &&
						_json[_index + 2] == 'l') {
						_index += 3;
						return Token.Null;
					}
					break;
			}
			throw new JsonParserException ("Could not find token at index ", --_index, GetContextText ());
		}
    }

	class JsonArray : List<object> { }
	class JsonDict : Dictionary<string, object> {
		internal const string ExtRefIndex = "$i";
		internal const string ExtTypes = "$types";
		internal const string ExtType = "$type";
		// TODO: Candidate to removal of unknown use of map
		//internal const string ExtMap = "$map";
		internal const string ExtSchema = "$schema";

		internal int RefIndex;
		internal JsonDict Types;
		internal string Type;
		// TODO: Candidate to removal of unknown use of map
		//internal JsonDict Map;
		internal object Schema;
	}
}
