using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HideElementOnButtonClick : MonoBehaviour, IPointerClickHandler
{
    public RawImage targetImage;
    public Texture defaultTexture;

    public GameObject objectToHide;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetImage != null && defaultTexture != null)
        {
            targetImage.texture = defaultTexture;
        }

        if (objectToHide != null)
        {
            objectToHide.SetActive(false);
        }
    }
}