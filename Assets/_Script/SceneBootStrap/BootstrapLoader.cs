using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    public string firstSceneToLoad = "Farm";

    private void Start()
    {
        SceneManager.LoadSceneAsync(firstSceneToLoad);
    }
}