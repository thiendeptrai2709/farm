using UnityEngine;

public class TerrainPainter : MonoBehaviour
{
    [Header("Terrain Settings")]
    [Tooltip("Kéo Terrain mặt đất vào đây")]
    public Terrain activeTerrain;

    [Tooltip("Số thứ tự của Layer đất trồng trong Terrain (Bắt đầu từ 0, 1, 2...)")]
    public int dirtLayerIndex = 1;

    // Hàm này sẽ nhận Tọa độ tâm và Kích thước của MẢNH ĐẤT MỚI để tô
    public void PaintDirtArea(Vector3 newAreaCenter, Vector3 areaSize)
    {
        if (activeTerrain == null) activeTerrain = Terrain.activeTerrain;
        if (activeTerrain == null) return;

        TerrainData terrainData = activeTerrain.terrainData;
        Vector3 terrainPos = activeTerrain.transform.position;

        // Tính tỷ lệ quy đổi từ không gian thực (Mét) sang không gian ảnh (Pixel của Splatmap)
        float widthRatio = terrainData.alphamapWidth / terrainData.size.x;
        float lengthRatio = terrainData.alphamapHeight / terrainData.size.z;

        // Tính góc dưới cùng bên trái của mảnh đất mới
        float startX = newAreaCenter.x - (areaSize.x / 2f);
        float startZ = newAreaCenter.z - (areaSize.z / 2f);

        int mapStartX = Mathf.RoundToInt((startX - terrainPos.x) * widthRatio);
        int mapStartZ = Mathf.RoundToInt((startZ - terrainPos.z) * lengthRatio);

        int mapSizeX = Mathf.RoundToInt(areaSize.x * widthRatio);
        int mapSizeZ = Mathf.RoundToInt(areaSize.z * lengthRatio);

        // Chốt chặn an toàn: Tránh lỗi tô màu tràn ra ngoài viền bản đồ
        mapStartX = Mathf.Clamp(mapStartX, 0, terrainData.alphamapWidth);
        mapStartZ = Mathf.Clamp(mapStartZ, 0, terrainData.alphamapHeight);
        mapSizeX = Mathf.Clamp(mapSizeX, 0, terrainData.alphamapWidth - mapStartX);
        mapSizeZ = Mathf.Clamp(mapSizeZ, 0, terrainData.alphamapHeight - mapStartZ);

        // Lấy mảng dữ liệu màu hiện tại của khu vực đó
        float[,,] splatmapData = terrainData.GetAlphamaps(mapStartX, mapStartZ, mapSizeX, mapSizeZ);

        // Vòng lặp quét từng pixel và đổ màu
        for (int z = 0; z < mapSizeZ; z++)
        {
            for (int x = 0; x < mapSizeX; x++)
            {
                for (int i = 0; i < terrainData.alphamapLayers; i++)
                {
                    // Nếu đúng layer đất trồng thì đẩy lên 100% (1.0f), các layer cỏ khác ép về 0
                    splatmapData[z, x, i] = (i == dirtLayerIndex) ? 1.0f : 0.0f;
                }
            }
        }

        // Đẩy dữ liệu đã vẽ ngược lại xuống Terrain
        terrainData.SetAlphamaps(mapStartX, mapStartZ, splatmapData);
        Debug.Log("[TerrainPainter] Đã tô màu thành công phần đất mở rộng!");
    }
}