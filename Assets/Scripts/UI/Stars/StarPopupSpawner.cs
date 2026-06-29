using UnityEngine;

public class StarPopupSpawner : MonoBehaviour
{
    public static StarPopupSpawner Instance { get; private set; }

    [SerializeField] private GameObject starPopupPrefab;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Spawn(Vector3 worldPos)
    {
        if (starPopupPrefab == null) return;
        GameObject go = Instantiate(starPopupPrefab, worldPos, Quaternion.identity);
        go.GetComponent<StarPopupUI>()?.Play();
    }
}