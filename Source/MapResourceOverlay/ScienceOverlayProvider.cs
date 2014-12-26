using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MapResourceOverlay
{
    class ScienceOverlayProvider : OverlayProviderBase
    {
        private ScienceOverlayView _scienceOverlayView;
        private ExperimentSituations _situation;
        private List<ScienceExperiment> _experiments;
        private readonly Dictionary<string, float> _biomeTagsToTotal;
        private double _displayMax;
        public ScienceOverlayProvider()
        {
            _situation = ExperimentSituations.FlyingHigh;
            _biomeTagsToTotal = new Dictionary<string, float>();
        }

        public override Color32 CalculateColor32(double latitude, double longitude, CelestialBody body, bool useScansat, bool bright)
        {
            if (useScansat && ScanSatWrapper.Instance.Active() && !IsCoveredAt(latitude,longitude,body))
            {
                return new Color32(0,0,0,0);
            }
			var scanSat = ScanSatWrapper.Instance;
			var biome = scanSat.GetBiome(longitude, latitude, body);
            float value;
            if (!_biomeTagsToTotal.TryGetValue(biome.name,out value))
            {
                this.Log(biome.name);
            }
            return new Color32(Convert.ToByte(Mathf.Clamp(value/(float)_displayMax*255, 0, 255)), 0, 0, 150);
        }

        public override OverlayTooltip TooltipContent(double latitude, double longitude, CelestialBody body)
        {
			var scanSat = ScanSatWrapper.Instance;
			var biome = scanSat.GetBiome(longitude, latitude, body);
            var all = Enum.GetValues(typeof(ExperimentSituations)).Cast<ExperimentSituations>()
                .Select(situation => ResearchAndDevelopment.GetExperimentIDs().Select(ResearchAndDevelopment.GetExperiment)
                    .Where(x => x.biomeMask != 0 && x.situationMask != 0)
                    .Where(x => x.IsAvailableWhileFixed(situation, body))
                    .GroupBy(x => x.BiomeIsRelevantWhile(situation))
                    .SelectMany(x => x.Select(y => new { exp = y, subj = ResearchAndDevelopment.GetExperimentSubject(y, situation, body, x.Key ? biome.name : "") }))
                    .Select(x => new { value = ResearchAndDevelopment.GetScienceValue(x.exp.dataScale * x.exp.baseValue, x.subj), exp = x })
                    .Aggregate(new { str = "", total = 0f },
                        (a, x) => new { str = a.str + " name: " + x.exp.exp.experimentTitle + " value: " + x.value, total = a.total + x.value },
                        result => "Situation " + Enum.GetName(typeof(ExperimentSituations), situation) + " total: " + result.total + result.str))
                .Aggregate("", (str, x) => str + x + "\n");
            var main = ResearchAndDevelopment.GetExperimentIDs().Select(ResearchAndDevelopment.GetExperiment)
                    .Where(x => x.biomeMask != 0 && x.situationMask != 0)
                    .Where(x => x.IsAvailableWhileFixed(_situation, body))
                    .GroupBy(x => x.BiomeIsRelevantWhile(_situation))
                    .SelectMany(x => x.Select(y => new { exp = y, subj = ResearchAndDevelopment.GetExperimentSubject(y, _situation, body, x.Key ? biome.name : "") }))
                    .Select(x => new { value = ResearchAndDevelopment.GetScienceValue(x.exp.dataScale * x.exp.baseValue, x.subj), exp = x })
                    .Aggregate(new { str = "", total = 0f },
                        (a, x) => new { str = a.str + " name: " + x.exp.exp.experimentTitle + " value: " + x.value, total = a.total + x.value },
                        result => "Situation " + Enum.GetName(typeof(ExperimentSituations), _situation) + " total: " + result.total + result.str);
            return new OverlayTooltip(biome.name,new GUIContent(main+"\n\n"+all), new Vector2(500,500));
        }

        public override bool IsCoveredAt(double latitude, double longitude, CelestialBody body)
        {
            return ScanSatWrapper.Instance.IsCovered(longitude, latitude, body, "AltimetryHiRes");
        }

        public override void Activate(CelestialBody body)
        {
            base.Activate(body);
            if (_scienceOverlayView != null)
            {
                _scienceOverlayView.SetVisible(false);
            }
            SetExperiments(_situation);
            CalculateBiomes();
            RequiresRedraw();
        }

        public override void BodyChanged(CelestialBody body)
        {
            base.BodyChanged(body);
            SetExperiments(_situation);
            CalculateBiomes();
            RequiresRedraw();
        }

        private void CalculateBiomes()
        {
            var biomeTags = _body.BiomeMap.Attributes.Select(x => x.name).ToList();
            //this.Log(biomeTags.Aggregate((x,y) => x+"\n"+y));
            _biomeTagsToTotal.Clear();
            foreach (var biome in biomeTags)
            {
                _biomeTagsToTotal.Add(biome,CalculateTotalForBiome(biome));
            }
            if (biomeTags.Count == 0)
            {
                _biomeTagsToTotal.Add("",CalculateTotalForBiome(""));
            }
            _displayMax = _biomeTagsToTotal.Select(x => x.Value).Max();
        }

        private float CalculateTotalForBiome(string biome)
        {
            return _experiments
                .GroupBy(x => x.BiomeIsRelevantWhile(_situation))
                .SelectMany(x => x.Select(y => new { exp = y, subj = ResearchAndDevelopment.GetExperimentSubject(y, _situation, _body, x.Key ? biome : "") }))
                .Select(x => ResearchAndDevelopment.GetScienceValue(x.exp.dataScale * x.exp.baseValue, x.subj))
                .Sum();
        }

        public override string GuiName { get { return "Science Map"; }  }

        public ExperimentSituations Situation
        {
            get { return _situation; }
            set
            {
                _situation = value; 
                SetExperiments(_situation);
                CalculateBiomes();
                RequiresRedraw();
            }
        }
        
        public override void DrawGui(MapOverlayGui window)
        {
            if (GUILayout.Button("Science Map Options"))
            {
                _scienceOverlayView = new ScienceOverlayView(this);
            }
        }

        private void SetExperiments(ExperimentSituations situations)
        {
            _experiments = ResearchAndDevelopment.GetExperimentIDs().Select(ResearchAndDevelopment.GetExperiment)
                .Where(x => x.biomeMask != 0 && x.situationMask != 0)
                .Where(x => x.IsAvailableWhileFixed(situations, _body)).ToList();
        }

    }

    internal class ScienceOverlayView : Window<ScienceOverlayView>
    {
        private readonly ScienceOverlayProvider _model;

        public ScienceOverlayView(ScienceOverlayProvider model)
            : base("Science Map Options", 250, 400)
        {
            _model = model;
            SetVisible(true);
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();
            foreach (var situation in Enum.GetValues(typeof(ExperimentSituations)).Cast<ExperimentSituations>())
            {
                var style = new GUIStyle(GUI.skin.button);
                if (situation == _model.Situation)
                {
                    style.normal.textColor = Color.yellow;
                }
                if (GUILayout.Button(Enum.GetName(typeof(ExperimentSituations),situation), style))
                {
                    _model.Situation = situation;
                }
            }
            GUILayout.EndVertical();
        }
    }
}