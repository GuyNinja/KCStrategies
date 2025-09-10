//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// ATM_PatternController_Swings_v1_20241213.cs - ATM Controller with Swings248 Integration
// Uses actual swing data and tick-based measurements like Swings248
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
using System.Windows.Controls;
using System.Windows;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class ATM_PatternController_Swings_v1_20241213 : Indicator
    {
        #region Variables
        // Swing analysis (like Swings248)
        private List<SwingPoint> historicalHighs;
        private List<SwingPoint> historicalLows;
        private List<double> bullishMeasuredMoves;  // In ticks
        private List<double> bearishMeasuredMoves;  // In ticks
        private List<double> bullishPullbacks;      // In ticks
        private List<double> bearishPullbacks;      // In ticks
        private List<double> allRanges;            // In ticks
        
        private Swing swingHighsProvider;
        private Swing swingLowsProvider;
        private EMA ema200;
        private EMA ema500;
        private ATR atr;
        
        private double lastSwingHighValue;
        private double lastSwingLowValue;
        
        // State Machine (like Swings248)
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
        
        // ATM Strategy Types
        private enum ATMStrategyType
        {
            DataDriven,     // Uses actual swing data
            Conservative,   // Safe defaults
            Aggressive,     // High risk/reward
            Scalper,        // Quick exits
            Momentum,       // Trend following
            Killbox         // Range trading
        }
        
        private ATMStrategyType currentATMType = ATMStrategyType.DataDriven;
        private Dictionary<ATMStrategyType, ATMStrategy> atmStrategies;
        private ATMStrategy currentATM;
        
        // UI Controls
        private Grid controlPanel;
        private TextBlock lblCurrentTrend;
        private TextBlock lblATMStrategy;
        private TextBlock lblTargetTicks;
        private TextBlock lblStopTicks;
        private TextBlock lblTrailTicks;
        private TextBlock lblMeasuredMoves;
        private TextBlock lblPullbacks;
        private Button btnDataDriven;
        private Button btnConservative;
        private Button btnAggressive;
        private Button btnScalper;
        private bool autoModeEnabled = true;
        
        // Performance tracking
        private int atmFlashCounter = 0;
        #endregion

        private class SwingPoint
        {
            public double Price { get; set; }
            public int BarNumber { get; set; }
            public string Label { get; set; }
            public string Tag { get; set; }
        }

        private class ATMStrategy
        {
            public string Name { get; set; }
            public int TargetTicks { get; set; }
            public int StopTicks { get; set; }
            public int BreakevenTicks { get; set; }
            public int TrailTicks { get; set; }
            public string Description { get; set; }
            public double Confidence { get; set; }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"ATM Pattern Controller with Swings248 Integration - Uses actual swing data for ATM recommendations";
                Name = "ATM_PatternController_Swings_v1_20241213";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;
                
                // Swing Settings (like Swings248)
                Strength = 12;
                Ema200Period = 200;
                Ema500Period = 500;
                AtrPeriod = 14;
                KillboxRangeMultiplier = 2.0;
                
                // ATM Settings
                MinDataPoints = 10;
                ConservativeMultiplier = 0.8;
                AggressiveMultiplier = 1.5;
                ScalperMultiplier = 0.6;
                
                // Visual Settings
                ShowControlPanel = true;
                ShowATMLines = true;
                ShowSwingLabels = true;
                
                // Colors
                BullishColor = Brushes.LimeGreen;
                BearishColor = Brushes.Red;
                KillboxColor = Brushes.Orange;
                ATMTargetColor = Brushes.Lime;
                ATMStopColor = Brushes.Red;
                
                // Initialize ATM strategies
                InitializeATMStrategies();
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
                atmFlashCounter = 0;
            }
            else if (State == State.Historical)
            {
                if (ChartControl != null && ShowControlPanel)
                {
                    CreateControlPanel();
                }
            }
            else if (State == State.Terminated)
            {
                if (controlPanel != null && ChartControl != null)
                {
                    ChartControl.Dispatcher.InvokeAsync(() =>
                    {
                        if (controlPanel.Parent is Panel parent)
                        {
                            parent.Children.Remove(controlPanel);
                        }
                    });
                }
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Ema500Period) return;
            
            if (flashCounter > 0) flashCounter--;
            atmFlashCounter++;

            // Process swings (like Swings248)
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
            
            // Update trends (like Swings248)
            UpdateTrends();
            
            // Generate ATM recommendations based on swing data
            if (autoModeEnabled)
            {
                GenerateDataDrivenATM();
            }
            
            // Update visual elements
            UpdateVisualElements();
            UpdateControlPanel();
        }

        private void InitializeATMStrategies()
        {
            atmStrategies = new Dictionary<ATMStrategyType, ATMStrategy>
            {
                [ATMStrategyType.DataDriven] = new ATMStrategy
                {
                    Name = "Data-Driven",
                    Description = "Based on actual swing measurements",
                    Confidence = 0.0
                },
                [ATMStrategyType.Conservative] = new ATMStrategy
                {
                    Name = "Conservative",
                    TargetTicks = 20,
                    StopTicks = 15,
                    BreakevenTicks = 8,
                    TrailTicks = 6,
                    Description = "Safe default values",
                    Confidence = 0.6
                },
                [ATMStrategyType.Aggressive] = new ATMStrategy
                {
                    Name = "Aggressive",
                    TargetTicks = 40,
                    StopTicks = 20,
                    BreakevenTicks = 10,
                    TrailTicks = 8,
                    Description = "High risk/reward",
                    Confidence = 0.5
                },
                [ATMStrategyType.Scalper] = new ATMStrategy
                {
                    Name = "Scalper",
                    TargetTicks = 12,
                    StopTicks = 10,
                    BreakevenTicks = 5,
                    TrailTicks = 4,
                    Description = "Quick scalps",
                    Confidence = 0.7
                },
                [ATMStrategyType.Momentum] = new ATMStrategy
                {
                    Name = "Momentum",
                    TargetTicks = 30,
                    StopTicks = 15,
                    BreakevenTicks = 8,
                    TrailTicks = 10,
                    Description = "Trend following",
                    Confidence = 0.6
                },
                [ATMStrategyType.Killbox] = new ATMStrategy
                {
                    Name = "Killbox",
                    TargetTicks = 8,
                    StopTicks = 12,
                    BreakevenTicks = 4,
                    TrailTicks = 3,
                    Description = "Range trading",
                    Confidence = 0.5
                }
            };
            
            currentATM = atmStrategies[ATMStrategyType.Conservative];
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
            
            // Calculate range in ticks (like Swings248)
            if(lastLow != null)
            {
                allRanges.Add(Math.Abs(currentSwingHigh - lastLow.Price) / TickSize);
            }

            // Calculate measured moves in ticks (like Swings248)
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

            historicalHighs.Add(new SwingPoint 
            { 
                Price = currentSwingHigh, 
                BarNumber = CurrentBar - swingHighBarsAgo, 
                Label = label, 
                Tag = "SLabelH" + (CurrentBar - swingHighBarsAgo) 
            });
            
            // Limit data (like Swings248)
            while (historicalHighs.Count > 50) historicalHighs.RemoveAt(0);
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
            
            // Calculate range in ticks (like Swings248)
            if(lastHigh != null)
            {
                allRanges.Add(Math.Abs(lastHigh.Price - currentSwingLow) / TickSize);
            }

            // Calculate measured moves in ticks (like Swings248)
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

            historicalLows.Add(new SwingPoint 
            { 
                Price = currentSwingLow, 
                BarNumber = CurrentBar - swingLowBarsAgo, 
                Label = label, 
                Tag = "SLabelL" + (CurrentBar - swingLowBarsAgo) 
            });
            
            // Limit data (like Swings248)
            while (historicalLows.Count > 50) historicalLows.RemoveAt(0);
            while (bullishMeasuredMoves.Count > 200) bullishMeasuredMoves.RemoveAt(0);
            while (bearishMeasuredMoves.Count > 200) bearishMeasuredMoves.RemoveAt(0);
            while (bullishPullbacks.Count > 200) bullishPullbacks.RemoveAt(0);
            while (bearishPullbacks.Count > 200) bearishPullbacks.RemoveAt(0);
            while (allRanges.Count > 200) allRanges.RemoveAt(0);
        }

        private void UpdateTrends()
        {
            // Macro trend (like Swings248)
            if (Close[0] > ema200[0] && Close[0] > ema500[0])
                macroTrend = MacroTrendState.Bullish;
            else if (Close[0] < ema200[0] && Close[0] < ema500[0])
                macroTrend = MacroTrendState.Bearish;
            else
                macroTrend = MacroTrendState.Neutral;

            // Check for killbox (like Swings248)
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
                    if (currentTrend != CurrentTrendState.TrendingBullish && currentTrend != CurrentTrendState.StronglyBullish) 
                        sequenceCount = 1; 
                    else 
                        sequenceCount++;
                    currentTrend = (sequenceCount > 1) ? CurrentTrendState.StronglyBullish : CurrentTrendState.TrendingBullish;
                }
                else if (isTrendingDown)
                {
                    if (currentTrend != CurrentTrendState.TrendingBearish && currentTrend != CurrentTrendState.StronglyBearish) 
                        sequenceCount = 1; 
                    else 
                        sequenceCount++;
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
                
                // Micro trend detection (like Swings248)
                bool isBullishContext = currentTrend == CurrentTrendState.WeakBullish || 
                                       currentTrend == CurrentTrendState.TrendingBullish || 
                                       currentTrend == CurrentTrendState.StronglyBullish;
                bool isBearishContext = currentTrend == CurrentTrendState.WeakBearish || 
                                       currentTrend == CurrentTrendState.TrendingBearish || 
                                       currentTrend == CurrentTrendState.StronglyBearish;

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
            
            return (highRange < killboxThreshold && lowRange < killboxThreshold);
        }

        private void GenerateDataDrivenATM()
        {
            var allMoves = bullishMeasuredMoves.Concat(bearishMeasuredMoves).ToList();
            var allPullbacks = bullishPullbacks.Concat(bearishPullbacks).ToList();
            
            // Need minimum data points for reliable calculations
            if (allMoves.Count < MinDataPoints || allRanges.Count < MinDataPoints)
            {
                // Fall back to pattern-based selection
                SelectPatternBasedATM();
                return;
            }
            
            // Calculate medians (like Swings248 ATM box)
            double medianMove = CalculateMedian(allMoves);
            double medianRange = CalculateMedian(allRanges);
            double medianPullback = allPullbacks.Count > 0 ? CalculateMedian(allPullbacks) : medianRange * 0.5;
            
            // Create data-driven ATM strategy
            var dataDrivenATM = atmStrategies[ATMStrategyType.DataDriven];
            
            // Adjust based on current trend state
            switch (currentTrend)
            {
                case CurrentTrendState.Killbox:
                    // Use smaller targets in killbox
                    dataDrivenATM.TargetTicks = (int)(medianRange * 0.6);
                    dataDrivenATM.StopTicks = (int)(medianRange * 0.8);
                    dataDrivenATM.BreakevenTicks = (int)(medianRange * 0.3);
                    dataDrivenATM.TrailTicks = (int)(medianPullback * 0.4);
                    dataDrivenATM.Confidence = 0.7;
                    break;
                    
                case CurrentTrendState.TrendingBullish:
                case CurrentTrendState.TrendingBearish:
                case CurrentTrendState.StronglyBullish:
                case CurrentTrendState.StronglyBearish:
                    // Use measured moves in trending markets
                    dataDrivenATM.TargetTicks = (int)(medianMove * 0.8);
                    dataDrivenATM.StopTicks = (int)(medianRange * 0.7);
                    dataDrivenATM.BreakevenTicks = (int)(medianRange * 0.4);
                    dataDrivenATM.TrailTicks = (int)(medianPullback * 0.6);
                    dataDrivenATM.Confidence = 0.9;
                    break;
                    
                default:
                    // Conservative approach for unclear trends
                    dataDrivenATM.TargetTicks = (int)(medianMove * 0.6);
                    dataDrivenATM.StopTicks = (int)(medianRange * 0.8);
                    dataDrivenATM.BreakevenTicks = (int)(medianRange * 0.4);
                    dataDrivenATM.TrailTicks = (int)(medianPullback * 0.5);
                    dataDrivenATM.Confidence = 0.6;
                    break;
            }
            
            currentATM = dataDrivenATM;
            currentATMType = ATMStrategyType.DataDriven;
        }

        private void SelectPatternBasedATM()
        {
            ATMStrategyType newType = ATMStrategyType.Conservative;
            
            switch (currentTrend)
            {
                case CurrentTrendState.Killbox:
                    newType = ATMStrategyType.Killbox;
                    break;
                case CurrentTrendState.TrendingBullish:
                case CurrentTrendState.TrendingBearish:
                case CurrentTrendState.StronglyBullish:
                case CurrentTrendState.StronglyBearish:
                    newType = ATMStrategyType.Momentum;
                    break;
                case CurrentTrendState.WeakBullish:
                case CurrentTrendState.WeakBearish:
                    newType = ATMStrategyType.Scalper;
                    break;
                default:
                    newType = ATMStrategyType.Conservative;
                    break;
            }
            
            currentATM = atmStrategies[newType];
            currentATMType = newType;
        }

        private void UpdateVisualElements()
        {
            if (!ShowATMLines) return;
            
            // Draw ATM lines
            double targetPrice = Close[0] + (currentATM.TargetTicks * TickSize);
            double stopPrice = Close[0] - (currentATM.StopTicks * TickSize);
            
            Draw.Line(this, "ATMTarget", false, 0, targetPrice, 10, targetPrice,
                ATMTargetColor, DashStyleHelper.Solid, 2);
                
            Draw.Line(this, "ATMStop", false, 0, stopPrice, 10, stopPrice,
                ATMStopColor, DashStyleHelper.Solid, 2);
            
            // Draw swing labels if enabled
            if (ShowSwingLabels)
            {
                foreach (var high in historicalHighs.TakeLast(10))
                {
                    string label = high.Label;
                    if(sequenceCount > 0 && (label == "HH" || label == "LH")) label += sequenceCount;
                    Brush color = high.Label.Contains("HH") ? BullishColor : (high.Label.Contains("LH") ? BearishColor : Brushes.DimGray);
                    Draw.Text(this, high.Tag, label, CurrentBar - high.BarNumber, high.Price + (TickSize * 5), color);
                }
                
                foreach (var low in historicalLows.TakeLast(10))
                {
                    string label = low.Label;
                    if(sequenceCount > 0 && (label == "LL" || label == "HL")) label += sequenceCount;
                    Brush color = low.Label.Contains("LL") ? BearishColor : (low.Label.Contains("HL") ? BullishColor : Brushes.DimGray);
                    Draw.Text(this, low.Tag, label, CurrentBar - low.BarNumber, low.Price - (TickSize * 5), color);
                }
            }
            
            // Draw killbox if detected
            if (currentTrend == CurrentTrendState.Killbox)
            {
                DrawKillbox();
            }
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

            Draw.Rectangle(this, "KillboxRect", false, CurrentBar - endBar, bottom, CurrentBar - startBar, top, 
                Brushes.Transparent, KillboxColor, 10);
        }

        private void CreateControlPanel()
        {
            ChartControl.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    controlPanel = new Grid
                    {
                        Background = new SolidColorBrush(Color.FromArgb(220, 20, 20, 20)),
                        Margin = new Thickness(10),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Width = 280
                    };

                    // Define grid structure
                    for (int i = 0; i < 10; i++)
                        controlPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    controlPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    // Create title
                    var title = new TextBlock
                    {
                        Text = "ATM Swings Controller",
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        FontSize = 12,
                        Margin = new Thickness(5),
                        TextAlignment = TextAlignment.Center
                    };

                    // Create info labels
                    lblCurrentTrend = CreateInfoLabel("Trend: Unknown");
                    lblATMStrategy = CreateInfoLabel("ATM: Conservative");
                    lblTargetTicks = CreateInfoLabel("Target: -- ticks");
                    lblStopTicks = CreateInfoLabel("Stop: -- ticks");
                    lblTrailTicks = CreateInfoLabel("Trail: -- ticks");
                    lblMeasuredMoves = CreateInfoLabel("Moves: --");
                    lblPullbacks = CreateInfoLabel("Pullbacks: --");

                    // Create control buttons
                    btnDataDriven = CreateControlButton("DATA", true, OnDataDrivenClick);
                    btnConservative = CreateControlButton("SAFE", false, OnConservativeClick);
                    btnAggressive = CreateControlButton("AGGR", false, OnAggressiveClick);
                    btnScalper = CreateControlButton("SCALP", false, OnScalperClick);

                    // Add to grid
                    Grid.SetRow(title, 0); Grid.SetColumn(title, 0);
                    Grid.SetRow(lblCurrentTrend, 1); Grid.SetColumn(lblCurrentTrend, 0);
                    Grid.SetRow(lblATMStrategy, 2); Grid.SetColumn(lblATMStrategy, 0);
                    Grid.SetRow(lblTargetTicks, 3); Grid.SetColumn(lblTargetTicks, 0);
                    Grid.SetRow(lblStopTicks, 4); Grid.SetColumn(lblStopTicks, 0);
                    Grid.SetRow(lblTrailTicks, 5); Grid.SetColumn(lblTrailTicks, 0);
                    Grid.SetRow(lblMeasuredMoves, 6); Grid.SetColumn(lblMeasuredMoves, 0);
                    Grid.SetRow(lblPullbacks, 7); Grid.SetColumn(lblPullbacks, 0);

                    var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };
                    buttonPanel.Children.Add(btnDataDriven);
                    buttonPanel.Children.Add(btnConservative);
                    buttonPanel.Children.Add(btnAggressive);
                    buttonPanel.Children.Add(btnScalper);

                    Grid.SetRow(buttonPanel, 8); Grid.SetColumn(buttonPanel, 0);

                    controlPanel.Children.Add(title);
                    controlPanel.Children.Add(lblCurrentTrend);
                    controlPanel.Children.Add(lblATMStrategy);
                    controlPanel.Children.Add(lblTargetTicks);
                    controlPanel.Children.Add(lblStopTicks);
                    controlPanel.Children.Add(lblTrailTicks);
                    controlPanel.Children.Add(lblMeasuredMoves);
                    controlPanel.Children.Add(lblPullbacks);
                    controlPanel.Children.Add(buttonPanel);

                    // Add to chart
                    if (ChartControl.TabControl != null)
                    {
                        var chartGrid = ChartControl.TabControl.Parent as Panel;
                        if (chartGrid != null)
                        {
                            chartGrid.Children.Add(controlPanel);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Print($"Error creating control panel: {ex.Message}");
                }
            });
        }

        private TextBlock CreateInfoLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = Brushes.LightGray,
                FontSize = 10,
                Margin = new Thickness(5, 2, 5, 2)
            };
        }

        private Button CreateControlButton(string text, bool isEnabled, RoutedEventHandler clickHandler)
        {
            var button = new Button
            {
                Content = text,
                Width = 50,
                Height = 25,
                Margin = new Thickness(2),
                Background = isEnabled ? Brushes.LightGreen : Brushes.DarkGray,
                Foreground = Brushes.Black,
                FontSize = 8,
                FontWeight = FontWeights.Bold
            };
            button.Click += clickHandler;
            return button;
        }

        private void UpdateControlPanel()
        {
            if (controlPanel == null) return;

            ChartControl.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    lblCurrentTrend.Text = $"Trend: {GetCurrentTrendText()}";
                    lblATMStrategy.Text = $"ATM: {currentATM.Name} ({currentATM.Confidence:P0})";
                    lblTargetTicks.Text = $"Target: {currentATM.TargetTicks} ticks";
                    lblStopTicks.Text = $"Stop: {currentATM.StopTicks} ticks";
                    lblTrailTicks.Text = $"Trail: {currentATM.TrailTicks} ticks";
                    
                    var allMoves = bullishMeasuredMoves.Concat(bearishMeasuredMoves).ToList();
                    var allPullbacks = bullishPullbacks.Concat(bearishPullbacks).ToList();
                    
                    string movesText = allMoves.Count > 0 ? $"Moves: {CalculateMedian(allMoves):F0} ({allMoves.Count})" : "Moves: N/A";
                    string pullbacksText = allPullbacks.Count > 0 ? $"Pullbacks: {CalculateMedian(allPullbacks):F0} ({allPullbacks.Count})" : "Pullbacks: N/A";
                    
                    lblMeasuredMoves.Text = movesText;
                    lblPullbacks.Text = pullbacksText;

                    // Update button states
                    btnDataDriven.Background = (autoModeEnabled && currentATMType == ATMStrategyType.DataDriven) ? Brushes.LightGreen : Brushes.DarkGray;
                    btnConservative.Background = (!autoModeEnabled && currentATMType == ATMStrategyType.Conservative) ? Brushes.Yellow : Brushes.DarkGray;
                    btnAggressive.Background = (!autoModeEnabled && currentATMType == ATMStrategyType.Aggressive) ? Brushes.Yellow : Brushes.DarkGray;
                    btnScalper.Background = (!autoModeEnabled && currentATMType == ATMStrategyType.Scalper) ? Brushes.Yellow : Brushes.DarkGray;
                }
                catch (Exception ex)
                {
                    Print($"Error updating control panel: {ex.Message}");
                }
            });
        }

        // Event handlers
        private void OnDataDrivenClick(object sender, RoutedEventArgs e)
        {
            autoModeEnabled = true;
            Print("ATM Controller: DATA-DRIVEN mode enabled");
        }

        private void OnConservativeClick(object sender, RoutedEventArgs e)
        {
            autoModeEnabled = false;
            currentATMType = ATMStrategyType.Conservative;
            currentATM = atmStrategies[currentATMType];
            Print("ATM Controller: CONSERVATIVE mode forced");
        }

        private void OnAggressiveClick(object sender, RoutedEventArgs e)
        {
            autoModeEnabled = false;
            currentATMType = ATMStrategyType.Aggressive;
            currentATM = atmStrategies[currentATMType];
            Print("ATM Controller: AGGRESSIVE mode forced");
        }

        private void OnScalperClick(object sender, RoutedEventArgs e)
        {
            autoModeEnabled = false;
            currentATMType = ATMStrategyType.Scalper;
            currentATM = atmStrategies[currentATMType];
            Print("ATM Controller: SCALPER mode forced");
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
                case CurrentTrendState.Killbox: return "Killbox";
                default: return "Sideways";
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Strength", Description="Swing strength parameter", Order=1, GroupName="Swing Settings")]
        public int Strength { get; set; }

        [NinjaScriptProperty]
        [Range(1, 1000)]
        [Display(Name="EMA 200 Period", Description="Period for EMA 200", Order=2, GroupName="Swing Settings")]
        public int Ema200Period { get; set; }

        [NinjaScriptProperty]
        [Range(1, 2000)]
        [Display(Name="EMA 500 Period", Description="Period for EMA 500", Order=3, GroupName="Swing Settings")]
        public int Ema500Period { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name="ATR Period", Description="Period for ATR calculation", Order=4, GroupName="Swing Settings")]
        public int AtrPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, 10)]
        [Display(Name="Killbox Range Multiplier", Description="ATR multiplier for killbox detection", Order=5, GroupName="Swing Settings")]
        public double KillboxRangeMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(5, 50)]
        [Display(Name="Min Data Points", Description="Minimum swing data points needed", Order=6, GroupName="ATM Settings")]
        public int MinDataPoints { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, 2.0)]
        [Display(Name="Conservative Multiplier", Description="Multiplier for conservative strategy", Order=7, GroupName="ATM Settings")]
        public double ConservativeMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(1.0, 3.0)]
        [Display(Name="Aggressive Multiplier", Description="Multiplier for aggressive strategy", Order=8, GroupName="ATM Settings")]
        public double AggressiveMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, 1.0)]
        [Display(Name="Scalper Multiplier", Description="Multiplier for scalper strategy", Order=9, GroupName="ATM Settings")]
        public double ScalperMultiplier { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Control Panel", Description="Display control panel", Order=10, GroupName="Visual Settings")]
        public bool ShowControlPanel { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show ATM Lines", Description="Display ATM target/stop lines", Order=11, GroupName="Visual Settings")]
        public bool ShowATMLines { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Swing Labels", Description="Display swing labels", Order=12, GroupName="Visual Settings")]
        public bool ShowSwingLabels { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Bullish Color", Description="Color for bullish swings", Order=13, GroupName="Colors")]
        public Brush BullishColor { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Bearish Color", Description="Color for bearish swings", Order=14, GroupName="Colors")]
        public Brush BearishColor { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Killbox Color", Description="Color for killbox detection", Order=15, GroupName="Colors")]
        public Brush KillboxColor { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="ATM Target Color", Description="Color for ATM target lines", Order=16, GroupName="Colors")]
        public Brush ATMTargetColor { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="ATM Stop Color", Description="Color for ATM stop lines", Order=17, GroupName="Colors")]
        public Brush ATMStopColor { get; set; }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ATM_PatternController_Swings_v1_20241213[] cacheATM_PatternController_Swings_v1_20241213;
		public ATM_PatternController_Swings_v1_20241213 ATM_PatternController_Swings_v1_20241213(int strength, int ema200Period, int ema500Period, int atrPeriod, double killboxRangeMultiplier, int minDataPoints, double conservativeMultiplier, double aggressiveMultiplier, double scalperMultiplier, bool showControlPanel, bool showATMLines, bool showSwingLabels, Brush bullishColor, Brush bearishColor, Brush killboxColor, Brush aTMTargetColor, Brush aTMStopColor)
		{
			return ATM_PatternController_Swings_v1_20241213(Input, strength, ema200Period, ema500Period, atrPeriod, killboxRangeMultiplier, minDataPoints, conservativeMultiplier, aggressiveMultiplier, scalperMultiplier, showControlPanel, showATMLines, showSwingLabels, bullishColor, bearishColor, killboxColor, aTMTargetColor, aTMStopColor);
		}

		public ATM_PatternController_Swings_v1_20241213 ATM_PatternController_Swings_v1_20241213(ISeries<double> input, int strength, int ema200Period, int ema500Period, int atrPeriod, double killboxRangeMultiplier, int minDataPoints, double conservativeMultiplier, double aggressiveMultiplier, double scalperMultiplier, bool showControlPanel, bool showATMLines, bool showSwingLabels, Brush bullishColor, Brush bearishColor, Brush killboxColor, Brush aTMTargetColor, Brush aTMStopColor)
		{
			if (cacheATM_PatternController_Swings_v1_20241213 != null)
				for (int idx = 0; idx < cacheATM_PatternController_Swings_v1_20241213.Length; idx++)
					if (cacheATM_PatternController_Swings_v1_20241213[idx] != null && cacheATM_PatternController_Swings_v1_20241213[idx].Strength == strength && cacheATM_PatternController_Swings_v1_20241213[idx].Ema200Period == ema200Period && cacheATM_PatternController_Swings_v1_20241213[idx].Ema500Period == ema500Period && cacheATM_PatternController_Swings_v1_20241213[idx].AtrPeriod == atrPeriod && cacheATM_PatternController_Swings_v1_20241213[idx].KillboxRangeMultiplier == killboxRangeMultiplier && cacheATM_PatternController_Swings_v1_20241213[idx].MinDataPoints == minDataPoints && cacheATM_PatternController_Swings_v1_20241213[idx].ConservativeMultiplier == conservativeMultiplier && cacheATM_PatternController_Swings_v1_20241213[idx].AggressiveMultiplier == aggressiveMultiplier && cacheATM_PatternController_Swings_v1_20241213[idx].ScalperMultiplier == scalperMultiplier && cacheATM_PatternController_Swings_v1_20241213[idx].ShowControlPanel == showControlPanel && cacheATM_PatternController_Swings_v1_20241213[idx].ShowATMLines == showATMLines && cacheATM_PatternController_Swings_v1_20241213[idx].ShowSwingLabels == showSwingLabels && cacheATM_PatternController_Swings_v1_20241213[idx].BullishColor == bullishColor && cacheATM_PatternController_Swings_v1_20241213[idx].BearishColor == bearishColor && cacheATM_PatternController_Swings_v1_20241213[idx].KillboxColor == killboxColor && cacheATM_PatternController_Swings_v1_20241213[idx].ATMTargetColor == aTMTargetColor && cacheATM_PatternController_Swings_v1_20241213[idx].ATMStopColor == aTMStopColor && cacheATM_PatternController_Swings_v1_20241213[idx].EqualsInput(input))
						return cacheATM_PatternController_Swings_v1_20241213[idx];
			return CacheIndicator<ATM_PatternController_Swings_v1_20241213>(new ATM_PatternController_Swings_v1_20241213(){ Strength = strength, Ema200Period = ema200Period, Ema500Period = ema500Period, AtrPeriod = atrPeriod, KillboxRangeMultiplier = killboxRangeMultiplier, MinDataPoints = minDataPoints, ConservativeMultiplier = conservativeMultiplier, AggressiveMultiplier = aggressiveMultiplier, ScalperMultiplier = scalperMultiplier, ShowControlPanel = showControlPanel, ShowATMLines = showATMLines, ShowSwingLabels = showSwingLabels, BullishColor = bullishColor, BearishColor = bearishColor, KillboxColor = killboxColor, ATMTargetColor = aTMTargetColor, ATMStopColor = aTMStopColor }, input, ref cacheATM_PatternController_Swings_v1_20241213);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ATM_PatternController_Swings_v1_20241213 ATM_PatternController_Swings_v1_20241213(int strength, int ema200Period, int ema500Period, int atrPeriod, double killboxRangeMultiplier, int minDataPoints, double conservativeMultiplier, double aggressiveMultiplier, double scalperMultiplier, bool showControlPanel, bool showATMLines, bool showSwingLabels, Brush bullishColor, Brush bearishColor, Brush killboxColor, Brush aTMTargetColor, Brush aTMStopColor)
		{
			return indicator.ATM_PatternController_Swings_v1_20241213(Input, strength, ema200Period, ema500Period, atrPeriod, killboxRangeMultiplier, minDataPoints, conservativeMultiplier, aggressiveMultiplier, scalperMultiplier, showControlPanel, showATMLines, showSwingLabels, bullishColor, bearishColor, killboxColor, aTMTargetColor, aTMStopColor);
		}

		public Indicators.ATM_PatternController_Swings_v1_20241213 ATM_PatternController_Swings_v1_20241213(ISeries<double> input , int strength, int ema200Period, int ema500Period, int atrPeriod, double killboxRangeMultiplier, int minDataPoints, double conservativeMultiplier, double aggressiveMultiplier, double scalperMultiplier, bool showControlPanel, bool showATMLines, bool showSwingLabels, Brush bullishColor, Brush bearishColor, Brush killboxColor, Brush aTMTargetColor, Brush aTMStopColor)
		{
			return indicator.ATM_PatternController_Swings_v1_20241213(input, strength, ema200Period, ema500Period, atrPeriod, killboxRangeMultiplier, minDataPoints, conservativeMultiplier, aggressiveMultiplier, scalperMultiplier, showControlPanel, showATMLines, showSwingLabels, bullishColor, bearishColor, killboxColor, aTMTargetColor, aTMStopColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ATM_PatternController_Swings_v1_20241213 ATM_PatternController_Swings_v1_20241213(int strength, int ema200Period, int ema500Period, int atrPeriod, double killboxRangeMultiplier, int minDataPoints, double conservativeMultiplier, double aggressiveMultiplier, double scalperMultiplier, bool showControlPanel, bool showATMLines, bool showSwingLabels, Brush bullishColor, Brush bearishColor, Brush killboxColor, Brush aTMTargetColor, Brush aTMStopColor)
		{
			return indicator.ATM_PatternController_Swings_v1_20241213(Input, strength, ema200Period, ema500Period, atrPeriod, killboxRangeMultiplier, minDataPoints, conservativeMultiplier, aggressiveMultiplier, scalperMultiplier, showControlPanel, showATMLines, showSwingLabels, bullishColor, bearishColor, killboxColor, aTMTargetColor, aTMStopColor);
		}

		public Indicators.ATM_PatternController_Swings_v1_20241213 ATM_PatternController_Swings_v1_20241213(ISeries<double> input , int strength, int ema200Period, int ema500Period, int atrPeriod, double killboxRangeMultiplier, int minDataPoints, double conservativeMultiplier, double aggressiveMultiplier, double scalperMultiplier, bool showControlPanel, bool showATMLines, bool showSwingLabels, Brush bullishColor, Brush bearishColor, Brush killboxColor, Brush aTMTargetColor, Brush aTMStopColor)
		{
			return indicator.ATM_PatternController_Swings_v1_20241213(input, strength, ema200Period, ema500Period, atrPeriod, killboxRangeMultiplier, minDataPoints, conservativeMultiplier, aggressiveMultiplier, scalperMultiplier, showControlPanel, showATMLines, showSwingLabels, bullishColor, bearishColor, killboxColor, aTMTargetColor, aTMStopColor);
		}
	}
}

#endregion
