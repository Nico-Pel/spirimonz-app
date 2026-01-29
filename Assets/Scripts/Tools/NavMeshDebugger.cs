using UnityEngine;
using UnityEngine.AI;

[ExecuteAlways]
public class NavMeshDebugger : MonoBehaviour
{
    public bool showNavMesh = true; // la checkbox dans l'inspecteur

    void OnDrawGizmos()
    {
        if (!showNavMesh) return; // ne rien dessiner si décoché

        var triangulation = NavMesh.CalculateTriangulation();
        Gizmos.color = Color.cyan;

        for (int i = 0; i < triangulation.indices.Length; i += 3)
        {
            Vector3 a = triangulation.vertices[triangulation.indices[i]];
            Vector3 b = triangulation.vertices[triangulation.indices[i + 1]];
            Vector3 c = triangulation.vertices[triangulation.indices[i + 2]];

            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, a);
        }
    }
}