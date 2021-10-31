using System.Runtime.CompilerServices;

namespace LibCache;

public class LruCache<T, R>
{
    private readonly Dictionary<T, DoublyNode<T>> cache = new();
    private readonly Dictionary<T, R> result = new();

    private DoublyLinkList<T> list = new();
    public LruCache(int capacity = 3)
    {
        Capacity = capacity;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Print() => list.Print();
    public int Capacity { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Refer(T value)
    {
        //if we have page hit
        if (cache.ContainsKey(value))
        {
            //if hit locaton is not in front we do work
            if (cache[value] != list.Front)
            {
                //get previous and next nodes
                var previous = cache[value].Previous;
                var next = cache[value].Next;

                //delete current node and reassign pointers
                if (previous != null)
                    previous.Next = next;
                if (next != null)
                    next.Previous = previous;

                //manually adjust the count
                list.Count--;

                //remove value from cache
                cache.Remove(value);

                //create new node add it in last and push to cache new pointer
                var node = list.Push(value);
                cache.Add(value, node);
            }
            return true;
        }
        //if capacity is full
        else if (list.Count == Capacity)
        {
            //get node in front which will be deleted
            var last = list.Front;

            //remove result of last node 
            if (last != null)
                result.Remove(last.Value);

            //remove the node
            list.Pop();

            //add new node
            var node = list.Push(value);
            cache.Add(value, node);
        }
        //can accomodate more node , add new node
        else
        {
            //add new node
            var node = list.Push(value);
            cache.Add(value, node);
        }

        return false;

    }
    public R Get(T value)
    {
        if (result.ContainsKey(value))
            return result[value];
        return default;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public R AddResult(T input, R output)
    {
        if (result.ContainsKey(input))
            result[input] = output;
        else
            result.Add(input, output);
        return output;
    }
}

internal class DoublyLinkList<T>
{
    public DoublyNode<T> Front { get; private set; }
    public DoublyNode<T> Rear { get; private set; }
    public int Count { get; internal set; } = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DoublyNode<T> Push(T value)
    {
        DoublyNode<T> node = new(value);
        if (Front == null || Rear == null)
            Front = Rear = node;
        else
        {
            Rear.Next = node;
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
    public DoublyNode<T> Previous { get; set; } = null;
    public T Value { get; set; }
    public DoublyNode(T value) => Value = value;

}