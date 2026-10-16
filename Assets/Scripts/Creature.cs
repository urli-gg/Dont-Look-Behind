using UnityEngine;
using UnityEngine.SceneManagement;

public class Creature : MonoBehaviour
{
    public Transform player;
    public Transform playerCamera;

    public float speed = 1.5f;
    public float retreatSpeed = 3f;
    public float gameOverDistance = 1.2f;
    public float lookThreshold = 0.7f;

    [Header("Audio")]
    public AudioSource footstepAudio;

    void Start()
    {
        if (footstepAudio != null)
        {
            footstepAudio.Play();
        }
    }

    void Update()
    {
        Vector3 directionToCreature =
            transform.position - playerCamera.position;

        directionToCreature.y = 0;
        directionToCreature.Normalize();

        Vector3 cameraForward = playerCamera.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        float dot = Vector3.Dot(
            cameraForward,
            directionToCreature
        );

        bool playerIsLooking = dot > lookThreshold;

        // Si estamos mirando a la criatura
        if (playerIsLooking)
        {
            // Detenemos temporalmente los pasos
            if (footstepAudio != null && footstepAudio.isPlaying)
            {
                footstepAudio.Pause();
            }

            Vector3 directionAway =
                transform.position - player.position;

            directionAway.y = 0;
            directionAway.Normalize();

            transform.position +=
                directionAway * retreatSpeed * Time.deltaTime;

            return;
        }

        // Si dejamos de mirarla, vuelven los pasos
        if (footstepAudio != null && !footstepAudio.isPlaying)
        {
            footstepAudio.UnPause();
        }

        Vector3 creaturePosition = transform.position;
        Vector3 playerPosition = player.position;

        creaturePosition.y = 0;
        playerPosition.y = 0;

        float distanceToPlayer =
            Vector3.Distance(creaturePosition, playerPosition);

        // Si alcanza al jugador
        if (distanceToPlayer <= gameOverDistance)
        {
            SceneManager.LoadScene("GameOver");
            return;
        }

        // Continúa persiguiendo al jugador
        Vector3 targetPosition = player.position;
        targetPosition.y = transform.position.y;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );
    }
}
