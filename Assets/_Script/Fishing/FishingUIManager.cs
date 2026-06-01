using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FishingUIManager : MonoBehaviour
{
    public static FishingUIManager Instance;

    [Header("Giao diện UI Audition")]
    public GameObject auditionPanel;
    public Image[] arrowSlots;
    public Sprite upSprite, downSprite, leftSprite, rightSprite;
    public Color normalColor = Color.white;
    public Color successColor = Color.green;

    [Header("Giao diện UI Nấc (Tier Panel)")]
    public GameObject tierPanel;
    public Image[] tierHighlights;
    public Color tierLockedColor = Color.gray;
    public Color tierUnlockedColor = Color.yellow;

    [Header("Slider Thời Gian")]
    public Slider timerSlider;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Script này sẽ được gắn vào gameplayCorePrefab nên tự động được DontDestroyOnLoad bảo tồn qua mọi Scene
        }
        else Destroy(gameObject);
    }

    public void ToggleAuditionPanel(bool show)
    {
        if (auditionPanel != null) auditionPanel.SetActive(show);
    }

    public void ToggleTierPanel(bool show)
    {
        if (tierPanel != null) tierPanel.SetActive(show);
    }

    public void ToggleTimer(bool show, float maxVal = 0, float currentVal = 0)
    {
        if (timerSlider != null)
        {
            timerSlider.gameObject.SetActive(show);
            if (show)
            {
                timerSlider.maxValue = maxVal;
                timerSlider.value = currentVal;
            }
        }
    }

    public void UpdateTimer(float currentVal)
    {
        if (timerSlider != null) timerSlider.value = currentVal;
    }

    public int GetMaxArrowSlots()
    {
        return arrowSlots != null ? arrowSlots.Length : 0;
    }

    public void SetupArrows(int sequenceLength, List<FishingZone.ArrowKey> targetSequence)
    {
        if (arrowSlots == null) return;

        for (int i = 0; i < arrowSlots.Length; i++)
        {
            if (i < sequenceLength && i < targetSequence.Count)
            {
                arrowSlots[i].gameObject.SetActive(true);
                arrowSlots[i].color = normalColor;
                arrowSlots[i].sprite = GetSpriteForArrow(targetSequence[i]);
            }
            else
            {
                arrowSlots[i].gameObject.SetActive(false);
            }
        }
    }

    public void MarkArrowSuccess(int index)
    {
        if (arrowSlots != null && index < arrowSlots.Length && arrowSlots[index] != null)
        {
            arrowSlots[index].color = successColor;
        }
    }

    public void UpdateTierUI(int successCount)
    {
        if (tierHighlights == null || tierHighlights.Length < 4) return;

        for (int i = 0; i < tierHighlights.Length; i++)
        {
            if (i <= successCount)
                tierHighlights[i].color = tierUnlockedColor;
            else
                tierHighlights[i].color = tierLockedColor;
        }
    }

    private Sprite GetSpriteForArrow(FishingZone.ArrowKey key)
    {
        switch (key)
        {
            case FishingZone.ArrowKey.Up: return upSprite;
            case FishingZone.ArrowKey.Down: return downSprite;
            case FishingZone.ArrowKey.Left: return leftSprite;
            case FishingZone.ArrowKey.Right: return rightSprite;
            default: return null;
        }
    }
}