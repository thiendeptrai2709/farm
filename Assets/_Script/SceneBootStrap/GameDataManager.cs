using System.Collections.Generic;
using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance;

    public Dictionary<string, ChestData> chestDataDict = new Dictionary<string, ChestData>();
    public Dictionary<string, FarmPlotData> farmPlotDataDict = new Dictionary<string, FarmPlotData>();
    public Dictionary<ItemData, float> savedDailyPrices = new Dictionary<ItemData, float>();

    public long lastFarmExitTimeTicks = 0;
    public bool isMarketInitialized = false;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}