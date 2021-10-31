using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LibCache
{
    [AttributeUsage(AttributeTargets.Method,AllowMultiple =false)]
    public class LruCacheAttribute:Attribute
    {
        public int MaxSize { get; set; }
        public LruCacheAttribute(int maxsize=10000)
        {
            MaxSize = maxsize;
        }
    }
}
