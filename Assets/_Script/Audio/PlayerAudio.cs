using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Cài đặt Nhịp bước chân (Giây)")]
    public float walkStepInterval = 0.5f;   // Tốc độ nhịp đi bộ
    public float runStepInterval = 0.3f;    // Tốc độ nhịp chạy
    public float crouchStepInterval = 0.65f; // Tốc độ nhịp ngồi (chậm nhất)

    private float stepTimer;

    private PlayerInputHandler inputHandler;
    private PlayerMovement playerMovement;
    private CharacterController controller;
    private PlayerGravityAndJump gravityScript;

    private void Start()
    {
        // Lấy dữ liệu từ các script bạn vừa cung cấp
        inputHandler = GetComponentInParent<PlayerInputHandler>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        controller = GetComponentInParent<CharacterController>();
        gravityScript = GetComponentInParent<PlayerGravityAndJump>();
        // Ép timer về 0 để ngay khi vừa bấm phím di chuyển là kêu tiếng đầu tiên luôn
        stepTimer = 0f;
    }

    private void Update()
    {
        
        if (playerMovement.isActionLocked || inputHandler.MoveInput.magnitude < 0.1f)
        {
            stepTimer = 0.15f;
            return;
        }

        bool isStableGrounded = gravityScript != null ? gravityScript.IsStableGrounded : (controller != null && controller.isGrounded);
        if (!isStableGrounded)
        {
            return;
        }

        // 3. ĐẾM NGƯỢC
        stepTimer -= Time.deltaTime;

        // 4. HẾT GIỜ -> PHÁT ÂM THANH
        if (stepTimer <= 0f)
        {
            PlayFootstepSFX();

            // 5. CÀI ĐẶT LẠI ĐỒNG HỒ DỰA TRÊN TRẠNG THÁI HIỆN TẠI
            if (inputHandler.IsCrouching)
            {
                stepTimer = crouchStepInterval; // Đang ngồi
            }
            // Trong file Movement của bạn, đi lùi (y < -0.1f) bị ép về slowSpeed, nên không được tính là chạy
            else if (inputHandler.IsRunning && inputHandler.MoveInput.y >= -0.1f)
            {
                stepTimer = runStepInterval; // Đang chạy tới/chéo
            }
            else
            {
                stepTimer = walkStepInterval; // Đi bộ bình thường hoặc đi lùi
            }
        }
    }

    private void PlayFootstepSFX()
    {
        if (AudioManager.Instance != null)
        {
            string sfxName = GetSurfaceType();
            AudioManager.Instance.PlaySFX(sfxName);
        }
    }

    // Hàm này giữ nguyên để file PlayerGravityAndJump vẫn gọi được khi nhảy
    public void PlayJump()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Jump");
        }
    }
    private string GetSurfaceType()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 1f))
        {
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain != null)
            {
                int texIndex = GetDominantTerrainTexture(terrain, hit.point);

                // Khớp đúng với Layer Palette trong hình của m:
                if (texIndex == 0) return "Footstep_Grass"; // Index 0: Lớp màu xanh lá (Mặt đất)
                if (texIndex == 1) return "Footstep_Sand";  // Index 1: Lớp màu vàng (Cát)

                return "Footstep_Grass"; // Mặc định trả về tiếng mặt đất nếu có lỗi
            }
            else
            {
                if (hit.collider.CompareTag("Wood")) return "Footstep_Wood";
                if (hit.collider.CompareTag("Stone")) return "Footstep_Stone";
            }
        }

        return "Footstep_Grass";
    }
    private int GetDominantTerrainTexture(Terrain terrain, Vector3 worldPos)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        int mapX = Mathf.RoundToInt(((worldPos.x - terrainPos.x) / terrainData.size.x) * terrainData.alphamapWidth);
        int mapZ = Mathf.RoundToInt(((worldPos.z - terrainPos.z) / terrainData.size.z) * terrainData.alphamapHeight);

        if (mapX < 0 || mapZ < 0 || mapX >= terrainData.alphamapWidth || mapZ >= terrainData.alphamapHeight)
            return 0;

        float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

        float maxMix = 0;
        int maxIndex = 0;

        for (int i = 0; i < splatmapData.GetUpperBound(2) + 1; i++)
        {
            if (splatmapData[0, 0, i] > maxMix)
            {
                maxIndex = i;
                maxMix = splatmapData[0, 0, i];
            }
        }

        return maxIndex;
    }
}