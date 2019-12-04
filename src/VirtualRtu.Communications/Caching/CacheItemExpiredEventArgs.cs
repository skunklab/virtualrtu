using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualRtu.Communications.Caching
{
    public class CacheItemExpiredEventArgs : EventArgs
    {
        public CacheItemExpiredEventArgs(string name, string key, object value)
        {
            Name = name;
            Key = key;
            Value = value;
        }


        public string Name { get; internal set; }

        public string Key { get; internal set; }

        public object Value { get; internal set; }
    }
}
