//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaView_v1_20241213.cs - Ultra-Minimal View Foundation
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
    /// NinjaView v1 - Ultra-minimal view foundation
    /// Lean display framework for market data visualization
    /// </summary>
    public class NinjaView_v1 : Indicator
    {
        #region Variables - Ultra Lean
        private double viewValue = 0;
        private bool showData = true;
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "NinjaView_v1_20241213";
                Description = "Ultra-minimal view foundation for market data";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                AddPlot(Brushes.CornflowerBlue, "ViewData");
            }
            else if (State == State.Configure)
            {
                // Minimal configuration
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1) return;
            
            // Ultra-simple view logic: display close price
            viewValue = showData ? Close[0] : 0;
            
            Values[0][0] = viewValue;
        }

        #region Properties - Minimal
        [Display(Name = "Show Data", GroupName = "Parameters", Order = 1)]
        public bool ShowData
        {
            get { return showData; }
            set { showData = value; }
        }
        #endregion

        /// <summary>Get current view value</summary>
        public double GetViewValue() => viewValue;
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private NinjaView_v1[] cacheNinjaView_v1;
		public NinjaView_v1 NinjaView_v1()
		{
			return NinjaView_v1(Input);
		}

		public NinjaView_v1 NinjaView_v1(ISeries<double> input)
		{
			if (cacheNinjaView_v1 != null)
				for (int idx = 0; idx < cacheNinjaView_v1.Length; idx++)
					if (cacheNinjaView_v1[idx] != null &&  cacheNinjaView_v1[idx].EqualsInput(input))
						return cacheNinjaView_v1[idx];
			return CacheIndicator<NinjaView_v1>(new NinjaView_v1(), input, ref cacheNinjaView_v1);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.NinjaView_v1 NinjaView_v1()
		{
			return indicator.NinjaView_v1(Input);
		}

		public Indicators.NinjaView_v1 NinjaView_v1(ISeries<double> input )
		{
			return indicator.NinjaView_v1(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.NinjaView_v1 NinjaView_v1()
		{
			return indicator.NinjaView_v1(Input);
		}

		public Indicators.NinjaView_v1 NinjaView_v1(ISeries<double> input )
		{
			return indicator.NinjaView_v1(input);
		}
	}
}

#endregion
