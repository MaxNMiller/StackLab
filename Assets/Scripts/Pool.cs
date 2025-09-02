using UnityEngine;
using System.Collections.Generic;

public class Pool<T> where T : Component
{
    private Queue<T> objects = new Queue<T>();
    private T prefab;
    private Transform parent;

    public Pool(T prefab, int initialSize, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;
        for (int i = 0; i < initialSize; i++)
        {
            T newObj = GameObject.Instantiate(prefab, parent);
            newObj.gameObject.SetActive(false);
            objects.Enqueue(newObj);
        }
    }

    public T Get()
    {
        if (objects.Count == 0)
        {
            T newObj = GameObject.Instantiate(prefab, parent);
            newObj.gameObject.SetActive(false);
            objects.Enqueue(newObj);
        }

        T obj = objects.Dequeue();
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void Release(T obj)
    {
        obj.gameObject.SetActive(false);
        objects.Enqueue(obj);
    }
}

