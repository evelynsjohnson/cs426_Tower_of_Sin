using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShowElementOnButtonClick : MonoBehaviour, IPointerClickHandler
{
    public RawImage targetImage;
    public Texture defaultTexture;

    public GameObject objectToShow;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetImage != null && defaultTexture != null)
        {
            targetImage.texture = defaultTexture;
        }

        if (objectToShow != null)
        {
            objectToShow.SetActive(true);
        }
    }
}