using System;

namespace Climb.Core
{
    /// <summary>
    /// Small normal-flight game model. Brakes set target bank and airspeed, not heading directly.
    /// Positive heading / bank turn right. Wind is a world-space air-mass velocity.
    /// No real line-tension calculation, stalls, turbulence or human weight-shift simulation.
    /// </summary>
    public sealed class FlightModel
    {
        private const float Gravity = 9.81f;
        private readonly FlightConfig config;
        private readonly Func<float,float,float> terrain;
        private Vec3 position;
        private Vec3 velocity;
        private float heading;
        private float bank;
        private float turnRate;
        private float airspeed;
        private bool grounded;
        private double accumulator;
        private double simulatedSeconds;
        private double discardedSeconds;

        public FlightModel(FlightConfig config = null, Func<float,float,float> terrain = null)
        {
            this.config = (config ?? new FlightConfig()).CopyAndValidate();
            this.terrain = terrain;
            Reset(new Vec3(0f, this.config.GroundY, 0f), 0f);
        }

        public FlightState State => new FlightState(position, velocity, heading, bank, turnRate,
            airspeed, grounded, simulatedSeconds, discardedSeconds);

        public void Reset(Vec3 position, float headingRadians = 0f)
        {
            if (!position.IsFinite || Math.Abs(position.X) > 1000000f ||
                Math.Abs(position.Y) > 1000000f || Math.Abs(position.Z) > 1000000f)
                throw new ArgumentOutOfRangeException(nameof(position));
            if (!Finite(headingRadians)) throw new ArgumentOutOfRangeException(nameof(headingRadians));
            float floor = Ground(position.X, position.Z);
            this.position = new Vec3(position.X, Math.Max(position.Y, floor), position.Z);
            heading = WrapAngle(headingRadians);
            bank = turnRate = 0f;
            airspeed = config.TrimAirspeed;
            grounded = position.Y <= floor;
            velocity = grounded ? Vec3.Zero : AirVelocity(airspeed, heading, config.BaseSinkSpeed);
            accumulator = simulatedSeconds = discardedSeconds = 0d;
        }

        /// <summary>
        /// Advance with held inputs. Internally uses fixed substeps; excess frame time is explicitly
        /// counted in DiscardedSeconds so a pause cannot create an unbounded catch-up movement.
        /// Non-finite brake values become neutral; non-finite wind components become zero.
        /// </summary>
        public void Step(float dt, float leftBrake, float rightBrake, Vec3 wind)
        {
            if (!Finite(dt) || dt < 0f) throw new ArgumentOutOfRangeException(nameof(dt));
            if (dt == 0f) return;
            float accepted = Math.Min(dt, config.MaximumFrameDeltaSeconds);
            discardedSeconds += (double)dt - accepted;
            accumulator += accepted;
            float left = Brake(leftBrake);
            float right = Brake(rightBrake);
            Vec3 boundedWind = BoundWind(wind);
            while (accumulator + 1e-9 >= config.FixedStepSeconds)
            {
                Tick(config.FixedStepSeconds, left, right, boundedWind);
                accumulator -= config.FixedStepSeconds;
                if (accumulator < 0d) accumulator = 0d;
                simulatedSeconds += config.FixedStepSeconds;
            }
        }

        private void Tick(float dt, float left, float right, Vec3 wind)
        {
            if (grounded) return;
            float collective = (left + right) * 0.5f;
            float targetSpeed = config.TrimAirspeed -
                (config.TrimAirspeed - config.MinimumAirspeed) * collective;
            airspeed = Approach(airspeed, targetSpeed, dt, config.AirspeedResponseSeconds);
            bank = Approach(bank, (right - left) * config.MaximumBankRadians, dt, config.BankResponseSeconds);

            float targetTurn = Gravity * (float)Math.Tan(bank) / airspeed;
            targetTurn = Clamp(targetTurn, -config.MaximumTurnRateRadians, config.MaximumTurnRateRadians);
            turnRate = Approach(turnRate, targetTurn, dt, config.TurnResponseSeconds);
            heading = WrapAngle(heading + turnRate * dt);

            float sink = config.BaseSinkSpeed + config.DeepBrakeExtraSink * collective * collective +
                config.BankExtraSink * (1f / (float)Math.Cos(bank) - 1f);
            velocity = AirVelocity(airspeed, heading, sink) + wind;
            Vec3 nextPosition = position + velocity * dt;
            if (nextPosition.Y <= Ground(nextPosition.X, nextPosition.Z))
            {
                // Intersect the flat ground within this substep instead of overshooting horizontally.
                float low=0, high=1;
                for(int i=0;i<18;i++) {
                    float mid=(low+high)*.5f; Vec3 sample=position+(nextPosition-position)*mid;
                    if(sample.Y>Ground(sample.X,sample.Z)) low=mid; else high=mid;
                }
                Vec3 hit = position + (nextPosition - position) * high;
                position = new Vec3(hit.X, Ground(hit.X,hit.Z), hit.Z);
                velocity = Vec3.Zero;
                airspeed = bank = turnRate = 0f;
                grounded = true;
            }
            else position = nextPosition;
        }

        private Vec3 BoundWind(Vec3 value)
        {
            // Clamp components before taking length; very large finite input must not overflow it.
            float maximum = config.MaximumWindSpeed;
            var bounded = new Vec3(
                Finite(value.X) ? Clamp(value.X, -maximum, maximum) : 0f,
                Finite(value.Y) ? Clamp(value.Y, -maximum, maximum) : 0f,
                Finite(value.Z) ? Clamp(value.Z, -maximum, maximum) : 0f);
            float length = bounded.Length;
            return length > maximum && length > 0f ? bounded * (maximum / length) : bounded;
        }

        private float Ground(float x,float z) => terrain == null ? config.GroundY : terrain(x,z);

        private static Vec3 AirVelocity(float speed, float angle, float sink) =>
            new Vec3((float)Math.Sin(angle) * speed, -sink, (float)Math.Cos(angle) * speed);
        private static float Approach(float value, float target, float dt, float response) =>
            target + (value - target) * (float)Math.Exp(-dt / response);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float Brake(float value) => Finite(value) ? Clamp(value, 0f, 1f) : 0f;
        private static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        private static float WrapAngle(float value)
        {
            double tau = Math.PI * 2d;
            double result = ((double)value + Math.PI) % tau;
            if (result < 0d) result += tau;
            return (float)(result - Math.PI);
        }
    }
}
