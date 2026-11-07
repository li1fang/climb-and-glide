using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Climb.Core;

namespace Climb.Tests
{
    internal static class AutomatedTrial
    {
        private sealed class Run
        {
            public string Scenario { get; set; }
            public int Fps { get; set; }
            public float LaunchHeight { get; set; }
            public float FlightSeconds { get; set; }
            public float LandingX { get; set; }
            public float LandingZ { get; set; }
            public int Samples { get; set; }
            public bool ResetPassed { get; set; }
        }
        private static void Require(bool pass, string message)
        { if (!pass) throw new InvalidOperationException("A0: " + message); }

        public static void RunAll(string output)
        {
            Directory.CreateDirectory(output);
            var runs = new List<Run>();
            foreach (int fps in new[] {30, 60, 120})
                foreach (string scenario in new[] {"neutral", "left", "right", "collective", "slalom"})
                    runs.Add(Play(output, fps, scenario));
            Guards();
            foreach (Run r in runs)
            {
                Run baseline = runs.Find(x => x.Scenario == r.Scenario && x.Fps == 60);
                Require(Math.Abs(r.LandingX - baseline.LandingX) < 1 && Math.Abs(r.LandingZ - baseline.LandingZ) < 1,
                    "frame-rate landing divergence: " + r.Scenario);
            }
            foreach (int fps in new[] {30,60,120})
            {
                Run l = runs.Find(x => x.Fps == fps && x.Scenario == "left");
                Run r = runs.Find(x => x.Fps == fps && x.Scenario == "right");
                Require(l.LandingX < -1 && r.LandingX > 1 && Math.Abs(l.LandingX + r.LandingX) < .01f, "steering symmetry");
                Require(runs.Find(x => x.Fps == fps && x.Scenario == "collective").LandingZ <
                    runs.Find(x => x.Fps == fps && x.Scenario == "neutral").LandingZ, "collective braking range");
            }
            File.WriteAllText(Path.Combine(output,"summary.json"), JsonSerializer.Serialize(new {
                node="A0-shared-session", passed=true, runUtc=DateTime.UtcNow,
                limitations="No Unity execution, rendered frames, headset or physical device validation", runs
            }, new JsonSerializerOptions { WriteIndented=true }));
            var html = new StringBuilder("<!doctype html><meta charset='utf-8'><title>A0 automated trial</title><style>body{font:16px system-ui;max-width:1100px;margin:40px auto;background:#13212c;color:#eaf2f8}table{border-collapse:collapse}td,th{padding:8px 16px;border-bottom:1px solid #456}svg{background:#203342}a{color:#7de}</style><h1>A0: shared-session automated trial — PASS</h1><p>15 complete ladder → launch → flight → landing → reset runs. Synthetic inputs. No Unity rendering, VR or device validation.</p><table><tr><th>Scenario</th><th>FPS</th><th>Air time (s)</th><th>Landing X / Z (m)</th></tr>");
            foreach (Run r in runs) html.Append(FormattableString.Invariant($"<tr><td>{r.Scenario}</td><td>{r.Fps}</td><td>{r.FlightSeconds:F2}</td><td>{r.LandingX:F2} / {r.LandingZ:F2}</td></tr>"));
            html.Append("</table><h2>Top-down flight traces (60 FPS)</h2><p>Horizontal: X. Vertical: forward Z. Each line is recorded state, not a rendered game capture.</p><svg viewBox='0 0 700 550' width='700' height='550'>");
            string[] colors={"#ffffff","#56baff","#ffbb55","#83de88","#ff7ac6"}; int c=0;
            foreach(string scenario in new[]{"neutral","left","right","collective","slalom"})
            {
                var points=new StringBuilder();
                foreach(string line in File.ReadLines(Path.Combine(output,scenario+"-60.csv")))
                {
                    string[] fields=line.Split(','); if(fields.Length!=8 || fields[1]!="Flying" && fields[1]!="Landed") continue;
                    float x=float.Parse(fields[4],CultureInfo.InvariantCulture), z=float.Parse(fields[6],CultureInfo.InvariantCulture);
                    points.Append(FormattableString.Invariant($"{350+x*4:F1},{510-z*4:F1} "));
                }
                html.Append($"<polyline points='{points}' fill='none' stroke='{colors[c]}' stroke-width='2'/><text x='10' y='{25+c*24}' fill='{colors[c]}'>{scenario}</text>"); c++;
            }
            html.Append("</svg><p>Raw CSV and summary.json are alongside this report. Guards: held input, tiny jitter, same-hand repetition, premature launch, neutral launch gate, pause, missing source, long gap, reset, platform reachability.</p>");
            File.WriteAllText(Path.Combine(output,"report.html"),html.ToString());
            Console.WriteLine("PASS A0: 15 end-to-end runs and session guards. " + Path.GetFullPath(output));
        }

