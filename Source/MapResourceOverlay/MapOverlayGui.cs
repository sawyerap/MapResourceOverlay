using System;
using UnityEngine;

namespace MapResourceOverlay
{
    public class MapOverlayGui : Window<MapOverlayGui>
    {
        public MapOverlay Model;

        public MapOverlayGui(MapOverlay model)
            : base("Map Overlay", 300, 200)
        {
            Model = model;

        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();
            if (Model.Show)
            {
                if (GUILayout.Button("Disable Overlay"))
                {
                    Model.Show = false;
                }
            }
            else
            {
                if (GUILayout.Button("Enable Overlay"))
                {
                    Model.Show = true;
                }
            }
            if (ScanSatWrapper.Instance.Active())
            {
                if (Model.UseScansat)
                {
                    if (GUILayout.Button("Disable Scansat"))
                    {
                        Model.UseScansat = false;
                    }
                }
                else
                {
                    if (GUILayout.Button("Enable Scansat"))
                    {
                        Model.UseScansat = true;
                    }
                }
            }
            if (Model.ShowTooltip)
            {
                if (GUILayout.Button("Disable Tooltip"))
                {
                    Model.ShowTooltip = false;
                }
            }
            else
            {
                if (GUILayout.Button("Enable Tooltip"))
                {
                    Model.ShowTooltip = true;
                }
            }

            Model.Bright = GUILayout.Toggle(Model.Bright, "bright");

            GUILayout.BeginHorizontal();
            GUILayout.Label("low Cuttoff %: ");
            int temp;
            var cutoff = GUILayout.TextField(Model.Cutoff.ToString());
            if (Int32.TryParse(cutoff, out temp))
            {
                Model.Cutoff = temp;
            }
            else if(cutoff == "")
            {
                Model.Cutoff = 0;
            }
            
            GUILayout.EndHorizontal();
            
            
            GUILayout.Box("Overlay types:");
            foreach (var overlayProvider in Model.OverlayProviders)
            {
                var style = new GUIStyle(GUI.skin.button);
                if (overlayProvider == Model.OverlayProvider)
                {
                    style.normal.textColor = Color.yellow;
                }
                if (GUILayout.Button(overlayProvider.GuiName,style))
                {
                    Model.SetOverlayProvider(overlayProvider);
                }
            }
            GUILayout.Box("Active Type Specific:");
            Model.OverlayProvider.DrawGui(this);
            GUILayout.EndVertical();
        }
    }
}