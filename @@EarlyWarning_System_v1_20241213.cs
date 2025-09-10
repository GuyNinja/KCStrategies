//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// EarlyWarning_System_v1_20241213.cs - News & Volume Early Warning Detection
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;
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
    /// Early Warning System - News Detection & Volume Surge Alerts
    /// Minimal, efficient early warning system for market events
    /// Integrates with 3Agent volume detection
    /// Date: December 13, 2024
    /// </summary>
    public class EarlyWarningSystem_v1 : Indicator
    {
        #region Variables - Minimal & Lean
        // Core warning system variables
        private bool isInitialized = false;
        private DateTime lastNewsCheck = DateTime.MinValue;
        private DateTime lastVolumeAlert = DateTime.MinValue;
        
        // Volume integration (connects to 3Agent)
        private ThreeAgent_v1 volumeAgent;
        private bool volumeAgentConnected = false;
        
        // News detection variables
        private List<NewsEvent> recentNews = new List<NewsEvent>();
        private int newsCheckIntervalMinutes = 5;
        private bool newsDetectionActive = false;
        
        // Alert system
        private List<EarlyWarning> activeWarnings = new List<EarlyWarning>();
        private int maxWarningsToKeep = 10;
        private bool soundAlertsEnabled = true;
        
        // Warning levels
        private enum WarningLevel { Low, Medium, High, Critical }
        private enum WarningType { VolumeSpike, NewsEvent, PriceAction, SystemAlert }
        
        // Current warning state
        private WarningLevel currentWarningLevel = WarningLevel.Low;
        private string warningMessage = "System Active";
        private int warningCount = 0;
        
        // Performance tracking
        private DateTime systemStartTime;
        private int totalAlertsGenerated = 0;
        #endregion

        #region News Event and Warning Classes
        private class NewsEvent
        {
            public DateTime Timestamp { get; set; }
            public string Title { get; set; }
            public string Summary { get; set; }
            public double ImpactScore { get; set; } // 0-100
            public string[] Keywords { get; set; }
            public bool IsHighImpact => ImpactScore > 70;
        }

        private class EarlyWarning
        {
            public DateTime Timestamp { get; set; }
            public WarningType Type { get; set; }
            public WarningLevel Level { get; set; }
            public string Message { get; set; }
            public double Confidence { get; set; }
            public bool IsActive { get; set; }
            public string Source { get; set; }
        }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Early Warning System v1 - News Detection & Volume Alert Integration";
                Name = "EarlyWarningSystem_v1_20241213";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false; // Separate window
                DisplayInDataBox = true;
                DrawOnPricePanel = false;
                
                // Minimal parameters following user preference
                VolumeThreshold = 2.0;
                NewsCheckInterval = 5;
                MaxNewsAge = 60;
                AlertThreshold = 75;
                
                // System settings
                EnableNewsDetection = false; // Disabled by default (controlled by main window)
                EnableVolumeAlerts = true;
                EnableSoundAlerts = true;
                ShowWarningPanel = true;
                
                // Display settings
                WarningColor = Brushes.Red;
                LineWidth = 2;
                
                // Add plots for warning levels
                AddPlot(new Stroke(Brushes.Yellow, 2), PlotStyle.Line, "WarningLevel");
                AddPlot(new Stroke(Brushes.Orange, 1), PlotStyle.Line, "AlertCount");
                
                systemStartTime = DateTime.Now;
            }
            else if (State == State.Configure)
            {
                // Try to connect to 3Agent for volume integration
                ConnectToVolumeAgent();
            }
            else if (State == State.DataLoaded)
            {
                isInitialized = true;
                if (EnableNewsDetection)
                    InitializeNewsSystem();
                
                Print($"EarlyWarning System initialized - News: {EnableNewsDetection}, Volume: {EnableVolumeAlerts}");
            }
        }

        protected override void OnBarUpdate()
        {
            if (!isInitialized || CurrentBar < 1) return;

            // Core Early Warning Logic
            CheckVolumeAlerts();
            CheckNewsUpdates();
            ProcessActiveWarnings();
            UpdateWarningLevel();
            UpdateDisplay();
        }

        #region Core Warning Methods
        private void CheckVolumeAlerts()
        {
            if (!EnableVolumeAlerts) return;
            
            // Integrate with 3Agent if available
            if (volumeAgentConnected && volumeAgent != null)
            {
                try
                {
                    bool hasVolumeSurge = volumeAgent.HasVolumeSurge();
                    double surgeMultiplier = volumeAgent.GetSurgeMultiplier();
                    string volumeCondition = volumeAgent.GetVolumeCondition();
                    
                    if (hasVolumeSurge && surgeMultiplier >= VolumeThreshold)
                    {
                        CreateVolumeWarning(surgeMultiplier, volumeCondition);
                    }
                }
                catch (Exception ex)
                {
                    // Handle connection issues gracefully
                    volumeAgentConnected = false;
                    if (EnableSoundAlerts)
                        Print($"EarlyWarning: Lost connection to 3Agent - {ex.Message}");
                }
            }
            else
            {
                // Fallback: Simple volume detection if 3Agent not available
                CheckSimpleVolumeAlert();
            }
        }

        private void CheckSimpleVolumeAlert()
        {
            // Basic volume surge detection as fallback
            if (CurrentBar < 20) return;
            
            double avgVolume = 0;
            for (int i = 1; i <= 20; i++)
            {
                avgVolume += Volume[i];
            }
            avgVolume /= 20;
            
            double volumeRatio = Volume[0] / avgVolume;
            
            if (volumeRatio >= VolumeThreshold)
            {
                CreateVolumeWarning(volumeRatio, $"Volume Surge {volumeRatio:F1}x");
            }
        }

        private void CheckNewsUpdates()
        {
            if (!EnableNewsDetection) return;
            
            // Check if it's time for news update
            if (DateTime.Now.Subtract(lastNewsCheck).TotalMinutes < NewsCheckInterval)
                return;
            
            lastNewsCheck = DateTime.Now;
            
            // Simulate news checking (in real implementation, this would call news APIs)
            CheckForMarketNews();
        }

        private void CheckForMarketNews()
        {
            // Placeholder for actual news API integration
            // In real implementation, this would:
            // 1. Call Alpha Vantage News API
            // 2. Parse financial news feeds
            // 3. Analyze sentiment and impact
            // 4. Generate warnings for high-impact news
            
            // For now, simulate occasional news events
            if (new Random().Next(100) < 5) // 5% chance per check
            {
                CreateNewsWarning("Market Event Detected", "Simulated news event for testing", 75.0);
            }
        }

        private void ProcessActiveWarnings()
        {
            // Remove expired warnings
            activeWarnings.RemoveAll(w => !w.IsActive || 
                DateTime.Now.Subtract(w.Timestamp).TotalMinutes > 30);
            
            // Limit warning history
            if (activeWarnings.Count > maxWarningsToKeep)
            {
                activeWarnings = activeWarnings
                    .OrderByDescending(w => w.Timestamp)
                    .Take(maxWarningsToKeep)
                    .ToList();
            }
        }

        private void UpdateWarningLevel()
        {
            // Calculate current warning level based on active warnings
            var recentWarnings = activeWarnings
                .Where(w => DateTime.Now.Subtract(w.Timestamp).TotalMinutes < 10)
                .ToList();
            
            if (!recentWarnings.Any())
            {
                currentWarningLevel = WarningLevel.Low;
                warningMessage = "System Normal";
            }
            else
            {
                var highestLevel = recentWarnings.Max(w => w.Level);
                currentWarningLevel = highestLevel;
                warningMessage = $"{recentWarnings.Count} Active Warning(s)";
            }
            
            warningCount = recentWarnings.Count;
        }
        #endregion

        #region Warning Creation Methods
        private void CreateVolumeWarning(double multiplier, string condition)
        {
            // Prevent duplicate alerts within 1 minute
            if (DateTime.Now.Subtract(lastVolumeAlert).TotalSeconds < 60)
                return;
            
            lastVolumeAlert = DateTime.Now;
            
            WarningLevel level = multiplier >= 3.0 ? WarningLevel.Critical :
                                multiplier >= 2.5 ? WarningLevel.High :
                                multiplier >= 2.0 ? WarningLevel.Medium : WarningLevel.Low;
            
            var warning = new EarlyWarning
            {
                Timestamp = DateTime.Now,
                Type = WarningType.VolumeSpike,
                Level = level,
                Message = $"Volume Surge: {multiplier:F1}x average - {condition}",
                Confidence = Math.Min(100, multiplier * 30),
                IsActive = true,
                Source = volumeAgentConnected ? "3Agent" : "Internal"
            };
            
            activeWarnings.Add(warning);
            totalAlertsGenerated++;
            
            // Alert output
            string alertMessage = $"🚨 VOLUME ALERT: {warning.Message}";
            Print(alertMessage);
            
            if (EnableSoundAlerts)
            {
                Alert("VolumeAlert", Priority.High, alertMessage, "", 10, Brushes.Red, Brushes.White);
            }
        }

        private void CreateNewsWarning(string title, string summary, double impact)
        {
            WarningLevel level = impact >= 80 ? WarningLevel.Critical :
                                impact >= 60 ? WarningLevel.High :
                                impact >= 40 ? WarningLevel.Medium : WarningLevel.Low;
            
            var warning = new EarlyWarning
            {
                Timestamp = DateTime.Now,
                Type = WarningType.NewsEvent,
                Level = level,
                Message = $"News: {title} (Impact: {impact:F0}%)",
                Confidence = impact,
                IsActive = true,
                Source = "NewsAPI"
            };
            
            activeWarnings.Add(warning);
            totalAlertsGenerated++;
            
            // Alert output
            string alertMessage = $"📰 NEWS ALERT: {warning.Message}";
            Print(alertMessage);
            
            if (EnableSoundAlerts && impact > 70)
            {
                Alert("NewsAlert", Priority.Medium, alertMessage, "", 8, Brushes.Orange, Brushes.White);
            }
        }
        #endregion

        #region Integration Methods
        private void ConnectToVolumeAgent()
        {
            try
            {
                // Try to find 3Agent instance
                // This is a simplified connection - real implementation would use proper NT8 indicator referencing
                volumeAgent = null; // Will be set up through proper NT8 indicator referencing
                volumeAgentConnected = false;
                
                // For now, we'll use fallback volume detection
                Print("EarlyWarning: Using internal volume detection (3Agent integration pending)");
            }
            catch (Exception ex)
            {
                Print($"EarlyWarning: Could not connect to 3Agent - {ex.Message}");
                volumeAgentConnected = false;
            }
        }

        private void InitializeNewsSystem()
        {
            // Initialize news detection system
            // In real implementation, this would set up API connections
            newsDetectionActive = true;
            Print("EarlyWarning: News detection system initialized");
        }
        #endregion

        #region Display Methods
        private void UpdateDisplay()
        {
            // Update plots
            Values[0][0] = (int)currentWarningLevel * 25; // Scale warning level to 0-100
            Values[1][0] = warningCount * 10; // Scale warning count
            
            if (!ShowWarningPanel) return;
            
            // Build warning display
            string displayText = $"🚨 Early Warning System\n" +
                               $"Status: {warningMessage}\n" +
                               $"Level: {currentWarningLevel}\n" +
                               $"Active: {warningCount}\n" +
                               $"Total: {totalAlertsGenerated}\n" +
                               $"Uptime: {DateTime.Now.Subtract(systemStartTime).TotalHours:F1}h";
            
            // Add recent warnings
            if (activeWarnings.Any())
            {
                displayText += "\n\nRecent Warnings:";
                var recent = activeWarnings
                    .OrderByDescending(w => w.Timestamp)
                    .Take(3);
                
                foreach (var warning in recent)
                {
                    string timeAgo = DateTime.Now.Subtract(warning.Timestamp).TotalMinutes < 1 ? 
                        "Now" : $"{DateTime.Now.Subtract(warning.Timestamp).TotalMinutes:F0}m ago";
                    displayText += $"\n• {warning.Type}: {timeAgo}";
                }
            }
            
            // Choose color based on warning level
            Brush textColor = currentWarningLevel == WarningLevel.Critical ? Brushes.Red :
                             currentWarningLevel == WarningLevel.High ? Brushes.Orange :
                             currentWarningLevel == WarningLevel.Medium ? Brushes.Yellow :
                             Brushes.LimeGreen;
            
            Draw.TextFixed(this, "EarlyWarningInfo", displayText, TextPosition.TopRight, 
                          Brushes.White, new SimpleFont("Arial", 9), Brushes.Black, 
                          new SolidColorBrush(Color.FromArgb(200, 25, 25, 25)), 50);
        }
        #endregion

        #region Properties
        [NinjaScriptProperty]
        [Range(1.5, 5.0)]
        [Display(Name="Volume Threshold", Description="Volume multiplier for alert generation", Order=1, GroupName="Early Warning")]
        public double VolumeThreshold { get; set; }

        [NinjaScriptProperty]
        [Range(1, 30)]
        [Display(Name="News Check Interval", Description="Minutes between news checks", Order=2, GroupName="Early Warning")]
        public int NewsCheckInterval { get; set; }

        [NinjaScriptProperty]
        [Range(15, 240)]
        [Display(Name="Max News Age", Description="Maximum age of news to consider (minutes)", Order=3, GroupName="Early Warning")]
        public int MaxNewsAge { get; set; }

        [NinjaScriptProperty]
        [Range(50, 95)]
        [Display(Name="Alert Threshold", Description="Minimum confidence for alerts", Order=4, GroupName="Early Warning")]
        public double AlertThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable News Detection", Description="Enable news monitoring (controlled by main window)", Order=5, GroupName="Systems")]
        public bool EnableNewsDetection { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable Volume Alerts", Description="Enable volume surge detection", Order=6, GroupName="Systems")]
        public bool EnableVolumeAlerts { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable Sound Alerts", Description="Enable audio notifications", Order=7, GroupName="Systems")]
        public bool EnableSoundAlerts { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Warning Panel", Description="Display warning information panel", Order=8, GroupName="Display")]
        public bool ShowWarningPanel { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Warning Color", Description="Color for warning displays", Order=9, GroupName="Display")]
        public Brush WarningColor { get; set; }

        [NinjaScriptProperty]
        [Range(1, 5)]
        [Display(Name="Line Width", Description="Width of warning indicator lines", Order=10, GroupName="Display")]
        public int LineWidth { get; set; }
        #endregion

        #region Public Interface Methods
        /// <summary>
        /// Get current warning level
        /// </summary>
        public WarningLevel GetWarningLevel() => currentWarningLevel;

        /// <summary>
        /// Get number of active warnings
        /// </summary>
        public int GetWarningCount() => warningCount;

        /// <summary>
        /// Get current warning message
        /// </summary>
        public string GetWarningMessage() => warningMessage;

        /// <summary>
        /// Enable/disable news detection (called from control window)
        /// </summary>
        public void SetNewsDetection(bool enabled)
        {
            EnableNewsDetection = enabled;
            if (enabled && !newsDetectionActive)
                InitializeNewsSystem();
            else if (!enabled)
                newsDetectionActive = false;
        }

        /// <summary>
        /// Get system performance summary
        /// </summary>
        public string GetSystemSummary()
        {
            return $"Warnings: {totalAlertsGenerated}, Active: {warningCount}, " +
                   $"Uptime: {DateTime.Now.Subtract(systemStartTime).TotalHours:F1}h";
        }

        /// <summary>
        /// Force a news check (for testing)
        /// </summary>
        public void ForceNewsCheck()
        {
            if (EnableNewsDetection)
            {
                lastNewsCheck = DateTime.MinValue;
                CheckNewsUpdates();
            }
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private EarlyWarningSystem_v1[] cacheEarlyWarningSystem_v1;
		public EarlyWarningSystem_v1 EarlyWarningSystem_v1(double volumeThreshold, int newsCheckInterval, int maxNewsAge, double alertThreshold, bool enableNewsDetection, bool enableVolumeAlerts, bool enableSoundAlerts, bool showWarningPanel, Brush warningColor, int lineWidth)
		{
			return EarlyWarningSystem_v1(Input, volumeThreshold, newsCheckInterval, maxNewsAge, alertThreshold, enableNewsDetection, enableVolumeAlerts, enableSoundAlerts, showWarningPanel, warningColor, lineWidth);
		}

		public EarlyWarningSystem_v1 EarlyWarningSystem_v1(ISeries<double> input, double volumeThreshold, int newsCheckInterval, int maxNewsAge, double alertThreshold, bool enableNewsDetection, bool enableVolumeAlerts, bool enableSoundAlerts, bool showWarningPanel, Brush warningColor, int lineWidth)
		{
			if (cacheEarlyWarningSystem_v1 != null)
				for (int idx = 0; idx < cacheEarlyWarningSystem_v1.Length; idx++)
					if (cacheEarlyWarningSystem_v1[idx] != null && cacheEarlyWarningSystem_v1[idx].VolumeThreshold == volumeThreshold && cacheEarlyWarningSystem_v1[idx].NewsCheckInterval == newsCheckInterval && cacheEarlyWarningSystem_v1[idx].MaxNewsAge == maxNewsAge && cacheEarlyWarningSystem_v1[idx].AlertThreshold == alertThreshold && cacheEarlyWarningSystem_v1[idx].EnableNewsDetection == enableNewsDetection && cacheEarlyWarningSystem_v1[idx].EnableVolumeAlerts == enableVolumeAlerts && cacheEarlyWarningSystem_v1[idx].EnableSoundAlerts == enableSoundAlerts && cacheEarlyWarningSystem_v1[idx].ShowWarningPanel == showWarningPanel && cacheEarlyWarningSystem_v1[idx].WarningColor == warningColor && cacheEarlyWarningSystem_v1[idx].LineWidth == lineWidth && cacheEarlyWarningSystem_v1[idx].EqualsInput(input))
						return cacheEarlyWarningSystem_v1[idx];
			return CacheIndicator<EarlyWarningSystem_v1>(new EarlyWarningSystem_v1(){ VolumeThreshold = volumeThreshold, NewsCheckInterval = newsCheckInterval, MaxNewsAge = maxNewsAge, AlertThreshold = alertThreshold, EnableNewsDetection = enableNewsDetection, EnableVolumeAlerts = enableVolumeAlerts, EnableSoundAlerts = enableSoundAlerts, ShowWarningPanel = showWarningPanel, WarningColor = warningColor, LineWidth = lineWidth }, input, ref cacheEarlyWarningSystem_v1);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.EarlyWarningSystem_v1 EarlyWarningSystem_v1(double volumeThreshold, int newsCheckInterval, int maxNewsAge, double alertThreshold, bool enableNewsDetection, bool enableVolumeAlerts, bool enableSoundAlerts, bool showWarningPanel, Brush warningColor, int lineWidth)
		{
			return indicator.EarlyWarningSystem_v1(Input, volumeThreshold, newsCheckInterval, maxNewsAge, alertThreshold, enableNewsDetection, enableVolumeAlerts, enableSoundAlerts, showWarningPanel, warningColor, lineWidth);
		}

		public Indicators.EarlyWarningSystem_v1 EarlyWarningSystem_v1(ISeries<double> input , double volumeThreshold, int newsCheckInterval, int maxNewsAge, double alertThreshold, bool enableNewsDetection, bool enableVolumeAlerts, bool enableSoundAlerts, bool showWarningPanel, Brush warningColor, int lineWidth)
		{
			return indicator.EarlyWarningSystem_v1(input, volumeThreshold, newsCheckInterval, maxNewsAge, alertThreshold, enableNewsDetection, enableVolumeAlerts, enableSoundAlerts, showWarningPanel, warningColor, lineWidth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.EarlyWarningSystem_v1 EarlyWarningSystem_v1(double volumeThreshold, int newsCheckInterval, int maxNewsAge, double alertThreshold, bool enableNewsDetection, bool enableVolumeAlerts, bool enableSoundAlerts, bool showWarningPanel, Brush warningColor, int lineWidth)
		{
			return indicator.EarlyWarningSystem_v1(Input, volumeThreshold, newsCheckInterval, maxNewsAge, alertThreshold, enableNewsDetection, enableVolumeAlerts, enableSoundAlerts, showWarningPanel, warningColor, lineWidth);
		}

		public Indicators.EarlyWarningSystem_v1 EarlyWarningSystem_v1(ISeries<double> input , double volumeThreshold, int newsCheckInterval, int maxNewsAge, double alertThreshold, bool enableNewsDetection, bool enableVolumeAlerts, bool enableSoundAlerts, bool showWarningPanel, Brush warningColor, int lineWidth)
		{
			return indicator.EarlyWarningSystem_v1(input, volumeThreshold, newsCheckInterval, maxNewsAge, alertThreshold, enableNewsDetection, enableVolumeAlerts, enableSoundAlerts, showWarningPanel, warningColor, lineWidth);
		}
	}
}

#endregion
