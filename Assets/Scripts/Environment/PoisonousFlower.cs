using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PoisonousFlower : MonoBehaviour
{
    [SerializeField] HealthScript healthScript;
    [SerializeField] private Volume volume;
    [SerializeField] private float countdownDuration = 5f;
    [SerializeField] private float recoveryDuration = 5f;
    [SerializeField] private string playerTag = "Player";

    [Header("Vignette Settings")]
    [SerializeField] private float startIntensity = 0f;
    [SerializeField] private float maxIntensity = 0.75f;

    private Vignette _vignette;
    private ColorAdjustments _colorAdjustments;
    private Coroutine _timerCoroutine;
    private float _remainingTime;
    private bool _isInside;
    private bool _isDead;

    private void Awake()
    {
        if (volume == null)
        {
            volume = GetComponent<Volume>();
        }

        _remainingTime = countdownDuration;

        if (volume != null && volume.profile != null)
        {
            if (!volume.profile.TryGet(out _vignette))
            {
                Debug.LogWarning("Vignette override not found on the Volume profile.");
            }
            else
            {
                UpdateVignetteFromRemainingTime();
            }

            if (!volume.profile.TryGet(out _colorAdjustments))
            {
                Debug.LogWarning("Color Adjustments override not found on the Volume profile—add it to the Volume for full blackout.");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isDead || !other.CompareTag(playerTag)) return;

        _isInside = true;
        RestartTimerLoop();
    }

    private void OnTriggerExit(Collider other)
    {
        if (_isDead || !other.CompareTag(playerTag)) return;

        _isInside = false;
        Debug.Log("Left hitbox; recovering...");
        RestartTimerLoop();
    }

    private void RestartTimerLoop()
    {
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
        }

        _timerCoroutine = StartCoroutine(FlowerLoop());
    }

    private IEnumerator FlowerLoop()
    {
        while (!_isDead)
        {
            if (_isInside)
            {
                _remainingTime = Mathf.Max(0f, _remainingTime - Time.deltaTime);
                UpdateVignetteFromRemainingTime();
                Debug.Log($"Dying in {_remainingTime:F1}s...");

                if (_remainingTime <= 0f)
                {
                    _timerCoroutine = null;
                    healthScript.Death();
                    yield break;
                }
            }
            else
            {
                float safeRecovery = Mathf.Max(0.01f, recoveryDuration);
                float recoveryRate = countdownDuration / safeRecovery;

                _remainingTime = Mathf.Min(countdownDuration, _remainingTime + recoveryRate * Time.deltaTime);
                UpdateVignetteFromRemainingTime();

                if (_remainingTime >= countdownDuration)
                {
                    _timerCoroutine = null;
                    yield break;
                }
            }

            yield return null;
        }
    }

    private void UpdateVignetteFromRemainingTime()
    {

        if (_vignette == null || countdownDuration <= 0f) return;

        float dangerProgress = 1f - Mathf.Clamp01(_remainingTime / countdownDuration);
        _vignette.intensity.value = Mathf.Lerp(startIntensity, maxIntensity, dangerProgress);
    }
}