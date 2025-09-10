//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// ATM_PatternController_v1_20241213.cs - Dynamic ATM Strategy Controller Based on Chart Patterns
// Automatically adjusts ATM strategies based on detected market patterns and conditions
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
    public class ATM_PatternController_v1_20241213 : Indicator
    {
        #region Variables
        // Pattern Recognition
        private enum MarketPattern
        {
            TrendingBullish,
            TrendingBearish,
            RangingCompressed,
            RangingVolatile,
            BreakoutPending,
            VolatileChoppy,
            KillBox,
            Unknown
        }
        
        private enum ATMStrategyType
        {
            Scalper,        // Quick exits, tight stops
            Momentum,       // Trend following
            Breakout,       // Wide targets for breakouts
            Range,          // Range bound trading
            Conservative,   // Risk management focus
            Aggressive      // High risk/reward
        }
        
        // Current market state
        private MarketPattern currentPattern = MarketPattern.Unknown;
        private ATMStrategyType recommendedStrategy = ATMStrategyType.Conservative;
        private double patternConfidence = 0.0;
        
        // ATM Strategies
        private Dictionary<ATMStrategyType, ATMConfig> atmStrategies;
        private ATMConfig currentATM;
        private ATMConfig previousATM;
        
        // Pattern detection variables
        private SMA sma20, sma50, sma200;
        private double atrValue;
        private int trendingBars = 0;
        private int rangingBars = 0;
        private double recentHigh = 0;
        private double recentLow = double.MaxValue;
        private List<double> volatilityBuffer = new List<double>();
        private List<double> volumeBuffer = new List<double>();
        
        // UI Controls
        private Grid controlPanel;
        private TextBlock lblCurrentPattern;
        private TextBlock lblRecommendedATM;
        private TextBlock lblConfidence;
        private TextBlock lblCurrentTarget;
        private TextBlock lblCurrentStop;
        private Button btnForceScalper;
        private Button btnForceMomentum;
        private Button btnForceBreakout;
        private Button btnAutoMode;
        private bool autoModeEnabled = true;
        
        // Performance tracking
        private int patternChangeCount = 0;
        private DateTime lastPatternChange = DateTime.MinValue;
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"ATM Pattern Controller - Dynamic ATM Strategy Selection Based on Chart Patterns";
                Name = "ATM_PatternController_v1_20241213";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;
                
                // Pattern Detection Settings
                SensitivityLevel = 70;
                ATRPeriod = 14;
                VolatilityLookback = 20;
                TrendConfirmationBars = 5;
                
                // ATM Base Settings
                BaseRiskTicks = 20;
                BaseTargetMultiplier = 2.0;
                BreakevenMultiplier = 0.5;
                TrailMultiplier = 0.3;
                
                // Visual Settings
                ShowControlPanel = true;
                ShowPatternLabels = true;
                ShowATMLines = true;
                
                // Colors
                TrendingColor = Brushes.Green;
                RangingColor = Brushes.Orange;
                VolatileColor = Brushes.Red;
                BreakoutColor = Brushes.Purple;
                ATMTargetColor = Brushes.Lime;
                ATMStopColor = Brushes.Red;
                
                // Initialize strategies
                InitializeATMStrategies();
            }
            else if (State == State.DataLoaded)
            {
                sma20 = SMA(Close, 20);
                sma50 = SMA(Close, 50);
                sma200 = SMA(Close, 200);
                
                // Initialize buffers
                volatilityBuffer = new List<double>();
                volumeBuffer = new List<double>();
                
                currentATM = atmStrategies[ATMStrategyType.Conservative];
                previousATM = currentATM;
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
            if (CurrentBar < Math.Max(ATRPeriod, VolatilityLookback)) return;

            // Update market data
            UpdateMarketData();
            
            // Detect current pattern
            DetectMarketPattern();
            
            // Select appropriate ATM strategy
            if (autoModeEnabled)
            {
                SelectOptimalATMStrategy();
            }
            
            // Update visual elements
            UpdateVisualElements();
            
            // Update control panel
            UpdateControlPanel();
        }

        private void InitializeATMStrategies()
        {
            atmStrategies = new Dictionary<ATMStrategyType, ATMConfig>
            {
                [ATMStrategyType.Scalper] = new ATMConfig
                {
                    Name = "Scalper",
                    TargetMultiplier = 1.0,
                    StopMultiplier = 0.6,
                    BreakevenMultiplier = 0.3,
                    TrailMultiplier = 0.2,
                    Description = "Quick scalps, tight stops"
                },
                [ATMStrategyType.Momentum] = new ATMConfig
                {
                    Name = "Momentum",
                    TargetMultiplier = 2.5,
                    StopMultiplier = 1.0,
                    BreakevenMultiplier = 0.5,
                    TrailMultiplier = 0.4,
                    Description = "Trend following strategy"
                },
                [ATMStrategyType.Breakout] = new ATMConfig
                {
                    Name = "Breakout",
                    TargetMultiplier = 4.0,
                    StopMultiplier = 1.2,
                    BreakevenMultiplier = 0.6,
                    TrailMultiplier = 0.5,
                    Description = "Wide targets for breakouts"
                },
                [ATMStrategyType.Range] = new ATMConfig
                {
                    Name = "Range",
                    TargetMultiplier = 1.5,
                    StopMultiplier = 0.8,
                    BreakevenMultiplier = 0.4,
                    TrailMultiplier = 0.3,
                    Description = "Range bound trading"
                },
                [ATMStrategyType.Conservative] = new ATMConfig
                {
                    Name = "Conservative",
                    TargetMultiplier = 1.8,
                    StopMultiplier = 0.9,
                    BreakevenMultiplier = 0.5,
                    TrailMultiplier = 0.3,
                    Description = "Balanced risk management"
                },
                [ATMStrategyType.Aggressive] = new ATMConfig
                {
                    Name = "Aggressive",
                    TargetMultiplier = 3.5,
                    StopMultiplier = 1.5,
                    BreakevenMultiplier = 0.7,
                    TrailMultiplier = 0.6,
                    Description = "High risk/reward"
                }
            };
        }

        private void UpdateMarketData()
        {
            // Calculate ATR
            atrValue = ATR(ATRPeriod)[0];
            
            // Update volatility buffer
            double currentVolatility = (High[0] - Low[0]) / Close[0] * 100;
            volatilityBuffer.Add(currentVolatility);
            if (volatilityBuffer.Count > VolatilityLookback)
                volatilityBuffer.RemoveAt(0);
            
            // Update volume buffer
            volumeBuffer.Add(Volume[0]);
            if (volumeBuffer.Count > VolatilityLookback)
                volumeBuffer.RemoveAt(0);
            
            // Track recent highs/lows
            if (CurrentBar >= VolatilityLookback)
            {
                recentHigh = MAX(High, VolatilityLookback)[0];
                recentLow = MIN(Low, VolatilityLookback)[0];
            }
        }

        private void DetectMarketPattern()
        {
            if (CurrentBar < 200) return;

            double avgVolatility = volatilityBuffer.Average();
            double volStdDev = CalculateStandardDeviation(volatilityBuffer);
            double avgVolume = volumeBuffer.Average();
            double currentVolume = Volume[0];
            
            // Trend detection
            bool bullishTrend = Close[0] > sma20[0] && sma20[0] > sma50[0] && sma50[0] > sma200[0];
            bool bearishTrend = Close[0] < sma20[0] && sma20[0] < sma50[0] && sma50[0] < sma200[0];
            
            // Range detection
            double rangeSize = (recentHigh - recentLow) / recentLow * 100;
            bool isCompressed = rangeSize < 2.0; // Less than 2% range
            bool isVolatile = avgVolatility > (volStdDev * 1.5);
            
            // Volume analysis
            bool highVolume = currentVolume > (avgVolume * 1.5);
            
            // Pattern classification
            MarketPattern newPattern = MarketPattern.Unknown;
            double confidence = 0.5;
            
            if (bullishTrend && avgVolatility < volStdDev)
            {
                newPattern = MarketPattern.TrendingBullish;
                confidence = 0.8;
                trendingBars++;
                rangingBars = 0;
            }
            else if (bearishTrend && avgVolatility < volStdDev)
            {
                newPattern = MarketPattern.TrendingBearish;
                confidence = 0.8;
                trendingBars++;
                rangingBars = 0;
            }
            else if (isCompressed && !isVolatile)
            {
                newPattern = MarketPattern.RangingCompressed;
                confidence = 0.7;
                rangingBars++;
                trendingBars = 0;
            }
            else if (isCompressed && highVolume)
            {
                newPattern = MarketPattern.BreakoutPending;
                confidence = 0.75;
            }
            else if (isVolatile && !isCompressed)
            {
                newPattern = MarketPattern.VolatileChoppy;
                confidence = 0.6;
            }
            else if (!bullishTrend && !bearishTrend && !isCompressed)
            {
                newPattern = MarketPattern.RangingVolatile;
                confidence = 0.6;
                rangingBars++;
                trendingBars = 0;
            }
            
            // Confirm pattern change with minimum bars
            if (newPattern != currentPattern)
            {
                if (trendingBars >= TrendConfirmationBars || rangingBars >= TrendConfirmationBars)
                {
                    currentPattern = newPattern;
                    patternConfidence = confidence;
                    patternChangeCount++;
                    lastPatternChange = Time[0];
                    
                    // Log pattern change
                    Print($"Pattern Change: {currentPattern} (Confidence: {confidence:F2})");
                }
            }
            else
            {
                patternConfidence = Math.Min(1.0, patternConfidence + 0.05); // Increase confidence over time
            }
        }

        private void SelectOptimalATMStrategy()
        {
            ATMStrategyType newStrategy = ATMStrategyType.Conservative;
            
            switch (currentPattern)
            {
                case MarketPattern.TrendingBullish:
                case MarketPattern.TrendingBearish:
                    newStrategy = ATMStrategyType.Momentum;
                    break;
                    
                case MarketPattern.RangingCompressed:
                    newStrategy = ATMStrategyType.Range;
                    break;
                    
                case MarketPattern.RangingVolatile:
                    newStrategy = ATMStrategyType.Scalper;
                    break;
                    
                case MarketPattern.BreakoutPending:
                    newStrategy = ATMStrategyType.Breakout;
                    break;
                    
                case MarketPattern.VolatileChoppy:
                    newStrategy = ATMStrategyType.Conservative;
                    break;
                    
                default:
                    newStrategy = ATMStrategyType.Conservative;
                    break;
            }
            
            // Apply confidence filter
            if (patternConfidence < 0.6)
            {
                newStrategy = ATMStrategyType.Conservative;
            }
            
            // Update strategy if changed
            if (newStrategy != recommendedStrategy)
            {
                recommendedStrategy = newStrategy;
                previousATM = currentATM;
                currentATM = atmStrategies[recommendedStrategy];
                
                Print($"ATM Strategy Changed: {currentATM.Name} - {currentATM.Description}");
            }
        }

        private void UpdateVisualElements()
        {
            if (!ShowATMLines) return;
            
            // Calculate current ATM levels
            double baseRisk = BaseRiskTicks * TickSize;
            double targetDistance = baseRisk * currentATM.TargetMultiplier;
            double stopDistance = baseRisk * currentATM.StopMultiplier;
            
            // Draw ATM lines
            Draw.Line(this, "ATMTarget", false, 0, Close[0] + targetDistance, 10, Close[0] + targetDistance,
                ATMTargetColor, DashStyleHelper.Solid, 2);
                
            Draw.Line(this, "ATMStop", false, 0, Close[0] - stopDistance, 10, Close[0] - stopDistance,
                ATMStopColor, DashStyleHelper.Solid, 2);
            
            // Pattern labels
            if (ShowPatternLabels && patternConfidence > 0.7)
            {
                string patternText = $"{currentPattern}\n{currentATM.Name}";
                Brush patternColor = GetPatternColor(currentPattern);
                
                Draw.Text(this, "PatternLabel", false, patternText, 0, High[0] + (5 * TickSize),
                    0, patternColor, new Gui.Tools.SimpleFont("Arial", 10), TextAlignment.Center,
                    patternColor, Colors.Transparent, 0);
            }
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
                        Width = 250
                    };

                    // Define grid structure
                    for (int i = 0; i < 8; i++)
                        controlPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    controlPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    // Create title
                    var title = new TextBlock
                    {
                        Text = "ATM Pattern Controller",
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        FontSize = 12,
                        Margin = new Thickness(5),
                        TextAlignment = TextAlignment.Center
                    };

                    // Create info labels
                    lblCurrentPattern = CreateInfoLabel("Pattern: Unknown");
                    lblRecommendedATM = CreateInfoLabel("ATM: Conservative");
                    lblConfidence = CreateInfoLabel("Confidence: 0%");
                    lblCurrentTarget = CreateInfoLabel("Target: --");
                    lblCurrentStop = CreateInfoLabel("Stop: --");

                    // Create control buttons
                    btnAutoMode = CreateControlButton("AUTO MODE", true, OnAutoModeClick);
                    btnForceScalper = CreateControlButton("SCALPER", false, OnForceScalperClick);
                    btnForceMomentum = CreateControlButton("MOMENTUM", false, OnForceMomentumClick);
                    btnForceBreakout = CreateControlButton("BREAKOUT", false, OnForceBreakoutClick);

                    // Add to grid
                    Grid.SetRow(title, 0); Grid.SetColumn(title, 0);
                    Grid.SetRow(lblCurrentPattern, 1); Grid.SetColumn(lblCurrentPattern, 0);
                    Grid.SetRow(lblRecommendedATM, 2); Grid.SetColumn(lblRecommendedATM, 0);
                    Grid.SetRow(lblConfidence, 3); Grid.SetColumn(lblConfidence, 0);
                    Grid.SetRow(lblCurrentTarget, 4); Grid.SetColumn(lblCurrentTarget, 0);
                    Grid.SetRow(lblCurrentStop, 5); Grid.SetColumn(lblCurrentStop, 0);

                    var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };
                    buttonPanel.Children.Add(btnAutoMode);
                    buttonPanel.Children.Add(btnForceScalper);
                    
                    var buttonPanel2 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };
                    buttonPanel2.Children.Add(btnForceMomentum);
                    buttonPanel2.Children.Add(btnForceBreakout);

                    Grid.SetRow(buttonPanel, 6); Grid.SetColumn(buttonPanel, 0);
                    Grid.SetRow(buttonPanel2, 7); Grid.SetColumn(buttonPanel2, 0);

                    controlPanel.Children.Add(title);
                    controlPanel.Children.Add(lblCurrentPattern);
                    controlPanel.Children.Add(lblRecommendedATM);
                    controlPanel.Children.Add(lblConfidence);
                    controlPanel.Children.Add(lblCurrentTarget);
                    controlPanel.Children.Add(lblCurrentStop);
                    controlPanel.Children.Add(buttonPanel);
                    controlPanel.Children.Add(buttonPanel2);

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
                Width = 60,
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
                    lblCurrentPattern.Text = $"Pattern: {currentPattern}";
                    lblRecommendedATM.Text = $"ATM: {currentATM.Name}";
                    lblConfidence.Text = $"Confidence: {patternConfidence:P0}";
                    
                    double baseRisk = BaseRiskTicks * TickSize;
                    double target = Close[0] + (baseRisk * currentATM.TargetMultiplier);
                    double stop = Close[0] - (baseRisk * currentATM.StopMultiplier);
                    
                    lblCurrentTarget.Text = $"Target: {target:F2}";
                    lblCurrentStop.Text = $"Stop: {stop:F2}";

                    // Update button states
                    btnAutoMode.Background = autoModeEnabled ? Brushes.LightGreen : Brushes.DarkGray;
                    btnForceScalper.Background = (!autoModeEnabled && recommendedStrategy == ATMStrategyType.Scalper) ? Brushes.Yellow : Brushes.DarkGray;
                    btnForceMomentum.Background = (!autoModeEnabled && recommendedStrategy == ATMStrategyType.Momentum) ? Brushes.Yellow : Brushes.DarkGray;
                    btnForceBreakout.Background = (!autoModeEnabled && recommendedStrategy == ATMStrategyType.Breakout) ? Brushes.Yellow : Brushes.DarkGray;
                }
                catch (Exception ex)
                {
                    Print($"Error updating control panel: {ex.Message}");
                }
            });
        }

        // Event handlers
        private void OnAutoModeClick(object sender, RoutedEventArgs e)
        {
            autoModeEnabled = true;
            Print("ATM Controller: AUTO MODE enabled");
        }

        private void OnForceScalperClick(object sender, RoutedEventArgs e)
        {
            autoModeEnabled = false;
            recommendedStrategy = ATMStrategyType.Scalper;
            currentATM = atmStrategies[recommendedStrategy];
            Print("ATM Controller: SCALPER mode forced");
        }

        private void OnForceMomentumClick(object sender, RoutedEventArgs e)
        {
            autoModeEnabled = false;
            recommendedStrategy = ATMStrategyType.Momentum;
            currentATM = atmStrategies[recommendedStrategy];
            Print("ATM Controller: MOMENTUM mode forced");
        }

        private void OnForceBreakoutClick(object sender, RoutedEventArgs e)
        {
            autoModeEnabled = false;
            recommendedStrategy = ATMStrategyType.Breakout;
            currentATM = atmStrategies[recommendedStrategy];
            Print("ATM Controller: BREAKOUT mode forced");
        }

        private double CalculateStandardDeviation(List<double> values)
        {
            if (values.Count < 2) return 0;
            
            double mean = values.Average();
            double sumSquares = values.Sum(x => Math.Pow(x - mean, 2));
            return Math.Sqrt(sumSquares / (values.Count - 1));
        }

        private Brush GetPatternColor(MarketPattern pattern)
        {
            switch (pattern)
            {
                case MarketPattern.TrendingBullish:
                case MarketPattern.TrendingBearish:
                    return TrendingColor;
                case MarketPattern.RangingCompressed:
                case MarketPattern.RangingVolatile:
                    return RangingColor;
                case MarketPattern.VolatileChoppy:
                    return VolatileColor;
                case MarketPattern.BreakoutPending:
                    return BreakoutColor;
                default:
                    return Brushes.Gray;
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(50, 100)]
        [Display(Name="Sensitivity Level", Description="Pattern detection sensitivity (50-100)", Order=1, GroupName="Pattern Detection")]
        public int SensitivityLevel { get; set; }

        [NinjaScriptProperty]
        [Range(5, 50)]
        [Display(Name="ATR Period", Description="Period for ATR calculation", Order=2, GroupName="Pattern Detection")]
        public int ATRPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(10, 50)]
        [Display(Name="Volatility Lookback", Description="Bars to analyze for volatility", Order=3, GroupName="Pattern Detection")]
        public int VolatilityLookback { get; set; }

        [NinjaScriptProperty]
        [Range(3, 20)]
        [Display(Name="Trend Confirmation Bars", Description="Bars needed to confirm trend change", Order=4, GroupName="Pattern Detection")]
        public int TrendConfirmationBars { get; set; }

        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name="Base Risk Ticks", Description="Base risk in ticks for ATM calculations", Order=5, GroupName="ATM Settings")]
        public int BaseRiskTicks { get; set; }

        [NinjaScriptProperty]
        [Range(1.0, 5.0)]
        [Display(Name="Base Target Multiplier", Description="Default target multiplier", Order=6, GroupName="ATM Settings")]
        public double BaseTargetMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, 1.0)]
        [Display(Name="Breakeven Multiplier", Description="Breakeven distance multiplier", Order=7, GroupName="ATM Settings")]
        public double BreakevenMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, 1.0)]
        [Display(Name="Trail Multiplier", Description="Trailing stop multiplier", Order=8, GroupName="ATM Settings")]
        public double TrailMultiplier { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Control Panel", Description="Display control panel", Order=9, GroupName="Visual Settings")]
        public bool ShowControlPanel { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Pattern Labels", Description="Display pattern labels on chart", Order=10, GroupName="Visual Settings")]
        public bool ShowPatternLabels { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show ATM Lines", Description="Display ATM target/stop lines", Order=11, GroupName="Visual Settings")]
        public bool ShowATMLines { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Trending Color", Description="Color for trending patterns", Order=12, GroupName="Colors")]
        public Brush TrendingColor { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Ranging Color", Description="Color for ranging patterns", Order=13, GroupName="Colors")]
        public Brush RangingColor { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Volatile Color", Description="Color for volatile patterns", Order=14, GroupName="Colors")]
        public Brush VolatileColor { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Breakout Color", Description="Color for breakout patterns", Order=15, GroupName="Colors")]
        public Brush BreakoutColor { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="ATM Target Color", Description="Color for ATM target lines", Order=16, GroupName="Colors")]
        public Brush ATMTargetColor { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="ATM Stop Color", Description="Color for ATM stop lines", Order=17, GroupName="Colors")]
        public Brush ATMStopColor { get; set; }
        #endregion

        #region Helper Classes
        public class ATMConfig
        {
            public string Name { get; set; }
            public double TargetMultiplier { get; set; }
            public double StopMultiplier { get; set; }
            public double BreakevenMultiplier { get; set; }
            public double TrailMultiplier { get; set; }
            public string Description { get; set; }
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ATM_PatternController_v1_20241213[] cacheATM_PatternController_v1_20241213;
		public ATM_PatternController_v1_20241213 ATM_PatternController_v1_20241213(int sensitivityLevel, int aTRPeriod, int volatilityLookback, int trendConfirmationBars, int baseRiskTicks, double baseTargetMultiplier, double breakevenMultiplier, double trailMultiplier, bool showControlPanel, bool showPatternLabels, bool showATMLines, Brush trendingColor, Brush rangingColor, Brush volatileColor, Brush breakoutColor, Brush aTMTargetColor, Brush aTMStopColor)
		{
			return ATM_PatternController_v1_20241213(Input, sensitivityLevel, aTRPeriod, volatilityLookback, trendConfirmationBars, baseRiskTicks, baseTargetMultiplier, breakevenMultiplier, trailMultiplier, showControlPanel, showPatternLabels, showATMLines, trendingColor, rangingColor, volatileColor, breakoutColor, aTMTargetColor, aTMStopColor);
		}

		public ATM_PatternController_v1_20241213 ATM_PatternController_v1_20241213(ISeries<double> input, int sensitivityLevel, int aTRPeriod, int volatilityLookback, int trendConfirmationBars, int baseRiskTicks, double baseTargetMultiplier, double breakevenMultiplier, double trailMultiplier, bool showControlPanel, bool showPatternLabels, bool showATMLines, Brush trendingColor, Brush rangingColor, Brush volatileColor, Brush breakoutColor, Brush aTMTargetColor, Brush aTMStopColor)
		{
			if (cacheATM_PatternController_v1_20241213 != null)
				for (int idx = 0; idx < cacheATM_PatternController_v1_20241213.Length; idx++)
					if (cacheATM_PatternController_v1_20241213[idx] != null && cacheATM_PatternController_v1_20241213[idx].SensitivityLevel == sensitivityLevel && cacheATM_PatternController_v1_20241213[idx].ATRPeriod == aTRPeriod && cacheATM_PatternController_v1_20241213[idx].VolatilityLookback == volatilityLookback && cacheATM_PatternController_v1_20241213[idx].TrendConfirmationBars == trendConfirmationBars && cacheATM_PatternController_v1_20241213[idx].BaseRiskTicks == baseRiskTicks && cacheATM_PatternController_v1_20241213[idx].BaseTargetMultiplier == baseTargetMultiplier && cacheATM_PatternController_v1_20241213[idx].BreakevenMultiplier == breakevenMultiplier && cacheATM_PatternController_v1_20241213[idx].TrailMultiplier == trailMultiplier && cacheATM_PatternController_v1_20241213[idx].ShowControlPanel == showControlPanel && cacheATM_PatternController_v1_20241213[idx].ShowPatternLabels == showPatternLabels && cacheATM_PatternController_v1_20241213[idx].ShowATMLines == showATMLines && cacheATM_PatternController_v1_20241213[idx].TrendingColor == trendingColor && cacheATM_PatternController_v1_20241213[idx].RangingColor == rangingColor && cacheATM_PatternController_v1_20241213[idx].VolatileColor == volatileColor && cacheATM_PatternController_v1_20241213[idx].BreakoutColor == breakoutColor && cacheATM_PatternController_v1_20241213[idx].ATMTargetColor == aTMTargetColor && cacheATM_PatternController_v1_20241213[idx].ATMStopColor == aTMStopColor && cacheATM_PatternController_v1_20241213[idx].EqualsInput(input))
						return cacheATM_PatternController_v1_20241213[idx];
			return CacheIndicator<ATM_PatternController_v1_20241213>(new ATM_PatternController_v1_20241213(){ SensitivityLevel = sensitivityLevel, ATRPeriod = aTRPeriod, VolatilityLookback = volatilityLookback, TrendConfirmationBars = trendConfirmationBars, BaseRiskTicks = baseRiskTicks, BaseTargetMultiplier = baseTargetMultiplier, BreakevenMultiplier = breakevenMultiplier, TrailMultiplier = trailMultiplier, ShowControlPanel = showControlPanel, ShowPatternLabels = showPatternLabels, ShowATMLines = showATMLines, TrendingColor = trendingColor, RangingColor = rangingColor, VolatileColor = volatileColor, BreakoutColor = breakoutColor, ATMTargetColor = aTMTargetColor, ATMStopColor = aTMStopColor }, input, ref cacheATM_PatternController_v1_20241213);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ATM_PatternController_v1_20241213 ATM_PatternController_v1_20241213(int sensitivityLevel, int aTRPeriod, int volatilityLookback, int trendConfirmationBars, int baseRiskTicks, double baseTargetMultiplier, double breakevenMultiplier, double trailMultiplier, bool showControlPanel, bool showPatternLabels, bool showATMLines, Brush trendingColor, Brush rangingColor, Brush volatileColor, Brush breakoutColor, Brush aTMTargetColor, Brush aTMStopColor)
		{
			return indicator.ATM_PatternController_v1_20241213(Input, sensitivityLevel, aTRPeriod, volatilityLookback, trendConfirmationBars, baseRiskTicks, baseTargetMultiplier, breakevenMultiplier, trailMultiplier, showControlPanel, showPatternLabels, showATMLines, trendingColor, rangingColor, volatileColor, breakoutColor, aTMTargetColor, aTMStopColor);
		}

		public Indicators.ATM_PatternController_v1_20241213 ATM_PatternController_v1_20241213(ISeries<double> input , int sensitivityLevel, int aTRPeriod, int volatilityLookback, int trendConfirmationBars, int baseRiskTicks, double baseTargetMultiplier, double breakevenMultiplier, double trailMultiplier, bool showControlPanel, bool showPatternLabels, bool showATMLines, Brush trendingColor, Brush rangingColor, Brush volatileColor, Brush breakoutColor, Brush aTMTargetColor, Brush aTMStopColor)
		{
			return indicator.ATM_PatternController_v1_20241213(input, sensitivityLevel, aTRPeriod, volatilityLookback, trendConfirmationBars, baseRiskTicks, baseTargetMultiplier, breakevenMultiplier, trailMultiplier, showControlPanel, showPatternLabels, showATMLines, trendingColor, rangingColor, volatileColor, breakoutColor, aTMTargetColor, aTMStopColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ATM_PatternController_v1_20241213 ATM_PatternController_v1_20241213(int sensitivityLevel, int aTRPeriod, int volatilityLookback, int trendConfirmationBars, int baseRiskTicks, double baseTargetMultiplier, double breakevenMultiplier, double trailMultiplier, bool showControlPanel, bool showPatternLabels, bool showATMLines, Brush trendingColor, Brush rangingColor, Brush volatileColor, Brush breakoutColor, Brush aTMTargetColor, Brush aTMStopColor)
		{
			return indicator.ATM_PatternController_v1_20241213(Input, sensitivityLevel, aTRPeriod, volatilityLookback, trendConfirmationBars, baseRiskTicks, baseTargetMultiplier, breakevenMultiplier, trailMultiplier, showControlPanel, showPatternLabels, showATMLines, trendingColor, rangingColor, volatileColor, breakoutColor, aTMTargetColor, aTMStopColor);
		}

		public Indicators.ATM_PatternController_v1_20241213 ATM_PatternController_v1_20241213(ISeries<double> input , int sensitivityLevel, int aTRPeriod, int volatilityLookback, int trendConfirmationBars, int baseRiskTicks, double baseTargetMultiplier, double breakevenMultiplier, double trailMultiplier, bool showControlPanel, bool showPatternLabels, bool showATMLines, Brush trendingColor, Brush rangingColor, Brush volatileColor, Brush breakoutColor, Brush aTMTargetColor, Brush aTMStopColor)
		{
			return indicator.ATM_PatternController_v1_20241213(input, sensitivityLevel, aTRPeriod, volatilityLookback, trendConfirmationBars, baseRiskTicks, baseTargetMultiplier, breakevenMultiplier, trailMultiplier, showControlPanel, showPatternLabels, showATMLines, trendingColor, rangingColor, volatileColor, breakoutColor, aTMTargetColor, aTMStopColor);
		}
	}
}

#endregion
