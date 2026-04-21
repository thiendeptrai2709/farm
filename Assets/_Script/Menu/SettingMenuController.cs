using UnityEngine;
using UnityEngine.UI; // Phải gọi thư viện UI để dùng được Outline

public class SettingMenuController : MonoBehaviour
{
    [Header("Setting Sub-Panels")]
    public GameObject graphicsTab;
    public GameObject audioTab;
    public GameObject controlsTab;
    public GameObject languageTab;

    [Header("Component Outline của các nút")]
    public Outline graphicsOutline;
    public Outline audioOutline;
    public Outline controlsOutline;
    public Outline languageOutline;

    private void OnEnable()
    {
        OpenAudioTab();
    }

    public void OpenGraphicsTab() => SwitchTab(graphicsTab, graphicsOutline);
    public void OpenAudioTab() => SwitchTab(audioTab, audioOutline);
    public void OpenControlsTab() => SwitchTab(controlsTab, controlsOutline);
    public void OpenLanguageTab() => SwitchTab(languageTab, languageOutline);

    // Thay đổi kiểu dữ liệu của targetOutline từ GameObject thành Outline
    private void SwitchTab(GameObject targetTab, Outline targetOutline)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");

        // Tắt toàn bộ Tab
        if (graphicsTab != null) graphicsTab.SetActive(false);
        if (audioTab != null) audioTab.SetActive(false);
        if (controlsTab != null) controlsTab.SetActive(false);
        if (languageTab != null) languageTab.SetActive(false);

        // Tắt component Outline (Dùng .enabled thay vì .SetActive)
        if (graphicsOutline != null) graphicsOutline.enabled = false;
        if (audioOutline != null) audioOutline.enabled = false;
        if (controlsOutline != null) controlsOutline.enabled = false;
        if (languageOutline != null) languageOutline.enabled = false;

        // Bật Tab và Outline được chọn
        if (targetTab != null) targetTab.SetActive(true);
        if (targetOutline != null) targetOutline.enabled = true;
    }
}