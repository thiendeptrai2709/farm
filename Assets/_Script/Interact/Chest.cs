using NUnit.Framework.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    [Header("Cài đặt Rương")]
    public int chestSize = 16;
    public List<InventorySlot> chestSlots = new List<InventorySlot>();
    public List<InventorySlot> startingItems = new List<InventorySlot>();
    [Header("Hiệu ứng nắp rương")]
    public Transform lidTransform;         // Kéo cái object NẮP RƯƠNG vào đây
    public float openAngle = -90f;         // Góc xoay mở nắp (Thường là -90 hoặc 90 độ)
    public float animationDuration = 0.4f; // Mở trong 0.4 giây cho chân thực

    private bool isOpen = false;
    public string chestID;
    public bool isBuiltByPlayer = false;
    public string prefabID;

    private Coroutine animationCoroutine;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    private void Start()
    {

        chestSlots.Clear();

        if (GameDataManager.Instance != null && GameDataManager.Instance.chestDataDict.ContainsKey(chestID))
        {
            chestSlots = new List<InventorySlot>(GameDataManager.Instance.chestDataDict[chestID].slots);
        }
        else
        {
            for (int i = 0; i < chestSize; i++)
            {
                if (i < startingItems.Count && startingItems[i].item != null)
                {
                    chestSlots.Add(new InventorySlot(startingItems[i].item, startingItems[i].amount));
                }
                else
                {
                    chestSlots.Add(new InventorySlot(null, 0));
                }
            }
        }

        if (lidTransform != null)
        {
            closedRotation = lidTransform.localRotation;
            openRotation = closedRotation * Quaternion.Euler(openAngle, 0, 0);
        }
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RegisterChest(this);
        }
    }
    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.UnregisterChest(this);
        }
        ForceSaveToManager();
    }
    public string GetInteractText()
    {
        // Đang mở rồi thì giấu chữ [E] đi cho đỡ rối mắt
        return isOpen ? "" : "[E] Open Chest";
    }

    public void Interact()
    {
        if (isOpen) return; // Nếu đang mở rồi thì không làm gì cả

        isOpen = true;
        InventoryManager.Instance.OpenChest(this);

        // Kích hoạt hiệu ứng từ từ mở nắp
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateLid(openRotation));
    }

    // Hàm này để InventoryManager gọi khi đóng rương (Bấm TAB hoặc đi ra xa)
    public void CloseChestVisuals()
    {
        isOpen = false;

        // Kích hoạt hiệu ứng từ từ đóng nắp
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateLid(closedRotation));
    }

    // Logic Toán học giúp nắp xoay mượt mà không bị giật cục
    private IEnumerator AnimateLid(Quaternion targetRotation)
    {
        if (lidTransform == null) yield break;

        Quaternion startRotation = lidTransform.localRotation;
        float timeElapsed = 0f;

        while (timeElapsed < animationDuration)
        {
            // Lerp là hàm làm mượt, xoay từ từ A sang B
            lidTransform.localRotation = Quaternion.Lerp(startRotation, targetRotation, timeElapsed / animationDuration);
            timeElapsed += Time.deltaTime;
            yield return null; // Chờ frame tiếp theo
        }
        lidTransform.localRotation = targetRotation; // Ép vào góc chuẩn cuối cùng
    }
    [ContextMenu("Tự động tạo ID cho Rương")]
    private void AutoGenerateID()
    {
        // Tạo một chuỗi ngẫu nhiên không bao giờ trùng
        chestID = "MapChest_" + System.Guid.NewGuid().ToString().Substring(0, 8);
    }
    public void ForceSaveToManager()
    {
        if (chestSlots.Count == 0)
        {
            for (int i = 0; i < chestSize; i++)
            {
                chestSlots.Add(new InventorySlot(null, 0));
            }
        }

        if (GameDataManager.Instance != null && !string.IsNullOrEmpty(chestID))
        {
            ChestData data = new ChestData();
            data.id = chestID;
            data.prefabID = this.prefabID;
            data.position = transform.position;
            data.rotation = transform.rotation;
            data.isBuiltByPlayer = this.isBuiltByPlayer;
            data.slots = new List<InventorySlot>(chestSlots);

            GameDataManager.Instance.chestDataDict[chestID] = data;
        }
    }
}