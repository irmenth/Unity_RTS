using System.Collections.Generic;
using UnityEngine;

public class UnitPool : MonoBehaviour
{
    public static UnitPool instance;

    private GameObject[] pool;
    private readonly Stack<int> freeIndices = new();
    private readonly Dictionary<GameObject, int> goIndices = new();
    private int poolLength = 0;

    public GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        int index;
        if (freeIndices.Count > 0)
        {
            index = freeIndices.Pop();
            pool[index].SetActive(true);
            pool[index].transform.SetPositionAndRotation(position, rotation);
        }
        else
        {
            index = poolLength++;
            if (index >= pool.Length)
            {
                Debug.LogError("[UnitPool] pool is full");
                return null;
            }

            pool[index] = Object.Instantiate(prefab, position, rotation);
            goIndices.Add(pool[index], index);
        }
        return pool[index];
    }

    public void Destroy(GameObject go)
    {
        int index = goIndices[go];
        pool[index].SetActive(false);
        freeIndices.Push(index);
    }

    public void ClearPool()
    {
        freeIndices.Clear();
        goIndices.Clear();
        poolLength = 0;
    }

    private void Awake()
    {
        instance = this;
        pool = new GameObject[(int)1.5e4];
    }

    private void OnDestroy()
    {
        instance = null;
    }
}
