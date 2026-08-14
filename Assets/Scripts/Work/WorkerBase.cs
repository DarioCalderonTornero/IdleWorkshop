using System.Collections;
using UnityEngine;

/// <summary>
/// Lógica compartida entre cualquier trabajador que se mueve por la escena
/// transportando un objeto: moverse a un punto, recoger, dejar, animaciones
/// de recogida/entrega. No procesa objetos ni gestiona colas — eso es
/// responsabilidad de las clases que hereden de esta (Worker, Receptionist).
/// </summary>
public abstract class WorkerBase : MonoBehaviour
{
    [Header("Posición idle")]
    [SerializeField] protected Transform idlePosition;

    [Header("Objeto en cabeza")]
    [SerializeField] protected Transform headAnchor;

    [Header("Movimiento")]
    [SerializeField] protected float baseMoveSpeed = 4f;

    protected float _currentMoveSpeed;

    protected virtual void Awake()
    {
        _currentMoveSpeed = baseMoveSpeed;
    }

    public void ApplyMoveSpeedMultiplier(float multiplier)
    {
        _currentMoveSpeed = baseMoveSpeed * multiplier;
    }

    // ── Movimiento ──────────────────────────────────────────────────
    protected IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target,
                _currentMoveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

    // ── Recoger / dejar objeto ──────────────────────────────────────
    protected void PickUpInstant(GameObject itemGO)
    {
        Transform anchor = headAnchor != null ? headAnchor : transform;
        itemGO.transform.SetParent(anchor);
        itemGO.transform.localPosition = Vector3.zero;
    }

    protected void PutDown(GameObject itemGO, Vector3 worldPos)
    {
        itemGO.transform.SetParent(null);
        itemGO.transform.position = worldPos;
    }

    protected IEnumerator PickUpAnim(GameObject itemGO)
    {
        Vector3 startPos = itemGO.transform.position;
        Transform anchor = headAnchor != null ? headAnchor : transform;
        Vector3 targetPos = anchor.position;

        float elapsed = 0f;
        float animTime = 0.35f;

        while (elapsed < animTime)
        {
            elapsed += Time.deltaTime;
            itemGO.transform.position = Vector3.Lerp(
                startPos, targetPos,
                Mathf.SmoothStep(0f, 1f, elapsed / animTime));
            yield return null;
        }

        itemGO.transform.SetParent(anchor);
        itemGO.transform.localPosition = Vector3.zero;
    }

    // ── Boost de tap ──────────────────────────────────────────────
    private float _tapBoostAccumulated = 0f;
    protected float ConsumeTapBoost()
    {
        float value = _tapBoostAccumulated;
        _tapBoostAccumulated = 0f;
        return value;
    }

    // Llamado desde TapHandler → WorkerRegistry
    public void ApplyTapBoost(float seconds)
    {
        _tapBoostAccumulated += seconds;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (idlePosition != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(idlePosition.position, 0.12f);
        }
    }
#endif
}