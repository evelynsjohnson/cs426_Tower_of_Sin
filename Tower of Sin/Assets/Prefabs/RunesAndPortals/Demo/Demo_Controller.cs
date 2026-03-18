using System.Collections;
using UnityEngine;

public class Demo_Controller : MonoBehaviour
{
    [SerializeField] private Portal_Controller portalSimpleScripts;
    [SerializeField] private PortalGate_Controller portalGateScript;
    [SerializeField] private Transform camBaseTF;
    [SerializeField] private Light lightMain;
    [SerializeField] private GameObject[] SSCameras;
    private int ssCamNR, ssCamsMax;

    private void Start()
    {
        ssCamsMax = SSCameras.Length;

        lightMain.intensity = 0f;
        StartCoroutine(DemoRoutine());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

    }

    private IEnumerator DemoRoutine()
    {
        
        //On
        //start delay
        yield return new WaitForSeconds(3f);
        StartCoroutine(CameraRoutine());
        
        
        yield return new WaitForSeconds(10f);
        portalGateScript.F_TogglePortalGate(true);
        
        yield return new WaitForSeconds(7f);
        portalSimpleScripts.TogglePortal(true);
        
        
        yield return new WaitForSeconds(3f);
        StartCoroutine(FadeLight());
        
        
        //Off
        
        yield return new WaitForSeconds(9f);
        portalGateScript.F_TogglePortalGate(false);
        
        yield return new WaitForSeconds(7f);
        portalSimpleScripts.TogglePortal(false);
    }

    private IEnumerator CameraRoutine()
    {
        while (true)
        {
            camBaseTF.Rotate(Vector3.up, Time.deltaTime * -8f);
            yield return null;
        }
    }

    private IEnumerator FadeLight()
    {
        float fadeFloat = 0f;

        while (fadeFloat < 1f)
        {
            fadeFloat = Mathf.MoveTowards(fadeFloat, 1f, Time.deltaTime * 0.02f);
            lightMain.intensity = fadeFloat;
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }
}
