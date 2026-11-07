namespace Climb.Core
{
    /// <summary>A snapshot. Velocity is ground-relative; Airspeed is horizontal air-relative speed.</summary>
    public readonly struct FlightState
    {
        public Vec3 Position { get; }
        public Vec3 Velocity { get; }
        public float HeadingRadians { get; }
        public float BankRadians { get; }
        public float TurnRateRadians { get; }
        public float Airspeed { get; }
        public bool IsGrounded { get; }
        public double SimulatedSeconds { get; }
        public double DiscardedSeconds { get; }

        internal FlightState(Vec3 position, Vec3 velocity, float heading, float bank, float turnRate,
            float airspeed, bool grounded, double simulatedSeconds, double discardedSeconds)
        {
            Position = position; Velocity = velocity; HeadingRadians = heading;
            BankRadians = bank; TurnRateRadians = turnRate; Airspeed = airspeed;
            IsGrounded = grounded; SimulatedSeconds = simulatedSeconds; DiscardedSeconds = discardedSeconds;
        }
    }
}
