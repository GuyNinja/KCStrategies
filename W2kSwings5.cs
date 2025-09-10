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
    public class W2kSwings5 : Indicator
    {
        #region State Management Variables
        private List<SwingPoint> twoWeekSwings;
        
        // --- Lists for statistical analysis ---
        private List<double> twoWeekMeasuredMoves;
        private List<double> twoWeekPullbacks;
        private List<double> twoWeekAllRanges;
        private List<double> twoWeekBullishMeasuredMoves;
        private List<double> twoWeekBearishMeasuredMoves;
        private List<double> twoWeekBullishPullbacks;
        private List<double> twoWeekBearishPullbacks;
        
        // --- New lists for Max Trend analysis ---
        private List<double> bullishTrendLengths;
        private List<double> bearishTrendLengths;
        private List<double> bullishTrendMaxPullbacks;
        private List<double> bearishTrendMaxPullbacks;

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
            public string Label { get; set; } 
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Analyzes swings within a specific 2-week trading window to provide ATM recommendations.";
                Name = "W2kSwings5";
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
            // Initialize all lists
            twoWeekSwings = new List<SwingPoint>();
            twoWeekMeasuredMoves = new List<double>();
            twoWeekPullbacks = new List<double>();
            twoWeekAllRanges = new List<double>();
            twoWeekBullishMeasuredMoves = new List<double>();
            twoWeekBearishMeasuredMoves = new List<double>();
            twoWeekBullishPullbacks = new List<double>();
            twoWeekBearishPullbacks = new List<double>();
            bullishTrendLengths = new List<double>();
            bearishTrendLengths = new List<double>();
            bullishTrendMaxPullbacks = new List<double>();
            bearishTrendMaxPullbacks = new List<double>();
            
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
            
            twoWeekSwings = twoWeekSwings.OrderBy(s => s.Time).ToList();
            CalculateStats();
        }

        private void CalculateStats()
        {
            if (twoWeekSwings.Count < 4) return;

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

            for (int i = 1; i < twoWeekSwings.Count; i++)
            {
                SwingPoint currentSwing = twoWeekSwings[i];
                SwingPoint priorSwing = twoWeekSwings[i-1];
                
                twoWeekAllRanges.Add(Math.Abs(currentSwing.Price - priorSwing.Price) / TickSize);

                if(currentSwing.IsHigh && currentSwing.Label == "HH" && !priorSwing.IsHigh && priorSwing.Label == "HL")
                {
                    double move = Math.Abs(currentSwing.Price - priorSwing.Price) / TickSize;
                    twoWeekMeasuredMoves.Add(move);
                    twoWeekBullishMeasuredMoves.Add(move);
                }
                else if(!currentSwing.IsHigh && currentSwing.Label == "LL" && priorSwing.IsHigh && priorSwing.Label == "LH")
                {
                    double move = Math.Abs(currentSwing.Price - priorSwing.Price) / TickSize;
                    twoWeekMeasuredMoves.Add(move);
                    twoWeekBearishMeasuredMoves.Add(move);
                }
                
                if(!currentSwing.IsHigh && currentSwing.Label == "HL" && priorSwing.IsHigh && priorSwing.Label == "HH")
                {
                    double pullback = Math.Abs(currentSwing.Price - priorSwing.Price) / TickSize;
                    twoWeekPullbacks.Add(pullback);
                    twoWeekBullishPullbacks.Add(pullback);
                }
                else if(currentSwing.IsHigh && currentSwing.Label == "LH" && !priorSwing.IsHigh && priorSwing.Label == "LL")
                {
                    double pullback = Math.Abs(currentSwing.Price - priorSwing.Price) / TickSize;
                    twoWeekPullbacks.Add(pullback);
                    twoWeekBearishPullbacks.Add(pullback);
                }
            }

            // --- New: Analyze entire trends for max values ---
            AnalyzeTrendSegments();
        }

        private void AnalyzeTrendSegments()
        {
            int trendStartIndex = -1;
            bool isBullTrend = false;

            for (int i = 2; i < twoWeekSwings.Count; i++)
            {
                bool isBullSequence = twoWeekSwings[i].Label == "HH" && twoWeekSwings[i-1].Label == "HL";
                bool isBearSequence = twoWeekSwings[i].Label == "LL" && twoWeekSwings[i-1].Label == "LH";

                if (trendStartIndex == -1) // Not currently in a trend, look for a start
                {
                    if (isBullSequence)
                    {
                        trendStartIndex = i - 1;
                        isBullTrend = true;
                    }
                    else if (isBearSequence)
                    {
                        trendStartIndex = i - 1;
                        isBullTrend = false;
                    }
                }
                else // Already in a trend, check if it continues or breaks
                {
                    bool trendBroken = (isBullTrend && twoWeekSwings[i].Label == "LL") || (!isBullTrend && twoWeekSwings[i].Label == "HH");
                    if (trendBroken || i == twoWeekSwings.Count - 1) // End of trend or end of data
                    {
                        int trendEndIndex = trendBroken ? i - 1 : i;
                        
                        double startPrice = twoWeekSwings[trendStartIndex].Price;
                        double endPrice = twoWeekSwings[trendEndIndex].Price;
                        double trendLength = Math.Abs(endPrice - startPrice) / TickSize;
                        
                        double maxPullback = 0;
                        for(int j = trendStartIndex + 1; j <= trendEndIndex; j++)
                        {
                            if((isBullTrend && twoWeekSwings[j].Label == "HL") || (!isBullTrend && twoWeekSwings[j].Label == "LH"))
                            {
                                double pullback = Math.Abs(twoWeekSwings[j].Price - twoWeekSwings[j-1].Price) / TickSize;
                                if(pullback > maxPullback) maxPullback = pullback;
                            }
                        }

                        if(isBullTrend)
                        {
                            bullishTrendLengths.Add(trendLength);
                            bullishTrendMaxPullbacks.Add(maxPullback);
                        }
                        else
                        {
                            bearishTrendLengths.Add(trendLength);
                            bearishTrendMaxPullbacks.Add(maxPullback);
                        }
                        
                        trendStartIndex = -1; // Reset for next trend
                    }
                }
            }
        }

        protected override void OnBarUpdate()
        {
            if (isDataProcessed && CurrentBar == Bars.Count - 1)
            {
                if (hasEnoughData)
                {
                    DrawTopLeftBox();
                    DrawBottomLeftBox();
                    DrawTopRightBox();
                    DrawBottomRightBox(); // Call the final drawing method
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

        private void DrawTopRightBox()
        {
            string longMove = string.Format("Moves (Avg/Med): {0}/{1}",
                twoWeekBullishMeasuredMoves.Count > 0 ? twoWeekBullishMeasuredMoves.Average().ToString("F0") : "N/A",
                twoWeekBullishMeasuredMoves.Count > 0 ? CalculateMedian(twoWeekBullishMeasuredMoves).ToString("F0") : "N/A");
            
            string longPullback = string.Format("Pullbacks (Avg/Med): {0}/{1}",
                twoWeekBullishPullbacks.Count > 0 ? twoWeekBullishPullbacks.Average().ToString("F0") : "N/A",
                twoWeekBullishPullbacks.Count > 0 ? CalculateMedian(twoWeekBullishPullbacks).ToString("F0") : "N/A");

            string shortMove = string.Format("Moves (Avg/Med): {0}/{1}",
                twoWeekBearishMeasuredMoves.Count > 0 ? twoWeekBearishMeasuredMoves.Average().ToString("F0") : "N/A",
                twoWeekBearishMeasuredMoves.Count > 0 ? CalculateMedian(twoWeekBearishMeasuredMoves).ToString("F0") : "N/A");

            string shortPullback = string.Format("Pullbacks (Avg/Med): {0}/{1}",
                twoWeekBearishPullbacks.Count > 0 ? twoWeekBearishPullbacks.Average().ToString("F0") : "N/A",
                twoWeekBearishPullbacks.Count > 0 ? CalculateMedian(twoWeekBearishPullbacks).ToString("F0") : "N/A");

            string boxText = string.Format(
                "--- Trend-Specific ATM (ticks) ---\n" +
                "Longs:\n" +
                "  {0}\n" +
                "  {1}\n" +
                "Shorts:\n" +
                "  {2}\n" +
                "  {3}",
                longMove, longPullback,
                shortMove, shortPullback
            );

            Draw.TextFixed(this, "TopRightBox", boxText, TextPosition.TopRight, Brushes.White, new SimpleFont("Arial", 12), Brushes.Transparent, Brushes.DarkSlateGray, 70);
        }

        private void DrawBottomRightBox()
        {
            string maxLongTarget = bullishTrendLengths.Count > 0 ? bullishTrendLengths.Max().ToString("F0") : "N/A";
            string maxLongPullback = bullishTrendMaxPullbacks.Count > 0 ? bullishTrendMaxPullbacks.Max().ToString("F0") : "N/A";
            string maxShortTarget = bearishTrendLengths.Count > 0 ? bearishTrendLengths.Max().ToString("F0") : "N/A";
            string maxShortPullback = bearishTrendMaxPullbacks.Count > 0 ? bearishTrendMaxPullbacks.Max().ToString("F0") : "N/A";

            string boxText = string.Format(
                "--- MAX POTENTIAL (ticks) ---\n" +
                "Max Long Target: {0}\n" +
                "  (Max Pullback: {1})\n" +
                "Max Short Target: {2}\n" +
                "  (Max Pullback: {3})",
                maxLongTarget, maxLongPullback,
                maxShortTarget, maxShortPullback
            );

            Draw.TextFixed(this, "BottomRightBox", boxText, TextPosition.BottomRight, Brushes.White, new SimpleFont("Arial", 12) { Bold = true }, Brushes.Transparent, Brushes.DarkSlateGray, 70);
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
		private W2kSwings5[] cacheW2kSwings5;
		public W2kSwings5 W2kSwings5(int strength)
		{
			return W2kSwings5(Input, strength);
		}

		public W2kSwings5 W2kSwings5(ISeries<double> input, int strength)
		{
			if (cacheW2kSwings5 != null)
				for (int idx = 0; idx < cacheW2kSwings5.Length; idx++)
					if (cacheW2kSwings5[idx] != null && cacheW2kSwings5[idx].Strength == strength && cacheW2kSwings5[idx].EqualsInput(input))
						return cacheW2kSwings5[idx];
			return CacheIndicator<W2kSwings5>(new W2kSwings5(){ Strength = strength }, input, ref cacheW2kSwings5);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.W2kSwings5 W2kSwings5(int strength)
		{
			return indicator.W2kSwings5(Input, strength);
		}

		public Indicators.W2kSwings5 W2kSwings5(ISeries<double> input , int strength)
		{
			return indicator.W2kSwings5(input, strength);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.W2kSwings5 W2kSwings5(int strength)
		{
			return indicator.W2kSwings5(Input, strength);
		}

		public Indicators.W2kSwings5 W2kSwings5(ISeries<double> input , int strength)
		{
			return indicator.W2kSwings5(input, strength);
		}
	}
}

#endregion
