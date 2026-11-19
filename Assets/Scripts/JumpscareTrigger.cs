using System.Collections;
using UnityEngine;

public class JumpscareTrigger : MonoBehaviour
{
    public GameObject jumpscareObject;
    public AudioSource jumpscareAudio;
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
        if (jumpscareObject != null)
            jumpscareObject.SetActive(true);
        if (jumpscareAudio != null)
            jumpscareAudio.Play();

        yield return new WaitForSeconds(jumpscareDuration);

        if (jumpscareObject != null)
            jumpscareObject.SetActive(false);
        if (TryGetComponent(out BoxCollider trigger))
            trigger.enabled = false;
    }
}