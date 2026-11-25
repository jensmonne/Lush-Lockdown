using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Change to .HighDefinition if using HDRP

public class WireHazard : MonoBehaviour
{
    [SerializeField] private HealthScript healthScript;
    [SerializeField] private Volume volume;
    [SerializeField] private float flashDuration = 0.5f;
    [SerializeField] private PlayerSounds playerSounds;
    [SerializeField] private AudioClip shockSFX;

    private int hitCounter;
    private LiftGammaGain liftGammaGain;
    private Coroutine flashRoutine;

    private static readonly Vector4 Neutral = new Vector4(1f, 1f, 1f, 0f);
    private static readonly Vector4 RedShift = new Vector4(1.35f, 0.7f, 0.7f, 0f);

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Touched: " + collision.gameObject.name);
        if (collision.gameObject.CompareTag("Player"))
        {
            HitWire();
        }
    }

    private void HitWire()
    {
        hitCounter++;
        if (hitCounter >= 2)
        {
            healthScript.Death();
        }
        else
        {
            ShockSFX();
            playerSounds.HurtSFX();
            healthScript.Hurt();
        }
    } 

    private void ShockSFX()
    {
        if (shockSFX == null) return;

        AudioSource tempSource = gameObject.AddComponent<AudioSource>();
        tempSource.clip = shockSFX;
        tempSource.Play();
        Destroy(tempSource, shockSFX.length);
    }
}