        private static Run Play(string output,int fps,string scenario)
        {
            var s=new DemoSession(); float dt=1f/fps, time=0, flightTime=0;
            int samples=0;
            using var csv=new StreamWriter(Path.Combine(output,scenario+"-"+fps+".csv"));
            csv.WriteLine("seconds,mode,left,right,x,y,z,earnedHeight");
            void Tick(float left,float right,bool confirm=false)
            {
                s.Step(dt,left,right,confirm); time+=dt; samples++;
                Vec3 p=s.Mode==DemoMode.Flying || s.Mode==DemoMode.Landed ? s.Flight.Position : new Vec3(0,s.DisplayHeight,0);
                Require(p.IsFinite && p.Y>=0,"finite position / ground");
                csv.WriteLine(FormattableString.Invariant($"{time:F5},{s.Mode},{s.LeftPull:F5},{s.RightPull:F5},{p.X:F5},{p.Y:F5},{p.Z:F5},{s.EarnedHeight:F3}"));
            }
            Tick(0,0);
            // Each pull has a full release; the player only sends inputs, never writes state.
            for(int stroke=0;stroke<48;stroke++)
            {
                for(int f=0;f<fps/2;f++) Tick(stroke%2==0?1:0,stroke%2==1?1:0);
                for(int f=0;f<fps/2;f++) Tick(0,0);
            }
            Require(s.AtPlatform && Math.Abs(s.DisplayHeight-12)<.01,"reach tower platform");
            float launchHeight=s.DisplayHeight;
            Tick(0,0,true);
            Require(s.Mode==DemoMode.PreparingFlight,"platform confirmation");
            for(int f=0;f<fps && s.Mode!=DemoMode.Flying;f++) Tick(0,0);
            Require(s.Mode==DemoMode.Flying && s.LeftPull==0 && s.RightPull==0,"neutral launch");
            for(int f=0;f<fps*60 && s.Mode==DemoMode.Flying;f++)
            {
                float l=0,r=0;
                if(scenario=="left" && flightTime<2) l=.6f;
                if(scenario=="right" && flightTime<2) r=.6f;
                if(scenario=="collective") l=r=.5f;
                if(scenario=="slalom") { if(flightTime<1.5f) l=.5f; else if(flightTime<3) r=.5f; }
                Tick(l,r); flightTime+=dt;
            }
            Require(s.Mode==DemoMode.Landed,"flight timeout");
            var result=new Run {Scenario=scenario,Fps=fps,LaunchHeight=launchHeight,FlightSeconds=flightTime,
                LandingX=s.Flight.Position.X,LandingZ=s.Flight.Position.Z,Samples=samples};
            Vec3 landing=s.Flight.Position; for(int f=0;f<fps;f++) Tick(1,1);
            Require((s.Flight.Position-landing).Length==0,"landed state moves");
            s.ResetLadder();
            result.ResetPassed=s.Mode==DemoMode.Climbing && s.EarnedHeight==0 && s.LeftPull==0 && s.RightPull==0;
            Require(result.ResetPassed,"reset");
            return result;
        }

        private static void Guards()
        {
            var s=new DemoSession(); const float dt=1f/60;
            void Hold(float l,float r,int frames=60) { for(int i=0;i<frames;i++) s.Step(dt,l,r); }
            Hold(0,0); s.Step(dt,0,0,true); Require(s.Mode==DemoMode.Climbing,"ground launch accepted");
            for(int i=0;i<300;i++) s.Step(dt,i%2==0?.008f:0,0);
            Require(s.EarnedHeight==0,"tiny jitter climbs");
            Hold(1,0,180); Require(s.EarnedHeight==.25f,"held input repeats");
            Hold(0,0); Hold(1,0); Require(s.EarnedHeight==.25f,"same-hand repeats");
            s.SetPaused(true); Hold(0,1); Require(s.EarnedHeight==.25f,"pause advances");
            s.SetPaused(false); s.Step(dt,0,1); Require(s.EarnedHeight==.25f,"resume gives free stroke");
            s.Step(dt,0,0,false,false); Require(s.Paused,"missing source does not pause");
            s.SetPaused(false); s.Step(2,1,1); Require(s.Paused,"long gap does not pause");
            s.ResetLadder(); Hold(0,0);
            for(int stroke=0;stroke<12;stroke++) { Hold(stroke%2==0?1:0,stroke%2==1?1:0,30); Hold(0,0,30); }
            Require(s.AtPlatform,"first platform unreachable");
            s.Step(dt,1,1,true); Hold(1,1,120); Require(s.Mode==DemoMode.PreparingFlight,"launch with held pulls");
            Hold(0,0,120); Require(s.Mode==DemoMode.Flying,"neutral gate cannot release");
            Vec3 p=s.Flight.Position; s.SetPaused(true); Hold(1,1);
            Require((s.Flight.Position-p).Length==0,"flight pause moves");
        }
    }
}
