using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Localization;

public class CarpenterTable : MonoBehaviour, IInteractable
{
    [Header("Đa Ngôn Ngữ")]
    public LocalizedString interactText;

    [Header("Danh sách nhà có thể xây ở bàn này")]
    public List<BuildingBlueprint> blueprintsAvailable;

    public string GetInteractText()
    {
        // Chức năng: Hiển thị chữ tương tác UI
        return interactText.IsEmpty ? "[E] Mở Bàn Thiết Kế" : interactText.GetLocalizedString();
    }

    public void Interact()
    {
        // Chức năng: Gọi bảng UI lên và truyền danh sách các bản vẽ vào
        if (BuilderUIManager.Instance != null)
        {
            BuilderUIManager.Instance.OpenUI(blueprintsAvailable, this.transform);
        }
    }
}