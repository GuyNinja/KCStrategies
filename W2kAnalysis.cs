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
    public class W2kAnalysis : Indicator
    {
        #region State Management Variables
        private List<SwingPoint> collectedSwings;
        private List<double> measuredMoves, pullbacks, allRanges;
        private List<double> bullishMeasuredMoves, bearishMeasuredMoves;
        private List<double> bullishPullbacks, bearishPullbacks;
        private List<double> bullishTrendLengths, bearishTrendLengths;
        private List<double> bullishTrendMaxPullbacks, bearishTrendMaxPullbacks;

        private bool isDataProcessed = false;
        private bool hasEnoughData = true; 
        private DateTime analysisStartDate;
        private DateTime analysisEndDate;
        
        private System.Random random = new System.Random();
        private int reminderFlashCounter, apexFlashCounter;
        private int nextReminderBar, nextApexFlashBar;
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
                Description = @"Analyzes swings from user-defined historical periods and trading sessions.";
                Name = "W2kAnalysis";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                DrawOnPricePanel = true;
                PaintPriceMarkers = false;

                Strength = 12;
                DaysToLoad = 14;

                Session1StartTime = new TimeSpan(7, 0, 0);
                Session1EndTime = new TimeSpan(11, 0, 0);
                
                EnableSession2 = false;
                Session2StartTime = new TimeSpan(12, 0, 0);
                Session2EndTime = new TimeSpan(16, 0, 0);

                EnableSession3 = false;
                Session3StartTime = new TimeSpan(18, 0, 0);
                Session3EndTime = new TimeSpan(22, 0, 0);
            }
            else if (State == State.DataLoaded)
            {
                if (!isDataProcessed)
                {
                    ProcessHistoricalData();
                    isDataProcessed = true;
                }
            }
        }

        private void ProcessHistoricalData()
        {
            // --- Initialize all data lists ---
            collectedSwings = new List<SwingPoint>();
            measuredMoves = new List<double>();
            pullbacks = new List<double>();
            allRanges = new List<double>();
            bullishMeasuredMoves = new List<double>();
            bearishMeasuredMoves = new List<double>();
            bullishPullbacks = new List<double>();
            bearishPullbacks = new List<double>();
            bullishTrendLengths = new List<double>();
            bearishTrendLengths = new List<double>();
            bullishTrendMaxPullbacks = new List<double>();
            bearishTrendMaxPullbacks = new List<double>();
            
            DateTime today = DateTime.Now.Date;
            analysisEndDate = today.AddDays(-(int)today.DayOfWeek - 2); 
            analysisStartDate = analysisEndDate.AddDays(-DaysToLoad);

            if (Bars.Count == 0 || Bars.GetTime(0).Date > analysisStartDate)
            {
                hasEnoughData = false;
                return;
            }

            CollectSwings();
            CalculateStats();
            
            nextReminderBar = CurrentBar + random.Next(240, 601); 
            nextApexFlashBar = CurrentBar + 240; 
            reminderFlashCounter = 0;
            apexFlashCounter = 0;
        }

        private void CollectSwings()
        {
            TimeZoneInfo easternZone;
            try { easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); }
            catch (TimeZoneNotFoundException) { Print("Eastern Standard Time zone not found."); return; }

            Swing swingHighs = Swing(Strength);
            Swing swingLows = Swing(Strength);
            
            for (int i = 0; i < Bars.Count; i++)
            {
                DateTime barTime = Bars.GetTime(i);
                DateTime estTime = TimeZoneInfo.ConvertTime(barTime, TimeZoneInfo.Local, easternZone);

                if (estTime.Date >= analysisStartDate.Date && estTime.Date <= analysisEndDate.Date)
                {
                    TimeSpan estTimeOfDay = estTime.TimeOfDay;
                    bool inSession = (estTimeOfDay >= Session1StartTime && estTimeOfDay <= Session1EndTime) ||
                                     (EnableSession2 && estTimeOfDay >= Session2StartTime && estTimeOfDay <= Session2EndTime) ||
                                     (EnableSession3 && estTimeOfDay >= Session3StartTime && estTimeOfDay <= Session3EndTime);

                    if (inSession)
                    {
                        if (swingHighs.High[i] > 0 && (i == 0 || swingHighs.High[i] != swingHighs.High[i-1]))
                            collectedSwings.Add(new SwingPoint { Price = swingHighs.High[i], Time = estTime, IsHigh = true });
                        if (swingLows.Low[i] > 0 && (i == 0 || swingLows.Low[i] != swingLows.Low[i-1]))
                            collectedSwings.Add(new SwingPoint { Price = swingLows.Low[i], Time = estTime, IsHigh = false });
                    }
                }
            }
            
            collectedSwings = collectedSwings.OrderBy(s => s.Time).ToList();
        }

        private void CalculateStats()
        {
            if (collectedSwings.Count < 4) return;

            for (int i = 1; i < collectedSwings.Count; i++)
            {
                SwingPoint currentSwing = collectedSwings[i];
                var priorSimilarSwings = collectedSwings.Where(s => s.IsHigh == currentSwing.IsHigh && s.Time < currentSwing.Time).ToList();
                if (priorSimilarSwings.Count == 0) continue;
                SwingPoint priorSwing = priorSimilarSwings.Last();
                if(currentSwing.IsHigh) currentSwing.Label = currentSwing.Price > priorSwing.Price ? "HH" : "LH";
                else currentSwing.Label = currentSwing.Price < priorSwing.Price ? "LL" : "HL";
            }

            for (int i = 1; i < collectedSwings.Count; i++)
            {
                SwingPoint currentSwing = collectedSwings[i];
                SwingPoint priorSwing = collectedSwings[i-1];
                allRanges.Add(Math.Abs(currentSwing.Price - priorSwing.Price) / TickSize);
                if(currentSwing.IsHigh && currentSwing.Label == "HH" && !priorSwing.IsHigh && priorSwing.Label == "HL") { double move = Math.Abs(currentSwing.Price - priorSwing.Price) / TickSize; measuredMoves.Add(move); bullishMeasuredMoves.Add(move); }
                else if(!currentSwing.IsHigh && currentSwing.Label == "LL" && priorSwing.IsHigh && priorSwing.Label == "LH") { double move = Math.Abs(currentSwing.Price - priorSwing.Price) / TickSize; measuredMoves.Add(move); bearishMeasuredMoves.Add(move); }
                if(!currentSwing.IsHigh && currentSwing.Label == "HL" && priorSwing.IsHigh && priorSwing.Label == "HH") { double pullback = Math.Abs(currentSwing.Price - priorSwing.Price) / TickSize; pullbacks.Add(pullback); bullishPullbacks.Add(pullback); }
                else if(currentSwing.IsHigh && currentSwing.Label == "LH" && !priorSwing.IsHigh && priorSwing.Label == "LL") { double pullback = Math.Abs(currentSwing.Price - priorSwing.Price) / TickSize; pullbacks.Add(pullback); bearishPullbacks.Add(pullback); }
            }

            AnalyzeTrendSegments();
        }

        private void AnalyzeTrendSegments()
        {
            int trendStartIndex = -1;
            bool isBullTrend = false;
            for (int i = 2; i < collectedSwings.Count; i++)
            {
                bool isBullSequence = collectedSwings[i].Label == "HH" && collectedSwings[i-1].Label == "HL";
                bool isBearSequence = collectedSwings[i].Label == "LL" && collectedSwings[i-1].Label == "LH";
                if (trendStartIndex == -1)
                {
                    if (isBullSequence) { trendStartIndex = i - 1; isBullTrend = true; }
                    else if (isBearSequence) { trendStartIndex = i - 1; isBullTrend = false; }
                }
                else
                {
                    bool trendBroken = (isBullTrend && collectedSwings[i].Label == "LL") || (!isBullTrend && collectedSwings[i].Label == "HH");
                    if (trendBroken || i == collectedSwings.Count - 1)
                    {
                        int trendEndIndex = trendBroken ? i - 1 : i;
                        double startPrice = collectedSwings[trendStartIndex].Price;
                        double endPrice = collectedSwings[trendEndIndex].Price;
                        double trendLength = Math.Abs(endPrice - startPrice) / TickSize;
                        double maxPullback = 0;
                        for(int j = trendStartIndex + 1; j <= trendEndIndex; j++)
                        {
                            if((isBullTrend && collectedSwings[j].Label == "HL") || (!isBullTrend && collectedSwings[j].Label == "LH"))
                            {
                                double pullback = Math.Abs(collectedSwings[j].Price - collectedSwings[j-1].Price) / TickSize;
                                if(pullback > maxPullback) maxPullback = pullback;
                            }
                        }
                        if(isBullTrend) { bullishTrendLengths.Add(trendLength); bullishTrendMaxPullbacks.Add(maxPullback); }
                        else { bearishTrendLengths.Add(trendLength); bearishTrendMaxPullbacks.Add(maxPullback); }
                        trendStartIndex = -1;
                    }
                }
            }
        }

        protected override void OnBarUpdate()
        {
            // --- CORRECTED LOGIC ---
            // If data hasn't been processed, don't do anything.
            if (!isDataProcessed)
                return;
            
            // --- Handle Timers on every bar ---
            if (CurrentBar >= nextReminderBar && reminderFlashCounter <= 0) { reminderFlashCounter = 30; nextReminderBar = CurrentBar + random.Next(240, 601); }
            if (CurrentBar >= nextApexFlashBar && apexFlashCounter <= 0) { apexFlashCounter = 30; nextApexFlashBar = CurrentBar + 240; }
            if(reminderFlashCounter > 0) reminderFlashCounter--;
            if(apexFlashCounter > 0) apexFlashCounter--;

            // --- Draw everything on every bar now that data is processed ---
            if (hasEnoughData)
            {
                DrawTopLeftBox();
                DrawBottomLeftBox();
                DrawTopRightBox();
                DrawBottomRightBox();
            }
            else 
            {
                DrawDataWarningBox();
            }
            
            DrawReminderFlash();
            DrawApexFlash();
        }

        private void DrawTopLeftBox()
        {
            string avgTarget = measuredMoves.Count > 0 ? measuredMoves.Average().ToString("F0") : "N/A";
            string medTarget = measuredMoves.Count > 0 ? CalculateMedian(measuredMoves).ToString("F0") : "N/A";
            string avgRange = allRanges.Count > 0 ? allRanges.Average().ToString("F0") : "N/A";
            string medRange = allRanges.Count > 0 ? CalculateMedian(allRanges).ToString("F0") : "N/A";
            string avgTrail = pullbacks.Count > 0 ? pullbacks.Average().ToString("F0") : "N/A";
            string medTrail = pullbacks.Count > 0 ? CalculateMedian(pullbacks).ToString("F0") : "N/A";
            string boxText = string.Format("--- General ATM (ticks) ---\nTarget (Avg/Med): {0}/{1}\nStop (Avg/Med): {2}/{3}\nB/E (Avg/Med): {2}/{3}\nTrail (Avg/Med): {4}/{5}", avgTarget, medTarget, avgRange, medRange, avgTrail, medTrail);
            Draw.TextFixed(this, "TopLeftBox", boxText, TextPosition.TopLeft, Brushes.White, new SimpleFont("Arial", 12), Brushes.Transparent, Brushes.DarkSlateGray, 70);
        }

        private void DrawBottomLeftBox()
        {
            string minTarget = measuredMoves.Count > 0 ? measuredMoves.Min().ToString("F0") : "N/A";
            string minStop = allRanges.Count > 0 ? allRanges.Min().ToString("F0") : "N/A";
            string minTrail = pullbacks.Count > 0 ? pullbacks.Min().ToString("F0") : "N/A";
            string boxText = string.Format("--- SCALPER ATM (ticks) ---\nTarget (Min): {0}\nStop (Min): {1}\nB/E (Min): {1}\nTrail (Min): {2}", minTarget, minStop, minTrail);
            Draw.TextFixed(this, "BottomLeftBox", boxText, TextPosition.BottomLeft, Brushes.White, new SimpleFont("Arial", 12), Brushes.Transparent, Brushes.DarkSlateGray, 70);
        }

        private void DrawTopRightBox()
        {
            string longMove = string.Format("Moves (Avg/Med): {0}/{1}", bullishMeasuredMoves.Count > 0 ? bullishMeasuredMoves.Average().ToString("F0") : "N/A", bullishMeasuredMoves.Count > 0 ? CalculateMedian(bullishMeasuredMoves).ToString("F0") : "N/A");
            string longPullback = string.Format("Pullbacks (Avg/Med): {0}/{1}", bullishPullbacks.Count > 0 ? bullishPullbacks.Average().ToString("F0") : "N/A", bullishPullbacks.Count > 0 ? CalculateMedian(bullishPullbacks).ToString("F0") : "N/A");
            string shortMove = string.Format("Moves (Avg/Med): {0}/{1}", bearishMeasuredMoves.Count > 0 ? bearishMeasuredMoves.Average().ToString("F0") : "N/A", bearishMeasuredMoves.Count > 0 ? CalculateMedian(bearishMeasuredMoves).ToString("F0") : "N/A");
            string shortPullback = string.Format("Pullbacks (Avg/Med): {0}/{1}", bearishPullbacks.Count > 0 ? bearishPullbacks.Average().ToString("F0") : "N/A", bearishPullbacks.Count > 0 ? CalculateMedian(bearishPullbacks).ToString("F0") : "N/A");
            string boxText = string.Format("--- Trend-Specific ATM (ticks) ---\nLongs:\n  {0}\n  {1}\nShorts:\n  {2}\n  {3}", longMove, longPullback, shortMove, shortPullback);
            Draw.TextFixed(this, "TopRightBox", boxText, TextPosition.TopRight, Brushes.White, new SimpleFont("Arial", 12), Brushes.Transparent, Brushes.DarkSlateGray, 70);
        }

        private void DrawBottomRightBox()
        {
            string maxLongTarget = bullishTrendLengths.Count > 0 ? bullishTrendLengths.Max().ToString("F0") : "N/A";
            string maxLongPullback = bullishTrendMaxPullbacks.Count > 0 ? bullishTrendMaxPullbacks.Max().ToString("F0") : "N/A";
            string maxShortTarget = bearishTrendLengths.Count > 0 ? bearishTrendLengths.Max().ToString("F0") : "N/A";
            string maxShortPullback = bearishTrendMaxPullbacks.Count > 0 ? bearishTrendMaxPullbacks.Max().ToString("F0") : "N/A";
            string boxText = string.Format("--- MAX POTENTIAL (ticks) ---\nMax Long Target: {0}\n  (Max Pullback: {1})\nMax Short Target: {2}\n  (Max Pullback: {3})", maxLongTarget, maxLongPullback, maxShortTarget, maxShortPullback);
            Draw.TextFixed(this, "BottomRightBox", boxText, TextPosition.BottomRight, Brushes.White, new SimpleFont("Arial", 12) { Bold = true }, Brushes.Transparent, Brushes.DarkSlateGray, 70);
        }
        
        private void DrawReminderFlash()
        {
            RemoveDrawObject("ReminderFlash");
            if (reminderFlashCounter > 0)
            {
                Brush color = (reminderFlashCounter % 2 == 0) ? Brushes.LimeGreen : Brushes.Red;
                Draw.TextFixed(this, "ReminderFlash", "No One Ever Lost Money Taking Profits", TextPosition.Center, color, new SimpleFont("Arial", 20) { Bold = true }, Brushes.Transparent, Brushes.Black, 50);
            }
        }
        
        private void DrawApexFlash()
        {
            RemoveDrawObject("ApexFlash");
            if (apexFlashCounter > 0)
            {
                string text = "ApexTraderFunding Discount Code: GuyNinja\nShow your support and buy evals with this code!";
                Draw.TextFixed(this, "ApexFlash", text, TextPosition.Center, Brushes.Gold, new SimpleFont("Arial", 16) { Bold = true }, Brushes.Transparent, Brushes.Black, 60);
            }
        }

        private void DrawDataWarningBox()
        {
            string warningText = "Not enough data loaded.\n" +
                                 "Indicator requires at least " + DaysToLoad + " days of data.\n" +
                                 "Prototype works best on NQ 12-Range Bars.\n" +
                                 "Please load more historical data.";
            Draw.TextFixed(this, "DataWarningBox", warningText, TextPosition.Center, Brushes.White, new SimpleFont("Arial", 16), Brushes.Transparent, Brushes.Red, 80);
        }
        
        private double CalculateMedian(List<double> data)
        {
            if (data.Count == 0) return 0;
            List<double> sortedData = new List<double>(data);
            sortedData.Sort();
            int mid = sortedData.Count / 2;
            if (sortedData.Count % 2 == 0) return (sortedData[mid - 1] + sortedData[mid]) / 2;
            else return sortedData[mid];
        }

        #region User-Configurable Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Strength", Order=1, GroupName="Parameters")]
        public int Strength { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name="Days To Load", Description="Number of historical days to analyze for stats.", Order=2, GroupName="Parameters")]
        public int DaysToLoad { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Session 1 Start", Description="Start time for the first trading session (EST).", Order=3, GroupName="Sessions")]
        public TimeSpan Session1StartTime { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Session 1 End", Description="End time for the first trading session (EST).", Order=4, GroupName="Sessions")]
        public TimeSpan Session1EndTime { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable Session 2", Order=5, GroupName="Sessions")]
        public bool EnableSession2 { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Session 2 Start", Description="Start time for the second trading session (EST).", Order=6, GroupName="Sessions")]
        public TimeSpan Session2StartTime { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Session 2 End", Description="End time for the second trading session (EST).", Order=7, GroupName="Sessions")]
        public TimeSpan Session2EndTime { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable Session 3", Order=8, GroupName="Sessions")]
        public bool EnableSession3 { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Session 3 Start", Description="Start time for the third trading session (EST).", Order=9, GroupName="Sessions")]
        public TimeSpan Session3StartTime { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Session 3 End", Description="End time for the third trading session (EST).", Order=10, GroupName="Sessions")]
        public TimeSpan Session3EndTime { get; set; }
        #endregion
    }
}


#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private W2kAnalysis[] cacheW2kAnalysis;
		public W2kAnalysis W2kAnalysis(int strength, int daysToLoad, TimeSpan session1StartTime, TimeSpan session1EndTime, bool enableSession2, TimeSpan session2StartTime, TimeSpan session2EndTime, bool enableSession3, TimeSpan session3StartTime, TimeSpan session3EndTime)
		{
			return W2kAnalysis(Input, strength, daysToLoad, session1StartTime, session1EndTime, enableSession2, session2StartTime, session2EndTime, enableSession3, session3StartTime, session3EndTime);
		}

		public W2kAnalysis W2kAnalysis(ISeries<double> input, int strength, int daysToLoad, TimeSpan session1StartTime, TimeSpan session1EndTime, bool enableSession2, TimeSpan session2StartTime, TimeSpan session2EndTime, bool enableSession3, TimeSpan session3StartTime, TimeSpan session3EndTime)
		{
			if (cacheW2kAnalysis != null)
				for (int idx = 0; idx < cacheW2kAnalysis.Length; idx++)
					if (cacheW2kAnalysis[idx] != null && cacheW2kAnalysis[idx].Strength == strength && cacheW2kAnalysis[idx].DaysToLoad == daysToLoad && cacheW2kAnalysis[idx].Session1StartTime == session1StartTime && cacheW2kAnalysis[idx].Session1EndTime == session1EndTime && cacheW2kAnalysis[idx].EnableSession2 == enableSession2 && cacheW2kAnalysis[idx].Session2StartTime == session2StartTime && cacheW2kAnalysis[idx].Session2EndTime == session2EndTime && cacheW2kAnalysis[idx].EnableSession3 == enableSession3 && cacheW2kAnalysis[idx].Session3StartTime == session3StartTime && cacheW2kAnalysis[idx].Session3EndTime == session3EndTime && cacheW2kAnalysis[idx].EqualsInput(input))
						return cacheW2kAnalysis[idx];
			return CacheIndicator<W2kAnalysis>(new W2kAnalysis(){ Strength = strength, DaysToLoad = daysToLoad, Session1StartTime = session1StartTime, Session1EndTime = session1EndTime, EnableSession2 = enableSession2, Session2StartTime = session2StartTime, Session2EndTime = session2EndTime, EnableSession3 = enableSession3, Session3StartTime = session3StartTime, Session3EndTime = session3EndTime }, input, ref cacheW2kAnalysis);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.W2kAnalysis W2kAnalysis(int strength, int daysToLoad, TimeSpan session1StartTime, TimeSpan session1EndTime, bool enableSession2, TimeSpan session2StartTime, TimeSpan session2EndTime, bool enableSession3, TimeSpan session3StartTime, TimeSpan session3EndTime)
		{
			return indicator.W2kAnalysis(Input, strength, daysToLoad, session1StartTime, session1EndTime, enableSession2, session2StartTime, session2EndTime, enableSession3, session3StartTime, session3EndTime);
		}

		public Indicators.W2kAnalysis W2kAnalysis(ISeries<double> input , int strength, int daysToLoad, TimeSpan session1StartTime, TimeSpan session1EndTime, bool enableSession2, TimeSpan session2StartTime, TimeSpan session2EndTime, bool enableSession3, TimeSpan session3StartTime, TimeSpan session3EndTime)
		{
			return indicator.W2kAnalysis(input, strength, daysToLoad, session1StartTime, session1EndTime, enableSession2, session2StartTime, session2EndTime, enableSession3, session3StartTime, session3EndTime);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.W2kAnalysis W2kAnalysis(int strength, int daysToLoad, TimeSpan session1StartTime, TimeSpan session1EndTime, bool enableSession2, TimeSpan session2StartTime, TimeSpan session2EndTime, bool enableSession3, TimeSpan session3StartTime, TimeSpan session3EndTime)
		{
			return indicator.W2kAnalysis(Input, strength, daysToLoad, session1StartTime, session1EndTime, enableSession2, session2StartTime, session2EndTime, enableSession3, session3StartTime, session3EndTime);
		}

		public Indicators.W2kAnalysis W2kAnalysis(ISeries<double> input , int strength, int daysToLoad, TimeSpan session1StartTime, TimeSpan session1EndTime, bool enableSession2, TimeSpan session2StartTime, TimeSpan session2EndTime, bool enableSession3, TimeSpan session3StartTime, TimeSpan session3EndTime)
		{
			return indicator.W2kAnalysis(input, strength, daysToLoad, session1StartTime, session1EndTime, enableSession2, session2StartTime, session2EndTime, enableSession3, session3StartTime, session3EndTime);
		}
	}
}

#endregion
