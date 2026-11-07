namespace Climb.Core
{
    // Trial-only readiness: do not spend the gameplay clock on loading/render warmup.
    public sealed class TrialStartupGate
    {
        public const float MaximumStableFrame = .1f;
        private float _elapsed, _stableSeconds;
        private int _stableFrames;
        public int Interruptions { get; private set; }
        public bool Observe(float dt)
        {
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0) {
                _stableSeconds = 0; _stableFrames = 0; Interruptions++; return false;
            }
            _elapsed += dt;
            if (dt > MaximumStableFrame) {
                _stableSeconds = 0; _stableFrames = 0; Interruptions++; return false;
            }
            _stableSeconds += dt; _stableFrames++;
            return _elapsed >= 3f && _stableSeconds >= 1f && _stableFrames >= 60;
        }
    }
}
