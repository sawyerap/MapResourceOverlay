using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using LibNoise.Unity.Operator;
using UnityEngine;
using Object = System.Object;

namespace MapResourceOverlay
{
    public class ResourceOverlayProvider : OverlayProviderBase
    {
        private ResourceConfig _activeResource;
        private double _displayMax;
      
        // REGOLITH 1.5.4

        private delegate float GetRegolithAbundance(double lat, double lon, string resource, int body, int type, int altitude, bool biomes);

		private static GetRegolithAbundance _getRegolithAbundance;
        
        // End Regolith material
        
        private bool _logaritmic;
        private bool _coloredScale;
        private bool _exponential;
        private bool _biomelock = false;

        public ResourceConfig ActiveResource
        {
            get { return _activeResource; }
            set
            {
                _activeResource = value;
                CalculateBase();
                RequiresRedraw();
            }
        }

        public override void Activate(CelestialBody body)
        {
            base.Activate(body);
            CalculateBase();
            RequiresRedraw();
        }

        private void CalculateBase()
        {
            _displayMax = 0;
            double avg = 0;
            for (int lat = 0; lat < 180; lat++)
            {
                for (int lon = 0; lon < 360; lon++)
                {
                    var amount = (double)(_getRegolithAbundance(lat, lon, 
						ActiveResource.Resource.ResourceName, _body.flightGlobalsIndex, 0, 0, _biomelock));
                    amount *= 100;
                    if (amount > _displayMax)
                    {
                        _displayMax = amount;
                    }
                    avg += amount;
                }
            }
            _displayMax *= 2;
            avg = avg/(180*360);
        }

        public override Color32 CalculateColor32(double latitude, double longitude, CelestialBody body, bool useScansat, bool bright)
        {
            var scanSat = ScanSatWrapper.Instance;
            if (useScansat && scanSat.Active() && !scanSat.IsCovered(longitude, latitude, body, ActiveResource.Resource))
            {
                return new Color32(0, 0, 0, 0);
            }
			var amount = (double)_getRegolithAbundance(latitude, longitude, 
                ActiveResource.Resource.ResourceName, body.flightGlobalsIndex, 0, 0, _biomelock);
            amount *= 100;
            if (amount > ActiveResource.Cutoff)
            {
                if (Exponential)
                {
                    amount = ((Math.Pow(amount, 2) - (Math.Pow(ActiveResource.Cutoff, 2))) * 255) / ((Math.Pow(_displayMax, 2) - (Math.Pow(ActiveResource.Cutoff, 2))));
                    if (ColoredScale)
                    {
                        var color = ScanSatWrapper.heightToColor((float)amount, 0, 255);
                        color.a = ActiveResource.HighColor.a;
                        return color;
                    }
                }
                else if (Logaritmic)
                {
                    if (ActiveResource.Cutoff < 1)
                    {
                        ActiveResource.Cutoff = 1;
                    }
                    amount = ((Math.Log(amount, 2) - (Math.Log(ActiveResource.Cutoff, 2))) * 255) / ((Math.Log(_displayMax, 2) - (Math.Log(ActiveResource.Cutoff, 2))));
                    if (ColoredScale)
                    {
                        var color = ScanSatWrapper.heightToColor((float)amount*255, 0, 255f);
                        color.a = ActiveResource.HighColor.a;
                        return color;
                    }
                }
                else
                {
                    if (ColoredScale)
                    {
                        var color = ScanSatWrapper.heightToColor((float)amount, ActiveResource.Cutoff, (float)_displayMax);
                        color.a = ActiveResource.HighColor.a;
                        return color;
                    }
                    amount = ((amount - ActiveResource.Cutoff) * 255) / (_displayMax - ActiveResource.Cutoff);
                }
                amount = Mathf.Clamp((float) amount, 0f, 255f);
                if (!bright)
                {
                    var r = amount*(ActiveResource.HighColor.r/255.0);
                    var g = amount*(ActiveResource.HighColor.g/255.0);
                    var b = amount*(ActiveResource.HighColor.b/255.0);
                    return new Color32(Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b),
                        ActiveResource.HighColor.a);
                }
                else
                {
                    return new Color32(155, Convert.ToByte(amount), Convert.ToByte(amount), 150);
                }
            }
            return ActiveResource.LowColor;
        }

