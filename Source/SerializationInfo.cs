using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PowerJson
{
	/// <summary>
	/// An overridden copy of <see cref="ReflectionCache"/>.
	/// </summary>
	[DebuggerDisplay ("{Reflection.TypeName}")]
	sealed class SerializationInfo
	{
		internal readonly ReflectionCache Reflection;
		internal readonly ComplexType CommonType;
		internal readonly JsonDataType JsonDataType;

		internal SerializationInfo[] TypeParameters;
		internal CreateObject Constructor;
		internal string Alias;
		internal WriteJsonValue SerializeMethod;
		internal RevertJsonValue DeserializeMethod;
		internal RevertJsonValue ItemDeserializeMethod;
		internal JsonMemberGetter[] Getters;
		// a member could have several setters because of the result of typed serialization
		internal Dictionary<string, JsonMemberSetter> Setters;
		// denotes the collection name for extended IEnumerable types
		internal string CollectionName;
		internal IJsonConverter Converter;
		internal IJsonInterceptor Interceptor;
		internal Dictionary<string, Enum> EnumNames;

		bool _alwaysDeserializable;
		internal bool AlwaysDeserializable
		{
			get { return _alwaysDeserializable; }
			set
			{
				_alwaysDeserializable = value;
				Constructor = Reflection.ConstructorInfo == ConstructorTypes.Default || value
					? Reflection.Constructor ?? ThrowInvisibleConstructorError
					: ThrowInvisibleConstructorError;
			}
		}

		public SerializationInfo (ReflectionCache reflection) {
			Reflection = reflection;
			Constructor = reflection.Constructor;
			CommonType = reflection.CommonType;
			JsonDataType = reflection.JsonDataType;
			SerializeMethod = reflection.SerializeMethod;
			DeserializeMethod = reflection.DeserializeMethod;
			ItemDeserializeMethod = reflection.ItemDeserializer;
			AlwaysDeserializable = false;
		}

		/// <summary>
		/// Creates an instance of the type by calling its parameterless constructor.
		/// </summary>
		/// <returns>
		/// The created instance.
		/// </returns>
		/// <exception cref="JsonSerializationException">
		/// The type has no constructor, or constructor is not publicly visible, or the constructor takes any argument.
		/// </exception>
		public object Instantiate () {
			try {
				return Constructor ();
			}
			catch (JsonSerializationException) { throw; }
			catch (Exception ex) {
				throw new JsonSerializationException (string.Format (@"Failed to instantiate ""{0}"" from assembly ""{1}""", Reflection.TypeName, Reflection.AssemblyName), ex);
			}
		}

		object ThrowInvisibleConstructorError () {
			throw new JsonSerializationException ("The constructor of type \"" + Reflection.TypeName + "\" from assembly \"" + Reflection.AssemblyName + "\" is not publicly visible.");
		}

		internal JsonMemberGetter FindGetters (string memberName) {
			return Array.Find (Getters, (i) => { return i.MemberName == memberName; });
		}

		internal List<JsonMemberSetter> FindProperties (string memberName) {
			var r = new List<JsonMemberSetter> ();
			foreach (var item in Setters) {
				if (item.Value.Member.MemberName == memberName) {
					r.Add (item.Value);
				}
			}
			return r;
		}
	}

	[DebuggerDisplay ("{MemberName} ({SerializedName}) getter")]
	sealed class JsonMemberGetter
	{
		internal readonly string MemberName;
		internal readonly MemberCache Member;
		internal readonly SerializationInfo TypeInfo;
		internal readonly SerializationInfo OwnerTypeInfo;

		internal TriState Serializable;

		internal bool SpecificName;
		internal string SerializedName;
		internal IDictionary<Type, string> TypedNames;

		internal bool HasNonSerializedValue;
		internal object[] NonSerializedValues;

		internal IJsonConverter Converter;
		internal IJsonConverter ItemConverter;
		internal readonly WriteJsonValue SerializeMethod;

		public JsonMemberGetter (SerializationInfo typeInfo, MemberCache member, SerializationManager manager) {
			Member = member;
			MemberName = member.MemberName;
			SerializedName = member.MemberName;
			SerializeMethod = JsonSerializer.GetWriteJsonMethod (member.MemberType);
			OwnerTypeInfo = typeInfo;
			TypeInfo = member.MemberInfo.ReflectedType.Equals (member.MemberType) ? typeInfo : manager.GetSerializationInfo (member.MemberType);
		}
	}

	[DebuggerDisplay ("{MemberName} setter")]
	sealed class JsonMemberSetter // myPropInfo
	{
		internal readonly string MemberName;
		internal readonly MemberCache Member;
		internal readonly SerializationInfo OwnerTypeInfo;
		internal readonly SerializationInfo TypeInfo;

		internal bool CanWrite;
		internal IJsonConverter Converter;
		internal IJsonConverter ItemConverter;

		public JsonMemberSetter (SerializationInfo typeInfo, MemberCache member, SerializationManager manager) {
			MemberName = member.MemberName;
			Member = member;
			CanWrite = member.IsReadOnly == false;
			OwnerTypeInfo = typeInfo;
			TypeInfo = member.MemberInfo.ReflectedType.Equals (member.MemberType) ? typeInfo : manager.GetSerializationInfo (member.MemberType);
		}
	}


}