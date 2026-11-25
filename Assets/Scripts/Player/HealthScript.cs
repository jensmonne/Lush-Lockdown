using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HealthScript : MonoBehaviour
{
    [SerializeField] PlayerSounds playerSounds;
    [SerializeField] private Volume volume;
    private float deathPostExposure = -15f;

    private bool _isDead = false;
    private Vignette _vignette;
    private ColorAdjustments _colorAdjustments;

    private void Awake()
    {
        if (volume == null) volume = GetComponent<Volume>();

        if (volume != null && volume.profile != null)
        {
            volume.profile.TryGet(out _vignette);
            volume.profile.TryGet(out _colorAdjustments);
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
}