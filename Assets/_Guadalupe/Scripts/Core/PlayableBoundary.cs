using UnityEngine;

namespace Guadalupe.Core
{
    /// <summary>
    /// Agrupa las tapias invisibles (Box Colliders hijos) que limitan la zona jugable y las dibuja en la vista
    /// de escena para poder colocarlas.
    /// </summary>
    public sealed class PlayableBoundary : MonoBehaviour
    {
        private static readonly Color GizmoColor = new Color(1f, 0.55f, 0.1f, 0.35f);

        private void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor;
            foreach (BoxCollider wall in GetComponentsInChildren<BoxCollider>())
            {
                Gizmos.matrix = wall.transform.localToWorldMatrix;
                Gizmos.DrawCube(wall.center, wall.size);
            }

            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
