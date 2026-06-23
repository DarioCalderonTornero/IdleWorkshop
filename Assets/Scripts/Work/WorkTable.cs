using UnityEngine;

public class WorkTable : MonoBehaviour
{
    [Tooltip("Punto donde el jugador se para mientras trabaja (delante de la mesa)")]
    [SerializeField] private Transform playerSlot;

    [Tooltip("Punto encima de la mesa donde se deposita el objeto")]
    [SerializeField] private Transform itemSlot;

    [Header("Procesado")]
    [Tooltip("Tiempo base en segundos para procesar un objeto")]
    [SerializeField] private float baseProcessTime = 4f;

    private float _currentProcessTime;

    void Awake()
    {
        _currentProcessTime = baseProcessTime;
    }

    public Vector3 PlayerSlotPos => playerSlot.position;
    public Vector3 ItemSlotPos => itemSlot.position;
    public float CurrentProcessTime => _currentProcessTime;

    public void ApplyProcessTimeMultiplier(float multiplier)
    {
        _currentProcessTime = Mathf.Max(0.1f, baseProcessTime * multiplier);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (playerSlot != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(playerSlot.position, 0.12f);
        }
        if (itemSlot != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(itemSlot.position, 0.12f);
        }
    }
#endif
}