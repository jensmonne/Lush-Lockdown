using System.Collections;
using UnityEngine;

public class PF : MonoBehaviour
{
    [SerializeField] private float countdownDuration = 5f;
    [SerializeField] private string playerTag = "Player";

    private Coroutine deathCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (deathCoroutine == null)
        {
            deathCoroutine = StartCoroutine(Flower());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (deathCoroutine != null)
        {
            StopCoroutine(deathCoroutine);
            deathCoroutine = null;
            Debug.Log("Countdown cancelled.");
        }
    }

    private IEnumerator Flower()
    {
        float remainingTime = countdownDuration;

        while (remainingTime > 0f)
        {
            Debug.Log($"Dying in {remainingTime:F1}s...");
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        deathCoroutine = null;
        Die();
    }

    private void Die()
    {
        Debug.Log("Player died!");
    }
}