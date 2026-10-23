using HeroShooter.Enemies.FSM;
using UnityEngine;
using UnityEngine.AI;

namespace HeroShooter.Enemies.States
{
    /// <summary>
    /// No target: wander around the spawn point on the NavMesh so the enemy reads as "looking for
    /// something" instead of standing still. After a few seconds without a target it picks a new spot.
    /// </summary>
    public class SearchState : IState
    {
        const float RepathInterval = 4f;
        const float WanderRadius = 9f;

        readonly EnemyBrain _brain;
        Vector3 _destination;
        float _nextRepathTime;

        public SearchState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void Enter()
        {
            PickNewPoint();
        }

        public void Tick(float deltaTime)
        {
            if (Time.time >= _nextRepathTime || Arrived())
            {
                PickNewPoint();
            }

            _brain.MoveTo(_destination, _brain.Stats.searchSpeed);
        }

        public void Exit()
        {
        }

        void PickNewPoint()
        {
            _nextRepathTime = Time.time + RepathInterval;

            Vector3 candidate = _brain.SpawnPoint + new Vector3(Random.Range(-WanderRadius, WanderRadius), 0f,
                Random.Range(-WanderRadius, WanderRadius));

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                _destination = hit.position;
            }
        }

        bool Arrived()
        {
            return !_brain.Agent.pathPending && _brain.Agent.remainingDistance <= _brain.Agent.stoppingDistance + 0.3f;
        }
    }
}
