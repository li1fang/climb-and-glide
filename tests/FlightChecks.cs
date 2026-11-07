using System;
using Climb.Core;

namespace Climb.Tests
{
    public static class FlightChecks
    {
        public static void Run()
        {
            NeutralFlight(); MirrorControls(); CollectiveBraking(); WindIsGroundRelative();
            InputBounds(); RenderCadence(); Landing(); ResetAndLongFrame(); BadConfiguration();
        }

        private static FlightModel Airborne(float height = 500f)
        {
            var model = new FlightModel();
            model.Reset(new Vec3(0f, height, 0f));
            return model;
        }

        private static void RunFor(FlightModel model, int count, float dt, float left, float right, Vec3 wind)
        { for (int i = 0; i < count; i++) model.Step(dt, left, right, wind); }

        private static void NeutralFlight()
        {
            var model = Airborne();
            RunFor(model, 500, 0.02f, 0f, 0f, Vec3.Zero);
            var s = model.State;
            Assert(s.Position.Z > 90f && s.Position.Y < 500f && s.Position.Y > 470f, "Neutral flight advances and descends.");
            Near(s.Position.X, 0f, 0.001f, "Neutral lateral drift");
            Near(s.HeadingRadians, 0f, 0.001f, "Neutral heading");
            Assert(s.Position.IsFinite && s.Velocity.IsFinite, "Finite neutral state.");
        }

        private static void MirrorControls()
        {
            var left = Airborne(); var right = Airborne();
            RunFor(left, 150, 0.02f, 0.7f, 0f, Vec3.Zero);
            RunFor(right, 150, 0.02f, 0f, 0.7f, Vec3.Zero);
            Assert(left.State.Position.X < -1f && right.State.Position.X > 1f, "Left and right brakes turn in their own direction.");
            Near(left.State.Position.X, -right.State.Position.X, 0.001f, "Mirrored lateral displacement");
            Near(left.State.Position.Y, right.State.Position.Y, 0.001f, "Mirrored height");
            Near(left.State.Position.Z, right.State.Position.Z, 0.001f, "Mirrored forward displacement");
            Near(left.State.BankRadians, -right.State.BankRadians, 0.001f, "Mirrored bank");
            float turningBank = right.State.BankRadians;
            RunFor(right, 300, 0.02f, 0f, 0f, Vec3.Zero);
            Assert(Math.Abs(right.State.BankRadians) < Math.Abs(turningBank) * 0.02f, "Released brakes recover bank toward neutral.");
            Assert(Math.Abs(right.State.TurnRateRadians) < 0.01f, "Released brakes eventually stop turning.");
        }

        private static void CollectiveBraking()
        {
            var neutral = Airborne(); var braked = Airborne();
            RunFor(neutral, 250, 0.02f, 0f, 0f, Vec3.Zero);
            RunFor(braked, 250, 0.02f, 0.6f, 0.6f, Vec3.Zero);
            Assert(braked.State.Airspeed < neutral.State.Airspeed - 1f, "Common braking changes speed, not only differential steering.");
            Near(braked.State.HeadingRadians, 0f, 0.001f, "Symmetric braking keeps heading");
        }

        private static void WindIsGroundRelative()
        {
            var calm = Airborne(); var windy = Airborne();
            RunFor(calm, 100, 0.02f, 0f, 0f, Vec3.Zero);
            RunFor(windy, 100, 0.02f, 0f, 0f, new Vec3(3f, 0f, 0f));
            Near(windy.State.Position.X - calm.State.Position.X, 6f, 0.02f, "Crosswind drift");
            Near(windy.State.Airspeed, calm.State.Airspeed, 0.001f, "Wind does not become brake input or airspeed");
            Near(windy.State.HeadingRadians, calm.State.HeadingRadians, 0.001f, "Crosswind keeps heading");
        }

        private static void InputBounds()
        {
            var a = Airborne(); var b = Airborne();
            RunFor(a, 100, 0.02f, 100f, -100f, Vec3.Zero);
            RunFor(b, 100, 0.02f, 1f, 0f, Vec3.Zero);
            Near((a.State.Position - b.State.Position).Length, 0f, 0.001f, "Out-of-range brakes clamp");
            a.Step(0.02f, float.NaN, float.PositiveInfinity, new Vec3(float.NaN, float.MaxValue, float.NegativeInfinity));
            Assert(a.State.Position.IsFinite && a.State.Velocity.IsFinite, "Invalid samples cannot poison flight state.");
        }

        private static void RenderCadence()
        {
            var slow = Airborne(); var fast = Airborne();
            RunFor(slow, 300, 1f / 30f, 0.1f, 0.4f, new Vec3(1f, 0f, 0f));
            RunFor(fast, 1200, 1f / 120f, 0.1f, 0.4f, new Vec3(1f, 0f, 0f));
            Near((slow.State.Position - fast.State.Position).Length, 0f, 0.15f, "30/120 fps held input trajectory");
            Near(slow.State.HeadingRadians, fast.State.HeadingRadians, 0.01f, "30/120 fps heading");
        }

        private static void Landing()
        {
            var model = Airborne(0.5f);
            RunFor(model, 100, 0.02f, 0f, 0f, Vec3.Zero);
            Assert(model.State.IsGrounded, "Flight intersects flat ground.");
            Near(model.State.Position.Y, 0f, 0.0001f, "Ground plane clamp");
            var hit = model.State.Position;
            RunFor(model, 50, 0.02f, 1f, 0f, new Vec3(10f, 10f, 10f));
            Near((model.State.Position - hit).Length, 0f, 0.0001f, "Grounded state cannot fly again without Reset");
            Near(model.State.Velocity.Length, 0f, 0.0001f, "Grounded velocity");
        }

        private static void ResetAndLongFrame()
        {
            var model = Airborne();
            model.Step(10f, 0f, 1f, Vec3.Zero);
            Assert(model.State.DiscardedSeconds > 9.7d && model.State.SimulatedSeconds <= 0.251d, "Long frame catch-up is bounded and disclosed.");
            model.Reset(new Vec3(0f, 100f, 0f), (float)Math.PI / 2f);
            model.Step(0.02f, 0f, 0f, Vec3.Zero);
            Assert(model.State.Position.X > 0f && Math.Abs(model.State.Position.Z) < 0.001f, "Reset heading convention.");
            Assert(model.State.DiscardedSeconds == 0d && !model.State.IsGrounded, "Reset clears previous run state.");
        }

        private static void BadConfiguration()
        {
            bool rejected = false;
            try { new FlightModel(new FlightConfig { MinimumAirspeed = 0f }); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Assert(rejected, "Invalid model configuration is rejected.");
        }

        private static void Assert(bool condition, string message)
        { if (!condition) throw new Exception("Flight: " + message); }
        private static void Near(float actual, float expected, float tolerance, string message)
        { Assert(!float.IsNaN(actual) && Math.Abs(actual - expected) <= tolerance, message + ": " + actual + " != " + expected); }
    }
}
