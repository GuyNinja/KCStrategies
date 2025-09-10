//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds all indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
    public class W2kSwings3 : Indicator
    {
        #region State Management Variables
        private List<SwingPoint> twoWeekSwings;
        
        // --- Lists for statistical analysis ---
        private List<double> twoWeekMeasuredMoves;
        private List<double> twoWeekPullbacks;
        private List<double> twoWeekAllRanges;

        private bool isDataProcessed = false;
        private bool hasEnoughData = true; 
        private DateTime analysisStartDate;
        private DateTime analysisEndDate;
        #endregion

        private class SwingPoint
        {
            public double Price { get; set; }
            public DateTime Time { get; set; }
            public bool IsHigh { get; set; }
            public string Label { get; set; } // Added to identify HH, LL etc.
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Analyzes swings within a specific 2-week trading window to provide ATM recommendations.";
                Name = "W2kSwings3";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                DrawOnPricePanel = true;
                PaintPriceMarkers = false;

                Strength = 12;
            }
            else if (State == State.DataLoaded)
            {
                if (!isDataProcessed)
                {
                    ProcessTwoWeekHistoricalData();
                    isDataProcessed = true;
                }
            }
        }

        private void ProcessTwoWeekHistoricalData()
        {
            twoWeekSwings = new List<SwingPoint>();
            twoWeekMeasuredMoves = new List<double>();
            twoWeekPullbacks = new List<double>();
            twoWeekAllRanges = new List<double>();
            
            // --- 1. Calculate the analysis date range ---
            DateTime today = DateTime.Now.Date;
            analysisEndDate = today.AddDays(-(int)today.DayOfWeek - 2); 
            analysisStartDate = analysisEndDate.AddDays(-14);

            if (Bars.Count == 0 || Bars.GetTime(0).Date > analysisStartDate)
            {
                hasEnoughData = false;
                return;
            }

            TimeZoneInfo easternZone;
            try
            {
                easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                Print("Eastern Standard Time zone not found. Please check system time zones.");
                return;
            }

            // --- 2. Find all swings in the 2-week period ---
            Swing swingHighs = Swing(Strength);
            Swing swingLows = Swing(Strength);
            
            for (int i = 0; i < Bars.Count; i++)
            {
                DateTime barTime = Bars.GetTime(i);
                DateTime estTime = TimeZoneInfo.ConvertTime(barTime, TimeZoneInfo.Local, easternZone);

                if (estTime.Date >= analysisStartDate.Date && estTime.Date <= analysisEndDate.Date &&
                    estTime.DayOfWeek >= DayOfWeek.Monday && estTime.DayOfWeek <= DayOfWeek.Friday &&
                    estTime.TimeOfDay >= new TimeSpan(7, 0, 0) && estTime.TimeOfDay <= new TimeSpan(11, 0, 0))
                {
                    if (swingHighs.High[i] > 0 && (i == 0 || swingHighs.High[i] != swingHighs.High[i-1]))
                    {
                        twoWeekSwings.Add(new SwingPoint { Price = swingHighs.High[i], Time = estTime, IsHigh = true });
                    }
                    if (swingLows.Low[i] > 0 && (i == 0 || swingLows.Low[i] != swingLows.Low[i-1]))
                    {
                        twoWeekSwings.Add(new SwingPoint { Price = swingLows.Low[i], Time = estTime, IsHigh = false });
                    }
                }
            }
            
            // Sort swings by time to ensure correct order
            twoWeekSwings = twoWeekSwings.OrderBy(s => s.Time).ToList();
            
            // --- 3. Analyze the collected swings to calculate stats ---
            CalculateStats();
        }

        private void CalculateStats()
        {
            if (twoWeekSwings.Count < 4) return; // Need at least a few swings to calculate anything

            // First, label all the swings (HH, LL, etc.)
            for (int i = 1; i < twoWeekSwings.Count; i++)
            {
                SwingPoint currentSwing = twoWeekSwings[i];
                var priorSimilarSwings = twoWeekSwings.Where(s => s.IsHigh == currentSwing.IsHigh && s.Time < currentSwing.Time).ToList();
                if (priorSimilarSwings.Count == 0) continue;
                
                SwingPoint priorSwing = priorSimilarSwings.Last();
                
                if(currentSwing.IsHigh)
                    currentSwing.Label = currentSwing.Price > priorSwing.Price ? "HH" : "LH";
                else
                    currentSwing.Label = currentSwing.Price < priorSwing.Price ? "LL" : "HL";
            }

            // Now, calculate the moves based on the labels
            for (int i = 1; i < twoWeekSwings.Count; i++)
            {
                SwingPoint currentSwing = twoWeekSwings[i];
                SwingPoint priorSwing = twoWeekSwings[i-1];
                
                // Add to all ranges
                twoWeekAllRanges.Add(Math.Abs(currentSwing.Price - priorSwing.Price) / TickSize);

                // Measured Moves (HL -> HH or LH -> LL)
                if(currentSwing.IsHigh && currentSwing.Label == "HH" && !priorSwing.IsHigh && priorSwing.Label == "HL")
                    twoWeekMeasuredMoves.Add(Math.Abs(currentSwing.Price - priorSwing.Price) / TickSize);
                else if(!currentSwing.IsHigh && currentSwing.Label == "LL" && priorSwing.IsHigh && priorSwing.Label == "LH")
                    twoWeekMeasuredMoves.Add(Math.Abs(currentSwing.Price - priorSwing.Price) / TickSize);
                
                // Pullbacks (HH -> HL or LL -> LH)
                if(!currentSwing.IsHigh && currentSwing.Label == "HL" && priorSwing.IsHigh && priorSwing.Label == "HH")
                    twoWeekPullbacks.Add(Math.Abs(currentSwing.Price - priorSwing.Price) / TickSize);
                else if(currentSwing.IsHigh && currentSwing.Label == "LH" && !priorSwing.IsHigh && priorSwing.Label == "LL")
                    twoWeekPullbacks.Add(Math.Abs(currentSwing.Price - priorSwing.Price) / TickSize);
            }
        }

        protected override void OnBarUpdate()
        {
            if (isDataProcessed && CurrentBar == Bars.Count - 1)
            {
                if (hasEnoughData)
                {
                    DrawTopLeftBox();
                    DrawBottomLeftBox(); // Call the new drawing method
                }
                else
                {
                    DrawDataWarningBox();
                }
            }
        }

        private void DrawTopLeftBox()
        {
            string avgTarget = twoWeekMeasuredMoves.Count > 0 ? twoWeekMeasuredMoves.Average().ToString("F0") : "N/A";
            string medTarget = twoWeekMeasuredMoves.Count > 0 ? CalculateMedian(twoWeekMeasuredMoves).ToString("F0") : "N/A";
            
            string avgRange = twoWeekAllRanges.Count > 0 ? twoWeekAllRanges.Average().ToString("F0") : "N/A";
            string medRange = twoWeekAllRanges.Count > 0 ? CalculateMedian(twoWeekAllRanges).ToString("F0") : "N/A";
            
            string avgTrail = twoWeekPullbacks.Count > 0 ? twoWeekPullbacks.Average().ToString("F0") : "N/A";
            string medTrail = twoWeekPullbacks.Count > 0 ? CalculateMedian(twoWeekPullbacks).ToString("F0") : "N/A";

            string boxText = string.Format(
                "--- General ATM (ticks) ---\n" +
                "Target (Avg/Med): {0}/{1}\n" +
                "Stop (Avg/Med): {2}/{3}\n" +
                "B/E (Avg/Med): {2}/{3}\n" +
                "Trail (Avg/Med): {4}/{5}",
                avgTarget, medTarget,
                avgRange, medRange,
                avgTrail, medTrail
            );

            Draw.TextFixed(this, "TopLeftBox", boxText, TextPosition.TopLeft, Brushes.White, new SimpleFont("Arial", 12), Brushes.Transparent, Brushes.DarkSlateGray, 70);
        }

        private void DrawBottomLeftBox()
        {
            string minTarget = twoWeekMeasuredMoves.Count > 0 ? twoWeekMeasuredMoves.Min().ToString("F0") : "N/A";
            string minStop = twoWeekAllRanges.Count > 0 ? twoWeekAllRanges.Min().ToString("F0") : "N/A";
            string minTrail = twoWeekPullbacks.Count > 0 ? twoWeekPullbacks.Min().ToString("F0") : "N/A";

            string boxText = string.Format(
                "--- SCALPER ATM (ticks) ---\n" +
                "Target (Min): {0}\n" +
                "Stop (Min): {1}\n" +
                "B/E (Min): {1}\n" +
                "Trail (Min): {2}",
                minTarget,
                minStop,
                minTrail
            );

            Draw.TextFixed(this, "BottomLeftBox", boxText, TextPosition.BottomLeft, Brushes.White, new SimpleFont("Arial", 12), Brushes.Transparent, Brushes.DarkSlateGray, 70);
        }

        private void DrawDataWarningBox()
        {
            string warningText = "Not enough data loaded.\n" +
                                 "Indicator requires at least 2 weeks of data\n" +
                                 "prior to the most recent Friday.\n" +
                                 "Please load more historical data.";
            Draw.TextFixed(this, "DataWarningBox", warningText, TextPosition.Center, Brushes.White, new SimpleFont("Arial", 16), Brushes.Transparent, Brushes.Red, 80);
        }
        
        private double CalculateMedian(List<double> data)
        {
            if (data.Count == 0) return 0;
            
            List<double> sortedData = new List<double>(data);
            sortedData.Sort();
            
            int mid = sortedData.Count / 2;
            if (sortedData.Count % 2 == 0)
                return (sortedData[mid - 1] + sortedData[mid]) / 2;
            else
                return sortedData[mid];
        }

        #region User-Configurable Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Strength", Order=1, GroupName="Parameters")]
        public int Strength { get; set; }
        #endregion
    }
}


