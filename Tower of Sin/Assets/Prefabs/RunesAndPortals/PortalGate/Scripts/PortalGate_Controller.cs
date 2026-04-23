using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PortalGate_Controller : MonoBehaviour
{
    [Header("Applied to the effects at start")]
    [SerializeField] private Color portalEffectColor = Color.cyan; // Gave it a default color just in case

    [Header("Boss Arena Override")]
    [SerializeField] private bool controlledByBossArena = false;

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

        // Get materials to set color and emission
        Material[] mats = portalRenderer.materials.ToArray();

        if (mats.Length > 1)
        {
            portalMat = mats[0];
            portalEffectMat = mats[1];

            portalMat.SetColor("_EmissionColor", portalEffectColor);
            portalMat.SetFloat("_EmissionStrength", 0f);
            portalEffectMat.SetColor("_ColorMain", portalEffectColor);
            portalEffectMat.SetFloat("_PortalFade", 0f);
        }

        if (symbolTF != null)
        {
            symbolStartPos = symbolTF.localPosition;

            Renderer symbolRenderer = symbolTF.GetComponent<Renderer>();
            if (symbolRenderer != null)
                symbolRenderer.material = portalMat;
        }

        // Get and set light intensity
        if (portalLight != null)
        {
            portalLight.color = portalEffectColor;
            lightF = portalLight.intensity;
            portalLight.intensity = 0f;
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

    private void Start()
    {
        // Keep old behavior for normal portals in other scenes.
        // Boss arena portals can opt out and be controlled externally.
        if (!controlledByBossArena)
        {
            F_TogglePortalGate(true);
        }
    }

    public void SetControlledByBossArena(bool value)
    {
        controlledByBossArena = value;
    }

    public bool IsControlledByBossArena()
    {
        return controlledByBossArena;
    }

    public void F_TogglePortalGate(bool _activate)
    {
        if (inTransition || portalActive == _activate || portalRenderer == null)
            return;

        portalActive = _activate;

        if (_activate) // activate
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
        else // deactivate
        {
            foreach (ParticleSystem part in effectsPartSystems)
            {
                if (part != null) part.Stop();
            }
        }

        if (!inTransition)
            transitionCor = StartCoroutine(PortalTransition());
    }

    private IEnumerator PortalTransition()
    {
        inTransition = true;

        if (portalActive) // fade in
        {
            while (transitionF < 1f)
            {
                transitionF = Mathf.MoveTowards(transitionF, 1f, Time.deltaTime * 0.2f);

                if (portalMat != null) portalMat.SetFloat("_EmissionStrength", transitionF);
                if (portalEffectMat != null) portalEffectMat.SetFloat("_PortalFade", transitionF * 0.4f);
                if (portalLight != null) portalLight.intensity = lightF * transitionF;
                if (portalAudio != null) portalAudio.volume = transitionF * 0.8f; // max volume

                yield return null;
            }

            inTransition = false;
        }
        else // fade out
        {
            while (transitionF > 0f)
            {
                transitionF = Mathf.MoveTowards(transitionF, 0f, Time.deltaTime * 0.4f);

                if (portalMat != null) portalMat.SetFloat("_EmissionStrength", transitionF);
                if (portalEffectMat != null) portalEffectMat.SetFloat("_PortalFade", transitionF * 0.4f);
                if (portalLight != null) portalLight.intensity = lightF * transitionF;
                if (portalAudio != null) portalAudio.volume = transitionF * 0.8f; // max volume

                yield return null;
            }

            if (portalAudio != null) portalAudio.Stop();
            inTransition = false;

            if (symbolMovementCor != null)
            {
                StopCoroutine(symbolMovementCor);
                symbolMovementCor = null;
            }
        }
    }

    private IEnumerator SymbolMovement()
    {
        Vector3 randomPos = symbolStartPos;
        float lerpF = 0f;

        while (true)
        {
            if (symbolTF.localPosition == randomPos)
            {
                Vector3 offset = Vector3.zero;
                offset.y = Random.Range(-0.08f, 0.08f);
                offset.z = Random.Range(-0.08f, 0.08f);

                randomPos = symbolStartPos + offset;
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