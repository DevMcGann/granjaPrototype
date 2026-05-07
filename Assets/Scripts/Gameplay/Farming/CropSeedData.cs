using UnityEngine;

[CreateAssetMenu(fileName = "CropSeedData", menuName = "Gameplay/Farming/Crop Seed Data")]
public class CropSeedData : ScriptableObject
{
    public string id;
    public float sowTime = 5f;
    public float growTime = 5f;
    public float harvestTime = 5f;
}
