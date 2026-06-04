using UnityEngine;
using UnityEngine.Localization.Settings;

public class SyncLocalization : MonoBehaviour
{
    // Ép hệ thống hoàn tất khởi tạo và tải ngôn ngữ ngay trong frame đầu
    private void Awake()
    {
        LocalizationSettings.InitializationOperation.WaitForCompletion();
    }
}