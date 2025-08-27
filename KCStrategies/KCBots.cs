// --- KCAlgoBase_Bots.cs ---
// Version 6.5.8 - Stable Rollback
// Key Changes:
// 1. CLEANUP: Commented out the PivotImpulseBot logic since its dependencies have been removed from the base framework.

#region Using declarations
using System;
using NinjaTrader.Cbi; // Required for MarketPosition
using NinjaTrader.NinjaScript.Strategies.KCStrategies;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Indicators.AlgoTrades;
#endregion

//This namespace is used to hold Strategies and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies.KCStrategies
{
    #region Bot Framework Interface and Enum
    public enum SignalDirection { NoSignal, Long, Short }

    public interface ISignalBot
    {
        string Name { get; }
        KCAlgoBase.MarketRegime Regime { get; }
        void Initialize(KCAlgoBase strategy);
        SignalDirection CheckSignal(int barsAgo);
    }
    #endregion

    #region --- UNIVERSAL BOTS ---
    public class KingKhanhBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "KingKhanh";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Undefined;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableKingKhanh) return SignalDirection.NoSignal;
			if (barsAgo != 0) return SignalDirection.NoSignal;

            if (s.botRegChanPlus == null || !s.botRegChanPlus.IsValidDataPoint(2))
                return SignalDirection.NoSignal;

            bool volatilityExpansion = (s.botRegChanPlus.UpperStdDevBand[0] - s.botRegChanPlus.LowerStdDevBand[0]) > (s.botRegChanPlus.UpperStdDevBand[1] - s.botRegChanPlus.LowerStdDevBand[1]);
            bool longBreakout = volatilityExpansion && s.CrossAbove(s.Close, s.botRegChanPlus.UpperStdDevBand, 1);
            bool shortBreakout = volatilityExpansion && s.CrossBelow(s.Close, s.botRegChanPlus.LowerStdDevBand, 1);

            bool touchedLowerPrev = s.Low[1] <= s.botRegChanPlus.LowerStdDevBand[1];
            bool closesInChannel = s.Close[0] > s.botRegChanPlus.LowerStdDevBand[0];
            bool bullishReversal = touchedLowerPrev && closesInChannel;
            
            bool touchedUpperPrev = s.High[1] >= s.botRegChanPlus.UpperStdDevBand[1];
            bool closesInChannelShort = s.Close[0] < s.botRegChanPlus.UpperStdDevBand[0];
            bool bearishReversal = touchedUpperPrev && closesInChannelShort;

            bool regChanUp = (s.botRegChanPlus.UpperStdDevBand[0] > s.botRegChanPlus.UpperStdDevBand[1]) 
                            && (s.botRegChanPlus.UpperStdDevBand[1] <= s.botRegChanPlus.UpperStdDevBand[2]);
            bool regChanDown = (s.botRegChanPlus.LowerStdDevBand[0] < s.botRegChanPlus.LowerStdDevBand[1]) 
                            && (s.botRegChanPlus.LowerStdDevBand[1] >= s.botRegChanPlus.LowerStdDevBand[2]);

            switch (s.currentRegime)
            {
                case KCAlgoBase.MarketRegime.Trending:
                    if (regChanUp && bullishReversal) return SignalDirection.Long;
                    if (regChanDown && bearishReversal) return SignalDirection.Short;
                    break;
                case KCAlgoBase.MarketRegime.Ranging:
                    if (bullishReversal) return SignalDirection.Long;
                    if (bearishReversal) return SignalDirection.Short;
                    break;
                case KCAlgoBase.MarketRegime.Breakout:
                    if (longBreakout) return SignalDirection.Long;
                    if (shortBreakout) return SignalDirection.Short;
                    break;
            }
            return SignalDirection.NoSignal;
        }
    }
	
	public class PivotImpulseBot : ISignalBot
	{
	    private KCAlgoBase s;
	    public string Name => "PivotImpulse";
	    public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Undefined;
	
	    public void Initialize(KCAlgoBase strategy)
	    {
	        s = strategy;
	    }
	
	    public SignalDirection CheckSignal(int barsAgo)
	    {
	        if (!s.EnablePivotImpulseBot) return SignalDirection.NoSignal;
	        if (barsAgo != 0 || s.botPivotImpulse == null)
	            return SignalDirection.NoSignal;
	
	        // Signal on the change of impulse direction
	        bool turnedUp = s.botPivotImpulse.IsImpulseUp[0] && !s.botPivotImpulse.IsImpulseUp[1];
	        if (turnedUp)
	            return SignalDirection.Long;
	
	        bool turnedDown = !s.botPivotImpulse.IsImpulseUp[0] && s.botPivotImpulse.IsImpulseUp[1];
	        if (turnedDown)
	            return SignalDirection.Short;
	
	        return SignalDirection.NoSignal;
	    }
	}
	
	/* // DISABLED
	public class PivotImpulseBot : ISignalBot
    {
        private KCAlgoBase s;
        private Swing swing; // This bot manages its own private Swing indicator instance.

        public string Name => "PivotImpulse";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Undefined; // RECLASSIFIED

        public void Initialize(KCAlgoBase strategy) 
        { 
            s = strategy; 
            // Initialize its own swing indicator using the parameters from the strategy UI to avoid conflicts.
            if (s.EnablePivotImpulseBot)
                swing = s.Swing(s.PIS_SwingStrength); 
        }

        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnablePivotImpulseBot || barsAgo != 0 || swing == null || s.botPivotImpulseLines == null || s.CurrentBar < (s.PIS_SwingStrength * 2))
                return SignalDirection.NoSignal;

            double slope0 = CalculatePivotImpulse(0);
            double slope1 = CalculatePivotImpulse(1);

            if (slope0 == double.MaxValue || slope1 == double.MaxValue)
                return SignalDirection.NoSignal;

            if (s.botPivotImpulseLines.IsImpulseUp[0] && !s.botPivotImpulseLines.IsImpulseUp[1])
                return SignalDirection.Long; 
            if (s.botPivotImpulseLines.IsImpulseUp[0] && slope0 > slope1 + s.PIS_SlopeThreshold)
                return SignalDirection.Long;
            
            if (!s.botPivotImpulseLines.IsImpulseUp[0] && s.botPivotImpulseLines.IsImpulseUp[1])
                return SignalDirection.Short;
            if (!s.botPivotImpulseLines.IsImpulseUp[0] && slope0 < slope1 - s.PIS_SlopeThreshold)
                return SignalDirection.Short;

            return SignalDirection.NoSignal;
        }

        private double CalculatePivotImpulse(int barsAgo)
        {
            int barsAgoHigh = swing.SwingHighBar(barsAgo, 1, 200);
            int barsAgoLow  = swing.SwingLowBar(barsAgo, 1, 200);

            if (barsAgoHigh < 0 || barsAgoLow < 0)
                return double.MaxValue;

            double startPrice, endPrice;
            int startBarsAgo;

            if (barsAgoLow < barsAgoHigh) // Up impulse
            {
                startBarsAgo = barsAgo + barsAgoLow;
                startPrice = s.Low[startBarsAgo];
                endPrice = s.High[barsAgo];
            }
            else // Down impulse
            {
                startBarsAgo = barsAgo + barsAgoHigh;
                startPrice = s.High[startBarsAgo];
                endPrice = s.Low[barsAgo];
            }

            double deltaBars = startBarsAgo - barsAgo;
            if (deltaBars <= 0) return double.MaxValue;

            return (endPrice - startPrice) / deltaBars;
        }
    }
	*/
    #endregion

    #region --- TREND BOTS ---
	
	public class MomentumExtremesBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "MomentumExtremes";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }

        public SignalDirection CheckSignal(int barsAgo)
        {
            if (!s.EnableMomentumExtremesBot) return SignalDirection.NoSignal;
			
            if (barsAgo != 0 || s.botMomentumExtremes == null || !s.botMomentumExtremes.IsValidDataPoint(2))
                return SignalDirection.NoSignal;

            bool longSignal = s.botMomentumExtremes.MED[0] > s.botMomentumExtremes.MED[1] 
                           && s.botMomentumExtremes.MED[1] <= s.botMomentumExtremes.MED[2];
            if (longSignal)
                return SignalDirection.Long;

            bool shortSignal = s.botMomentumExtremes.MED[0] < s.botMomentumExtremes.MED[1] 
                            && s.botMomentumExtremes.MED[1] >= s.botMomentumExtremes.MED[2];
            if (shortSignal)
                return SignalDirection.Short;

            return SignalDirection.NoSignal;
        }
    }
	
	public class ReaperBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Reaper";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }

        public SignalDirection CheckSignal(int barsAgo)
        {
            if (!s.EnableReaperBot) return SignalDirection.NoSignal;
            if (barsAgo != 0) return SignalDirection.NoSignal;

            if (!s.High.IsValidDataPoint(2) || !s.Low.IsValidDataPoint(2) || !s.Close.IsValidDataPoint(1))
                return SignalDirection.NoSignal;

            bool longCondition = s.High[0] > s.High[1] && s.High[1] <= s.High[2] && s.Close[0] > s.Close[1];
            if (longCondition)
                return SignalDirection.Long;

            bool shortCondition = s.Low[0] < s.Low[1] && s.Low[1] >= s.Low[2] && s.Close[0] < s.Close[1];
            if (shortCondition)
                return SignalDirection.Short;

            return SignalDirection.NoSignal;
        }
    }
	
	public class MomentumVmaBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "MomentumVmaDriver";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }

        public SignalDirection CheckSignal(int barsAgo)
        {
            if (!s.EnableMomentumVmaBot) return SignalDirection.NoSignal;
            if (barsAgo != 0 || s.botMomentumVmaDriver == null || !s.botMomentumVmaDriver.IsValidDataPoint(2))
                return SignalDirection.NoSignal;

            bool isTurningUp = s.botMomentumVmaDriver.MVD[0] > s.botMomentumVmaDriver.MVD[1]
                               && s.botMomentumVmaDriver.MVD[1] <= s.botMomentumVmaDriver.MVD[2];
            if (isTurningUp)
                return SignalDirection.Long;

            bool isTurningDown = s.botMomentumVmaDriver.MVD[0] < s.botMomentumVmaDriver.MVD[1]
                                 && s.botMomentumVmaDriver.MVD[1] >= s.botMomentumVmaDriver.MVD[2];
            if (isTurningDown)
                return SignalDirection.Short;

            return SignalDirection.NoSignal;
        }
    }
	
    public class SwingStructureBot : ISignalBot
	{
	    private KCAlgoBase s;
	    public string Name => "SwingStructure";
	    public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
	    public void Initialize(KCAlgoBase strategy) { s = strategy; }
	    
	    public SignalDirection CheckSignal(int barsAgo)
	    {
			if (!s.EnableSwingStructureBot) return SignalDirection.NoSignal;
	        if (barsAgo != 0 || s.botSwing == null) return SignalDirection.NoSignal;
	
	        if (s.IsTrendingUp())
	        {
	            int barsAgoOfRecentLow = s.botSwing.SwingLowBar(1, 1, 200);
	            int barsAgoOfPriorLow  = s.botSwing.SwingLowBar(1, 2, 200);

	            if (barsAgoOfRecentLow > 0 && barsAgoOfPriorLow > barsAgoOfRecentLow)
	            {
	                double recentLowPrice = s.botSwing.SwingLow[barsAgoOfRecentLow];
					double priorLowPrice  = s.botSwing.SwingLow[barsAgoOfPriorLow];

	                if (recentLowPrice > priorLowPrice)
	                {
	                    if (s.Close[0] > s.High[barsAgoOfRecentLow]) return SignalDirection.Long;
	                }
	            }
	        }
	
	        if (s.IsTrendingDown())
	        {
	            int barsAgoOfRecentHigh = s.botSwing.SwingHighBar(1, 1, 200);
	            int barsAgoOfPriorHigh  = s.botSwing.SwingHighBar(1, 2, 200);

	            if (barsAgoOfRecentHigh > 0 && barsAgoOfPriorHigh > barsAgoOfRecentHigh)
	            {
	                double recentHighPrice = s.botSwing.SwingHigh[barsAgoOfRecentHigh];
					double priorHighPrice  = s.botSwing.SwingHigh[barsAgoOfPriorHigh];
	                
	                if (recentHighPrice < priorHighPrice)
	                {
	                    if (s.Close[0] < s.Low[barsAgoOfRecentHigh]) return SignalDirection.Short;
	                }
	            }
	        }
	        return SignalDirection.NoSignal;
	    }
	}
	
    public class ZigZagBot : ISignalBot
	{
	    private KCAlgoBase s;
	    public string Name => "ZigZag";
	    public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
	    public void Initialize(KCAlgoBase strategy) { s = strategy; }
	    
	    public SignalDirection CheckSignal(int barsAgo)
	    {
			if (!s.EnableZigZagBot) return SignalDirection.NoSignal;
	        if (barsAgo != 0 || s.botZigZag == null || !s.botZigZag.IsValidDataPoint(1)) return SignalDirection.NoSignal;
	
	        if (s.IsTrendingUp())
	        {
	            int barsAgoOfRecentLow = s.botZigZag.LowBar(1, 1, 1);
	            if (barsAgoOfRecentLow == 1)
	            {
	                int barsAgoOfPriorLow = s.botZigZag.LowBar(2, 2, 200);
	                if (barsAgoOfPriorLow > 1 && s.botZigZag.ZigZagLow[1] > s.botZigZag.ZigZagLow[barsAgoOfPriorLow])
	                {
						return SignalDirection.Long;
	                }
	            }
	        }
	
	        if (s.IsTrendingDown())
	        {
	            int barsAgoOfRecentHigh = s.botZigZag.HighBar(1, 1, 1);
	            if (barsAgoOfRecentHigh == 1)
	            {
	                int barsAgoOfPriorHigh = s.botZigZag.HighBar(2, 2, 200);
	                if (barsAgoOfPriorHigh > 1 && s.botZigZag.ZigZagHigh[1] < s.botZigZag.ZigZagHigh[barsAgoOfPriorHigh])
					{
						return SignalDirection.Short;
	                }
	            }
	        }
	        return SignalDirection.NoSignal;
	    }
	}

	public class TrendArchitectBot : ISignalBot
	{
	    private KCAlgoBase s;
	    public string Name => "TrendArchitect";
	    public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
	    public void Initialize(KCAlgoBase strategy) { s = strategy; }
	    
	    public SignalDirection CheckSignal(int barsAgo)
	    {
			if (!s.EnableTrendArchitectBot) return SignalDirection.NoSignal;
	        if (barsAgo != 0 || s.botTrendArchitect == null) return SignalDirection.NoSignal;
	
	        double scoreThreshold = 0.65;
	
	        if (s.botTrendArchitect.BuySignalScore[0] >= scoreThreshold && s.botTrendArchitect.BuySignalScore[1] < scoreThreshold)
	            return SignalDirection.Long;
	            
	        if (s.botTrendArchitect.SellSignalScore[0] >= scoreThreshold && s.botTrendArchitect.SellSignalScore[1] < scoreThreshold)
	            return SignalDirection.Short;
	            
	        return SignalDirection.NoSignal;
	    }
	}

    // RE-ADDED: ChaserBot class definition
    public class ChaserBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Chaser";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableChaser) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botLinReg == null) return SignalDirection.NoSignal;

			bool slopeUp = s.botLinReg[0] - s.botLinReg[1] > s.LinRegSlopeThreshold;
			bool slopeDown = s.botLinReg[0] - s.botLinReg[1] < -s.LinRegSlopeThreshold;

            if (slopeUp && s.CrossAbove(s.Close, s.botLinReg, 1)) return SignalDirection.Long;
            if (slopeDown && s.CrossBelow(s.Close, s.botLinReg, 1)) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class HookerBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Hooker";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableHooker) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botHmaHooks == null) return SignalDirection.NoSignal;
            
            if (s.botHmaHooks.LongSignal[0] > 0) return SignalDirection.Long;
            if (s.botHmaHooks.ShortSignal[0] > 0) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }
    
    public class MomoBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Momo";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableMomo) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botMomentum == null) return SignalDirection.NoSignal;

            if (s.CrossAbove(s.botMomentum, s.MomoThreshold, 1)) return SignalDirection.Long;
            if (s.CrossBelow(s.botMomentum, -s.MomoThreshold, 1)) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }
    
    public class CoralBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Coral";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableCoralBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botCoral == null || !s.botCoral.IsValidDataPoint(2)) return SignalDirection.NoSignal;
			
            if (s.botCoral[0] > s.botCoral[1] && s.botCoral[1] <= s.botCoral[2]) return SignalDirection.Long;
            if (s.botCoral[0] < s.botCoral[1] && s.botCoral[1] >= s.botCoral[2]) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class SuperTrendBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "SuperTrend";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableSuperTrendBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botSuperSmoother == null) return SignalDirection.NoSignal;
			
            if (s.botSuperSmoother.Trend[0] == 1 && s.botSuperSmoother.Trend[1] != 1) return SignalDirection.Long;
            if (s.botSuperSmoother.Trend[0] == -1 && s.botSuperSmoother.Trend[1] != -1) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class Johny5Bot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Johny5";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableJohny5) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botJbSignal == null) return SignalDirection.NoSignal;
			
            if (s.botJbSignal.StrategySignal[0] == 1 && s.botJbSignal.StrategySignal[1] != 1) return SignalDirection.Long;
            if (s.botJbSignal.StrategySignal[0] == -1 && s.botJbSignal.StrategySignal[1] != -1) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class TSSuperTrendBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "TSSuperTrend";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableTSSuperTrendBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botTSSuperTrend == null) return SignalDirection.NoSignal;

            if (s.botTSSuperTrend.UpTrend[0] > 0 && s.botTSSuperTrend.UpTrend[1] <= 0) return SignalDirection.Long;
            if (s.botTSSuperTrend.DownTrend[0] > 0 && s.botTSSuperTrend.DownTrend[1] <= 0) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }
    
    public class MagicTrendyBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "MagicTrendy";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableMagicTrendy) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botTrendMagic == null) return SignalDirection.NoSignal;
			
            if (s.CrossAbove(s.Close, s.botTrendMagic.Trend, 1)) return SignalDirection.Long;
            if (s.CrossBelow(s.Close, s.botTrendMagic.Trend, 1)) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class T3FilterBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Trendy (T3)";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableT3FilterBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botT3Filter == null) return SignalDirection.NoSignal;
			
            if (s.CrossAbove(s.botT3Filter.Values[0], 0, 1)) return SignalDirection.Long;
            if (s.CrossBelow(s.botT3Filter.Values[1], 0, 1)) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }
    
    public class TefaProBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "TEFA Pro";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableTefaProBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botTefaPro == null) return SignalDirection.NoSignal;
			
            if (s.CrossAbove(s.botTefaPro.Values[1], 0, 1)) return SignalDirection.Long;
            if (s.CrossBelow(s.botTefaPro.Values[1], 0, 1)) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }
    
    public class PsarBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "PSAR";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnablePSARBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botPSAR == null) return SignalDirection.NoSignal;

            if (s.CrossAbove(s.Close, s.botPSAR, 1)) return SignalDirection.Long;
            if (s.CrossBelow(s.Close, s.botPSAR, 1)) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class RangeFilterBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "RangeFilter";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableRangeFilterBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botRangeFilter == null) return SignalDirection.NoSignal;
			
            if (s.botRangeFilter.ConditionIni[0] == 1 && s.botRangeFilter.ConditionIni[1] != 1) return SignalDirection.Long;
            if (s.botRangeFilter.ConditionIni[0] == -1 && s.botRangeFilter.ConditionIni[1] != -1) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class SmaCompositeBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "SMAComposite";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableSmaCompositeBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botSmaComposite == null) return SignalDirection.NoSignal;
			
            if (s.botSmaComposite.LongSignal[0] == 1 && s.botSmaComposite.LongSignal[1] != 1) return SignalDirection.Long;
            if (s.botSmaComposite.ShortSignal[0] == -1 && s.botSmaComposite.ShortSignal[1] != -1) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class Zombie9Bot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Zombie";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableZombie9Bot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botZombie9Macd == null || s.botZombie9Sma == null) return SignalDirection.NoSignal;
			
            if (s.Close[0] > s.botZombie9Sma[0] && s.CrossAbove(s.botZombie9Macd.LineChange, s.botZombie9Macd.AvgChange, 1)) return SignalDirection.Long;
            if (s.Close[0] < s.botZombie9Sma[0] && s.CrossBelow(s.botZombie9Macd.LineChange, s.botZombie9Macd.AvgChange, 1)) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class EngulfingContinuationBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "EngulfingConti";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Trending;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableEngulfingContinuationBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botEngulfing == null) return SignalDirection.NoSignal;

            if (s.botEngulfing.ContiOutsideBar[0] == 1) return SignalDirection.Long;
            if (s.botEngulfing.ContiOutsideBar[0] == -1) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    #endregion
    
    #region --- RANGE BOTS ---

    public class PivottyBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Pivotty";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Ranging;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnablePivotty) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botSvePivots == null || !s.botSvePivots.S1.IsValidDataPoint(1)) return SignalDirection.NoSignal;
			
            if (s.SvePivotsUseS1 && s.botSvePivots.S1[0] > 0 && s.Low[1] < s.botSvePivots.S1[1] && s.Close[0] > s.botSvePivots.S1[0]) return SignalDirection.Long;
            if (s.SvePivotsUseS2 && s.botSvePivots.S2[0] > 0 && s.Low[1] < s.botSvePivots.S2[1] && s.Close[0] > s.botSvePivots.S2[0]) return SignalDirection.Long;
            if (s.SvePivotsUseS3 && s.botSvePivots.S3[0] > 0 && s.Low[1] < s.botSvePivots.S3[1] && s.Close[0] > s.botSvePivots.S3[0]) return SignalDirection.Long;
            
			if (s.SvePivotsUseR1 && s.botSvePivots.R1[0] > 0 && s.High[1] > s.botSvePivots.R1[1] && s.Close[0] < s.botSvePivots.R1[0]) return SignalDirection.Short;
            if (s.SvePivotsUseR2 && s.botSvePivots.R2[0] > 0 && s.High[1] > s.botSvePivots.R2[1] && s.Close[0] < s.botSvePivots.R2[0]) return SignalDirection.Short;
            if (s.SvePivotsUseR3 && s.botSvePivots.R3[0] > 0 && s.High[1] > s.botSvePivots.R3[1] && s.Close[0] < s.botSvePivots.R3[0]) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }
    
    public class WillyBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Willy";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Ranging;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableWilly) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botWilly == null) return SignalDirection.NoSignal;
			
            if (s.CrossAbove(s.botWilly, s.wrDown, 1)) return SignalDirection.Long;
            if (s.CrossBelow(s.botWilly, s.wrUp, 1)) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class AndeanBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Andean";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Ranging;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableAndean) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.andeanOscillator == null) return SignalDirection.NoSignal;
			
            if (s.CrossAbove(s.andeanOscillator.BullishComponent, s.andeanOscillator.BearishComponent, 1)) return SignalDirection.Long;
            if (s.CrossBelow(s.andeanOscillator.BullishComponent, s.andeanOscillator.BearishComponent, 1)) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class CasherBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Casher";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Ranging;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableCasher) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botHiloBands == null) return SignalDirection.NoSignal;
			
            if (s.botHiloBands.MidlineUpSignal[0] == 1 && s.botHiloBands.MidlineUpSignal[1] != 1) return SignalDirection.Long;
            if (s.botHiloBands.MidlineDownSignal[0] == 1 && s.botHiloBands.MidlineDownSignal[1] != 1) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }
    
    public class StochasticsBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Stochastics";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Ranging;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableStochasticsBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botStochastics == null) return SignalDirection.NoSignal;
			
            if (s.CrossAbove(s.botStochastics.K, s.botStochastics.D, 1) && s.botStochastics.K[1] < 20) return SignalDirection.Long;
            if (s.CrossBelow(s.botStochastics.K, s.botStochastics.D, 1) && s.botStochastics.K[1] > 80) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }
    
    public class BalaBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Bala";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Ranging;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableBalaBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.Bala1 == null) return SignalDirection.NoSignal;

            if (s.Bala1.LCrosses2[0] != 0 && s.Bala1.LCrosses2[1] == 0) return SignalDirection.Long;
            if (s.Bala1.UCrosses2[0] != 0 && s.Bala1.UCrosses2[1] == 0) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }
    
    public class SuperRexBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "SuperRex";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Ranging;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableSuperRex) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botCmo == null) return SignalDirection.NoSignal;

            if (s.CrossAbove(s.botCmo, s.CmoOversoldLevel, 1)) return SignalDirection.Long;
            if (s.CrossBelow(s.botCmo, s.CmoOverboughtLevel, 1)) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }
    
    public class GliderBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Glider";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Ranging;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableGliderBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botSMI == null) return SignalDirection.NoSignal;

            if (s.CrossAbove(s.botSMI.SMIChange, s.botSMI.SMIEMA, 1)) return SignalDirection.Long;
            if (s.CrossBelow(s.botSMI.SMIChange, s.botSMI.SMIEMA, 1)) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }
    
    public class SmartMoneyBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "SmartMoney";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Ranging;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableSmartMoneyBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botMarketStructure == null) return SignalDirection.NoSignal;

            if (s.botMarketStructure.Signal[0] == 1 && s.botMarketStructure.Signal[1] != 1) return SignalDirection.Long;
            if (s.botMarketStructure.Signal[0] == -1 && s.botMarketStructure.Signal[1] != -1) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class EngulfingReversalBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "EngulfingRev";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Ranging;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableEngulfingReversalBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botEngulfing == null) return SignalDirection.NoSignal;
			
            if (s.botEngulfing.CurrentOutsideBar[0] == 1) return SignalDirection.Long;
            if (s.botEngulfing.CurrentOutsideBar[0] == -1) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class ORBReversalBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "ORB Reversal";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Ranging;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableORBBot || !s.ORBUseReversalSignals) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botORB == null) return SignalDirection.NoSignal;

            if (s.botORB.Signal[0] == 2 && s.botORB.Signal[1] != 2) return SignalDirection.Long;
            if (s.botORB.Signal[0] == -2 && s.botORB.Signal[1] != -2) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class RsiBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "RSI";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Ranging;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableRsiBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botRsi == null) return SignalDirection.NoSignal;

            if (s.CrossAbove(s.botRsi, s.RsiOversold, 1)) return SignalDirection.Long;
            if (s.CrossBelow(s.botRsi, s.RsiOverbought, 1)) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class SessionFaderBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "SessionFader";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Ranging;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableSessionFaderBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botSessionFader == null) return SignalDirection.NoSignal;

			bool sweptLow = s.Low[1] < s.botSessionFader.CurrentLow[1];
			bool closedAboveLow = s.Close[0] > s.botSessionFader.CurrentLow[0];
			if (sweptLow && closedAboveLow) return SignalDirection.Long;

			bool sweptHigh = s.High[1] > s.botSessionFader.CurrentHigh[1];
			bool closedBelowHigh = s.Close[0] < s.botSessionFader.CurrentHigh[0];
            if (sweptHigh && closedBelowHigh) return SignalDirection.Short;
			
            return SignalDirection.NoSignal;
        }
    }

    #endregion
    
    #region --- BREAKOUT BOTS ---
    
    public class SessionBreakerBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "SessionBreaker";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Breakout;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableSessionBreakerBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botSessionBreaker == null) return SignalDirection.NoSignal;

            if (s.CrossAbove(s.High, s.botSessionBreaker.CurrentHigh, 1)) return SignalDirection.Long;
            if (s.CrossBelow(s.Low, s.botSessionBreaker.CurrentLow, 1)) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class ORBBreakoutBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "ORB Breakout";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Breakout;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableORBBot || !s.ORBUseBreakoutSignals) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botORB == null) return SignalDirection.NoSignal;

            if (s.botORB.Signal[0] == 1 && s.botORB.Signal[1] != 1) return SignalDirection.Long;
            if (s.botORB.Signal[0] == -1 && s.botORB.Signal[1] != -1) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class TtmSqueezeBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "TTM Squeeze";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Breakout;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableTTMSqueezeBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botTTMSqueeze == null) return SignalDirection.NoSignal;
			
            if (s.botTTMSqueeze.BuilderSignal[0] == 1 && s.botTTMSqueeze.BuilderSignal[1] != 1) return SignalDirection.Long;
            if (s.botTTMSqueeze.BuilderSignal[0] == -1 && s.botTTMSqueeze.BuilderSignal[1] != -1) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class BollingerBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Bollinger";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Breakout;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableBollingerBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botBollinger == null) return SignalDirection.NoSignal;

            if (s.CrossAbove(s.Close, s.botBollinger.Upper, 1)) return SignalDirection.Long;
            if (s.CrossBelow(s.Close, s.botBollinger.Lower, 1)) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    public class KeltnerBot : ISignalBot
    {
        private KCAlgoBase s;
        public string Name => "Keltner";
        public KCAlgoBase.MarketRegime Regime => KCAlgoBase.MarketRegime.Breakout;
        public void Initialize(KCAlgoBase strategy) { s = strategy; }
        public SignalDirection CheckSignal(int barsAgo)
        {
			if (!s.EnableKeltnerBot) return SignalDirection.NoSignal;
			if (barsAgo != 0 || s.botKeltner == null) return SignalDirection.NoSignal;

            if (s.CrossAbove(s.Close, s.botKeltner.Upper, 1)) return SignalDirection.Long;
            if (s.CrossBelow(s.Close, s.botKeltner.Lower, 1)) return SignalDirection.Short;
            return SignalDirection.NoSignal;
        }
    }

    #endregion
}