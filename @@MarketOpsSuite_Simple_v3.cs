//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// MarketOpsSuite_Simple_v3.cs - Market Operations Suite Simple Version
// Clean, minimal implementation with core functionality
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
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class MarketOpsSuite_Simple_v3 : Indicator
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
                Description = @"Market Ops Suite Simple - Core functionality with overnight levels, daily SMA, and volume spikes";
                Name = "MarketOpsSuite_Simple_v3";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                
                // Overnight Levels
                ShowOvernightLines = true;
                ShowMidpoint = true;
                OvernightStart = 1800; // 6:00 PM
                OvernightEnd = 600;    // 6:00 AM
                LineWidth = 2;
                
                // Daily SMA
                ShowDailySma = true;
                SmaPeriod = 20;
                SmaLineWidth = 2;
                SmaColor = Brushes.Gold;
                
                // Volume Spikes
                ShowVolumeSpikes = true;
                VolumeLookback = 20;
                VolumeSpikeThreshold = 2.0;
            }
            else if (State == State.Configure)
            {
                // Add plots
                AddPlot(new Stroke(Brushes.Blue, LineWidth), PlotStyle.Line, "OvernightHigh");
                AddPlot(new Stroke(Brushes.Red, LineWidth), PlotStyle.Line, "OvernightLow");
                AddPlot(new Stroke(Brushes.Yellow, LineWidth), PlotStyle.Line, "OvernightMidpoint");
                AddPlot(new Stroke(SmaColor, SmaLineWidth), PlotStyle.Line, "DailySMA");
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

            // Daily SMA Calculation
            if (ShowDailySma)
            {
                ProcessDailySma();
            }

            // Overnight Levels
            if (ShowOvernightLines)
            {
                ProcessOvernightLevels();
            }

            // Volume Spike Detection
            if (ShowVolumeSpikes)
            {
                ProcessVolumeSpikes();
            }
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

        private void ProcessOvernightLevels()
        {
            int currentTime = ToTime(Time[0]);
            int currentDay = ToDay(Time[0]);
            
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
            OvernightMidpoint[0] = todaysONM;
            
            Values[0][0] = todaysONH; // OvernightHigh plot
            Values[1][0] = todaysONL; // OvernightLow plot
            Values[2][0] = todaysONM; // OvernightMidpoint plot
        }

        private void ProcessVolumeSpikes()
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
                Draw.Text(this, $"VolumeSpike_{CurrentBar}", "VS", 0, High[0] + (ATR(14)[0] * 0.5), Brushes.Orange);
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Display(Name="Show Overnight Lines", Description="Display overnight high and low lines", Order=1, GroupName="Overnight Levels")]
        public bool ShowOvernightLines { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Midpoint", Description="Display overnight midpoint line", Order=2, GroupName="Overnight Levels")]
        public bool ShowMidpoint { get; set; }

        [NinjaScriptProperty]
        [Range(0, 2359)]
        [Display(Name="Overnight Start Time", Description="Start time for overnight session (24hr format)", Order=3, GroupName="Overnight Levels")]
        public int OvernightStart { get; set; }

        [NinjaScriptProperty]
        [Range(0, 2359)]
        [Display(Name="Overnight End Time", Description="End time for overnight session (24hr format)", Order=4, GroupName="Overnight Levels")]
        public int OvernightEnd { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name="Line Width", Description="Width of overnight level lines", Order=5, GroupName="Overnight Levels")]
        public int LineWidth { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Daily SMA", Description="Display daily simple moving average", Order=6, GroupName="Daily SMA")]
        public bool ShowDailySma { get; set; }

        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name="SMA Period", Description="Period for daily SMA calculation", Order=7, GroupName="Daily SMA")]
        public int SmaPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name="SMA Line Width", Description="Width of SMA line", Order=8, GroupName="Daily SMA")]
        public int SmaLineWidth { get; set; }

        [NinjaScriptProperty]
        [Display(Name="SMA Color", Description="Color of SMA line", Order=9, GroupName="Daily SMA")]
        public Brush SmaColor { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Volume Spikes", Description="Display volume spike indicators", Order=10, GroupName="Volume Analysis")]
        public bool ShowVolumeSpikes { get; set; }

        [NinjaScriptProperty]
        [Range(5, 50)]
        [Display(Name="Volume Lookback", Description="Number of bars to calculate average volume", Order=11, GroupName="Volume Analysis")]
        public int VolumeLookback { get; set; }

        [NinjaScriptProperty]
        [Range(1.0, 5.0)]
        [Display(Name="Volume Spike Threshold", Description="Multiplier above average volume to trigger spike", Order=12, GroupName="Volume Analysis")]
        public double VolumeSpikeThreshold { get; set; }

        [XmlIgnore]
        public Series<double> OvernightHigh { get; set; }

        [XmlIgnore]
        public Series<double> OvernightLow { get; set; }

        [XmlIgnore]
        public Series<double> OvernightMidpoint { get; set; }

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
		private MarketOpsSuite_Simple_v3[] cacheMarketOpsSuite_Simple_v3;
		public MarketOpsSuite_Simple_v3 MarketOpsSuite_Simple_v3(bool showOvernightLines, bool showMidpoint, int overnightStart, int overnightEnd, int lineWidth, bool showDailySma, int smaPeriod, int smaLineWidth, Brush smaColor, bool showVolumeSpikes, int volumeLookback, double volumeSpikeThreshold)
		{
			return MarketOpsSuite_Simple_v3(Input, showOvernightLines, showMidpoint, overnightStart, overnightEnd, lineWidth, showDailySma, smaPeriod, smaLineWidth, smaColor, showVolumeSpikes, volumeLookback, volumeSpikeThreshold);
		}

		public MarketOpsSuite_Simple_v3 MarketOpsSuite_Simple_v3(ISeries<double> input, bool showOvernightLines, bool showMidpoint, int overnightStart, int overnightEnd, int lineWidth, bool showDailySma, int smaPeriod, int smaLineWidth, Brush smaColor, bool showVolumeSpikes, int volumeLookback, double volumeSpikeThreshold)
		{
			if (cacheMarketOpsSuite_Simple_v3 != null)
				for (int idx = 0; idx < cacheMarketOpsSuite_Simple_v3.Length; idx++)
					if (cacheMarketOpsSuite_Simple_v3[idx] != null && cacheMarketOpsSuite_Simple_v3[idx].ShowOvernightLines == showOvernightLines && cacheMarketOpsSuite_Simple_v3[idx].ShowMidpoint == showMidpoint && cacheMarketOpsSuite_Simple_v3[idx].OvernightStart == overnightStart && cacheMarketOpsSuite_Simple_v3[idx].OvernightEnd == overnightEnd && cacheMarketOpsSuite_Simple_v3[idx].LineWidth == lineWidth && cacheMarketOpsSuite_Simple_v3[idx].ShowDailySma == showDailySma && cacheMarketOpsSuite_Simple_v3[idx].SmaPeriod == smaPeriod && cacheMarketOpsSuite_Simple_v3[idx].SmaLineWidth == smaLineWidth && cacheMarketOpsSuite_Simple_v3[idx].SmaColor == smaColor && cacheMarketOpsSuite_Simple_v3[idx].ShowVolumeSpikes == showVolumeSpikes && cacheMarketOpsSuite_Simple_v3[idx].VolumeLookback == volumeLookback && cacheMarketOpsSuite_Simple_v3[idx].VolumeSpikeThreshold == volumeSpikeThreshold && cacheMarketOpsSuite_Simple_v3[idx].EqualsInput(input))
						return cacheMarketOpsSuite_Simple_v3[idx];
			return CacheIndicator<MarketOpsSuite_Simple_v3>(new MarketOpsSuite_Simple_v3(){ ShowOvernightLines = showOvernightLines, ShowMidpoint = showMidpoint, OvernightStart = overnightStart, OvernightEnd = overnightEnd, LineWidth = lineWidth, ShowDailySma = showDailySma, SmaPeriod = smaPeriod, SmaLineWidth = smaLineWidth, SmaColor = smaColor, ShowVolumeSpikes = showVolumeSpikes, VolumeLookback = volumeLookback, VolumeSpikeThreshold = volumeSpikeThreshold }, input, ref cacheMarketOpsSuite_Simple_v3);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MarketOpsSuite_Simple_v3 MarketOpsSuite_Simple_v3(bool showOvernightLines, bool showMidpoint, int overnightStart, int overnightEnd, int lineWidth, bool showDailySma, int smaPeriod, int smaLineWidth, Brush smaColor, bool showVolumeSpikes, int volumeLookback, double volumeSpikeThreshold)
		{
			return indicator.MarketOpsSuite_Simple_v3(Input, showOvernightLines, showMidpoint, overnightStart, overnightEnd, lineWidth, showDailySma, smaPeriod, smaLineWidth, smaColor, showVolumeSpikes, volumeLookback, volumeSpikeThreshold);
		}

		public Indicators.MarketOpsSuite_Simple_v3 MarketOpsSuite_Simple_v3(ISeries<double> input , bool showOvernightLines, bool showMidpoint, int overnightStart, int overnightEnd, int lineWidth, bool showDailySma, int smaPeriod, int smaLineWidth, Brush smaColor, bool showVolumeSpikes, int volumeLookback, double volumeSpikeThreshold)
		{
			return indicator.MarketOpsSuite_Simple_v3(input, showOvernightLines, showMidpoint, overnightStart, overnightEnd, lineWidth, showDailySma, smaPeriod, smaLineWidth, smaColor, showVolumeSpikes, volumeLookback, volumeSpikeThreshold);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MarketOpsSuite_Simple_v3 MarketOpsSuite_Simple_v3(bool showOvernightLines, bool showMidpoint, int overnightStart, int overnightEnd, int lineWidth, bool showDailySma, int smaPeriod, int smaLineWidth, Brush smaColor, bool showVolumeSpikes, int volumeLookback, double volumeSpikeThreshold)
		{
			return indicator.MarketOpsSuite_Simple_v3(Input, showOvernightLines, showMidpoint, overnightStart, overnightEnd, lineWidth, showDailySma, smaPeriod, smaLineWidth, smaColor, showVolumeSpikes, volumeLookback, volumeSpikeThreshold);
		}

		public Indicators.MarketOpsSuite_Simple_v3 MarketOpsSuite_Simple_v3(ISeries<double> input , bool showOvernightLines, bool showMidpoint, int overnightStart, int overnightEnd, int lineWidth, bool showDailySma, int smaPeriod, int smaLineWidth, Brush smaColor, bool showVolumeSpikes, int volumeLookback, double volumeSpikeThreshold)
		{
			return indicator.MarketOpsSuite_Simple_v3(input, showOvernightLines, showMidpoint, overnightStart, overnightEnd, lineWidth, showDailySma, smaPeriod, smaLineWidth, smaColor, showVolumeSpikes, volumeLookback, volumeSpikeThreshold);
		}
	}
}

#endregion


