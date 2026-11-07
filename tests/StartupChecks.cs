using System;
using Climb.Core;

internal static class StartupChecks
{
    public static void Run()
    {
        var gate = new TrialStartupGate();
        for (int i=0; i<30; i++) if (gate.Observe(.05f)) throw new Exception("Premature startup");
        if (gate.Observe(2.284498f)) throw new Exception("Long frame accepted");
        for (int i=0; i<59; i++) if (gate.Observe(1f/60)) throw new Exception("Incomplete stable window accepted");
        for (int i=0; i<3; i++) gate.Observe(1f/60);
        if (!gate.Observe(1f/60)) throw new Exception("Stable startup rejected");
        // A stall immediately after apparent readiness must revoke readiness, not
        // leak into a separate next-frame start transition.
        if (gate.Observe(2.3f) || gate.Observe(float.NaN) || gate.Observe(.016f)) throw new Exception("Readiness did not reset");
        var session = new DemoSession();
        session.Step(2.3f, 1, 0, false, true);
        if (!session.Paused || session.EarnedHeight != 0) throw new Exception("Gameplay pause protection lost");
        Console.WriteLine("PASS StartupChecks: stable window, boundary stall, invalid time, gameplay protection");
    }
}
