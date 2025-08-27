// --- DaMaster.cs ---
// Version 1.8 - Rollback
// Key Changes:
// 1. ROLLBACK: Removed experimental bot properties to revert to a stable configuration.
// 2. CLEANUP: Removed properties for obsolete bots.

#region Using declarations
using System;
using NinjaTrader.Cbi; // Required for MarketPosition
using NinjaTrader.NinjaScript.Strategies.KCStrategies;
#endregion

//This namespace is used to hold Strategies and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies.KCStrategies
{
    public class DaMaster : KCAlgoBase
    {
        // We override the base DisplayName to give our strategy a unique name in the NinjaTrader UI.
        public override string DisplayName { get { return Name; } }

        protected override void OnStateChange()
        {
            // First, call the base class's OnStateChange to set up the framework.
            base.OnStateChange();
            
            if (State == State.SetDefaults)
            {
                // --- Strategy Identity ---
                Description     = @"A data collection and optimization strategy. All bots and all time sessions are enabled to find the best parameters for the 'GrandMaster' strategy.";
                Name            = "DaMaster"; // This name will appear in the NinjaTrader strategy list
                StrategyName    = "DaMaster";
                StrategyVersion = "1.8 - Data Collection Mode";
                Credits         = "Derived from KCAlgoBase Framework";
                ChartType       = "MNQ, based on analysis";

                // --- 1. CORE ENGINE CONFIGURATION ---
                EnableAutoRegimeDetection = true; // Use the regime detector to switch between TP and Trailing stops
                EnableChopDetection       = true; // Keep the chop filter active to avoid unfavorable conditions

                // --- 2. TIME FILTER (Set to ALL HOURS) ---
                Time6 = true; // This is the 24-hour session timer in the framework
                // Disable all other specific time windows.
                Time2 = false; Time3 = false; Time4 = false; Time5 = false; 
                
                // --- 3. TRADE MANAGEMENT & EXIT STRATEGY (Same as GrandMaster) ---
                // These are the base parameters that the dynamic logic in OnBarUpdate will use.
                InitialStop         = 73;  // Universal 50-tick initial Stop-Loss
                ProfitTarget        = 120; // Default 120-tick Take Profit (for Trending markets)
                BESetAuto           = true;
				BreakevenTriggerMode = BETriggerMode.ProfitTargetPercentage;
                BETriggerTicks      = 20;  // Move to BE at +30 ticks profit
                BE_Offset           = 4;   // Secure 4 ticks on BE move
                
                // Set the default StopType to HighLowTrail. This will be used for Ranging markets.
                StopType = StopManagementType.HighLowTrail; 

                // --- 4. BOT ACTIVATION (ALL BOTS ENABLED) ---
                // Every bot is turned on to gather performance data across all market conditions.
                EnableMomentumExtremesBot = true;
                EnableMomentumVmaBot      = true;
				EnablePivotImpulseBot	  = true;
                EnableT3FilterBot         = true;
                EnableCasher              = true;
				EnableChaser			  = true;
                EnableMomo                = true;
                EnableEngulfingReversalBot= true;
                EnableReaperBot           = true;
                EnableStochasticsBot      = true;
                EnableHooker              = true;
                EnableCoralBot            = true;
                EnableSuperTrendBot       = true;
                EnableAndean              = true;
                EnableGliderBot           = true;
                EnableWilly               = true;
                EnableBalaBot             = true;
                EnableMagicTrendy         = true;
                EnableKingKhanh           = true;
                EnablePivotty             = true;
                EnableSmartMoneyBot       = true;
                EnableZombie9Bot          = true;
                EnableTTMSqueezeBot       = true;
                EnableORBBot              = true;
                EnableSessionBreakerBot   = true;
                EnableBollingerBot        = true;
                EnableKeltnerBot          = true;
                EnableRsiBot              = true;
                EnableSessionFaderBot     = true;
                EnableTSSuperTrendBot     = true;
                EnableTefaProBot          = true;
                EnablePSARBot             = true;
                EnableRangeFilterBot      = true;
                EnableSmaCompositeBot     = true;
                EnableEngulfingContinuationBot = true;
                EnableZigZagBot           = true;
                EnableSwingStructureBot   = true;
            }
        }

        protected override void OnBarUpdate()
        {
            // Call the base class's OnBarUpdate first to handle core logic.
            base.OnBarUpdate();
        }
    }
}