#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private W2kSwings3[] cacheW2kSwings3;
		public W2kSwings3 W2kSwings3(int strength)
		{
			return W2kSwings3(Input, strength);
		}

		public W2kSwings3 W2kSwings3(ISeries<double> input, int strength)
		{
			if (cacheW2kSwings3 != null)
				for (int idx = 0; idx < cacheW2kSwings3.Length; idx++)
					if (cacheW2kSwings3[idx] != null && cacheW2kSwings3[idx].Strength == strength && cacheW2kSwings3[idx].EqualsInput(input))
						return cacheW2kSwings3[idx];
			return CacheIndicator<W2kSwings3>(new W2kSwings3(){ Strength = strength }, input, ref cacheW2kSwings3);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.W2kSwings3 W2kSwings3(int strength)
		{
			return indicator.W2kSwings3(Input, strength);
		}

		public Indicators.W2kSwings3 W2kSwings3(ISeries<double> input , int strength)
		{
			return indicator.W2kSwings3(input, strength);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.W2kSwings3 W2kSwings3(int strength)
		{
			return indicator.W2kSwings3(Input, strength);
		}

		public Indicators.W2kSwings3 W2kSwings3(ISeries<double> input , int strength)
		{
			return indicator.W2kSwings3(input, strength);
		}
	}
}

#endregion
