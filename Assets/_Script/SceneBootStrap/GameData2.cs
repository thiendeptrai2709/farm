using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class ChestData
{
    public string id;
    public Vector3 position;
    public string prefabID;
    public Quaternion rotation; // Rương thì phải lưu góc xoay vì người chơi có thể xoay ngang/dọc
    public bool isBuiltByPlayer; // Bùa chú chống nhân đôi rương
    public List<InventorySlot> slots = new List<InventorySlot>();
}

[System.Serializable]
public class FarmPlotData
{
    public string id;
    public Vector3 position;
    public PlotState state;
    public string seedID; // [ĐÃ SỬA]: Chỉ lưu Tên của hạt giống (Vd: "CarrotSeed")
    public float growTimer;
    public int harvestCount;
    public bool watered;
    public bool fertilized;
}

[System.Serializable]
public class SavedTreePitData
{
    public string id;
    public Vector3 position;
    public TreePit.PitState state;
    public string seedID; // [ĐÃ SỬA]: Chỉ lưu Tên của hạt giống to (Vd: "AppleTreeSeed")
    public float growTimer;
    public int health;
    public bool watered;
    public bool fertilized;
}