﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using KSP.IO;
using Toolbar;
using UnityEngine;
using OpenResourceSystem;

namespace MapResourceOverlay
{
    public class MapOverlay : ScenarioModule
    {
        private CelestialBody _body;
        private Mesh _mesh;

        private List<ResourceConfig> _resources;

        private IButton _mapOverlayButton;
        private MapOverlayGui _gui;
        private bool _changed;
        private ResourceConfig _selectedResourceName;
        private Coordinates _mouseCoords;
        private CelestialBody _targetBody;

        private ScanSatWrapper _scanSat;
        private Transform _origTransform;

        private Vector2 _mouse;
        private int _toolTipId;
        private int _currentLat;
        [KSPField(isPersistant = true)] public int cutoff = 20;
        [KSPField(isPersistant = true)] public bool bright;
        [KSPField(isPersistant = true)] public bool useScansat;
        [KSPField(isPersistant = true)] public bool show = true;
        [KSPField(isPersistant = true)] public bool showTooltip = true;
        [KSPField(isPersistant = true)] public string overlayProviderName = "MapResourceOverlay.ResourceOverlayProvider";
        private GlobalSettings _globalSettings;
        private List<IOverlayProvider> _overlayProviders;

        public IOverlayProvider OverlayProvider { get; private set; }        
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
            set {
                if (bright != value)
                {
                    bright = value; _changed = true;
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
            _globalSettings = new GlobalSettings();
            gameObject.AddComponent<MeshRenderer>();
            _scanSat = ScanSatWrapper.Instance;
            base.OnAwake();
            _mapOverlayButton = ToolbarManager.Instance.add("MapResourceOverlay", "ResourceOverlay");
            _mapOverlayButton.TexturePath = "MapResourceOverlay/Assets/MapOverlayIcon";
            _mapOverlayButton.ToolTip = "Map Resource Overlay";
            _mapOverlayButton.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
            _mapOverlayButton.OnClick += e => ToggleGui();
            _toolTipId = new System.Random().Next(65536) + Assembly.GetExecutingAssembly().GetName().Name.GetHashCode() + "tooltip".GetHashCode();
            GameEvents.onHideUI.Add(MakeInvisible);
            GameEvents.onShowUI.Add(MakeVisible);
            _overlayProviders = new List<IOverlayProvider>();
            
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
            
            this.Log("destroying MapResourceOverlay");
            gameObject.transform.parent = _origTransform;
            _mapOverlayButton.Destroy();
            if (_gui != null)
            {
                _gui.Model = null;
            }
            Show = false;
            gameObject.renderer.enabled = false;
            
            GameEvents.onHideUI.Remove(MakeInvisible);
            GameEvents.onShowUI.Remove(MakeVisible);
            
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
            SelectedResourceName = Resources[0];
            var type = typeof(IOverlayProvider);
            _overlayProviders = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetLoadableTypes().Where(x => type.IsAssignableFrom(x) && !x.IsInterface))
                .Select(x=> Activator.CreateInstance(x) as IOverlayProvider)
                .Where(x => x != null)
                .ToList();
            var provider = OverlayProviders.FirstOrDefault(x => x.GetType().FullName == overlayProviderName);
            if (provider == null)
            {
                OverlayProvider = new ResourceOverlayProvider();
            }
            OverlayProvider = provider;
        }

        public List<ResourceConfig> Resources
        {
            get { return _resources; }
        }

        public ResourceConfig SelectedResourceName
        {
            get { return _selectedResourceName; }
            set
            {
                _selectedResourceName = value;
                _changed = true;
            }
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
            OverlayProvider = overlayProvider;
            _changed = true;
            overlayProviderName = overlayProvider.GetType().FullName;
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
            if (!show || MapView.MapCamera == null)
            {
                gameObject.renderer.enabled = false;
            }
            else
            {
                gameObject.renderer.enabled = true;
                _targetBody = MapView.MapCamera.target.GetTargetBody();

                if (_targetBody != null && (_targetBody != _body || _changed))
                {
                    this.Log("Drawing at " + _targetBody.name + " because " + (_targetBody != _body ? "body changed." : "something else changed."));
                    _changed = false;
                    var dir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var radii = System.IO.File.ReadAllLines(dir + "/Assets/Radii.cfg");
                    var radius = float.Parse(radii.First(x => x.StartsWith(_targetBody.GetName())).Split('=')[1]);
                    _body = _targetBody;
                    CreateMesh(_targetBody, SelectedResourceName);
                    gameObject.renderer.material = new Material(System.IO.File.ReadAllText(dir + "/Assets/MapOverlayShader.txt"));
                    gameObject.renderer.enabled = true;
                    gameObject.renderer.castShadows = false;
                    gameObject.transform.parent = ScaledSpace.Instance.scaledSpaceTransforms.FirstOrDefault(t => t.name == _body.bodyName);
                    gameObject.layer = 10;
                    gameObject.transform.localScale = Vector3.one * 1000f * radius;
                    gameObject.transform.localPosition = (Vector3.zero);
                    gameObject.transform.localRotation = (Quaternion.identity);
                }
                if (_targetBody != null && useScansat && _scanSat.Active())
                {
                    RecalculateColors(_targetBody, SelectedResourceName);
                }
            }
        }

