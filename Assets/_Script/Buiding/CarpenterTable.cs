using UnityEngine;
using System.Collections.Generic;

public class CarpenterTable : MonoBehaviour, IInteractable
{
    [Header("Danh sách nhà có thể xây ở bàn này")]
    public List<BuildingBlueprint> blueprintsAvailable;

    public string GetInteractText()
    {
        return "[E] Mở Bàn Thiết Kế";
    }

    public void Interact()
    {
        // Gọi bảng UI lên và truyền danh sách các bản vẽ vào
        if (BuilderUIManager.Instance != null)
        {
            BuilderUIManager.Instance.OpenUI(blueprintsAvailable, this.transform);
        }
    }
}