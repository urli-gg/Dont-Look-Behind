using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class Creature : MonoBehaviour
{
    public enum BehaviourState { Waiting, Stalking, Pursuing, Retreating, Investigating, Approaching, Attacking, Recovering, Disarmed }

    public Transform player;
    public Transform playerCamera;
    public LayerMask architecture;
    public NavMeshAgent agent;
    public CreatureAttackSequence capture;
    public Transform visual;
    public Animator animationBody;
    public AudioSource footstepAudio;
    public AudioSource breathAudio;

    [Min(0.1f)] public float speed = 1.05f;
    [Min(0.1f)] public float retreatSpeed = 2.3f;
    [Min(0.1f)] public float gameOverDistance = 0.86f;
    [Range(0f, 1f)] public float lookThreshold = 0.74f;
    [Min(1)] public int maxTurns = 3;
    [Min(0.1f)] public float maxLookTime = 3f;
    [Min(0f)] public float initialGrace = 3.5f;

    public BehaviourState State { get; private set; }
    public int TurnsRemaining { get; private set; }
    public bool Repelling => State == BehaviourState.Retreating;

    private NavMeshPath retreatPath;
    private float graceUntil;
    private float repelUntil;
    private float lookSince = -1f;
    private float awaySince = -1f;
    private bool gazeLatched;
    private float nextNavigation;
    private float recoverUntil;
    private float investigateUntil;
    private float nextNoise;
    private float nextStep;
    private Vector3 investigation;
    private Quaternion visualRest;

    private void Awake() => retreatPath = new NavMeshPath();

    private void OnEnable()
    {
        TurnsRemaining = maxTurns;
        State = BehaviourState.Waiting;
        graceUntil = Time.time + initialGrace;
        if (visual != null)
            visualRest = visual.localRotation;
        HotelNoise.Heard += OnNoise;
    }

    private void OnDisable() => HotelNoise.Heard -= OnNoise;

    private void Update()
    {
        if (player == null || playerCamera == null || State is BehaviourState.Attacking or BehaviourState.Disarmed)
            return;
        if (Time.time < graceUntil)
            return;

        Vector3 target = playerCamera.position;
        Vector3 groundTarget = new(target.x, transform.position.y, target.z);
        Vector3 toPlayer = groundTarget - transform.position;
        Vector3 eyes = transform.position + Vector3.up * 1.39f;
        bool visible = !Physics.Linecast(playerCamera.position, eyes, architecture, QueryTriggerInteraction.Ignore);
        bool looking = visible && Vector3.Dot(playerCamera.forward, (eyes - target).normalized) >= (gazeLatched ? lookThreshold - 0.07f : lookThreshold);
        UpdateGaze(looking);

        if (looking && repelUntil > Time.time)
        {
            Drive(BehaviourState.Retreating, RetreatTarget(-toPlayer.normalized), retreatSpeed);
            recoverUntil = Time.time + 0.7f;
        }
        else if (Time.time < recoverUntil)
        {
            Drive(BehaviourState.Recovering, transform.position, speed);
        }
        else if (visible && toPlayer.sqrMagnitude <= gameOverDistance * gameOverDistance &&
                 !Physics.Linecast(playerCamera.position, transform.position + Vector3.up * 0.92f, architecture))
        {
            Attack();
        }
        else if (Time.time < investigateUntil && toPlayer.sqrMagnitude > 4f)
        {
            Drive(BehaviourState.Investigating, investigation, speed * 0.83f);
        }
        else if (toPlayer.sqrMagnitude < 5.76f && visible)
        {
            Drive(BehaviourState.Approaching, groundTarget, speed * 0.92f);
        }
        else
        {
            float variation = 1f + 0.10f * Mathf.Sin(Time.time * 0.68f);
            Drive(visible ? BehaviourState.Pursuing : BehaviourState.Stalking, groundTarget, speed * variation);
        }
        Animate(toPlayer);
    }

    private void UpdateGaze(bool looking)
    {
        if (!looking)
        {
            lookSince = -1f;
            if (awaySince < 0f)
                awaySince = Time.time;
            if (Time.time - awaySince > 0.22f)
            {
                gazeLatched = false;
                repelUntil = 0f;
            }
            return;
        }

        awaySince = -1f;
        if (gazeLatched || TurnsRemaining <= 0)
            return;
        if (lookSince < 0f)
            lookSince = Time.time;
        if (Time.time - lookSince < 0.13f)
            return;
        gazeLatched = true;
        TurnsRemaining--;
        repelUntil = Time.time + maxLookTime;
    }

    private Vector3 RetreatTarget(Vector3 away)
    {
        if (agent == null)
            return transform.position + away * 3f;
        if (State == BehaviourState.Retreating && Time.time < nextNavigation)
            return transform.position;
        Vector3 best = transform.position;
        float bestDistance = 0f;
        foreach (float angle in RetreatAngles)
        {
            Vector3 sample = transform.position + Quaternion.Euler(0f, angle, 0f) * away * 3f;
            if (!NavMesh.SamplePosition(sample, out var hit, 1.6f, NavMesh.AllAreas))
                continue;
            float distance = Vector3.Dot(hit.position - transform.position, away);
            if (distance <= bestDistance || !NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, retreatPath) ||
                retreatPath.status != NavMeshPathStatus.PathComplete)
                continue;
            best = hit.position;
            bestDistance = distance;
        }
        return best;
    }

    private static readonly float[] RetreatAngles = { 0f, 55f, -55f, 95f, -95f };

    private void Drive(BehaviourState next, Vector3 destination, float moveSpeed)
    {
        bool changed = State != next;
        State = next;
        if (agent == null)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
            return;
        }
        if (!agent.isOnNavMesh)
            return;

        agent.speed = moveSpeed;
        if (!changed && Time.time < nextNavigation)
            return;
        nextNavigation = Time.time + 0.25f;
        agent.isStopped = (destination - transform.position).sqrMagnitude < 0.08f;
        if (!agent.isStopped && NavMesh.SamplePosition(destination, out var hit, 1.75f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    private void Animate(Vector3 toPlayer)
    {
        bool moving = agent != null && agent.isOnNavMesh && agent.velocity.sqrMagnitude > 0.07f;
        Vector3 facing = moving && !Repelling ? agent.velocity : toPlayer;
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.02f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(facing), 190f * Time.deltaTime);
        if (visual != null)
            visual.localRotation = visualRest * Quaternion.Euler(0f, 0f, moving ? Mathf.Sin(Time.time * 8f) * 3.2f : Mathf.Sin(Time.time * 2f) * 1.4f);
        if (animationBody != null)
            animationBody.speed = moving ? 0.85f : 0.43f;

        if (moving && !Repelling && footstepAudio != null && Time.time >= nextStep)
        {
            nextStep = Time.time + Random.Range(0.44f, 0.62f);
            footstepAudio.pitch = Random.Range(0.87f, 1.1f);
            footstepAudio.Play();
        }

        if (breathAudio != null && !breathAudio.isPlaying)
            breathAudio.Play();
        if (breathAudio != null)
            breathAudio.volume = Mathf.Lerp(0.75f, 0.1f, Mathf.InverseLerp(1f, 7f, toPlayer.magnitude));
    }

    private void OnNoise(Vector3 location, float radius)
    {
        if (Time.time < nextNoise || State is BehaviourState.Waiting or BehaviourState.Attacking or BehaviourState.Disarmed ||
            (transform.position - location).sqrMagnitude > radius * radius)
            return;
        nextNoise = Time.time + 5.5f;
        investigateUntil = Time.time + 2.45f;
        investigation = location;
    }

    private void StopMovement()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        if (footstepAudio != null)
            footstepAudio.Stop();
        if (breathAudio != null)
            breathAudio.Stop();
    }

    private void Attack()
    {
        State = BehaviourState.Attacking;
        StopMovement();
        if (capture != null)
            capture.Play(visual, animationBody, playerCamera);
        else
            SceneManager.LoadScene("GameOver");
    }

    public bool TryDisarm()
    {
        if (State == BehaviourState.Attacking)
            return false;
        State = BehaviourState.Disarmed;
        StopMovement();
        return true;
    }
}
