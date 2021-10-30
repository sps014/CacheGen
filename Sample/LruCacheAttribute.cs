using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample
{
    public class LruCacheAttribute:Attribute
    {
        public int MaxSize { get; set; }
        public LruCacheAttribute(int maxsize=10000)
        {
            MaxSize = maxsize;
        }
    }
}
