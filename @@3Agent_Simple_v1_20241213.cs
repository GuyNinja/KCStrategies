//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// 3Agent_Simple_v1_20241213.cs - Ultra-Minimal Volume Efficiency Version
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
    /// 3Agent Simple - Ultra-minimal volume surge detection
    /// Lean volume analysis with minimal overhead
    /// </summary>
    public class ThreeAgent_Simple_v1 : Indicator
    {
        #region Variables - Ultra Lean
        private double signal = 50;
        private SMA volSMA20;
        private bool volumeSurge = false;
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "3Agent_Simple_v1";
                Description = "Ultra-minimal volume surge detection";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                AddPlot(Brushes.Cyan, "Signal");
            }
            else if (State == State.Configure)
            {
                volSMA20 = SMA(Volumes[0], 20);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 20) return;
            
            double volRatio = Volume[0] / volSMA20[0];
            double priceChange = Close[0] - Close[1];
            
            // Ultra-simple volume logic
            volumeSurge = volRatio >= 2.0;
            
            if (volumeSurge && priceChange > 0)
                signal = 100;      // Bull volume surge
            else if (volumeSurge && priceChange < 0)
                signal = 0;        // Bear volume surge
            else if (volRatio > 1.5)
                signal = 75;       // High volume
            else if (volRatio < 0.7)
                signal = 25;       // Low volume
            else
                signal = 50;       // Normal volume
            
            Values[0][0] = signal;
        }

        /// <summary>Get volume signal: 100=BullSurge, 75=High, 50=Normal, 25=Low, 0=BearSurge</summary>
        public double GetSignal() => signal;
        
        /// <summary>Check if volume surge detected</summary>
        public bool HasSurge() => volumeSurge;
        
        /// <summary>Get current volume ratio vs average</summary>
        public double GetVolumeRatio() => Volume[0] / volSMA20[0];
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ThreeAgent_Simple_v1[] cacheThreeAgent_Simple_v1;
		public ThreeAgent_Simple_v1 ThreeAgent_Simple_v1()
		{
			return ThreeAgent_Simple_v1(Input);
		}

		public ThreeAgent_Simple_v1 ThreeAgent_Simple_v1(ISeries<double> input)
		{
			if (cacheThreeAgent_Simple_v1 != null)
				for (int idx = 0; idx < cacheThreeAgent_Simple_v1.Length; idx++)
					if (cacheThreeAgent_Simple_v1[idx] != null && cacheThreeAgent_Simple_v1[idx].EqualsInput(input))
						return cacheThreeAgent_Simple_v1[idx];
			return CacheIndicator<ThreeAgent_Simple_v1>(new ThreeAgent_Simple_v1(), input, ref cacheThreeAgent_Simple_v1);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ThreeAgent_Simple_v1 ThreeAgent_Simple_v1()
		{
			return indicator.ThreeAgent_Simple_v1(Input);
		}

		public Indicators.ThreeAgent_Simple_v1 ThreeAgent_Simple_v1(ISeries<double> input)
		{
			return indicator.ThreeAgent_Simple_v1(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ThreeAgent_Simple_v1 ThreeAgent_Simple_v1()
		{
			return indicator.ThreeAgent_Simple_v1(Input);
		}

		public Indicators.ThreeAgent_Simple_v1 ThreeAgent_Simple_v1(ISeries<double> input)
		{
			return indicator.ThreeAgent_Simple_v1(input);
		}
	}
}

#endregion
