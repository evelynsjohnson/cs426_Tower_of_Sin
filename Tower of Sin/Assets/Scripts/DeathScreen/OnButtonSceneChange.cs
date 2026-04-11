using UnityEngine;

public class OnButtonSceneChange : MonoBehaviour
{
    public string sceneName;
    // when a raw image button is clicked, change the scene
    public void ChangeScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
