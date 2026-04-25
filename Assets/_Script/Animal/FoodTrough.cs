using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TroughSlot
{
    public ItemData item;
    public int amount;
}

public class FoodTrough : MonoBehaviour, IInteractable
{
    [Header("Dữ liệu Máng Ăn")]
    public TroughSlot[] slots = new TroughSlot[5]; // Kho chứa thực tế
    public List<ItemData> validFoodItem;                 // Bộ lọc đồ ăn hợp lệ
    public string troughID;
    private void Start()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) slots[i] = new TroughSlot();
        }
       
    }
    public void SaveTroughData(GameData data)
    {
        SavedTroughData tData = data.savedTroughs.Find(t => t.troughID == this.troughID);
        if (tData == null)
        {
            tData = new SavedTroughData { troughID = this.troughID };
            data.savedTroughs.Add(tData);
        }

        tData.slots.Clear();
        foreach (var slot in slots)
        {
            string id = slot.item != null ? slot.item.name : ""; // Lưu bằng tên file
            tData.slots.Add(new SavedSlotData { itemID = id, amount = slot.amount, currentDurability = -1f });
        }
        Debug.Log($"[FoodTrough {troughID}] Đã lưu dữ liệu đồ ăn.");
    }

    public void LoadTroughData(GameData data)
    {
        if (data == null || data.savedTroughs == null) return;

        SavedTroughData tData = data.savedTroughs.Find(t => t.troughID == this.troughID);
        if (tData == null) return;

        if (InventoryManager.Instance == null || InventoryManager.Instance.itemDatabase == null)
        {
            Debug.LogWarning($"[FoodTrough {troughID}] CẢNH BÁO: Balo chưa xuất hiện, không thể load đồ ăn!");
            return;
        }
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < tData.slots.Count)
            {
                SavedSlotData sSlot = tData.slots[i];
                // Chạy qua kho tổng InventoryManager để xin data thật của món đồ
                ItemData loadedItem = string.IsNullOrEmpty(sSlot.itemID) ? null : InventoryManager.Instance.itemDatabase.GetItemByName(sSlot.itemID);

                slots[i].item = loadedItem;
                slots[i].amount = sSlot.amount;
            }
        }
        Debug.Log($"[FoodTrough {troughID}] Đã phục hồi đồ ăn từ File Save.");
    }
    public string GetInteractText()
    {
        return "[E] Mở Máng Ăn";
    }

    public void Interact()
    {
        if (FoodTroughUIManager.Instance != null)
        {
            FoodTroughUIManager.Instance.OpenTroughUI(this);
        }
    }

    // Gà gọi hàm này khi đến ăn
    public bool BeEatenByAnimal()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].item != null && slots[i].amount > 0)
            {
                slots[i].amount--;
                if (slots[i].amount <= 0)
                    slots[i].item = null;

                return true; // Ăn thành công
            }
        }
        return false;
    }

    // Báo cho AI biết máng có đồ không để nó chạy tới
    public bool HasFood()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].item != null && slots[i].amount > 0)
                return true;
        }
        return false;
    }
    [ContextMenu("Tự động tạo ID cho Máng Ăn")]
    private void AutoGenerateID()
    {
        troughID = "Trough_" + System.Guid.NewGuid().ToString().Substring(0, 8);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}