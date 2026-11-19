using System;
using System.Diagnostics;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Forti.Core.Simulation;
using Forti.Input;
using Forti.Rendering;

namespace Forti
{
    public partial class MainWindow : Window
    {
        private readonly SimulationWorld _simulation = new SimulationWorld();
        private readonly GameInputManager _input = new GameInputManager();
        private SceneRenderer? _renderer;

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private long _lastTicks;

        // FPS calculation
        private double _fpsAccumulator = 0.0;
        private int _fpsFrames = 0;
        private double _currentFps = 60.0;
        private double _currentFrameMs = 16.6;

        public bool IsBenchmarkMode { get; set; } = false;
        private int _benchmarkFrames = 0;
        private double _benchmarkTotalSeconds = 0.0;
        private double _benchmarkTotalWorkMs = 0.0;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            KeyDown += OnKeyDown;

            _simulation.StateMachine.StateChanged += OnStateChanged;
            _simulation.StateMachine.PauseChanged += OnPauseChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Initialize 3D renderer
            _renderer = new SceneRenderer(MainViewport);
            _renderer.BindBots(_simulation.Bots);

            // Sync sensitivity
            _input.Sensitivity = 0.15f;
            MenuSensitivitySlider.Value = 0.15;
            PauseSensitivitySlider.Value = 0.15;

            // Start game loop
            _stopwatch.Start();
            _lastTicks = _stopwatch.ElapsedTicks;
            CompositionTarget.Rendering += OnGameLoop;

            // Ensure we start in MainMenu
            UpdateScreenVisibility();

            if (IsBenchmarkMode)
            {
                _simulation.ResetWorld();
                _simulation.StateMachine.StartMatch(skipAirDrop: true);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= OnGameLoop;
            _stopwatch.Stop();
            _input.ReleaseMouse();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            _input.HandleKeyDown(e);

            if (_input.ConsumeDebugOverlayToggle())
            {
                DebugView.Visibility = (DebugView.Visibility == Visibility.Visible)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }

            if (_input.ConsumePauseRequest())
            {
                if (_simulation.StateMachine.CurrentState == MatchState.OnGround ||
                    _simulation.StateMachine.CurrentState == MatchState.Freefall ||
                    _simulation.StateMachine.CurrentState == MatchState.Gliding)
                {
                    _simulation.StateMachine.TogglePause();
                }
            }
        }

        private void OnStateChanged(MatchState previous, MatchState current)
        {
            Dispatcher.Invoke(UpdateScreenVisibility);
        }

        private void OnPauseChanged(bool isPaused)
        {
            Dispatcher.Invoke(UpdateScreenVisibility);
        }

        private void UpdateScreenVisibility()
        {
            var state = _simulation.StateMachine.CurrentState;
            bool isPaused = _simulation.StateMachine.IsPaused;

            if (state == MatchState.MainMenu)
            {
                _input.ReleaseMouse();
                MainMenuScreen.Visibility = Visibility.Visible;
                PauseScreen.Visibility = Visibility.Collapsed;
                ResultsScreen.Visibility = Visibility.Collapsed;
                HudLayer.Visibility = Visibility.Collapsed;
            }
            else if (isPaused)
            {
                _input.ReleaseMouse();
                MainMenuScreen.Visibility = Visibility.Collapsed;
                PauseScreen.Visibility = Visibility.Visible;
                ResultsScreen.Visibility = Visibility.Collapsed;
                HudLayer.Visibility = Visibility.Visible;
            }
            else if (state == MatchState.Victory || state == MatchState.Defeat)
            {
                _input.ReleaseMouse();
                MainMenuScreen.Visibility = Visibility.Collapsed;
                PauseScreen.Visibility = Visibility.Collapsed;
                ResultsScreen.Visibility = Visibility.Visible;
                HudLayer.Visibility = Visibility.Collapsed;

                if (state == MatchState.Victory)
                {
                    ResultsTitleText.Text = "VICTORY ROYALE";
                    ResultsTitleText.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
                    ResultsSubText.Text = "You are the last champion standing!";
                }
                else
                {
                    ResultsTitleText.Text = "ELIMINATED";
                    ResultsTitleText.Foreground = new SolidColorBrush(Color.FromRgb(255, 75, 75));
                    ResultsSubText.Text = "Better luck on the next drop!";
                }
            }
            else
            {
                // In Match Gameplay
                _input.CaptureMouse(this);
                MainMenuScreen.Visibility = Visibility.Collapsed;
                PauseScreen.Visibility = Visibility.Collapsed;
                ResultsScreen.Visibility = Visibility.Collapsed;
                HudLayer.Visibility = Visibility.Visible;
            }
        }

