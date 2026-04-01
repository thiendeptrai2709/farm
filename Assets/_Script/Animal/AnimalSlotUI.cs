using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnimalSlotUI : MonoBehaviour
{
    public Image animalIcon;
    public TextMeshProUGUI animalNameText;
    public Button selectButton;

    private AnimalData myAnimalData;

    public void Setup(AnimalData data)
    {
        myAnimalData = data;

        if (animalIcon != null) animalIcon.sprite = data.icon;
        if (animalNameText != null) animalNameText.text = data.animalName;

        // Xóa lệnh cũ và gán lệnh mới cho nút bấm
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnClick_SelectThisAnimal);
    }

    private void OnClick_SelectThisAnimal()
    {
        if (AnimalPenUIManager.Instance != null && myAnimalData != null)
        {
            AnimalPenUIManager.Instance.SelectAnimal(myAnimalData);
        }
    }
}