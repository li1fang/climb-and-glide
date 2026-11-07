using System.Collections.Generic;
using Climb.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR;
using XRUsage = UnityEngine.XR.CommonUsages;

namespace Climb.Demo
{
    /// <summary>
    /// Generated graybox, synthetic controls only. No PUMP/BLE or force commands.
    /// Native Unity rendering, XR and comfort still require real-editor/headset checks.
    /// </summary>
    public sealed partial class ClimbDemoController : MonoBehaviour
    {
        public FlightConfig Flight = new FlightConfig();
        public LadderConfig Ladder = new LadderConfig();
        private const float PlatformSpacing = DemoSession.PlatformSpacing;
        private const float TowerHeight = DemoSession.TowerHeight;
        private DemoSession _session;
        private DemoMode _mode => _session.Mode;
        private float _leftPull => _session.LeftPull;
        private float _rightPull => _session.RightPull;
        private float _displayHeight => _session.DisplayHeight;
        private bool _paused => _session.Paused;
        private Transform _world, _rig, _canopy, _leftHandle, _rightHandle;
        private Camera _camera;
        private TextMesh _board;
        private LineRenderer _leftLine, _rightLine;
        private readonly List<Material> _materials = new List<Material>();
        // Distances suit 3 / 6 / 9-12 m launches with the default still-air tuning.
        private readonly Vector3[] _targets = System.Array.ConvertAll(ValleyCourse.Targets, p => new Vector3(p.X,p.Y,p.Z));
        private int _targetIndex;
        private float _lookYaw, _lookPitch;
        private float _xrHeightOffset = -1f;
        private bool _previousConfirm, _focused = true;
        private string _status = "";
        private bool _autoTrial, _trialPrimed;
        private float _trialTime;
        private System.IO.StreamWriter _trialLog;
        private int _landedFrames;
        private string _trialDirectory;
        private readonly HashSet<string> _captures = new HashSet<string>();

        private void Start()
        {
            _session = new DemoSession(Flight, Ladder, ValleyCourse.BaseHeight, ValleyCourse.Height);
            _autoTrial = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--auto-trial") >= 0;
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--trial-left") >= 0) _targetIndex = 1;
            if (_autoTrial) {
                _trialDirectory = System.IO.Path.Combine(Application.persistentDataPath, "A2-" + System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
                System.IO.Directory.CreateDirectory(_trialDirectory);
                _trialLog = new System.IO.StreamWriter(System.IO.Path.Combine(_trialDirectory, "frames.csv"));
                _trialLog.WriteLine("time,dt,mode,height,x,z,altitude,agl,targetDistance,bearing,targetIndex");
                Application.runInBackground = true;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 60;
                Debug.Log("A2 output: " + _trialDirectory);
            }
            BuildWorld();
            BuildPlayer();
            ResetLadder();
        }

        private void Update()
        {
            if (_rig == null) return;
            float dt = Time.unscaledDeltaTime;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.tabKey.wasPressedThisFrame) _targetIndex = (_targetIndex + 1) % _targets.Length;
                if (keyboard.pKey.wasPressedThisFrame) TogglePause();
                if (keyboard.cKey.wasPressedThisFrame) ResetLadder();
                if (keyboard.fKey.wasPressedThisFrame) Launch(new Vec3(0, 92, 0));
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    if (_mode == DemoMode.Flying || _mode == DemoMode.Landed) Launch(_session.LastLaunch);
                    else ResetLadder();
                }
            }