        private void OnGameLoop(object? sender, EventArgs e)
        {
            long currentTicks = _stopwatch.ElapsedTicks;
            double elapsedSeconds = (double)(currentTicks - _lastTicks) / Stopwatch.Frequency;
            _lastTicks = currentTicks;

            // Enforce clamped delta time: max 0.05s
            float dt = MathF.Min((float)elapsedSeconds, SimulationWorld.MaxDeltaTime);

            // FPS accumulator
            _fpsFrames++;
            _fpsAccumulator += elapsedSeconds;
            if (_fpsAccumulator >= 0.5)
            {
                _currentFps = _fpsFrames / _fpsAccumulator;
                _currentFrameMs = (_fpsAccumulator / _fpsFrames) * 1000.0;
                _fpsFrames = 0;
                _fpsAccumulator = 0.0;
            }

            // Only process gameplay inputs when in match and not paused
            var state = _simulation.StateMachine.CurrentState;
            bool isPlaying = (state == MatchState.OnGround || state == MatchState.InBus || state == MatchState.Freefall || state == MatchState.Gliding) && !_simulation.StateMachine.IsPaused;

            if (isPlaying)
            {
                // Poll mouse look
                _input.PollMouseLook(this);
                _simulation.Player.Yaw += _input.MouseDeltaX;
                _simulation.Player.Pitch = Math.Clamp(_simulation.Player.Pitch - _input.MouseDeltaY, -45f, 75f);

                // Movement keys
                _input.UpdateKeyStates();
                _simulation.Player.ApplyMovementInput(_input.MovementInput, _input.IsSprinting, dt);

                // Jump
                if (_input.JumpRequested && _simulation.Player.CharacterState.IsGrounded)
                {
                    _simulation.Player.Velocity = new Vector3(
                        _simulation.Player.Velocity.X,
                        7.5f,
                        _simulation.Player.Velocity.Z
                    );
                    _simulation.Player.CharacterState.IsGrounded = false;
                }
            }

            // Step pure simulation (handles its own pause/menu guards)
            var workSw = Stopwatch.StartNew();
            _simulation.Step(dt);

            // Render scene
            if (_renderer != null)
            {
                _renderer.Render(_simulation);
            }
            workSw.Stop();
            double frameWorkMs = workSw.Elapsed.TotalMilliseconds;

            if (IsBenchmarkMode)
            {
                _simulation.Player.ApplyMovementInput(new Vector2(0f, 1f), false, dt);
                _benchmarkFrames++;
                _benchmarkTotalSeconds += elapsedSeconds;
                _benchmarkTotalWorkMs += frameWorkMs;

                if (_benchmarkFrames >= 60)
                {
                    double avgVsyncFps = _benchmarkFrames / _benchmarkTotalSeconds;
                    double avgWorkMs = _benchmarkTotalWorkMs / _benchmarkFrames;
                    double potentialFps = 1000.0 / Math.Max(avgWorkMs, 0.001);

                    Console.WriteLine($"[BENCHMARK] Active Render Work: {avgWorkMs:F3} ms/frame (CPU capacity: {potentialFps:F0} FPS)");
                    Console.WriteLine($"[BENCHMARK] Active Presentation: {avgVsyncFps:F1} FPS ({(_benchmarkTotalSeconds / _benchmarkFrames) * 1000.0:F2} ms/frame)");
                    Application.Current.Shutdown(0);
                    return;
                }
            }

            if (_renderer != null)
            {
                if (isPlaying)
                {
                    UpdateHud();
                }

                if (DebugView.Visibility == Visibility.Visible)
                {
                    DebugView.UpdateMetrics(
                        _currentFps,
                        frameWorkMs,
                        _simulation,
                        _renderer.VisibleEntityCount,
                        _renderer.VisibleChunkCount
                    );
                }
            }
        }

        private void UpdateHud()
        {
            var charState = _simulation.Player.CharacterState;
            HudHealthBar.Value = charState.Health;
            HudHealthText.Text = $"HEALTH {(int)charState.Health}";

            HudShieldBar.Value = charState.Shield;
            HudShieldText.Text = $"SHIELD {(int)charState.Shield}";

            HudAliveText.Text = _simulation.AliveParticipants.ToString();

            if (_simulation.Zone.IsShrinking)
            {
                HudStormText.Text = $"Shrinking ({_simulation.Zone.CurrentRadius:F0}m)";
                HudStormText.Foreground = new SolidColorBrush(Color.FromRgb(255, 75, 75));
            }
            else
            {
                HudStormText.Text = $"Waiting ({_simulation.Zone.PhaseWaitTimer:F0}s)";
                HudStormText.Foreground = new SolidColorBrush(Color.FromRgb(208, 112, 255));
            }
        }

        // --- UI Screen Button Handlers ---

        private void OnStartBattleClick(object sender, RoutedEventArgs e)
        {
            _simulation.ResetWorld();
            _simulation.StateMachine.StartMatch(skipAirDrop: true);
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = (SettingsPanel.Visibility == Visibility.Visible)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void OnQuitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OnResumeClick(object sender, RoutedEventArgs e)
        {
            _simulation.StateMachine.SetPaused(false);
        }

        private void OnReturnToMenuClick(object sender, RoutedEventArgs e)
        {
            _simulation.StateMachine.ReturnToMainMenu();
        }

        private void OnPlayAgainClick(object sender, RoutedEventArgs e)
        {
            _simulation.ResetWorld();
            _simulation.StateMachine.StartMatch(skipAirDrop: true);
        }

        private void OnSensitivityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float val = (float)e.NewValue;
            _input.Sensitivity = val;

            if (MenuSensitivityLabel != null) MenuSensitivityLabel.Text = val.ToString("F2");
            if (PauseSensitivityLabel != null) PauseSensitivityLabel.Text = val.ToString("F2");

            if (sender == MenuSensitivitySlider && PauseSensitivitySlider != null)
                PauseSensitivitySlider.Value = val;
            else if (sender == PauseSensitivitySlider && MenuSensitivitySlider != null)
                MenuSensitivitySlider.Value = val;
        }
    }
}
