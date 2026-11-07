using System;

namespace Climb.Tests
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                FlightChecks.Run();
                Console.WriteLine("PASS FlightChecks");
                LadderChecks.Run();
                Console.WriteLine("PASS LadderChecks");
                ValleyChecks.Run();
                if (args.Length == 2 && args[0] == "--trial") AutomatedTrial.RunAll(args[1]);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }
    }
}
