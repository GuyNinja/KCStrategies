//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds all indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
    public class New2Swings : Indicator
    {
        #region State Management Variables
        private List<SwingPoint> twoWeekSwings;
        private bool isDataProcessed = false;
        private DateTime analysisStartDate;
        private DateTime analysisEndDate;
        #endregion

        private class SwingPoint
        {
            public double Price { get; set; }
            public DateTime Time { get; set; }
            public bool IsHigh { get; set; }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Analyzes swings within a specific 2-week trading window.";
                Name = "New2Swings";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                DrawOnPricePanel = true;
                PaintPriceMarkers = false;
                IsChartOnly = true;

                Strength = 12;
            }
            else if (State == State.DataLoaded)
            {
                // Process data only once when the script is loaded
                if (!isDataProcessed)
                {
                    ProcessTwoWeekHistoricalData();
                    isDataProcessed = true;
                }
            }
        }

        private void ProcessTwoWeekHistoricalData()
        {
            twoWeekSwings = new List<SwingPoint>();
            
            // --- 1. Calculate the analysis date range ---
            DateTime lastBarDate = Time[0].Date;
            int daysToSubtract = (int)lastBarDate.DayOfWeek - (int)DayOfWeek.Friday;
            if (daysToSubtract < 0) daysToSubtract += 7;
            
            analysisEndDate = lastBarDate.AddDays(-daysToSubtract);
            analysisStartDate = analysisEndDate.AddDays(-13); // 14 days total including the end date

            // --- 2. Get the required TimeZoneInfo for EST ---
            TimeZoneInfo easternZone;
            try
            {
                easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                Print("Eastern Standard Time zone not found. Please check system time zones.");
                return;
            }

            // --- 3. Iterate through historical bars to find swings ---
            Swing swingHighs = Swing(Strength);
            Swing swingLows = Swing(Strength);

            for (int i = 0; i < Bars.Count; i++)
            {
                // CORRECTED: Changed GetTime(i) to Time[i]
                DateTime barTime = Time[i];
                // Exit loop if we go past the start date to optimize
                if (barTime.Date < analysisStartDate)
                    break;

                DateTime estTime = TimeZoneInfo.ConvertTimeFromUtc(barTime, easternZone);

                if (estTime.Date >= analysisStartDate.Date && estTime.Date <= analysisEndDate.Date &&
                    estTime.DayOfWeek >= DayOfWeek.Monday && estTime.DayOfWeek <= DayOfWeek.Friday &&
                    estTime.TimeOfDay.Hours >= 7 && estTime.TimeOfDay.Hours < 11)
                {
                    if (swingHighs.High[i] > 0 && (i == 0 || swingHighs.High[i] != swingHighs.High[i-1]))
                    {
                        twoWeekSwings.Add(new SwingPoint { Price = swingHighs.High[i], Time = estTime, IsHigh = true });
                    }
                    if (swingLows.Low[i] > 0 && (i == 0 || swingLows.Low[i] != swingLows.Low[i-1]))
                    {
                        twoWeekSwings.Add(new SwingPoint { Price = swingLows.Low[i], Time = estTime, IsHigh = false });
                    }
                }
            }
        }

        protected override void OnBarUpdate()
        {
            // Only draw the verification box once on the last bar after the data is processed
            if (isDataProcessed && CurrentBar == Bars.Count - 1)
            {
                DrawVerificationBox();
            }
        }

        private void DrawVerificationBox()
        {
            string verificationText = string.Format(
                "2-Week Analysis Period:\n" +
                "Start: {0}\n" +
                "End: {1}\n" +
                "Valid Swings Found: {2}",
                analysisStartDate.ToShortDateString(),
                analysisEndDate.ToShortDateString(),
                twoWeekSwings.Count
            );

            Draw.TextFixed(this, "VerificationBox", verificationText, TextPosition.TopLeft, Brushes.White, new SimpleFont("Arial", 12), Brushes.Transparent, Brushes.DarkSlateGray, 70);
        }
        
        #region User-Configurable Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Strength", Order=1, GroupName="Parameters")]
        public int Strength { get; set; }
        #endregion
    }
}


#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private New2Swings[] cacheNew2Swings;
		public New2Swings New2Swings(int strength)
		{
			return New2Swings(Input, strength);
		}

		public New2Swings New2Swings(ISeries<double> input, int strength)
		{
			if (cacheNew2Swings != null)
				for (int idx = 0; idx < cacheNew2Swings.Length; idx++)
					if (cacheNew2Swings[idx] != null && cacheNew2Swings[idx].Strength == strength && cacheNew2Swings[idx].EqualsInput(input))
						return cacheNew2Swings[idx];
			return CacheIndicator<New2Swings>(new New2Swings(){ Strength = strength }, input, ref cacheNew2Swings);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.New2Swings New2Swings(int strength)
		{
			return indicator.New2Swings(Input, strength);
		}

		public Indicators.New2Swings New2Swings(ISeries<double> input , int strength)
		{
			return indicator.New2Swings(input, strength);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.New2Swings New2Swings(int strength)
		{
			return indicator.New2Swings(Input, strength);
		}

		public Indicators.New2Swings New2Swings(ISeries<double> input , int strength)
		{
			return indicator.New2Swings(input, strength);
		}
	}
}

#endregion
