using System;
using System.Collections.Generic;

namespace fastJSON
{
	[JsonSerializable]
    sealed class DatasetSchema
    {
        public List<string> Info ;//{ get; set; }
        public string Name ;//{ get; set; }
    }
}
