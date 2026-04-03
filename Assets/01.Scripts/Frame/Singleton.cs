using UnityEngine;
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField] private bool isDontDestroyOnLoad = true;
    private static T _instance;
    public static T Instance => _instance;

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            if (isDontDestroyOnLoad) DontDestroyOnLoad(gameObject);
            Init();
            return;
        }

        if (_instance != this) Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    protected abstract void Init();
}
