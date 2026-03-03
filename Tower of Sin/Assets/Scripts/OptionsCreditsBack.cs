using UnityEngine;
using static TitleScreenButtons;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsCreditsBack : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
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
        if (mainCanvas != null && creditsCanvas != null && optionsCanvas != null)
            mainCanvas.SetActive(true);
            creditsCanvas.SetActive(false);
            optionsCanvas.SetActive(false);
    }
}
