using UnityEngine;

public class CorridorStartTrigger : MonoBehaviour
{
    public ElevatorSequence elevatorSequence;
    public Transform playerHead;
    public HorrorEventDirector horrorDirector;

    private void OnTriggerEnter(Collider other)
    {
        if (playerHead == null && other.transform.root.CompareTag("Player") && elevatorSequence != null)
            elevatorSequence.ActivateCreature();
    }

    private void Update()
    {
        // Also catches teleportation and room-scale steps that skip a narrow trigger.
        if (elevatorSequence == null || !elevatorSequence.DoorsOpen || playerHead == null)
            return;
        if (transform.InverseTransformPoint(playerHead.position).z < 0f)
            return;

        elevatorSequence.ActivateCreature();
        if (horrorDirector != null)
            horrorDirector.Begin();
        enabled = false;
    }
}
