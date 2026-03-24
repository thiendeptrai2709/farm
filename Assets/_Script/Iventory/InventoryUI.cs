using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    [Header("Bảng Balo chính (Bật/Tắt)")]
    public GameObject inventoryPanel;
    public Transform inventoryContainer;

    [Header("Thanh Hotbar (Luôn bật dưới đáy)")]
    public Transform hotbarContainer;

    [Header("Khuôn mẫu")]
    public GameObject slotPrefab;

    private List<InventorySlotUI> uiHotbarSlots = new List<InventorySlotUI>();
    private List<InventorySlotUI> uiInventorySlots = new List<InventorySlotUI>();

    public event Action<bool> OnInventoryUIToggled;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializeUI();
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateUI;
            InventoryManager.Instance.OnChestToggled += TogglePanel;
        }

       
        inventoryPanel.SetActive(false);
    }

    public void TogglePanel(bool isOpen)
    {
        if (inventoryPanel.activeSelf == isOpen) return;

        inventoryPanel.SetActive(isOpen);
        if (isOpen) UpdateUI();

        // 2. PHÁT LOA: Thông báo cho toàn Server biết Balo vừa Đóng hay Mở
        OnInventoryUIToggled?.Invoke(isOpen);
    }

    private void InitializeUI()
    {
        // 1. Đẻ 8 ô Hotbar dưới đáy màn hình
        for (int i = 0; i < InventoryManager.Instance.maxHotbarSlots; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, hotbarContainer);
            InventorySlotUI slotUI = newSlot.GetComponent<InventorySlotUI>();

            slotUI.storageType = StorageType.Hotbar; // Cắm cờ Hotbar
            slotUI.SetSlotIndex(i);
            uiHotbarSlots.Add(slotUI);
        }

        // 2. Đẻ 16 ô Balo chính giữa màn hình
        for (int i = 0; i < InventoryManager.Instance.maxInventorySlots; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, inventoryContainer);
            InventorySlotUI slotUI = newSlot.GetComponent<InventorySlotUI>();

            slotUI.storageType = StorageType.Inventory; // Cắm cờ Balo
            slotUI.SetSlotIndex(i);
            uiInventorySlots.Add(slotUI);
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        // 1. Vẽ lại Hotbar
        List<InventorySlot> hotbarData = InventoryManager.Instance.hotbarSlots;
        for (int i = 0; i < uiHotbarSlots.Count; i++)
        {
            if (i < hotbarData.Count) uiHotbarSlots[i].UpdateSlot(hotbarData[i]);
            else uiHotbarSlots[i].ClearSlot();
        }

        // 2. Vẽ lại Balo (Chỉ vẽ khi người chơi đang mở Balo)
        if (inventoryPanel.activeSelf)
        {
            List<InventorySlot> inventoryData = InventoryManager.Instance.inventorySlots;
            for (int i = 0; i < uiInventorySlots.Count; i++)
            {
                if (i < inventoryData.Count) uiInventorySlots[i].UpdateSlot(inventoryData[i]);
                else uiInventorySlots[i].ClearSlot();
            }
        }
    }
    public void ForceOpen()
    {
        inventoryPanel.SetActive(true);
        UpdateUI();
        // KHÔNG fire OnInventoryUIToggled ở đây
    }

    public void ForceClose()
    {
        inventoryPanel.SetActive(false);
        // KHÔNG fire OnInventoryUIToggled ở đây
    }
}