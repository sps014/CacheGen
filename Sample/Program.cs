global using static LibCache.Gen;
using LibCache;

Console.WriteLine(Fibbo(10));

[LruCache(5000)]
static int Fibbo(int x,int y=0){
    if (x == 1 || x == 0)
        return x;

    return FibboCached(x - 1,y) + FibboCached(x - 2,y);
}


