using UnityEngine;

public class DoorController : MonoBehaviour
{
    public float openHeight = 3f;
    public float openSpeed = 2f;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    private bool opening = false;

    void Start()
    {
        closedPosition = transform.localPosition;

        openPosition =
            closedPosition + Vector3.up * openHeight;
    }

    void Update()
    {
        if (opening)
        {
            transform.localPosition =
                Vector3.MoveTowards(
                    transform.localPosition,
                    openPosition,
                    openSpeed * Time.deltaTime
                );
        }
    }

    public void OpenDoor()
    {
        opening = true;
    }
}
