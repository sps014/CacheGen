global using static LibCache.Gen;
using LibCache;

Console.WriteLine(Pg.Fibbo(10));
static class Pg
{
    [LruCache(5000)]
    public static int Fibbo(int x)
    {
        if (x == 1 || x == 0)
            return x;

        return FibboCached(x - 1) + FibboCached(x - 2);
    }

}
