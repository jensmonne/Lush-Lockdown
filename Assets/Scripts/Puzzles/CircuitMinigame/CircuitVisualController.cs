using System.Collections.Generic;
using UnityEngine;

public class CircuitVisualController : MonoBehaviour
{
    [Header("UML Attributes - Materials & Colors")]
    [SerializeField] private List<Material> cableMaterials = new List<Material>();
    [SerializeField] private Color emissionColorActive = Color.cyan;
    [SerializeField] private Color emissionColorInactive = Color.gray;
    [Header("UML Attributes - Particle Effects")]
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
    
   
    }

    public void PlaySparkeffect()
    {
        if (sparkParticles != null)
        {
            sparkParticles.Play();
            
        }
       
    }

    public void PlayElectricShockEffect()
    {
        if (shockVFX != null)
        {
            shockVFX.Play();
       
        }
    }
}
