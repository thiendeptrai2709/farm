using UnityEngine;

public class NPCMerchant : MonoBehaviour, IInteractable
{
    [Header("Shop Data")]
    public ShopData myShopData;

    // Show text when hovering/approaching NPC
    public string GetInteractText()
    {
        if (myShopData != null)
        {
            return $"[E] Trade with {myShopData.npcName}";
        }
        return "[E] Talk";
    }

    // When player presses E
    public void Interact()
    {
        if (myShopData != null)
        {
            ShopUIManager.Instance.OpenShop(myShopData, this.transform);
        }
    }
}