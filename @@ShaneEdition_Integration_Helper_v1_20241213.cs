//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// ShaneEdition_Integration_Helper_v1_20241213.cs - System Integration Coordinator
//
#region Using declarations
using System;
using System.Collections.Generic;
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
    /// ShaneEdition Integration Helper - Connects Control Window with Agents and Systems
    /// Provides centralized coordination between all ShaneEdition components
    /// Date: December 13, 2024
    /// </summary>
    public class ShaneEditionIntegration_v1 : Indicator
    {
        #region Variables - Coordination Hub
        // System state tracking
        private bool isInitialized = false;
        private Dictionary<string, bool> systemStates = new Dictionary<string, bool>();
        private Dictionary<string, object> systemInstances = new Dictionary<string, object>();
        
        // Agent references (when available)
        private OneAgent_v1 agent1;
        private TwoAgent_v1 agent2;
        private ThreeAgent_v1 agent3;
        private EarlyWarningSystem_v1 earlyWarning;
        
        // Integration status
        private int connectedAgents = 0;
        private bool earlyWarningConnected = false;
        private string systemStatus = "Initializing";
        
        // Coordination variables
        private double consensusSignal = 0; // Combined agent signal
        private double systemConfidence = 0; // Overall system confidence
        private string coordinatedDecision = "No Decision";
        
        // Performance tracking
        private DateTime lastUpdate = DateTime.MinValue;
        private int totalSignals = 0;
        private int consensusSignals = 0;
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"ShaneEdition Integration Helper - System Coordination Hub";
                Name = "ShaneEditionIntegration_v1_20241213";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;
                DrawOnPricePanel = false;
                
                // Integration settings
                EnableAutoCoordination = true;
                ConsensusThreshold = 0.6;
                MinAgentsRequired = 2;
                ShowIntegrationPanel = true;
                EnableLogging = false;
                
                // Display settings
                IntegrationColor = Brushes.Purple;
                LineWidth = 2;
                
                // Add plots for coordination output
                AddPlot(new Stroke(Brushes.Purple, 2), PlotStyle.Line, "ConsensusSignal");
                AddPlot(new Stroke(Brushes.Violet, 1), PlotStyle.Line, "SystemConfidence");
                
                InitializeSystemStates();
            }
            else if (State == State.Configure)
            {
                // Attempt to connect to available systems
                ConnectToSystems();
            }
            else if (State == State.DataLoaded)
            {
                isInitialized = true;
                systemStatus = "Active";
                if (EnableLogging)
                    Print("ShaneEdition Integration Hub initialized");
            }
        }

        protected override void OnBarUpdate()
        {
            if (!isInitialized || CurrentBar < 1) return;

            if (EnableAutoCoordination)
            {
                CoordinateAgentSignals();
                UpdateSystemConfidence();
                MakeCoordinatedDecision();
                UpdateDisplay();
            }
        }

        #region System Connection Methods
        private void InitializeSystemStates()
        {
            systemStates["NewsDetection"] = false;
            systemStates["SystemOverwatch"] = false;
            systemStates["VolumeAlerts"] = false;
            systemStates["ChatbotSystem"] = false;
            systemStates["1Agent"] = false;
            systemStates["2Agent"] = false;
            systemStates["3Agent"] = false;
            systemStates["EarlyWarning"] = false;
        }

        private void ConnectToSystems()
        {
            try
            {
                // This is a placeholder for actual system connections
                // In real implementation, this would use NinjaTrader's indicator referencing
                
                // For now, we'll track connection attempts
                Print("ShaneEdition Integration: Attempting to connect to systems...");
                
                // Simulate connection discovery
                connectedAgents = 0;
                earlyWarningConnected = false;
                
                UpdateSystemStatus();
            }
            catch (Exception ex)
            {
                Print($"ShaneEdition Integration: Connection error - {ex.Message}");
            }
        }

        private void UpdateSystemStatus()
        {
            systemStatus = $"Connected: {connectedAgents} agents";
            if (earlyWarningConnected)
                systemStatus += ", Early Warning";
        }
        #endregion

        #region Agent Coordination Methods
        private void CoordinateAgentSignals()
        {
            if (connectedAgents < MinAgentsRequired) return;

            List<double> agentSignals = new List<double>();
            List<double> agentConfidences = new List<double>();
            
            // Collect signals from connected agents
            // Note: In real implementation, these would be actual agent references
            
            // For now, we'll simulate coordination logic
            if (agentSignals.Count >= MinAgentsRequired)
            {
                // Calculate weighted consensus
                consensusSignal = CalculateConsensus(agentSignals, agentConfidences);
                totalSignals++;
                
                if (Math.Abs(consensusSignal) >= ConsensusThreshold)
                {
                    consensusSignals++;
                }
            }
        }

        private double CalculateConsensus(List<double> signals, List<double> confidences)
        {
            if (signals.Count == 0) return 0;

            double weightedSum = 0;
            double totalWeight = 0;

            for (int i = 0; i < signals.Count; i++)
            {
                double weight = confidences.Count > i ? confidences[i] / 100.0 : 1.0;
                weightedSum += signals[i] * weight;
                totalWeight += weight;
            }

            return totalWeight > 0 ? weightedSum / totalWeight : 0;
        }

        private void UpdateSystemConfidence()
        {
            // Calculate overall system confidence based on:
            // 1. Number of connected agents
            // 2. Agreement between agents
            // 3. System health metrics
            
            double baseConfidence = (double)connectedAgents / 10.0 * 100; // 10 total planned agents
            
            // Boost confidence when agents agree
            if (totalSignals > 0)
            {
                double consensusRate = (double)consensusSignals / totalSignals;
                systemConfidence = Math.Min(100, baseConfidence + (consensusRate * 30));
            }
            else
            {
                systemConfidence = baseConfidence;
            }
        }

        private void MakeCoordinatedDecision()
        {
            if (Math.Abs(consensusSignal) >= ConsensusThreshold && systemConfidence > 50)
            {
                coordinatedDecision = consensusSignal > 0 ? "BULLISH CONSENSUS" :
                                     consensusSignal < 0 ? "BEARISH CONSENSUS" : "NEUTRAL";
                
                if (EnableLogging)
                {
                    Print($"ShaneEdition Decision: {coordinatedDecision} " +
                          $"(Signal: {consensusSignal:F2}, Confidence: {systemConfidence:F0}%)");
                }
            }
            else
            {
                coordinatedDecision = "INSUFFICIENT CONSENSUS";
            }
        }
        #endregion

        #region System Control Interface
        /// <summary>
        /// Enable/Disable specific system components (called from Control Window)
        /// </summary>
        public void SetSystemState(string systemName, bool enabled)
        {
            if (systemStates.ContainsKey(systemName))
            {
                systemStates[systemName] = enabled;
                
                // Handle specific system enable/disable logic
                switch (systemName)
                {
                    case "NewsDetection":
                        if (earlyWarningConnected && earlyWarning != null)
                        {
                            earlyWarning.SetNewsDetection(enabled);
                        }
                        break;
                        
                    case "VolumeAlerts":
                        // Enable/disable volume alert processing
                        break;
                        
                    // Add other system controls as needed
                }
                
                if (EnableLogging)
                    Print($"ShaneEdition: {systemName} {(enabled ? "ENABLED" : "DISABLED")}");
            }
        }

        /// <summary>
        /// Get current system status summary
        /// </summary>
        public string GetSystemSummary()
        {
            return $"Status: {systemStatus}\n" +
                   $"Agents: {connectedAgents}\n" +
                   $"Signals: {totalSignals}\n" +
                   $"Consensus: {consensusSignals}\n" +
                   $"Decision: {coordinatedDecision}\n" +
                   $"Confidence: {systemConfidence:F0}%";
        }

        /// <summary>
        /// Get current consensus signal
        /// </summary>
        public double GetConsensusSignal() => consensusSignal;

        /// <summary>
        /// Get system confidence level
        /// </summary>
        public double GetSystemConfidence() => systemConfidence;

        /// <summary>
        /// Force system reconnection attempt
        /// </summary>
        public void ReconnectSystems()
        {
            ConnectToSystems();
        }
        #endregion

        #region Display Methods
        private void UpdateDisplay()
        {
            // Update plots
            Values[0][0] = consensusSignal * 50 + 50; // Scale -1,1 to 0,100
            Values[1][0] = systemConfidence;
            
            if (!ShowIntegrationPanel) return;
            
            // Build integration status display
            string displayText = $"🔗 ShaneEdition Integration\n" +
                               $"Status: {systemStatus}\n" +
                               $"Consensus: {consensusSignal:F2}\n" +
                               $"Confidence: {systemConfidence:F0}%\n" +
                               $"Decision: {coordinatedDecision}\n" +
                               $"Signals: {totalSignals} ({consensusSignals} consensus)";
            
            // Add system states
            displayText += "\n\nSystems:";
            foreach (var system in systemStates)
            {
                string status = system.Value ? "✓" : "✗";
                displayText += $"\n{status} {system.Key}";
            }
            
            // Choose color based on system health
            Brush textColor = systemConfidence > 75 ? Brushes.LimeGreen :
                             systemConfidence > 50 ? Brushes.Yellow :
                             systemConfidence > 25 ? Brushes.Orange : Brushes.Red;
            
            Draw.TextFixed(this, "IntegrationInfo", displayText, TextPosition.BottomRight, 
                          Brushes.White, new SimpleFont("Arial", 9), Brushes.Black, 
                          new SolidColorBrush(Color.FromArgb(200, 50, 25, 75)), 50);
        }
        #endregion

        #region Properties
        [NinjaScriptProperty]
        [Display(Name="Enable Auto Coordination", Description="Enable automatic agent coordination", Order=1, GroupName="Integration")]
        public bool EnableAutoCoordination { get; set; }

        [NinjaScriptProperty]
        [Range(0.3, 0.9)]
        [Display(Name="Consensus Threshold", Description="Minimum signal strength for consensus", Order=2, GroupName="Integration")]
        public double ConsensusThreshold { get; set; }

        [NinjaScriptProperty]
        [Range(1, 5)]
        [Display(Name="Min Agents Required", Description="Minimum agents needed for consensus", Order=3, GroupName="Integration")]
        public int MinAgentsRequired { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Integration Panel", Description="Display integration status panel", Order=4, GroupName="Display")]
        public bool ShowIntegrationPanel { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Integration Color", Description="Color for integration displays", Order=5, GroupName="Display")]
        public Brush IntegrationColor { get; set; }

        [NinjaScriptProperty]
        [Range(1, 5)]
        [Display(Name="Line Width", Description="Width of integration lines", Order=6, GroupName="Display")]
        public int LineWidth { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable Logging", Description="Enable debug logging", Order=7, GroupName="Debug")]
        public bool EnableLogging { get; set; }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ShaneEditionIntegration_v1[] cacheShaneEditionIntegration_v1;
		public ShaneEditionIntegration_v1 ShaneEditionIntegration_v1(bool enableAutoCoordination, double consensusThreshold, int minAgentsRequired, bool showIntegrationPanel, Brush integrationColor, int lineWidth, bool enableLogging)
		{
			return ShaneEditionIntegration_v1(Input, enableAutoCoordination, consensusThreshold, minAgentsRequired, showIntegrationPanel, integrationColor, lineWidth, enableLogging);
		}

		public ShaneEditionIntegration_v1 ShaneEditionIntegration_v1(ISeries<double> input, bool enableAutoCoordination, double consensusThreshold, int minAgentsRequired, bool showIntegrationPanel, Brush integrationColor, int lineWidth, bool enableLogging)
		{
			if (cacheShaneEditionIntegration_v1 != null)
				for (int idx = 0; idx < cacheShaneEditionIntegration_v1.Length; idx++)
					if (cacheShaneEditionIntegration_v1[idx] != null && cacheShaneEditionIntegration_v1[idx].EnableAutoCoordination == enableAutoCoordination && cacheShaneEditionIntegration_v1[idx].ConsensusThreshold == consensusThreshold && cacheShaneEditionIntegration_v1[idx].MinAgentsRequired == minAgentsRequired && cacheShaneEditionIntegration_v1[idx].ShowIntegrationPanel == showIntegrationPanel && cacheShaneEditionIntegration_v1[idx].IntegrationColor == integrationColor && cacheShaneEditionIntegration_v1[idx].LineWidth == lineWidth && cacheShaneEditionIntegration_v1[idx].EnableLogging == enableLogging && cacheShaneEditionIntegration_v1[idx].EqualsInput(input))
						return cacheShaneEditionIntegration_v1[idx];
			return CacheIndicator<ShaneEditionIntegration_v1>(new ShaneEditionIntegration_v1(){ EnableAutoCoordination = enableAutoCoordination, ConsensusThreshold = consensusThreshold, MinAgentsRequired = minAgentsRequired, ShowIntegrationPanel = showIntegrationPanel, IntegrationColor = integrationColor, LineWidth = lineWidth, EnableLogging = enableLogging }, input, ref cacheShaneEditionIntegration_v1);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ShaneEditionIntegration_v1 ShaneEditionIntegration_v1(bool enableAutoCoordination, double consensusThreshold, int minAgentsRequired, bool showIntegrationPanel, Brush integrationColor, int lineWidth, bool enableLogging)
		{
			return indicator.ShaneEditionIntegration_v1(Input, enableAutoCoordination, consensusThreshold, minAgentsRequired, showIntegrationPanel, integrationColor, lineWidth, enableLogging);
		}

		public Indicators.ShaneEditionIntegration_v1 ShaneEditionIntegration_v1(ISeries<double> input , bool enableAutoCoordination, double consensusThreshold, int minAgentsRequired, bool showIntegrationPanel, Brush integrationColor, int lineWidth, bool enableLogging)
		{
			return indicator.ShaneEditionIntegration_v1(input, enableAutoCoordination, consensusThreshold, minAgentsRequired, showIntegrationPanel, integrationColor, lineWidth, enableLogging);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ShaneEditionIntegration_v1 ShaneEditionIntegration_v1(bool enableAutoCoordination, double consensusThreshold, int minAgentsRequired, bool showIntegrationPanel, Brush integrationColor, int lineWidth, bool enableLogging)
		{
			return indicator.ShaneEditionIntegration_v1(Input, enableAutoCoordination, consensusThreshold, minAgentsRequired, showIntegrationPanel, integrationColor, lineWidth, enableLogging);
		}

		public Indicators.ShaneEditionIntegration_v1 ShaneEditionIntegration_v1(ISeries<double> input , bool enableAutoCoordination, double consensusThreshold, int minAgentsRequired, bool showIntegrationPanel, Brush integrationColor, int lineWidth, bool enableLogging)
		{
			return indicator.ShaneEditionIntegration_v1(input, enableAutoCoordination, consensusThreshold, minAgentsRequired, showIntegrationPanel, integrationColor, lineWidth, enableLogging);
		}
	}
}

#endregion
