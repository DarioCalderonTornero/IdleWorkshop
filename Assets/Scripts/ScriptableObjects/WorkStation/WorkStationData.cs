using UnityEngine;

[CreateAssetMenu(fileName = "WorkStationData", menuName = "Idle/WorkStation Data")]
public class WorkStationData : ScriptableObject
{
    public GameObject prefab;
    //public Vector3 position;
    public double cost;
    public Sprite UI;
    public string stationName;
}
