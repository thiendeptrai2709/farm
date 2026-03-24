using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlueprintSlotUI : MonoBehaviour
{
    public Image buildingIcon;
    public TextMeshProUGUI buildingNameText;

    private BuildingBlueprint myBlueprint;
    public GameObject highlightObj; // Đảm bảo đã kéo object viền vào đây trong Inspector

    public void Setup(BuildingBlueprint blueprint)
    {
        myBlueprint = blueprint;
        buildingIcon.sprite = blueprint.icon;
        buildingNameText.text = blueprint.buildingName;
    }

    // Gắn hàm này vào sự kiện OnClick của Button trong Unity
    public void OnClick_SelectBlueprint()
    {
        if (BuilderUIManager.Instance != null)
        {
            // [SỬA Ở ĐÂY]: Truyền thêm chữ 'this' để Manager biết đang bấm vào Slot nào
            BuilderUIManager.Instance.SelectBlueprint(myBlueprint, this);
        }
    }

    public void SetHighlight(bool isOn)
    {
        if (highlightObj != null) highlightObj.SetActive(isOn);
    }
}