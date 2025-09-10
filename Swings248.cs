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
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds all indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
    public class Swings248 : Indicator
    {
        #region State Management Variables
        private List<SwingPoint> historicalHighs;
        private List<SwingPoint> historicalLows;
        private List<double> bullishMeasuredMoves;
        private List<double> bearishMeasuredMoves;
        private List<double> bullishPullbacks;
        private List<double> bearishPullbacks;
        private List<double> allRanges;

        private Swing swingHighsProvider;
        private Swing swingLowsProvider;
        private EMA ema200;
        private EMA ema500;
        private ATR atr;

        private double lastSwingHighValue;
        private double lastSwingLowValue;

        // --- State Machine Variables ---
        private enum MacroTrendState { Neutral, Bullish, Bearish }
        private enum CurrentTrendState { Sideways, WeakBullish, WeakBearish, TrendingBullish, TrendingBearish, StronglyBullish, StronglyBearish, Killbox }
        private enum MicroTrendState { Following, EarlyBirdLong, EarlyBirdShort }
        
        private MacroTrendState macroTrend;
        private CurrentTrendState currentTrend;
        private CurrentTrendState previousCurrentTrend;
        private MicroTrendState microTrend;

        private int sequenceCount;
        private int barsInTrend;
        private int flashCounter;
        
        private System.Random random = new System.Random();
        private int reminderFlashCounter;
        private int nextReminderBar;
        private int atmFlashCounter;
        #endregion

        private class SwingPoint
        {
            public double Price { get; set; }
            public int BarNumber { get; set; }
            public string Label { get; set; }
            public string Tag { get; set; }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Advanced Swing Analysis with ATM Strategy Recommendations.";
                Name = "Swings248";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                PaintPriceMarkers = false;

                Strength = 12;
                NumSwingsToLabel = 30;
                Ema200Period = 200;
                Ema500Period = 500;
                AtrPeriod = 14;
                KillboxRangeMultiplier = 2.0;

                AddPlot(new Stroke(Brushes.LimeGreen, 2), PlotStyle.Dot, "SwingHighPlot");
                AddPlot(new Stroke(Brushes.Red, 2), PlotStyle.Dot, "SwingLowPlot");
            }
            else if (State == State.Configure)
            {
                swingHighsProvider = Swing(Strength);
                swingLowsProvider = Swing(Strength);
                ema200 = EMA(Ema200Period);
                ema500 = EMA(Ema500Period);
                atr = ATR(AtrPeriod);
            }
            else if (State == State.DataLoaded)
            {
                historicalHighs = new List<SwingPoint>();
                historicalLows = new List<SwingPoint>();
                bullishMeasuredMoves = new List<double>();
                bearishMeasuredMoves = new List<double>();
                bullishPullbacks = new List<double>();
                bearishPullbacks = new List<double>();
                allRanges = new List<double>();
                lastSwingHighValue = 0;
                lastSwingLowValue = 0;

                macroTrend = MacroTrendState.Neutral;
                currentTrend = CurrentTrendState.Sideways;
                previousCurrentTrend = CurrentTrendState.Sideways;
                microTrend = MicroTrendState.Following;
                sequenceCount = 0;
                barsInTrend = 0;
                flashCounter = 0;
                
                nextReminderBar = CurrentBar + random.Next(120, 301);
                reminderFlashCounter = 0;
                atmFlashCounter = 0;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Ema500Period) return;
            
            if (flashCounter > 0) flashCounter--;
            if (reminderFlashCounter > 0) reminderFlashCounter--;
            
            atmFlashCounter++;

            if (CurrentBar >= nextReminderBar)
            {
                reminderFlashCounter = 30;
                nextReminderBar = CurrentBar + random.Next(120, 301);
            }

            double currentSwingHigh = swingHighsProvider.SwingHigh[0];
            double currentSwingLow = swingLowsProvider.SwingLow[0];

            if (currentSwingHigh > 0 && currentSwingHigh != lastSwingHighValue)
            {
                ProcessNewSwingHigh(currentSwingHigh);
                lastSwingHighValue = currentSwingHigh;
            }

            if (currentSwingLow > 0 && currentSwingLow != lastSwingLowValue)
            {
                ProcessNewSwingLow(currentSwingLow);
                lastSwingLowValue = currentSwingLow;
            }
            
            UpdateTrends();
            DrawIndicator();
        }

        private void ProcessNewSwingHigh(double currentSwingHigh)
        {
            int swingHighBarsAgo = swingHighsProvider.SwingHighBar(0, 1, 100);
            if (swingHighBarsAgo < 0) return;

            string label = "H";
            SwingPoint lastHigh = historicalHighs.LastOrDefault();
            SwingPoint lastLow = historicalLows.LastOrDefault();

            if (lastHigh != null)
            {
                label = currentSwingHigh > lastHigh.Price ? "HH" : "LH";
            }
            
            if(lastLow != null)
            {
                allRanges.Add(Math.Abs(currentSwingHigh - lastLow.Price) / TickSize);
            }

            if (label == "HH" && lastLow != null && lastLow.Label.Contains("HL"))
            {
                double move = Math.Abs(currentSwingHigh - lastLow.Price) / TickSize;
                bullishMeasuredMoves.Add(move);
            }
            else if (label == "LH" && lastLow != null && lastLow.Label.Contains("LL"))
            {
                double pullback = Math.Abs(currentSwingHigh - lastLow.Price) / TickSize;
                bearishPullbacks.Add(pullback);
            }

            historicalHighs.Add(new SwingPoint { Price = currentSwingHigh, BarNumber = CurrentBar - swingHighBarsAgo, Label = label, Tag = "SLabelH" + (CurrentBar - swingHighBarsAgo) });
        }

        private void ProcessNewSwingLow(double currentSwingLow)
        {
            int swingLowBarsAgo = swingLowsProvider.SwingLowBar(0, 1, 100);
            if (swingLowBarsAgo < 0) return;

            string label = "L";
            SwingPoint lastLow = historicalLows.LastOrDefault();
            SwingPoint lastHigh = historicalHighs.LastOrDefault();

            if (lastLow != null)
            {
                label = currentSwingLow < lastLow.Price ? "LL" : "HL";
            }
            
            if(lastHigh != null)
            {
                allRanges.Add(Math.Abs(lastHigh.Price - currentSwingLow) / TickSize);
            }

            if (label == "LL" && lastHigh != null && lastHigh.Label.Contains("LH"))
            {
                double move = Math.Abs(lastHigh.Price - currentSwingLow) / TickSize;
                bearishMeasuredMoves.Add(move);
            }
            else if (label == "HL" && lastHigh != null && lastHigh.Label.Contains("HH"))
            {
                double pullback = Math.Abs(lastHigh.Price - currentSwingLow) / TickSize;
                bullishPullbacks.Add(pullback);
            }

            historicalLows.Add(new SwingPoint { Price = currentSwingLow, BarNumber = CurrentBar - swingLowBarsAgo, Label = label, Tag = "SLabelL" + (CurrentBar - swingLowBarsAgo) });
        }

        private void UpdateTrends()
        {
            if (Close[0] > ema200[0] && Close[0] > ema500[0])
                macroTrend = MacroTrendState.Bullish;
            else if (Close[0] < ema200[0] && Close[0] < ema500[0])
                macroTrend = MacroTrendState.Bearish;
            else
                macroTrend = MacroTrendState.Neutral;

            if (CheckForKillbox())
            {
                currentTrend = CurrentTrendState.Killbox;
                sequenceCount = 0;
                microTrend = MicroTrendState.Following;
            }
            else
            {
                SwingPoint lastHigh = historicalHighs.LastOrDefault();
                SwingPoint lastLow = historicalLows.LastOrDefault();
                
                bool isTrendingUp = lastHigh != null && lastLow != null && lastHigh.Label == "HH" && lastLow.Label == "HL";
                bool isTrendingDown = lastHigh != null && lastLow != null && lastHigh.Label == "LH" && lastLow.Label == "LL";

                if (isTrendingUp)
                {
                    if (currentTrend != CurrentTrendState.TrendingBullish && currentTrend != CurrentTrendState.StronglyBullish) sequenceCount = 1; else sequenceCount++;
                    currentTrend = (sequenceCount > 1) ? CurrentTrendState.StronglyBullish : CurrentTrendState.TrendingBullish;
                }
                else if (isTrendingDown)
                {
                    if (currentTrend != CurrentTrendState.TrendingBearish && currentTrend != CurrentTrendState.StronglyBearish) sequenceCount = 1; else sequenceCount++;
                    currentTrend = (sequenceCount > 1) ? CurrentTrendState.StronglyBearish : CurrentTrendState.TrendingBearish;
                }
                else
                {
                    sequenceCount = 0;
                    if (Close[0] > ema200[0])
                        currentTrend = CurrentTrendState.WeakBullish;
                    else if (Close[0] < ema200[0])
                        currentTrend = CurrentTrendState.WeakBearish;
                    else
                        currentTrend = CurrentTrendState.Sideways;
                }
                
                bool isBullishContext = currentTrend == CurrentTrendState.WeakBullish || currentTrend == CurrentTrendState.TrendingBullish || currentTrend == CurrentTrendState.StronglyBullish;
                bool isBearishContext = currentTrend == CurrentTrendState.WeakBearish || currentTrend == CurrentTrendState.TrendingBearish || currentTrend == CurrentTrendState.StronglyBearish;

                if (isBullishContext && isTrendingDown)
                {
                    microTrend = MicroTrendState.EarlyBirdShort;
                    flashCounter = 10;
                }
                else if (isBearishContext && isTrendingUp)
                {
                    microTrend = MicroTrendState.EarlyBirdLong;
                    flashCounter = 10;
                }
                else
                {
                    microTrend = MicroTrendState.Following;
                }
            }
            
            if (currentTrend != previousCurrentTrend)
            {
                barsInTrend = 0;
                previousCurrentTrend = currentTrend;
            }
            else
            {
                barsInTrend++;
            }
        }
        
        private bool CheckForKillbox()
        {
            if (historicalHighs.Count < 4 || historicalLows.Count < 4) return false;

            double killboxThreshold = atr[0] * KillboxRangeMultiplier;

            var lastFourHighs = historicalHighs.Skip(historicalHighs.Count - 4).ToList();
            var lastFourLows = historicalLows.Skip(historicalLows.Count - 4).ToList();

            double highRange = lastFourHighs.Max(h => h.Price) - lastFourHighs.Min(h => h.Price);
            double lowRange = lastFourLows.Max(l => l.Price) - lastFourLows.Min(l => l.Price);
            
            if (highRange < killboxThreshold && lowRange < killboxThreshold)
            {
                return true;
            }

            return false;
        }

        private void DrawIndicator()
        {
            RemoveDrawObject("KillboxRect");
            RemoveDrawObject("KillboxText");

            while (historicalHighs.Count > NumSwingsToLabel) { RemoveDrawObject(historicalHighs[0].Tag); historicalHighs.RemoveAt(0); }
            while (historicalLows.Count > NumSwingsToLabel) { RemoveDrawObject(historicalLows[0].Tag); historicalLows.RemoveAt(0); }
            while (bullishMeasuredMoves.Count > 200) { bullishMeasuredMoves.RemoveAt(0); }
            while (bearishMeasuredMoves.Count > 200) { bearishMeasuredMoves.RemoveAt(0); }
            while (bullishPullbacks.Count > 200) { bullishPullbacks.RemoveAt(0); }
            while (bearishPullbacks.Count > 200) { bearishPullbacks.RemoveAt(0); }
            while (allRanges.Count > 200) { allRanges.RemoveAt(0); }

            if (swingHighsProvider.SwingHigh[0] > 0) Values[0][0] = swingHighsProvider.SwingHigh[0];
            if (swingLowsProvider.SwingLow[0] > 0) Values[1][0] = swingLowsProvider.SwingLow[0];

            foreach (var high in historicalHighs)
            {
                string label = high.Label;
                if(sequenceCount > 0 && (label == "HH" || label == "LH")) label += sequenceCount;
                Brush color = high.Label.Contains("HH") ? Brushes.LimeGreen : (high.Label.Contains("LH") ? Brushes.Red : Brushes.DimGray);
                Draw.Text(this, high.Tag, label, CurrentBar - high.BarNumber, high.Price + (TickSize * 5), color);
            }
            foreach (var low in historicalLows)
            {
                string label = low.Label;
                if(sequenceCount > 0 && (label == "LL" || label == "HL")) label += sequenceCount;
                Brush color = low.Label.Contains("LL") ? Brushes.Red : (low.Label.Contains("HL") ? Brushes.LimeGreen : Brushes.DimGray);
                Draw.Text(this, low.Tag, label, CurrentBar - low.BarNumber, low.Price - (TickSize * 5), color);
            }

            if (currentTrend == CurrentTrendState.Killbox) DrawKillbox();

            DrawStatusBox();
            DrawStatsBox();
            DrawAtmBox();
            DrawReminderFlash();
        }
        
        private void DrawKillbox()
        {
            if (historicalHighs.Count < 4 || historicalLows.Count < 4) return;
            
            var lastFourHighs = historicalHighs.Skip(historicalHighs.Count - 4).ToList();
            var lastFourLows = historicalLows.Skip(historicalLows.Count - 4).ToList();

            double top = lastFourHighs.Max(h => h.Price);
            double bottom = lastFourLows.Min(l => l.Price);
            int startBar = Math.Min(lastFourHighs.Min(h => h.BarNumber), lastFourLows.Min(l => l.BarNumber));
            int endBar = CurrentBar;

            Draw.Rectangle(this, "KillboxRect", false, CurrentBar - endBar, bottom, CurrentBar - startBar, top, Brushes.Transparent, Brushes.DarkRed, 10);
            Draw.TextFixed(this, "KillboxText", "KILLBOX", TextPosition.BottomRight, Brushes.Red, new SimpleFont("Arial", 16) { Bold = true }, Brushes.Transparent, Brushes.Transparent, 0);
        }

        private void DrawStatusBox()
        {
            string microText = "Following...";
            Brush microColor = Brushes.White;

            if (microTrend == MicroTrendState.EarlyBirdLong)
            {
                microText = "EARLY BIRD LONG";
                microColor = (flashCounter > 0 && flashCounter % 2 == 0) ? Brushes.LimeGreen : Brushes.Green;
            }
            else if (microTrend == MicroTrendState.EarlyBirdShort)
            {
                microText = "EARLY BIRD SHORT";
                microColor = (flashCounter > 0 && flashCounter % 2 == 0) ? Brushes.Red : Brushes.Maroon;
            }

            string statusText = string.Format("Macro: {0}\nCurrent: {1}\nMicro: {2}\nBars in Trend: {3}",
                GetMacroTrendText(),
                GetCurrentTrendText(),
                microText,
                barsInTrend);

            Draw.TextFixed(this, "StatusBox", statusText, TextPosition.TopLeft, Brushes.White, new SimpleFont("Arial", 12), Brushes.Transparent, Brushes.Transparent, 0);
            
            string microLine = string.Format("\n\nMicro: {0}", microText);
            Draw.TextFixed(this, "StatusBoxMicro", microLine, TextPosition.TopLeft, microColor, new SimpleFont("Arial", 12), Brushes.Transparent, Brushes.Transparent, 0);
        }
        
        private void DrawStatsBox()
        {
            var allMoves = bullishMeasuredMoves.Concat(bearishMeasuredMoves).ToList();
            var allPullbacks = bullishPullbacks.Concat(bearishPullbacks).ToList();

            string allMovesAvg = allMoves.Count > 0 ? allMoves.Average().ToString("F2") : "N/A";
            string allMovesMed = allMoves.Count > 0 ? CalculateMedian(allMoves).ToString("F2") : "N/A";
            string longMovesAvg = bullishMeasuredMoves.Count > 0 ? bullishMeasuredMoves.Average().ToString("F2") : "N/A";
            string longMovesMed = bullishMeasuredMoves.Count > 0 ? CalculateMedian(bullishMeasuredMoves).ToString("F2") : "N/A";
            string shortMovesAvg = bearishMeasuredMoves.Count > 0 ? bearishMeasuredMoves.Average().ToString("F2") : "N/A";
            string shortMovesMed = bearishMeasuredMoves.Count > 0 ? CalculateMedian(bearishMeasuredMoves).ToString("F2") : "N/A";

            string allPullbacksAvg = allPullbacks.Count > 0 ? allPullbacks.Average().ToString("F2") : "N/A";
            string allPullbacksMed = allPullbacks.Count > 0 ? CalculateMedian(allPullbacks).ToString("F2") : "N/A";
            string longPullbacksAvg = bullishPullbacks.Count > 0 ? bullishPullbacks.Average().ToString("F2") : "N/A";
            string longPullbacksMed = bullishPullbacks.Count > 0 ? CalculateMedian(bullishPullbacks).ToString("F2") : "N/A";
            string shortPullbacksAvg = bearishPullbacks.Count > 0 ? bearishPullbacks.Average().ToString("F2") : "N/A";
            string shortPullbacksMed = bearishPullbacks.Count > 0 ? CalculateMedian(bearishPullbacks).ToString("F2") : "N/A";
            
            string statsText = string.Format(
                "--- Measured Moves (ticks) ---\n" +
                "All (Avg/Med): {0}/{1}\n" +
                "Longs (Avg/Med): {2}/{3}\n" +
                "Shorts (Avg/Med): {4}/{5}\n" +
                "--- Pullbacks (ticks) ---\n" +
                "All (Avg/Med): {6}/{7}\n" +
                "Longs (Avg/Med): {8}/{9}\n" +
                "Shorts (Avg/Med): {10}/{11}",
                allMovesAvg, allMovesMed, longMovesAvg, longMovesMed, shortMovesAvg, shortMovesMed,
                allPullbacksAvg, allPullbacksMed, longPullbacksAvg, longPullbacksMed, shortPullbacksAvg, shortPullbacksMed
            );
                
            Draw.TextFixed(this, "StatsBox", statsText, TextPosition.TopRight, Brushes.White, new SimpleFont("Arial", 12), Brushes.Transparent, Brushes.Transparent, 0);
        }
        
        private void DrawAtmBox()
        {
            RemoveDrawObject("AtmBox");
            string title;
            string targetText, stopText, trailText;

            if ((atmFlashCounter / 150) % 2 == 0) // Approx 5 second flash on a 30-sec chart
            {
                title = "ATM Recs (Median)";
                targetText = bullishMeasuredMoves.Concat(bearishMeasuredMoves).Any() ? CalculateMedian(bullishMeasuredMoves.Concat(bearishMeasuredMoves).ToList()).ToString("F0") : "N/A";
                stopText = allRanges.Any() ? CalculateMedian(allRanges).ToString("F0") : "N/A";
                trailText = bullishPullbacks.Concat(bearishPullbacks).Any() ? CalculateMedian(bullishPullbacks.Concat(bearishPullbacks).ToList()).ToString("F0") : "N/A";
            }
            else
            {
                title = "ATM Recs (Average)";
                targetText = bullishMeasuredMoves.Concat(bearishMeasuredMoves).Any() ? bullishMeasuredMoves.Concat(bearishMeasuredMoves).Average().ToString("F0") : "N/A";
                stopText = allRanges.Any() ? allRanges.Average().ToString("F0") : "N/A";
                trailText = bullishPullbacks.Concat(bearishPullbacks).Any() ? bullishPullbacks.Concat(bearishPullbacks).Average().ToString("F0") : "N/A";
            }
            
            string atmText = string.Format("{0}\n Target: {1}\n Stop: {2}\n B/E: {2}\n Trail: {3}",
                title, targetText, stopText, trailText);

            Draw.TextFixed(this, "AtmBox", atmText, TextPosition.BottomRight, Brushes.White, new SimpleFont("Arial", 12), Brushes.Transparent, Brushes.Transparent, 0);
        }
        
        private void DrawReminderFlash()
        {
            RemoveDrawObject("ReminderFlash");
            if (reminderFlashCounter > 0)
            {
                Brush color = (reminderFlashCounter % 2 == 0) ? Brushes.LimeGreen : Brushes.Red;
                Draw.TextFixed(this, "ReminderFlash", "No One Ever Lost Money Taking Profits", TextPosition.Center, color, new SimpleFont("Arial", 20) { Bold = true }, Brushes.Transparent, Brushes.Transparent, 0);
            }
        }
        
        private int CalculateBarsForNextReminder()
        {
            return random.Next(240, 601);
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
        
        private string GetMacroTrendText()
        {
            switch (macroTrend)
            {
                case MacroTrendState.Bullish: return "Bullish";
                case MacroTrendState.Bearish: return "Bearish";
                default: return "Neutral/Choppy";
            }
        }
        
        private string GetCurrentTrendText()
        {
            switch(currentTrend)
            {
                case CurrentTrendState.WeakBullish: return "Weak Bullish";
                case CurrentTrendState.WeakBearish: return "Weak Bearish";
                case CurrentTrendState.TrendingBullish: return "Trending Bullish";
                case CurrentTrendState.TrendingBearish: return "Trending Bearish";
                case CurrentTrendState.StronglyBullish: return "Strongly Bullish";
                case CurrentTrendState.StronglyBearish: return "Strongly Bearish";
                case CurrentTrendState.Killbox: return "Killbox / Choppy";
                default: return "Sideways";
            }
        }

        #region User-Configurable Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Strength", Order=1, GroupName="Parameters")]
        public int Strength { get; set; }

        [NinjaScriptProperty]
        [Range(1, 50)]
        [Display(Name="NumSwingsToLabel", Description="Number of recent swings to keep labels for.", Order=2, GroupName="Parameters")]
        public int NumSwingsToLabel { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, 1000)]
        [Display(Name="Ema 200 Period", Description="Period for the faster EMA.", Order=3, GroupName="Parameters")]
        public int Ema200Period { get; set; }

        [NinjaScriptProperty]
        [Range(1, 2000)]
        [Display(Name="Ema 500 Period", Description="Period for the slower EMA.", Order=4, GroupName="Parameters")]
        public int Ema500Period { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name="Atr Period", Description="Period for the ATR calculation.", Order=5, GroupName="Parameters")]
        public int AtrPeriod { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.1, 10)]
        [Display(Name="Killbox Range Multiplier", Description="ATR multiplier to define the choppy range.", Order=6, GroupName="Parameters")]
        public double KillboxRangeMultiplier { get; set; }
        #endregion
    }
}


