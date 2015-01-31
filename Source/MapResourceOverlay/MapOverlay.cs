using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using KSP.IO;
using UnityEngine;

namespace MapResourceOverlay
{
    public class MapOverlay : ScenarioModule
    {
        private CelestialBody _body;
        private Mesh _mesh;

        private IButton _mapOverlayButton = null;
        private ApplicationLauncherButton _appbutton = null;
        private MapOverlayGui _gui;
        private bool _changed;
        private Coordinates _mouseCoords;
        private CelestialBody _targetBody;

        private ScanSatWrapper _scanSat;
        private Transform _origTransform;

        private Vector2 _mouse;
        private int _toolTipId;
        private int _currentLat;
        [KSPField(isPersistant = true)] public int cutoff = 0;
        [KSPField(isPersistant = true)] public bool bright;
        [KSPField(isPersistant = true)] public bool flighttooltip = false;
        [KSPField(isPersistant = true)] public bool useScansat;
        [KSPField(isPersistant = true)] public bool show = true;
        [KSPField(isPersistant = true)] public bool showTooltip = true;
        [KSPField(isPersistant = true)] public string overlayProviderName = "ResourceOverlayProvider";
        private List<IOverlayProvider> _overlayProviders;
        private IOverlayProvider _overlayProvider;
        private Texture2D _texture;

        public IOverlayProvider OverlayProvider
        {
            get { return _overlayProvider; }
        }

        public int Cutoff
        {
            get { return cutoff; }
            set
            {
                if (cutoff != value)
                {
                    cutoff = value;
                    _changed = true;
                }
            }
        }

        public bool Bright
        {
            get { return bright; }
            set
            {
                if (bright != value)
                {
                    bright = value;
                    _changed = true;
                }
            }
        }

        public bool FlightTooltip
        {
            get { return flighttooltip; }
            set
            {
                if (flighttooltip != value)
                {
                    flighttooltip = value;
                    _changed = true;
                }
            }
        }
        
        public bool UseScansat
        {
            get { return useScansat; }
            set
            {
                if (useScansat != value)
                {
                    useScansat = value;
                    _changed = true;
                }
            }
        }

        public bool Show
        {
            get { return show; }
            set
            {
                if (show != value)
                {
                    show = value;
                    _changed = true;
                }
            }
        }

        public override void OnAwake()
        {
            this.Log("Awaking");
            _origTransform = gameObject.transform.parent;
            var filter = gameObject.AddComponent<MeshFilter>();
            if (filter != null)
            {
                _mesh = filter.mesh;
            }
            else
            {
                _mesh = gameObject.GetComponent<MeshFilter>().mesh;
            }
            gameObject.AddComponent<MeshRenderer>();
            _scanSat = ScanSatWrapper.Instance;
            base.OnAwake();

            if (ToolbarManager.ToolbarAvailable)
            {
                _mapOverlayButton = ToolbarManager.Instance.add("MapResourceOverlay", "ResourceOverlay");
                _mapOverlayButton.TexturePath = "MapResourceOverlay/Assets/MapOverlayIcon.small";
                _mapOverlayButton.ToolTip = "Map Resource Overlay";
                _mapOverlayButton.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
                _mapOverlayButton.OnClick += e => ToggleGui();
            }
            else
            {
                //Stock toolbar
                _texture = new Texture2D(38, 38);
                _texture.LoadImage(
                    System.IO.File.ReadAllBytes(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Assets/MapOverlayIcon.enabled.png"));
                _appbutton = ApplicationLauncher.Instance.AddModApplication(ToggleGui, ToggleGui,
                    null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                    _texture);
                // Prevent duplicates with this callback.
                GameEvents.onGameSceneLoadRequested.Add(ActivateOnSceneChange);
            }
            _toolTipId = new System.Random().Next(65536) + Assembly.GetExecutingAssembly().GetName().Name.GetHashCode() +
                         "tooltip".GetHashCode();
            GameEvents.onHideUI.Add(MakeInvisible);
            GameEvents.onShowUI.Add(MakeVisible);
            _overlayProviders = new List<IOverlayProvider>();
            _targetBody = MapView.MapCamera.target.GetTargetBody();
        }

        public void ToggleGui()
        {
            if (_gui != null)
            {
                _gui.SetVisible(false);
            }
            _gui = new MapOverlayGui(this);
            _gui.SetVisible(true);
        }

        public void OnDisable()
        {
            this.Log("disabling MapOverlay");

        }

        public void OnDestroy()
        {
            // Clean up game objects and buttons
            this.Log("destroying MapResourceOverlay");
            gameObject.transform.parent = _origTransform;
            if (_mapOverlayButton != null)
            {
                _mapOverlayButton.Destroy();
            }
            if (_appbutton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_appbutton);
            }
            if (_gui != null)
            {
                _gui.Model = null;
            }
            Show = false;
            gameObject.renderer.enabled = false;

            GameEvents.onHideUI.Remove(MakeInvisible);
            GameEvents.onShowUI.Remove(MakeVisible);
            GameEvents.onGameSceneLoadRequested.Remove(ActivateOnSceneChange);
            
            OverlayProvider.RedrawRequired -= OverlayProviderOnRedrawRequired;
        }

        public void MakeVisible()
        {
            enabled = true;
        }

        public void MakeInvisible()
        {
            enabled = false;
        }

        public void Start()
        {
            this.Log("MapResourceOverlay starting");
            gameObject.layer = 10;
            
        }


        public bool ShowTooltip
        {
            get { return showTooltip; }
            set { showTooltip = value; }
        }

        public List<IOverlayProvider> OverlayProviders
        {
            get { return _overlayProviders; }
        }


        public void SetOverlayProvider(IOverlayProvider overlayProvider)
        {
            if (OverlayProvider != null)
            {
                OverlayProvider.Deactivate();
            }
            _overlayProvider = overlayProvider;
            overlayProvider.Activate(_targetBody);
            overlayProvider.RedrawRequired += OverlayProviderOnRedrawRequired;
            _changed = true;
            overlayProviderName = overlayProvider.GetType().Name;
        }

        private void OverlayProviderOnRedrawRequired(object sender, EventArgs eventArgs)
        {
            _changed = true;
        }

        public void Update()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT)
            {
                gameObject.renderer.enabled = (false);
            }
            else UpdateMapView();
        }

