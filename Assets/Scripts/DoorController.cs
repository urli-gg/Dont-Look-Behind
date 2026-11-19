using UnityEngine;

public class DoorController : MonoBehaviour
{
    public float openHeight = 3f;
    public float openSpeed = 2f;
    public GameObject winTrigger;
    public AudioSource motor;

    private Vector3 openPosition;
    private bool opening;
    public bool IsOpen { get; private set; }

    private void Awake()
    {
        openPosition = transform.localPosition + Vector3.up * openHeight;
        if (winTrigger != null)
            winTrigger.SetActive(false);
    }

    private void Update()
    {
        if (!opening)
            return;
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, openPosition, openSpeed * Time.deltaTime);
        if (transform.localPosition != openPosition)
            return;

        opening = false;
        IsOpen = true;
        if (motor != null)
            motor.Stop();
        if (winTrigger != null)
            winTrigger.SetActive(true);
    }

    public void OpenDoor()
    {
        if (opening || IsOpen)
            return;
        opening = true;
        if (motor != null)
            motor.Play();
    }
}