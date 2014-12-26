using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MapResourceOverlay
{
    public interface IOverlayProvider :IConfigNode
    {
        Color32 CalculateColor32(double latitude, double longitude, CelestialBody body, bool useScansat, bool bright);
        OverlayTooltip TooltipContent(double latitude, double longitude, CelestialBody body);
        bool IsCoveredAt(double latitude, double longitude, CelestialBody body);
        string GuiName { get; }
        void Activate(CelestialBody body);
        void Deactivate();
        event EventHandler RedrawRequired;
        void DrawGui(MapOverlayGui gui);
        bool CanActivate();
        void BodyChanged(CelestialBody body);
    }

    public class OverlayTooltip
    {
        public OverlayTooltip(string title, GUIContent content, Vector2 size = new Vector2())
        {
            Title = title;
            Content = content;
            Size = size;
        }

        public string Title { get; set; }
        public GUIContent Content { get; set; }
        public Vector2 Size { get; set; }
    }
    public abstract class OverlayProviderBase : IOverlayProvider
    {
        protected CelestialBody _body;

        public virtual void Load(ConfigNode node)
        {
            
        }

        public virtual void Save(ConfigNode node)
        {
            
        }

        public virtual Color32 CalculateColor32(double latitude, double longitude, CelestialBody body, bool useScansat, bool bright)
        {
            return new Color32();
        }

        public virtual OverlayTooltip TooltipContent(double latitude, double longitude, CelestialBody body)
        {
            return new OverlayTooltip("",new GUIContent());
        }

        public virtual bool IsCoveredAt(double latitude, double longitude, CelestialBody body)
        {
            return true;
        }

        protected virtual void RequiresRedraw()
        {
            if (RedrawRequired != null)
            {
                RedrawRequired(this, null);
            }
        }

        public virtual string GuiName { get; private set; }
        public virtual void Activate(CelestialBody body)
        {
            _body = body;
            RequiresRedraw();
        }

        public virtual void Deactivate()
        {
            _body = null;
        }

        public event EventHandler RedrawRequired;
        public virtual void DrawGui(MapOverlayGui gui)
        {
            
        }

        public virtual bool CanActivate()
        {
            return true;
        }

        public virtual void BodyChanged(CelestialBody body)
        {
            _body = body;
        }
    }

    class SlopeMapProvider : OverlayProviderBase
    {
        private double _max = 800;
        private double _accuracy = 0.5;
        //private double[,] _arr;

        public override Color32 CalculateColor32(double latitude, double longitude, CelestialBody body, bool useScansat, bool bright)
        {
            var height = ScanSatWrapper.GetElevation(body, longitude, latitude);
            var list = new List<double>
            {
                ScanSatWrapper.GetElevation(body, longitude, latitude + _accuracy),
                ScanSatWrapper.GetElevation(body, longitude, latitude - _accuracy),
                ScanSatWrapper.GetElevation(body, longitude - _accuracy, latitude),
                ScanSatWrapper.GetElevation(body, longitude + _accuracy, latitude)
            };
            var slope = list.Select(x => height - x).Max();

            ////TODO: switch over
            //var height = _arr[ArrayLatitudeIndex(latitude), ArrayLongitudeIndex(longitude)];
            //var list = new List<double>
            //{
            //    _arr[ArrayLatitudeIndex(latitude),ArrayLongitudeIndex(longitude+_accuracy)],
            //    _arr[ArrayLatitudeIndex(latitude),ArrayLongitudeIndex(longitude-_accuracy)],
            //    _arr[ArrayLatitudeIndex(latitude+_accuracy),ArrayLongitudeIndex(longitude )],
            //    _arr[ArrayLatitudeIndex(latitude-_accuracy),ArrayLongitudeIndex(longitude)],

            //    _arr[ArrayLatitudeIndex(latitude+0.5*Math.PI*_accuracy),ArrayLongitudeIndex(longitude+0.5*Math.PI*_accuracy)],
            //    _arr[ArrayLatitudeIndex(latitude-0.5*Math.PI*_accuracy),ArrayLongitudeIndex(longitude-0.5*Math.PI*_accuracy)],
            //    _arr[ArrayLatitudeIndex(latitude+0.5*Math.PI*_accuracy),ArrayLongitudeIndex(longitude-0.5*Math.PI*_accuracy)],
            //    _arr[ArrayLatitudeIndex(latitude-0.5*Math.PI*_accuracy),ArrayLongitudeIndex(longitude+0.5*Math.PI*_accuracy)]
            //};
            //var slope = list.Select(x => height -x ).Max();
            var byteColor = Convert.ToByte(Mathf.Clamp((float) slope*255/(float)_max,    0, 255));
            return new Color32(byteColor,byteColor,byteColor,255);
        }

        private int ArrayIndex(double latitude, double longitude)
        {
            latitude =Math.Abs(latitude -90);
            longitude += 180;
            return (int) (Math.Round(longitude*10, 1) + Math.Round(latitude*10, 1)*(3600 + 1) + 1);
        }

        private int ArrayLongitudeIndex(double longitude)
        {
            return (int)Math.Abs((longitude*10)%3600);
        }

        private int ArrayLatitudeIndex(double latitude)
        {
            return (int) Math.Abs((Math.Abs(latitude - 90)*10)%1800);
        }

        public override OverlayTooltip TooltipContent(double latitude, double longitude, CelestialBody body)
        {
            var height = ScanSatWrapper.GetElevation(body, longitude, latitude);
            var list = new List<double>
            {
                ScanSatWrapper.GetElevation(body, longitude, latitude + _accuracy),
                ScanSatWrapper.GetElevation(body, longitude, latitude - _accuracy),
                ScanSatWrapper.GetElevation(body, longitude - _accuracy, latitude),
                ScanSatWrapper.GetElevation(body, longitude + _accuracy, latitude)
            };
            var slope = list.Select(x => height - x).Max();

            ////TODO: switch over
            //this.Log("lat: " + latitude + " lon: " + longitude + " lat index: " + ArrayLatitudeIndex(latitude) + " long index: " + ArrayLongitudeIndex(longitude));
            //var height = _arr[ArrayLatitudeIndex(latitude), ArrayLongitudeIndex(longitude)];
            //this.Log("height: " + height);
            //var list = new List<double>
            //{
            //    _arr[ArrayLatitudeIndex(latitude),ArrayLongitudeIndex(longitude+_accuracy)],
            //    _arr[ArrayLatitudeIndex(latitude),ArrayLongitudeIndex(longitude-_accuracy)],
            //    _arr[ArrayLatitudeIndex(latitude+_accuracy),ArrayLongitudeIndex(longitude )],
            //    _arr[ArrayLatitudeIndex(latitude-_accuracy),ArrayLongitudeIndex(longitude)],

            //    _arr[ArrayLatitudeIndex(latitude+0.5*Math.PI*_accuracy),ArrayLongitudeIndex(longitude+0.5*Math.PI*_accuracy)],
            //    _arr[ArrayLatitudeIndex(latitude-0.5*Math.PI*_accuracy),ArrayLongitudeIndex(longitude-0.5*Math.PI*_accuracy)],
            //    _arr[ArrayLatitudeIndex(latitude+0.5*Math.PI*_accuracy),ArrayLongitudeIndex(longitude-0.5*Math.PI*_accuracy)],
            //    _arr[ArrayLatitudeIndex(latitude-0.5*Math.PI*_accuracy),ArrayLongitudeIndex(longitude+0.5*Math.PI*_accuracy)]
            //};
            //var slope = list.Select(x => height - x).Max();
            //this.Log("index: " + ArrayIndex(latitude, longitude) +" slope: "+slope+ "height :" + height + " others: " + list.Aggregate("", (x, y) => x + "\n height: " + y));

            
            return new OverlayTooltip("",new GUIContent(slope.ToString()));
        }

        public override void Activate(CelestialBody body)
        {
            ////TODO: enable this
            //var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //if (!File.Exists(dir+"/testfile.txt"))
            //{
            //    using (var stream = new BinaryWriter(File.Open(dir + "/testfile.txt", FileMode.OpenOrCreate)))
            //    {
            //        for (int lat = 0; lat < 1800; lat++)
            //        {
            //            for (int lon = 0; lon < 3601; lon++)
            //            {
            //                stream.Write(ScanSatWrapper.GetElevation(body, lon/10.0, 90.0-lat/10.0));
            //            }
            //        }
            //    }
            //}

            //using (var stream = new BinaryReader(File.Open(dir + "/testfile.txt", FileMode.Open)))
            //{
            //    _arr = new double[1800,3601];

            //    for (int i = 0; i < 1800; i++)
            //    {
            //        for (int lon = 0; lon < 3601; lon++)
            //        {
            //            _arr[i,lon] = stream.ReadDouble();
            //        }
            //    }
            //}
        }

        public override string GuiName
        {
            get { return "Slope Map"; }
        }

        public override void DrawGui(MapOverlayGui gui)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Accuracy");
            double temp;
            var cutoff = GUILayout.TextField(_accuracy.ToString());
            GUILayout.EndHorizontal();
            if (Double.TryParse(cutoff, out temp))
            {
                _accuracy = temp;
            }
            else if (cutoff == "")
            {
                _accuracy = 0;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Max slope");
            double temp2;
            var cutoff2 = GUILayout.TextField(_max.ToString());
            GUILayout.EndHorizontal();
            if (Double.TryParse(cutoff2, out temp2))
            {
                _max = temp2;
            }
            else if (cutoff == "")
            {
                _max = 0;
            }
        }
    }

    public class AnomalyMapProvider : OverlayProviderBase
    {
        private Dictionary<Coordinates, PQSCity> _pqsCities;

        public override void Activate(CelestialBody body)
        {
            base.Activate(body);
            CalculateAnomalies();
        }

        public override void BodyChanged(CelestialBody body)
        {
            base.BodyChanged(body);
            CalculateAnomalies();
        }

        private void CalculateAnomalies()
        {
            _pqsCities = new Dictionary<Coordinates,PQSCity>();
            PQSCity[] sites = _body.GetComponentsInChildren<PQSCity>(true);
            foreach (var pqsCity in sites)
            {
                _pqsCities[new Coordinates(_body.GetLatitude(pqsCity.transform.position),
                        _body.GetLongitude(pqsCity.transform.position))] = pqsCity;
                
            }
            //this.Log(_pqsCities.Aggregate("",(x,y) => x +"    "+ y.Value.name +" lat: "+y.Key.Latitude+" long: "+y.Key.Longitude));
        }

        public override Color32 CalculateColor32(double latitude, double longitude, CelestialBody body, bool useScansat, bool bright)
        {
            var anoms = _pqsCities.Keys.Where(x => x.Distance(new Coordinates(latitude, Utilities.ClampDegrees(longitude))) < 2).ToList();
            if (anoms.Count > 0)
            {
                return new Color32(0,255,255,255);
            }
            return new Color32(0,0,0,0);
        }

        public override OverlayTooltip TooltipContent(double latitude, double longitude, CelestialBody body)
        {
            var anoms = _pqsCities.Keys.Where(x => x.Distance(new Coordinates(latitude, Utilities.ClampDegrees(longitude))) < 2).ToList();
            if (anoms.Count > 0)
            {
                var anom = _pqsCities[anoms.First()];
                return new OverlayTooltip(anom.name,new GUIContent(""));
            }
            return base.TooltipContent(latitude, longitude, body);
        }


        public override string GuiName
        {
            get { return "Anomaly Map"; }
        }
    }



    class HeightmapProvider : OverlayProviderBase
    {
        public byte Alpha { get; set; }

        public override Color32 CalculateColor32(double latitude, double longitude, CelestialBody body, bool useScansat, bool bright)
        {
            if (useScansat && ScanSatWrapper.Instance.Active() && !IsCoveredAt(latitude, longitude, body))
            {
                return new Color32(0, 0, 0, 0);
            }
            var scanSat = ScanSatWrapper.Instance;
            var color = scanSat.GetElevationColor32(body, longitude, latitude);
            color.a = Alpha;
            return color;
        }

        public override OverlayTooltip TooltipContent(double latitude, double longitude, CelestialBody body)
        {
            return new OverlayTooltip("",new GUIContent("Height: "+ScanSatWrapper.GetElevation(body, longitude, latitude)+"m"));
        }

        public override bool IsCoveredAt(double latitude, double longitude, CelestialBody body)
        {
            return ScanSatWrapper.Instance.IsCovered(longitude, latitude, body, "AltimetryHiRes");
        }

        public override string GuiName { get { return "Height Map"; } }

        public override void Load(ConfigNode node)
        {
            try
            {
                if (!node.HasNode("Heightmap"))
                {
                    Alpha = 100;
                    return;
                }
                var myNode = node.GetNode("Heightmap");
                byte result;
                if (!Byte.TryParse(myNode.GetValue("Alpha"),out result))
                {
                    result = 100;
                }
                Alpha = result;
            }
            catch (Exception e)
            {
                this.Log("Couldnt Load " + GetType().FullName + " " + e);
            }
        }

        public override void Save(ConfigNode node)
        {
            var myNode = node.AddNode("Heightmap");
            myNode.AddValue("Alpha",Alpha);
        }
    }

    class BiomeOverlayProvider : OverlayProviderBase
    {
        public byte Alpha { get; set; }
        public override Color32 CalculateColor32(double latitude, double longitude, CelestialBody body, bool useScansat, bool bright)
        {
            if (useScansat && ScanSatWrapper.Instance.Active() && !IsCoveredAt(latitude, longitude, body))
            {
                return new Color32(0,0,0,0);
            }
            var scanSat = ScanSatWrapper.Instance;
            var biome = scanSat.GetBiome(longitude, latitude, body);
            if (biome != null)
            {
                Color32 color = biome.mapColor;
                color.a = Alpha;
                return color;
            }
            return new Color32(0, 0, 0, 0);
        }

        public override OverlayTooltip TooltipContent(double latitude, double longitude, CelestialBody body)
        {
            var scanSat = ScanSatWrapper.Instance;
            var biome = scanSat.GetBiome(longitude, latitude, body);
            return new OverlayTooltip("",new GUIContent("Biome: "+biome.name));
        }

        public override bool IsCoveredAt(double latitude, double longitude, CelestialBody body)
        {
            return ScanSatWrapper.Instance.IsCovered(longitude, latitude, body, "Biome");
        }

        public override string GuiName { get { return "Biome Map"; } }

        public override void Load(ConfigNode node)
        {
            try
            {
                if (!node.HasNode("Biomemap"))
                {
                    Alpha = 100;
                    return;
                }
                var myNode = node.GetNode("Biomemap");
                byte result;
                if (!Byte.TryParse(myNode.GetValue("Alpha"), out result))
                {
                    result = 100;
                }
                Alpha = result;
            }
            catch (Exception e)
            {
                this.Log("Couldnt Load "+GetType().FullName+ " "+e);
            }
        }

        public override void Save(ConfigNode node)
        {
            var myNode = node.AddNode("Biomemap");
            myNode.AddValue("Alpha", Alpha);
        }
    }
}
