using System.Collections;
using UnityEngine;

public class JumpscareTrigger : MonoBehaviour
{
    public GameObject jumpscareObject;
    public AudioSource jumpscareAudio;
    public AudioSource reveal;
    public Light accent;
    public Transform viewer;
    public Vector3 drift = new(0.38f, 0f, 0f);
    public float anticipation = 0.36f;
    public float jumpscareDuration = 0.8f;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.CompareTag("Player"))
            PlayOnce();
    }

    public void PlayOnce()
    {
        if (hasTriggered || HotelOptions.ReducedScares)
            return;
        hasTriggered = true;
        StartCoroutine(PlayJumpscare());
    }

    private IEnumerator PlayJumpscare()
    {
        if (jumpscareAudio != null)
            jumpscareAudio.Play();
        if (anticipation > 0f)
            yield return new WaitForSeconds(anticipation);

        if (jumpscareObject == null)
            yield break;
        Vector3 start = jumpscareObject.transform.localPosition;
        jumpscareObject.SetActive(true);
        if (reveal != null)
            reveal.Play();
        float watchTime = 0f;
        for (float elapsed = 0f; elapsed < jumpscareDuration; elapsed += Time.deltaTime)
        {
            float progress = elapsed / jumpscareDuration;
            jumpscareObject.transform.localPosition = start + drift * Mathf.SmoothStep(0f, 1f, progress);
            if (accent != null)
                accent.intensity = 0.65f * Mathf.Sin(progress * Mathf.PI);
            if (viewer != null && Vector3.Dot(viewer.forward, (jumpscareObject.transform.position + Vector3.up - viewer.position).normalized) > 0.93f)
                watchTime += Time.deltaTime;
            if (elapsed > 0.33f && watchTime > 0.49f)
                break;
            yield return null;
        }

        if (accent != null)
            accent.intensity = 0f;
        jumpscareObject.SetActive(false);
        if (TryGetComponent(out BoxCollider trigger))
            trigger.enabled = false;
    }
}
