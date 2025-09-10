//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// 1Agent_v1_20241213.cs - First Agent in Sequential Series - Minimal & Efficient
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
    /// 1Agent - First in Sequential Agent Series
    /// Minimal, efficient price action analysis agent
    /// Focus: Basic trend detection with minimal complexity
    /// Date: December 13, 2024
    /// </summary>
    public class OneAgent_v1 : Indicator
    {
        #region Variables - Minimal & Lean
        // Core analysis variables
        private double currentTrend = 0; // -1 = Down, 0 = Neutral, 1 = Up
        private double signalStrength = 0; // 0-100 confidence
        private bool isInitialized = false;
        
        // Simple moving averages for trend
        private SMA fastMA;
        private SMA slowMA;
        
        // Agent status
        private string agentStatus = "Initializing";
        private string lastSignal = "None";
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"1Agent v1 - First Sequential Agent - Minimal Price Action Analysis";
                Name = "1Agent_v1_20241213";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false; // Separate window
                DisplayInDataBox = true;
                DrawOnPricePanel = false;
                
                // Minimal parameters following user preference
                FastPeriod = 9;
                SlowPeriod = 21;
                AlertThreshold = 70;
                
                // Display settings
                AgentColor = Brushes.LimeGreen;
                LineWidth = 2;
                ShowInfoPanel = true;
                EnableLogging = false;
                
                // Add plots for agent output
                AddPlot(new Stroke(Brushes.LimeGreen, 2), PlotStyle.Line, "TrendSignal");
                AddPlot(new Stroke(Brushes.Orange, 1), PlotStyle.Line, "Strength");
            }
            else if (State == State.Configure)
            {
                // Initialize minimal indicators
                fastMA = SMA(FastPeriod);
                slowMA = SMA(SlowPeriod);
            }
            else if (State == State.DataLoaded)
            {
                isInitialized = true;
                agentStatus = "Active";
                if (EnableLogging)
                    Print($"1Agent initialized - Fast: {FastPeriod}, Slow: {SlowPeriod}");
            }
        }

        protected override void OnBarUpdate()
        {
            if (!isInitialized || CurrentBar < Math.Max(FastPeriod, SlowPeriod)) 
            {
                agentStatus = "Waiting for data";
                return;
            }

            // Core 1Agent Logic - Minimal & Efficient
            CalculateTrend();
            CalculateSignalStrength();
            UpdateAgentOutput();
            UpdateDisplay();
        }

        #region Core Agent Methods - Lean Implementation
        private void CalculateTrend()
        {
            // Simple MA crossover for trend direction
            double fastValue = fastMA[0];
            double slowValue = slowMA[0];
            double priceVsFast = Close[0] - fastValue;
            double fastVsSlow = fastValue - slowValue;
            
            // Minimal logic - following user preference for simplicity
            if (Close[0] > fastValue && fastValue > slowValue)
                currentTrend = 1; // Bullish
            else if (Close[0] < fastValue && fastValue < slowValue)
                currentTrend = -1; // Bearish
            else
                currentTrend = 0; // Neutral
        }

        private void CalculateSignalStrength()
        {
            // Calculate confidence based on alignment and momentum
            double alignment = Math.Abs(fastMA[0] - slowMA[0]) / Close[0] * 1000;
            double momentum = Math.Abs(Close[0] - Close[1]) / Close[0] * 1000;
            
            // Simple strength calculation
            signalStrength = Math.Min(100, (alignment + momentum) * 10);
            
            // Update signal status
            if (signalStrength > AlertThreshold)
            {
                lastSignal = currentTrend > 0 ? "LONG" : currentTrend < 0 ? "SHORT" : "NEUTRAL";
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
            Values[0][0] = currentTrend * 50 + 50; // Scale -1,1 to 0,100
            Values[1][0] = signalStrength;
            
            if (EnableLogging && signalStrength > AlertThreshold)
            {
                Print($"1Agent Signal: {lastSignal}, Strength: {signalStrength:F1}");
            }
        }

        private void UpdateDisplay()
        {
            if (!ShowInfoPanel) return;
            
            // Minimal info display
            string info = $"1Agent v1\n" +
                         $"Status: {agentStatus}\n" +
                         $"Trend: {(currentTrend > 0 ? "▲" : currentTrend < 0 ? "▼" : "●")}\n" +
                         $"Strength: {signalStrength:F0}%\n" +
                         $"Signal: {lastSignal}";
            
            Draw.TextFixed(this, "1AgentInfo", info, TextPosition.TopLeft, 
                          Brushes.White, new SimpleFont("Arial", 10), Brushes.Black, 
                          Brushes.DarkGray, 50);
        }
        #endregion

        #region Properties - Minimal Configuration
        [NinjaScriptProperty]
        [Range(3, 50)]
        [Display(Name="Fast Period", Description="Fast MA period for trend detection", Order=1, GroupName="1Agent Core")]
        public int FastPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(10, 100)]
        [Display(Name="Slow Period", Description="Slow MA period for trend confirmation", Order=2, GroupName="1Agent Core")]
        public int SlowPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(50, 95)]
        [Display(Name="Alert Threshold", Description="Minimum strength for signal alerts", Order=3, GroupName="1Agent Core")]
        public double AlertThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Agent Color", Description="Color for 1Agent display", Order=4, GroupName="Display")]
        public Brush AgentColor { get; set; }

        [NinjaScriptProperty]
        [Range(1, 5)]
        [Display(Name="Line Width", Description="Width of trend line", Order=5, GroupName="Display")]
        public int LineWidth { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Info Panel", Description="Display agent information panel", Order=6, GroupName="Display")]
        public bool ShowInfoPanel { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable Logging", Description="Enable debug logging", Order=7, GroupName="Debug")]
        public bool EnableLogging { get; set; }
        #endregion

        #region Public Methods for Agent Coordination
        /// <summary>
        /// Get current trend signal for multi-agent coordination
        /// Returns: -1 (Bearish), 0 (Neutral), 1 (Bullish)
        /// </summary>
        public double GetTrendSignal() => currentTrend;

        /// <summary>
        /// Get signal confidence strength 0-100
        /// </summary>
        public double GetSignalStrength() => signalStrength;

        /// <summary>
        /// Get current agent status for monitoring
        /// </summary>
        public string GetAgentStatus() => agentStatus;

        /// <summary>
        /// Check if agent has strong signal above threshold
        /// </summary>
        public bool HasStrongSignal() => signalStrength > AlertThreshold;
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OneAgent_v1[] cacheOneAgent_v1;
		public OneAgent_v1 OneAgent_v1(int fastPeriod, int slowPeriod, double alertThreshold, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			return OneAgent_v1(Input, fastPeriod, slowPeriod, alertThreshold, agentColor, lineWidth, showInfoPanel, enableLogging);
		}

		public OneAgent_v1 OneAgent_v1(ISeries<double> input, int fastPeriod, int slowPeriod, double alertThreshold, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			if (cacheOneAgent_v1 != null)
				for (int idx = 0; idx < cacheOneAgent_v1.Length; idx++)
					if (cacheOneAgent_v1[idx] != null && cacheOneAgent_v1[idx].FastPeriod == fastPeriod && cacheOneAgent_v1[idx].SlowPeriod == slowPeriod && cacheOneAgent_v1[idx].AlertThreshold == alertThreshold && cacheOneAgent_v1[idx].AgentColor == agentColor && cacheOneAgent_v1[idx].LineWidth == lineWidth && cacheOneAgent_v1[idx].ShowInfoPanel == showInfoPanel && cacheOneAgent_v1[idx].EnableLogging == enableLogging && cacheOneAgent_v1[idx].EqualsInput(input))
						return cacheOneAgent_v1[idx];
			return CacheIndicator<OneAgent_v1>(new OneAgent_v1(){ FastPeriod = fastPeriod, SlowPeriod = slowPeriod, AlertThreshold = alertThreshold, AgentColor = agentColor, LineWidth = lineWidth, ShowInfoPanel = showInfoPanel, EnableLogging = enableLogging }, input, ref cacheOneAgent_v1);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OneAgent_v1 OneAgent_v1(int fastPeriod, int slowPeriod, double alertThreshold, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			return indicator.OneAgent_v1(Input, fastPeriod, slowPeriod, alertThreshold, agentColor, lineWidth, showInfoPanel, enableLogging);
		}

		public Indicators.OneAgent_v1 OneAgent_v1(ISeries<double> input , int fastPeriod, int slowPeriod, double alertThreshold, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			return indicator.OneAgent_v1(input, fastPeriod, slowPeriod, alertThreshold, agentColor, lineWidth, showInfoPanel, enableLogging);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OneAgent_v1 OneAgent_v1(int fastPeriod, int slowPeriod, double alertThreshold, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			return indicator.OneAgent_v1(Input, fastPeriod, slowPeriod, alertThreshold, agentColor, lineWidth, showInfoPanel, enableLogging);
		}

		public Indicators.OneAgent_v1 OneAgent_v1(ISeries<double> input , int fastPeriod, int slowPeriod, double alertThreshold, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			return indicator.OneAgent_v1(input, fastPeriod, slowPeriod, alertThreshold, agentColor, lineWidth, showInfoPanel, enableLogging);
		}
	}
}

#endregion
