using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroShooter.Enemies.FSM
{
    /// <summary>
    /// The machine itself: a dictionary of states keyed by <see cref="EnemyState"/>, one current state,
    /// and a transition event. It knows nothing about enemies, so ranged and melee share it unchanged.
    /// </summary>
    public class StateMachine
    {
        readonly Dictionary<EnemyState, IState> _states = new Dictionary<EnemyState, IState>();

        public EnemyState Current { get; private set; } = EnemyState.None;
        public float TimeInState { get; private set; }
        public bool HasState(EnemyState key) => _states.ContainsKey(key);

        /// <summary>Raised after a transition, with (from, to).</summary>
        public event Action<EnemyState, EnemyState> Transitioned;

        public void Register(EnemyState key, IState state)
        {
            _states[key] = state;
        }

        public void Tick(float deltaTime)
        {
            TimeInState += deltaTime;

            IState state;
            if (_states.TryGetValue(Current, out state))
            {
                state.Tick(deltaTime);
            }
        }

        public void ChangeState(EnemyState next)
        {
            if (next == Current)
            {
                return;
            }

            IState leaving;
            if (_states.TryGetValue(Current, out leaving))
            {
                leaving.Exit();
            }

            EnemyState previous = Current;
            Current = next;
            TimeInState = 0f;

            IState entering;
            if (_states.TryGetValue(next, out entering))
            {
                entering.Enter();
            }

            Transitioned?.Invoke(previous, next);
        }
    }
}
