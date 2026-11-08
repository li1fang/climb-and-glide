using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using Usage = UnityEngine.XR.CommonUsages;

namespace Climb.Demo
{
    // Live headset/controller adapter. Pulls are synthetic; there is no device-force path.
    public sealed class PcVrSession : MonoBehaviour
    {
        public bool Running { get; private set; }
        public bool Valid { get; private set; }
        public string Status { get; private set; } = "Starting OpenXR";
        public Vector3 HeadPosition { get; private set; }
        public Quaternion HeadRotation { get; private set; } = Quaternion.identity;
        public float Left => _actions == null ? 0 : _actions["Flight/LeftPull"].ReadValue<float>();
        public float Right => _actions == null ? 0 : _actions["Flight/RightPull"].ReadValue<float>();
        public bool Pressed(string name) => _actions != null && _actions["Flight/"+name].WasPressedThisFrame();
        private InputActionAsset _actions;
        private XRManagerSettings _manager;
        private readonly List<XRInputSubsystem> _inputs=new List<XRInputSubsystem>();
        private readonly List<XRDisplaySubsystem> _displays=new List<XRDisplaySubsystem>();
        private StreamWriter _log;
        private float _nextLog, _started;
        private bool _focused=true;
        private bool _probe;

        private IEnumerator Start()
        {
            _probe=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--vr-probe")>=0;
            _started=Time.realtimeSinceStartup;
            var folder=Path.Combine(Application.persistentDataPath,"PCVR-"+System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(folder);
            _log=new StreamWriter(Path.Combine(folder,"session.csv")){AutoFlush=true};
            _log.WriteLine("seconds,dt,running,valid,status,left,right");
            Debug.Log("PCVR output: "+folder);
            _actions=Instantiate(Resources.Load<InputActionAsset>("PcVrActions"));
            _actions.Enable(); // Asset and bindings exist before OpenXR attaches action sets.
            _manager=XRGeneralSettings.Instance?.Manager;
            if(_manager==null){ Status="XR manager missing"; FinishProbe(false); yield break; }
            yield return _manager.InitializeLoader();
            if(_manager.activeLoader==null){ Status="OpenXR unavailable - start Meta Link and retry"; Record(); FinishProbe(false); yield break; }
            _manager.StartSubsystems();
            SubsystemManager.GetSubsystems(_inputs);
            foreach(var input in _inputs) input.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device);
            SubsystemManager.GetSubsystems(_displays);
            foreach(var display in _displays) display.displayFocusChanged += FocusChanged;
            Application.runInBackground=true;
            QualitySettings.vSyncCount=0; Application.targetFrameRate=-1;
            Status="Waiting for headset and both controllers";
        }
        private void Update()
        {
            Sample();
            if(Time.realtimeSinceStartup>=_nextLog){Record();_nextLog=Time.realtimeSinceStartup+.25f;}
            if(_probe && Time.realtimeSinceStartup-_started>15) FinishProbe(Valid);
        }
        public bool Sample()
        {
            Running=false; foreach(var display in _displays) Running |= display.running;
            var head=InputDevices.GetDeviceAtXRNode(XRNode.Head);
            bool headValid=head.TryGetFeatureValue(Usage.isTracked,out bool tracked)&&tracked;
            headValid &= head.TryGetFeatureValue(Usage.devicePosition,out Vector3 position);
            headValid &= head.TryGetFeatureValue(Usage.deviceRotation,out Quaternion rotation);
            bool present=!head.TryGetFeatureValue(Usage.userPresence,out bool presence)||presence;
            bool left=Tracked(XRNode.LeftHand),right=Tracked(XRNode.RightHand);
            bool bound=_actions!=null && _actions["Flight/LeftPull"].controls.Count>0 && _actions["Flight/RightPull"].controls.Count>0;
            Valid=Running && _focused && present && headValid && left && right && bound;
            if(headValid){
                bool floor=false; foreach(var input in _inputs) floor |= input.GetTrackingOriginMode()==TrackingOriginModeFlags.Floor;
                HeadPosition=position+(floor?Vector3.zero:Vector3.up*1.65f); HeadRotation=rotation;
            }
            if(Running) Status=Valid?"Tracking ready":!headValid?"Head tracking lost":!_focused||!present?"Headset inactive":!left||!right?"Controller tracking lost":"Controller bindings unavailable";
            return headValid;
        }
        private static bool Tracked(XRNode node) => InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(Usage.isTracked,out bool value)&&value;
        private void FocusChanged(bool focused){_focused=focused;}
        private void Record(){_log?.WriteLine(System.FormattableString.Invariant($"{Time.realtimeSinceStartup-_started},{Time.unscaledDeltaTime},{Running},{Valid},{Status},{Left},{Right}"));}
        private void FinishProbe(bool valid){if(_probe) Application.Quit(valid?0:4);}
        private void OnDestroy()
        {
            foreach(var display in _displays) display.displayFocusChanged-=FocusChanged;
            _log?.Dispose();
            if(_manager!=null && _manager.activeLoader!=null){_manager.StopSubsystems();_manager.DeinitializeLoader();}
            if(_actions!=null){_actions.Disable();Destroy(_actions);}
        }
    }
}
