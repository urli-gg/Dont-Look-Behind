using System;
using UnityEngine;

public sealed class NightmareLighting : MonoBehaviour
{
    [Serializable]
    public sealed class Fixture
    {
        public Light light;
        public MeshRenderer housing;
        public Material[] litMaterials;
        public Material[] deadMaterials;
        [Min(0f)] public float level;
        [Range(0f, 1f)] public float fault;
        public bool failBehind;
        public bool room;
        public bool emergency;
        public bool ruined;

        [NonSerialized] public bool extinguished;
        [NonSerialized] public bool roomVisited;
        [NonSerialized] public bool showingDead;
        [NonSerialized] public float dipUntil;
    }

    public Fixture[] fixtures;
    public Light exitPilot;
    public HorrorTensionDirector tension;
    public Creature creature;
    public Transform playerHead;

    private float blackoutUntil;
    private bool blackoutActive;
    private float ambianceDip;
    private bool active;

    private void Awake() => ApplyImmediate();

    public void Begin() => active = true;

    public void ApplyImmediate()
    {
        float multiplier = tension != null && tension.Profile != null ? tension.Profile.lightScale : 1f;
        if (fixtures == null)
            return;
        foreach (var fixture in fixtures)
        {
            if (fixture.light == null)
                continue;
            fixture.light.intensity = fixture.ruined || fixture.emergency ? 0f : fixture.level * multiplier;
            if (!fixture.room)
                fixture.light.enabled = fixture.light.intensity > 0.01f;
            ShowHousing(fixture, fixture.ruined || fixture.emergency);
        }
    }

    public bool IsBlackout => blackoutActive;

    public void Blackout(float seconds)
    {
        blackoutUntil = Mathf.Max(blackoutUntil, Time.time + seconds);
    }

    public void Disrupt(Light light, float seconds, bool burnOut)
    {
        if (light == null || fixtures == null)
            return;
        foreach (var fixture in fixtures)
        {
            if (fixture.light != light)
                continue;
            fixture.extinguished |= burnOut && !HotelOptions.ReducedScares;
            fixture.dipUntil = Time.time + seconds;
            return;
        }
    }

    private void Update()
    {
        if (!active || tension == null || playerHead == null)
            return;
        float now = Time.time;
        bool reduced = HotelOptions.ReducedScares;
        float threat = tension.Intensity;
        float scale = tension.Profile.lightScale;
        blackoutActive = now < blackoutUntil;
        ambianceDip = Mathf.MoveTowards(ambianceDip, blackoutActive ? 1f : 0f, Time.deltaTime * (reduced ? 0.8f : 1.9f));
        bool immediateThreat = creature.gameObject.activeInHierarchy && creature.State != Creature.BehaviourState.Disarmed;
        foreach (var fixture in fixtures)
        {
            if (fixture.light == null)
                continue;
            Vector3 position = fixture.light.transform.position;
            bool entered = fixture.room && Mathf.Abs(playerHead.position.x) > 2.15f &&
                Mathf.Abs(playerHead.position.x - position.x) < 1.7f && Mathf.Abs(playerHead.position.z - position.z) < 2.05f;
            if (entered && !fixture.roomVisited)
            {
                fixture.roomVisited = true;
                fixture.dipUntil = now + 2.15f;
            }

            if (fixture.failBehind && !reduced && threat > 0.42f && playerHead.position.z > position.z + 3.7f)
                fixture.extinguished = true;

            float target = fixture.level * scale;
            if (fixture.emergency)
            {
                target *= tension.ExitClimax ? 0.95f : tension.State >= HorrorTensionDirector.Stage.Panic ? 0.40f : 0f;
                target *= 0.87f + 0.13f * Mathf.Sin(now * 2.5f);
            }
            else
            {
                bool nearbyCreature = immediateThreat && (creature.transform.position - position).sqrMagnitude < 13f;
                float instability = fixture.fault * (nearbyCreature ? 1f : threat * 0.38f);
                float envelope = Mathf.SmoothStep(0f, 1f, Mathf.PerlinNoise(now * (reduced ? 1.35f : 4.1f), position.z + 32f));
                target *= 1f - instability * envelope * (reduced ? 0.22f : 0.74f);
                target *= Mathf.Lerp(1f, reduced ? 0.47f : 0.075f, ambianceDip);
                if (fixture.dipUntil > now)
                    target *= reduced ? 0.65f : 0.23f;
                if (fixture.ruined || fixture.extinguished)
                    target = 0f;
            }

            fixture.light.intensity = Mathf.MoveTowards(fixture.light.intensity, target, Time.deltaTime * (reduced ? 0.9f : 2.3f));
            if (!fixture.room)
                fixture.light.enabled = fixture.light.intensity > 0.015f;
            ShowHousing(fixture, !fixture.light.enabled || fixture.light.intensity < fixture.level * 0.18f);
        }

        if (exitPilot != null)
            exitPilot.intensity = Mathf.MoveTowards(exitPilot.intensity, tension.ExitClimax ? 0.85f : 0.26f, Time.deltaTime * 0.8f);
    }

    private static void ShowHousing(Fixture fixture, bool dead)
    {
        if (fixture.housing == null || fixture.showingDead == dead)
            return;
        fixture.showingDead = dead;
        fixture.housing.sharedMaterials = dead ? fixture.deadMaterials : fixture.litMaterials;
    }
}
