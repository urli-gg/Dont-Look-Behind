using UnityEngine;

[RequireComponent(typeof(HotelCarryable))]
public class BreakableObject : MonoBehaviour
{
    public GameObject fracturePrefab;
    public AudioClip[] sounds;
    [Min(0.1f)] public float impactSpeed = 3.15f;
    [Min(0f)] public float hearingRadius = 8f;
    [Min(1f)] public float fragmentLifetime = 7f;

    private HotelCarryable carryable;
    private bool broken;

    private void Awake()
    {
        carryable = GetComponent<HotelCarryable>();
        int debris = LayerMask.NameToLayer("Hotel Debris");
        if (debris >= 0)
            Physics.IgnoreLayerCollision(debris, debris);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (carryable.Held || collision.contactCount == 0)
            return;
        for (int index = 0; index < collision.contactCount; index++)
        {
            ContactPoint contact = collision.GetContact(index);
            if (Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contact.normal)) < impactSpeed)
                continue;
            Break(contact.point, collision.relativeVelocity);
            return;
        }
    }

    public void Break(Vector3 point, Vector3 impact)
    {
        if (broken || fracturePrefab == null)
            return;
        broken = true;
        GameObject debris = Instantiate(fracturePrefab, transform.position, transform.rotation);
        if (debris.TryGetComponent(out AudioSource shatter) && sounds != null && sounds.Length > 0)
        {
            shatter.pitch = Random.Range(0.92f, 1.08f);
            shatter.PlayOneShot(sounds[Random.Range(0, sounds.Length)]);
        }
        foreach (var fragment in debris.GetComponentsInChildren<Rigidbody>())
        {
            Vector3 outward = (fragment.worldCenterOfMass - point).normalized;
            fragment.AddForce(Vector3.ClampMagnitude(impact, 5f) * 0.22f + outward * 1.15f + Vector3.up * 0.42f, ForceMode.VelocityChange);
            fragment.AddTorque(Random.insideUnitSphere * 3.5f, ForceMode.VelocityChange);
        }

        HotelNoise.Emit(point, hearingRadius);
        Destroy(debris, fragmentLifetime);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

}
