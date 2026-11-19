using System;
using System.Collections;
using UnityEngine;

public class HorrorEventDirector : MonoBehaviour
{
    public enum Effect { Sound, FaultyLight, Drop, Apparition, Shift, Blackout, Behind, Door, Fall }

    [Serializable]
    public class Beat
    {
        public string id;
        public Effect effect;
        public Transform marker;
        public AudioSource sound;
        public Light fixture;
        public BreakableObject breakable;
        public JumpscareTrigger ambientScare;
        public HotelSwingDoor door;
        public Transform shift;
        public Vector3 offset;
        public HorrorTensionDirector.Stage minimumStage;
        public float minimumDistance;
        public float maximumDistance;
        public bool requiresRoom;
        public bool burnOut;
        public bool duckMix;
        [NonSerialized] public bool played;
    }

    public Transform playerHead;
    public Beat[] beats;
    public HorrorTensionDirector tension;
    public NightmareLighting lighting;
    public NightmareSound soundscape;
    [Min(1f)] public float minimumGap = 5f;
    [Min(1f)] public float maximumGap = 9f;
    [Min(1f)] public float searchRadius = 7f;
    public int EventsPlayed { get; private set; }

    private bool begun;
    private int exhausted;
    private Effect? previous;

    public void Begin()
    {
        if (begun)
            return;
        begun = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (beats == null)
            yield break;
        yield return new WaitForSeconds(UnityEngine.Random.Range(3.2f, 5.1f));
        while (playerHead != null && exhausted < beats.Length && (tension == null || tension.Active))
        {
            var selected = Select();
            if (selected == null)
            {
                yield return new WaitForSeconds(1.1f);
                continue;
            }

            selected.played = true;
            exhausted++;
            previous = selected.effect;
            EventsPlayed++;
            if (tension != null)
                tension.RecordEvent(selected.id, selected.effect.ToString());
            yield return Perform(selected);
            float gap = tension != null && tension.Profile != null ? tension.Profile.eventGap : UnityEngine.Random.Range(minimumGap, maximumGap);
            float relief = EventsPlayed % 3 == 0 ? 1.6f : 1f;
            if (HotelOptions.ReducedScares)
                relief *= 1.2f;
            yield return new WaitForSeconds(gap * relief * UnityEngine.Random.Range(0.82f, 1.27f));
        }
    }

    private Beat Select()
    {
        Beat best = null;
        float total = 0f;
        foreach (var beat in beats)
        {
            if (beat.played || beat.marker == null || beat.effect == Effect.Drop && beat.breakable == null)
                continue;
            if (beat.requiresRoom && (tension == null || tension.RoomDwell < 1.2f))
                continue;
            if (tension != null && tension.State < beat.minimumStage)
                continue;
            Vector3 delta = beat.marker.position - playerHead.position;
            float maximum = beat.maximumDistance > 0f ? beat.maximumDistance : searchRadius;
            if (delta.sqrMagnitude > maximum * maximum || delta.sqrMagnitude < beat.minimumDistance * beat.minimumDistance)
                continue;
            if (beat.effect == Effect.Apparition &&
                (HotelOptions.ReducedScares || Vector3.Dot(playerHead.forward, delta.normalized) < 0.36f))
                continue;

            float weight = previous == beat.effect ? 0.35f : 1f;
            if (beat.requiresRoom || beat.effect == Effect.Blackout)
                weight *= 1.45f;
            total += weight;
            if (UnityEngine.Random.value * total < weight)
                best = beat;
        }
        return best;
    }

    private IEnumerator Perform(Beat beat)
    {
        bool reduced = HotelOptions.ReducedScares;
        if (beat.duckMix && soundscape != null)
            soundscape.Duck(1.45f);
        PlayCue(beat, reduced);

        switch (beat.effect)
        {
            case Effect.FaultyLight when lighting != null:
                lighting.Disrupt(beat.fixture, reduced ? 1.5f : 2.4f, beat.burnOut);
                break;
            case Effect.Blackout when lighting != null:
                lighting.Blackout(reduced ? 1.25f : 1.95f);
                break;
            case Effect.Drop when beat.breakable != null:
                yield return new WaitForSeconds(0.27f);
                if (beat.breakable != null && !beat.breakable.GetComponent<HotelCarryable>().Held)
                    beat.breakable.Break(beat.breakable.transform.position, Vector3.down * 2f);
                break;
            case Effect.Apparition when beat.ambientScare != null:
                beat.ambientScare.PlayOnce();
                yield return new WaitForSeconds(1.45f);
                break;
            case Effect.Door when beat.door != null:
                yield return new WaitForSeconds(0.38f);
                beat.door.CloseFromFright(reduced ? 95f : 165f);
                break;
            case Effect.Shift or Effect.Fall when beat.shift != null:
                yield return MoveProp(beat, reduced);
                break;
        }
    }

    private void PlayCue(Beat beat, bool reduced)
    {
        if (beat.sound == null || beat.sound.clip == null)
            return;
        if (beat.effect == Effect.Behind)
        {
            Vector3 forward = Vector3.ProjectOnPlane(playerHead.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            beat.sound.transform.position = playerHead.position - forward * 1.30f + right * side * 0.92f;
        }
        beat.sound.pitch = UnityEngine.Random.Range(0.89f, 1.10f);
        beat.sound.PlayOneShot(beat.sound.clip, reduced ? 0.48f : 0.77f);
    }

    private static IEnumerator MoveProp(Beat beat, bool reduced)
    {
        Vector3 origin = beat.shift.localPosition;
        Quaternion start = beat.shift.localRotation;
        bool falling = beat.effect == Effect.Fall;
        Vector3 move = falling ? beat.offset : new Vector3(beat.offset.x, 0f, beat.offset.z);
        Quaternion change = falling ? Quaternion.Euler(0f, 0f, -78f) : Quaternion.Euler(0f, beat.offset.y, 0f);
        float duration = falling ? (reduced ? 0.65f : 0.37f) : 0.55f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = elapsed / duration;
            float t = falling ? progress * progress : Mathf.SmoothStep(0f, 1f, progress);
            beat.shift.localPosition = origin + move * t;
            beat.shift.localRotation = Quaternion.Slerp(start, start * change, t);
            yield return null;
        }
        beat.shift.localPosition = origin + move;
        beat.shift.localRotation = start * change;
    }
}
