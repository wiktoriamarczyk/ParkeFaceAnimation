using UnityEngine;

public class SkinMeshToMeshCollider : MonoBehaviour
{
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private MeshCollider meshCollider;
    private void Awake()
    {
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        UpdateMeshCollider();
    }

    private void Update()
    {
        UpdateMeshCollider();
    }

    private void UpdateMeshCollider()
    {
        if (skinnedMeshRenderer == null || meshCollider == null)
        {
            return;
        }
        Mesh mesh = new Mesh();
        skinnedMeshRenderer.BakeMesh(mesh);
        meshCollider.sharedMesh = mesh;
    }
}