        private void UpdateMapView()
        {
            if (!show || MapView.MapCamera == null || !MapView.MapIsEnabled)
            {
                gameObject.renderer.enabled = false;
            }
            else
            {
                gameObject.renderer.enabled = true;
                _targetBody = MapView.MapCamera.target.GetTargetBody();

                if (_targetBody != null && (_targetBody != _body || _changed))
                {
                    this.Log("Drawing at " + _targetBody.name + " because " +
                             (_targetBody != _body ? "body changed." : "something else changed."));
                    OverlayProvider.BodyChanged(_targetBody);
                    _changed = false;
                    var dir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var radii = System.IO.File.ReadAllLines(dir + "/Assets/Radii.cfg");
                    var radius = float.Parse(radii.First(x => x.StartsWith(_targetBody.GetName())).Split('=')[1]);
                    _body = _targetBody;
                    CreateMesh(_targetBody);
                    gameObject.renderer.material =
                        new Material(System.IO.File.ReadAllText(dir + "/Assets/MapOverlayShader.txt"));
                    gameObject.renderer.enabled = true;
                    gameObject.renderer.castShadows = false;
                    gameObject.transform.parent =
                        ScaledSpace.Instance.scaledSpaceTransforms.FirstOrDefault(t => t.name == _body.bodyName);
                    gameObject.layer = 10;
                    gameObject.transform.localScale = Vector3.one*1000f*radius;
                    gameObject.transform.localPosition = (Vector3.zero);
                    gameObject.transform.localRotation = (Quaternion.identity);
                }
                if (_targetBody != null && useScansat && _scanSat.Active())
                {
                    RecalculateColors(_targetBody);
                }
            }
        }

        private void RecalculateColors(CelestialBody targetBody)
        {
            const int nbLong = 360;

            #region Vertices

            var colors = _mesh.colors32;

            colors[0] = CalculateColor32ForResourceAt(targetBody, 90, 0);
            for (int lat = _currentLat; lat < _currentLat + 2; lat++)
            {
                for (int lon = 0; lon <= nbLong; lon++)
                {
                    colors[lon + lat*(nbLong + 1) + 1] = CalculateColor32ForResourceAt(targetBody, 90 - lat, lon);
                }
            }
            colors[colors.Length - 1] = CalculateColor32ForResourceAt(targetBody, -90, 0);

            #endregion

            _currentLat += 2;
            if (_currentLat >= 180)
            {
                _currentLat = 0;
            }
            _mesh.colors32 = colors;
        }

