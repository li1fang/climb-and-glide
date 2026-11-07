using System;

namespace Climb.Core
{
    /// <summary>
    /// Tunable normal-flight demonstration parameters, not a calibrated paraglider specification.
    /// Units are metres, seconds and radians. The model does not implement stalls or canopy deformation.
    /// </summary>
    [Serializable]
    public sealed class FlightConfig
    {
        public float FixedStepSeconds = 0.01f;
        public float MaximumFrameDeltaSeconds = 0.25f;
        public float TrimAirspeed = 10f;
        public float MinimumAirspeed = 5f;
        public float AirspeedResponseSeconds = 1.4f;
        public float BaseSinkSpeed = 1.25f;
        public float DeepBrakeExtraSink = 2.1f;
        public float BankExtraSink = 3f;
        public float MaximumBankRadians = 0.65f;
        public float BankResponseSeconds = 0.8f;
        public float MaximumTurnRateRadians = 1.1f;
        public float TurnResponseSeconds = 0.45f;
        public float MaximumWindSpeed = 30f;
        public float GroundY = 0f;

        internal FlightConfig CopyAndValidate()
        {
            var copy = (FlightConfig)MemberwiseClone();
            Range(copy.FixedStepSeconds, 0.001f, 0.05f, nameof(FixedStepSeconds));
            Range(copy.MaximumFrameDeltaSeconds, copy.FixedStepSeconds, 1f, nameof(MaximumFrameDeltaSeconds));
            Range(copy.TrimAirspeed, 1f, 50f, nameof(TrimAirspeed));
            Range(copy.MinimumAirspeed, 1f, copy.TrimAirspeed, nameof(MinimumAirspeed));
            Range(copy.AirspeedResponseSeconds, 0.01f, 20f, nameof(AirspeedResponseSeconds));
            Range(copy.BaseSinkSpeed, 0.01f, 20f, nameof(BaseSinkSpeed));
            Range(copy.DeepBrakeExtraSink, 0f, 20f, nameof(DeepBrakeExtraSink));
            Range(copy.BankExtraSink, 0f, 20f, nameof(BankExtraSink));
            Range(copy.MaximumBankRadians, 0.01f, 1.2f, nameof(MaximumBankRadians));
            Range(copy.BankResponseSeconds, 0.01f, 20f, nameof(BankResponseSeconds));
            Range(copy.MaximumTurnRateRadians, 0.01f, 3f, nameof(MaximumTurnRateRadians));
            Range(copy.TurnResponseSeconds, 0.01f, 20f, nameof(TurnResponseSeconds));
            Range(copy.MaximumWindSpeed, 0f, 100f, nameof(MaximumWindSpeed));
            Range(copy.GroundY, -10000f, 10000f, nameof(GroundY));
            return copy;
        }

        private static void Range(float value, float minimum, float maximum, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < minimum || value > maximum)
                throw new ArgumentOutOfRangeException(name, "Value is outside the demonstration model's supported range.");
        }
    }
}
