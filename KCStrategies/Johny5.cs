#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators; // Needed for JBSignal1
using NinjaTrader.NinjaScript.DrawingTools;
// Ensure the namespace for KCAlgoBase is correct if it's different
using NinjaTrader.NinjaScript.Strategies.KCStrategies; // Assuming KCAlgoBase is in this namespace
#endregion

namespace NinjaTrader.NinjaScript.Strategies.KCStrategies // Or your preferred strategy namespace
{
    public class Johny5 : KCAlgoBase // Inherit from your KCAlgoBase
    {
        private JBSignal JBSignal1;

        // Parameters for JBSignal1 - these will override JBSignal1's defaults if set here
        // You can expose them as strategy parameters if you want to tune them from the UI
        private int macdFast = 12;
        private int macdSlow = 26;
        private int macdSmooth = 9;
        private int williamsRPer = 21;
        private int williamsREMAPer = 13;
        private double almaFastLen = 19;
        private double almaSlowLen = 20;

        private bool currentJBBuySignal = false;
        private bool currentJBSellSignal = false;

        protected override void OnStateChange()
        {
            base.OnStateChange(); // Important to call the base class's OnStateChange

            if (State == State.SetDefaults)
            {
                Description = @"Strategy using JBSignal1 indicator for entries, managed by KCAlgoBase.";
                Name = "Johny5 v.1.0.0";
				
                // You can override other defaults from KCAlgoBase here if needed
                // e.g., DefaultOrderType = OrderType.Market;
                InitialStop = 109;
                ProfitTarget = 129;

                // Default JBSignal1 Parameters (can be overridden by strategy parameters below if you add them)
                MacdFast = 12;
                MacdSlow = 26;
                MacdSmooth = 9;
                WilliamsRPeriod = 21;
                WilliamsREMAPeriod = 13;
                AlmaFastLength = 19;
                AlmaSlowLength = 20;
            }
            else if (State == State.DataLoaded)
            {
				InitializeIndicators();
            }
        }

        // This method is called by KCAlgoBase during its OnBarUpdate
        protected override void InitializeIndicators()
        {
            // Initialize indicators specific to this derived strategy
            // The base.InitializeIndicators() from KCAlgoBase will also be called via base.OnStateChange() if structured that way
            // or you can call it explicitly if needed: base.InitializeIndicators();
            
            JBSignal1 = JBSignal(MacdFast, MacdSlow, MacdSmooth, WilliamsRPeriod, WilliamsREMAPeriod, AlmaFastLength, AlmaSlowLength);
            
            // Optional: Add JBSignal1 to the chart for visualization
            // Note: JBSignal1 itself draws arrows. If you only want its ALMA lines,
            // you might modify JBSignal1 or create a version that only plots lines.
            if (JBSignal1 != null)
            {
                AddChartIndicator(JBSignal1);
            }
        }
// In JBSignalTrader.cs

