using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class ElevatorSequence : MonoBehaviour
{
    public Transform leftDoor;
    public Transform rightDoor;
    public GameObject menuCanvas;
    public GameObject creature;
    public AudioSource motorSound;
    public AudioSource dingSound;
    public AudioSource doorSound;
    public Light cabinLight;
    public TMP_Text floorDisplay;
    public TeleportationArea[] corridorTeleport;
    [Min(0.1f)] public float travelDuration = 4.5f;
    [Min(0.1f)] public float doorDuration = 2.1f;
    [Min(0.1f)] public float doorDistance = 0.86f;

    public bool DoorsOpen { get; private set; }
    public event System.Action Started;

    private bool started;

    private void Awake()
    {
        if (creature != null)
            creature.SetActive(false);
        SetCorridorTeleport(false);
    }

    public void StartElevator()
    {
        if (started || leftDoor == null || rightDoor == null)
            return;

        started = true;
        Started?.Invoke();
        StartCoroutine(Travel());
    }

    private IEnumerator Travel()
    {
        // Let XR finish the select event before unregistering the menu's interactables.
        yield return null;
        if (menuCanvas != null)
            menuCanvas.SetActive(false);
        if (motorSound != null)
            motorSound.Play();

        float baseIntensity = cabinLight != null ? cabinLight.intensity : 0f;
        for (float elapsed = 0f; elapsed < travelDuration; elapsed += Time.deltaTime)
        {
            float progress = elapsed / travelDuration;
            if (floorDisplay != null)
                floorDisplay.text = $"↓  {Mathf.Max(1, 4 - Mathf.FloorToInt(progress * 4f)):00}";
            if (cabinLight != null && !HotelOptions.ReducedScares)
                cabinLight.intensity = baseIntensity * (0.83f + 0.17f * Mathf.Sin(elapsed * 7f));
            yield return null;
        }

        if (cabinLight != null)
            cabinLight.intensity = baseIntensity;
        if (floorDisplay != null)
            floorDisplay.text = "01  ·  LLEGADA";
        if (motorSound != null)
            motorSound.Stop();
        if (dingSound != null)
            dingSound.Play();

        yield return new WaitForSeconds(0.35f);
        if (doorSound != null)
            doorSound.Play();

        Vector3 leftStart = leftDoor.localPosition;
        Vector3 rightStart = rightDoor.localPosition;
        for (float elapsed = 0f; elapsed < doorDuration; elapsed += Time.deltaTime)
        {
            MoveDoors(leftStart, rightStart, Mathf.SmoothStep(0f, 1f, elapsed / doorDuration));
            yield return null;
        }

        MoveDoors(leftStart, rightStart, 1f);
        DoorsOpen = true;
        SetCorridorTeleport(true);
    }

    private void MoveDoors(Vector3 leftStart, Vector3 rightStart, float progress)
    {
        leftDoor.localPosition = leftStart + Vector3.left * (doorDistance * progress);
        rightDoor.localPosition = rightStart + Vector3.right * (doorDistance * progress);
    }

    private void SetCorridorTeleport(bool allow)
    {
        if (corridorTeleport == null)
            return;
        foreach (var area in corridorTeleport)
            area.enabled = allow;
    }

    public void ActivateCreature()
    {
        if (DoorsOpen && creature != null)
            creature.SetActive(true);
    }
}
