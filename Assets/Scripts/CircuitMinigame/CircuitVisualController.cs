using System.Collections.Generic;
using UnityEngine;

public class CircuitVisualController : MonoBehaviour
{
    [Header("UML Attributen - Materials & Colors")]
    [SerializeField] private List<Material> cableMaterials = new List<Material>();
    [SerializeField] private Color emissionColorActive = Color.cyan;
    [SerializeField] private Color emissionColorInactive = Color.gray;
    
    [Header("UML Attributen - Particle Effects")]
    [SerializeField] private ParticleSystem sparkParticles;
    [SerializeField] private ParticleSystem shockVFX;

    public void SetPowerActive(bool active)
    {
        Color targetColor = active ? emissionColorActive : emissionColorInactive;
        foreach (Material mat in cableMaterials)
        {
            if (mat != null)
            {
                // Zet emissie aan en pas de kleur aan
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", targetColor);
            }
        }
        Debug.Log("Visueel: Stroom is nu " + (active ? "AAN (Neon fel)" : "UIT (Gedimd)"));
        // Hier kun je straks materialen/kleuren aanpassen
    }

    public void PlaySparkeffect()
    {
        if (sparkParticles != null)
        {
            sparkParticles.Play();
            Debug.Log("CircuitVisualController: Vonken-effect afgespeeld!");
        }
       
    }

    public void PlayElectricShockEffect()
    {
        if (shockVFX != null)
        {
            shockVFX.Play();
            Debug.Log("CircuitVisualController: Schok-effect afgespeeld!");
        }
    }
}
