using System;
using UnityEngine;

[Serializable]
public class SowTileSaveData
{
    public Vector3 position;
    public SowState state;
    public float timer;
    public string cropId;
    public bool isBonusApplied;
}
