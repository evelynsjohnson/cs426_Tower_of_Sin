using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PortalGate_Controller : MonoBehaviour
{
    [Header("Applied to the effects at start")]
    [SerializeField] private Color portalEffectColor = Color.cyan; // Gave it a default color just in case

    [Header("Changing these might `break` the effects")]
    [Space(20)]
    [SerializeField] private Renderer portalRenderer;
    [SerializeField] private ParticleSystem[] effectsPartSystems;
    [SerializeField] private Light portalLight;
    [SerializeField] private Transform symbolTF;
    [SerializeField] private AudioSource portalAudio, flashAudio;

    private bool portalActive, inTransition;
    private float transitionF, lightF;
    private Material portalMat, portalEffectMat;
    private Vector3 symbolStartPos;

    private Coroutine transitionCor, symbolMovementCor;

    private void OnEnable()
    {
        // Safety check to prevent errors if the renderer is missing
        if (portalRenderer == null) return;

        //get materials to set color and emmision
        Material[] mats = portalRenderer.materials.ToArray();

        if (mats.Length > 1)
        {
            portalMat = mats[0];
            portalEffectMat = mats[1];

            portalMat.SetColor("_EmissionColor", portalEffectColor);
            portalMat.SetFloat("_EmissionStrength", 0);
            portalEffectMat.SetColor("_ColorMain", portalEffectColor);
            portalEffectMat.SetFloat("_PortalFade", 0f);
        }

        if (symbolTF != null)
        {
            symbolStartPos = symbolTF.localPosition;
            if (symbolTF.GetComponent<Renderer>() != null)
                symbolTF.GetComponent<Renderer>().material = portalMat;
        }

        //get and set light intensity
        if (portalLight != null)
        {
            portalLight.color = portalEffectColor;
            lightF = portalLight.intensity;
            portalLight.intensity = 0;
        }

        foreach (ParticleSystem part in effectsPartSystems)
        {
            if (part != null)
            {
                ParticleSystem.MainModule mod = part.main;
                mod.startColor = portalEffectColor;
            }
        }
    }

    // --- ADDED THIS TO MAKE IT AUTO-START ---
    private void Start()
    {
        F_TogglePortalGate(true);
    }

    public void F_TogglePortalGate(bool _activate)
    {
        if (inTransition || portalActive == _activate || portalRenderer == null)
            return;

        portalActive = _activate;

        if (_activate)//activate
        {
            foreach (ParticleSystem part in effectsPartSystems)
            {
                if (part != null) part.Play();
            }

            if (portalAudio != null) portalAudio.Play();
            if (flashAudio != null) flashAudio.Play();

            if (symbolTF != null)
                symbolMovementCor = StartCoroutine(SymbolMovement());
        }
        else if (!_activate)//deactivate
        {
            foreach (ParticleSystem part in effectsPartSystems)
            {
                if (part != null) part.Stop();
            }
        }

        if (!inTransition)
            transitionCor = StartCoroutine(PortalTransition());
    }

    IEnumerator PortalTransition()
    {
        inTransition = true;

        if (portalActive)//fade in
        {
            while (transitionF < 1f)
            {
                transitionF = Mathf.MoveTowards(transitionF, 1, Time.deltaTime * 0.2f);

                if (portalMat != null) portalMat.SetFloat("_EmissionStrength", transitionF);
                if (portalEffectMat != null) portalEffectMat.SetFloat("_PortalFade", transitionF * 0.4f);
                if (portalLight != null) portalLight.intensity = lightF * transitionF;
                if (portalAudio != null) portalAudio.volume = transitionF * 0.8f;//max volume

                yield return new WaitForSeconds(Time.deltaTime);
            }

            inTransition = false;
        }
        else if (!portalActive)//fade out
        {
            while (transitionF > 0f)
            {
                transitionF = Mathf.MoveTowards(transitionF, 0f, Time.deltaTime * 0.4f);

                if (portalMat != null) portalMat.SetFloat("_EmissionStrength", transitionF);
                if (portalEffectMat != null) portalEffectMat.SetFloat("_PortalFade", transitionF * 0.4f);
                if (portalLight != null) portalLight.intensity = lightF * transitionF;
                if (portalAudio != null) portalAudio.volume = transitionF * 0.8f;//max volume

                yield return new WaitForSeconds(Time.deltaTime);
            }

            if (portalAudio != null) portalAudio.Stop();
            inTransition = false;

            if (symbolMovementCor != null) StopCoroutine(symbolMovementCor);
        }
    }

    private IEnumerator SymbolMovement()
    {
        Vector3 randomPos = symbolStartPos;
        float lerpF = 0;

        while (true)
        {
            if (symbolTF.localPosition == randomPos)
            {
                randomPos[1] = Random.Range(-0.08f, 0.08f);
                randomPos[2] = Random.Range(-0.08f, 0.08f);

                randomPos = symbolStartPos + randomPos;
                lerpF = 0f;
            }
            else
            {
                symbolTF.localPosition = Vector3.Slerp(symbolTF.localPosition, randomPos, lerpF);
                lerpF += 0.001f;
            }

            yield return new WaitForSeconds(0.04f);
        }
    }
}