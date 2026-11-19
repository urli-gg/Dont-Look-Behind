using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(-80)]
public class HotelNavigation : MonoBehaviour
{
    public NavMeshData layout;
    public BoxCollider[] staticCollision;
    private NavMeshDataInstance instance;

    private void OnEnable()
    {
        if (layout != null)
            instance = NavMesh.AddNavMeshData(layout);
    }

    private void OnDisable()
    {
        if (instance.valid)
            instance.Remove();
    }
}
