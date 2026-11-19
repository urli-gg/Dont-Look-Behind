using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class CreatureAttackSequence : MonoBehaviour
{
    public AudioSource attackSound;
    public AudioSource tailSound;
    public AudioSource anticipationSound;
    public NightmareSound soundscape;
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
        if (soundscape != null)
            soundscape.ForCapture();
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
            animator.speed = reduced ? 0.64f : 0.38f;
        if (anticipationSound != null)
            anticipationSound.PlayOneShot(anticipationSound.clip, reduced ? 0.24f : 0.55f);
        bool impactPlayed = false;

        bool tailPlayed = false;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float progress = elapsed / duration;
            float recoil = Mathf.Sin(Mathf.InverseLerp(0f, 0.30f, progress) * Mathf.PI);
            float motion = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.22f, 0.42f, progress));
            if (!impactPlayed && progress >= 0.24f)
            {
                impactPlayed = true;
                if (animator != null)
                    animator.speed = reduced ? 0.78f : 1.45f;
                if (attackSound != null)
                    attackSound.PlayOneShot(attackSound.clip, reduced ? 0.35f : 0.83f);
                if (!reduced && haptics != null)
                    foreach (var hand in haptics)
                        hand.SendHapticImpulse(0.34f, 0.16f);
            }
            if (visual != null && !reduced)
            {
                visual.localPosition = start + offset * motion - Vector3.up * recoil * 0.058f;
                visual.localRotation = rest * Quaternion.Euler(13f * recoil - 16f * motion, -8f * recoil, 7f * motion);
            }
            if (attackLight != null)
                attackLight.intensity = (reduced ? 0.20f : 1.05f) * Mathf.Sin(Mathf.InverseLerp(0.14f, 0.86f, progress) * Mathf.PI);
            if (!tailPlayed && progress > 0.50f && tailSound != null)
            {
                tailPlayed = true;
                tailSound.Play();
            }
            if (fade != null)
                fade.SetOpacity(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(reduced ? 0.16f : 0.44f, 0.92f, progress)));
            yield return null;
        }

        if (fade != null)
            fade.SetOpacity(1f);
        yield return SceneManager.LoadSceneAsync("GameOver");
    }
}