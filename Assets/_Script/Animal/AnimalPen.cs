using UnityEngine;

public class AnimalPen : MonoBehaviour, IInteractable
{
    [Header("Trạng thái Chuồng")]
    public Transform spawnPoint; // Điểm con vật sẽ xuất hiện trong chuồng

    private bool isProcessing = false; // Đang ấp/chờ giao hàng không?
    private float processTimer = 0f;
    private AnimalData animalToSpawn;

    private GameObject currentAnimal; // Con vật đang sống trong chuồng

    private void Update()
    {
        // LOGIC ĐẾM NGƯỢC THỜI GIAN
        if (isProcessing && animalToSpawn != null)
        {
            processTimer -= Time.deltaTime;

            if (processTimer <= 0)
            {
                FinishSpawning();
            }
        }
    }

    public string GetInteractText()
    {
        if (currentAnimal != null) return "Chuồng đã đầy!";
        if (isProcessing) return $"Đang chờ {animalToSpawn.animalName}... ({(int)processTimer}s)";

        return "[E] Mở bảng mua vật nuôi";
    }

    public void Interact()
    {
        // NẾU CHUỒNG TRỐNG VÀ CHƯA CÓ GIAO DỊCH NÀO -> MỞ UI
        if (currentAnimal == null && !isProcessing)
        {
            if (AnimalPenUIManager.Instance != null)
            {
                AnimalPenUIManager.Instance.OpenUI(this); // Gửi chính cái chuồng này sang UI
            }
        }
    }

    // GỌI TỪ BẢNG UI SAU KHI NGƯỜI CHƠI BẤM CHỌN MUA
    public void StartSpawningAnimal(AnimalData selectedAnimal)
    {
        animalToSpawn = selectedAnimal;
        processTimer = selectedAnimal.spawnTime;
        isProcessing = true;

        Debug.Log($"[Chuồng] Đã ghi nhận lệnh ấp {selectedAnimal.animalName}. Chờ {processTimer} giây...");
    }

    private void FinishSpawning()
    {
        isProcessing = false;

        // Đẻ ra con vật 3D tại vị trí spawnPoint
        if (animalToSpawn.animalPrefab != null)
        {
            currentAnimal = Instantiate(animalToSpawn.animalPrefab, spawnPoint.position, spawnPoint.rotation);
        }

        Debug.Log($"[Chuồng] BÙM! {animalToSpawn.animalName} đã ra đời!");
        animalToSpawn = null;
    }
}