using System;
using System.Collections.Generic;
using System.Linq;

namespace ChatBridge.Caching
{
    public class CacheKey
    {
        private object[] _keys;

        public int Count => _keys.Length;

        public IEnumerable<object> Keys => _keys;

        public CacheKey(IEnumerable<object> keys)
        {
            _keys = keys.ToArray();
        }

        public int FindKeyIndex(object key)
        {
            for (int i = 0; i < Count; i++)
            {
                if (_keys[i] == key)
                {
                    return i;
                }
            }
            return -1;
        }

        public object First()
        {
            return _keys[0];
        }

        public object GetKey(int index)
        {
            if(index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return _keys[index];
        }
    }
}
