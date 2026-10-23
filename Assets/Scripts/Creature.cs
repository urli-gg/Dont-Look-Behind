using UnityEngine;
using UnityEngine.SceneManagement;

public class Creature : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public Transform playerCamera;

    [Header("Movimiento")]
    public float speed = 1.5f;
    public float retreatSpeed = 3f;
    public float gameOverDistance = 1.2f;

    [Header("Deteccion de mirada")]
    [Range(-1f, 1f)]
    public float lookThreshold = 0.7f;

    [Header("Limites para mirar a la criatura")]
    public int maxTurns = 3;
    public float maxLookTime = 3f;

    [Header("Audio")]
    public AudioSource footstepAudio;


    // Cuantas veces el jugador ya volteo a mirar
    private int turnsUsed = 0;

    // Sirve para detectar el momento exacto
    // en que el jugador empieza a mirar
    private bool wasLookingLastFrame = false;

    // Tiempo que lleva mirando durante el giro actual
    private float currentLookTime = 0f;

    // Evita intentar cargar GameOver varias veces
    private bool gameOverTriggered = false;


    void Start()
    {
        // Comprobamos referencias importantes
        if (player == null || playerCamera == null)
        {
            Debug.LogError("Creature: falta asignar Player o Player Camera en el Inspector.");

            enabled = false;
            return;
        }

        // Comienzan los pasos de la criatura
        if (footstepAudio != null && !footstepAudio.isPlaying)
        {
            footstepAudio.Play();
        }
    }


    void Update()
    {
        if (gameOverTriggered)
            return;


        // -------------------------------------------------
        // 1. DETECTAR SI EL JUGADOR MIRA A LA CRIATURA
        // -------------------------------------------------

        Vector3 directionToCreature = transform.position - playerCamera.position;

        // Solo nos interesa la rotacion horizontal
        directionToCreature.y = 0f;

        if (directionToCreature.sqrMagnitude > 0.001f)
        {
            directionToCreature.Normalize();
        }


        Vector3 cameraForward = playerCamera.forward;

        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude > 0.001f)
        {
            cameraForward.Normalize();
        }


        float dot = Vector3.Dot(cameraForward, directionToCreature);


        bool playerIsLooking = dot > lookThreshold;


        // -------------------------------------------------
        // 2. CONTAR CADA VEZ QUE EL JUGADOR VOLTEA
        // -------------------------------------------------

        // Solo cuenta una vez cuando empieza a mirar.
        // No cuenta cada frame.
        if (playerIsLooking && !wasLookingLastFrame)
        {
            turnsUsed++;

            // Nueva mirada = nuevo temporizador
            currentLookTime = 0f;

            Debug.Log("Giro numero: " + turnsUsed + " / " + maxTurns);
        }


        // -------------------------------------------------
        // 3. CONTROLAR CUANTO TIEMPO PUEDE MIRAR
        // -------------------------------------------------

        bool hasTurnAvailable = turnsUsed <= maxTurns;


        // Si está mirando y todavía tiene oportunidades,
        // aumenta el tiempo de mirada.
        if (playerIsLooking && hasTurnAvailable)
        {
            currentLookTime += Time.deltaTime;
        }

        // Cuando deja de mirar se prepara el temporizador
        // para el siguiente giro.
        if (!playerIsLooking)
        {
            currentLookTime = 0f;
        }


        // Solo puede ahuyentar a la criatura si:
        //
        // 1. La esta mirando.
        // 2. Todavia tiene giros disponibles.
        // 3. No agotó los segundos de esta mirada.
        bool canRepelCreature = playerIsLooking && hasTurnAvailable && currentLookTime < maxLookTime;


        wasLookingLastFrame = playerIsLooking;


        // -------------------------------------------------
        // 4. AHUYENTAR A LA CRIATURA
        // -------------------------------------------------

        if (canRepelCreature)
        {
            // Mientras retrocede, detenemos los pasos
            if (footstepAudio != null && footstepAudio.isPlaying)
            {
                footstepAudio.Pause();
            }


            Vector3 directionAway = transform.position - player.position;

            directionAway.y = 0f;


            if (directionAway.sqrMagnitude > 0.001f)
            {
                directionAway.Normalize();
            }


            transform.position += directionAway * retreatSpeed * Time.deltaTime;

            return;
        }


        // -------------------------------------------------
        // 5. SI YA NO PUEDE AHUYENTARLA, VUELVE A PERSEGUIR
        // -------------------------------------------------

        // IMPORTANTE:
        // Incluso si el jugador SIGUE mirando,
        // cuando se terminan maxLookTime segundos
        // llegamos aquí y la criatura vuelve a perseguirlo.

        if (footstepAudio != null && !footstepAudio.isPlaying)
        {
            footstepAudio.UnPause();
        }


        // -------------------------------------------------
        // 6. CALCULAR DISTANCIA AL JUGADOR
        // -------------------------------------------------

        Vector3 creaturePosition = transform.position;

        Vector3 playerPosition = player.position;


        creaturePosition.y = 0f;
        playerPosition.y = 0f;


        float distanceToPlayer = Vector3.Distance(creaturePosition, playerPosition);


        // -------------------------------------------------
        // 7. GAME OVER
        // -------------------------------------------------

        if (distanceToPlayer <= gameOverDistance)
        {
            gameOverTriggered = true;

            SceneManager.LoadScene("GameOver");

            return;
        }


        // -------------------------------------------------
        // 8. PERSEGUIR AL JUGADOR
        // -------------------------------------------------

        Vector3 targetPosition = player.position;

        // La criatura no intenta subir a la altura
        // de la cabeza del jugador.
        targetPosition.y = transform.position.y;


        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
    }
}
