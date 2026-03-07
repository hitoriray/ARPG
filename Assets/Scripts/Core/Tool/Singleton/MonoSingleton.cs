using UnityEngine;

// Add null checks when accessing singleton during teardown to avoid invalid references.
public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    protected static T instance;
    protected static bool isQuitting;
    private static bool isAutoCreatedInstance;

    public static T Instance
    {
        get
        {
            if (isQuitting) return null;
            if (instance == null)
            {
                instance = GameObject.FindAnyObjectByType<T>();
                isAutoCreatedInstance = false;
                if (instance == null)
                {
                    var go = new GameObject(typeof(T).Name);
                    instance = go.AddComponent<T>();
                    isAutoCreatedInstance = true;
                }
            }

            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = (T)this;
            isAutoCreatedInstance = false;
            DontDestroyOnLoad(gameObject);
            return;
        }

        if (instance == this)
        {
            isAutoCreatedInstance = false;
            DontDestroyOnLoad(gameObject);
            return;
        }

        // Replace auto-created placeholder instance with scene instance (keeps serialized refs).
        if (isAutoCreatedInstance && instance != null)
        {
            Destroy(instance.gameObject);
            instance = (T)this;
            isAutoCreatedInstance = false;
            DontDestroyOnLoad(gameObject);
            return;
        }

        // Destroy only this component to avoid deleting other managers on the same GameObject.
        Destroy(this);
    }

    protected virtual void OnApplicationQuit()
    {
        isQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            isAutoCreatedInstance = false;
        }
    }
}
