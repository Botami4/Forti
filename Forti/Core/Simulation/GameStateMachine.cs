using System;

namespace Forti.Core.Simulation
{
    public class GameStateMachine
    {
        public MatchState CurrentState { get; private set; } = MatchState.MainMenu;
        public MatchState StateBeforePause { get; private set; } = MatchState.MainMenu;
        public bool IsPaused { get; private set; } = false;

        public event Action<MatchState, MatchState>? StateChanged;
        public event Action<bool>? PauseChanged;

        public bool CanTransitionTo(MatchState target)
        {
            if (CurrentState == target) return false;

            return (CurrentState, target) switch
            {
                (MatchState.MainMenu, MatchState.InBus) => true,
                (MatchState.MainMenu, MatchState.OnGround) => true, // Shortcut for testing/skeleton
                (MatchState.InBus, MatchState.Freefall) => true,
                (MatchState.InBus, MatchState.OnGround) => true, // Instant drop stub
                (MatchState.Freefall, MatchState.Gliding) => true,
                (MatchState.Freefall, MatchState.OnGround) => true,
                (MatchState.Gliding, MatchState.OnGround) => true,
                (MatchState.OnGround, MatchState.Victory) => true,
                (MatchState.OnGround, MatchState.Defeat) => true,
                (_, MatchState.MainMenu) => true, // Reset to menu from any state
                _ => false
            };
        }

        public bool TryTransition(MatchState target)
        {
            if (!CanTransitionTo(target))
                return false;

            MatchState previous = CurrentState;
            CurrentState = target;
            StateChanged?.Invoke(previous, target);
            return true;
        }

        public void StartMatch(bool skipAirDrop = true)
        {
            if (CurrentState != MatchState.MainMenu) return;

            if (skipAirDrop)
            {
                // Playable skeleton direct ground drop
                TryTransition(MatchState.InBus);
                TryTransition(MatchState.OnGround);
            }
            else
            {
                TryTransition(MatchState.InBus);
            }
        }

        public void SetPaused(bool paused)
        {
            if (CurrentState == MatchState.MainMenu || CurrentState == MatchState.Victory || CurrentState == MatchState.Defeat)
            {
                return; // Can't pause in menu or end game
            }

            if (IsPaused == paused) return;

            IsPaused = paused;
            if (paused)
            {
                StateBeforePause = CurrentState;
            }

            PauseChanged?.Invoke(IsPaused);
        }

        public void TogglePause()
        {
            SetPaused(!IsPaused);
        }

        public void TriggerVictory()
        {
            if (CurrentState == MatchState.OnGround)
            {
                TryTransition(MatchState.Victory);
            }
        }

        public void TriggerDefeat()
        {
            if (CurrentState == MatchState.OnGround || CurrentState == MatchState.Freefall || CurrentState == MatchState.Gliding)
            {
                TryTransition(MatchState.Defeat);
            }
        }

        public void ReturnToMainMenu()
        {
            IsPaused = false;
            TryTransition(MatchState.MainMenu);
        }
    }
}
