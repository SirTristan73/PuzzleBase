using UnityEngine;

public abstract class InstanceManager<T> : MonoBehaviour where T : MonoBehaviour

{
    public static T Instance { get; private set; }
    protected virtual void Awake() => Instance = this as T;

    protected virtual void OnApplicationQuit()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

public abstract class Singleton<T> : InstanceManager<T> where T : MonoBehaviour
{
    protected override void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        
        base.Awake();
    }
}

public abstract class PersistentSingleton<T> : Singleton<T> where T : MonoBehaviour
{
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
}
