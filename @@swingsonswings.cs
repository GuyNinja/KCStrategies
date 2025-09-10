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
	// Class name changed to SwingsOnSwings
    public class SwingsOnSwings : Indicator
    {
        #region State Management Variables
        private List<SwingPoint> historicalHighs;
        private List<SwingPoint> historicalLows;
        private List<double> bullishMeasuredMoves;
        private List<double> bearishMeasuredMoves;
        private List<double> bullishPullbacks;
        private List<double> bearishPullbacks;
        private List<double> allRanges;
        private List<double> recentRanges;
        private List<MovingAverageTouch> maTouches;
        private List<EarlyBirdSignal> earlyBirdSignals;

        private Swing swingHighsProvider;
        private Swing swingLowsProvider;
        private EMA ema200;
        private EMA ema500;
        private EMA ema50;
        private EMA ema20;
        private ATR atr;
        private SMA sma20;

        private double lastSwingHighValue;
        private double lastSwingLowValue;
        private DateTime lastTradingSessionStart;

        // --- State Machine Variables ---
        private enum MacroTrendState { Neutral, Bullish, Bearish }
        private enum CurrentTrendState { Sideways, WeakBullish, WeakBearish, TrendingBullish, TrendingBearish, StronglyBullish, StronglyBearish, Killbox }
        private enum MicroTrendState { Following, EarlyBirdLong, EarlyBirdShort }
        private enum MarketCondition { Trending, Ranging, Volatile, Compressed }
        
        private MacroTrendState macroTrend;
        private CurrentTrendState currentTrend;
        private CurrentTrendState previousCurrentTrend;
        private MicroTrendState microTrend;
        private MarketCondition marketCondition;

        private int sequenceCount;
        private int barsInTrend;
        private int flashCounter;
        private int killboxAlertCounter;
        
        private System.Random random;
        private int reminderFlashCounter;
        private int nextReminderBar;
        private int atmFlashCounter;
        
        // ATM Strategy Data
        private AtmStrategy scalperAtm;
        private AtmStrategy modestAtm;
        private AtmStrategy hrhrAtm;
        
        // Trading Hours Management
        private bool isWithinTradingHours;
		private bool wasWithinTradingHours;
        private int barsSinceSessionStart;
        private double sessionHigh;
        private double sessionLow;
        private double sessionOpen;
        #endregion

        #region Helper Classes
        private class SwingPoint
        {
            public double Price { get; set; }
            public int BarNumber { get; set; }
            public string Label { get; set; }
            public string Tag { get; set; }
            public DateTime Time { get; set; }
        }

        private class MovingAverageTouch
        {
            public double Price { get; set; }
            public int BarNumber { get; set; }
            public string MaType { get; set; }
            public bool IsSupport { get; set; }
            public bool IsResistance { get; set; }
        }

        private class EarlyBirdSignal
        {
            public DateTime Time { get; set; }
            public string SignalType { get; set; }
            public double Price { get; set; }
            public string Description { get; set; }
            public double Strength { get; set; }
        }

        private class AtmStrategy
        {
            public string Name { get; set; }
            public int TargetTicks { get; set; }
            public int StopTicks { get; set; }
            public int BreakEvenTicks { get; set; }
            public int TrailTicks { get; set; }
            public double WinRate { get; set; }
            public double RiskRewardRatio { get; set; }
            public string Description { get; set; }
            public bool IsRecommended { get; set; }
        }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Advanced Swing Analysis Agent with ATM Strategy Recommendations and Separate Window Display.";
                Name = "SwingsOnSwings"; // Name property updated
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;
                DrawOnPricePanel = false;
                PaintPriceMarkers = false;

                Strength = 12;
                NumSwingsToLabel = 30;
                Ema200Period = 200;
                Ema500Period = 500;
                Ema50Period = 50;
                Ema20Period = 20;
                AtrPeriod = 14;
                KillboxRangeMultiplier = 2.0;
                RecentBarsForRange = 20;
                TradingHoursStart = 6;
                TradingHoursEnd = 10;
                EnableTradingHoursFilter = true;

                AddPlot(new Stroke(Brushes.Blue, 2), PlotStyle.Line, "TrendStrength");
                AddPlot(new Stroke(Brushes.Green, 2), PlotStyle.Line, "MarketCondition");
                AddPlot(new Stroke(Brushes.Orange, 2), PlotStyle.Line, "AtmRecommendation");
            }
            else if (State == State.Configure)
            {
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
                recentRanges = new List<double>();
                maTouches = new List<MovingAverageTouch>();
                earlyBirdSignals = new List<EarlyBirdSignal>();
				
				swingHighsProvider = Swing(Strength);
                swingLowsProvider = Swing(Strength);
                ema200 = EMA(Ema200Period);
                ema500 = EMA(Ema500Period);
                ema50 = EMA(Ema50Period);
                ema20 = EMA(Ema20Period);
                atr = ATR(AtrPeriod);
                sma20 = SMA(20);
                
                lastSwingHighValue = 0;
                lastSwingLowValue = 0;
                lastTradingSessionStart = DateTime.MinValue;

                macroTrend = MacroTrendState.Neutral;
                currentTrend = CurrentTrendState.Sideways;
                previousCurrentTrend = CurrentTrendState.Sideways;
                microTrend = MicroTrendState.Following;
                marketCondition = MarketCondition.Trending;
                
                sequenceCount = 0;
                barsInTrend = 0;
                flashCounter = 0;
                killboxAlertCounter = 0;
				
				random = new System.Random();
                
                nextReminderBar = CurrentBar + random.Next(120, 301);
                reminderFlashCounter = 0;
                atmFlashCounter = 0;
                
                scalperAtm = new AtmStrategy { Name = "Scalper" };
                modestAtm = new AtmStrategy { Name = "Modest" };
                hrhrAtm = new AtmStrategy { Name = "High Risk High Reward" };
                
                isWithinTradingHours = false;
				wasWithinTradingHours = false;
                barsSinceSessionStart = 0;
                sessionHigh = 0;
                sessionLow = 0;
                sessionOpen = 0;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Ema500Period) return;
			
			wasWithinTradingHours = isWithinTradingHours;
            
            if (flashCounter > 0) flashCounter--;
            if (reminderFlashCounter > 0) reminderFlashCounter--;
            if (killboxAlertCounter > 0) killboxAlertCounter--;
            
            atmFlashCounter++;

            if (CurrentBar >= nextReminderBar)
            {
                reminderFlashCounter = 30;
                nextReminderBar = CurrentBar + random.Next(120, 301);
            }

            CheckTradingHours();
            UpdateSessionData();

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
            
            CheckMovingAverageTouches();
            CheckEarlyBirdSignals();
            
            UpdateTrends();
            UpdateMarketCondition();
            GenerateAtmStrategies();
            DrawIndicator();
        }

        private void CheckTradingHours()
        {
            if (!EnableTradingHoursFilter)
            {
                isWithinTradingHours = true;
                return;
            }

            DateTime currentTime = Time[0];
            int currentHour = currentTime.Hour;
            DayOfWeek currentDay = currentTime.DayOfWeek;
            
            bool isWeekday = currentDay >= DayOfWeek.Monday && currentDay <= DayOfWeek.Friday;
            bool isWithinHours = currentHour >= TradingHoursStart && currentHour < TradingHoursEnd;
            
            isWithinTradingHours = isWeekday && isWithinHours;
            
            if (isWithinTradingHours && !wasWithinTradingHours)
            {
                lastTradingSessionStart = currentTime;
                barsSinceSessionStart = 0;
                sessionHigh = High[0];
                sessionLow = Low[0];
                sessionOpen = Open[0];
            }
        }

        private void UpdateSessionData()
        {
            if (isWithinTradingHours)
            {
                barsSinceSessionStart++;
                sessionHigh = Math.Max(sessionHigh, High[0]);
                sessionLow = Math.Min(sessionLow, Low[0]);
                if (barsSinceSessionStart == 1)
                {
                    sessionOpen = Open[0];
                }
            }
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
                double range = Math.Abs(currentSwingHigh - lastLow.Price) / TickSize;
                allRanges.Add(range);
                recentRanges.Add(range);
            }

            if (label == "HH" && lastLow != null && lastLow.Label.Contains("HL"))
            {
                double move = Math.Abs(currentSwingHigh - lastLow.Price) / TickSize;
                bullishMeasuredMoves.Add(move);
            }
            else if (label == "LH" && lastHigh != null && lastHigh.Label == "HH")
            {
                double pullback = Math.Abs(lastHigh.Price - currentSwingHigh) / TickSize;
                bearishPullbacks.Add(pullback);
            }

            historicalHighs.Add(new SwingPoint { 
                Price = currentSwingHigh, 
                BarNumber = CurrentBar - swingHighBarsAgo, 
                Label = label, 
                Tag = "SLabelH" + (CurrentBar - swingHighBarsAgo),
                Time = Time[swingHighBarsAgo]
            });
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
                double range = Math.Abs(lastHigh.Price - currentSwingLow) / TickSize;
                allRanges.Add(range);
                recentRanges.Add(range);
            }

            if (label == "LL" && lastHigh != null && lastHigh.Label.Contains("LH"))
            {
                double move = Math.Abs(lastHigh.Price - currentSwingLow) / TickSize;
                bearishMeasuredMoves.Add(move);
            }
            else if (label == "HL" && lastLow != null && lastLow.Label == "LL")
            {
                double pullback = Math.Abs(lastHigh.Price - currentSwingLow) / TickSize;
                bullishPullbacks.Add(pullback);
            }

            historicalLows.Add(new SwingPoint { 
                Price = currentSwingLow, 
                BarNumber = CurrentBar - swingLowBarsAgo, 
                Label = label, 
                Tag = "SLabelL" + (CurrentBar - swingLowBarsAgo),
                Time = Time[swingLowBarsAgo]
            });
        }

        private void CheckMovingAverageTouches()
        {
            if (Crosses(Close, ema20, 1))
            {
                maTouches.Add(new MovingAverageTouch {
                    Price = Close[0], BarNumber = CurrentBar, MaType = "EMA20",
                    IsSupport = Close[0] >= ema20[0], IsResistance = Close[0] <= ema20[0]
                });
            }
            
            if (Crosses(Close, ema50, 1))
            {
                 maTouches.Add(new MovingAverageTouch {
                    Price = Close[0], BarNumber = CurrentBar, MaType = "EMA50",
                    IsSupport = Close[0] >= ema50[0], IsResistance = Close[0] <= ema50[0]
                });
            }
            
            if (Crosses(Close, ema200, 1))
            {
                 maTouches.Add(new MovingAverageTouch {
                    Price = Close[0], BarNumber = CurrentBar, MaType = "EMA200",
                    IsSupport = Close[0] >= ema200[0], IsResistance = Close[0] <= ema200[0]
                });
            }
        }

        private void CheckEarlyBirdSignals()
        {
            SwingPoint lastHigh = historicalHighs.LastOrDefault();
            SwingPoint lastLow = historicalLows.LastOrDefault();
            
            if (lastHigh == null || lastLow == null) return;
            
            bool bullishContext = currentTrend == CurrentTrendState.WeakBullish || 
                                currentTrend == CurrentTrendState.TrendingBullish || 
                                currentTrend == CurrentTrendState.StronglyBullish;
            
            bool bearishContext = currentTrend == CurrentTrendState.WeakBearish || 
                                currentTrend == CurrentTrendState.TrendingBearish || 
                                currentTrend == CurrentTrendState.StronglyBearish;
            
            if (bearishContext && lastHigh.Label == "HH" && lastLow.Label == "HL")
            {
                double strength = CalculateSignalStrength(lastHigh, lastLow, true);
                earlyBirdSignals.Add(new EarlyBirdSignal {
                    Time = Time[0], SignalType = "Early Bird Long", Price = Close[0],
                    Description = "Bullish swing break in bearish context", Strength = strength
                });
            }
            
            if (bullishContext && lastHigh.Label == "LH" && lastLow.Label == "LL")
            {
                double strength = CalculateSignalStrength(lastHigh, lastLow, false);
                earlyBirdSignals.Add(new EarlyBirdSignal {
                    Time = Time[0], SignalType = "Early Bird Short", Price = Close[0],
                    Description = "Bearish swing break in bullish context", Strength = strength
                });
            }
            
            CheckMicroTrendDivergence();
        }

        private void CheckMicroTrendDivergence()
        {
            if (historicalHighs.Count < 3 || historicalLows.Count < 3) return;
            
            var recentHighs = historicalHighs.Skip(historicalHighs.Count - 3).ToList();
            var recentLows = historicalLows.Skip(historicalLows.Count - 3).ToList();
            
            bool isMicroBull = recentHighs[2].Price > recentHighs[1].Price && recentHighs[1].Price > recentHighs[0].Price &&
                               recentLows[2].Price > recentLows[1].Price && recentLows[1].Price > recentLows[0].Price;
            
            bool isMicroBear = recentHighs[2].Price < recentHighs[1].Price && recentHighs[1].Price < recentHighs[0].Price &&
                               recentLows[2].Price < recentLows[1].Price && recentLows[1].Price < recentLows[0].Price;
            
            if (macroTrend == MacroTrendState.Bullish && isMicroBear)
            {
                earlyBirdSignals.Add(new EarlyBirdSignal {
                    Time = Time[0], SignalType = "Micro Bearish Divergence", Price = Close[0],
                    Description = "Micro trend bearish in bullish macro context", Strength = 0.7
                });
            }
            else if (macroTrend == MacroTrendState.Bearish && isMicroBull)
            {
                earlyBirdSignals.Add(new EarlyBirdSignal {
                    Time = Time[0], SignalType = "Micro Bullish Divergence", Price = Close[0],
                    Description = "Micro trend bullish in bearish macro context", Strength = 0.7
                });
            }
        }

        private double CalculateSignalStrength(SwingPoint high, SwingPoint low, bool isLong)
        {
            double baseStrength = 0.5;
            if (atr[0] == 0) return baseStrength;

            double distanceFromEma200 = Math.Abs(Close[0] - ema200[0]) / atr[0];
            if (distanceFromEma200 < 1.0) baseStrength += 0.2;
            
            if (atr[0] > SMA(atr, 20)[0]) baseStrength += 0.1;
            
            return Math.Min(baseStrength, 1.0);
        }

        private void UpdateTrends()
        {
            if (Close[0] > ema200[0] && ema200[0] > ema500[0])
                macroTrend = MacroTrendState.Bullish;
            else if (Close[0] < ema200[0] && ema200[0] < ema500[0])
                macroTrend = MacroTrendState.Bearish;
            else
                macroTrend = MacroTrendState.Neutral;

            if (CheckForKillbox())
            {
                currentTrend = CurrentTrendState.Killbox;
                sequenceCount = 0;
                microTrend = MicroTrendState.Following;
                if(killboxAlertCounter <= 0) killboxAlertCounter = 50; 
            }
            else
            {
                SwingPoint lastHigh = historicalHighs.LastOrDefault();
                SwingPoint lastLow = historicalLows.LastOrDefault();
                
                bool isTrendingUp = lastHigh != null && lastLow != null && lastHigh.Label == "HH" && lastLow.Label == "HL";
                bool isTrendingDown = lastHigh != null && lastLow != null && lastHigh.Label == "LH" && lastLow.Label == "LL";

                if (isTrendingUp)
                {
                    if (currentTrend != CurrentTrendState.TrendingBullish && currentTrend != CurrentTrendState.StronglyBullish) 
                        sequenceCount = 1; 
                    else 
                        sequenceCount++;
                    currentTrend = (sequenceCount > 2) ? CurrentTrendState.StronglyBullish : CurrentTrendState.TrendingBullish;
                }
                else if (isTrendingDown)
                {
                    if (currentTrend != CurrentTrendState.TrendingBearish && currentTrend != CurrentTrendState.StronglyBearish) 
                        sequenceCount = 1; 
                    else 
                        sequenceCount++;
                    currentTrend = (sequenceCount > 2) ? CurrentTrendState.StronglyBearish : CurrentTrendState.TrendingBearish;
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
                
                var latestSignal = earlyBirdSignals.LastOrDefault();
                if (latestSignal != null && (Time[0] - latestSignal.Time).TotalMinutes < 5)
                {
                    if (latestSignal.SignalType == "Early Bird Long")
                        microTrend = MicroTrendState.EarlyBirdLong;
                    else if (latestSignal.SignalType == "Early Bird Short")
                        microTrend = MicroTrendState.EarlyBirdShort;
                    else
                        microTrend = MicroTrendState.Following;
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
        
        private void UpdateMarketCondition()
        {
            double atrSma = SMA(atr, 20)[0];
            if (atrSma == 0) return;

            if (currentTrend == CurrentTrendState.Killbox)
            {
                marketCondition = MarketCondition.Compressed;
            }
            else if (atr[0] > atrSma * 1.5)
            {
                marketCondition = MarketCondition.Volatile;
            }
            else if (currentTrend == CurrentTrendState.Sideways || 
                     currentTrend == CurrentTrendState.WeakBullish || 
                     currentTrend == CurrentTrendState.WeakBearish)
            {
                marketCondition = MarketCondition.Ranging;
            }
            else
            {
                marketCondition = MarketCondition.Trending;
            }
        }
        
        private bool CheckForKillbox()
        {
            if (historicalHighs.Count < 4 || historicalLows.Count < 4 || atr[0] == 0) return false;

            double killboxThreshold = atr[0] * KillboxRangeMultiplier;

            var lastFourHighs = historicalHighs.Skip(Math.Max(0, historicalHighs.Count - 4)).ToList();
            var lastFourLows = historicalLows.Skip(Math.Max(0, historicalLows.Count - 4)).ToList();

            double highRange = lastFourHighs.Max(h => h.Price) - lastFourHighs.Min(h => h.Price);
            double lowRange = lastFourLows.Max(l => l.Price) - lastFourLows.Min(l => l.Price);
            
            return (highRange < killboxThreshold && lowRange < killboxThreshold);
        }

        private void GenerateAtmStrategies()
        {
            if ((!isWithinTradingHours && EnableTradingHoursFilter) || allRanges.Count == 0)
			{
				scalperAtm.IsRecommended = false;
				modestAtm.IsRecommended = false;
				hrhrAtm.IsRecommended = false;
				return;
			}
            
            var allMoves = bullishMeasuredMoves.Concat(bearishMeasuredMoves).ToList();
            var allPullbacks = bullishPullbacks.Concat(bearishPullbacks).ToList();
            
            if (allMoves.Count == 0 || allPullbacks.Count == 0) return;
            
            double medianMove = CalculateMedian(allMoves);
            double medianPullback = CalculateMedian(allPullbacks);
            double avgRecentRange = recentRanges.Count > 0 ? recentRanges.Average() : allRanges.Average();
            
            scalperAtm.TargetTicks = (int)(medianMove * 0.4);
            scalperAtm.StopTicks = (int)(avgRecentRange * 0.6);
            scalperAtm.BreakEvenTicks = (int)(avgRecentRange * 0.3);
            scalperAtm.TrailTicks = (int)(medianPullback * 0.5);
            scalperAtm.WinRate = 0.75;
            scalperAtm.RiskRewardRatio = (scalperAtm.StopTicks > 0) ? Math.Round((double)scalperAtm.TargetTicks / scalperAtm.StopTicks, 2) : 0;
            scalperAtm.Description = "Quick scalps, tight stops";
            
            modestAtm.TargetTicks = (int)(medianMove * 0.8);
            modestAtm.StopTicks = (int)(avgRecentRange * 0.8);
            modestAtm.BreakEvenTicks = (int)(avgRecentRange * 0.4);
            modestAtm.TrailTicks = (int)(medianPullback * 0.7);
            modestAtm.WinRate = 0.65;
            modestAtm.RiskRewardRatio = (modestAtm.StopTicks > 0) ? Math.Round((double)modestAtm.TargetTicks / modestAtm.StopTicks, 2) : 0;
            modestAtm.Description = "Balanced risk/reward";
            
            hrhrAtm.TargetTicks = (int)(medianMove * 1.5);
            hrhrAtm.StopTicks = (int)(avgRecentRange * 1.2);
            hrhrAtm.BreakEvenTicks = (int)(avgRecentRange * 0.6);
            hrhrAtm.TrailTicks = (int)(medianPullback * 1.0);
            hrhrAtm.WinRate = 0.45;
            hrhrAtm.RiskRewardRatio = (hrhrAtm.StopTicks > 0) ? Math.Round((double)hrhrAtm.TargetTicks / hrhrAtm.StopTicks, 2) : 0;
            hrhrAtm.Description = "High risk, high reward";

            scalperAtm.IsRecommended = marketCondition == MarketCondition.Trending && sequenceCount > 1;
            modestAtm.IsRecommended = marketCondition == MarketCondition.Trending || marketCondition == MarketCondition.Ranging;
            hrhrAtm.IsRecommended = marketCondition == MarketCondition.Volatile && sequenceCount > 2;
        }

        private void DrawIndicator()
        {
            while (historicalHighs.Count > NumSwingsToLabel) { RemoveDrawObject(historicalHighs[0].Tag); historicalHighs.RemoveAt(0); }
            while (historicalLows.Count > NumSwingsToLabel) { RemoveDrawObject(historicalLows[0].Tag); historicalLows.RemoveAt(0); }
            while (bullishMeasuredMoves.Count > 200) { bullishMeasuredMoves.RemoveAt(0); }
            while (bearishMeasuredMoves.Count > 200) { bearishMeasuredMoves.RemoveAt(0); }
            while (bullishPullbacks.Count > 200) { bullishPullbacks.RemoveAt(0); }
            while (bearishPullbacks.Count > 200) { bearishPullbacks.RemoveAt(0); }
            while (allRanges.Count > 200) { allRanges.RemoveAt(0); }
            while (recentRanges.Count > RecentBarsForRange) { recentRanges.RemoveAt(0); }
            while (maTouches.Count > 100) { maTouches.RemoveAt(0); }
            while (earlyBirdSignals.Count > 50) { earlyBirdSignals.RemoveAt(0); }

            if (!IsOverlay)
            {
                Values[0][0] = GetTrendStrength();
                Values[1][0] = GetMarketConditionValue();
                Values[2][0] = GetAtmRecommendationValue();
            }

            DrawStatusWindow();
            DrawAtmWindow();
            DrawKillboxAlert();
        }

        private double GetTrendStrength()
        {
            switch (currentTrend)
            {
                case CurrentTrendState.StronglyBullish: return 3.0;
                case CurrentTrendState.TrendingBullish: return 2.0;
                case CurrentTrendState.WeakBullish: return 1.0;
                case CurrentTrendState.Sideways: return 0.0;
                case CurrentTrendState.WeakBearish: return -1.0;
                case CurrentTrendState.TrendingBearish: return -2.0;
                case CurrentTrendState.StronglyBearish: return -3.0;
                case CurrentTrendState.Killbox: return 0.0;
                default: return 0.0;
            }
        }

        private double GetMarketConditionValue()
        {
            switch (marketCondition)
            {
                case MarketCondition.Trending: return 1.0;
                case MarketCondition.Ranging: return 0.5;
                case MarketCondition.Volatile: return 2.0;
                case MarketCondition.Compressed: return 0.0;
                default: return 0.0;
            }
        }

        private double GetAtmRecommendationValue()
        {
            if (hrhrAtm.IsRecommended) return 3.0;
            if (modestAtm.IsRecommended) return 2.0;
            if (scalperAtm.IsRecommended) return 1.0;
            return 0.0;
        }

        private void DrawStatusWindow()
        {
            var textBrush = Brushes.White;
            string statusText = string.Format(
                "=== SWINGS ON SWINGS ===\n" +
                "Trading Hours: {0}\n" +
                "Session Bars: {1}\n" +
                "Session Range: {2}\n\n" +
                "MACRO TREND: {3}\n" +
                "CURRENT TREND: {4}\n" +
                "MICRO TREND: {5}\n" +
                "MARKET COND: {6}\n" +
                "Bars in Trend: {7}\n\n" +
                "Sequence: {8}\n" +
                "ATR: {9:F2}\n" +
                "Range Comp: {10:F1}%",
                isWithinTradingHours ? "ACTIVE" : "INACTIVE",
                barsSinceSessionStart,
                (sessionHigh - sessionLow).ToString("F2"),
                GetMacroTrendText(),
                GetCurrentTrendText(),
                GetMicroTrendText(),
                GetMarketConditionText(),
                barsInTrend,
                sequenceCount,
                atr[0],
                GetRangeCompressionPercentage()
            );

            Draw.TextFixed(this, "StatusWindow", statusText, TextPosition.TopLeft, textBrush, 
                new SimpleFont("Consolas", 10), Brushes.Transparent, Brushes.Transparent, 0);
        }

        private void DrawAtmWindow()
        {
			var textBrush = Brushes.White;
            string atmText = string.Format(
                "=== ATM STRATEGIES ===\n\n" +
                "{0}SCALPER\n" +
                " TGT: {1} | STP: {2}\n" +
                " R/R: {3:F2} | BE: {4}\n\n" +
                "{5}MODEST\n" +
                " TGT: {6} | STP: {7}\n" +
                " R/R: {8:F2} | BE: {9}\n\n" +
                "{10}HRHR\n" +
                " TGT: {11} | STP: {12}\n" +
                " R/R: {13:F2} | BE: {14}",
                scalperAtm.IsRecommended ? "★ " : "", scalperAtm.TargetTicks, scalperAtm.StopTicks, scalperAtm.RiskRewardRatio, scalperAtm.BreakEvenTicks,
                modestAtm.IsRecommended ? "★ " : "", modestAtm.TargetTicks, modestAtm.StopTicks, modestAtm.RiskRewardRatio, modestAtm.BreakEvenTicks,
                hrhrAtm.IsRecommended ? "★ " : "", hrhrAtm.TargetTicks, hrhrAtm.StopTicks, hrhrAtm.RiskRewardRatio, hrhrAtm.BreakEvenTicks
            );

            Draw.TextFixed(this, "AtmWindow", atmText, TextPosition.TopRight, textBrush, 
                new SimpleFont("Consolas", 10), Brushes.Transparent, Brushes.Transparent, 0);
        }

        private void DrawKillboxAlert()
        {
            if (killboxAlertCounter > 0)
            {
                Brush color = (killboxAlertCounter % 10 < 5) ? Brushes.Red : Brushes.DarkRed;
                Draw.TextFixed(this, "KillboxAlert", "--- KILLBOX --- TRADE THE RANGE ---", TextPosition.Center, color, 
                    new SimpleFont("Arial", 24) { Bold = true }, Brushes.Transparent, Brushes.Transparent, 0);
            }
        }

        private double GetRangeCompressionPercentage()
        {
            if (recentRanges.Count < RecentBarsForRange || allRanges.Count < 50) return 0.0;
            
            var recentAvg = recentRanges.Average();
            var historicalAvg = allRanges.Take(allRanges.Count - RecentBarsForRange).Average();
            
            if (historicalAvg == 0) return 0.0;
            
            return ((historicalAvg - recentAvg) / historicalAvg) * 100;
        }
        
        private double CalculateMedian(List<double> data)
        {
            if (data.Count == 0) return 0;
            
            var sortedData = data.OrderBy(n => n).ToList();
            
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
                case MacroTrendState.Bullish: return "BULLISH";
                case MacroTrendState.Bearish: return "BEARISH";
                default: return "NEUTRAL";
            }
        }
        
        private string GetCurrentTrendText()
        {
            switch(currentTrend)
            {
                case CurrentTrendState.WeakBullish: return "Weak Bull";
                case CurrentTrendState.WeakBearish: return "Weak Bear";
                case CurrentTrendState.TrendingBullish: return "Trending Bull";
                case CurrentTrendState.TrendingBearish: return "Trending Bear";
                case CurrentTrendState.StronglyBullish: return "Strong Bull";
                case CurrentTrendState.StronglyBearish: return "Strong Bear";
                case CurrentTrendState.Killbox: return "KILLBOX";
                default: return "Sideways";
            }
        }

        private string GetMicroTrendText()
        {
            switch (microTrend)
            {
                case MicroTrendState.EarlyBirdLong: return "EB LONG";
                case MicroTrendState.EarlyBirdShort: return "EB SHORT";
                default: return "Following";
            }
        }

        private string GetMarketConditionText()
        {
            switch (marketCondition)
            {
                case MarketCondition.Trending: return "TRENDING";
                case MarketCondition.Ranging: return "RANGING";
                case MarketCondition.Volatile: return "VOLATILE";
                case MarketCondition.Compressed: return "COMPRESSED";
                default: return "UNKNOWN";
            }
        }

        #region User-Configurable Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Strength", Order=1, GroupName="Parameters")]
        public int Strength { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name="NumSwingsToLabel", Description="Number of recent swings to keep.", Order=2, GroupName="Parameters")]
        public int NumSwingsToLabel { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, 1000)]
        [Display(Name="Ema 200 Period", Description="Period for the 200 EMA.", Order=3, GroupName="Parameters")]
        public int Ema200Period { get; set; }

        [NinjaScriptProperty]
        [Range(1, 2000)]
        [Display(Name="Ema 500 Period", Description="Period for the 500 EMA.", Order=4, GroupName="Parameters")]
        public int Ema500Period { get; set; }

        [NinjaScriptProperty]
        [Range(1, 200)]
        [Display(Name="Ema 50 Period", Description="Period for the 50 EMA.", Order=5, GroupName="Parameters")]
        public int Ema50Period { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name="Ema 20 Period", Description="Period for the 20 EMA.", Order=6, GroupName="Parameters")]
        public int Ema20Period { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name="Atr Period", Description="Period for the ATR calculation.", Order=7, GroupName="Parameters")]
        public int AtrPeriod { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.1, 10)]
        [Display(Name="Killbox Range Multiplier", Description="ATR multiplier to define the choppy range.", Order=8, GroupName="Parameters")]
        public double KillboxRangeMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name="Recent Bars For Range", Description="Number of recent bars to analyze for range compression.", Order=9, GroupName="Parameters")]
        public int RecentBarsForRange { get; set; }

        [NinjaScriptProperty]
        [Range(0, 23)]
        [Display(Name="Trading Hours Start", Description="Start hour for trading session (0-23 CST).", Order=10, GroupName="Trading Hours")]
        public int TradingHoursStart { get; set; }

        [NinjaScriptProperty]
        [Range(0, 23)]
        [Display(Name="Trading Hours End", Description="End hour for trading session (0-23 CST).", Order=11, GroupName="Trading Hours")]
        public int TradingHoursEnd { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable Trading Hours Filter", Description="Enable filtering by trading hours.", Order=12, GroupName="Trading Hours")]
        public bool EnableTradingHoursFilter { get; set; }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SwingsOnSwings[] cacheSwingsOnSwings;
		public SwingsOnSwings SwingsOnSwings(int strength, int numSwingsToLabel, int ema200Period, int ema500Period, int ema50Period, int ema20Period, int atrPeriod, double killboxRangeMultiplier, int recentBarsForRange, int tradingHoursStart, int tradingHoursEnd, bool enableTradingHoursFilter)
		{
			return SwingsOnSwings(Input, strength, numSwingsToLabel, ema200Period, ema500Period, ema50Period, ema20Period, atrPeriod, killboxRangeMultiplier, recentBarsForRange, tradingHoursStart, tradingHoursEnd, enableTradingHoursFilter);
		}

		public SwingsOnSwings SwingsOnSwings(ISeries<double> input, int strength, int numSwingsToLabel, int ema200Period, int ema500Period, int ema50Period, int ema20Period, int atrPeriod, double killboxRangeMultiplier, int recentBarsForRange, int tradingHoursStart, int tradingHoursEnd, bool enableTradingHoursFilter)
		{
			if (cacheSwingsOnSwings != null)
				for (int idx = 0; idx < cacheSwingsOnSwings.Length; idx++)
					if (cacheSwingsOnSwings[idx] != null && cacheSwingsOnSwings[idx].Strength == strength && cacheSwingsOnSwings[idx].NumSwingsToLabel == numSwingsToLabel && cacheSwingsOnSwings[idx].Ema200Period == ema200Period && cacheSwingsOnSwings[idx].Ema500Period == ema500Period && cacheSwingsOnSwings[idx].Ema50Period == ema50Period && cacheSwingsOnSwings[idx].Ema20Period == ema20Period && cacheSwingsOnSwings[idx].AtrPeriod == atrPeriod && cacheSwingsOnSwings[idx].KillboxRangeMultiplier == killboxRangeMultiplier && cacheSwingsOnSwings[idx].RecentBarsForRange == recentBarsForRange && cacheSwingsOnSwings[idx].TradingHoursStart == tradingHoursStart && cacheSwingsOnSwings[idx].TradingHoursEnd == tradingHoursEnd && cacheSwingsOnSwings[idx].EnableTradingHoursFilter == enableTradingHoursFilter && cacheSwingsOnSwings[idx].EqualsInput(input))
						return cacheSwingsOnSwings[idx];
			return CacheIndicator<SwingsOnSwings>(new SwingsOnSwings(){ Strength = strength, NumSwingsToLabel = numSwingsToLabel, Ema200Period = ema200Period, Ema500Period = ema500Period, Ema50Period = ema50Period, Ema20Period = ema20Period, AtrPeriod = atrPeriod, KillboxRangeMultiplier = killboxRangeMultiplier, RecentBarsForRange = recentBarsForRange, TradingHoursStart = tradingHoursStart, TradingHoursEnd = tradingHoursEnd, EnableTradingHoursFilter = enableTradingHoursFilter }, input, ref cacheSwingsOnSwings);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SwingsOnSwings SwingsOnSwings(int strength, int numSwingsToLabel, int ema200Period, int ema500Period, int ema50Period, int ema20Period, int atrPeriod, double killboxRangeMultiplier, int recentBarsForRange, int tradingHoursStart, int tradingHoursEnd, bool enableTradingHoursFilter)
		{
			return indicator.SwingsOnSwings(Input, strength, numSwingsToLabel, ema200Period, ema500Period, ema50Period, ema20Period, atrPeriod, killboxRangeMultiplier, recentBarsForRange, tradingHoursStart, tradingHoursEnd, enableTradingHoursFilter);
		}

		public Indicators.SwingsOnSwings SwingsOnSwings(ISeries<double> input , int strength, int numSwingsToLabel, int ema200Period, int ema500Period, int ema50Period, int ema20Period, int atrPeriod, double killboxRangeMultiplier, int recentBarsForRange, int tradingHoursStart, int tradingHoursEnd, bool enableTradingHoursFilter)
		{
			return indicator.SwingsOnSwings(input, strength, numSwingsToLabel, ema200Period, ema500Period, ema50Period, ema20Period, atrPeriod, killboxRangeMultiplier, recentBarsForRange, tradingHoursStart, tradingHoursEnd, enableTradingHoursFilter);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SwingsOnSwings SwingsOnSwings(int strength, int numSwingsToLabel, int ema200Period, int ema500Period, int ema50Period, int ema20Period, int atrPeriod, double killboxRangeMultiplier, int recentBarsForRange, int tradingHoursStart, int tradingHoursEnd, bool enableTradingHoursFilter)
		{
			return indicator.SwingsOnSwings(Input, strength, numSwingsToLabel, ema200Period, ema500Period, ema50Period, ema20Period, atrPeriod, killboxRangeMultiplier, recentBarsForRange, tradingHoursStart, tradingHoursEnd, enableTradingHoursFilter);
		}

		public Indicators.SwingsOnSwings SwingsOnSwings(ISeries<double> input , int strength, int numSwingsToLabel, int ema200Period, int ema500Period, int ema50Period, int ema20Period, int atrPeriod, double killboxRangeMultiplier, int recentBarsForRange, int tradingHoursStart, int tradingHoursEnd, bool enableTradingHoursFilter)
		{
			return indicator.SwingsOnSwings(input, strength, numSwingsToLabel, ema200Period, ema500Period, ema50Period, ema20Period, atrPeriod, killboxRangeMultiplier, recentBarsForRange, tradingHoursStart, tradingHoursEnd, enableTradingHoursFilter);
		}
	}
}

#endregion
