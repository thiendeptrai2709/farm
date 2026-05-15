using UnityEngine;
using UnityEngine.Localization; // Chức năng: Gọi thư viện Đa ngôn ngữ

[CreateAssetMenu(fileName = "New Animal", menuName = "Farm/Animal Data")]
public class AnimalData : ScriptableObject
{
    [Header("Đa Ngôn Ngữ")]
    public LocalizedString localizedAnimalName;

    // Chức năng: Đọc tên con vật từ bảng dịch, nếu rỗng thì lấy tên file ScriptableObject
    public string animalName
    {
        get
        {
            return localizedAnimalName.IsEmpty ? name : localizedAnimalName.GetLocalizedString();
        }
    }

    [Header("Thông tin cơ bản")]
    public Sprite icon;
    public float spawnTime; // Thời gian chờ để đẻ/giao đến (Tính bằng giây)
    public GameObject animalPrefab; // Mô hình 3D con vật (Gà, Bò...) sẽ chạy loăng quăng

    // Tương lai có thể thêm: Chi phí mua (ItemData), Thức ăn yêu thích...
}