        private void RecalculateColors(CelestialBody targetBody, ResourceConfig resource)
        {
            const int nbLong = 360;
            #region Vertices
            var colors = _mesh.colors32;

            colors[0] = CalculateColor32ForResourceAt(targetBody, resource, 90, 0);
            for (int lat = _currentLat; lat < _currentLat + 2; lat++)
            {
                for (int lon = 0; lon <= nbLong; lon++)
                {
                    colors[lon + lat * (nbLong + 1) + 1] = CalculateColor32ForResourceAt(targetBody, resource, 90 - lat, lon);
                }
            }
            colors[colors.Length - 1] = CalculateColor32ForResourceAt(targetBody, resource, -90, 0);
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
            if (_targetBody != FlightGlobals.ActiveVessel.mainBody || paused || !showTooltip) //dont show tooltips on different bodys or ORS lags
            {
                return;
            }
            if (show && _targetBody != null)
            {
                if (Event.current.type == EventType.Layout)
                {
                    try
                    {
                        _mouseCoords = _targetBody.GetMouseCoordinates();
                        _mouse = Event.current.mousePosition;
                        if (useScansat && _scanSat.Active() && _mouseCoords != null &&
                            !_scanSat.IsCovered(_mouseCoords.Longitude, _mouseCoords.Latitude, _targetBody,_selectedResourceName.Resource))
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
                        _targetBody, SelectedResourceName);
                    
                    GUI.Window(_toolTipId, new Rect(_mouse.x + 10, _mouse.y + 10, 200f, 55f), i =>
                    {
                        GUI.Label(new Rect(5, 10, 190, 20), "Long: " + _mouseCoords.Longitude.ToString("###.##") + " Lat: " + _mouseCoords.Latitude.ToString("####.##"));
                        GUI.Label(new Rect(5, 30, 190, 20), overlayTooltip.Content);

                    },
                    overlayTooltip.Title);
                }
            }
        }

        private void CreateMesh(CelestialBody targetBody, ResourceConfig resource)
        {

            _mesh.Clear();

            const float radius = 1f;
            // Longitude |||
            const int nbLong = 360;
            // Latitude ---
            const int nbLat = 180;

            #region Vertices
            Vector3[] vertices = new Vector3[(nbLong + 1) * nbLat + 2];
            var colors = new Color32[(nbLong + 1) * nbLat + 2];
            float _pi = Mathf.PI;
            float _2pi = _pi * 2f;

            vertices[0] = Vector3.up * radius;
            colors[0] = CalculateColor32ForResourceAt(targetBody, resource, 90, 0);
            for (int lat = 0; lat < nbLat; lat++)
            {
                float a1 = _pi * (lat + 1) / (nbLat + 1);
                float sin1 = Mathf.Sin(a1);
                float cos1 = Mathf.Cos(a1);

                for (int lon = 0; lon <= nbLong; lon++)
                {
                    float a2 = _2pi * (lon == nbLong ? 0 : lon) / nbLong;
                    float sin2 = Mathf.Sin(a2);
                    float cos2 = Mathf.Cos(a2);

                    vertices[lon + lat * (nbLong + 1) + 1] = new Vector3(sin1 * cos2, cos1, sin1 * sin2) * radius;
                    colors[lon + lat * (nbLong + 1) + 1] = CalculateColor32ForResourceAt(targetBody, resource, 90 - lat, lon);
                }
            }
            vertices[vertices.Length - 1] = Vector3.up * -radius;
            colors[vertices.Length - 1] = CalculateColor32ForResourceAt(targetBody, resource, -90, 0);
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
                    uvs[lon + lat * (nbLong + 1) + 1] = new Vector2((float)lon / nbLong, 1f - (float)(lat + 1) / (nbLat + 1));
            #endregion

            #region Triangles
            int nbFaces = vertices.Length;
            int nbTriangles = nbFaces * 2;
            int nbIndexes = nbTriangles * 3;
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
                    int current = lon + lat * (nbLong + 1) + 1;
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

        private Color32 CalculateColor32At(CelestialBody body, ResourceConfig resource, double latitude,double longitude)
        {
            return OverlayProvider.CalculateColor32(latitude, longitude, body, resource, useScansat, bright, cutoff);
        }

        private Color32 CalculateColor32ForResourceAt(CelestialBody body, ResourceConfig resource, double latitude,double longitude)
        {
            return CalculateColor32At(body, resource, latitude, longitude);
        }

        #region Save and Load

        public override void OnLoad(ConfigNode node)
        {
            this.Log("loading");
            base.OnLoad(node);
            var globalConfigFilename = IOUtils.GetFilePathFor(GetType(), "MapResourceOverlay.cfg");
            if (File.Exists<MapOverlay>(globalConfigFilename))
            {
                var globalNode = ConfigNode.Load(globalConfigFilename);
                _globalSettings.Load(globalNode);
            }
            _resources = _globalSettings.ColorConfigs;
        }

        public override void OnSave(ConfigNode node)
        {
            this.Log("saving");
            base.OnSave(node);
            var gloablNode = new ConfigNode();
            _globalSettings.Save(gloablNode);
            gloablNode.Save(IOUtils.GetFilePathFor(GetType(), "MapResourceOverlay.cfg"));

        }

        #endregion

    }
}
