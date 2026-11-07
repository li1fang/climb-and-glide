using System;
using Climb.Core;
namespace Climb.Tests
{
    internal static class ValleyChecks
    {
        private static void Check(bool value,string message) {if(!value) throw new Exception(message);}
        public static void Run()
        {
            Check(ValleyCourse.Height(0,0)==80 && ValleyCourse.Height(0,70)==0,"plateau / valley heights");
            Check(Math.Abs(ValleyCourse.Height(70,35)-31.42857f)<.001,"collision height matches rendered triangle diagonal");
            Check(ValleyCourse.Bearing(Vec3.Zero,new Vec3(-20,0,20),0)<0,"left bearing");
            Check(ValleyCourse.Bearing(Vec3.Zero,new Vec3(20,0,20),0)>0,"right bearing");
            Check(Math.Abs(ValleyCourse.Bearing(Vec3.Zero,new Vec3(0,0,-20),0))>3,"behind bearing");
            Check(ValleyCourse.Distance(Vec3.Zero,new Vec3(3,99,4))==5,"horizontal distance excludes height");
            var flat=new FlightModel(); flat.Reset(new Vec3(0,12,0));
            var valley=new FlightModel(null,ValleyCourse.Height); valley.Reset(new Vec3(0,92,0));
            float flatTime=0,valleyTime=0;
            while(!flat.State.IsGrounded && flatTime<120){flat.Step(.02f,0,0,Vec3.Zero);flatTime+=.02f;}
            while(!valley.State.IsGrounded && valleyTime<120){
                valley.Step(.02f,0,0,Vec3.Zero);valleyTime+=.02f;
                Check(valley.State.Position.Y+1e-4>=ValleyCourse.Height(valley.State.Position.X,valley.State.Position.Z),"terrain penetration");
            }
            Check(valley.State.IsGrounded && valleyTime>flatTime*6,"height drop must extend flight");
            valley.Reset(new Vec3(0,90,30),(float)Math.PI);
            for(int i=0;i<10000 && !valley.State.IsGrounded;i++) valley.Step(.02f,0,0,Vec3.Zero);
            Check(valley.State.IsGrounded && valley.State.Position.Y>60,"return flight lands on plateau not through it");
            var session=new DemoSession(null,null,80,ValleyCourse.Height);
            session.Launch(new Vec3(0,92,0)); session.ResetLadder();
            Check(session.BaseHeight==80 && session.EarnedHeight==0,"reset preserves terrain offset");
            Console.WriteLine($"PASS ValleyChecks: neutral flight {flatTime:F2}s -> {valleyTime:F2}s, terrain/bearing/reset checks");
        }
    }
}
