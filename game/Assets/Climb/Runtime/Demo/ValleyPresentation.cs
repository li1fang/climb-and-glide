using System.Collections.Generic;
using Climb.Core;
using UnityEngine;

namespace Climb.Demo
{
    public sealed partial class ClimbDemoController
    {
        private Mesh _terrainMesh;
        private GUIStyle _navTitle, _navText;
        private float TargetDistance() => Vector2.Distance(new Vector2(_rig.position.x,_rig.position.z),new Vector2(_targets[_targetIndex].x,_targets[_targetIndex].z));
        private float TargetBearing() => ValleyCourse.Bearing(new Vec3(_rig.position.x,_rig.position.y,_rig.position.z),
            ValleyCourse.Targets[_targetIndex],_mode==DemoMode.Flying || _mode==DemoMode.Landed ? _session.Flight.HeadingRadians : 0) * Mathf.Rad2Deg;

        // Automated tester reads the same navigation state; it sends pulls, never edits flight state.
        private void AutoSteer(out float left,out float right)
        {
            float ratio=TargetDistance()/Mathf.Max(1,_session.Flight.Position.Y);
            float lo=0,hi=.8f;
            for(int i=0;i<12;i++) {
                float c=(lo+hi)*.5f;
                float range=(10-5*c)/(1.25f+2.1f*c*c);
                if(range>ratio) lo=c; else hi=c;
            }
            float brake=(lo+hi)*.5f;
            float steering=Mathf.Clamp(TargetBearing()*Mathf.Deg2Rad*.8f-_session.Flight.TurnRateRadians*.7f,-.65f,.65f);
            left=Mathf.Clamp01(brake-Mathf.Max(0,steering)+Mathf.Max(0,-steering));
            right=Mathf.Clamp01(brake+Mathf.Max(0,steering)-Mathf.Max(0,-steering));
        }

