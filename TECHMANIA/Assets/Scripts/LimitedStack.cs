using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimitedStack<T> where T : class
{
    private List<T> data;
    private int capacity;
    private int count;
    // Points to the element that Push will write to.
    private int topIndex;

    public LimitedStack(int capacity)
    {
        data = new List<T>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            data.Add(null);
        }
        this.capacity = capacity;
        count = 0;
        topIndex = 0;
    }

    public void Push(T t)
    {
        data[topIndex] = t;
        topIndex = (topIndex + 1) % capacity;
        count++;
        if (count >= capacity) count = capacity;
    }

    public T Pop()
    {
        if (Empty())
        {
            throw new InvalidOperationException("Cannot pop from empty stack.");
        }
        topIndex--;
        if (topIndex < 0) topIndex += capacity;
        count--;
        return data[topIndex];
    }

    public void Clear()
    {
        count = 0;
    }

    public bool Empty()
    {
        return count == 0;
    }

    public bool Full()
    {
        return count == capacity;
    }
}
