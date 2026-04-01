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

    private void Start()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) slots[i] = new TroughSlot();
        }
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
}