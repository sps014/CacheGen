global using static LibCache.Gen;
using LibCache;
using System.Runtime.CompilerServices;



Console.WriteLine(Fibbo(10));

[LruCache(5000)]

static int Fibbo(int x){
    if (x == 1 || x == 0)
        return x;

    return FibboCached(x - 1) + FibboCached(x - 2);
}


