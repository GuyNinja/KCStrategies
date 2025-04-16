#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Strategies;
#endregion

namespace NinjaTrader.NinjaScript.Strategies.KCStrategies
{
    public class SuperTrendBot : KCAlgoBase
    {
        // Parameters
		private double SuperTrendLong;
		private double SuperTrendShort;

		private TSSuperTrend TSSuperTrend1;

		private bool longSignal = false;
        private bool shortSignal = false;

		public override string DisplayName { get { return Name; } }
		
        protected override void OnStateChange()
        {
            base.OnStateChange();

            if (State == State.SetDefaults)
            {
                Description = "Strategy based on SuperTrend.";
                Name = "SuperTrendBot v1.0";
                StrategyName = "SuperTrendBot";
                Version = "1.0 Feb. 2025";
                Credits = "Strategy by Khanh Nguyen";
                ChartType = "Orenko 34-40-40";						
				
				SuperTrendLong	= 1;
				SuperTrendShort	= 1;
				
                InitialStop		= 120;
				ProfitTarget	= 120;
            }
            else if (State == State.DataLoaded)
            {
                InitializeIndicators();
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBars[0] < BarsRequiredToTrade)
                return;
            
			if ((Close[0] >= TSSuperTrend1.UpTrend[0])
				 && (TSSuperTrend1.UpTrend[0] != 0)
				 && (TSSuperTrend1.DownTrend[0] == 0)
				 && (TSSuperTrend1.DownTrend[1] != 0))
			{
				SuperTrendLong = TSSuperTrend1.UpTrend[0];
				longSignal = true;
			}
			
			if ((Position.MarketPosition == MarketPosition.Long)
				 && (GetCurrentAsk(0) > TSSuperTrend1.UpTrend[0])
				 && (GetCurrentBid(0) > TSSuperTrend1.UpTrend[0])
				 && (TSSuperTrend1.UpTrend[0] > SuperTrendLong))
			{
				SuperTrendLong = TSSuperTrend1.UpTrend[0];
			}
			
			if ((Close[0] <= TSSuperTrend1.DownTrend[0])
				 && (TSSuperTrend1.DownTrend[0] != 0)
				 && (TSSuperTrend1.UpTrend[0] == 0)
				 && (TSSuperTrend1.UpTrend[1] != 0))
			{
				SuperTrendShort = TSSuperTrend1.DownTrend[0];
				shortSignal = true;
			}
			
			if ((Position.MarketPosition == MarketPosition.Short)
				 && (GetCurrentAsk(0) < TSSuperTrend1.DownTrend[0])
				 && (GetCurrentBid(0) < TSSuperTrend1.DownTrend[0])
				 && (TSSuperTrend1.DownTrend[0] < SuperTrendShort))
			{
				SuperTrendShort = TSSuperTrend1.DownTrend[0];
			}
			
			base.OnBarUpdate();
        }

        protected override bool ValidateEntryLong()
        {
            // Logic for validating long entries
			if (longSignal) return true;
			else return false;
        }

        protected override bool ValidateEntryShort()
        {
            // Logic for validating short entries
			if (shortSignal) return true;
            else return false;
        }

       	protected override bool ValidateExitLong()
        {
            // Logic for validating long exits
            return enableExit? true : false;
        }

        protected override bool ValidateExitShort()
        {
			// Logic for validating short exits
			return enableExit? true : false;
        }

        #region Indicators
        protected override void InitializeIndicators()
        {						
			TSSuperTrend1				= TSSuperTrend(Close, SuperTrendMode.ATR, 14, 2.618, MovingAverageType.HMA, 14, false, false, false);
			TSSuperTrend1.Plots[0].Brush = Brushes.Green;
			TSSuperTrend1.Plots[1].Brush = Brushes.Red;
			AddChartIndicator(TSSuperTrend1);
        }
        #endregion

        #region Properties

        #endregion
    }
}