        public bool Logaritmic
        {
            get { return _logaritmic; }
            set
            {
                if (_logaritmic!= value)
                {
                    _logaritmic = value;
                    RequiresRedraw();
                    if (_logaritmic)
                    {
                        _exponential = false;
                    }
                }
            }
        }

        public override OverlayTooltip TooltipContent(double latitude, double longitude, CelestialBody body)
        {
            var abundance = (double)_getRegolithAbundance(latitude, longitude, 
                ActiveResource.Resource.ResourceName, body.flightGlobalsIndex, 0, 0, _biomelock);
			abundance *= 100;
            return new OverlayTooltip(ActiveResource.Resource.ResourceName,
                new GUIContent("Amount: " + (abundance).ToString("0.0") + "%"));
        }

        public override bool IsCoveredAt(double latitude, double longitude, CelestialBody body)
        {
            return ScanSatWrapper.Instance.IsCovered(longitude, latitude, body, ActiveResource.Resource);
        }

        public override string GuiName
        {
            get { return "Resource Map"; }
        }

        public List<ResourceConfig> ColorConfigs { get; set; }

        private void LoadFailsafe()
        {
            var config = new ResourceConfig
            {
                Resource = new Resource("Karbonite"),
                LowColor = new Color32(0, 0, 0, 0),
                HighColor = new Color32(255, 0, 0, 200)
            };
            var config2 = new ResourceConfig
            {
                Resource = new Resource("Ore"),
                LowColor = new Color32(0, 0, 0, 0),
                HighColor = new Color32(0, 255, 0, 200)
            };
            var config3 = new ResourceConfig
            {
                Resource = new Resource("Water"),
                LowColor = new Color32(0, 0, 0, 0),
                HighColor = new Color32(0, 0, 255, 200)
            };
            var config4 = new ResourceConfig
            {
                Resource = new Resource("Minerals"),
                LowColor = new Color32(0, 0, 0, 0),
                HighColor = new Color32(0, 255, 255, 200)
            };
            var config5 = new ResourceConfig
            {
                Resource = new Resource("Substrate"),
                LowColor = new Color32(0, 0, 0, 0),
                HighColor = new Color32(255, 0, 255, 200)
            };
            var config6 = new ResourceConfig
            {
                Resource = new Resource("Uraninite"),
                LowColor = new Color32(0, 0, 0, 0),
                HighColor = new Color32(255, 255, 0, 200)
            };
            ColorConfigs = new List<ResourceConfig> { config, config2, config3, config4, config5, config6 };
            ActiveResource = config;
        }

        public override void Load(ConfigNode node)
        {
            try
            {
                InitiateRegolith();
            }
            catch (Exception e)
            {
                this.Log("Couldnt find Regolith because: " + e);
            }
            try
            {
                if (node.HasNode("ResourceOverlay"))
                {
                    var globalSettingsNode = node.GetNode("ResourceOverlay");
                    ActiveResource = ResourceConfig.Load(globalSettingsNode.GetNode("ActiveResource").nodes[0]);
                    var colorConfigsNode = globalSettingsNode.GetNode("colorConfigs");
                    ColorConfigs = new List<ResourceConfig>();
                    foreach (ConfigNode value in colorConfigsNode.nodes)
                    {
                        ColorConfigs.Add(ResourceConfig.Load(value));
                    }
                }
            }
            catch (Exception e)
            {
                this.Log("Could not load config, using default, because " + e);
                LoadFailsafe();
            }
        }

        public override void Save(ConfigNode node)
        {
            var globalSettingsNode = node.AddNode("ResourceOverlay");
            ActiveResource.Save(globalSettingsNode.AddNode("ActiveResource"));
            var colorConfigsNode = globalSettingsNode.AddNode("colorConfigs");
            foreach (var colorConfig in ColorConfigs)
            {
                colorConfig.Save(colorConfigsNode);
            }
        }

