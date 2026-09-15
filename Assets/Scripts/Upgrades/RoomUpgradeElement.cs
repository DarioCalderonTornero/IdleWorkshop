using UnityEngine;

[System.Serializable]
public class RoomUpgradeElement
{
    [Tooltip("Rellena esto SOLO si el elemento empieza bloqueado. El componente " +
             "debe implementar IUnlockable (WorkDeskUnlockable, Unlockable...).")]
    public MonoBehaviour unlockableTarget;

    [Tooltip("Rellena esto SOLO si es un elemento sin bloqueo: worker, carrito, recepción...")]
    public MonoBehaviour upgradeableTarget;

    public Sprite icon;

    /// <summary>El objetivo bloqueable, o null si este elemento no se desbloquea.</summary>
    public IUnlockable Unlockable => unlockableTarget as IUnlockable;
}