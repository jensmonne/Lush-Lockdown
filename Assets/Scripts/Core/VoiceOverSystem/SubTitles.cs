using TMPro;
using UnityEngine;

public class SubtitleDisplay : MonoBehaviour
{
    [SerializeField] private GameObject panel;      // child object holding the background + text
    [SerializeField] private TMP_Text label;
    [SerializeField] private Transform head;        // your VR camera
    [SerializeField] private float distance = 1.8f;
    [SerializeField] private float followSpeed = 3f;

    private void Awake() => Hide();

    public void Show(string text)
    {
        label.text = text;
        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);

    private void LateUpdate()
    {
        // Glide toward a spot in front of the player's face
        Vector3 target = head.position + head.forward * distance;
        transform.position = Vector3.Lerp(transform.position, target, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(transform.position - head.position);
    }
}