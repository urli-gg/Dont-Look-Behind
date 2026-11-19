using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class HotelCarryable : MonoBehaviour
{
    public AudioSource handlingSound;
    public Rigidbody Body { get; private set; }
    public bool Held => desktopHeld || grab.isSelected;

    private XRGrabInteractable grab;
    private bool desktopHeld;

    private void Awake()
    {
        Body = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable() => grab.selectEntered.AddListener(HandleGrab);
    private void OnDisable() => grab.selectEntered.RemoveListener(HandleGrab);
    private void HandleGrab(SelectEnterEventArgs args) => PlayHandling();

    public bool BeginDesktopHold()
    {
        if (Held)
            return false;
        desktopHeld = true;
        Body.isKinematic = true;
        PlayHandling();
        return true;
    }

    public void DesktopHold(Vector3 position)
    {
        Vector3 displacement = position - Body.position;
        float distance = displacement.magnitude;
        if (distance > 0.001f && Body.SweepTest(displacement / distance, out var hit, distance, QueryTriggerInteraction.Ignore))
            position = Body.position + displacement / distance * Mathf.Max(0f, hit.distance - 0.035f);
        Body.MovePosition(position);
    }

    public void DesktopRelease(Vector3 velocity)
    {
        desktopHeld = false;
        Body.isKinematic = false;
        Body.linearVelocity = velocity;
    }

    private void PlayHandling()
    {
        if (handlingSound != null)
            handlingSound.Play();
    }
}