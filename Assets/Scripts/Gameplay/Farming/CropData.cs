using UnityEngine;

[CreateAssetMenu(fileName = "CropData", menuName = "Gameplay/Farming/Crop Data")]
public class CropData : ScriptableObject
{
    public string id;
    public GameObject cropPrefab;
    public GameObject finalItemPrefab;
    public float growTime = 5f;
}
