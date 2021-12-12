using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBridge.Caching.Memory
{
    public class MessageMemoryCacheConfiguration
    {
        public int Limit { get; set; } = 500;
    }
}