        public override bool CanActivate()
        {
            //this.Log("?"+ (_getRegolithAbundance != null));
            return _getRegolithAbundance != null;
        }

        private void InitiateRegolith()
        {
            var RegolithType = AssemblyLoader.loadedAssemblies.SelectMany(
                x => x.assembly.GetExportedTypes())
				.FirstOrDefault(x => x.FullName == "Regolith.Common.RegolithResourceMap");
            
            //this.Log("CanActivateRego Type ?"+ (RegolithType != null));
            
            if (RegolithType != null)
            {
                MethodInfo RegolithMethod = RegolithType.GetMethod("GetAbundance", new Type[] { typeof(double), typeof(double), typeof(string), typeof(int), typeof(int), typeof(int), typeof(bool) });
                
                //this.Log("CanActivateRego RegolithMethod ?"+ (RegolithType != null));
                
                _getRegolithAbundance = (GetRegolithAbundance)Delegate.CreateDelegate(typeof (GetRegolithAbundance), RegolithMethod);
                
                //this.Log("CanActivateRego _getRegolithAbundance ?"+ (_getRegolithAbundance != null));
            }
        }

        public override void DrawGui(MapOverlayGui gui)
        {
            base.DrawGui(gui);
            if (GUILayout.Button("Resource Options"))
            {
                new ResourceOverlayView(this);
            }
            
        }

        public bool Exponential
        {
            get { return _exponential; }
            set
            {
                if (_exponential != value)
                {
                    _exponential = value; 
                    RequiresRedraw();
                    if (_exponential)
                    {
                        _logaritmic = false;
                    }
                }
                
                
            }
        }

        public bool ColoredScale
        {
            get { return _coloredScale; }
            set
            {
                if (_coloredScale != value)
                {
                    _coloredScale = value;
                    RequiresRedraw();
                }
                
                
            }
        }
        
        public bool BiomeLock
        {
            get { return _biomelock; }
            set
            {
                if (_biomelock != value)
                {
                    _biomelock = value;
                    RequiresRedraw();
                }
                
                
            }
        }
    }

    public class ResourceOverlayView : Window<ResourceOverlayView>
    {
        private readonly ResourceOverlayProvider _model;

        public ResourceOverlayView(ResourceOverlayProvider model) : base("Resource Overlay Options", 300, 400)
        {
            _model = model;
            SetVisible(true);
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();
            _model.Logaritmic = GUILayout.Toggle(_model.Logaritmic, "Logarithmic Scale");
            //Removed Exponential until more work done.
            //_model.Exponential = GUILayout.Toggle(_model.Exponential, "Exponential Scale");
            _model.ColoredScale = GUILayout.Toggle(_model.ColoredScale, "Colored Scale");
            // Override Biome Lock option for Regolith
            _model.BiomeLock = GUILayout.Toggle(_model.BiomeLock, "Biome Sample Lock");
            
            
            GUILayout.Space(15);
            foreach (var res in _model.ColorConfigs)
            {
                var style = new GUIStyle(GUI.skin.button);
                if (res == _model.ActiveResource)
                {
                    style.normal.textColor = Color.yellow;
                }
                if (GUILayout.Button(res.Resource.ResourceName, style))
                {
                    _model.ActiveResource = res;
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label("low Cuttoff %: ");
                int temp;
                var cutoff = GUILayout.TextField(res.Cutoff.ToString());
                GUILayout.EndHorizontal();
                bool changed = cutoff != res.Cutoff.ToString() && res == _model.ActiveResource;
                if (Int32.TryParse(cutoff, out temp))
                {
                    res.Cutoff = temp;
                }
                else if (cutoff == "")
                {
                    res.Cutoff = 0;
                }
                if (changed)
                {
                    _model.ActiveResource = res;
                }
            }
            GUILayout.EndVertical();
        }
    }
}