        private void BuildValley()
        {
            var vertices=new List<Vector3>(); var colors=new List<Color>(); var triangles=new List<int>();
            for(int x=-400;x<400;x+=20) for(int z=-240;z<1000;z+=10) {
                int n=vertices.Count;
                foreach(var p in new[]{new Vector2(x,z),new Vector2(x,z+10),new Vector2(x+20,z+10),new Vector2(x+20,z)}) {
                    float h=ValleyCourse.Height(p.x,p.y); vertices.Add(new Vector3(p.x,h,p.y));
                    bool alternate=((Mathf.FloorToInt(x/40f)+Mathf.FloorToInt(z/40f))&1)==0;
                    Color c=alternate ? new Color(.31f,.46f,.26f) : new Color(.38f,.52f,.30f);
                    if(h>1 && h<79) c=Color.Lerp(new Color(.40f,.35f,.29f),new Color(.58f,.53f,.42f),h/80);
                    colors.Add(c);
                }
                triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            _terrainMesh=new Mesh {name="Valley height field"}; _terrainMesh.SetVertices(vertices); _terrainMesh.SetColors(colors);
            _terrainMesh.SetTriangles(triangles,0); _terrainMesh.RecalculateNormals(); _terrainMesh.RecalculateBounds();
            var obj=new GameObject("Valley terrain"); obj.transform.SetParent(_world,false);
            obj.AddComponent<MeshFilter>().sharedMesh=_terrainMesh;
            Material terrain=MakeMaterial(Color.white); terrain.SetFloat("_VertexColors",1);
            obj.AddComponent<MeshRenderer>().sharedMaterial=terrain;
            Material white=MakeMaterial(new Color(.9f,.9f,.72f)), bark=MakeMaterial(new Color(.33f,.23f,.14f)), leaves=MakeMaterial(new Color(.15f,.32f,.19f));
            for(int z=100;z<=850;z+=50) {
                Shape("50m distance marker",PrimitiveType.Cube,_world,new Vector3(35,.04f,z),new Vector3(12,.08f,1),white);
                for(int side=-1;side<=1;side+=2) {
                    float x=side*(65+(z%150));
                    Shape("Tree trunk",PrimitiveType.Cylinder,_world,new Vector3(x,2,z),new Vector3(.8f,2,.8f),bark);
                    Shape("Tree crown",PrimitiveType.Sphere,_world,new Vector3(x,6,z),new Vector3(7,9,7),leaves);
                }
            }
            for(int i=0;i<_targets.Length;i++) {
                Shape("Target cross",PrimitiveType.Cube,_world,_targets[i]+Vector3.up*.09f,new Vector3(22,.08f,2),white);
                Shape("Target cross",PrimitiveType.Cube,_world,_targets[i]+Vector3.up*.10f,new Vector3(2,.08f,22),white);
            }
        }

        private void DrawNavigation()
        {
            if(_rig==null) return;
            if(_navTitle==null) {
                _navTitle=new GUIStyle(GUI.skin.label){fontSize=23,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
                _navText=new GUIStyle(GUI.skin.label){fontSize=17,alignment=TextAnchor.MiddleCenter};
            }
            float angle=TargetBearing(), agl=Mathf.Max(0,_rig.position.y-ValleyCourse.Height(_rig.position.x,_rig.position.z));
            string direction=Mathf.Abs(angle)<5?"AHEAD":(angle<0?"< LEFT":"RIGHT >")+" "+Mathf.Abs(angle).ToString("0")+" deg";
            if(Mathf.Abs(angle)>135) direction="BEHIND - "+direction;
            if (_mode == DemoMode.Landed) direction = TargetDistance() <= 20 ? "LANDED IN TARGET" : "LANDED OUTSIDE TARGET";
            if (_mode != DemoMode.Landed) direction = "COURSE " + direction;
            string target="TARGET "+(char)('A'+_targetIndex);
            float center=Screen.width*.5f;
            GUI.Box(new Rect(center-280,12,560,86),"");
            GUI.Label(new Rect(center-275,16,550,34),target+"  |  "+direction,_navTitle);
            GUI.Label(new Rect(center-275,50,550,30),"Horizontal "+TargetDistance().ToString("0")+" m   |   Height above target "+_rig.position.y.ToString("0")+" m",_navText);
            GUI.Box(new Rect(16,16,206,92),"");
            GUI.Label(new Rect(20,20,198,32),"AGL  "+agl.ToString("0.0")+" m",_navTitle);
            GUI.Label(new Rect(20,55,198,30),_mode==DemoMode.Flying ? "Speed "+_session.Flight.Airspeed.ToString("0.0")+" m/s" : _mode==DemoMode.Landed ? "ON GROUND" : "Climb "+_displayHeight.ToString("0.0")+" / 12 m",_navText);
            string phase=_paused?"PAUSED - P to resume":_mode==DemoMode.Climbing?(AtPlatform()?"PLATFORM - Space to launch, or keep climbing":"Alternate Q / E to climb"):
                _mode==DemoMode.PreparingFlight?"Release both pulls to launch":_mode==DemoMode.Flying?"GLIDING - Q / E steer, both brake":"LANDED - target distance "+TargetDistance().ToString("0.0")+" m - R retry";
            GUI.Box(new Rect(center-360,Screen.height-80,720,64),"");
            GUI.Label(new Rect(center-350,Screen.height-79,700,30),phase,_navTitle);
            GUI.Label(new Rect(center-350,Screen.height-48,700,25),"Tab: target   RMB: look   F: flight   C: ladder   P: pause",_navText);
            if (_mode == DemoMode.Landed) return;
            Vector3 p=_camera.WorldToScreenPoint(_targets[_targetIndex]+Vector3.up*2);
            bool visible=p.z>0 && p.x>80 && p.x<Screen.width-80 && p.y>110 && p.y<Screen.height-115;
            if(p.z<0) {p.x=Screen.width-p.x;p.y=Screen.height-p.y;}
            float px=Mathf.Clamp(p.x,85,Screen.width-85), py=Mathf.Clamp(Screen.height-p.y,135,Screen.height-115);
            GUI.color=new Color(1,.85f,.28f);
            GUI.Box(new Rect(px-75,py-17,150,34),(visible?"+ ":"OFFSCREEN ")+(char)('A'+_targetIndex)+" "+TargetDistance().ToString("0")+"m");
            GUI.color=Color.white;
        }
    }
}
