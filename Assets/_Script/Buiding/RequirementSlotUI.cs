using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RequirementSlotUI : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI amountText;

    public void Setup(ItemData item, int currentAmount, int requiredAmount)
    {
        if (item != null) itemIcon.sprite = item.icon;

        // Nếu đủ đồ -> Chữ màu xanh lục. Nếu thiếu đồ -> Chữ màu đỏ.
        if (currentAmount >= requiredAmount)
        {
            amountText.text = $"<color=#55FF55>{currentAmount} / {requiredAmount}</color>";
        }
        else
        {
            amountText.text = $"<color=#FF5555>{currentAmount} / {requiredAmount}</color>";
        }
    }
}