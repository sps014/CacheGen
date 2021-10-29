using System.Runtime.CompilerServices;

LruCache<int> cache = new();
cache.Refer(1);
cache.Print();
cache.Refer(2);
cache.Print();
cache.Refer(3);
cache.Print();
cache.Refer(4);
cache.Print();
cache.Refer(7);
cache.Print();

public class LruCache<T>
{
    private Dictionary<T,DoublyNode<T>> cache = new();

    private DoublyLinkList<T> list=new();
    public LruCache(int capacity=3)
    {
        Capacity = capacity;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Print()=>list.Print();

    public int Capacity { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Refer(T value)
    {
        if(cache.ContainsKey(value))
        {
            if(cache[value]!=list.Front)
            {
                var previous=cache[value].Previous;
                var next=cache[value].Next;

                if(previous!=null)
                    previous.Next=next;
                if(next!=null)
                    next.Previous=previous;

                list.Count--;
                cache.Remove(value);
                var node = list.Push(value);
                cache.Add(value, node);
            }
        }
        else if (list.Count == Capacity)
        {
            list.Pop();
            var node = list.Push(value);
            cache.Add(value, node);
        }
        else
        {
            var node = list.Push(value);
            cache.Add(value, node);
        }

    }
}

internal class DoublyLinkList<T>
{
    public DoublyNode<T> Front { get;private set; }
    public DoublyNode<T> Rear { get; private set; }
    public int Count { get; internal set; } = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DoublyNode<T> Push(T value)
    {
        DoublyNode<T> node = new(value);
        if(Front == null || Rear==null)
            Front=Rear=node;
        else
        {
            Rear.Next=node;
            node.Previous = Rear;
            Rear = node;
        }

        Count++;
        return node;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Pop()
    {
        if (Count == 0)
            return;

        if (Count > 1)
        {
            Front = Front.Next;
            Front.Previous = null;
        }
        else
            Rear = Front = null;

        Count--;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Print()
    {
        var current = Front;
        while (current != null)
        {
            Console.Write($"{current.Value} ");
            current = current.Next;
        }
        Console.WriteLine();
    }
}
internal class DoublyNode<T>
{
    public DoublyNode<T> Next { get; set; } = null;
    public DoublyNode<T> Previous { get; set; }= null;
    public T Value { get; set; }
    public DoublyNode(T value)
    {
        Value = value;
    }

}