        protected override void OnBarUpdate()
        {
            // --- Initial Readiness Checks ---
            if (JBSignal1 == null) 
            {
                if (State >= State.Historical) base.OnBarUpdate();
                return;
            }

            int requiredBarsForMACD = MacdSlow + MacdSmooth -1; 
            int requiredBarsForWR = Math.Max(WilliamsRPeriod, WilliamsREMAPeriod);
            int requiredBarsForAlma = (int)Math.Ceiling(Math.Max(AlmaFastLength, AlmaSlowLength));
            int requiredBarsForJBSignalItself = Math.Max(requiredBarsForMACD, Math.Max(requiredBarsForWR, requiredBarsForAlma));
            int trulyRequiredBars = Math.Max(BarsRequiredToTrade, requiredBarsForJBSignalItself);

            if (CurrentBar < trulyRequiredBars)
            {
                if (State >= State.Historical) base.OnBarUpdate();
                return; 
            }

            // --- Get Indicator Values ---
            MACD macdCurrent = MACD(Input, MacdFast, MacdSlow, MacdSmooth);
            if (!macdCurrent.IsValidDataPoint(1)) { if (State >= State.Historical) base.OnBarUpdate(); return; }
            double macdDiff = macdCurrent.Diff[0];
            double macdDiffprev = macdCurrent.Diff[1];
            
            WilliamsREMA wrCurrent = WilliamsREMA(Input, WilliamsRPeriod, WilliamsREMAPeriod);
            if (wrCurrent.Plots == null || wrCurrent.Plots.Length < 2 || !wrCurrent.Values[0].IsValidDataPoint(0) || !wrCurrent.Values[1].IsValidDataPoint(0)) 
            {
                 if (State >= State.Historical) base.OnBarUpdate();
                 return;
            }
            double williamsValue = wrCurrent.Values[0][0];
            double emaOfWilliamsR = wrCurrent.Values[1][0]; 

            if (!JBSignal1.Values[0].IsValidDataPoint(0) || !JBSignal1.Values[1].IsValidDataPoint(0)) 
            {
                if (State >= State.Historical) base.OnBarUpdate();
                return;
            }
            double almaValueFast = JBSignal1.Values[0][0]; // Fast ALMA from JBSignal's first plot
            double almaValueSlow = JBSignal1.Values[1][0]; // Slow ALMA from JBSignal's second plot
            
            currentJBBuySignal = false;
            currentJBSellSignal = false;

            // --- Original JBSignal Confluence Logic ---
            bool originalBuyConfluence = williamsValue > emaOfWilliamsR 
                                        && macdDiff > 0 
                                        && macdDiff > macdDiffprev 
                                        && Close[0] > almaValueFast 
                                        && almaValueFast > almaValueSlow 
                                        && Close[0] > Open[0];

            bool originalSellConfluence = williamsValue < emaOfWilliamsR 
                                         && macdDiff < 0 
                                         && macdDiff < macdDiffprev 
                                         && Close[0] < almaValueFast 
                                         && almaValueFast < almaValueSlow 
                                         && Close[0] < Open[0];

            // --- New Refined ALMA Crossover Logic ---
            bool almaBullishAlignment = almaValueFast > almaValueSlow;
            bool almaBearishAlignment = almaValueFast < almaValueSlow;

            // For long: Price (Close) crosses above the Fast ALMA AND Fast ALMA is above Slow ALMA
            bool refinedAlmaCrossUp = CrossAbove(Close, JBSignal1.Values[0], 1) && almaBullishAlignment;
            
            // For short: Price (Close) crosses below the Fast ALMA AND Fast ALMA is below Slow ALMA
            bool refinedAlmaCrossDown = CrossBelow(Close, JBSignal1.Values[0], 1) && almaBearishAlignment;

            // --- Combine Signals ---
            // A buy signal is EITHER the original confluence OR the refined bullish ALMA crossover
            // Adding bar confirmation (Close > Open) to the ALMA cross signal as well.
            if (originalBuyConfluence || (refinedAlmaCrossUp && Close[0] > Open[0]))
            {
                currentJBBuySignal = true;
                if (originalBuyConfluence) PrintOnce($"JBSig_BuyConfluence_{CurrentBar}", $"{Time[0]}: JBSignal original buy confluence MET.");
                if (refinedAlmaCrossUp && Close[0] > Open[0]) PrintOnce($"JBSig_BuyRefinedAlmaCross_{CurrentBar}", $"{Time[0]}: JBSignal Refined ALMA cross-up buy MET (Close over Fast, Fast > Slow).");
            }
            
            // A sell signal is EITHER the original confluence OR the refined bearish ALMA crossover
            // Adding bar confirmation (Close < Open) to the ALMA cross signal.
            if (originalSellConfluence || (refinedAlmaCrossDown && Close[0] < Open[0]))
            {
                currentJBSellSignal = true;
                if (originalSellConfluence) PrintOnce($"JBSig_SellConfluence_{CurrentBar}", $"{Time[0]}: JBSignal original sell confluence MET.");
                if (refinedAlmaCrossDown && Close[0] < Open[0]) PrintOnce($"JBSig_SellRefinedAlmaCross_{CurrentBar}", $"{Time[0]}: JBSignal Refined ALMA cross-down sell MET (Close under Fast, Fast < Slow).");
            }
            
            if (State >= State.Historical)
                base.OnBarUpdate();
        }
		
        protected override bool ValidateEntryLong()
        {
            // The base.OnBarUpdate() will call this.
            // Here, we use the signal generated from JBSignal1's logic.
            // Add any additional filters specific to this strategy if needed.
            if (currentJBBuySignal)
            {
                Print($"{Time[0]}: JBSignalTrader ValidateEntryLong: TRUE (currentJBBuySignal is true)");
                // Reset signal after consumption to avoid re-entry on the same signal continuously
                // unless KCAlgoBase's entry logic (e.g., isFlat check) handles this.
                // For safety, let's make it a one-time trigger per signal generation.
                // currentJBBuySignal = false; // Moved this reset to ProcessLongEntry in base, after entry attempt.
                return true;
            }
            return false;
        }

        protected override bool ValidateEntryShort()
        {
            // The base.OnBarUpdate() will call this.
            if (currentJBSellSignal)
            {
                Print($"{Time[0]}: JBSignalTrader ValidateEntryShort: TRUE (currentJBSellSignal is true)");
                // currentJBSellSignal = false; // Moved this reset to ProcessShortEntry in base
                return true;
            }
            return false;
        }

        // Optional: Override other methods from KCAlgoBase if specific behavior is needed for this strategy
        // For example, if this strategy needs different default stop/target calculations than the base.
        // protected override void SetInitialStopLossParameters() { ... }
        // protected override void SetInitialProfitTargetParameters() { ... }


        #region Strategy Parameters for JBSignal1
        // These parameters allow tuning JBSignal1 from the strategy UI

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="MACD Fast", Description="MACD Fast Period", Order=100, GroupName="Strategy Parameters - JBSignal1")]
        public int MacdFast
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="MACD Slow", Description="MACD Slow Period", Order=101, GroupName="Strategy Parameters - JBSignal1")]
        public int MacdSlow
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="MACD Smooth", Description="MACD Smoothing Period", Order=102, GroupName="Strategy Parameters - JBSignal1")]
        public int MacdSmooth
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Williams %R Period", Description="Period for Williams %R", Order=103, GroupName="Strategy Parameters - JBSignal1")]
        public int WilliamsRPeriod
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Williams %R EMA Period", Description="EMA Period for Williams %R", Order=104, GroupName="Strategy Parameters - JBSignal1")]
        public int WilliamsREMAPeriod
        { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, double.MaxValue)] // ALMA length can be double
        [Display(Name="Fast ALMA Length", Description="Window size for the Fast ALMA", Order=105, GroupName="Strategy Parameters - JBSignal1")]
        public double AlmaFastLength
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, double.MaxValue)]
        [Display(Name="Slow ALMA Length", Description="Window size for the Slow ALMA", Order=106, GroupName="Strategy Parameters - JBSignal1")]
        public double AlmaSlowLength
        { get; set; }

        #endregion
    }
}