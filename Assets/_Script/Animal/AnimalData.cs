using UnityEngine;

[CreateAssetMenu(fileName = "New Animal", menuName = "Farm/Animal Data")]
public class AnimalData : ScriptableObject
{
    public string animalName;
    public Sprite icon;
    public float spawnTime; // Thời gian chờ để đẻ/giao đến (Tính bằng giây)
    public GameObject animalPrefab; // Mô hình 3D con vật (Gà, Bò...) sẽ chạy loăng quăng

    // Tương lai có thể thêm: Chi phí mua (ItemData), Thức ăn yêu thích...
}