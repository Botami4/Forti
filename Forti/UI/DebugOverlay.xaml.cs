using System;
using System.Windows.Controls;
using Forti.Core.Simulation;

namespace Forti.UI
{
    public partial class DebugOverlay : UserControl
    {
        public DebugOverlay()
        {
            InitializeComponent();
        }

        public void UpdateMetrics(double fps, double frameMs, SimulationWorld sim, int visibleEntities, int visibleChunks)
        {
            TxtFps.Text = $"{fps:F1} FPS ({frameMs:F1} ms)";
            TxtGameState.Text = sim.StateMachine.IsPaused 
                ? $"{sim.StateMachine.CurrentState} (PAUSED)" 
                : sim.StateMachine.CurrentState.ToString();
            TxtEntities.Text = $"{visibleEntities} / {sim.TotalParticipants} visible";
            TxtChunks.Text = visibleChunks.ToString();
            TxtPlayerPos.Text = $"X: {sim.Player.Position.X:F1}, Y: {sim.Player.Position.Y:F1}, Z: {sim.Player.Position.Z:F1}";

            string stormStatus = sim.Zone.IsShrinking ? "Shrinking" : $"Wait {sim.Zone.PhaseWaitTimer:F0}s";
            TxtStorm.Text = $"R: {sim.Zone.CurrentRadius:F0}m ({stormStatus})";
            TxtAlive.Text = $"{sim.AliveParticipants} / {sim.TotalParticipants}";
        }
    }
}
