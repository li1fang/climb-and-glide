using System;

namespace Climb.Core
{
    /// <summary>
    /// Game-side stroke thresholds. These defaults are placeholders for calibration,
    /// not measurements of grip, muscle effort, or PUMP force.
    /// </summary>
    [Serializable]
    public sealed class LadderConfig
    {
        public float PullDistanceMeters = 0.05f;
        public float RearmDistanceMeters = 0.03f;
        public float HeightPerStrokeMeters = 0.25f;
        public float MaxSampleGapSeconds = 0.5f;

        internal void Validate()
        {
            RequirePositiveFinite(PullDistanceMeters, nameof(PullDistanceMeters));
            RequirePositiveFinite(RearmDistanceMeters, nameof(RearmDistanceMeters));
            RequirePositiveFinite(HeightPerStrokeMeters, nameof(HeightPerStrokeMeters));
            RequirePositiveFinite(MaxSampleGapSeconds, nameof(MaxSampleGapSeconds));
        }

        private static void RequirePositiveFinite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(name, "Expected a positive finite value.");
        }
    }
}
