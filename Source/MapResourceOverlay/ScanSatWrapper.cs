using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/*
 * Most of this is a modified version taken from SCANSat. License should be distributed with this code.
 * Initial changes were made by Lukas Domagala and are under the project License.
 * Updates for SCANSAT 10 were made by atomicfury and are under the project License.
 */
namespace MapResourceOverlay
{
    internal class ScanSatWrapper
    {
        private static ScanSatWrapper _instance;
        public static ScanSatWrapper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ScanSatWrapper();
                }
                return _instance;
            } 
        }
        
        // BEGIN SCANSAT CALLS - use SCANutil API
        
        private ScanSatWrapper()
        {
            InitializeScansatIntegration();
        }

        private delegate bool IsCoveredDelegate(double lon, double lat, CelestialBody body, int mask);
        private IsCoveredDelegate _scansatIsCoveredDelegate;
        
        private delegate int GetScanTypeDelegate(string resourceName);
        private GetScanTypeDelegate _getScanTypeDelegate;

        private int GetScansatId(string resourceName)
        {
            if (_getScanTypeDelegate != null)
            {
                return _getScanTypeDelegate(resourceName);
            }
            return 0;
        }

        private void InitializeScansatIntegration()
        {
            var scanutil = AssemblyLoader.loadedAssemblies.SelectMany(x => x.assembly.GetExportedTypes()).FirstOrDefault(x => x.FullName == "SCANsat.SCANUtil");
            
            //this.Log("scanutil?"+ (scanutil != null));
            
            if (scanutil != null)
            {
                //this.Log("attempting iscovered method,");
                
                var method = scanutil.GetMethod("isCovered",
                    new[] { typeof(double), typeof(double), typeof(CelestialBody), typeof(int) });
                                   
                //this.Log("iscovered method?"+ (method != null));
                
                if (method != null)
                {
                    _scansatIsCoveredDelegate = (IsCoveredDelegate)Delegate.CreateDelegate(typeof(IsCoveredDelegate), method);
                    this.Log("active covered method?"+ (_scansatIsCoveredDelegate != null));
                }
                
                //GetSCANtype
                var next_method = scanutil.GetMethod("GetSCANtype",
                    new[] { typeof(string) });
                
                //this.Log("getSCANtype method?"+ (next_method != null));
                
                if (next_method != null)
                {
                    _getScanTypeDelegate = (GetScanTypeDelegate)Delegate.CreateDelegate(typeof(GetScanTypeDelegate), next_method);
                    //this.Log("active gettype method?"+ (_getScanTypeDelegate != null));
                }
                
            }
        }

        public bool Active()
        {
            return _scansatIsCoveredDelegate != null;
        }
        
        public bool IsCovered(double longitude, double latitude, CelestialBody body, Resource resource)
        {
            if (_scansatIsCoveredDelegate == null)
            {
                return false;
            }
            return _scansatIsCoveredDelegate(longitude, latitude, body, GetScansatId(resource.ScansatName));
        }
        
        // END SCANSAT CALLS

		public CBAttributeMapSO.MapAttribute GetBiome(double lon, double lat, CelestialBody body)
		{
			if (body.BiomeMap == null) return null;
            CBAttributeMapSO.MapAttribute this_biome = body.BiomeMap.GetAtt(Mathf.Deg2Rad * lat , Mathf.Deg2Rad * lon);
            return this_biome;
		}
               
        public Color32 GetElevationColor32(CelestialBody body, double lon, double lat)
        {
            return heightToColor((float)GetElevation(body, lon, lat), 0, 7500);
        }

        public static double GetElevation(CelestialBody body, double lon, double lat)
        {
            if (body.pqsController == null) return 0;
            double rlon = Mathf.Deg2Rad * lon;
            double rlat = Mathf.Deg2Rad * lat;
            Vector3d rad = new Vector3d(Math.Cos(rlat) * Math.Cos(rlon), Math.Sin(rlat), Math.Cos(rlat) * Math.Sin(rlon));
            return Math.Round(body.pqsController.GetSurfaceHeight(rad) - body.pqsController.radius, 1);
        }
        public static Color heightToColor(float val, double low, double high)
        {
            Color c = Color.black;
            int sealevel = (int)low;
            float max = (float) high;
            if (val <= sealevel)
            {
                val = (Mathf.Clamp(val, -1500, sealevel) + 1500) / 1000f;
                c = Color.Lerp(xkcd_DarkPurple, xkcd_Cerulean, val);
            }
            else
            {
                val = (heightGradient.Length - 2) * Mathf.Clamp(val, sealevel, (sealevel + max)) / (sealevel + max); // 4*val / 7500
                c = Color.Lerp(heightGradient[(int)val], heightGradient[(int)val + 1], val - (int)val);
            }
            return c;
        }
        public static Color xkcd_Amber = XKCDColors.Amber;
        public static Color xkcd_ArmyGreen = XKCDColors.ArmyGreen;
        public static Color xkcd_PukeGreen = XKCDColors.PukeGreen;
        public static Color xkcd_Lemon = XKCDColors.Lemon;
        public static Color xkcd_OrangeRed = XKCDColors.OrangeRed;
        public static Color xkcd_CamoGreen = XKCDColors.CamoGreen;
        public static Color xkcd_Marigold = XKCDColors.Marigold;
        public static Color xkcd_Puce = XKCDColors.Puce;
        public static Color xkcd_DarkTeal = XKCDColors.DarkTeal;
        public static Color xkcd_DarkPurple = XKCDColors.DarkPurple;
        public static Color xkcd_DarkGrey = XKCDColors.DarkGrey;
        public static Color xkcd_LightGrey = XKCDColors.LightGrey;
        public static Color xkcd_PurplyPink = XKCDColors.PurplyPink;
        public static Color xkcd_Magenta = XKCDColors.Magenta;
        public static Color xkcd_YellowGreen = XKCDColors.YellowGreen;
        public static Color xkcd_LightRed = XKCDColors.LightRed;
        public static Color xkcd_Cerulean = XKCDColors.Cerulean;
        public static Color xkcd_Yellow = XKCDColors.Yellow;
        public static Color xkcd_Red = XKCDColors.Red;
        public static Color xkcd_White = XKCDColors.White;
        public static Color[] heightGradient = {
			xkcd_ArmyGreen,
			xkcd_Yellow,
			xkcd_Red,
			xkcd_Magenta,
			xkcd_White,
			xkcd_White
		};

        // XKCD Colors
        // 	(these are collected here for the same reason)


        public bool IsCovered(double longitude, double latitude, CelestialBody body,string str)
        {
            if (!Active())
            {
                return false;
            }
            return _scansatIsCoveredDelegate(longitude, latitude, body, GetScansatId(str));
        }
    }
}
