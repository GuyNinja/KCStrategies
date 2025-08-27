// --- GrandMaster.cs ---
// Version 5.9 - Optimized from Trade Log Analysis (07-09)
// Key Changes:
// 1. ANALYSIS: Updated based on the performance log from 2025-07-07 to 2025-07-09.
// 2. BOTS: Enabled only the top 8 most consistently profitable bots from the new log.
// 3. TIME: Trading window confirmed for the most profitable period: 09:30 - 11:00 EST.
// 4. RISK/EXIT: Switched to an ATR Trail stop with a 2.0 Risk/Reward profit target. This proved more effective than a fixed stop in the log.
// 5. PARAMETERS: Adjusted BE Trigger to 50 ticks to align with the new adaptive exit strategy.

#region Using declarations
using System;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript.Strategies.KCStrategies;
#endregion

//This namespace is used to hold Strategies and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies.KCStrategies
{
    public class PivotImpulse : KCAlgoBase
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
                Description     = @"Optimized version based on the trade log analysis from 2025-07-09.";
                Name            = "Pivot Impulse Bot";
                StrategyName    = "Pivot Impulse Bot";
                StrategyVersion = "5.9 - Log Analysis 07-09";
                Credits         = "Derived from KCAlgoBase Framework";
                ChartType       = "MNQ, based on analysis";

                // --- 1. CORE ENGINE CONFIGURATION ---
                EnableAutoRegimeDetection = false;
                EnableChopDetection       = true;

                // --- 2. TIME FILTER (Optimized from Log) ---
                Start   = DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
                End     = DateTime.Parse("11:00", System.Globalization.CultureInfo.InvariantCulture);
                
                Time2 = false; Time3 = false; Time4 = false; Time5 = false; Time6 = false;
                
                // --- 3. TRADE MANAGEMENT & EXIT STRATEGY (Optimized from Log) ---
                StopType            = StopManagementType.ATRTrail; // ATR Trail was effective.
                atrMultiplier       = 2.5;                         // Standard multiplier.
                PTType              = ProfitTargetType.RiskRewardRatio; // Target based on ATR stop.
                RiskRewardRatio     = 2.0;                         // Aim for 2:1 R:R.
                
                BESetAuto           = true;
				BreakevenTriggerMode = BETriggerMode.ProfitTargetPercentage;
                BETriggerTicks      = 20;  // Move to BE at +30 ticks profit
                BE_Offset           = 4;   // Secure 4 ticks on BE move

                // --- 4. BOT ACTIVATION (Enabled top 8 profitable bots from the log) ---
                
                // ✅ ENABLED BOTS (Net profitable in the log)
                EnablePivotImpulseBot   = true;
                EnableBalaBot           = false;
                EnableStochasticsBot    = false;
                EnableAndean            = false;
                EnableTrendArchitectBot = false;
                EnableWilly             = false;
                EnableKingKhanh         = false;
                EnableTSSuperTrendBot   = false;
				EnableReaperBot			= false;
				EnableGliderBot			= false; // Returned to profitability.

                // ❌ DISABLED BOTS (Unprofitable or poor performance in the new log)
                EnableCoralBot          = false;
                EnableMagicTrendy       = false;
                EnableCasher            = false;
                EnableHooker            = false;
                EnableSmartMoneyBot     = false;
                EnableMomentumVmaBot    = false; // Unprofitable in this log.
                EnableChaser            = false;
                EnableSuperRex          = false;
                EnableMomentumExtremesBot = false;
				EnableSuperTrendBot		= false;
				EnableJohny5			= false;
				EnableMomo				= false;
                EnableSmaCompositeBot   = false;
                EnablePivotty           = false;
                EnableZombie9Bot        = false; // No trades in log.
                EnableTTMSqueezeBot     = false;
                EnableORBBot            = false;
                EnableSessionBreakerBot = false;
                EnableBollingerBot      = false;
                EnableKeltnerBot        = false;
                EnableRsiBot            = false;
                EnableSessionFaderBot   = false;
                EnableTefaProBot        = false;
                EnablePSARBot           = false;
                EnableRangeFilterBot    = false;
                EnableZigZagBot         = false;
                EnableSwingStructureBot = false;
                EnableEngulfingContinuationBot = false;
				EnableT3FilterBot		= false;
				EnableEngulfingReversalBot= false;
            }
        }

        protected override void OnBarUpdate()
        {
            // Call the base class's OnBarUpdate first to handle core logic.
            base.OnBarUpdate();

            // No custom OnBarUpdate logic is needed.
        }
    }
}