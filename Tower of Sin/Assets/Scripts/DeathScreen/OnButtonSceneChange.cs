using UnityEngine;
using UnityEngine.SceneManagement;

public class OnButtonSceneChange : MonoBehaviour
{
    public string sceneName;

    public void ChangeScene()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneName);
    }

    public void ChangeScene(string targetSceneName)
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(targetSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset player whenever entering the death realm or prison,
        // depending on how your flow works.
        if (scene.name == "Death_Realm" || scene.name == "Prison")
        {
            PlayerHealth ph = FindAnyObjectByType<PlayerHealth>();
            if (ph != null)
                ph.ResetForNewRun();
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}