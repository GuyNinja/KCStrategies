// --- GrandMaster.cs ---
// Version 7.0 - Optimized from Trade Log Analysis (08-26)
// Key Changes:
// 1. ANALYSIS: Updated based on the performance log from 2025-07-10 to 2025-08-26.
// 2. BOTS: Enabled only the top 6 consistently profitable bots (Andean, PivotImpulse, Momo, Willy, Glider, Hooker).
// 3. TIME: Trading window adjusted to 09:30 - 11:30 EST to focus on the volatile morning session.
// 4. RISK/EXIT: Switched to a FixedStop strategy with a 2:1 Risk/Reward profile.
// 5. PARAMETERS: Set SL=45 and TP=90 based on MAE/MFE analysis of winning trades. BE Trigger set to 30 ticks.

#region Using declarations
using System;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript.Strategies.KCStrategies;
#endregion

//This namespace is used to hold Strategies and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies.KCStrategies
{
    public class GrandMaster : KCAlgoBase
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
                Description     = @"Optimized version based on the trade log analysis from 2025-08-26.";
                Name            = "GrandMaster";
                StrategyName    = "GrandMaster";
                StrategyVersion = "7.0 - Log Analysis 08-26";
                Credits         = "Derived from KCAlgoBase Framework";
                ChartType       = "MNQ, based on analysis";

                // --- 1. CORE ENGINE CONFIGURATION ---
                EnableAutoRegimeDetection = false; // Using one consistent ruleset
                EnableChopDetection       = true;  // Keep chop filter to avoid worst conditions

                // --- 2. TIME FILTER (Optimized from Log) ---
                Start   = DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
                End     = DateTime.Parse("11:30", System.Globalization.CultureInfo.InvariantCulture);
                
                Time2 = false; Time3 = false; Time4 = false; Time5 = false; Time6 = false;
                
                // --- 3. TRADE MANAGEMENT & EXIT STRATEGY (Optimized from Log) ---
                StopType             = StopManagementType.FixedStop;
                InitialStop          = 45;   // Tighter stop based on MAE of winning trades.
                
                PTType               = ProfitTargetType.Fixed;
                ProfitTarget         = 90;   // Creates a 2:1 R:R.
                
                BESetAuto            = true;
                BreakevenTriggerMode = BETriggerMode.FixedTicks;
                BETriggerTicks       = 30;   // Move to BE after a reasonable profit.
                BE_Offset            = 4;    // Secure 4 ticks on BE move.

                // --- 4. BOT ACTIVATION (Enabled top profitable bots from the log) ---
                
                // ✅ ENABLED BOTS
                EnableAndean            = true;
                EnablePivotImpulseBot   = true;
                EnableMomo              = true;
                EnableWilly             = true;
                EnableGliderBot         = true; // Test candidate, monitor performance
                EnableHooker            = true;

                // ❌ DISABLED BOTS (Unprofitable or poor performance in the log)
                EnableKingKhanh           = false;
                EnableCasher              = false;
                EnableJohny5              = false;
                EnableTrendArchitectBot   = false;
                EnableBalaBot             = false;
                EnableMomentumVmaBot 	  = false;
                EnableCoralBot            = false;
                EnableSuperRex            = false;
                EnableReaperBot           = false;
                EnableEngulfingReversalBot= false;
                EnableStochasticsBot      = false;
                EnableTSSuperTrendBot     = false;
                EnableMagicTrendy         = false;
                EnableT3FilterBot         = false;
                EnableZombie9Bot          = false;
                EnableSmaCompositeBot     = false;
                EnableRangeFilterBot      = false;
                EnablePSARBot             = false;
                EnableTefaProBot          = false;
                EnableSuperTrendBot       = false;
                EnableTTMSqueezeBot       = false;
                EnableORBBot              = false;
                EnableSessionBreakerBot   = false;
                EnableBollingerBot        = false;
                EnableKeltnerBot          = false;
                EnableRsiBot              = false;
                EnableSessionFaderBot     = false;
                EnableZigZagBot           = false;
                EnableSwingStructureBot   = false;
                EnableSmartMoneyBot       = false;
                EnableEngulfingContinuationBot = false;
				EnableChaser			  = false;
				EnableMomentumExtremesBot = false;
            }
        }

        protected override void OnBarUpdate()
        {
            // Call the base class's OnBarUpdate first to handle core logic.
            base.OnBarUpdate();

            // No custom OnBarUpdate logic is needed for this version.
        }
    }
}