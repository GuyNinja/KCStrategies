//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// 1Agent_Simple_v1_20241213.cs - Ultra-Minimal Efficiency Version
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
    /// 1Agent Simple - Ultra-minimal efficiency version
    /// Lean price momentum agent with minimal overhead
    /// </summary>
    public class OneAgent_Simple_v1 : Indicator
    {
        #region Variables - Ultra Lean
        private double signal = 0;
        private SMA ma9, ma21;
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "1Agent_Simple_v1";
                Description = "Ultra-minimal price momentum agent";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                AddPlot(Brushes.LimeGreen, "Signal");
            }
            else if (State == State.Configure)
            {
                ma9 = SMA(9);
                ma21 = SMA(21);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 21) return;
            
            // Ultra-simple logic: price vs MAs
            signal = (Close[0] > ma9[0] && ma9[0] > ma21[0]) ? 100 : 
                     (Close[0] < ma9[0] && ma9[0] < ma21[0]) ? 0 : 50;
            
            Values[0][0] = signal;
        }

        /// <summary>Get simple trend signal: 100=Bull, 50=Neutral, 0=Bear</summary>
        public double GetSignal() => signal;
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OneAgent_Simple_v1[] cacheOneAgent_Simple_v1;
		public OneAgent_Simple_v1 OneAgent_Simple_v1()
		{
			return OneAgent_Simple_v1(Input);
		}

		public OneAgent_Simple_v1 OneAgent_Simple_v1(ISeries<double> input)
		{
			if (cacheOneAgent_Simple_v1 != null)
				for (int idx = 0; idx < cacheOneAgent_Simple_v1.Length; idx++)
					if (cacheOneAgent_Simple_v1[idx] != null &&  cacheOneAgent_Simple_v1[idx].EqualsInput(input))
						return cacheOneAgent_Simple_v1[idx];
			return CacheIndicator<OneAgent_Simple_v1>(new OneAgent_Simple_v1(), input, ref cacheOneAgent_Simple_v1);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OneAgent_Simple_v1 OneAgent_Simple_v1()
		{
			return indicator.OneAgent_Simple_v1(Input);
		}

		public Indicators.OneAgent_Simple_v1 OneAgent_Simple_v1(ISeries<double> input )
		{
			return indicator.OneAgent_Simple_v1(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OneAgent_Simple_v1 OneAgent_Simple_v1()
		{
			return indicator.OneAgent_Simple_v1(Input);
		}

		public Indicators.OneAgent_Simple_v1 OneAgent_Simple_v1(ISeries<double> input )
		{
			return indicator.OneAgent_Simple_v1(input);
		}
	}
}

#endregion
