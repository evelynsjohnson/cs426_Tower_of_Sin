using UnityEngine;
using UnityEngine.SceneManagement;

public class OnButtonSceneChange : MonoBehaviour
{
    public string sceneName;
    public void ChangeScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
