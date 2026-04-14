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
    public string id; // Thêm dòng này để ghi nhớ tên luống đất
    public Vector3 position; // LƯU TỌA ĐỘ để mọc lại đúng chỗ
    public PlotState state;
    public SeedItemData seed;
    public float growTimer;
    public int harvestCount;
    public bool watered;
    public bool fertilized;
}