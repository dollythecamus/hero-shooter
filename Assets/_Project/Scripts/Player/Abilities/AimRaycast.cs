using UnityEngine;

namespace HeroShooter.Player.Abilities
{
    /// <summary>
    /// Resolves "whatever the crosshair is over". One place, so Mark and Snare cannot disagree about what
    /// the player is aiming at, and so line of sight is enforced the same way for both.
    /// </summary>
    public static class AimRaycast
    {
        public static Transform FindTarget(AbilityContext context, float range)
        {
            if (context.CameraTransform == null)
            {
                return null;
            }

            Ray ray = new Ray(context.CameraTransform.position, context.CameraTransform.forward);

            if (!Physics.Raycast(ray, out RaycastHit hit, range, context.EnemyMask | context.WorldMask,
                    QueryTriggerInteraction.Ignore))
            {
                return null;
            }

            // If a wall was closer than the enemy, the raycast returns the wall and there is no target.
            if (((1 << hit.collider.gameObject.layer) & context.EnemyMask) == 0)
            {
                return null;
            }

            return hit.collider.transform;
        }
    }
}
