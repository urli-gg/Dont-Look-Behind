using System;
using System.Collections;
using UnityEngine;

public class HorrorEventDirector : MonoBehaviour
{
    public enum Effect { Sound, FaultyLight, Drop, Apparition, Shift }

    [Serializable]
    public class Beat
    {
        public Effect effect;
        public Transform marker;
        public AudioSource sound;
        public Light fixture;
        public BreakableObject breakable;
        public JumpscareTrigger ambientScare;
        public Transform shift;
        public Vector3 offset;
        [NonSerialized] public bool played;
    }

    public Transform playerHead;
    public Beat[] beats;
    [Min(1f)] public float minimumGap = 5f;
    [Min(1f)] public float maximumGap = 9f;
    [Min(1f)] public float searchRadius = 7f;

    private bool begun;

    public void Begin()
    {
        if (begun)
            return;
        begun = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        while (playerHead != null && beats is { Length: > 0 })
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(minimumGap, Mathf.Max(minimumGap, maximumGap)));
            int start = UnityEngine.Random.Range(0, beats.Length);
            for (int i = 0; i < beats.Length; i++)
            {
                Beat beat = beats[(i + start) % beats.Length];
                if (beat.played || beat.marker == null || (beat.marker.position - playerHead.position).sqrMagnitude > searchRadius * searchRadius)
                    continue;
                beat.played = true;
                yield return Perform(beat);
                break;
            }
        }
    }

    private IEnumerator Perform(Beat beat)
    {
        bool reduced = HotelOptions.ReducedScares;
        if (beat.sound != null)
            beat.sound.PlayOneShot(beat.sound.clip, reduced ? 0.45f : 0.85f);

        switch (beat.effect)
        {
            case Effect.FaultyLight when beat.fixture != null:
                float original = beat.fixture.intensity;
                for (float elapsed = 0f; elapsed < 0.75f; elapsed += Time.deltaTime)
                {
                    beat.fixture.intensity = Mathf.Lerp(original, original * (reduced ? 0.72f : 0.12f), elapsed / 0.75f);
                    yield return null;
                }
                break;
            case Effect.Drop when beat.breakable != null:
                beat.breakable.Break(beat.breakable.transform.position, Vector3.down * 2f);
                break;
            case Effect.Apparition when beat.ambientScare != null && !reduced:
                beat.ambientScare.PlayOnce();
                break;
            case Effect.Shift when beat.shift != null:
                Vector3 origin = beat.shift.localPosition;
                Quaternion start = beat.shift.localRotation;
                Quaternion target = start * Quaternion.Euler(0f, beat.offset.y, 0f);
                for (float elapsed = 0f; elapsed < 0.34f; elapsed += Time.deltaTime)
                {
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.34f);
                    beat.shift.localPosition = origin + new Vector3(beat.offset.x, 0f, beat.offset.z) * t;
                    beat.shift.localRotation = Quaternion.Slerp(start, target, t);
                    yield return null;
                }
                beat.shift.localPosition = origin + new Vector3(beat.offset.x, 0f, beat.offset.z);
                beat.shift.localRotation = target;
                break;
        }
    }
}