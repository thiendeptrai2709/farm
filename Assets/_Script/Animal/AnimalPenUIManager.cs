using UnityEngine;
using System;
using System.Collections.Generic;

public class AnimalPenUIManager : MonoBehaviour
{
    public static AnimalPenUIManager Instance;

    [Header("UI Panels")]
    public GameObject uiPanel;
    public Transform contentContainer; // Cục Grid/Vertical Layout Group chứa danh sách nút bấm
    public GameObject animalSlotPrefab; // Kéo Prefab chứa script AnimalSlotUI vào đây

    [Header("Danh sách con vật bán tại trang trại")]
    public List<AnimalData> availableAnimals; // Kéo file ChickenData, CowData... vào đây

    public event Action<bool> OnAnimalUIToggled;

    private AnimalPen currentOpenPen;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (uiPanel != null) uiPanel.SetActive(false);
    }

    public bool IsOpen()
    {
        return uiPanel != null && uiPanel.activeSelf;
    }

    public void OpenUI(AnimalPen pen)
    {
        currentOpenPen = pen;
        uiPanel.SetActive(true);

        // Phát loa báo khóa Camera, hiện Chuột
        OnAnimalUIToggled?.Invoke(true);

        PopulateAnimalList();
    }

    public void CloseUI()
    {
        uiPanel.SetActive(false);
        currentOpenPen = null;

        // Phát loa báo thả Camera, giấu Chuột
        OnAnimalUIToggled?.Invoke(false);
    }

    private void PopulateAnimalList()
    {
        // 1. Xóa sạch danh sách cũ cho đỡ rác
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Sinh ra danh sách mới từ cục availableAnimals
        foreach (var animal in availableAnimals)
        {
            GameObject newSlotObj = Instantiate(animalSlotPrefab, contentContainer);
            AnimalSlotUI slotUI = newSlotObj.GetComponent<AnimalSlotUI>();
            if (slotUI != null)
            {
                slotUI.Setup(animal);
            }
        }
    }

    // Hàm này được gọi khi ông bấm vào cái nút Gà/Bò trên UI
    public void SelectAnimal(AnimalData animal)
    {
        if (currentOpenPen != null)
        {
            currentOpenPen.StartSpawningAnimal(animal);
            CloseUI(); // Mua xong thì tự tắt bảng UI đi
        }
    }
}