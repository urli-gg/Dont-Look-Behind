using System;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-20)]
public sealed class HorrorTensionDirector : MonoBehaviour
{
    public enum Stage { Unease, Suspicion, Threat, Panic, Desperation }

    public NightmareProfile[] profiles;
    public CorridorStartTrigger corridor;
    public Creature creature;
    public GameCodeManager puzzle;
    public Keypad keypad;
    public Transform playerHead;
    public NightmareLighting lighting;
    public NightmareSound soundscape;
    public TMP_Text clueHint;

    public NightmareProfile Profile { get; private set; }
    public Stage State { get; private set; }
    public float Intensity { get; private set; }
    public bool Active { get; private set; }
    public bool ExitClimax { get; private set; }
    public Vector3 PlayerVelocity { get; private set; }
    public float RoomDwell { get; private set; }
    public int Clues => puzzle.CluesFound;
    public NightmareTelemetry Telemetry { get; private set; }

    private const float TickInterval = 0.20f;
    private float explorationStarted;
    private float lastClue;
    private float nextTick;
    private float nextStep;
    private float noisePressure;
    private Vector3 previousPosition;
    private bool alarmPlayed;

    private void Awake()
    {
        SetProfile(HotelOptions.Difficulty);
        Telemetry = new NightmareTelemetry();
    }

    private void OnEnable()
    {
        if (puzzle != null) puzzle.ClueDiscovered += OnClue;
        if (creature != null) creature.LookSpent += OnLook;
        if (keypad != null) keypad.Interacted += OnKeypad;
        if (corridor != null && corridor.elevatorSequence != null)
            corridor.elevatorSequence.Started += StartRun;
        HotelNoise.Heard += OnNoise;
    }

    private void OnDisable()
    {
        if (puzzle != null) puzzle.ClueDiscovered -= OnClue;
        if (creature != null) creature.LookSpent -= OnLook;
        if (keypad != null) keypad.Interacted -= OnKeypad;
        if (corridor != null && corridor.elevatorSequence != null)
            corridor.elevatorSequence.Started -= StartRun;
        HotelNoise.Heard -= OnNoise;
        Telemetry?.Complete("abandoned");
    }

    public void SetProfile(NightmareDifficulty difficulty)
    {
        if (Active || profiles == null || profiles.Length == 0)
            return;
        Profile = Array.Find(profiles, profile => profile != null && profile.difficulty == difficulty) ?? profiles[0];
        if (clueHint != null)
            clueHint.text = Profile.clueHint;
        if (lighting != null)
            lighting.ApplyImmediate();
    }

    private void StartRun()
    {
        Telemetry.Begin(Profile.difficulty);
    }

    public void Begin()
    {
        if (Active || Profile == null)
            return;
        if (!Telemetry.Started)
            StartRun();
        Active = true;
        Intensity = 0.13f;
        State = Stage.Unease;
        explorationStarted = lastClue = Time.time;
        previousPosition = Ground(playerHead.position);
        nextTick = Time.time + TickInterval;
        Telemetry.Record("corridor");
        if (lighting != null) lighting.Begin();
        if (soundscape != null) soundscape.Begin();
    }

    private void Update()
    {
        if (!Active || Time.time < nextTick)
            return;
        nextTick = Time.time + TickInterval;
        Vector3 position = Ground(playerHead.position);
        SampleFootsteps(position);

        float distance = Vector3.Distance(position, Ground(creature.transform.position));
        bool threatPresent = creature.isActiveAndEnabled && creature.State is not (Creature.BehaviourState.Waiting or Creature.BehaviourState.Disarmed);
        float closeness = threatPresent ? 1f - Mathf.InverseLerp(1.2f, 10f, distance) : 0f;
        RoomDwell = InsideGuestRoom(position) ? RoomDwell + TickInterval : 0f;
        float clock = Mathf.Clamp01((Time.time - explorationStarted) / 125f);
        float stall = Mathf.Clamp01((Time.time - lastClue) / 31f);
        float room = Mathf.Clamp01((RoomDwell - Profile.roomPressureAfter) / 7f);
        float progress = Clues * 0.08f * Profile.progressPressure;
        float used = (creature.maxTurns - creature.TurnsRemaining) * 0.072f;
        noisePressure = Mathf.MoveTowards(noisePressure, 0f, TickInterval * 0.055f);

        if (Clues >= puzzle.codeLength || (Clues == puzzle.codeLength - 1 && position.z > 15.6f))
            EnterClimax();

        float target = 0.13f + clock * 0.22f + stall * 0.18f + closeness * 0.31f + progress + used + room * 0.23f + noisePressure;
        if (ExitClimax)
            target = Mathf.Max(0.87f, target + 0.13f);
        Intensity = Mathf.MoveTowards(Intensity, Mathf.Clamp01(target), TickInterval * (target > Intensity ? 0.40f : 0.095f));
        ResolveStage();
        if (threatPresent)
            Telemetry.Observe(creature.State, distance <= 5.5f && creature.HasLineOfSightToPlayer);
    }

