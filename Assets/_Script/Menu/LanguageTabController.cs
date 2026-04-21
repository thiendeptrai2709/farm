using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using TMPro;

public class LanguageTabController : MonoBehaviour
{
    public TextMeshProUGUI mainButtonText;
    public GameObject dropdownPanel;

    private bool isChanging = false;

    private void OnEnable()
    {
        StartCoroutine(InitializeLanguageUI());
    }

    private IEnumerator InitializeLanguageUI()
    {
        yield return LocalizationSettings.InitializationOperation;
        UpdateMainButtonText();

        if (dropdownPanel != null)
        {
            dropdownPanel.SetActive(false);
        }
    }

    public void ToggleDropdownPanel()
    {
        if (dropdownPanel != null)
        {
            dropdownPanel.SetActive(!dropdownPanel.activeSelf);
        }
    }

    public void SelectLanguage(int localeID)
    {
        if (isChanging) return;
        StartCoroutine(SetLocaleCoroutine(localeID));
    }

    private IEnumerator SetLocaleCoroutine(int localeID)
    {
        isChanging = true;

        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeID];

        UpdateMainButtonText();

        if (dropdownPanel != null)
        {
            dropdownPanel.SetActive(false);
        }

        isChanging = false;
    }

    private void UpdateMainButtonText()
    {
        if (mainButtonText != null)
        {
            var currentLocale = LocalizationSettings.SelectedLocale;
            int currentID = LocalizationSettings.AvailableLocales.Locales.IndexOf(currentLocale);

            if (currentID == 0) mainButtonText.text = "English";
            else if (currentID == 1) mainButtonText.text = "Tiếng Việt";
        }
    }
}