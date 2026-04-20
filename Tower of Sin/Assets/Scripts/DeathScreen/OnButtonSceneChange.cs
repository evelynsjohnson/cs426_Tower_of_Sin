using UnityEngine;
using UnityEngine.SceneManagement;

public class OnButtonSceneChange : MonoBehaviour
{
    public string sceneName;
    public void ChangeScene(string sceneName)
    {
        if (sceneName == "Death_Realm")
        {
            PlayerHealth ph = FindObjectOfType<PlayerHealth>();
            if (ph != null)
                ph.ResetForNewRun();
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}