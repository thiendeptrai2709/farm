using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class HammerUIManager : MonoBehaviour
{
    public static HammerUIManager Instance;

    [Header("UI Panels")]
    public GameObject hammerPanel;
    public Transform propListContainer;
    public GameObject blueprintSlotPrefab;

    [Header("Details & Requirements")]
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailDescText;
    public Transform requirementContainer;
    public GameObject requirementSlotPrefab;
    public Button buildButton;

    private BuildingBlueprint currentSelectedProp;
    private List<GameObject> spawnedReqSlots = new List<GameObject>();
    private BlueprintSlotUI currentSelectedSlotUI;
    private bool hasEnoughMaterials = false;

    // ĐÂY LÀ CÁI LOA PHÁT TÍN HIỆU
    public event Action<bool> OnHammerUIToggled;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (hammerPanel != null) hammerPanel.SetActive(false);
    }

    public bool IsOpen()
    {
        return hammerPanel != null && hammerPanel.activeSelf;
    }

    public void OpenUI(List<BuildingBlueprint> availableProps)
    {
        hammerPanel.SetActive(true);

        // 1. Phát loa: "Tao đang mở UI đây, nhả chuột ra và khóa chân lại!"
        OnHammerUIToggled?.Invoke(true);

        PopulateList(availableProps);
    }

    public void CloseUI()
    {
        hammerPanel.SetActive(false);

        // 2. Phát loa: "Tao đóng UI rồi, cất chuột đi và thả chân ra!"
        OnHammerUIToggled?.Invoke(false);
    }

    private void PopulateList(List<BuildingBlueprint> props)
    {
        foreach (Transform child in propListContainer) Destroy(child.gameObject);

        foreach (var prop in props)
        {
            if (prop.blueprintType == BlueprintType.SmallProp)
            {
                GameObject newSlot = Instantiate(blueprintSlotPrefab, propListContainer);
                newSlot.GetComponent<BlueprintSlotUI>().Setup(prop);

                Button btn = newSlot.GetComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SelectProp(prop, newSlot.GetComponent<BlueprintSlotUI>()));
            }
        }

        if (propListContainer.childCount > 0)
        {
            BlueprintSlotUI firstSlot = propListContainer.GetChild(0).GetComponent<BlueprintSlotUI>();
            SelectProp(props[0], firstSlot);
        }
    }

    public void SelectProp(BuildingBlueprint prop, BlueprintSlotUI slotUI)
    {
        currentSelectedProp = prop;

        if (currentSelectedSlotUI != null) currentSelectedSlotUI.SetHighlight(false);
        currentSelectedSlotUI = slotUI;
        if (currentSelectedSlotUI != null) currentSelectedSlotUI.SetHighlight(true);

        detailNameText.text = prop.buildingName;
        detailDescText.text = prop.description;

        RefreshRequirements();
    }

    private void RefreshRequirements()
    {
        foreach (var slot in spawnedReqSlots) Destroy(slot);
        spawnedReqSlots.Clear();

        hasEnoughMaterials = true;

        foreach (var req in currentSelectedProp.buildItemCosts)
        {
            GameObject reqObj = Instantiate(requirementSlotPrefab, requirementContainer);
            spawnedReqSlots.Add(reqObj);

            int currentAmountHas = InventoryManager.Instance.GetTotalItemCount(req.item);

            reqObj.GetComponent<RequirementSlotUI>().Setup(req.item, currentAmountHas, req.amount);

            if (currentAmountHas < req.amount)
            {
                hasEnoughMaterials = false;
            }
        }

        buildButton.interactable = hasEnoughMaterials;
    }

    public void OnClick_StartPlacing()
    {
        if (!hasEnoughMaterials || currentSelectedProp == null) return;

        Debug.Log($"[Hammer] Bắt đầu tìm chỗ đặt: {currentSelectedProp.buildingName}");

        CloseUI();

        if (HammerBuildManager.Instance != null)
        {
            HammerBuildManager.Instance.StartPlacing(currentSelectedProp);
        }
    }
}