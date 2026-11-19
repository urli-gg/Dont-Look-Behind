using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class CreatureAttackSequence : MonoBehaviour
{
    public AudioSource attackSound;
    public AudioSource tailSound;
    public Light attackLight;
    public HotelFade fade;
    public HapticImpulsePlayer[] haptics;
    [Min(0.6f)] public float duration = 1.9f;
    [Range(0f, 0.32f)] public float lungeDistance = 0.24f;

    private bool playing;

    public void Play(Transform visual, Animator animator, Transform viewer)
    {
        if (playing)
            return;
        playing = true;
        StartCoroutine(Capture(visual, animator, viewer));
    }

    private IEnumerator Capture(Transform visual, Animator animator, Transform viewer)
    {
        bool reduced = HotelOptions.ReducedScares;
        Vector3 start = visual != null ? visual.localPosition : Vector3.zero;
        Quaternion rest = visual != null ? visual.localRotation : Quaternion.identity;
        Vector3 worldDelta = Vector3.ProjectOnPlane(viewer.position - transform.position, Vector3.up);
        float reach = Mathf.Clamp(worldDelta.magnitude - 0.57f, 0f, lungeDistance);
        Vector3 offset = visual != null ? visual.parent.InverseTransformDirection(worldDelta.normalized) * reach : Vector3.zero;
        if (animator != null)
            animator.speed = reduced ? 0.7f : 1.35f;
        if (attackSound != null)
            attackSound.PlayOneShot(attackSound.clip, reduced ? 0.33f : 0.86f);
        if (!reduced && haptics != null)
            foreach (var hand in haptics)
                hand.SendHapticImpulse(0.32f, 0.19f);

        bool tailPlayed = false;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float progress = elapsed / duration;
            float motion = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.02f, 0.38f, progress));
            if (visual != null && !reduced)
            {
                visual.localPosition = start + offset * motion;
                visual.localRotation = rest * Quaternion.Euler(-12f * motion, 0f, 6f * motion);
            }
            if (attackLight != null)
                attackLight.intensity = (reduced ? 0.24f : 1.2f) * (1f - progress) * Mathf.Sin(progress * Mathf.PI);
            if (!tailPlayed && progress > 0.37f && tailSound != null)
            {
                tailPlayed = true;
                tailSound.Play();
            }
            if (fade != null)
                fade.SetOpacity(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(reduced ? 0.12f : 0.32f, 0.92f, progress)));
            yield return null;
        }

        if (fade != null)
            fade.SetOpacity(1f);
        yield return SceneManager.LoadSceneAsync("GameOver");
    }
}