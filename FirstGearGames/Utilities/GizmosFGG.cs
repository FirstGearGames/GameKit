
using UnityEngine;

namespace OldFartGames.Utilities
{

    public static class GizmosFGG
    {

        public static void DrawDoubleWireSphere(Vector3 origin, float radius, Color innerColor, Color outerColor, float outerRadiusMultiplier = 1.15f)
        {
            Gizmos.color = innerColor;
            Gizmos.DrawWireSphere(origin, radius);
            Gizmos.color = outerColor;
            Gizmos.DrawWireSphere(origin, radius * outerRadiusMultiplier);
        }
    }

}
