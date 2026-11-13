
using UnityEngine;

public class CorridorStartTrigger : MonoBehaviour
{
    public ElevatorSequence elevatorSequence;

    private bool activated;

    private void OnTriggerEnter(Collider other)
    {
        if (activated || elevatorSequence == null)
            return;

        // Detecta el XR Origin etiquetado como Player.
        if (other.transform.root.CompareTag("Player"))
        {
            activated = true;
            elevatorSequence.ActivateCreature();
        }
    }
}