        public void OnGUI()
        {
            bool paused = false;
            if (HighLogic.LoadedSceneIsFlight)
            {
                try
                {
                    paused = PauseMenu.isOpen || FlightResultsDialog.isDisplaying;
                }
                catch (Exception)
                {
                    // ignore the error and assume the pause menu is not open
                }
            }
            if (_targetBody != FlightGlobals.ActiveVessel.mainBody || paused || !showTooltip)
                //dont show tooltips on different bodys or ORS lags
            {
                return;
            }
            if (show && _targetBody != null && (MapView.MapIsEnabled || flighttooltip))
            {
                if (Event.current.type == EventType.Layout)
                {
                    try
                    {
                        _mouseCoords = _targetBody.GetMouseCoordinates();
                        _mouse = Event.current.mousePosition;
                        if (useScansat && _scanSat.Active() && _mouseCoords != null &&
                            !OverlayProvider.IsCoveredAt(_mouseCoords.Longitude, _mouseCoords.Latitude, _targetBody))
                        {
                            _mouseCoords = null;
                        }
                    }
                    catch (NullReferenceException e)
                    {
                        this.Log("layout nullref" + e);
                    }
                }
                if (_mouseCoords != null)
                {

                    _toolTipId = 0;
                    var overlayTooltip = OverlayProvider.TooltipContent(_mouseCoords.Latitude, _mouseCoords.Longitude,
                        _targetBody);
                    if (Math.Abs(overlayTooltip.Size.x) < 0.01 && Math.Abs(overlayTooltip.Size.y) < 0.01)
                    {
                        overlayTooltip.Size = new Vector2(200f, 55f);
                    }
                    var style = new GUIStyle(GUI.skin.label) {wordWrap = true};
                    GUI.Window(_toolTipId,
                        new Rect(_mouse.x + 10, _mouse.y + 10, overlayTooltip.Size.x, overlayTooltip.Size.y), i =>
                        {
                            GUI.Label(new Rect(5, 10, 190, 20),
                                "Long: " + _mouseCoords.Longitude.ToString("###.##") + " Lat: " +
                                _mouseCoords.Latitude.ToString("####.##"));
                            GUI.Label(new Rect(5, 30, overlayTooltip.Size.x - 10, overlayTooltip.Size.y - 35),
                                overlayTooltip.Content, style);

                        },
                        overlayTooltip.Title);
                }
            }
        }

        private void CreateMesh(CelestialBody targetBody)
        {

            _mesh.Clear();

            const float radius = 1f;
            // Longitude |||
            const int nbLong = 360;
            // Latitude ---
            const int nbLat = 180;

            #region Vertices

            Vector3[] vertices = new Vector3[(nbLong + 1)*nbLat + 2];
            var colors = new Color32[(nbLong + 1)*nbLat + 2];
            float _pi = Mathf.PI;
            float _2pi = _pi*2f;

            vertices[0] = Vector3.up*radius;
            colors[0] = CalculateColor32ForResourceAt(targetBody, 90, 0);
            for (int lat = 0; lat < nbLat; lat++)
            {
                float a1 = _pi*(lat + 1)/(nbLat + 1);
                float sin1 = Mathf.Sin(a1);
                float cos1 = Mathf.Cos(a1);

                for (int lon = 0; lon <= nbLong; lon++)
                {
                    float a2 = _2pi*(lon == nbLong ? 0 : lon)/nbLong;
                    float sin2 = Mathf.Sin(a2);
                    float cos2 = Mathf.Cos(a2);

                    vertices[lon + lat*(nbLong + 1) + 1] = new Vector3(sin1*cos2, cos1, sin1*sin2)*radius;
                    colors[lon + lat*(nbLong + 1) + 1] = CalculateColor32ForResourceAt(targetBody, 90 - lat, lon);
                }
            }
            vertices[vertices.Length - 1] = Vector3.up*-radius;
            colors[vertices.Length - 1] = CalculateColor32ForResourceAt(targetBody, -90, 0);

            #endregion

            #region Normales

            Vector3[] normales = new Vector3[vertices.Length];
            for (int n = 0; n < vertices.Length; n++)
                normales[n] = vertices[n].normalized;

            #endregion

            #region UVs

            Vector2[] uvs = new Vector2[vertices.Length];
            uvs[0] = Vector2.up;
            uvs[uvs.Length - 1] = Vector2.zero;
            for (int lat = 0; lat < nbLat; lat++)
                for (int lon = 0; lon <= nbLong; lon++)
                    uvs[lon + lat*(nbLong + 1) + 1] = new Vector2((float) lon/nbLong, 1f - (float) (lat + 1)/(nbLat + 1));

            #endregion

            #region Triangles

            int nbFaces = vertices.Length;
            int nbTriangles = nbFaces*2;
            int nbIndexes = nbTriangles*3;
            int[] triangles = new int[nbIndexes];

            //Top Cap
            int i = 0;
            for (int lon = 0; lon < nbLong; lon++)
            {
                triangles[i++] = lon + 2;
                triangles[i++] = lon + 1;
                triangles[i++] = 0;
            }

            //Middle
            for (int lat = 0; lat < nbLat - 1; lat++)
            {
                for (int lon = 0; lon < nbLong; lon++)
                {
                    int current = lon + lat*(nbLong + 1) + 1;
                    int next = current + nbLong + 1;

                    triangles[i++] = next + 1;
                    triangles[i++] = current + 1;
                    triangles[i++] = current;

                    triangles[i++] = next;
                    triangles[i++] = next + 1;
                    triangles[i++] = current;
                }
            }

            //Bottom Cap
            for (int lon = 0; lon < nbLong; lon++)
            {
                triangles[i++] = vertices.Length - 1;
                triangles[i++] = vertices.Length - (lon + 2) - 1;
                triangles[i++] = vertices.Length - (lon + 1) - 1;
            }

            #endregion

            _mesh.vertices = vertices;
            _mesh.normals = normales;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;
            _mesh.colors32 = colors;
            _mesh.RecalculateBounds();
            _mesh.Optimize();
        }

