using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Localization;

public enum BearState { Sleeping, Eating, Sitting }

[System.Serializable]
public class BearReward
{
    public ItemData rewardItem;
    public int minAmount = 1;
    public int maxAmount = 1;
    [Tooltip("Trọng số rơi đồ. Tỷ lệ % = Trọng số / Tổng trọng số")]
    public int dropWeight = 100;
}

public class BearNPC : MonoBehaviour, IInteractable
{
    [Header("Cấu hình Animation")]
    public Animator anim;
    private readonly string ANIM_EAT = "Eat";
    private readonly string ANIM_SIT = "IsSitting";
    private readonly string ANIM_SLEEP = "IsSleeping";

    [Header("Cài đặt thời gian")]
    public float sitDuration = 10f;
    private float sitTimer = 0f;

    [Header("Cài đặt Âm thanh (3D)")]
    public string eatSFX = "Bear_Eat";
    public string sleepSFX = "Bear_Snore";
    public float snoreInterval = 4.5f;
    private float snoreTimer = 0f;

    [Header("Tối ưu hiệu năng (Map rộng)")]
    public float logicCullingDistance = 35f;
    private Transform playerTransform;
    private float performanceCheckTimer = 0f;
    private bool isTooFar = false;

    [Header("Phần thưởng theo Tier Cá")]
    public List<BearReward> commonRewards;
    public List<BearReward> rareRewards;
    public List<BearReward> legendaryRewards;
    [Header("Nâng cấp Thể Lực (Cá Huyền Thoại Rừng)")]
    public float staminaBonusPerFish = 10f; // Mỗi con cá tăng bao nhiêu điểm
    public float maxStaminaLimit = 200f;

    private BearState currentState = BearState.Sleeping;

    [Header("Đa Ngôn Ngữ")]
    public LocalizedString locNeedFood;
    public LocalizedString locFeedBear;
    public LocalizedString locWrongFood;

    private void Start()
    {
        SetState(BearState.Sleeping);
    }

