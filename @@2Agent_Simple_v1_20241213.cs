//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// 2Agent_Simple_v1_20241213.cs - Ultra-Minimal RSI Efficiency Version
//
#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    /// <summary>
    /// 2Agent Simple - Ultra-minimal RSI momentum agent
    /// Lean overbought/oversold detection with minimal overhead
    /// </summary>
    public class TwoAgent_Simple_v1 : Indicator
    {
        #region Variables - Ultra Lean
        private double signal = 50;
        private RSI rsi14;
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "2Agent_Simple_v1";
                Description = "Ultra-minimal RSI momentum agent";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                AddPlot(Brushes.Orange, "Signal");
            }
            else if (State == State.Configure)
            {
                rsi14 = RSI(14, 1);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 14) return;
            
            double rsiVal = rsi14[0];
            
            // Ultra-simple RSI logic
            signal = rsiVal > 70 ? 0 :      // Overbought = Bear signal
                     rsiVal < 30 ? 100 :    // Oversold = Bull signal
                     50;                    // Neutral
            
            Values[0][0] = signal;
        }

        /// <summary>Get RSI signal: 100=Bull, 50=Neutral, 0=Bear</summary>
        public double GetSignal() => signal;
        
        /// <summary>Get current RSI value</summary>
        public double GetRsi() => rsi14[0];
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TwoAgent_Simple_v1[] cacheTwoAgent_Simple_v1;
		public TwoAgent_Simple_v1 TwoAgent_Simple_v1()
		{
			return TwoAgent_Simple_v1(Input);
		}

		public TwoAgent_Simple_v1 TwoAgent_Simple_v1(ISeries<double> input)
		{
			if (cacheTwoAgent_Simple_v1 != null)
				for (int idx = 0; idx < cacheTwoAgent_Simple_v1.Length; idx++)
					if (cacheTwoAgent_Simple_v1[idx] != null &&  cacheTwoAgent_Simple_v1[idx].EqualsInput(input))
						return cacheTwoAgent_Simple_v1[idx];
			return CacheIndicator<TwoAgent_Simple_v1>(new TwoAgent_Simple_v1(), input, ref cacheTwoAgent_Simple_v1);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TwoAgent_Simple_v1 TwoAgent_Simple_v1()
		{
			return indicator.TwoAgent_Simple_v1(Input);
		}

		public Indicators.TwoAgent_Simple_v1 TwoAgent_Simple_v1(ISeries<double> input )
		{
			return indicator.TwoAgent_Simple_v1(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TwoAgent_Simple_v1 TwoAgent_Simple_v1()
		{
			return indicator.TwoAgent_Simple_v1(Input);
		}

		public Indicators.TwoAgent_Simple_v1 TwoAgent_Simple_v1(ISeries<double> input )
		{
			return indicator.TwoAgent_Simple_v1(input);
		}
	}
}

#endregion
