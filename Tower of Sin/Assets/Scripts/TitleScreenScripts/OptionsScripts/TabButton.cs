using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TabButton : MonoBehaviour, IPointerClickHandler
{
    public Texture normalTexture;
    public Texture redTexture;
    public GameObject tabContentPanel;
    public OptionsManager manager;

    private RawImage rawImage;
    private bool isActiveTab = false;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager != null)
        {
            manager.OnTabSelected(this);
        }
    }

    public void SetActiveState(bool isActive)
    {
        isActiveTab = isActive;

        if (rawImage == null) rawImage = GetComponent<RawImage>();

        if (rawImage != null) rawImage.texture = isActive ? redTexture : normalTexture;

        if (tabContentPanel != null)
        {
            tabContentPanel.SetActive(isActive);
        }
    }

    public bool GetIsActive()
    {
        return isActiveTab;
    }
}