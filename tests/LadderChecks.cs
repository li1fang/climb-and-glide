using System;
using Climb.Core;

namespace Climb.Tests
{
    public static class LadderChecks
    {
        public static void Run()
        {
            StartupAndHoldingDoNotClimb();
            SmallJitterDoesNotClimb();
            AlternatingPullsClimbOnceEach();
            EitherHandMayStart();
            RepeatedSingleHandDoesNotClimb();
            SimultaneousCandidatesAreRejected();
            InvalidDataAndResumptionDoNotClimb();
            LongSamplingGapRebases();
            ExcessTravelIsOnlyOneStroke();
            InvalidConfigurationIsRejected();
            Console.WriteLine("Ladder: 10 behavioral checks passed.");
        }

        private static void StartupAndHoldingDoNotClimb()
        {
            var model = new LadderModel();
            Equal(0f, Observe(model, 0.9f, 0.8f), "Already-extended startup must only set baseline.");
            for (int i = 0; i < 100; i++)
                Equal(0f, Observe(model, 0.9f, 0.8f), "Holding must not create ongoing ascent.");
            Equal(0f, model.HeightMeters, "No initial free height.");
        }

        private static void SmallJitterDoesNotClimb()
        {
            var model = new LadderModel();
            Observe(model, 0.5f, 0.5f);
            for (int i = 0; i < 1000; i++)
            {
                float noise = (i % 3 - 1) * 0.004f;
                Equal(0f, Observe(model, 0.5f + noise, 0.5f - noise),
                    "Sub-centimetre jitter must not create a stroke.");
            }
            Equal(0f, model.HeightMeters, "Jitter must not accumulate into ascent.");
        }

        private static void AlternatingPullsClimbOnceEach()
        {
            var model = new LadderModel();
            Observe(model, 0.5f, 0.5f);
            Equal(0.25f, Observe(model, 0.56f, 0.5f), "Left pull.");
            Equal(LadderHand.Left, model.LastEventHand, "Left event.");
            Equal(0f, Observe(model, 0.6f, 0.5f), "Continued pull is the same stroke.");
            Equal(0.25f, Observe(model, 0.6f, 0.56f), "Right pull follows left.");
            Equal(0f, Observe(model, 0.55f, 0.56f), "Returning left does not climb.");
            Equal(0.25f, Observe(model, 0.61f, 0.56f), "Rearmed left follows right.");
            Equal(0.75f, model.HeightMeters, "Three strokes earn exactly three steps.");
            Equal(0f, Observe(model, 0.61f, 0.56f), "Stop immediately stops earning height.");
            Equal(LadderHand.None, model.LastEventHand, "No event persists while stopped.");
        }

        private static void EitherHandMayStart()
        {
            var model = new LadderModel();
            Observe(model, 0.4f, 0.4f);
            Equal(0.25f, Observe(model, 0.4f, 0.46f), "Right can start.");
            Equal(LadderHand.Right, model.LastAcceptedHand, "First hand is right.");
        }

        private static void RepeatedSingleHandDoesNotClimb()
        {
            var model = new LadderModel();
            Observe(model, 0.4f, 0.4f);
            Observe(model, 0.46f, 0.4f);
            for (int i = 0; i < 20; i++)
            {
                Observe(model, 0.4f, 0.4f);
                Equal(0f, Observe(model, 0.46f, 0.4f), "Repeated left must not add height.");
            }
            Equal(0.25f, model.HeightMeters, "Only first left stroke counted.");
            Equal(0.25f, Observe(model, 0.46f, 0.46f), "Right still restores valid alternation.");
        }

        private static void SimultaneousCandidatesAreRejected()
        {
            var model = new LadderModel();
            Observe(model, 0.4f, 0.4f);
            Equal(0f, Observe(model, 0.46f, 0.46f), "Ambiguous simultaneous pull cannot grant two steps.");
            Equal(0f, Observe(model, 0.49f, 0.49f), "Continuing simultaneous pull stays consumed.");
            Equal(0f, model.HeightMeters, "No simultaneous ascent.");
            Observe(model, 0.4f, 0.4f);
            Equal(0.25f, Observe(model, 0.46f, 0.4f), "Returning both permits a fresh unambiguous pull.");
        }

        private static void InvalidDataAndResumptionDoNotClimb()
        {
            var model = new LadderModel();
            Observe(model, 0.4f, 0.4f);
            Observe(model, 0.46f, 0.4f);
            Equal(0f, model.Update(1f, 1f, 0.1f, false), "Disconnected/stale data must not climb.");
            Equal(0f, Observe(model, 1f, 1f), "Recovery rebases without awarding a stroke.");
            Equal(LadderHand.Left, model.LastAcceptedHand, "A disconnect must not reset alternation.");
            Equal(0f, model.Update(float.NaN, 1f, 0.1f, true), "NaN must invalidate.");
            Equal(0f, Observe(model, 1.2f, 1.2f), "Recovery from NaN rebases.");
            Equal(0f, model.Update(-1f, 1.2f, 0.1f, true), "Negative rope length is invalid.");
            Equal(0f, Observe(model, 1.3f, 1.3f), "Recovery from bad length rebases.");
            Equal(0.25f, model.HeightMeters, "Invalid data cannot alter earned height.");
        }

        private static void LongSamplingGapRebases()
        {
            var model = new LadderModel();
            Observe(model, 0.4f, 0.4f);
            Equal(0f, model.Update(0.8f, 0.4f, 1f, true), "A long gap cannot turn a jump into a stroke.");
            Equal(0f, Observe(model, 0.8f, 0.4f), "Rebased held value does not climb.");
            Equal(0.25f, Observe(model, 0.86f, 0.4f), "Fresh pull after a gap works.");
        }

        private static void ExcessTravelIsOnlyOneStroke()
        {
            var model = new LadderModel();
            Observe(model, 0.2f, 0.2f);
            Equal(0.25f, Observe(model, 1.2f, 0.2f), "One long pull must not multiply the step count.");
            Equal(0f, Observe(model, 1.3f, 0.2f), "Further pull does not continuously climb.");
            model.Reset();
            Equal(0f, model.HeightMeters, "Explicit reset clears progress.");
            Equal(LadderHand.None, model.LastAcceptedHand, "Explicit reset clears hand order.");
        }

        private static void InvalidConfigurationIsRejected()
        {
            bool rejected = false;
            try { new LadderModel(new LadderConfig { PullDistanceMeters = float.NaN }); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            if (!rejected) throw new Exception("Invalid thresholds must be rejected at construction.");
        }

        private static float Observe(LadderModel model, float left, float right)
        {
            return model.Update(left, right, 0.1f, true);
        }

        private static void Equal(float expected, float actual, string message)
        {
            if (Math.Abs(expected - actual) > 0.00001f || float.IsNaN(actual))
                throw new Exception(message + " Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(LadderHand expected, LadderHand actual, string message)
        {
            if (expected != actual) throw new Exception(message);
        }
    }
}
