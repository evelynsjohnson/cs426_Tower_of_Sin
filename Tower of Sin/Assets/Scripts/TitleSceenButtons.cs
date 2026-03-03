using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class TitleScreenButtons : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    public enum ButtonType
    {
        Begin,
        Options,
        Credits
    }

    public ButtonType buttonType;

    public RawImage targetImage;
    public Texture normalTexture;
    public Texture hoverTexture;

    public GameObject optionsCanvas;
    public GameObject creditsCanvas;
    public GameObject mainCanvas;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetImage != null && hoverTexture != null)
            targetImage.texture = hoverTexture;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetImage != null && normalTexture != null)
            targetImage.texture = normalTexture;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (buttonType)
        {
            case ButtonType.Begin:
                SceneManager.LoadScene("Prison_Scene");
                break;

            case ButtonType.Options:
                if (optionsCanvas != null)
                    optionsCanvas.SetActive(true);
                    mainCanvas.SetActive(false);
                break;

            case ButtonType.Credits:
                if (creditsCanvas != null)
                    creditsCanvas.SetActive(true);
                    mainCanvas.SetActive(false);
                break;
        }
    }
}