using UnityEngine;
using UnityEngine.InputSystem;

public class WorkStation2Tester : MonoBehaviour
{
    [SerializeField] private WorkStation targetStation;
    [SerializeField] private ItemDatabase itemDatabase;

    private void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            ItemDefinition itemDef = itemDatabase.GetRandom();
            if (itemDef == null || itemDef.itemPrefab == null) return;

            GameObject itemGO = Instantiate(itemDef.itemPrefab, targetStation.ReceptionItemPoint.position, Quaternion.identity);
            targetStation.RequestWork(itemGO, itemDef);
        }
    }
}