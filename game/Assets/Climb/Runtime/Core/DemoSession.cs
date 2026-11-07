using System;

namespace Climb.Core
{
    public enum DemoMode { Climbing, PreparingFlight, Flying, Landed }

    // The interactive Unity scene and automated player both drive this session.
    // Inputs here are synthetic normalized travel, never hardware force commands.
    public sealed class DemoSession
    {
        public const float PlatformSpacing = 3f;
        public const float TowerHeight = 12f;
        private readonly LadderModel ladder;
        private readonly FlightModel flight;
        private float neutralSeconds;
        public float BaseHeight { get; }
        public DemoMode Mode { get; private set; }
        public bool Paused { get; private set; }
        public float LeftPull { get; private set; }
        public float RightPull { get; private set; }
        public float DisplayHeight { get; private set; }
        public float EarnedHeight => Math.Min(TowerHeight, ladder.HeightMeters);
        public FlightState Flight => flight.State;
        public Vec3 LastLaunch { get; private set; } = new Vec3(0, 35, 0);
        public bool AtPlatform => Mode == DemoMode.Climbing && EarnedHeight >= PlatformSpacing &&
            Math.Abs(DisplayHeight - EarnedHeight) < .01f &&
            Math.Abs(EarnedHeight / PlatformSpacing - Math.Round(EarnedHeight / PlatformSpacing)) < .01f;

        public DemoSession(FlightConfig flightConfig = null, LadderConfig ladderConfig = null, float baseHeight = 0, Func<float,float,float> terrain = null)
        {
            BaseHeight = baseHeight;
            ladder = new LadderModel(ladderConfig);
            flight = new FlightModel(flightConfig, terrain);
            ResetLadder();
        }

        public void ResetLadder()
        {
            ladder.Reset();
            flight.Reset(new Vec3(0,BaseHeight,0));
            Mode = DemoMode.Climbing;
            Paused = false;
            DisplayHeight = LeftPull = RightPull = neutralSeconds = 0;
        }

        public void Launch(Vec3 position)
        {
            flight.Reset(position);
            LastLaunch = position;
            Mode = flight.State.IsGrounded ? DemoMode.Landed : DemoMode.Flying;
            Paused = false;
            LeftPull = RightPull = neutralSeconds = 0;
        }

        public void SetPaused(bool paused)
        {
            Paused = paused;
            ladder.Invalidate();
        }

        public void Step(float dt, float leftTarget, float rightTarget, bool confirmPressed = false, bool valid = true)
        {
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0) throw new ArgumentOutOfRangeException(nameof(dt));
            // A missing source or large discontinuity pauses; it cannot earn a stroke.
            if (!valid || !Finite(leftTarget) || !Finite(rightTarget) || dt > .5f)
            { SetPaused(true); return; }
            if (Paused) return;
            dt = Math.Min(dt, .1f);
            LeftPull = Move(LeftPull, Clamp(leftTarget), dt * 1.5f);
            RightPull = Move(RightPull, Clamp(rightTarget), dt * 1.5f);
            if (Mode == DemoMode.Climbing)
            {
                if (confirmPressed && AtPlatform)
                {
                    Mode = DemoMode.PreparingFlight;
                    neutralSeconds = 0;
                    ladder.Invalidate();
                    return;
                }
                if (ladder.HeightMeters < TowerHeight)
                    ladder.Update(.4f + .5f * LeftPull, .4f + .5f * RightPull, dt, true);
                DisplayHeight = Move(DisplayHeight, EarnedHeight, dt * 2f);
            }
            else if (Mode == DemoMode.PreparingFlight)
            {
                neutralSeconds = LeftPull < .05f && RightPull < .05f ? neutralSeconds + dt : 0;
                if (neutralSeconds >= .35f) Launch(new Vec3(0, BaseHeight + DisplayHeight, 0));
            }
            else if (Mode == DemoMode.Flying)
            {
                flight.Step(dt, LeftPull, RightPull, Vec3.Zero);
                if (flight.State.IsGrounded) Mode = DemoMode.Landed;
            }
        }

        private static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
        private static float Clamp(float x) => Math.Max(0, Math.Min(1, x));
        private static float Move(float x, float target, float amount) =>
            x < target ? Math.Min(target, x + amount) : Math.Max(target, x - amount);
    }
}
