using UnityEngine;

public class TerrainPainter : MonoBehaviour
{
    [Header("Terrain Settings")]
    public Terrain activeTerrain;

    [Tooltip("Số thứ tự của Layer ĐẤT TRỒNG (Bắt đầu từ 0)")]
    public int dirtLayerIndex = 1;

    [Tooltip("Số thứ tự của Layer CỎ (Để nó lấy cỏ lấp đất lại)")]
    public int grassLayerIndex = 0; // THƯỜNG LÀ 0

    // Hàm chà sạch bóng một khu vực (Xóa đất, đắp cỏ)
    public void WipeSpecificArea(Vector3 center, Vector3 areaSize)
    {
        if (activeTerrain == null) activeTerrain = Terrain.activeTerrain;
        if (activeTerrain == null) return;

        TerrainData terrainData = activeTerrain.terrainData;
        Vector3 terrainPos = activeTerrain.transform.position;

        float widthRatio = terrainData.alphamapWidth / terrainData.size.x;
        float lengthRatio = terrainData.alphamapHeight / terrainData.size.z;

        float startX = center.x - (areaSize.x / 2f);
        float startZ = center.z - (areaSize.z / 2f);

        int mapStartX = Mathf.RoundToInt((startX - terrainPos.x) * widthRatio);
        int mapStartZ = Mathf.RoundToInt((startZ - terrainPos.z) * lengthRatio);
        int mapSizeX = Mathf.RoundToInt(areaSize.x * widthRatio);
        int mapSizeZ = Mathf.RoundToInt(areaSize.z * lengthRatio);

        // Clamp chống tràn map
        mapStartX = Mathf.Clamp(mapStartX, 0, terrainData.alphamapWidth);
        mapStartZ = Mathf.Clamp(mapStartZ, 0, terrainData.alphamapHeight);
        mapSizeX = Mathf.Clamp(mapSizeX, 0, terrainData.alphamapWidth - mapStartX);
        mapSizeZ = Mathf.Clamp(mapSizeZ, 0, terrainData.alphamapHeight - mapStartZ);

        if (mapSizeX <= 0 || mapSizeZ <= 0) return;

        float[,,] splatmapData = terrainData.GetAlphamaps(mapStartX, mapStartZ, mapSizeX, mapSizeZ);

        for (int z = 0; z < mapSizeZ; z++)
        {
            for (int x = 0; x < mapSizeX; x++)
            {
                float dirtAmount = splatmapData[z, x, dirtLayerIndex];

                // Nếu chỗ này có đất trồng thì mới xử lý
                if (dirtAmount > 0)
                {
                    // Chuyển toàn bộ alpha của đất sang cho cỏ (bù vào chỗ bị khuyết)
                    splatmapData[z, x, grassLayerIndex] += dirtAmount;

                    // Xóa lớp đất
                    splatmapData[z, x, dirtLayerIndex] = 0.0f;

                    // Khóa giá trị cỏ không cho vượt quá 1.0f để tránh lỗi hiển thị Terrain
                    splatmapData[z, x, grassLayerIndex] = Mathf.Clamp01(splatmapData[z, x, grassLayerIndex]);
                }
            }
        }
        terrainData.SetAlphamaps(mapStartX, mapStartZ, splatmapData);
    }
    public void PaintDirtArea(Vector3 newAreaCenter, Vector3 areaSize)
    {
        if (activeTerrain == null) activeTerrain = Terrain.activeTerrain;
        if (activeTerrain == null) return;

        TerrainData terrainData = activeTerrain.terrainData;
        Vector3 terrainPos = activeTerrain.transform.position;

        float widthRatio = terrainData.alphamapWidth / terrainData.size.x;
        float lengthRatio = terrainData.alphamapHeight / terrainData.size.z;

        float startX = newAreaCenter.x - (areaSize.x / 2f);
        float startZ = newAreaCenter.z - (areaSize.z / 2f);

        int mapStartX = Mathf.RoundToInt((startX - terrainPos.x) * widthRatio);
        int mapStartZ = Mathf.RoundToInt((startZ - terrainPos.z) * lengthRatio);
        int mapSizeX = Mathf.RoundToInt(areaSize.x * widthRatio);
        int mapSizeZ = Mathf.RoundToInt(areaSize.z * lengthRatio);

        mapStartX = Mathf.Clamp(mapStartX, 0, terrainData.alphamapWidth);
        mapStartZ = Mathf.Clamp(mapStartZ, 0, terrainData.alphamapHeight);
        mapSizeX = Mathf.Clamp(mapSizeX, 0, terrainData.alphamapWidth - mapStartX);
        mapSizeZ = Mathf.Clamp(mapSizeZ, 0, terrainData.alphamapHeight - mapStartZ);

        float[,,] splatmapData = terrainData.GetAlphamaps(mapStartX, mapStartZ, mapSizeX, mapSizeZ);

        for (int z = 0; z < mapSizeZ; z++)
        {
            for (int x = 0; x < mapSizeX; x++)
            {
                for (int i = 0; i < terrainData.alphamapLayers; i++)
                {
                    splatmapData[z, x, i] = (i == dirtLayerIndex) ? 1.0f : 0.0f;
                }
            }
        }
        terrainData.SetAlphamaps(mapStartX, mapStartZ, splatmapData);
    }
}