using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class Extensions
{
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> collection)
    {
        return collection.OrderBy(x => Random.value);
    }

    public static Stack<T> ToStack<T>(this IEnumerable<T> collection)
    {
        Stack<T> stack = new Stack<T>();
        foreach (T t in collection)
            stack.Push(t);

        return stack;
    }
}
