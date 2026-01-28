using JKFrame;
using UnityEngine;

public static class ProjectUtility
{
    public static GameObject GetOrInstantiateGameObject(GameObject prefab, Transform parent)
    {
        GameObject go = PoolSystem.GetGameObject(prefab.name, parent);
        if (go == null)
        {
            go = GameObject.Instantiate(prefab, parent);
        }

        return go;
    } 
}