    private void ResolveStage()
    {
        float hysteresis = 0.045f;
        Stage next = State;
        if ((int)State < Thresholds.Length && Intensity >= Thresholds[(int)State])
            next++;
        else if (State > Stage.Unease && Intensity < Thresholds[(int)State - 1] - hysteresis)
            next--;
        if (next == State)
            return;
        State = next;
        Telemetry.Record("tension", State.ToString(), Intensity);
    }

    private static readonly float[] Thresholds = { 0.29f, 0.48f, 0.67f, 0.84f };

    private void SampleFootsteps(Vector3 position)
    {
        Vector3 movement = position - previousPosition;
        previousPosition = position;
        float distance = movement.magnitude;
        PlayerVelocity = distance > 1.65f ? Vector3.zero :
            Vector3.Lerp(PlayerVelocity, movement / TickInterval, 0.65f);
        if (distance < 0.25f || Time.time < nextStep)
            return;

        nextStep = Time.time + 0.55f;
        HotelNoise.Emit(position + Vector3.up * 0.12f, distance > 1.65f ? 5.4f : 2.6f);
        if (soundscape != null)
            soundscape.Footstep(position, distance > 1.65f);
    }

    private static Vector3 Ground(Vector3 position) => new(position.x, 0f, position.z);

    public static bool InsideGuestRoom(Vector3 position) =>
        (position.x < -2.05f && Mathf.Abs(position.z + 8.6f) < 2.30f) ||
        (position.x > 2.05f && Mathf.Abs(position.z - 3.85f) < 2.30f);

    private void OnClue(int index)
    {
        if (!Telemetry.Started)
            return;
        lastClue = Time.time;
        Telemetry.Clue(index);
        if (soundscape != null)
            soundscape.Clue(puzzle.numberSpawnPoints[index].position);
        if (index is 1 or 2)
            HotelNoise.Emit(puzzle.numberSpawnPoints[index].position, 6f);
        if (Clues == puzzle.codeLength - 1)
            soundscape?.CueThreeClues();
        if (Clues >= puzzle.codeLength)
            EnterClimax();
    }

    private void OnLook(int remaining) => Telemetry?.Look(remaining);

    private void OnNoise(Vector3 location, float radius)
    {
        if (!Active || (location - playerHead.position).sqrMagnitude > 64f)
            return;
        noisePressure = Mathf.Min(0.17f, noisePressure + (radius > 5f ? 0.075f : 0.017f));
    }

    private void OnKeypad(Keypad.ActionKind action)
    {
        if (!Active)
            return;
        Telemetry.Keypad(action);
        if (action != Keypad.ActionKind.Cleared)
            EnterClimax();
    }

    private void EnterClimax()
    {
        ExitClimax = true;
        if (alarmPlayed)
            return;
        alarmPlayed = true;
        Telemetry.Record("climax");
        if (soundscape != null) soundscape.Climax();
        if (lighting != null) lighting.Blackout(1.05f);
    }

    public void RecordEvent(string id, string kind)
    {
        Telemetry?.Record("event", id + "/" + kind, Intensity);
    }

    public void CompleteRun(bool escaped)
    {
        Active = false;
        Telemetry?.Complete(escaped ? "victory" : "death");
    }
}
