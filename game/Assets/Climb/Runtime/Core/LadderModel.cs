using System;

namespace Climb.Core
{
    public enum LadderHand
    {
        None,
        Left,
        Right
    }

    /// <summary>
    /// A deliberately simple alternating-stroke game convention.
    /// A sufficiently large pull gives one step, then that hand must return before
    /// another candidate pull. Accepted steps must alternate hands.
    /// This class never infers real grip or force and never emits hardware commands.
    /// </summary>
    public sealed class LadderModel
    {
        private readonly float _pullDistance;
        private readonly float _rearmDistance;
        private readonly float _heightPerStroke;
        private readonly float _maxSampleGap;
        private HandStroke _left;
        private HandStroke _right;
        private bool _hasBaseline;

        public float HeightMeters { get; private set; }
        public LadderHand LastAcceptedHand { get; private set; }
        public LadderHand LastEventHand { get; private set; }

        public LadderModel(LadderConfig config = null)
        {
            config = config ?? new LadderConfig();
            config.Validate();
            // Snapshot the parameters, so an inspector edit cannot change a stroke
            // halfway through. Recreate the model at an intentional reset instead.
            _pullDistance = config.PullDistanceMeters;
            _rearmDistance = config.RearmDistanceMeters;
            _heightPerStroke = config.HeightPerStrokeMeters;
            _maxSampleGap = config.MaxSampleGapSeconds;
            Reset();
        }

        /// <summary>
        /// Feed one observation pair; lengths are metres, increasing when pulled out.
        /// valid must be false for stale, disconnected, mismapped, or uncalibrated data.
        /// deltaTimeSeconds is the elapsed time since the previous observation pair.
        /// Returns newly earned height only, never ongoing velocity.
        /// </summary>
        public float Update(float leftLengthMeters, float rightLengthMeters,
            float deltaTimeSeconds, bool valid)
        {
            LastEventHand = LadderHand.None;

            if (!valid || !IsLength(leftLengthMeters) || !IsLength(rightLengthMeters) ||
                !IsFinite(deltaTimeSeconds) || deltaTimeSeconds <= 0f)
            {
                Invalidate();
                return 0f;
            }

            if (!_hasBaseline || deltaTimeSeconds > _maxSampleGap)
            {
                EstablishBaseline(leftLengthMeters, rightLengthMeters);
                return 0f;
            }

            bool leftCandidate = _left.Observe(leftLengthMeters, _pullDistance, _rearmDistance);
            bool rightCandidate = _right.Observe(rightLengthMeters, _pullDistance, _rearmDistance);

            // If both hands qualify in the same observation pair, the intended order
            // is ambiguous. Neither receives height; both must return to rearm.
            if (leftCandidate == rightCandidate)
                return 0f;

            LadderHand candidate = leftCandidate ? LadderHand.Left : LadderHand.Right;
            if (candidate == LastAcceptedHand)
                return 0f;

            LastAcceptedHand = candidate;
            LastEventHand = candidate;
            HeightMeters += _heightPerStroke;
            return _heightPerStroke;
        }

        public void Reset()
        {
            HeightMeters = 0f;
            LastAcceptedHand = LadderHand.None;
            Invalidate();
        }

        /// <summary>
        /// Forget the observation baseline without erasing earned height or changing
        /// which hand is expected next. The next valid pair cannot award a step.
        /// </summary>
        public void Invalidate()
        {
            _hasBaseline = false;
            LastEventHand = LadderHand.None;
        }

        private void EstablishBaseline(float leftLengthMeters, float rightLengthMeters)
        {
            _left = new HandStroke(leftLengthMeters);
            _right = new HandStroke(rightLengthMeters);
            _hasBaseline = true;
        }

        private static bool IsLength(float value)
        {
            return IsFinite(value) && value >= 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private struct HandStroke
        {
            private bool _armed;
            private float _anchor;
            private float _peak;

            public HandStroke(float initialLength)
            {
                _armed = true;
                _anchor = initialLength;
                _peak = initialLength;
            }

            public bool Observe(float length, float pullDistance, float rearmDistance)
            {
                if (_armed)
                {
                    _anchor = Math.Min(_anchor, length);
                    if (length - _anchor < pullDistance)
                        return false;

                    _armed = false;
                    _peak = length;
                    return true;
                }

                _peak = Math.Max(_peak, length);
                if (_peak - length >= rearmDistance)
                {
                    _armed = true;
                    _anchor = length;
                }

                return false;
            }
        }
    }
}
