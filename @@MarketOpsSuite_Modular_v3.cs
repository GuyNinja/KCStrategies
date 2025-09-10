//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// MarketOpsSuite_Modular_v3.cs - Market Operations Suite Modular Version
// Modular implementation with separate components
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Core.FloatingPoint;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class MarketOpsSuite_Modular_v3 : Indicator
    {
        #region Variables
        private double todaysONH;
        private double todaysONL;
        private double todaysONM;
        private int lastDayCalculated;
        private List<double> dailyCloses;
        private double currentDailySmaValue;
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Market Ops Suite Modular - Modular implementation with separate components";
                Name = "MarketOpsSuite_Modular_v3";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                
                // Module Enable/Disable
                EnableOvernightLevels = true;
                EnableDailySma = true;
                EnableVolumeAnalysis = true;
                
                // Overnight Levels
                OvernightStart = 1800; // 6:00 PM
                OvernightEnd = 600;    // 6:00 AM
                LineWidth = 2;
                
                // Daily SMA
                SmaPeriod = 20;
                SmaLineWidth = 2;
                
                // Volume Spikes
                VolumeLookback = 20;
                VolumeSpikeThreshold = 2.0;
                
                // Colors
                OvernightColor = Brushes.Blue;
                MidpointColor = Brushes.Yellow;
                SmaColor = Brushes.Gold;
                VolumeSpikeColor = Brushes.Orange;
            }
            else if (State == State.Configure)
            {
                // Add plots
                AddPlot(new Stroke(OvernightColor, DashStyleHelper.Solid, LineWidth), PlotStyle.Line, "OvernightHigh");
                AddPlot(new Stroke(OvernightColor, DashStyleHelper.Solid, LineWidth), PlotStyle.Line, "OvernightLow");
                AddPlot(new Stroke(MidpointColor, DashStyleHelper.Solid, LineWidth), PlotStyle.Line, "OvernightMidpoint");
                AddPlot(new Stroke(SmaColor, DashStyleHelper.Solid, SmaLineWidth), PlotStyle.Line, "DailySMA");
            }
            else if (State == State.DataLoaded)
            {
                // Initialize Series
                OvernightHigh = new Series<double>(this, MaximumBarsLookBack.Infinite);
                OvernightLow = new Series<double>(this, MaximumBarsLookBack.Infinite);
                DailySmaValue = new Series<double>(this, MaximumBarsLookBack.Infinite);
                VolumeSpikePercent = new Series<double>(this, MaximumBarsLookBack.Infinite);
                
                // Initialize daily closes list
                dailyCloses = new List<double>();
                lastDayCalculated = -1;
                
                // Initialize historical daily closes
                for (int i = CurrentBar - 1; i > 0; i--)
                {
                    if (ToDay(Time[i]) != ToDay(Time[i + 1]))
                        dailyCloses.Insert(0, Close[i + 1]);
                }
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1) return;

            // Process modules based on enable flags
            if (EnableOvernightLevels)
            {
                ProcessOvernightLevels();
            }

            if (EnableDailySma)
            {
                ProcessDailySma();
            }

            if (EnableVolumeAnalysis)
            {
                ProcessVolumeAnalysis();
            }
        }

        private void ProcessOvernightLevels()
        {
            int currentTime = ToTime(Time[0]);
            
            // Check if we're in overnight hours
            bool isOvernight = (currentTime >= OvernightStart || currentTime <= OvernightEnd);
            
            if (isOvernight)
            {
                // Update overnight high/low
                if (High[0] > todaysONH || todaysONH == 0)
                {
                    todaysONH = High[0];
                }
                if (Low[0] < todaysONL || todaysONL == 0)
                {
                    todaysONL = Low[0];
                }
                
                // Calculate midpoint
                todaysONM = (todaysONH + todaysONL) / 2;
            }
            else
            {
                // Reset for new day
                todaysONH = High[0];
                todaysONL = Low[0];
                todaysONM = (todaysONH + todaysONL) / 2;
            }
            
            // Update Series and plots
            OvernightHigh[0] = todaysONH;
            OvernightLow[0] = todaysONL;
            
            Values[0][0] = todaysONH; // OvernightHigh plot
            Values[1][0] = todaysONL; // OvernightLow plot
            Values[2][0] = todaysONM; // OvernightMidpoint plot
        }

        private void ProcessDailySma()
        {
            int currentDay = ToDay(Time[0]);
            
            // Add new daily close if it's a new day
            if (currentDay != lastDayCalculated)
            {
                if (lastDayCalculated != -1)
                {
                    dailyCloses.Add(Close[1]); // Previous bar's close
                }
                lastDayCalculated = currentDay;
                
                // Keep only the last SmaPeriod closes
                while (dailyCloses.Count > SmaPeriod)
                {
                    dailyCloses.RemoveAt(0);
                }
            }
            
            // Calculate SMA
            if (dailyCloses.Count >= SmaPeriod)
            {
                currentDailySmaValue = dailyCloses.Average();
                DailySmaValue[0] = currentDailySmaValue;
                Values[3][0] = currentDailySmaValue; // DailySMA plot
            }
        }

        private void ProcessVolumeAnalysis()
        {
            if (CurrentBar < VolumeLookback) return;
            
            // Calculate average volume
            double avgVolume = 0;
            for (int i = 0; i < VolumeLookback; i++)
            {
                avgVolume += Volume[i];
            }
            avgVolume /= VolumeLookback;
            
            // Calculate volume spike percentage
            double volumeRatio = Volume[0] / avgVolume;
            VolumeSpikePercent[0] = volumeRatio;
            
            // Check if current volume is a spike
            if (volumeRatio > VolumeSpikeThreshold)
            {
                // Draw volume spike indicator
                Draw.Text(this, $"VolumeSpike_{CurrentBar}", "VS", 0, High[0] + (ATR(14)[0] * 0.5), VolumeSpikeColor);
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Display(Name="Enable Overnight Levels", Description="Enable overnight level calculations", Order=1, GroupName="Module Control")]
        public bool EnableOvernightLevels { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable Daily SMA", Description="Enable daily SMA calculations", Order=2, GroupName="Module Control")]
        public bool EnableDailySma { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable Volume Analysis", Description="Enable volume spike detection", Order=3, GroupName="Module Control")]
        public bool EnableVolumeAnalysis { get; set; }

        [NinjaScriptProperty]
        [Range(0, 2359)]
        [Display(Name="Overnight Start Time", Description="Start time for overnight session (24hr format)", Order=4, GroupName="Overnight Levels")]
        public int OvernightStart { get; set; }

        [NinjaScriptProperty]
        [Range(0, 2359)]
        [Display(Name="Overnight End Time", Description="End time for overnight session (24hr format)", Order=5, GroupName="Overnight Levels")]
        public int OvernightEnd { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name="Line Width", Description="Width of overnight level lines", Order=6, GroupName="Overnight Levels")]
        public int LineWidth { get; set; }

        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name="SMA Period", Description="Period for daily SMA calculation", Order=7, GroupName="Daily SMA")]
        public int SmaPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name="SMA Line Width", Description="Width of SMA line", Order=8, GroupName="Daily SMA")]
        public int SmaLineWidth { get; set; }

        [NinjaScriptProperty]
        [Range(5, 50)]
        [Display(Name="Volume Lookback", Description="Number of bars to calculate average volume", Order=9, GroupName="Volume Analysis")]
        public int VolumeLookback { get; set; }

        [NinjaScriptProperty]
        [Range(1.0, 5.0)]
        [Display(Name="Volume Spike Threshold", Description="Multiplier above average volume to trigger spike", Order=10, GroupName="Volume Analysis")]
        public double VolumeSpikeThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Overnight Color", Description="Color for overnight level lines", Order=11, GroupName="Colors")]
        public Brush OvernightColor { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Midpoint Color", Description="Color for midpoint line", Order=12, GroupName="Colors")]
        public Brush MidpointColor { get; set; }

        [NinjaScriptProperty]
        [Display(Name="SMA Color", Description="Color of SMA line", Order=13, GroupName="Colors")]
        public Brush SmaColor { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Volume Spike Color", Description="Color for volume spike indicators", Order=14, GroupName="Colors")]
        public Brush VolumeSpikeColor { get; set; }

        [XmlIgnore]
        public Series<double> OvernightHigh { get; set; }

        [XmlIgnore]
        public Series<double> OvernightLow { get; set; }

        [XmlIgnore]
        public Series<double> DailySmaValue { get; set; }

        [XmlIgnore]
        public Series<double> VolumeSpikePercent { get; set; }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private MarketOpsSuite_Modular_v3[] cacheMarketOpsSuite_Modular_v3;
		public MarketOpsSuite_Modular_v3 MarketOpsSuite_Modular_v3(bool enableOvernightLevels, bool enableDailySma, bool enableVolumeAnalysis, int overnightStart, int overnightEnd, int lineWidth, int smaPeriod, int smaLineWidth, int volumeLookback, double volumeSpikeThreshold, Brush overnightColor, Brush midpointColor, Brush smaColor, Brush volumeSpikeColor)
		{
			return MarketOpsSuite_Modular_v3(Input, enableOvernightLevels, enableDailySma, enableVolumeAnalysis, overnightStart, overnightEnd, lineWidth, smaPeriod, smaLineWidth, volumeLookback, volumeSpikeThreshold, overnightColor, midpointColor, smaColor, volumeSpikeColor);
		}

		public MarketOpsSuite_Modular_v3 MarketOpsSuite_Modular_v3(ISeries<double> input, bool enableOvernightLevels, bool enableDailySma, bool enableVolumeAnalysis, int overnightStart, int overnightEnd, int lineWidth, int smaPeriod, int smaLineWidth, int volumeLookback, double volumeSpikeThreshold, Brush overnightColor, Brush midpointColor, Brush smaColor, Brush volumeSpikeColor)
		{
			if (cacheMarketOpsSuite_Modular_v3 != null)
				for (int idx = 0; idx < cacheMarketOpsSuite_Modular_v3.Length; idx++)
					if (cacheMarketOpsSuite_Modular_v3[idx] != null && cacheMarketOpsSuite_Modular_v3[idx].EnableOvernightLevels == enableOvernightLevels && cacheMarketOpsSuite_Modular_v3[idx].EnableDailySma == enableDailySma && cacheMarketOpsSuite_Modular_v3[idx].EnableVolumeAnalysis == enableVolumeAnalysis && cacheMarketOpsSuite_Modular_v3[idx].OvernightStart == overnightStart && cacheMarketOpsSuite_Modular_v3[idx].OvernightEnd == overnightEnd && cacheMarketOpsSuite_Modular_v3[idx].LineWidth == lineWidth && cacheMarketOpsSuite_Modular_v3[idx].SmaPeriod == smaPeriod && cacheMarketOpsSuite_Modular_v3[idx].SmaLineWidth == smaLineWidth && cacheMarketOpsSuite_Modular_v3[idx].VolumeLookback == volumeLookback && cacheMarketOpsSuite_Modular_v3[idx].VolumeSpikeThreshold == volumeSpikeThreshold && cacheMarketOpsSuite_Modular_v3[idx].OvernightColor == overnightColor && cacheMarketOpsSuite_Modular_v3[idx].MidpointColor == midpointColor && cacheMarketOpsSuite_Modular_v3[idx].SmaColor == smaColor && cacheMarketOpsSuite_Modular_v3[idx].VolumeSpikeColor == volumeSpikeColor && cacheMarketOpsSuite_Modular_v3[idx].EqualsInput(input))
						return cacheMarketOpsSuite_Modular_v3[idx];
			return CacheIndicator<MarketOpsSuite_Modular_v3>(new MarketOpsSuite_Modular_v3(){ EnableOvernightLevels = enableOvernightLevels, EnableDailySma = enableDailySma, EnableVolumeAnalysis = enableVolumeAnalysis, OvernightStart = overnightStart, OvernightEnd = overnightEnd, LineWidth = lineWidth, SmaPeriod = smaPeriod, SmaLineWidth = smaLineWidth, VolumeLookback = volumeLookback, VolumeSpikeThreshold = volumeSpikeThreshold, OvernightColor = overnightColor, MidpointColor = midpointColor, SmaColor = smaColor, VolumeSpikeColor = volumeSpikeColor }, input, ref cacheMarketOpsSuite_Modular_v3);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MarketOpsSuite_Modular_v3 MarketOpsSuite_Modular_v3(bool enableOvernightLevels, bool enableDailySma, bool enableVolumeAnalysis, int overnightStart, int overnightEnd, int lineWidth, int smaPeriod, int smaLineWidth, int volumeLookback, double volumeSpikeThreshold, Brush overnightColor, Brush midpointColor, Brush smaColor, Brush volumeSpikeColor)
		{
			return indicator.MarketOpsSuite_Modular_v3(Input, enableOvernightLevels, enableDailySma, enableVolumeAnalysis, overnightStart, overnightEnd, lineWidth, smaPeriod, smaLineWidth, volumeLookback, volumeSpikeThreshold, overnightColor, midpointColor, smaColor, volumeSpikeColor);
		}

		public Indicators.MarketOpsSuite_Modular_v3 MarketOpsSuite_Modular_v3(ISeries<double> input , bool enableOvernightLevels, bool enableDailySma, bool enableVolumeAnalysis, int overnightStart, int overnightEnd, int lineWidth, int smaPeriod, int smaLineWidth, int volumeLookback, double volumeSpikeThreshold, Brush overnightColor, Brush midpointColor, Brush smaColor, Brush volumeSpikeColor)
		{
			return indicator.MarketOpsSuite_Modular_v3(input, enableOvernightLevels, enableDailySma, enableVolumeAnalysis, overnightStart, overnightEnd, lineWidth, smaPeriod, smaLineWidth, volumeLookback, volumeSpikeThreshold, overnightColor, midpointColor, smaColor, volumeSpikeColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MarketOpsSuite_Modular_v3 MarketOpsSuite_Modular_v3(bool enableOvernightLevels, bool enableDailySma, bool enableVolumeAnalysis, int overnightStart, int overnightEnd, int lineWidth, int smaPeriod, int smaLineWidth, int volumeLookback, double volumeSpikeThreshold, Brush overnightColor, Brush midpointColor, Brush smaColor, Brush volumeSpikeColor)
		{
			return indicator.MarketOpsSuite_Modular_v3(Input, enableOvernightLevels, enableDailySma, enableVolumeAnalysis, overnightStart, overnightEnd, lineWidth, smaPeriod, smaLineWidth, volumeLookback, volumeSpikeThreshold, overnightColor, midpointColor, smaColor, volumeSpikeColor);
		}

		public Indicators.MarketOpsSuite_Modular_v3 MarketOpsSuite_Modular_v3(ISeries<double> input , bool enableOvernightLevels, bool enableDailySma, bool enableVolumeAnalysis, int overnightStart, int overnightEnd, int lineWidth, int smaPeriod, int smaLineWidth, int volumeLookback, double volumeSpikeThreshold, Brush overnightColor, Brush midpointColor, Brush smaColor, Brush volumeSpikeColor)
		{
			return indicator.MarketOpsSuite_Modular_v3(input, enableOvernightLevels, enableDailySma, enableVolumeAnalysis, overnightStart, overnightEnd, lineWidth, smaPeriod, smaLineWidth, volumeLookback, volumeSpikeThreshold, overnightColor, midpointColor, smaColor, volumeSpikeColor);
		}
	}
}

#endregion
