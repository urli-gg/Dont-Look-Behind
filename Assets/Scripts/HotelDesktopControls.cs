using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class HotelDesktopControls : MonoBehaviour
{
    public Camera view;
    public CharacterController body;
    public Behaviour[] xrComponents;
    public float walkSpeed = 2.25f;

    private XRDisplaySubsystem display;
    private bool desktop;
    private float pitch;
    private HotelCarryable held;

    private void Start()
    {
        var displays = new System.Collections.Generic.List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        display = displays.Count > 0 ? displays[0] : null;
        desktop = display == null || !display.running;
        if (!desktop)
            return;

        foreach (var provider in xrComponents)
            provider.enabled = false;

        body.enabled = true;
        body.height = 1.65f;
        body.radius = 0.23f;
        body.center = Vector3.up * 0.83f;
        body.minMoveDistance = 0f;
        view.transform.localPosition = Vector3.up * (1.62f - view.transform.parent.localPosition.y);
        view.transform.localRotation = Quaternion.identity;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!desktop || Keyboard.current == null || Mouse.current == null)
            return;
        var keys = Keyboard.current;
        var mouse = Mouse.current;
        if (keys.escapeKey.wasPressedThisFrame)
            Cursor.lockState = CursorLockMode.None;
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (mouse.leftButton.wasPressedThisFrame)
                Cursor.lockState = CursorLockMode.Locked;
            return;
        }

        Vector2 delta = mouse.delta.ReadValue() * 0.09f;
        transform.Rotate(Vector3.up, delta.x);
        pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);
        view.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        float x = (keys.dKey.isPressed ? 1f : 0f) - (keys.aKey.isPressed ? 1f : 0f);
        float z = (keys.wKey.isPressed ? 1f : 0f) - (keys.sKey.isPressed ? 1f : 0f);
        Vector3 move = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f);
        body.Move((move * walkSpeed + Vector3.down * 3f) * Time.deltaTime);

        if (held != null)
        {
            held.DesktopHold(view.transform.position + view.transform.forward * 0.72f);
            if (mouse.rightButton.wasPressedThisFrame || keys.eKey.wasPressedThisFrame)
            {
                held.DesktopRelease(mouse.rightButton.wasPressedThisFrame ? view.transform.forward * 7f : Vector3.zero);
                held = null;
            }
            return;
        }

        if (!mouse.leftButton.wasPressedThisFrame && !keys.eKey.wasPressedThisFrame)
            return;
        if (!Physics.Raycast(view.transform.position, view.transform.forward, out var hit, 2.3f, ~0, QueryTriggerInteraction.Ignore))
            return;
        if (hit.collider.GetComponentInParent<HotelButton>() is { } button)
            button.Press();
        else if (hit.collider.GetComponentInParent<HotelCarryable>() is { } prop && prop.BeginDesktopHold())
            held = prop;
    }

    private void OnGUI()
    {
        if (desktop && !Application.isBatchMode && Cursor.lockState == CursorLockMode.Locked)
            GUI.Label(new Rect(Screen.width / 2f - 4f, Screen.height / 2f - 9f, 12f, 18f), "·");
    }

    private void OnDestroy()
    {
        if (desktop)
            Cursor.lockState = CursorLockMode.None;
    }
}