    private void Update()
    {
        performanceCheckTimer += Time.deltaTime;
        if (performanceCheckTimer >= 0.5f)
        {
            performanceCheckTimer = 0f;

            if (playerTransform == null && PlayerMovement.Instance != null)
            {
                playerTransform = PlayerMovement.Instance.transform;
            }

            if (playerTransform != null)
            {
                float sqrDistance = (transform.position - playerTransform.position).sqrMagnitude;
                isTooFar = sqrDistance > (logicCullingDistance * logicCullingDistance);
            }
        }

        if (isTooFar) return;

        if (currentState == BearState.Sitting)
        {
            sitTimer += Time.deltaTime;
            if (sitTimer >= sitDuration)
            {
                SetState(BearState.Sleeping);
            }
        }
        else if (currentState == BearState.Sleeping)
        {
            snoreTimer -= Time.deltaTime;
            if (snoreTimer <= 0f)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFXAtPosition(sleepSFX, transform.position);
                }
                snoreTimer = snoreInterval;
            }
        }
    }

    public string GetInteractText()
    {
        string needFoodTxt = locNeedFood.IsEmpty ? "Gấu đang ngủ, hãy cầm gì đó cho nó ăn..." : locNeedFood.GetLocalizedString();
        string wrongFoodTxt = locWrongFood.IsEmpty ? "Món này gấu không ăn được..." : locWrongFood.GetLocalizedString();
        string feedTxt = locFeedBear.IsEmpty ? "[E] Cho gấu ăn" : locFeedBear.GetLocalizedString();

        if (InventoryManager.Instance == null || InventoryManager.Instance.selectedHotbarIndex == -1)
            return needFoodTxt;

        InventorySlot currentSlot = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex];
        ItemData heldItem = currentSlot.item;

        if (heldItem == null) return needFoodTxt;

        if (heldItem.itemType == ItemType.Fish || heldItem.itemType == ItemType.Consumable)
        {
            return $"{feedTxt} {heldItem.displayName}";
        }

        return wrongFoodTxt;
    }

    public void Interact()
    {
        if (currentState == BearState.Eating) return;

        if (InventoryManager.Instance == null || InventoryManager.Instance.selectedHotbarIndex == -1) return;

        InventorySlot currentSlot = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex];
        ItemData heldItem = currentSlot.item;

        if (heldItem == null) return;

        if (heldItem.itemType == ItemType.Fish || heldItem.itemType == ItemType.Consumable)
        {
            FishTier fishTier = FishTier.Common;
            bool isForestLegendary = false;

            if (heldItem is FishItemData fishData)
            {
                fishTier = fishData.tier;

                // Kiểm tra xem có phải Cá Huyền Thoại và câu ở Rừng không
                if (fishTier == FishTier.Legendary && fishData.allowedLocations.Contains(FishingLocation.Forest))
                {
                    isForestLegendary = true;
                }
            }

            // ĐIỀU KIỆN ĐẶC BIỆT: Nâng cấp thể lực (Không rớt đồ)
            if (isForestLegendary && PlayerStamina.Instance != null && PlayerStamina.Instance.maxStamina < maxStaminaLimit)
            {
                StartCoroutine(ProcessStaminaUpgrade(currentSlot));
                return; // Thoát luôn, không chạy xuống phần rớt đồ (RollReward) ở dưới nữa
            }

            // Nếu không phải cá Huyền thoại rừng, hoặc thể lực đã max khung -> Chạy logic rớt đồ bình thường
            List<BearReward> pool = commonRewards;
            if (heldItem.itemType == ItemType.Fish)
            {
                if (fishTier == FishTier.Legendary || fishTier == FishTier.Epic) pool = legendaryRewards;
                else if (fishTier == FishTier.Rare || fishTier == FishTier.Uncommon) pool = rareRewards;
            }

            BearReward selectedReward = RollReward(pool);

            if (selectedReward != null && selectedReward.rewardItem != null)
            {
                int amountToGive = Random.Range(selectedReward.minAmount, selectedReward.maxAmount + 1);

                if (!InventoryManager.Instance.HasSpaceFor(selectedReward.rewardItem, amountToGive))
                {
                    Debug.LogWarning("[Gấu] Balo của bạn đã đầy! Không thể nhận quà lúc này.");
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UI_Error");
                    return;
                }

                StartCoroutine(ProcessFeeding(currentSlot, selectedReward.rewardItem, amountToGive));
            }
        }
    }

    private BearReward RollReward(List<BearReward> pool)
    {
        if (pool == null || pool.Count == 0) return null;

        int totalWeight = 0;
        foreach (var reward in pool) totalWeight += reward.dropWeight;

        int randomRoll = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var reward in pool)
        {
            currentWeight += reward.dropWeight;
            if (randomRoll < currentWeight)
            {
                return reward;
            }
        }
        return null;
    }

    private IEnumerator ProcessFeeding(InventorySlot slotToDeduct, ItemData rewardItem, int amountToGive)
    {
        slotToDeduct.amount--;
        if (slotToDeduct.amount <= 0)
        {
            slotToDeduct.item = null;
            slotToDeduct.currentDurability = -1f;
        }
        InventoryManager.Instance.RefreshInventoryUI();

        SetState(BearState.Eating);

        yield return new WaitForSeconds(3f);

        InventoryManager.Instance.AddItem(rewardItem, amountToGive);
        Debug.Log($"Gấu tặng bạn: {amountToGive} cái {rewardItem.displayName}");

        SetState(BearState.Sitting);
    }

    private void SetState(BearState newState)
    {
        currentState = newState;
        sitTimer = 0f;

        switch (currentState)
        {
            case BearState.Sleeping:
                anim.SetBool(ANIM_SLEEP, true);
                anim.SetBool(ANIM_SIT, false);
                snoreTimer = 1f;
                break;
            case BearState.Sitting:
                anim.SetBool(ANIM_SLEEP, false);
                anim.SetBool(ANIM_SIT, true);
                break;
            case BearState.Eating:
                anim.SetBool(ANIM_SLEEP, false);
                anim.SetBool(ANIM_SIT, false);
                anim.SetTrigger(ANIM_EAT);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFXAtPosition(eatSFX, transform.position);
                }
                break;
        }
    }
    private IEnumerator ProcessStaminaUpgrade(InventorySlot slotToDeduct)
    {
        // 1. Thu hồi cá
        slotToDeduct.amount--;
        if (slotToDeduct.amount <= 0)
        {
            slotToDeduct.item = null;
            slotToDeduct.currentDurability = -1f;
        }
        InventoryManager.Instance.RefreshInventoryUI();

        // 2. Chạy Anim Gấu Ăn
        SetState(BearState.Eating);
        yield return new WaitForSeconds(3f);

        // 3. Tăng thể lực
        if (PlayerStamina.Instance != null)
        {
            PlayerStamina.Instance.UpgradeMaxStamina(staminaBonusPerFish, maxStaminaLimit);
            Debug.Log($"[Gấu] Dâng cá thành công! Đã tăng giới hạn thể lực lên {PlayerStamina.Instance.maxStamina}");
        }

        SetState(BearState.Sitting);
    }
}