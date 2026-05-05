using UnityEngine;
using TMPro;

public class NoticeBoardShopBlock : MonoBehaviour
{
    public TextMeshProUGUI shopNameText;
    public Transform itemGridTransform;

    public void SetupBlock(string shopName)
    {
        // Chức năng: Gán tên chủ cửa hàng lên UI
        shopNameText.text = shopName;
    }
}