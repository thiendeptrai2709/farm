using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
public class AnimalPen : MonoBehaviour, IInteractable
{
    [Header("Trạng thái Chuồng")]
    public Transform spawnPoint; // Điểm con vật sẽ xuất hiện trong chuồng

    private bool isProcessing = false; // Đang ấp/chờ giao hàng không?
    private float processTimer = 0f;
    private AnimalData animalToSpawn;

    public BuildingBlueprint capacityUpgradeBlueprint; // Kéo file bản vẽ Nâng Cấp Chuồng vào đây để nó đọc thông số

    public List<AnimalMovement> currentAnimals = new List<AnimalMovement>();

    public string penID;

    [Header("Đa Ngôn Ngữ")]
    public LocalizedString textSpawning;
    public LocalizedString textBarnFull;
    public LocalizedString textOpenBuyPanel;
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
    public int GetCurrentMaxCapacity()
    {
        if (capacityUpgradeBlueprint != null)
        {
            int currentLevel = 0;
            if (SaveManager.Instance != null && SaveManager.Instance.GetCurrentData() != null)
            {
                currentLevel = SaveManager.Instance.GetCurrentData().penCapacityLevel;
            }
            // Tính toán dựa trên hệ số của bản vẽ: Sức chứa gốc + (Level * Số lượng tăng mỗi Level)
            return capacityUpgradeBlueprint.baseCapacity + (currentLevel * capacityUpgradeBlueprint.capacityIncreasePerLevel);
        }
        return 5; // Mặc định trả về 5 nếu quên kéo file bản vẽ vào inspector
    }
    public string GetInteractText()
    {
        // 1. Nếu đang đẻ -> Hiện chữ Đang đẻ (Không có nút [E] để bấm)
        if (isProcessing)
        {
            string spawnStr = textSpawning.IsEmpty ? "Đang đẻ" : textSpawning.GetLocalizedString();
            return $"{spawnStr} {animalToSpawn.animalName}... ({(int)processTimer}s)";
        }

        // 2. Nếu đã full -> Hiện chữ Full (Không có nút [E] để bấm)
        if (GetAliveAnimalCount() >= GetCurrentMaxCapacity())
        {
            return textBarnFull.IsEmpty ? "Chuồng đã full!" : textBarnFull.GetLocalizedString();
        }

        // 3. Nếu rảnh rỗi và còn chỗ -> Hiện nút tương tác bình thường
        return textOpenBuyPanel.IsEmpty ? "[E] Mở bảng mua vật nuôi" : textOpenBuyPanel.GetLocalizedString();
    }
    public int GetAliveAnimalCount()
    {
        // Chức năng: Tự động quét và dọn dẹp những con bị xóa đi (nếu có) để trả lại Slot trống cho chuồng
        currentAnimals.RemoveAll(animal => animal == null);
        return currentAnimals.Count;
    }
    public void Interact()
    {
        // KHÓA TƯƠNG TÁC: Nếu chuồng đã Full hoặc đang đẻ thì lệnh bấm E sẽ bị đá văng, không mở UI
        if (GetAliveAnimalCount() >= GetCurrentMaxCapacity() || isProcessing)
        {
            return;
        }

        if (AnimalPenUIManager.Instance != null)
        {
            AnimalPenUIManager.Instance.OpenUI(this);
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

        if (animalToSpawn.animalPrefab != null)
        {
            GameObject newAnimal = Instantiate(animalToSpawn.animalPrefab, spawnPoint.position, spawnPoint.rotation);
            // Cập nhật dòng này
            currentAnimals.Add(newAnimal.GetComponent<AnimalMovement>());
        }

        Debug.Log($"[Chuồng] BÙM! {animalToSpawn.animalName} đã ra đời!");
        animalToSpawn = null;
    }
    public void SaveAnimalData(GameData data)
    {
        // 1. Tìm xem chuồng này đã có hồ sơ trong File Save chưa
        SavedAnimalPenData myPenData = data.savedAnimalPens.Find(p => p.penID == this.penID);
        if (myPenData == null)
        {
            myPenData = new SavedAnimalPenData { penID = this.penID };
            data.savedAnimalPens.Add(myPenData);
        }

        // 2. Xóa data cũ của CHỈ RIÊNG chuồng này để cập nhật cái mới
        myPenData.savedAnimals.Clear();

        myPenData.isProcessing = this.isProcessing;
        myPenData.processTimer = this.processTimer;
        myPenData.animalToSpawnName = (this.animalToSpawn != null && this.animalToSpawn.animalPrefab != null) ? this.animalToSpawn.animalPrefab.name : "";
        myPenData.lastSavedTimeTicks = System.DateTime.Now.Ticks;

        currentAnimals.RemoveAll(animal => animal == null);
        foreach (var animal in currentAnimals)
        {
            if (animal != null)
            {
                string aName = animal.gameObject.name.Replace("(Clone)", "").Trim();

                SavedAnimalData sData = new SavedAnimalData
                {
                    animalName = aName,
                    position = animal.transform.position,
                    rotation = animal.transform.rotation,
                    currentHunger = animal.currentHunger,
                    currentState = animal.currentState
                };
                myPenData.savedAnimals.Add(sData);
            }
        }
        Debug.Log($"[AnimalPen {penID}] Đã lưu {myPenData.savedAnimals.Count} con vật.");
    }
    private void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.GetCurrentData() != null)
        {
            LoadAnimalData(SaveManager.Instance.GetCurrentData());
        }
    }
    public void LoadAnimalData(GameData data)
    {
        foreach (var animal in currentAnimals)
        {
            if (animal != null) Destroy(animal.gameObject);
        }
        currentAnimals.Clear();

        if (data == null || data.savedAnimalPens == null) return;

        SavedAnimalPenData myPenData = data.savedAnimalPens.Find(p => p.penID == this.penID);
        if (myPenData == null) return;

        // [MỚI]: Tải lại trạng thái đang ấp thú
        this.isProcessing = myPenData.isProcessing;
        this.processTimer = myPenData.processTimer;

        if (this.isProcessing && !string.IsNullOrEmpty(myPenData.animalToSpawnName))
        {
            // Đi tìm cái file AnimalData khớp với tên để nhét lại vào biến animalToSpawn
            if (AnimalPenUIManager.Instance != null && AnimalPenUIManager.Instance.availableAnimals != null)
            {
                this.animalToSpawn = AnimalPenUIManager.Instance.availableAnimals.Find(a => a.animalPrefab != null && a.animalPrefab.name == myPenData.animalToSpawnName);
            }
            if (myPenData.lastSavedTimeTicks > 0)
            {
                // Tính xem người chơi đã đi vắng bao nhiêu Ticks
                long elapsedTicks = System.DateTime.Now.Ticks - myPenData.lastSavedTimeTicks;

                // Quy đổi Ticks ra Giây (10 triệu Ticks = 1 giây)
                float elapsedSeconds = elapsedTicks / 10000000f;

                // Trừ thẳng vào thời gian đếm ngược
                this.processTimer -= elapsedSeconds;

                // Nếu thời gian đếm ngược bị trừ thủng mức 0 -> Tức là đã đẻ xong từ lúc đang đi vắng
                if (this.processTimer <= 0)
                {
                    this.processTimer = 0;
                    // Ở khung hình Update tiếp theo, nó sẽ tự gọi hàm FinishSpawning()
                }
                Debug.Log($"[AnimalPen {penID}] Bạn đã đi vắng {elapsedSeconds} giây. Cập nhật lại thời gian ấp còn: {this.processTimer} giây.");
            }
        }
        else
        {
            this.animalToSpawn = null;
        }

        // Tải lại thú đã đẻ
        if (myPenData.savedAnimals.Count > 0)
        {
            foreach (SavedAnimalData aData in myPenData.savedAnimals)
            {
                GameObject prefabToSpawn = null;
                if (AnimalPenUIManager.Instance != null && AnimalPenUIManager.Instance.availableAnimals != null)
                {
                    AnimalData matchData = AnimalPenUIManager.Instance.availableAnimals.Find(a => a.animalPrefab != null && a.animalPrefab.name == aData.animalName);
                    if (matchData != null) prefabToSpawn = matchData.animalPrefab;
                }

                if (prefabToSpawn != null)
                {
                    GameObject newObj = Instantiate(prefabToSpawn, aData.position, aData.rotation);
                    AnimalMovement aMovement = newObj.GetComponent<AnimalMovement>();

                    if (aMovement != null)
                    {
                        aMovement.currentHunger = aData.currentHunger;
                        aMovement.currentState = aData.currentState;
                        currentAnimals.Add(aMovement);
                    }
                }
                else
                {
                    Debug.LogWarning($"[AnimalPen {penID}] Báo động: Không tìm thấy Prefab thú tên là {aData.animalName}. Hãy kiểm tra xem file AnimalData trong UIManager đã có con này chưa!");
                }
            }
        }

        Debug.Log($"[AnimalPen {penID}] Đã phục hồi {currentAnimals.Count} con vật. Đang ấp: {this.isProcessing}");
    }
}