//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// MoPSMB1.cs - MarketOps Suite ModBuilder v1 - Clean Build
// Combines EMA trend analysis with swing detection and volume analysis
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Core.FloatingPoint;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class MoPSMB1 : Indicator
    {
        #region Variables
        // EMA indicators
        private EMA emaHigh;
        private EMA emaClose;
        private EMA emaLow;
        
        // Swing analysis
        private Swing swingHighs;
        private Swing swingLows;
        private List<SwingPoint> recentSwings;
        
        // Volume analysis
        private SMA volumeSma;
        private double[] volumeHistory;
        private int volumeHistoryIndex;
        
        // Trend analysis
        private TrendState currentTrend;
        private int barsInTrend;
        private double trendStrength;
        
        // Overnight levels
        private double overnightHigh = double.NaN;
        private double overnightLow = double.NaN;
        private DateTime currentDate = DateTime.MinValue;
        #endregion

        #region Enums and Classes
        public enum TrendState
        {
            Neutral,
            Uptrend,
            Downtrend,
            Consolidation
        }

        public class SwingPoint
        {
            public double Price { get; set; }
            public int BarIndex { get; set; }
            public DateTime Time { get; set; }
            public string Type { get; set; }
        }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"MarketOps Suite ModBuilder v1 - Clean implementation with EMA trend analysis, swing detection, and volume analysis";
                Name = "MoPSMB1";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;
                
                // Default Parameters
                EmaHighPeriod = 21;
                EmaClosePeriod = 13;
                EmaLowPeriod = 8;
                SwingStrength = 5;
                VolumeSpikePeriod = 20;
                VolumeSpikeThreshold = 150.0;
                
                // Visual Settings
                ShowSwingLabels = true;
                ShowTrendZones = true;
                ShowEmaBands = true;
                ShowVolumeSpikes = true;
                ShowOvernightLevels = true;
                
                // Colors
                UptrendColor = Brushes.Lime;
                DowntrendColor = Brushes.Red;
                NeutralColor = Brushes.Gray;
                HigherHighColor = Brushes.Green;
                LowerLowColor = Brushes.Crimson;
                
                // Add plots for EMA values
                AddPlot(new Stroke(Brushes.ForestGreen, DashStyleHelper.Solid, 2), PlotStyle.Line, "EmaHigh");
                AddPlot(new Stroke(Brushes.MediumBlue, DashStyleHelper.Solid, 2), PlotStyle.Line, "EmaClose");
                AddPlot(new Stroke(Brushes.Red, DashStyleHelper.Solid, 2), PlotStyle.Line, "EmaLow");
                AddPlot(new Stroke(Brushes.Yellow, DashStyleHelper.Solid, 1), PlotStyle.Line, "TrendStrength");
            }
            else if (State == State.DataLoaded)
            {
                // Initialize indicators
                emaHigh = EMA(High, EmaHighPeriod);
                emaClose = EMA(Close, EmaClosePeriod);
                emaLow = EMA(Low, EmaLowPeriod);
                
                swingHighs = Swing(High, SwingStrength);
                swingLows = Swing(Low, SwingStrength);
                
                volumeSma = SMA(Volume, VolumeSpikePeriod);
                
                // Initialize variables
                recentSwings = new List<SwingPoint>();
                volumeHistory = new double[VolumeSpikePeriod];
                volumeHistoryIndex = 0;
                currentTrend = TrendState.Neutral;
                barsInTrend = 0;
                trendStrength = 0.0;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Math.Max(EmaHighPeriod, VolumeSpikePeriod)) return;

            // Update EMA plot values
            Values[0][0] = emaHigh[0]; // EmaHigh
            Values[1][0] = emaClose[0]; // EmaClose
            Values[2][0] = emaLow[0]; // EmaLow

            // Update overnight levels
            UpdateOvernightLevels();
            
            // Process swing points
            ProcessSwingPoints();
            
            // Analyze trend
            AnalyzeTrend();
            
            // Check volume spikes
            CheckVolumeSpikes();
            
            // Draw visual elements
            DrawVisualElements();
            
            // Update trend strength plot
            Values[3][0] = trendStrength;
        }

        private void UpdateOvernightLevels()
        {
            DateTime barDate = Time[0].Date;
            
            if (barDate != currentDate)
            {
                currentDate = barDate;
                overnightHigh = High[0];
                overnightLow = Low[0];
            }
            else
            {
                if (High[0] > overnightHigh) overnightHigh = High[0];
                if (Low[0] < overnightLow) overnightLow = Low[0];
            }
            
            if (ShowOvernightLevels && !double.IsNaN(overnightHigh) && !double.IsNaN(overnightLow))
            {
                Draw.Line(this, "ONH_" + CurrentBar, false, 1, overnightHigh, 0, overnightHigh, 
                    Brushes.Orange, DashStyleHelper.Dash, 2);
                Draw.Line(this, "ONL_" + CurrentBar, false, 1, overnightLow, 0, overnightLow, 
                    Brushes.Orange, DashStyleHelper.Dash, 2);
                    
                // Midpoint
                double midpoint = (overnightHigh + overnightLow) / 2;
                Draw.Line(this, "ONM_" + CurrentBar, false, 1, midpoint, 0, midpoint, 
                    Brushes.Yellow, DashStyleHelper.Dot, 1);
            }
        }

        private void ProcessSwingPoints()
        {
            // Check for new swing highs
            if (swingHighs.SwingHigh[0] > 0 && swingHighs.SwingHigh[0] != swingHighs.SwingHigh[1])
            {
                var swingPoint = new SwingPoint
                {
                    Price = swingHighs.SwingHigh[0],
                    BarIndex = CurrentBar - swingHighs.SwingHighBar(0, 1, 100),
                    Time = Time[swingHighs.SwingHighBar(0, 1, 100)],
                    Type = "High"
                };
                
                recentSwings.Add(swingPoint);
                
                if (ShowSwingLabels)
                {
                    string label = DetermineSwingLabel(swingPoint);
                    Draw.Text(this, "SwingH_" + CurrentBar, label, 
                        swingHighs.SwingHighBar(0, 1, 100), swingHighs.SwingHigh[0] + (2 * TickSize), 
                        GetSwingColor(label));
                }
            }
            
            // Check for new swing lows
            if (swingLows.SwingLow[0] > 0 && swingLows.SwingLow[0] != swingLows.SwingLow[1])
            {
                var swingPoint = new SwingPoint
                {
                    Price = swingLows.SwingLow[0],
                    BarIndex = CurrentBar - swingLows.SwingLowBar(0, 1, 100),
                    Time = Time[swingLows.SwingLowBar(0, 1, 100)],
                    Type = "Low"
                };
                
                recentSwings.Add(swingPoint);
                
                if (ShowSwingLabels)
                {
                    string label = DetermineSwingLabel(swingPoint);
                    Draw.Text(this, "SwingL_" + CurrentBar, label, 
                        swingLows.SwingLowBar(0, 1, 100), swingLows.SwingLow[0] - (2 * TickSize), 
                        GetSwingColor(label));
                }
            }
            
            // Keep only recent swings
            if (recentSwings.Count > 20)
            {
                recentSwings.RemoveAt(0);
            }
        }

        private string DetermineSwingLabel(SwingPoint currentSwing)
        {
            if (recentSwings.Count < 2) return currentSwing.Type == "High" ? "HH" : "LL";
            
            var lastSimilarSwing = recentSwings
                .Where(s => s.Type == currentSwing.Type)
                .OrderByDescending(s => s.BarIndex)
                .Skip(1)
                .FirstOrDefault();
                
            if (lastSimilarSwing == null) return currentSwing.Type == "High" ? "HH" : "LL";
            
            if (currentSwing.Type == "High")
            {
                return currentSwing.Price > lastSimilarSwing.Price ? "HH" : "LH";
            }
            else
            {
                return currentSwing.Price < lastSimilarSwing.Price ? "LL" : "HL";
            }
        }

        private Brush GetSwingColor(string label)
        {
            return label switch
            {
                "HH" => HigherHighColor,
                "LL" => LowerLowColor,
                "LH" => Brushes.Orange,
                "HL" => Brushes.Cyan,
                _ => NeutralColor
            };
        }

        private void AnalyzeTrend()
        {
            double emaHighValue = emaHigh[0];
            double emaCloseValue = emaClose[0];
            double emaLowValue = emaLow[0];
            
            TrendState newTrend = currentTrend;
            
            // Determine trend based on EMA alignment and price position
            if (emaHighValue > emaCloseValue && emaCloseValue > emaLowValue && Close[0] > emaCloseValue)
            {
                newTrend = TrendState.Uptrend;
                trendStrength = 2.0;
            }
            else if (emaLowValue < emaCloseValue && emaCloseValue < emaHighValue && Close[0] < emaCloseValue)
            {
                newTrend = TrendState.Downtrend;
                trendStrength = -2.0;
            }
            else if (Math.Abs(emaHighValue - emaLowValue) < (emaCloseValue * 0.002)) // 0.2% range
            {
                newTrend = TrendState.Consolidation;
                trendStrength = 0.0;
            }
            else
            {
                newTrend = TrendState.Neutral;
                trendStrength = 0.0;
            }
            
            if (newTrend != currentTrend)
            {
                currentTrend = newTrend;
                barsInTrend = 0;
            }
            else
            {
                barsInTrend++;
            }
        }

        private void CheckVolumeSpikes()
        {
            if (CurrentBar < VolumeSpikePeriod) return;
            
            double avgVolume = volumeSma[0];
            double currentVolume = Volume[0];
            
            if (currentVolume > (avgVolume * VolumeSpikeThreshold / 100.0) && ShowVolumeSpikes)
            {
                Draw.ArrowUp(this, "VolSpike_" + CurrentBar, false, 0, Low[0] - (3 * TickSize), 
                    Brushes.Purple);
            }
        }

        private void DrawVisualElements()
        {
            if (ShowEmaBands && CurrentBar > 1)
            {
                Brush trendColor = GetTrendColor();
                
                // Draw EMA bands
                Draw.Line(this, "EmaH_" + CurrentBar, false, 1, emaHigh[1], 0, emaHigh[0], trendColor, DashStyleHelper.Solid, 2);
                Draw.Line(this, "EmaL_" + CurrentBar, false, 1, emaLow[1], 0, emaLow[0], trendColor, DashStyleHelper.Solid, 2);
            }
            
            if (ShowTrendZones && barsInTrend > 3)
            {
                // Draw trend background
                DrawTrendZone();
            }
        }

        private Brush GetTrendColor()
        {
            return currentTrend switch
            {
                TrendState.Uptrend => UptrendColor,
                TrendState.Downtrend => DowntrendColor,
                TrendState.Consolidation => Brushes.Yellow,
                _ => NeutralColor
            };
        }

        private void DrawTrendZone()
        {
            // Simple trend zone drawing - can be enhanced
            if (CurrentBar > 5)
            {
                Brush zoneColor = GetTrendColor();
                zoneColor.Opacity = 0.1;
                
                // This is a placeholder for trend zone drawing
                // In a full implementation, you'd draw rectangles or regions
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name="EMA High Period", Description="Period for EMA High calculation", Order=1, GroupName="EMA Settings")]
        public int EmaHighPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name="EMA Close Period", Description="Period for EMA Close calculation", Order=2, GroupName="EMA Settings")]
        public int EmaClosePeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name="EMA Low Period", Description="Period for EMA Low calculation", Order=3, GroupName="EMA Settings")]
        public int EmaLowPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, 50)]
        [Display(Name="Swing Strength", Description="Strength parameter for swing detection", Order=4, GroupName="Swing Settings")]
        public int SwingStrength { get; set; }

        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name="Volume Spike Period", Description="Period for volume spike calculation", Order=5, GroupName="Volume Settings")]
        public int VolumeSpikePeriod { get; set; }

        [NinjaScriptProperty]
        [Range(100.0, 500.0)]
        [Display(Name="Volume Spike Threshold", Description="Threshold percentage for volume spikes", Order=6, GroupName="Volume Settings")]
        public double VolumeSpikeThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Swing Labels", Description="Show swing point labels", Order=7, GroupName="Visual Settings")]
        public bool ShowSwingLabels { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Trend Zones", Description="Show trend background zones", Order=8, GroupName="Visual Settings")]
        public bool ShowTrendZones { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show EMA Bands", Description="Show EMA bands", Order=9, GroupName="Visual Settings")]
        public bool ShowEmaBands { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Volume Spikes", Description="Show volume spike markers", Order=10, GroupName="Visual Settings")]
        public bool ShowVolumeSpikes { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Overnight Levels", Description="Show overnight high/low levels", Order=11, GroupName="Visual Settings")]
        public bool ShowOvernightLevels { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Uptrend Color", Description="Color for uptrend", Order=12, GroupName="Colors")]
        public Brush UptrendColor { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Downtrend Color", Description="Color for downtrend", Order=13, GroupName="Colors")]
        public Brush DowntrendColor { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Neutral Color", Description="Color for neutral trend", Order=14, GroupName="Colors")]
        public Brush NeutralColor { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Higher High Color", Description="Color for higher high swings", Order=15, GroupName="Colors")]
        public Brush HigherHighColor { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Lower Low Color", Description="Color for lower low swings", Order=16, GroupName="Colors")]
        public Brush LowerLowColor { get; set; }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> EmaHigh => Values[0];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> EmaClose => Values[1];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> EmaLow => Values[2];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> TrendStrength => Values[3];
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private MoPSMB1[] cacheMoPSMB1;
		public MoPSMB1 MoPSMB1(int emaHighPeriod, int emaClosePeriod, int emaLowPeriod, int swingStrength, int volumeSpikePeriod, double volumeSpikeThreshold, bool showSwingLabels, bool showTrendZones, bool showEmaBands, bool showVolumeSpikes, bool showOvernightLevels, Brush uptrendColor, Brush downtrendColor, Brush neutralColor, Brush higherHighColor, Brush lowerLowColor)
		{
			return MoPSMB1(Input, emaHighPeriod, emaClosePeriod, emaLowPeriod, swingStrength, volumeSpikePeriod, volumeSpikeThreshold, showSwingLabels, showTrendZones, showEmaBands, showVolumeSpikes, showOvernightLevels, uptrendColor, downtrendColor, neutralColor, higherHighColor, lowerLowColor);
		}

		public MoPSMB1 MoPSMB1(ISeries<double> input, int emaHighPeriod, int emaClosePeriod, int emaLowPeriod, int swingStrength, int volumeSpikePeriod, double volumeSpikeThreshold, bool showSwingLabels, bool showTrendZones, bool showEmaBands, bool showVolumeSpikes, bool showOvernightLevels, Brush uptrendColor, Brush downtrendColor, Brush neutralColor, Brush higherHighColor, Brush lowerLowColor)
		{
			if (cacheMoPSMB1 != null)
				for (int idx = 0; idx < cacheMoPSMB1.Length; idx++)
					if (cacheMoPSMB1[idx] != null && cacheMoPSMB1[idx].EmaHighPeriod == emaHighPeriod && cacheMoPSMB1[idx].EmaClosePeriod == emaClosePeriod && cacheMoPSMB1[idx].EmaLowPeriod == emaLowPeriod && cacheMoPSMB1[idx].SwingStrength == swingStrength && cacheMoPSMB1[idx].VolumeSpikePeriod == volumeSpikePeriod && cacheMoPSMB1[idx].VolumeSpikeThreshold == volumeSpikeThreshold && cacheMoPSMB1[idx].ShowSwingLabels == showSwingLabels && cacheMoPSMB1[idx].ShowTrendZones == showTrendZones && cacheMoPSMB1[idx].ShowEmaBands == showEmaBands && cacheMoPSMB1[idx].ShowVolumeSpikes == showVolumeSpikes && cacheMoPSMB1[idx].ShowOvernightLevels == showOvernightLevels && cacheMoPSMB1[idx].UptrendColor == uptrendColor && cacheMoPSMB1[idx].DowntrendColor == downtrendColor && cacheMoPSMB1[idx].NeutralColor == neutralColor && cacheMoPSMB1[idx].HigherHighColor == higherHighColor && cacheMoPSMB1[idx].LowerLowColor == lowerLowColor && cacheMoPSMB1[idx].EqualsInput(input))
						return cacheMoPSMB1[idx];
			return CacheIndicator<MoPSMB1>(new MoPSMB1(){ EmaHighPeriod = emaHighPeriod, EmaClosePeriod = emaClosePeriod, EmaLowPeriod = emaLowPeriod, SwingStrength = swingStrength, VolumeSpikePeriod = volumeSpikePeriod, VolumeSpikeThreshold = volumeSpikeThreshold, ShowSwingLabels = showSwingLabels, ShowTrendZones = showTrendZones, ShowEmaBands = showEmaBands, ShowVolumeSpikes = showVolumeSpikes, ShowOvernightLevels = showOvernightLevels, UptrendColor = uptrendColor, DowntrendColor = downtrendColor, NeutralColor = neutralColor, HigherHighColor = higherHighColor, LowerLowColor = lowerLowColor }, input, ref cacheMoPSMB1);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MoPSMB1 MoPSMB1(int emaHighPeriod, int emaClosePeriod, int emaLowPeriod, int swingStrength, int volumeSpikePeriod, double volumeSpikeThreshold, bool showSwingLabels, bool showTrendZones, bool showEmaBands, bool showVolumeSpikes, bool showOvernightLevels, Brush uptrendColor, Brush downtrendColor, Brush neutralColor, Brush higherHighColor, Brush lowerLowColor)
		{
			return indicator.MoPSMB1(Input, emaHighPeriod, emaClosePeriod, emaLowPeriod, swingStrength, volumeSpikePeriod, volumeSpikeThreshold, showSwingLabels, showTrendZones, showEmaBands, showVolumeSpikes, showOvernightLevels, uptrendColor, downtrendColor, neutralColor, higherHighColor, lowerLowColor);
		}

		public Indicators.MoPSMB1 MoPSMB1(ISeries<double> input , int emaHighPeriod, int emaClosePeriod, int emaLowPeriod, int swingStrength, int volumeSpikePeriod, double volumeSpikeThreshold, bool showSwingLabels, bool showTrendZones, bool showEmaBands, bool showVolumeSpikes, bool showOvernightLevels, Brush uptrendColor, Brush downtrendColor, Brush neutralColor, Brush higherHighColor, Brush lowerLowColor)
		{
			return indicator.MoPSMB1(input, emaHighPeriod, emaClosePeriod, emaLowPeriod, swingStrength, volumeSpikePeriod, volumeSpikeThreshold, showSwingLabels, showTrendZones, showEmaBands, showVolumeSpikes, showOvernightLevels, uptrendColor, downtrendColor, neutralColor, higherHighColor, lowerLowColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MoPSMB1 MoPSMB1(int emaHighPeriod, int emaClosePeriod, int emaLowPeriod, int swingStrength, int volumeSpikePeriod, double volumeSpikeThreshold, bool showSwingLabels, bool showTrendZones, bool showEmaBands, bool showVolumeSpikes, bool showOvernightLevels, Brush uptrendColor, Brush downtrendColor, Brush neutralColor, Brush higherHighColor, Brush lowerLowColor)
		{
			return indicator.MoPSMB1(Input, emaHighPeriod, emaClosePeriod, emaLowPeriod, swingStrength, volumeSpikePeriod, volumeSpikeThreshold, showSwingLabels, showTrendZones, showEmaBands, showVolumeSpikes, showOvernightLevels, uptrendColor, downtrendColor, neutralColor, higherHighColor, lowerLowColor);
		}

		public Indicators.MoPSMB1 MoPSMB1(ISeries<double> input , int emaHighPeriod, int emaClosePeriod, int emaLowPeriod, int swingStrength, int volumeSpikePeriod, double volumeSpikeThreshold, bool showSwingLabels, bool showTrendZones, bool showEmaBands, bool showVolumeSpikes, bool showOvernightLevels, Brush uptrendColor, Brush downtrendColor, Brush neutralColor, Brush higherHighColor, Brush lowerLowColor)
		{
			return indicator.MoPSMB1(input, emaHighPeriod, emaClosePeriod, emaLowPeriod, swingStrength, volumeSpikePeriod, volumeSpikeThreshold, showSwingLabels, showTrendZones, showEmaBands, showVolumeSpikes, showOvernightLevels, uptrendColor, downtrendColor, neutralColor, higherHighColor, lowerLowColor);
		}
	}
}

#endregion
