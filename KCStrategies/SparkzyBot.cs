#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies.KCStrategies;
#endregion

//This namespace is required to hold Strategies and must not be changed.
namespace NinjaTrader.NinjaScript.Strategies.KCStrategies
{
    public class SparkzyBot : KCAlgoBase
    {
        #region Strategy-Specific Indicators & Variables
        // These indicators are now declared and initialized entirely within this strategy file,
        // making it completely independent of the KCAlgoBase bot framework.
        private WilliamsR sparkzyWpr;
        private MACD      sparkzyMacd;
        private CoralTrendIndicatorLB sparkzyTrendFilter;
        #endregion

        public override string DisplayName { get { return Name; } }

        protected override void OnStateChange()
        {
            // The derived class logic runs FIRST, then the base class logic.
            if (State == State.SetDefaults)
            {
                // --- Strategy Identity ---
                Description     = @"A trend and momentum confluence strategy based on the 'sparkzy' logic, using the KCAlgoBase framework for managed execution.";
                Name            = "SparkzyBot";
                StrategyName    = "SparkzyBot";
                StrategyVersion = "2.0 - Standalone Logic";
                Credits         = "Original logic by sparkzy, ported to KCAlgoBase";
                ChartType       = "Any";

                // --- 1. SET FRAMEWORK DEFAULTS FOR SPARKZY LOGIC ---
                // Configure the KCAlgoBase framework to handle our orders.
                StopType          = StopManagementType.ATRTrail;
                PTType            = ProfitTargetType.RiskRewardRatio;
                RiskRewardRatio   = 2.0;
                AtrPeriod         = 14;
                atrMultiplier     = 1.5; // This is the Stop Loss ATR Multiplier
                InitialStop       = 50;  // Fallback for ATR
                ProfitTarget      = 100; // Fallback for RR
                Contracts         = 1;   // Default to 1 contract
                
                // --- 2. CRITICAL FIX: DISABLE ALL FRAMEWORK BOTS ---
                // This prevents the KCAlgoBase from trying to initialize any of its own bot indicators,
                // which was the source of the "Object reference not set" errors.
                EnableTrendBots   = false;
                EnableRangeBots   = false;
                EnableBreakoutBots= false;

                // --- 3. DEFINE SPARKZY'S OWN PARAMETERS ---
                // These parameters are now local to this strategy and will not conflict with the base framework.
                Sparkzy_WprLength        = 21;
                Sparkzy_MacdFast         = 5;
                Sparkzy_MacdSlow         = 8;
                Sparkzy_MacdSmooth       = 5;
                Sparkzy_WprOverbought    = -20;
                Sparkzy_WprOversold      = -80;
                Sparkzy_CoralPeriod      = 34;
                Sparkzy_CoralConstantD   = 0.85;
            }

            // Call the base class's OnStateChange AFTER setting our defaults.
            base.OnStateChange();

            if (State == State.DataLoaded)
            {
                // Initialize our own, local indicators using our own parameters.
                sparkzyWpr         = WilliamsR(Sparkzy_WprLength);
                sparkzyMacd        = MACD(Sparkzy_MacdFast, Sparkzy_MacdSlow, Sparkzy_MacdSmooth);
                sparkzyTrendFilter = CoralTrendIndicatorLB(Sparkzy_CoralPeriod, Sparkzy_CoralConstantD, false, false, PlotStyle.Line, 2);
                
                if (IsVisible)
                {
                    AddChartIndicator(sparkzyTrendFilter);
                }
            }
        }

        /// <summary>
        /// This method is called by the KCAlgoBase framework on every bar.
        /// We override it to implement the unique entry logic of the Sparkzy strategy.
        /// </summary>
        protected override void CheckForCustomSignals()
        {
            // Check if our local indicators are ready.
            if (sparkzyWpr == null || !sparkzyWpr.IsValidDataPoint(1) || 
                sparkzyMacd == null || !sparkzyMacd.IsValidDataPoint(1) || 
                sparkzyTrendFilter == null || !sparkzyTrendFilter.IsValidDataPoint(1))
                return;

            // --- Define Sparkzy's Confluence Conditions ---

            // 1. Trend Condition
            bool trendIsUp = sparkzyTrendFilter[0] > sparkzyTrendFilter[1];
            bool trendIsDown = sparkzyTrendFilter[0] < sparkzyTrendFilter[1];

            // 2. Momentum Condition (using MACD Histogram, which is the 'Diff' series in NT8)
            bool momentumIsUp = sparkzyMacd.Diff[0] > 0;
            bool momentumIsDown = sparkzyMacd.Diff[0] < 0;

            // 3. Exhaustion/Burst Filter (using Williams %R)
            bool isApproachingOverbought = sparkzyWpr[0] > Sparkzy_WprOverbought;
            bool isApproachingOversold = sparkzyWpr[0] < Sparkzy_WprOversold;

            // --- Generate Final Long Signal ---
            if (trendIsUp && momentumIsUp && isApproachingOverbought)
            {
                longSignal = true;
                a_lastSignalSource = "Sparkzy";
            }

            // --- Generate Final Short Signal ---
            if (trendIsDown && momentumIsDown && isApproachingOversold)
            {
                shortSignal = true;
                a_lastSignalSource = "Sparkzy";
            }
        }

        #region Sparkzy Indicator Parameters
        // These parameters are now part of this strategy directly, making it self-contained.

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Sparkzy WPR Length", Order = 1, GroupName = "Sparkzy Parameters")]
        public int Sparkzy_WprLength { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Sparkzy WPR Overbought", Order = 2, GroupName = "Sparkzy Parameters")]
        public double Sparkzy_WprOverbought { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Sparkzy WPR Oversold", Order = 3, GroupName = "Sparkzy Parameters")]
        public double Sparkzy_WprOversold { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Sparkzy MACD Fast", Order = 4, GroupName = "Sparkzy Parameters")]
        public int Sparkzy_MacdFast { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Sparkzy MACD Slow", Order = 5, GroupName = "Sparkzy Parameters")]
        public int Sparkzy_MacdSlow { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Sparkzy MACD Smooth", Order = 6, GroupName = "Sparkzy Parameters")]
        public int Sparkzy_MacdSmooth { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Sparkzy Coral Period", Order = 7, GroupName = "Sparkzy Parameters")]
        public int Sparkzy_CoralPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Sparkzy Coral Constant D", Order = 8, GroupName = "Sparkzy Parameters")]
        public double Sparkzy_CoralConstantD { get; set; }

        #endregion
    }
}