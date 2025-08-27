// --- REFACTORED CODE (CORRECTED STRUCTURE) ---
// Version 6.2.18-ATM
// Key Changes for this Fix:
// 1. CORRECTED INHERITANCE: The main strategy class `TrendArchitect` now correctly inherits from `ATMAlgoBase`, not the old `KCAlgoBase`.
// 2. REMOVED INVALID CODE: Deleted the `TrendArchitectBot` class from this file, as all bots are now self-contained within the `ATMAlgoBase` framework. Also removed the local `botTrendArchitect` property and the `OnStateChange -> DataLoaded` logic, as these are now handled by the base class.
// 3. LOGIC SIMPLIFICATION: The `OnStateChange` method now only sets the default values for the bots that should be enabled for this specific child strategy.

#region Using declarations
using System;
using NinjaTrader.NinjaScript.Strategies.KCStrategies;
#endregion

namespace NinjaTrader.NinjaScript.Strategies.KCStrategies
{
    public class TrendArchitect : KCAlgoBase
    {			
        public override string DisplayName { get { return Name; } }

        protected override void OnStateChange()
        {
            base.OnStateChange();
            
            if (State == State.SetDefaults)
            {
				Description		= @"A strategy focused on using the TrendArchitect bot as its primary signal source.";
                Name            = "TrendArchitect";
                StrategyName    = "TrendArchitect";
                StrategyVersion = "6.2.18 Aug 2025";
				Credits			= "Strategy by Khanh Nguyen, built on the ATMAlgoBase Framework";
				ChartType		= "Tbars 28";	                
                
                // === CORE ENGINE CONFIGURATION ===
                EnableAutoRegimeDetection = true;
                EnableChopDetection       = true;
				EnableConfluenceScoring   = true;
				MinConfluenceScore        = 90;
				
                // === TIME FILTER ===
                Start = DateTime.Parse("08:00", System.Globalization.CultureInfo.InvariantCulture);
				End	= DateTime.Parse("12:00", System.Globalization.CultureInfo.InvariantCulture);
                Time2 = false; Time3 = false; Time4 = false; Time5 = false; Time6 = false;

                // === BOT CONFIGURATION: Enable ONLY the TrendArchitect bot ===
                EnableTrendArchitectBot = true; 
				ShowTrendArchitectPlot 	= true; 
				
                // --- Disable all other bots ---
				EnableKingKhanh				= false;
                EnableHooker              	= false;
                EnableSmartMoneyBot       	= false; 
				EnablePivotty 				= false;
                EnableWilly               	= false;
                EnableEngulfingReversalBot	= false;
				EnableCoralBot 				= false;
				EnableT3FilterBot 			= false;
				EnableSuperRex 				= false; 
				EnableSwingStructureBot		= false; 
                EnableCasher              	= false;
                EnableZigZagBot             = false;
                EnableGliderBot           	= false;
                EnableMomo                	= false;
                EnableSessionFaderBot     	= false;
                EnableAndean              	= false;
                EnableBalaBot             	= false;
                EnableStochasticsBot      	= false; 
                EnableSuperTrendBot 		= false; 
				EnableJohny5 				= false; 
				EnableTSSuperTrendBot 		= false;
                EnableMagicTrendy 			= false; 
				EnableTefaProBot 			= false;
                EnablePSARBot 				= false; 
				EnableRangeFilterBot 		= false; 
				EnableSmaCompositeBot 		= false;
                EnableZombie9Bot 			= false; 
                EnableORBBot 				= false; 
				EnableSessionBreakerBot 	= false;
                EnableBollingerBot 			= false; 
				EnableKeltnerBot 			= false;
                EnableRsiBot 				= false; 
				EnableTTMSqueezeBot 		= false; 
				EnableEngulfingContinuationBot = false;
            }
        }
    }
}