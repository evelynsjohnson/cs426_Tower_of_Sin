using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OptionsButtonOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Texture normalTexture;
    public Texture redTexture;
    private RawImage rawImage;
    private TabButton tabButton;
    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        tabButton = GetComponent<TabButton>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tabButton != null && tabButton.GetIsActive()) return;

        if (rawImage != null) rawImage.texture = redTexture;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tabButton != null && tabButton.GetIsActive()) return;

        if (rawImage != null) rawImage.texture = normalTexture;
    }
}