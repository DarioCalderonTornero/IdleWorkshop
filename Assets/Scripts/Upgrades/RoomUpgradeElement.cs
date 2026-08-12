using UnityEngine;

[System.Serializable]
public class RoomUpgradeElement
{
    [Tooltip("Rellena esto SOLO si es una mesa (con estado bloqueado/desbloqueado).")]
    public WorkDeskUnlockable unlockableTarget;

    [Tooltip("Rellena esto SOLO si es un elemento sin bloqueo: worker o decorativo.")]
    public MonoBehaviour upgradeableTarget;

    public Sprite icon;
}