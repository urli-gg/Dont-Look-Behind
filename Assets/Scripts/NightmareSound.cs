using UnityEngine;

public sealed class NightmareSound : MonoBehaviour
{
    public HorrorTensionDirector tension;
    public AudioSource roomTone;
    public AudioSource pursuitBed;
    public AudioSource[] pipes;
    public AudioSource footsteps;
    public AudioSource clueEmitter;
    public AudioSource finalEmitter;
    public AudioClip step;
    public AudioClip landing;
    public AudioClip seal;
    public AudioClip lastSeal;
    public AudioClip finalKnock;

    private bool running;
    private float duckUntil;
    private float nextSilence;
    private bool captured;
    private float[] pipeRest;

    public void Begin()
    {
        if (running)
            return;
        running = true;
        pipeRest = new float[pipes.Length];
        for (int i = 0; i < pipes.Length; i++)
            pipeRest[i] = pipes[i].volume;
        roomTone.Play();
        pursuitBed.Play();
        nextSilence = Time.time + Random.Range(14f, 20f);
    }

    private void Update()
    {
        if (!running)
            return;
        if (Time.time >= nextSilence)
        {
            if (!tension.ExitClimax)
                Duck(1.4f);
            nextSilence = Time.time + Random.Range(13f, 24f);
        }

        float pressure = tension.Intensity;
        float duck = captured ? 0.08f : Time.time < duckUntil ? 0.20f : 1f;
        float mix = tension.Profile.audioScale * (HotelOptions.ReducedScares ? 0.68f : 1f) * duck;
        float target = Mathf.Lerp(0.16f, 0.36f, pressure) * mix;
        roomTone.volume = Mathf.MoveTowards(roomTone.volume, target, Time.deltaTime * 0.25f);
        pursuitBed.volume = Mathf.MoveTowards(pursuitBed.volume,
            Mathf.InverseLerp(0.43f, 0.95f, pressure) * 0.33f * mix, Time.deltaTime * 0.25f);

        for (int i = 0; i < pipes.Length; i++)
            pipes[i].volume = pipeRest[i] * Mathf.Lerp(1f, 0.52f, pressure) * duck;
    }

    public void Duck(float seconds)
    {
        duckUntil = Mathf.Max(duckUntil, Time.time + seconds);
    }

    public void ForCapture()
    {
        captured = true;
        Duck(3f);
    }

    public void Footstep(Vector3 at, bool teleported)
    {
        footsteps.transform.position = at + Vector3.up * 0.08f;
        footsteps.pitch = Random.Range(0.86f, 1.11f);
        footsteps.PlayOneShot(teleported ? landing : step, teleported ? 0.40f : 0.27f);
    }

    public void Clue(Vector3 at)
    {
        clueEmitter.transform.position = at;
        clueEmitter.PlayOneShot(seal, 0.45f);
    }

    public void CueThreeClues()
    {
        Duck(1.4f);
        clueEmitter.PlayOneShot(lastSeal, HotelOptions.ReducedScares ? 0.24f : 0.42f);
    }

    public void Climax()
    {
        Duck(0.8f);
        finalEmitter.PlayOneShot(finalKnock, HotelOptions.ReducedScares ? 0.38f : 0.71f);
    }
}
