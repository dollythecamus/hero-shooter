using UnityEngine;

namespace HeroShooter.Enemies
{
    /// <summary>
    /// Perception only: finds a target by tag, checks range, and checks line of sight.
    /// It keeps looking for a short grace period after losing sight, so an enemy does not forget the
    /// player the instant they step behind a crate.
    /// </summary>
    public class Spyglass : MonoBehaviour
    {
        [Tooltip("Tag searched for. The player prefab must carry it.")]
        [SerializeField] string targetTag = "Player";

        [SerializeField] float eyeHeight = 1.4f;

        [Tooltip("Layers that break line of sight (walls, cover). Leave the target's own layer out.")]
        [SerializeField] LayerMask sightBlockers;

        float _lastSeenTime = -999f;
        float _grace;

        public Transform Target { get; private set; }

        /// <summary>True while a target is inside range and either visible or within the grace window.</summary>
        public bool HasTarget => Target != null && Time.time - _lastSeenTime <= _grace;

        public float DistanceToTarget => Target == null ? float.PositiveInfinity : Vector3.Distance(transform.position, Target.position);

        public void Initialise(float sightRange, float loseSightGrace)
        {
            _grace = Mathf.Max(0.1f, loseSightGrace);

            if (Target == null)
            {
                GameObject found = GameObject.FindGameObjectWithTag(targetTag);
                if (found != null)
                {
                    Target = found.transform;
                }
            }

            if (Target != null && InRange(sightRange) && CanSee(Target))
            {
                _lastSeenTime = Time.time;
            }
        }

        public bool InRange(float range) => DistanceToTarget <= range;

        /// <summary>A single ray from eye height to the target's centre (or its chest, if it has one).</summary>
        public bool CanSee(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 point = target.position + Vector3.up * 1.0f;
            Vector3 direction = point - eye;

            if (Physics.Raycast(eye, direction.normalized, out RaycastHit hit, direction.magnitude,
                    sightBlockers, QueryTriggerInteraction.Ignore))
            {
                return hit.transform == target || hit.transform.IsChildOf(target);
            }

            return true;
        }

        /// <summary>Re-checks sight. Called once per frame by the brain; acquires a target if it has none yet.</summary>
        public void Refresh(float sightRange)
        {
            if (Target == null)
            {
                GameObject found = GameObject.FindGameObjectWithTag(targetTag);
                if (found != null)
                {
                    Target = found.transform;
                }
            }

            if (Target == null || !InRange(sightRange))
            {
                return;
            }

            if (CanSee(Target))
            {
                _lastSeenTime = Time.time;
            }
        }
    }
}
