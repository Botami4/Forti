using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace Forti.Input
{
    public class GameInputManager
    {
        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public Vector2 MovementInput { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool JumpRequested { get; private set; }
        public bool DebugOverlayToggled { get; private set; }
        public bool PauseRequested { get; private set; }

        public float MouseDeltaX { get; private set; }
        public float MouseDeltaY { get; private set; }
        public float Sensitivity { get; set; } = 0.15f;
        public bool IsMouseCaptured { get; private set; } = false;

        public void UpdateKeyStates()
        {
            float x = 0f;
            float y = 0f;

            if (Keyboard.IsKeyDown(Key.W) || Keyboard.IsKeyDown(Key.Up)) y += 1f;
            if (Keyboard.IsKeyDown(Key.S) || Keyboard.IsKeyDown(Key.Down)) y -= 1f;
            if (Keyboard.IsKeyDown(Key.D) || Keyboard.IsKeyDown(Key.Right)) x += 1f;
            if (Keyboard.IsKeyDown(Key.A) || Keyboard.IsKeyDown(Key.Left)) x -= 1f;

            MovementInput = new Vector2(x, y);
            IsSprinting = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            JumpRequested = Keyboard.IsKeyDown(Key.Space);
        }

        public void HandleKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.F3)
            {
                DebugOverlayToggled = true;
            }
            else if (e.Key == Key.Escape)
            {
                PauseRequested = true;
            }
        }

        public bool ConsumeDebugOverlayToggle()
        {
            if (DebugOverlayToggled)
            {
                DebugOverlayToggled = false;
                return true;
            }
            return false;
        }

        public bool ConsumePauseRequest()
        {
            if (PauseRequested)
            {
                PauseRequested = false;
                return true;
            }
            return false;
        }

        public void CaptureMouse(Window window)
        {
            if (window == null) return;
            IsMouseCaptured = true;
            Mouse.OverrideCursor = Cursors.None;

            Point centerClient = new Point(window.ActualWidth * 0.5, window.ActualHeight * 0.5);
            if (window.IsLoaded && window.ActualWidth > 0 && window.ActualHeight > 0)
            {
                try
                {
                    Point centerScreen = window.PointToScreen(centerClient);
                    SetCursorPos((int)centerScreen.X, (int)centerScreen.Y);
                }
                catch
                {
                    // Window might not be fully presented yet
                }
            }
        }

        public void ReleaseMouse()
        {
            IsMouseCaptured = false;
            Mouse.OverrideCursor = null;
        }

        public void PollMouseLook(Window window)
        {
            if (!IsMouseCaptured || window == null || !window.IsActive)
            {
                MouseDeltaX = 0f;
                MouseDeltaY = 0f;
                return;
            }

            try
            {
                Point centerClient = new Point(window.ActualWidth * 0.5, window.ActualHeight * 0.5);
                Point centerScreen = window.PointToScreen(centerClient);

                if (GetCursorPos(out POINT currentPos))
                {
                    int dx = currentPos.X - (int)centerScreen.X;
                    int dy = currentPos.Y - (int)centerScreen.Y;

                    MouseDeltaX = dx * Sensitivity;
                    MouseDeltaY = dy * Sensitivity;

                    if (dx != 0 || dy != 0)
                    {
                        SetCursorPos((int)centerScreen.X, (int)centerScreen.Y);
                    }
                }
            }
            catch
            {
                MouseDeltaX = 0f;
                MouseDeltaY = 0f;
            }
        }
    }
}
