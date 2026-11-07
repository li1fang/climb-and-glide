using System;
namespace Climb.Core
{
    public static class ValleyCourse
    {
        public const float BaseHeight = 80;
        public static readonly Vec3[] Targets = { new Vec3(0,0,550), new Vec3(-150,0,430), new Vec3(160,0,650) };
        // Match the rendered mesh's 20m x 10m cells and diagonal exactly, including corners.
        public static float Height(float x, float z)
        {
            float x0=(float)Math.Floor(x/20)*20,z0=(float)Math.Floor(z/10)*10;
            float u=(x-x0)/20,v=(z-z0)/10;
            float a=Raw(x0,z0),b=Raw(x0,z0+10),c=Raw(x0+20,z0+10),d=Raw(x0+20,z0);
            return v>=u ? a+(c-b)*u+(b-a)*v : a+(d-a)*u+(c-d)*v;
        }
        private static float Raw(float x,float z) => BaseHeight * Clamp((100-Math.Abs(x))/40) * Clamp((70-z)/70) * Clamp((z+240)/60);
        private static float Clamp(float x) => Math.Max(0,Math.Min(1,x));
        public static float Bearing(Vec3 from, Vec3 target, float heading)
        {
            float a=(float)Math.Atan2(target.X-from.X,target.Z-from.Z)-heading;
            while(a>Math.PI) a-=(float)(2*Math.PI);
            while(a< -Math.PI) a+=(float)(2*Math.PI);
            return a;
        }
        public static float Distance(Vec3 from, Vec3 target)
        { float x=target.X-from.X,z=target.Z-from.Z; return (float)Math.Sqrt(x*x+z*z); }
    }
}