#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Swings248[] cacheSwings248;
		public Swings248 Swings248(int strength, int numSwingsToLabel, int ema200Period, int ema500Period, int atrPeriod, double killboxRangeMultiplier)
		{
			return Swings248(Input, strength, numSwingsToLabel, ema200Period, ema500Period, atrPeriod, killboxRangeMultiplier);
		}

		public Swings248 Swings248(ISeries<double> input, int strength, int numSwingsToLabel, int ema200Period, int ema500Period, int atrPeriod, double killboxRangeMultiplier)
		{
			if (cacheSwings248 != null)
				for (int idx = 0; idx < cacheSwings248.Length; idx++)
					if (cacheSwings248[idx] != null && cacheSwings248[idx].Strength == strength && cacheSwings248[idx].NumSwingsToLabel == numSwingsToLabel && cacheSwings248[idx].Ema200Period == ema200Period && cacheSwings248[idx].Ema500Period == ema500Period && cacheSwings248[idx].AtrPeriod == atrPeriod && cacheSwings248[idx].KillboxRangeMultiplier == killboxRangeMultiplier && cacheSwings248[idx].EqualsInput(input))
						return cacheSwings248[idx];
			return CacheIndicator<Swings248>(new Swings248(){ Strength = strength, NumSwingsToLabel = numSwingsToLabel, Ema200Period = ema200Period, Ema500Period = ema500Period, AtrPeriod = atrPeriod, KillboxRangeMultiplier = killboxRangeMultiplier }, input, ref cacheSwings248);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Swings248 Swings248(int strength, int numSwingsToLabel, int ema200Period, int ema500Period, int atrPeriod, double killboxRangeMultiplier)
		{
			return indicator.Swings248(Input, strength, numSwingsToLabel, ema200Period, ema500Period, atrPeriod, killboxRangeMultiplier);
		}

		public Indicators.Swings248 Swings248(ISeries<double> input , int strength, int numSwingsToLabel, int ema200Period, int ema500Period, int atrPeriod, double killboxRangeMultiplier)
		{
			return indicator.Swings248(input, strength, numSwingsToLabel, ema200Period, ema500Period, atrPeriod, killboxRangeMultiplier);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Swings248 Swings248(int strength, int numSwingsToLabel, int ema200Period, int ema500Period, int atrPeriod, double killboxRangeMultiplier)
		{
			return indicator.Swings248(Input, strength, numSwingsToLabel, ema200Period, ema500Period, atrPeriod, killboxRangeMultiplier);
		}

		public Indicators.Swings248 Swings248(ISeries<double> input , int strength, int numSwingsToLabel, int ema200Period, int ema500Period, int atrPeriod, double killboxRangeMultiplier)
		{
			return indicator.Swings248(input, strength, numSwingsToLabel, ema200Period, ema500Period, atrPeriod, killboxRangeMultiplier);
		}
	}
}

#endregion
