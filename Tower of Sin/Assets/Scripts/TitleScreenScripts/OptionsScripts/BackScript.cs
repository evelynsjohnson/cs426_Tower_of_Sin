using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BackScript : MonoBehaviour, IPointerClickHandler
{
    public GameObject canvasToHide;
    public GameObject canvasToShow;

    public Texture normalTexture;
    private RawImage rawImage;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (rawImage != null) rawImage.texture = normalTexture;

        if (canvasToHide != null)
        {
            canvasToHide.SetActive(false);
        }

        if (canvasToShow != null)
        {
            canvasToShow.SetActive(true);
        }
    }
}