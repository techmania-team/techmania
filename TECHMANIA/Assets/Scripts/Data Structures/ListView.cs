using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A custom List that allows "removing" items from the front.
// Indexing will work as if the removed items do not exist.
// Enumerators are not implemented, so foreach will not work.
public class ListView<T> : IList<T>
{
    private List<T> list;
    private int firstIndex;

    public ListView()
    {
        list = new List<T>();
        firstIndex = 0;
    }

    public void RemoveFirst()
    {
        if (firstIndex == list.Count)
        {
            throw new System.InvalidOperationException(
                "Cannot remove first element from an empty ListView.");
        }
        firstIndex++;
    }

    public void LogToConsole()
    {
        string log = $"ListView has {Count} elements: ";
        for (int i = 0; i < Count; i++)
        {
            log += this[i] + ", ";
        }
        Debug.Log(log);
    }

    #region IList implementation
    public T this[int index] 
    {
        get => list[firstIndex + index];
        set => list[firstIndex + index] = value;
    }

    public int Count => list.Count - firstIndex;

    public bool IsReadOnly => false;

    public void Add(T item)
    {
        list.Add(item);
    }

    public void Clear()
    {
        list.Clear();
        firstIndex = 0;
    }

    public bool Contains(T item)
    {
        throw new System.NotImplementedException();
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerator<T> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }

    public int IndexOf(T item)
    {
        throw new System.NotImplementedException();
    }

    public void Insert(int index, T item)
    {
        throw new System.NotImplementedException();
    }

    public bool Remove(T item)
    {
        throw new System.NotImplementedException();
    }

    public void RemoveAt(int index)
    {
        throw new System.NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
    #endregion
}
