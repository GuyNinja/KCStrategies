//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// 2Agent_v1_20241213.cs - Second Agent in Sequential Series - RSI Momentum Focus
//
#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    /// <summary>
    /// 2Agent - Second in Sequential Agent Series
    /// RSI momentum analysis with divergence detection
    /// Focus: Overbought/oversold conditions with momentum shifts
    /// Date: December 13, 2024
    /// </summary>
    public class TwoAgent_v1 : Indicator
    {
        #region Variables - Minimal & Lean
        // Core analysis variables
        private double currentMomentum = 0; // -1 = Bearish, 0 = Neutral, 1 = Bullish
        private double momentumStrength = 0; // 0-100 confidence
        private bool isInitialized = false;
        
        // RSI and momentum indicators
        private RSI rsi;
        private ROC roc; // Rate of Change for momentum
        
        // Divergence tracking
        private double lastRsiHigh = 0;
        private double lastRsiLow = 100;
        private double lastPriceHigh = 0;
        private double lastPriceLow = double.MaxValue;
        private bool bullishDivergence = false;
        private bool bearishDivergence = false;
        
        // Agent status
        private string agentStatus = "Initializing";
        private string lastSignal = "None";
        private string divergenceStatus = "None";
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"2Agent v1 - Second Sequential Agent - RSI Momentum Analysis";
                Name = "2Agent_v1_20241213";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false; // Separate window
                DisplayInDataBox = true;
                DrawOnPricePanel = false;
                
                // Minimal parameters following user preference
                RsiPeriod = 14;
                RocPeriod = 10;
                OverboughtLevel = 70;
                OversoldLevel = 30;
                AlertThreshold = 75;
                
                // Display settings
                AgentColor = Brushes.Orange;
                LineWidth = 2;
                ShowInfoPanel = true;
                EnableLogging = false;
                EnableDivergenceDetection = true;
                
                // Add plots for agent output
                AddPlot(new Stroke(Brushes.Orange, 2), PlotStyle.Line, "MomentumSignal");
                AddPlot(new Stroke(Brushes.Yellow, 1), PlotStyle.Line, "Strength");
            }
            else if (State == State.Configure)
            {
                // Initialize minimal indicators
                rsi = RSI(RsiPeriod, 1);
                roc = ROC(RocPeriod);
            }
            else if (State == State.DataLoaded)
            {
                isInitialized = true;
                agentStatus = "Active";
                if (EnableLogging)
                    Print($"2Agent initialized - RSI: {RsiPeriod}, ROC: {RocPeriod}");
            }
        }

        protected override void OnBarUpdate()
        {
            if (!isInitialized || CurrentBar < Math.Max(RsiPeriod, RocPeriod)) 
            {
                agentStatus = "Waiting for data";
                return;
            }

            // Core 2Agent Logic - RSI Momentum Focus
            AnalyzeMomentum();
            DetectDivergences();
            CalculateSignalStrength();
            UpdateAgentOutput();
            UpdateDisplay();
        }

        #region Core Agent Methods - Lean Implementation
        private void AnalyzeMomentum()
        {
            double currentRsi = rsi[0];
            double currentRoc = roc[0];
            
            // Simple RSI-based momentum analysis
            if (currentRsi < OversoldLevel && currentRoc > 0)
                currentMomentum = 1; // Bullish - oversold with positive momentum
            else if (currentRsi > OverboughtLevel && currentRoc < 0)
                currentMomentum = -1; // Bearish - overbought with negative momentum
            else if (currentRsi > 50 && currentRoc > 0)
                currentMomentum = 0.5; // Mild bullish
            else if (currentRsi < 50 && currentRoc < 0)
                currentMomentum = -0.5; // Mild bearish
            else
                currentMomentum = 0; // Neutral
        }

        private void DetectDivergences()
        {
            if (!EnableDivergenceDetection) return;
            
            double currentRsi = rsi[0];
            double currentPrice = Close[0];
            
            // Track highs and lows for divergence
            if (currentPrice > lastPriceHigh)
            {
                lastPriceHigh = currentPrice;
                lastRsiHigh = currentRsi;
            }
            
            if (currentPrice < lastPriceLow)
            {
                lastPriceLow = currentPrice;
                lastRsiLow = currentRsi;
            }
            
            // Simple divergence detection
            bullishDivergence = (currentPrice < lastPriceLow && currentRsi > lastRsiLow);
            bearishDivergence = (currentPrice > lastPriceHigh && currentRsi < lastRsiHigh);
            
            divergenceStatus = bullishDivergence ? "Bull Div" : 
                              bearishDivergence ? "Bear Div" : "None";
        }

        private void CalculateSignalStrength()
        {
            double currentRsi = rsi[0];
            double rsiExtreme = Math.Min(currentRsi, 100 - currentRsi); // Distance from 50
            double rocStrength = Math.Abs(roc[0]) * 10; // ROC magnitude
            
            // Calculate base strength from RSI extremes and momentum
            momentumStrength = Math.Min(100, (rsiExtreme * 2) + rocStrength);
            
            // Boost strength for divergences
            if (bullishDivergence || bearishDivergence)
                momentumStrength = Math.Min(100, momentumStrength * 1.5);
            
            // Update signal status
            if (momentumStrength > AlertThreshold)
            {
                lastSignal = currentMomentum > 0.5 ? "LONG" : 
                            currentMomentum < -0.5 ? "SHORT" : "NEUTRAL";
                agentStatus = $"Signal: {lastSignal}";
            }
            else
            {
                lastSignal = "Weak";
                agentStatus = "Monitoring";
            }
        }

        private void UpdateAgentOutput()
        {
            // Output to plots for coordination with other agents
            Values[0][0] = currentMomentum * 50 + 50; // Scale -1,1 to 0,100
            Values[1][0] = momentumStrength;
            
            if (EnableLogging && momentumStrength > AlertThreshold)
            {
                Print($"2Agent Signal: {lastSignal}, Strength: {momentumStrength:F1}, {divergenceStatus}");
            }
        }

        private void UpdateDisplay()
        {
            if (!ShowInfoPanel) return;
            
            // Get current RSI for display
            double currentRsi = rsi[0];
            
            // Minimal info display
            string info = $"2Agent v1\n" +
                         $"Status: {agentStatus}\n" +
                         $"RSI: {currentRsi:F1}\n" +
                         $"Momentum: {(currentMomentum > 0.5 ? "▲" : currentMomentum < -0.5 ? "▼" : "●")}\n" +
                         $"Strength: {momentumStrength:F0}%\n" +
                         $"Signal: {lastSignal}\n" +
                         $"Div: {divergenceStatus}";
            
            Draw.TextFixed(this, "2AgentInfo", info, TextPosition.TopRight, 
                          Brushes.White, new SimpleFont("Arial", 10), Brushes.Black, 
                          Brushes.DarkSlateGray, 50);
        }
        #endregion

        #region Properties - Minimal Configuration
        [NinjaScriptProperty]
        [Range(7, 50)]
        [Display(Name="RSI Period", Description="RSI period for momentum analysis", Order=1, GroupName="2Agent Core")]
        public int RsiPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(5, 30)]
        [Display(Name="ROC Period", Description="Rate of Change period", Order=2, GroupName="2Agent Core")]
        public int RocPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(60, 90)]
        [Display(Name="Overbought Level", Description="RSI overbought threshold", Order=3, GroupName="2Agent Core")]
        public double OverboughtLevel { get; set; }

        [NinjaScriptProperty]
        [Range(10, 40)]
        [Display(Name="Oversold Level", Description="RSI oversold threshold", Order=4, GroupName="2Agent Core")]
        public double OversoldLevel { get; set; }

        [NinjaScriptProperty]
        [Range(50, 95)]
        [Display(Name="Alert Threshold", Description="Minimum strength for signal alerts", Order=5, GroupName="2Agent Core")]
        public double AlertThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable Divergence Detection", Description="Enable RSI divergence analysis", Order=6, GroupName="2Agent Core")]
        public bool EnableDivergenceDetection { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Agent Color", Description="Color for 2Agent display", Order=7, GroupName="Display")]
        public Brush AgentColor { get; set; }

        [NinjaScriptProperty]
        [Range(1, 5)]
        [Display(Name="Line Width", Description="Width of momentum line", Order=8, GroupName="Display")]
        public int LineWidth { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Info Panel", Description="Display agent information panel", Order=9, GroupName="Display")]
        public bool ShowInfoPanel { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable Logging", Description="Enable debug logging", Order=10, GroupName="Debug")]
        public bool EnableLogging { get; set; }
        #endregion

        #region Public Methods for Agent Coordination
        /// <summary>
        /// Get current momentum signal for multi-agent coordination
        /// Returns: -1 (Bearish), 0 (Neutral), 1 (Bullish)
        /// </summary>
        public double GetMomentumSignal() => currentMomentum;

        /// <summary>
        /// Get signal confidence strength 0-100
        /// </summary>
        public double GetSignalStrength() => momentumStrength;

        /// <summary>
        /// Get current agent status for monitoring
        /// </summary>
        public string GetAgentStatus() => agentStatus;

        /// <summary>
        /// Check if agent has strong signal above threshold
        /// </summary>
        public bool HasStrongSignal() => momentumStrength > AlertThreshold;

        /// <summary>
        /// Get current RSI value for external analysis
        /// </summary>
        public double GetRsiValue() => rsi[0];

        /// <summary>
        /// Check for bullish or bearish divergence
        /// </summary>
        public bool HasDivergence() => bullishDivergence || bearishDivergence;

        /// <summary>
        /// Get divergence type: "Bull", "Bear", or "None"
        /// </summary>
        public string GetDivergenceType() => divergenceStatus;
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TwoAgent_v1[] cacheTwoAgent_v1;
		public TwoAgent_v1 TwoAgent_v1(int rsiPeriod, int rocPeriod, double overboughtLevel, double oversoldLevel, double alertThreshold, bool enableDivergenceDetection, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			return TwoAgent_v1(Input, rsiPeriod, rocPeriod, overboughtLevel, oversoldLevel, alertThreshold, enableDivergenceDetection, agentColor, lineWidth, showInfoPanel, enableLogging);
		}

		public TwoAgent_v1 TwoAgent_v1(ISeries<double> input, int rsiPeriod, int rocPeriod, double overboughtLevel, double oversoldLevel, double alertThreshold, bool enableDivergenceDetection, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			if (cacheTwoAgent_v1 != null)
				for (int idx = 0; idx < cacheTwoAgent_v1.Length; idx++)
					if (cacheTwoAgent_v1[idx] != null && cacheTwoAgent_v1[idx].RsiPeriod == rsiPeriod && cacheTwoAgent_v1[idx].RocPeriod == rocPeriod && cacheTwoAgent_v1[idx].OverboughtLevel == overboughtLevel && cacheTwoAgent_v1[idx].OversoldLevel == oversoldLevel && cacheTwoAgent_v1[idx].AlertThreshold == alertThreshold && cacheTwoAgent_v1[idx].EnableDivergenceDetection == enableDivergenceDetection && cacheTwoAgent_v1[idx].AgentColor == agentColor && cacheTwoAgent_v1[idx].LineWidth == lineWidth && cacheTwoAgent_v1[idx].ShowInfoPanel == showInfoPanel && cacheTwoAgent_v1[idx].EnableLogging == enableLogging && cacheTwoAgent_v1[idx].EqualsInput(input))
						return cacheTwoAgent_v1[idx];
			return CacheIndicator<TwoAgent_v1>(new TwoAgent_v1(){ RsiPeriod = rsiPeriod, RocPeriod = rocPeriod, OverboughtLevel = overboughtLevel, OversoldLevel = oversoldLevel, AlertThreshold = alertThreshold, EnableDivergenceDetection = enableDivergenceDetection, AgentColor = agentColor, LineWidth = lineWidth, ShowInfoPanel = showInfoPanel, EnableLogging = enableLogging }, input, ref cacheTwoAgent_v1);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TwoAgent_v1 TwoAgent_v1(int rsiPeriod, int rocPeriod, double overboughtLevel, double oversoldLevel, double alertThreshold, bool enableDivergenceDetection, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			return indicator.TwoAgent_v1(Input, rsiPeriod, rocPeriod, overboughtLevel, oversoldLevel, alertThreshold, enableDivergenceDetection, agentColor, lineWidth, showInfoPanel, enableLogging);
		}

		public Indicators.TwoAgent_v1 TwoAgent_v1(ISeries<double> input , int rsiPeriod, int rocPeriod, double overboughtLevel, double oversoldLevel, double alertThreshold, bool enableDivergenceDetection, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			return indicator.TwoAgent_v1(input, rsiPeriod, rocPeriod, overboughtLevel, oversoldLevel, alertThreshold, enableDivergenceDetection, agentColor, lineWidth, showInfoPanel, enableLogging);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TwoAgent_v1 TwoAgent_v1(int rsiPeriod, int rocPeriod, double overboughtLevel, double oversoldLevel, double alertThreshold, bool enableDivergenceDetection, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			return indicator.TwoAgent_v1(Input, rsiPeriod, rocPeriod, overboughtLevel, oversoldLevel, alertThreshold, enableDivergenceDetection, agentColor, lineWidth, showInfoPanel, enableLogging);
		}

		public Indicators.TwoAgent_v1 TwoAgent_v1(ISeries<double> input , int rsiPeriod, int rocPeriod, double overboughtLevel, double oversoldLevel, double alertThreshold, bool enableDivergenceDetection, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			return indicator.TwoAgent_v1(input, rsiPeriod, rocPeriod, overboughtLevel, oversoldLevel, alertThreshold, enableDivergenceDetection, agentColor, lineWidth, showInfoPanel, enableLogging);
		}
	}
}

#endregion
