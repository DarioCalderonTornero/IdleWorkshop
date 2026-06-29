using UnityEngine;
using TMPro;
using System.Collections;

public class StarPopupUI : MonoBehaviour
{
    [SerializeField] private TextMeshPro label;   // <-- TextMeshPro, no TextMeshProUGUI
    [SerializeField] private float riseDistance = 1f;
    [SerializeField] private float duration = 1.2f;

    public void Play()
    {
        StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * riseDistance;

        float elapsed = 0f;
        Color startColor = label.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, endPos, t);
            label.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

            yield return null;
        }

        Destroy(gameObject);
    }
}