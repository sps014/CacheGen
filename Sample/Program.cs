global using static LibCache.Gen;
using LibCache;

Console.WriteLine(Fibbo(10));

string X = "AGGTAB";
string Y = "GXTXAYB";

Console.Write("Length of LCS is "
              + Lcs(X, Y, X.Length, Y.Length));

[LruCache(5000)]
static int Fibbo(int x)
{
    if (x == 1 || x == 0)
        return x;

    return FibboCached(x - 1) + FibboCached(x - 2);
}

[LruCache]
static int Lcs(string X, string Y, int m, int n)
{
    if (m == 0 || n == 0)
        return 0;
    if (X[m - 1] == Y[n - 1])
        return 1 + LcsCached(X, Y, m - 1, n - 1);
    else
        return Math.Max(LcsCached(X, Y, m, n - 1),
                   LcsCached(X, Y, m - 1, n));
}
[LruCache]
static void Dfs(int y)
{
    DfsCached(5);
}