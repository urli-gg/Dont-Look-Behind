using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class HotelSwingDoor : MonoBehaviour
{
    public BoxCollider leafCollider;
    public NavMeshObstacle obstacle;
    public AudioSource hingeSound;
    public float openAngle = 103f;
    public float speed = 105f;

    private readonly Collider[] nearby = new Collider[12];
    private bool moving;
    private bool open = true;

    public void Toggle()
    {
        if (!moving)
            StartCoroutine(Swing(speed));
    }

    public void CloseFromFright(float swingSpeed)
    {
        if (open && !moving && !Occupied())
            StartCoroutine(Swing(swingSpeed));
    }

    private IEnumerator Swing(float swingSpeed)
    {
        moving = true;
        float angle = transform.localEulerAngles.y;
        float end = open ? 0f : openAngle;
        if (hingeSound != null)
            hingeSound.Play();
        while (!Mathf.Approximately(angle, end))
        {
            if (open && Occupied())
            {
                end = openAngle;
                break;
            }
            angle = Mathf.MoveTowardsAngle(angle, end, swingSpeed * Time.deltaTime);
            transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            yield return null;
        }

        while (!Mathf.Approximately(angle, end))
        {
            angle = Mathf.MoveTowardsAngle(angle, end, swingSpeed * Time.deltaTime);
            transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            yield return null;
        }
        open = end != 0f;
        moving = false;
    }

    private bool Occupied()
    {
        if (leafCollider == null)
            return false;
        Vector3 center = leafCollider.transform.TransformPoint(leafCollider.center);
        Vector3 padding = new(0.20f, 0.03f, 0.58f);
        int count = Physics.OverlapBoxNonAlloc(center, leafCollider.size / 2f + padding, nearby,
            leafCollider.transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (nearby[i].transform.root.CompareTag("Player") || nearby[i].attachedRigidbody != null)
                return true;
        return false;
    }
}