        private Color32 CalculateColor32At(CelestialBody body, double latitude, double longitude)
        {
            return OverlayProvider.CalculateColor32(latitude, longitude, body, useScansat, bright);
        }

        private Color32 CalculateColor32ForResourceAt(CelestialBody body, double latitude, double longitude)
        {
            return CalculateColor32At(body, latitude, longitude);
        }

        #region Save and Load

        public override void OnLoad(ConfigNode node)
        {
            this.Log("loading");
            base.OnLoad(node);

            var type = typeof (IOverlayProvider);
            _overlayProviders = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetLoadableTypes().Where(x => type.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract))
                .Select(x =>
                {
                    try
                    {
                        return Activator.CreateInstance(x) as IOverlayProvider;
                    }
                    catch (Exception e)
                    {
                        this.Log("Couldnt instantiate: "+x.FullName+"\n"+e);
                        return null;
                    }
                })
                .Where(x => x != null)
                .ToList();

            LoadConfig(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Base.cfg");
            var globalSavedConfigFilename = IOUtils.GetFilePathFor(GetType(), "MapResourceOverlay.cfg");
            LoadConfig(globalSavedConfigFilename);
            _overlayProviders = _overlayProviders.Where(x => x.CanActivate()).ToList();
            var provider = OverlayProviders.FirstOrDefault(x => x.GetType().Name == overlayProviderName);
            if (provider == null)
            {
                provider = new BiomeOverlayProvider();
            }
            SetOverlayProvider(provider);
        }


        private void LoadConfig(string filename)
        {
            try
            {
                if (System.IO.File.Exists(filename))
                {
                    var baseNode = ConfigNode.Load(filename);
                    if (baseNode.HasNode("MAP_OVERLAY"))
                    {
                        var globalNode = baseNode.GetNode("MAP_OVERLAY");
                        foreach (var overlayProvider in OverlayProviders)
                        {
                            try
                            {
                                overlayProvider.BodyChanged(_targetBody);
                                overlayProvider.Load(globalNode);
                            }
                            catch (Exception e)
                            {
                                this.Log("OverlayProvider " + overlayProvider.GetType().FullName + " couldnt load " + e);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.Log("Could not load "+filename+".\n"+e);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            this.Log("saving");
            base.OnSave(node);
            var baseNode = new ConfigNode();
            
            var globalNode = baseNode.AddNode("MAP_OVERLAY");
            foreach (var overlayProvider in OverlayProviders)
            {
                overlayProvider.Save(globalNode);
            }
            baseNode.Save(IOUtils.GetFilePathFor(GetType(), "MapResourceOverlay.cfg"));
        }

        #endregion

        public void Reload()
        {
            _changed = true;
        }
        
        public void ActivateOnSceneChange(GameScenes _scene)
        {
            ApplicationLauncher.Instance.RemoveModApplication(_appbutton);
        }
    }
}
