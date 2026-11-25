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

    private void Awake()
    {
        if (volume == null)
        {
            volume = GetComponent<Volume>();
        }

        if (volume != null && volume.profile != null && volume.profile.TryGet(out liftGammaGain))
        {
            liftGammaGain.lift.overrideState = true;
            liftGammaGain.gamma.overrideState = true;
            liftGammaGain.gain.overrideState = true;
            ResetColor();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
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
            StartRedFlash();
        }
    }

    private void StartRedFlash()
    {
        if (liftGammaGain == null) return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FadeFromRed());
    }

    private IEnumerator FadeFromRed()
    {
        SetRed();
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flashDuration);

            liftGammaGain.lift.value = Vector4.Lerp(RedShift, Neutral, t);
            liftGammaGain.gamma.value = Vector4.Lerp(RedShift, Neutral, t);
            liftGammaGain.gain.value = Vector4.Lerp(RedShift, Neutral, t);

            yield return null;
        }

        ResetColor();
        flashRoutine = null;
    }

    private void SetRed()
    {
        liftGammaGain.lift.value = RedShift;
        liftGammaGain.gamma.value = RedShift;
        liftGammaGain.gain.value = RedShift;
    }

    private void ResetColor()
    {
        liftGammaGain.lift.value = Neutral;
        liftGammaGain.gamma.value = Neutral;
        liftGammaGain.gain.value = Neutral;
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