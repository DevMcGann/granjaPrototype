using System;
using UnityEngine;

[Serializable]
public class CropFieldSaveData
{
    public Vector3 position;
    public FieldState state;
    public float timer;
    public string cropId;
    public bool isBonusApplied;
}