            float leftTarget = keyboard != null && keyboard.qKey.isPressed ? 1f : 0f;
            float rightTarget = keyboard != null && keyboard.eKey.isPressed ? 1f : 0f;
            bool confirm = keyboard != null && keyboard.spaceKey.isPressed;
            bool inputValid = ReadXRControls(ref leftTarget, ref rightTarget, ref confirm);
            if (_autoTrial) {
                _trialTime += dt;
                if (!_trialPrimed) {
                    ApplySessionView(); UpdateView(); UpdateStatus();
                    if (_trialTime < 3f) return;
                    _session.ResetLadder(); _trialPrimed = true; _trialTime = 0;
                    return;
                }
                _trialLog.WriteLine(System.FormattableString.Invariant($"{_trialTime},{dt},{_mode},{_displayHeight},{_session.Flight.Position.X},{_session.Flight.Position.Z},{_rig.position.y},{_rig.position.y-ValleyCourse.Height(_rig.position.x,_rig.position.z)},{TargetDistance()},{TargetBearing()},{_targetIndex}"));
                inputValid = true;
                leftTarget = rightTarget = 0;
                confirm = _session.AtPlatform && _session.EarnedHeight >= 12;
                if (_mode == DemoMode.Climbing && _trialTime > 1) {
                    float t = _trialTime - 1;
                    if (t % 1f < .5f) { if (((int)t % 2) == 0) leftTarget = 1; else rightTarget = 1; }
                }
                if (_mode == DemoMode.Flying) {
                    AutoSteer(out leftTarget, out rightTarget);
                    if (_session.Flight.SimulatedSeconds > 15 && _captures.Add("FlightMid")) ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(_trialDirectory,"FlightMid.png"));
                    if (_session.Flight.Position.Y < 15 && _captures.Add("Approach")) ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(_trialDirectory,"Approach.png"));
                }
                string phase = _mode.ToString();
                if (_trialTime > .5f && _captures.Add(phase)) ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(_trialDirectory, phase + ".png"));
                if (_mode == DemoMode.Landed && ++_landedFrames >= 30) {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(_trialDirectory, "completed.json"), "{\"landed\":true,\"synthetic\":true}");
                    Application.Quit(0);
                }
                if (_trialTime > 240) { Debug.LogError("A2 timeout"); Application.Quit(2); }
            }
            bool confirmPressed = confirm && !_previousConfirm;
            _previousConfirm = confirm;

            if ((_focused || _autoTrial) && dt > 0f)
                _session.Step(dt, leftTarget, rightTarget, confirmPressed, inputValid);
            ApplySessionView();

            UpdateView();
            if (_mode == DemoMode.Flying && _lookPitch == 12) _lookPitch = 18;
            UpdateHandles();
            UpdateStatus();
        }

        private void ApplySessionView()
        {
            bool airborne = _mode == DemoMode.Flying || _mode == DemoMode.Landed;
            _canopy.gameObject.SetActive(airborne);
            if (airborne)
            {
                FlightState state = _session.Flight;
                _rig.position = ToUnity(state.Position);
                _rig.rotation = Quaternion.Euler(0, state.HeadingRadians * Mathf.Rad2Deg, 0);
                _canopy.localRotation = Quaternion.Euler(0, 0, -state.BankRadians * Mathf.Rad2Deg);
            }
            else _rig.SetPositionAndRotation(new Vector3(0, ValleyCourse.BaseHeight + _displayHeight, 0), Quaternion.identity);
        }

        private void ResetLadder()
        {
            if (_session == null) return;
            _session.ResetLadder();
            _lookYaw = 0; _lookPitch = 12;
            ApplySessionView();
        }

        private void Launch(Vec3 position)
        {
            _session.Launch(position);
            _lookYaw = _lookPitch = 0;
            ApplySessionView();
        }

        private bool AtPlatform() => _session.AtPlatform;
        private void TogglePause() => _session.SetPaused(!_paused);

        private void OnApplicationFocus(bool focused)
        {
            _focused = focused;
            if (!focused && !_autoTrial) _session?.SetPaused(true);
        }

        private static bool ReadXRControls(ref float left, ref float right, ref bool confirm)
        {
            if (!XRSettings.isDeviceActive) return true;
            var l = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var r = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool leftValid = l.TryGetFeatureValue(XRUsage.trigger, out float lv);
            bool rightValid = r.TryGetFeatureValue(XRUsage.trigger, out float rv);
            if (leftValid) left = Mathf.Max(left, lv);
            if (rightValid) right = Mathf.Max(right, rv);
            if (l.TryGetFeatureValue(XRUsage.primaryButton, out bool lb)) confirm |= lb;
            if (r.TryGetFeatureValue(XRUsage.primaryButton, out bool rb)) confirm |= rb;
            return leftValid && rightValid;
        }

        private void UpdateView()
        {
            if (XRSettings.isDeviceActive)
            {
                var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                if (head.TryGetFeatureValue(XRUsage.isTracked, out bool tracked) && tracked)
                {
                    Vector3 position = InputTracking.GetLocalPosition(XRNode.Head);
                    if (_xrHeightOffset < 0f) _xrHeightOffset = position.y < 0.2f ? 1.65f : 0f;
                    _camera.transform.localPosition = position + Vector3.up * _xrHeightOffset;
                    _camera.transform.localRotation = InputTracking.GetLocalRotation(XRNode.Head);
                }
                return;
            }
            _xrHeightOffset = -1f;
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                _lookYaw += delta.x * 0.12f;
                _lookPitch = Mathf.Clamp(_lookPitch - delta.y * 0.12f, -80f, 80f);
            }
            _camera.transform.localPosition = new Vector3(0, 1.65f, 0);
            _camera.transform.localRotation = Quaternion.Euler(_lookPitch, _lookYaw, 0f);
        }

        private void UpdateHandles()
        {
            _leftHandle.localPosition = new Vector3(-0.35f, 1.45f - _leftPull * 0.5f, 0.55f);
            _rightHandle.localPosition = new Vector3(0.35f, 1.45f - _rightPull * 0.5f, 0.55f);
            _leftLine.SetPosition(1, _leftHandle.localPosition);
            _rightLine.SetPosition(1, _rightHandle.localPosition);
        }

        private void UpdateStatus()
        {
            string phase;
            if (_mode == DemoMode.Climbing)
                phase = AtPlatform() ? "PLATFORM: Space / primary button to prepare flight; or keep alternating."
                    : "LADDER: alternate meaningful pulls. Hold still to stop earning height.";
            else if (_mode == DemoMode.PreparingFlight) phase = "PREPARING: release Q/E or triggers to centre both controls.";
            else if (_mode == DemoMode.Flying)
                phase = "FLIGHT: height " + _session.Flight.Position.Y.ToString("0.0") + " m | airspeed " + _session.Flight.Airspeed.ToString("0.0") + " m/s";
            else phase = "LANDED: nearest target centre " + NearestTargetDistance().ToString("0.0") + " m. R to fly again.";
            _status = "SYNTHETIC DEMO - NO PUMP / NO FORCE COMMANDS\n" + (_paused ? "PAUSED - P to resume\n" : "") + phase +
                "\nL " + _leftPull.ToString("0.00") + "  R " + _rightPull.ToString("0.00") + " | ladder " + _displayHeight.ToString("0.00") + " m" +
                "\nQ/E: pull, release: return | F: air test | C: ladder | R: reset | P: pause\nRMB + mouse: look only. XR triggers simulate rope pulls.\nNormal-flight approximation; valley terrain. XR not yet validated.";
            _board.text = _status;
            _board.gameObject.SetActive(XRSettings.isDeviceActive);
        }

        private float NearestTargetDistance()
        {
            float best = float.PositiveInfinity;
            foreach (Vector3 target in _targets)
            {
                Vector3 difference = _rig.position - target;
                difference.y = 0f;
                best = Mathf.Min(best, difference.magnitude);
            }
            return best;
        }

        private void BuildWorld()
        {
            _world = new GameObject("Generated demo world").transform;
            Material ground = MakeMaterial(new Color(0.17f, 0.25f, 0.18f));
            Material steel = MakeMaterial(new Color(0.52f, 0.58f, 0.62f));
            Material deck = MakeMaterial(new Color(0.32f, 0.38f, 0.42f));
            Material target = MakeMaterial(new Color(0.95f, 0.63f, 0.13f));
            BuildValley();
            Shape("Distant ground", PrimitiveType.Cube, _world, new Vector3(0,-.3f,300), new Vector3(5000,.3f,5000), ground);
            var tower = new GameObject("Plateau tower").transform; tower.SetParent(_world,false); tower.localPosition = Vector3.up * ValleyCourse.BaseHeight;
            for (int side = -1; side <= 1; side += 2)
            {
                Shape("Tower side", PrimitiveType.Cube, tower, new Vector3(side * 2, 6, -1.5f), new Vector3(0.22f, 12, 0.22f), steel);
                Shape("Ladder rail", PrimitiveType.Cube, tower, new Vector3(side * 0.6f, 6, 0.85f), new Vector3(0.08f, 12, 0.08f), steel);
            }
            for (float y = 0.25f; y <= TowerHeight; y += 0.25f)
                Shape("Rung", PrimitiveType.Cube, tower, new Vector3(0, y, 0.85f), new Vector3(1.2f, 0.045f, 0.08f), steel);
            for (float y = PlatformSpacing; y <= TowerHeight; y += PlatformSpacing)
                Shape("Platform", PrimitiveType.Cube, tower, new Vector3(0, y - 0.08f, -0.7f), new Vector3(4.5f, 0.16f, 3.5f), deck);
            for (int i = 0; i < _targets.Length; i++)
            {
                Shape("Landing target " + (i + 1), PrimitiveType.Cylinder, _world, _targets[i] + Vector3.up * 0.02f, new Vector3(40, 0.02f, 40), target);
                Shape("Target mast", PrimitiveType.Cube, _world, _targets[i] + new Vector3(9, 3, 0), new Vector3(0.15f, 6, 0.15f), steel);
            }
            var sun = new GameObject("Demo sun");
            sun.transform.SetParent(_world);
            sun.transform.rotation = Quaternion.Euler(40, -35, 0);
            var light = sun.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f;
        }

        private void BuildPlayer()
        {
            _rig = new GameObject("Synthetic player rig").transform;
            var cameraObject = new GameObject("Demo camera"); cameraObject.transform.SetParent(_rig, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor; _camera.backgroundColor = new Color(0.43f, 0.65f, 0.82f);
            _camera.nearClipPlane = 0.05f; _camera.farClipPlane = 2200f; _camera.fieldOfView = 75f;
            Material cloth = MakeMaterial(new Color(0.16f, 0.53f, 0.9f));
            Material handle = MakeMaterial(new Color(0.96f, 0.42f, 0.16f));
            _canopy = Shape("Visual canopy", PrimitiveType.Sphere, _rig, new Vector3(0, 4, 0.3f), new Vector3(7, 0.3f, 2), cloth);
            _leftHandle = Shape("Left simulated handle", PrimitiveType.Cube, _rig, Vector3.zero, new Vector3(0.18f, 0.06f, 0.08f), handle);
            _rightHandle = Shape("Right simulated handle", PrimitiveType.Cube, _rig, Vector3.zero, new Vector3(0.18f, 0.06f, 0.08f), handle);
            _leftLine = MakeLine("Left visual rope", -0.35f, handle);
            _rightLine = MakeLine("Right visual rope", 0.35f, handle);
            var board = new GameObject("World-space development HUD"); board.transform.SetParent(_rig, false);
            board.transform.localPosition = new Vector3(-1.35f, 2.4f, 3.5f);
            _board = board.AddComponent<TextMesh>(); _board.anchor = TextAnchor.UpperLeft;
            _board.fontSize = 48; _board.characterSize = 0.027f; _board.color = Color.white;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) { _board.font = font; _board.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
        }

        private LineRenderer MakeLine(string label, float x, Material material)
        {
            var obj = new GameObject(label); obj.transform.SetParent(_rig, false);
            var line = obj.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.positionCount = 2;
            line.startWidth = line.endWidth = 0.012f; line.sharedMaterial = material;
            line.SetPosition(0, new Vector3(x, 3.2f, 0.55f));
            return line;
        }

        private static Transform Shape(string label, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(type); obj.name = label;
            obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale;
            if (material != null) obj.GetComponent<Renderer>().sharedMaterial = material;
            // Visual geometry only; the pure flight model intersects its flat ground.
            Collider collider = obj.GetComponent<Collider>(); if (collider != null) Destroy(collider);
            return obj.transform;
        }

        private Material MakeMaterial(Color color)
        {
            bool srp = GraphicsSettings.currentRenderPipeline != null;
            Shader shader = Resources.Load<Shader>("ClimbSurface");
            if (shader == null) shader = Shader.Find(srp ? "Universal Render Pipeline/Lit" : "Standard");
            if (shader == null) shader = Shader.Find(srp ? "Universal Render Pipeline/Unlit" : "Unlit/Color");
            if (shader == null) { Debug.LogWarning("No demo material shader available; retaining primitive defaults."); return null; }
            var material = new Material(shader) { color = color }; _materials.Add(material); return material;
        }

        private static Vector3 ToUnity(Vec3 value) => new Vector3(value.X, value.Y, value.Z);

        private void OnGUI()
        {
            if (XRSettings.isDeviceActive) return; // World-space TextMesh is the XR-facing status display.
            DrawNavigation();
        }

        private void OnDestroy()
        {
            _trialLog?.Dispose();
            if (_terrainMesh != null) Destroy(_terrainMesh);
            if (_world != null) Destroy(_world.gameObject);
            if (_rig != null) Destroy(_rig.gameObject);
            foreach (Material material in _materials) if (material != null) Destroy(material);
        }
    }
}
