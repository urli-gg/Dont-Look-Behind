using UnityEngine;
using System.Collections;

public class JumpscareTrigger : MonoBehaviour
{
    [Header("Objeto que aparecera")]
    public GameObject jumpscareObject;

    [Header("Sonido del jumpscare")]
    public AudioSource jumpscareAudio;

    [Header("Configuracion")]
    public float jumpscareDuration = 0.8f;

    // Evita que el jumpscare ocurra varias veces
    private bool hasTriggered = false;


    private void OnTriggerEnter(Collider other)
    {
        // Si ya sucedio una vez, no hacemos nada
        if (hasTriggered)
        {
            return;
        }


        // En VR muchos colliders pertenecen a objetos
        // hijos del XR Origin.
        // Por eso comprobamos el objeto raiz.
        if (other.transform.root.CompareTag("Player"))
        {
            hasTriggered = true;

            // Ejecutamos el jumpscare
            StartCoroutine(PlayJumpscare());
        }
    }


    private IEnumerator PlayJumpscare()
    {
        // Mostrar el monstruo
        if (jumpscareObject != null)
        {
            jumpscareObject.SetActive(true);
        }


        // Reproducir sonido
        if (jumpscareAudio != null)
        {
            jumpscareAudio.Play();
        }


        // Esperar el tiempo configurado
        yield return new WaitForSeconds(jumpscareDuration);


        // Ocultar nuevamente el monstruo
        if (jumpscareObject != null)
        {
            jumpscareObject.SetActive(false);
        }


        // Desactivamos el collider para asegurarnos
        // de que nunca vuelva a activarse
        BoxCollider triggerCollider =
            GetComponent<BoxCollider>();

        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }
    }
}
