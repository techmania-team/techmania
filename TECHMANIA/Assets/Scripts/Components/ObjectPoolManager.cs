using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    private Dictionary<GameObject, ObjectPool> prefabToPool;
    private Dictionary<GameObject, ObjectPool> lentObjectToPool;

    private void Start()
    {
        prefabToPool = new Dictionary<GameObject, ObjectPool>();
        lentObjectToPool = new Dictionary<GameObject, ObjectPool>();

        foreach (ObjectPool p in GetComponentsInChildren<ObjectPool>())
        {
            prefabToPool.Add(p.prefab, p);
            Debug.Log($"Initializing object pool of prefab {p.prefab.name}, with {p.transform.childCount} objects in it.");
            for (int i = 0; i < p.transform.childCount; i++)
            {
                p.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }

    public GameObject Borrow(GameObject prefab, Transform parent)
    {
        if (!prefabToPool.ContainsKey(prefab))
        {
            throw new System.Exception("No pool available for " +
                prefab.name);
        }

        ObjectPool pool = prefabToPool[prefab];
        Transform poolParent = pool.transform;
        GameObject o;
        if (poolParent.childCount > 0)
        {
            o = poolParent.GetChild(0).gameObject;
            o.transform.SetParent(parent);
            Debug.Log($"Borrowing 1 object of prefab {prefab.name} from object pool, {poolParent.childCount} objects remain.");
        }
        else
        {
            o = Instantiate(prefab, parent);
            Debug.Log($"Instantiating 1 object of prefab {prefab.name} as the object pool is exhausted.");
        }
        o.SetActive(true);
        lentObjectToPool.Add(o, pool);
        return o;
    }

    public void Return(GameObject o)
    {
        if (!lentObjectToPool.ContainsKey(o))
        {
            throw new System.Exception("Cannot determine which object pool the returned object was from.");
        }

        o.SetActive(false);
        ObjectPool pool = lentObjectToPool[o];
        o.transform.SetParent(pool.transform);
        lentObjectToPool.Remove(o);
        Debug.Log($"Returning 1 object of prefab {pool.prefab.name} to object pool, {pool.transform.childCount} objects remain.");
    }
}
