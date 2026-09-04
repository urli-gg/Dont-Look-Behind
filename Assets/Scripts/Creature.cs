using UnityEngine;
using UnityEngine.SceneManagement;

public class Creature : MonoBehaviour
{
    public Transform player;
    public Transform playerCamera;

    public float speed = 1.5f;
    public float retreatSpeed = 3f;
    public float gameOverDistance = 1.2f;

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distanceToPlayer <= gameOverDistance)
        {
            SceneManager.LoadScene("GameOver");
            return;
        }

        Vector3 directionToCreature =
            (transform.position - playerCamera.position).normalized;

        float dot = Vector3.Dot(
            playerCamera.forward,
            directionToCreature
        );

        bool playerIsLooking = dot > 0.7f;

        if (playerIsLooking)
        {
            Vector3 directionAway =
                (transform.position - player.position).normalized;

            transform.position +=
                directionAway * retreatSpeed * Time.deltaTime;
        }
        else
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position,
                speed * Time.deltaTime
            );
        }
    }
}
