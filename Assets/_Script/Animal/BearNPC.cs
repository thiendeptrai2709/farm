using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    private BearState currentState = BearState.Sleeping;

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
        if (InventoryManager.Instance == null || InventoryManager.Instance.selectedHotbarIndex == -1)
            return "Gấu đang ngủ, hãy cầm gì đó cho nó ăn...";

        InventorySlot currentSlot = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex];
        ItemData heldItem = currentSlot.item;

        if (heldItem == null) return "Gấu đang ngủ, hãy cầm gì đó cho nó ăn...";

        if (heldItem.itemType == ItemType.Fish || heldItem.itemType == ItemType.Consumable)
        {
            return $"[E] Cho gấu ăn {heldItem.displayName}";
        }

        return "Món này gấu không ăn được...";
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
            if (heldItem is FishItemData fishData) fishTier = fishData.tier;

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
}