using UnityEngine;

/// <summary>
/// Datos de una mesa de trabajo dentro del taller.
/// Ponlo en el GameObject de la mesa.
/// </summary>
public class WorkTable : MonoBehaviour
{
    [Tooltip("Punto donde el jugador se para mientras trabaja (delante de la mesa)")]
    [SerializeField] private Transform playerSlot;

    [Tooltip("Punto encima de la mesa donde se deposita el objeto")]
    [SerializeField] private Transform itemSlot;

    public Vector3 PlayerSlotPos => playerSlot.position;
    public Vector3 ItemSlotPos => itemSlot.position;

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