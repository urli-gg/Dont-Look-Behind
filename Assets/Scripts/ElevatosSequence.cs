
using System.Collections;
using UnityEngine;

public class ElevatorSequence : MonoBehaviour
{
    [Header("Puertas")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Menu")]
    public GameObject menuCanvas;

    [Header("Criatura")]
    public GameObject creature;

    [Header("Sonidos")]
    public AudioSource motorSound;
    public AudioSource dingSound;

    [Header("Configuracion")]
    public float travelDuration = 3f;
    public float doorDuration = 1.5f;
    public float doorDistance = 1.4f;

    private Vector3 leftStart;
    private Vector3 rightStart;
    private bool started;

    void Awake()
    {
        if (leftDoor != null)
            leftStart = leftDoor.localPosition;

        if (rightDoor != null)
            rightStart = rightDoor.localPosition;

        // La criatura espera hasta que el jugador salga.
        if (creature != null)
            creature.SetActive(false);
    }

    public void StartElevator()
    {
        if (started)
            return;

        if (leftDoor == null || rightDoor == null)
        {
            Debug.LogError("Faltan puertas del elevador.");
            return;
        }

        started = true;
        StartCoroutine(Travel());
    }

    IEnumerator Travel()
    {
        if (menuCanvas != null)
            menuCanvas.SetActive(false);

        if (motorSound != null)
            motorSound.Play();

        yield return new WaitForSeconds(travelDuration);

        if (motorSound != null)
            motorSound.Stop();

        if (dingSound != null)
            dingSound.Play();

        Vector3 leftEnd =
            leftStart + Vector3.left * doorDistance;

        Vector3 rightEnd =
            rightStart + Vector3.right * doorDistance;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, doorDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            leftDoor.localPosition =
                Vector3.Lerp(leftStart, leftEnd, t);

            rightDoor.localPosition =
                Vector3.Lerp(rightStart, rightEnd, t);

            yield return null;
        }

        // No cargamos otra escena.
        // El jugador ya puede salir al pasillo.
    }

    public void ActivateCreature()
    {
        if (creature != null)
            creature.SetActive(true);
    }
}


