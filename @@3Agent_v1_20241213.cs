//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// 3Agent_v1_20241213.cs - Third Agent in Sequential Series - Volume Analysis Focus
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
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    /// <summary>
    /// 3Agent - Third in Sequential Agent Series
    /// Volume analysis with surge detection and flow analysis
    /// Focus: Unusual volume patterns and early warning detection
    /// Date: December 13, 2024
    /// </summary>
    public class ThreeAgent_v1 : Indicator
    {
        #region Variables - Minimal & Lean
        // Core analysis variables
        private double currentVolumeSignal = 0; // -1 = Bearish Volume, 0 = Neutral, 1 = Bullish Volume
        private double volumeStrength = 0; // 0-100 confidence
        private bool isInitialized = false;
        
        // Volume analysis indicators
        private VOLMA volMA;
        private SMA volSMA;
        private double avgVolume = 0;
        private List<double> recentVolumes = new List<double>();
        
        // Volume surge detection
        private bool volumeSurge = false;
        private double surgeMultiplier = 0;
        private double lastSurgeBar = -1;
        private string volumeCondition = "Normal";
        
        // Volume-price relationship
        private bool bullishVolumeFlow = false;
        private bool bearishVolumeFlow = false;
        private double volumePriceRatio = 0;
        
        // Agent status
        private string agentStatus = "Initializing";
        private string lastSignal = "None";
        private string flowDirection = "Neutral";
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"3Agent v1 - Third Sequential Agent - Volume Analysis & Surge Detection";
                Name = "3Agent_v1_20241213";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false; // Separate window
                DisplayInDataBox = true;
                DrawOnPricePanel = false;
                
                // Minimal parameters following user preference
                VolumePeriod = 20;
                SurgeThreshold = 2.0;
                FlowSensitivity = 1.5;
                AlertThreshold = 75;
                
                // Display settings
                AgentColor = Brushes.Cyan;
                LineWidth = 2;
                ShowInfoPanel = true;
                EnableLogging = false;
                EnableSurgeDetection = true;
                EnableFlowAnalysis = true;
                
                // Add plots for agent output
                AddPlot(new Stroke(Brushes.Cyan, 2), PlotStyle.Line, "VolumeSignal");
                AddPlot(new Stroke(Brushes.Magenta, 1), PlotStyle.Line, "Strength");
            }
            else if (State == State.Configure)
            {
                // Initialize minimal indicators
                volMA = VOLMA(VolumePeriod);
                volSMA = SMA(Volumes[0], VolumePeriod);
            }
            else if (State == State.DataLoaded)
            {
                isInitialized = true;
                agentStatus = "Active";
                if (EnableLogging)
                    Print($"3Agent initialized - Volume Period: {VolumePeriod}, Surge Threshold: {SurgeThreshold}");
            }
        }

        protected override void OnBarUpdate()
        {
            if (!isInitialized || CurrentBar < VolumePeriod) 
            {
                agentStatus = "Waiting for data";
                return;
            }

            // Core 3Agent Logic - Volume Analysis Focus
            AnalyzeVolumeFlow();
            DetectVolumeSurges();
            AnalyzeVolumePriceRelationship();
            CalculateSignalStrength();
            UpdateAgentOutput();
            UpdateDisplay();
        }

        #region Core Agent Methods - Lean Implementation
        private void AnalyzeVolumeFlow()
        {
            double currentVolume = Volume[0];
            double avgVol = volSMA[0];
            
            // Track recent volumes for flow analysis
            if (recentVolumes.Count >= 5)
                recentVolumes.RemoveAt(0);
            recentVolumes.Add(currentVolume);
            
            if (recentVolumes.Count < 5) return;
            
            // Simple volume flow analysis
            double volumeIncrease = recentVolumes.Skip(2).Average() / recentVolumes.Take(3).Average();
            double priceChange = Close[0] - Close[2];
            
            // Determine volume flow direction
            if (EnableFlowAnalysis)
            {
                bullishVolumeFlow = (volumeIncrease > FlowSensitivity && priceChange > 0);
                bearishVolumeFlow = (volumeIncrease > FlowSensitivity && priceChange < 0);
                
                flowDirection = bullishVolumeFlow ? "Bullish" : 
                               bearishVolumeFlow ? "Bearish" : "Neutral";
            }
        }

        private void DetectVolumeSurges()
        {
            if (!EnableSurgeDetection) return;
            
            double currentVolume = Volume[0];
            double avgVol = volSMA[0];
            
            // Calculate surge multiplier
            surgeMultiplier = avgVol > 0 ? currentVolume / avgVol : 1.0;
            
            // Detect volume surge
            volumeSurge = (surgeMultiplier >= SurgeThreshold);
            
            if (volumeSurge && CurrentBar != lastSurgeBar)
            {
                lastSurgeBar = CurrentBar;
                volumeCondition = $"SURGE {surgeMultiplier:F1}x";
                
                if (EnableLogging)
                    Print($"Volume Surge Detected: {surgeMultiplier:F1}x average at bar {CurrentBar}");
            }
            else if (!volumeSurge)
            {
                volumeCondition = surgeMultiplier > 1.2 ? "High" : 
                                 surgeMultiplier < 0.8 ? "Low" : "Normal";
            }
        }

        private void AnalyzeVolumePriceRelationship()
        {
            double priceChange = Close[0] - Close[1];
            double volumeChange = Volume[0] - Volume[1];
            double avgVol = volSMA[0];
            
            // Calculate volume-price ratio
            if (Math.Abs(priceChange) > 0)
            {
                volumePriceRatio = (Volume[0] / avgVol) * Math.Sign(priceChange);
            }
            
            // Determine volume signal based on price-volume relationship
            if (priceChange > 0 && Volume[0] > avgVol * 1.2)
                currentVolumeSignal = 1; // Bullish - price up on high volume
            else if (priceChange < 0 && Volume[0] > avgVol * 1.2)
                currentVolumeSignal = -1; // Bearish - price down on high volume
            else if (Math.Abs(priceChange) > 0 && Volume[0] < avgVol * 0.8)
                currentVolumeSignal = -Math.Sign(priceChange) * 0.5; // Weak move - low volume
            else
                currentVolumeSignal = 0; // Neutral
        }

        private void CalculateSignalStrength()
        {
            double volumeRatio = Volume[0] / volSMA[0];
            double surgeBonus = volumeSurge ? 25 : 0;
            double flowBonus = (bullishVolumeFlow || bearishVolumeFlow) ? 20 : 0;
            
            // Calculate base strength from volume characteristics
            double baseStrength = Math.Min(50, (volumeRatio - 1) * 50);
            volumeStrength = Math.Max(0, Math.Min(100, baseStrength + surgeBonus + flowBonus));
            
            // Update signal status
            if (volumeStrength > AlertThreshold)
            {
                lastSignal = currentVolumeSignal > 0.5 ? "BULL VOL" : 
                            currentVolumeSignal < -0.5 ? "BEAR VOL" : "HIGH VOL";
                agentStatus = $"Signal: {lastSignal}";
            }
            else
            {
                lastSignal = "Quiet";
                agentStatus = "Monitoring";
            }
        }

        private void UpdateAgentOutput()
        {
            // Output to plots for coordination with other agents
            Values[0][0] = currentVolumeSignal * 50 + 50; // Scale -1,1 to 0,100
            Values[1][0] = volumeStrength;
            
            if (EnableLogging && volumeStrength > AlertThreshold)
            {
                Print($"3Agent Signal: {lastSignal}, Strength: {volumeStrength:F1}, Condition: {volumeCondition}");
            }
        }

        private void UpdateDisplay()
        {
            if (!ShowInfoPanel) return;
            
            // Get current volume info for display
            double currentVolume = Volume[0];
            double avgVol = volSMA[0];
            
            // Minimal info display
            string info = $"3Agent v1\n" +
                         $"Status: {agentStatus}\n" +
                         $"Volume: {currentVolume:F0}\n" +
                         $"Avg: {avgVol:F0}\n" +
                         $"Ratio: {surgeMultiplier:F1}x\n" +
                         $"Condition: {volumeCondition}\n" +
                         $"Flow: {flowDirection}\n" +
                         $"Strength: {volumeStrength:F0}%\n" +
                         $"Signal: {lastSignal}";
            
            Draw.TextFixed(this, "3AgentInfo", info, TextPosition.BottomLeft, 
                          Brushes.White, new SimpleFont("Arial", 10), Brushes.Black, 
                          Brushes.DarkCyan, 50);
        }
        #endregion

        #region Properties - Minimal Configuration
        [NinjaScriptProperty]
        [Range(10, 100)]
        [Display(Name="Volume Period", Description="Period for volume moving average", Order=1, GroupName="3Agent Core")]
        public int VolumePeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1.5, 5.0)]
        [Display(Name="Surge Threshold", Description="Volume multiplier for surge detection", Order=2, GroupName="3Agent Core")]
        public double SurgeThreshold { get; set; }

        [NinjaScriptProperty]
        [Range(1.0, 3.0)]
        [Display(Name="Flow Sensitivity", Description="Sensitivity for volume flow detection", Order=3, GroupName="3Agent Core")]
        public double FlowSensitivity { get; set; }

        [NinjaScriptProperty]
        [Range(50, 95)]
        [Display(Name="Alert Threshold", Description="Minimum strength for signal alerts", Order=4, GroupName="3Agent Core")]
        public double AlertThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable Surge Detection", Description="Enable volume surge analysis", Order=5, GroupName="3Agent Core")]
        public bool EnableSurgeDetection { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable Flow Analysis", Description="Enable volume flow direction analysis", Order=6, GroupName="3Agent Core")]
        public bool EnableFlowAnalysis { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Agent Color", Description="Color for 3Agent display", Order=7, GroupName="Display")]
        public Brush AgentColor { get; set; }

        [NinjaScriptProperty]
        [Range(1, 5)]
        [Display(Name="Line Width", Description="Width of volume signal line", Order=8, GroupName="Display")]
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
        /// Get current volume signal for multi-agent coordination
        /// Returns: -1 (Bearish Volume), 0 (Neutral), 1 (Bullish Volume)
        /// </summary>
        public double GetVolumeSignal() => currentVolumeSignal;

        /// <summary>
        /// Get signal confidence strength 0-100
        /// </summary>
        public double GetSignalStrength() => volumeStrength;

        /// <summary>
        /// Get current agent status for monitoring
        /// </summary>
        public string GetAgentStatus() => agentStatus;

        /// <summary>
        /// Check if agent has strong signal above threshold
        /// </summary>
        public bool HasStrongSignal() => volumeStrength > AlertThreshold;

        /// <summary>
        /// Check for volume surge detection
        /// </summary>
        public bool HasVolumeSurge() => volumeSurge;

        /// <summary>
        /// Get volume surge multiplier
        /// </summary>
        public double GetSurgeMultiplier() => surgeMultiplier;

        /// <summary>
        /// Get current volume condition description
        /// </summary>
        public string GetVolumeCondition() => volumeCondition;

        /// <summary>
        /// Get volume flow direction: "Bullish", "Bearish", or "Neutral"
        /// </summary>
        public string GetFlowDirection() => flowDirection;

        /// <summary>
        /// Check for bullish volume flow
        /// </summary>
        public bool HasBullishFlow() => bullishVolumeFlow;

        /// <summary>
        /// Check for bearish volume flow
        /// </summary>
        public bool HasBearishFlow() => bearishVolumeFlow;
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ThreeAgent_v1[] cacheThreeAgent_v1;
		public ThreeAgent_v1 ThreeAgent_v1(int volumePeriod, double surgeThreshold, double flowSensitivity, double alertThreshold, bool enableSurgeDetection, bool enableFlowAnalysis, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			return ThreeAgent_v1(Input, volumePeriod, surgeThreshold, flowSensitivity, alertThreshold, enableSurgeDetection, enableFlowAnalysis, agentColor, lineWidth, showInfoPanel, enableLogging);
		}

		public ThreeAgent_v1 ThreeAgent_v1(ISeries<double> input, int volumePeriod, double surgeThreshold, double flowSensitivity, double alertThreshold, bool enableSurgeDetection, bool enableFlowAnalysis, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			if (cacheThreeAgent_v1 != null)
				for (int idx = 0; idx < cacheThreeAgent_v1.Length; idx++)
					if (cacheThreeAgent_v1[idx] != null && cacheThreeAgent_v1[idx].VolumePeriod == volumePeriod && cacheThreeAgent_v1[idx].SurgeThreshold == surgeThreshold && cacheThreeAgent_v1[idx].FlowSensitivity == flowSensitivity && cacheThreeAgent_v1[idx].AlertThreshold == alertThreshold && cacheThreeAgent_v1[idx].EnableSurgeDetection == enableSurgeDetection && cacheThreeAgent_v1[idx].EnableFlowAnalysis == enableFlowAnalysis && cacheThreeAgent_v1[idx].AgentColor == agentColor && cacheThreeAgent_v1[idx].LineWidth == lineWidth && cacheThreeAgent_v1[idx].ShowInfoPanel == showInfoPanel && cacheThreeAgent_v1[idx].EnableLogging == enableLogging && cacheThreeAgent_v1[idx].EqualsInput(input))
						return cacheThreeAgent_v1[idx];
			return CacheIndicator<ThreeAgent_v1>(new ThreeAgent_v1(){ VolumePeriod = volumePeriod, SurgeThreshold = surgeThreshold, FlowSensitivity = flowSensitivity, AlertThreshold = alertThreshold, EnableSurgeDetection = enableSurgeDetection, EnableFlowAnalysis = enableFlowAnalysis, AgentColor = agentColor, LineWidth = lineWidth, ShowInfoPanel = showInfoPanel, EnableLogging = enableLogging }, input, ref cacheThreeAgent_v1);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ThreeAgent_v1 ThreeAgent_v1(int volumePeriod, double surgeThreshold, double flowSensitivity, double alertThreshold, bool enableSurgeDetection, bool enableFlowAnalysis, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			return indicator.ThreeAgent_v1(Input, volumePeriod, surgeThreshold, flowSensitivity, alertThreshold, enableSurgeDetection, enableFlowAnalysis, agentColor, lineWidth, showInfoPanel, enableLogging);
		}

		public Indicators.ThreeAgent_v1 ThreeAgent_v1(ISeries<double> input , int volumePeriod, double surgeThreshold, double flowSensitivity, double alertThreshold, bool enableSurgeDetection, bool enableFlowAnalysis, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			return indicator.ThreeAgent_v1(input, volumePeriod, surgeThreshold, flowSensitivity, alertThreshold, enableSurgeDetection, enableFlowAnalysis, agentColor, lineWidth, showInfoPanel, enableLogging);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ThreeAgent_v1 ThreeAgent_v1(int volumePeriod, double surgeThreshold, double flowSensitivity, double alertThreshold, bool enableSurgeDetection, bool enableFlowAnalysis, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			return indicator.ThreeAgent_v1(Input, volumePeriod, surgeThreshold, flowSensitivity, alertThreshold, enableSurgeDetection, enableFlowAnalysis, agentColor, lineWidth, showInfoPanel, enableLogging);
		}

		public Indicators.ThreeAgent_v1 ThreeAgent_v1(ISeries<double> input , int volumePeriod, double surgeThreshold, double flowSensitivity, double alertThreshold, bool enableSurgeDetection, bool enableFlowAnalysis, Brush agentColor, int lineWidth, bool showInfoPanel, bool enableLogging)
		{
			return indicator.ThreeAgent_v1(input, volumePeriod, surgeThreshold, flowSensitivity, alertThreshold, enableSurgeDetection, enableFlowAnalysis, agentColor, lineWidth, showInfoPanel, enableLogging);
		}
	}
}

#endregion
