
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class CreditsButton : MonoBehaviour
{
    [SerializeField] private GameObject groupToShow;
    [SerializeField] private GameObject groupToHide;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (groupToShow != null)
            groupToShow.SetActive(true);

        if (groupToHide != null)
            groupToHide.SetActive(false);
    }
}