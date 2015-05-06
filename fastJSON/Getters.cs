using System;
using System.Collections.Generic;

namespace fastJSON
{
	// HACK: This class is hard-coded in ReflectionCache to be deserializable.
    sealed class DatasetSchema
    {
        public List<string> Info ;//{ get; set; }
        public string Name ;//{ get; set; }
    }
}
