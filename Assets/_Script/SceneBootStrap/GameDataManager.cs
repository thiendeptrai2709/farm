using System.Collections.Generic;
using UnityEngine;
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance;

    public Dictionary<ItemData, float> savedDailyPrices = new Dictionary<ItemData, float>();
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