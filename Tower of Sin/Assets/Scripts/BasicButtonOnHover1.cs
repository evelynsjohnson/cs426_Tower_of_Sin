using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Texture normalTexture;
    public Texture redTexture;
    private RawImage rawImage;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (rawImage != null) rawImage.texture = redTexture;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (rawImage != null) rawImage.texture = normalTexture;
    }
}