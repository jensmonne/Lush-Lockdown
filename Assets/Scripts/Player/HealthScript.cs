using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HealthScript : MonoBehaviour
{
    [SerializeField] PlayerSounds playerSounds;
    [SerializeField] private Volume volume;

    [SerializeField] private float flashDuration = 0.5f;
    private float deathPostExposure = -15f;

    private bool _isDead = false;
    private Vignette _vignette;
    private ColorAdjustments _colorAdjustments;

    private LiftGammaGain liftGammaGain;
    private Coroutine flashRoutine;

    private static readonly Vector4 Neutral = new Vector4(1f, 1f, 1f, 0f);
    private static readonly Vector4 RedShift = new Vector4(1.35f, 0.7f, 0.7f, 0f);

    private void Awake()
    {
        if (volume == null) volume = GetComponent<Volume>();

        if (volume != null && volume.profile != null)
        {
            volume.profile.TryGet(out _vignette);
            volume.profile.TryGet(out _colorAdjustments);
        }
        if (volume != null && volume.profile != null && volume.profile.TryGet(out liftGammaGain))
        {
            liftGammaGain.lift.overrideState = true;
            liftGammaGain.gamma.overrideState = true;
            liftGammaGain.gain.overrideState = true;
            ResetColor();
        }
    }

    public void Death()
    {
        if (_isDead) return;
        _isDead = true;
        playerSounds.DeathSFX();
        Debug.Log("Player died!");

        if (_vignette != null)
        {
            _vignette.color.Override(Color.black);
            _vignette.smoothness.Override(1f);
            _vignette.intensity.Override(1f);
        }

        if (_colorAdjustments != null)
        {
            _colorAdjustments.colorFilter.Override(Color.black);
            _colorAdjustments.postExposure.Override(deathPostExposure);
        }
    }

    public void Hurt()
    {
        StartRedFlash();
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

}