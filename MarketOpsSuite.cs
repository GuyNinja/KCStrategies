#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using Brush = System.Windows.Media.Brush;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class MarketOpsSuite : Indicator
	{
		#region Variables
		private double todaysONH, todaysONL, todaysONM;
		private double openingRangeHigh, openingRangeLow;
		private int lastDayCalculated, lastORDayCalculated; 
		private List<Brush> dailyBrushes = new List<Brush>();
		private SMA volSma; 
		private List<double> dailyCloses = new List<double>();
		private double currentDailySmaValue;
		private bool alertArmedONH, alertArmedONL, alertArmedSMA;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description	= @"A multi-function suite for displaying key market levels and volume analysis.";
				Name		= @"MarketOpsSuite";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DrawOnPricePanel							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;

				ShowOvernightLines	= true; ShowMidpoint		= true; OvernightStart		= 1800; OvernightEnd		= 0930; SessionEndTime		= 1600; LineStyle			= DashStyleHelper.Solid; LineWidth			= 2; ShowCurrentWeekOnly = true; MidpointColor		= Brushes.CornflowerBlue; MidpointLineStyle 	= DashStyleHelper.Dot;
				ShowLabels			= true; LabelDateFormat		= "DayOfWeek"; UseAlternateLabelFormat = true; LabelFont			= new Gui.Tools.SimpleFont("Arial", 12); LabelColor			= Brushes.Gray; LabelVerticalOffset	= 5; LabelHorizontalOffset = 2;
				SundayColor 		= Brushes.MediumPurple; MondayColor 		= Brushes.DodgerBlue; TuesdayColor		= Brushes.Crimson; WednesdayColor		= Brushes.Orange; ThursdayColor		= Brushes.SeaGreen; FridayColor			= Brushes.Gold;
				ShowSessionShading	= true; SessionColor1		= Brushes.SlateGray; SessionColor2		= Brushes.DarkGray; SessionOpacity		= 15;
				ShowOpeningRange	= true; OpeningRangeMinutes = 15; OpeningRangeColor 	= Brushes.OrangeRed; OrLineStyle			= DashStyleHelper.Dash;
				ShowVolumeSpikes		= true; VolumeLookback			= 3; VolumeSpikeThreshold	= 150; SpikeColor				= Brushes.White; ShowSpikeLabels			= true; SpikeLabelColor			= Brushes.LightGray; 
				ShowDailySma 		= true; SmaPeriod			= 20; SmaColor			= Brushes.Gold; SmaLineStyle		= DashStyleHelper.Dash; SmaLineWidth		= 2;
				EnableAlerts		= true; AlertOnONH			= true; AlertOnONL			= true; AlertOnDailySma		= true; AlertSound			= "Alert1.wav";

				OvernightHigh 		= new Series<double>(this, MaximumBarsLookBack.Infinite); OvernightLow		= new Series<double>(this, MaximumBarsLookBack.Infinite); DailySmaValue		= new Series<double>(this, MaximumBarsLookBack.Infinite); VolumeSpikePercent 	= new Series<double>(this, MaximumBarsLookBack.Infinite);
			}
			else if (State == State.DataLoaded)
			{
				dailyBrushes.Add(SundayColor); dailyBrushes.Add(MondayColor); dailyBrushes.Add(TuesdayColor); dailyBrushes.Add(WednesdayColor); dailyBrushes.Add(ThursdayColor); dailyBrushes.Add(FridayColor); dailyBrushes.Add(Brushes.Transparent);
				volSma = SMA(Volume, VolumeLookback);
				for (int i = CurrentBar - 1; i > 0; i--) { if (ToDay(Time[i]) != ToDay(Time[i+1])) dailyCloses.Insert(0, Close[i+1]); }
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 200) return;

			if (Bars.IsFirstBarOfSession){ alertArmedONH = true; alertArmedONL = true; alertArmedSMA = true; }
			
			if (Bars.IsFirstBarOfSession) { if(CurrentBar > 1) dailyCloses.Add(Close[1]); if (dailyCloses.Count > SmaPeriod + 5) dailyCloses.RemoveAt(0); }
			if(dailyCloses.Count >= SmaPeriod) { double sum = 0; for (int i = dailyCloses.Count - SmaPeriod; i < dailyCloses.Count; i++) sum += dailyCloses[i]; currentDailySmaValue = sum / SmaPeriod; }
			DailySmaValue[0] = currentDailySmaValue;
			if (ShowDailySma && DailySmaValue[0] > 0) Draw.HorizontalLine(this, "DailySMA", DailySmaValue[0], SmaColor, SmaLineStyle, SmaLineWidth); else RemoveDrawObject("DailySMA");
			
			if (CurrentBar > VolumeLookback) {
				if(ShowVolumeSpikes){ double avgVolume = volSma[1]; double currentVolume = Volume[0]; double percentIncrease = 0; if (avgVolume > 0) percentIncrease = ((currentVolume / avgVolume) - 1) * 100; VolumeSpikePercent[0] = percentIncrease; if (percentIncrease >= VolumeSpikeThreshold){ Draw.Dot(this, "VolSpike_" + CurrentBar, false, 0, Low[0] - 2 * TickSize, SpikeColor); if (ShowSpikeLabels){ string labelText = string.Format("+{0:F0}%", percentIncrease); Draw.Text(this, "VolSpikeLabel_" + CurrentBar, labelText, -LabelHorizontalOffset, Low[0] - 5 * TickSize, SpikeLabelColor); } }
				} else { VolumeSpikePercent[0] = 0; }
			}
			
			if (ToTime(Time[0]) >= OvernightEnd && lastDayCalculated != ToDay(Time[0])) {
				if (ShowCurrentWeekOnly && Time[0].DayOfWeek == DayOfWeek.Tuesday){ DateTime lastFriday = Time[0].AddDays(-4).Date; string lastFridayStr = lastFriday.ToString("yyyyMMdd"); RemoveDrawObject("ONH_" + lastFridayStr); RemoveDrawObject("ONL_" + lastFridayStr); RemoveDrawObject("ONM_" + lastFridayStr); RemoveDrawObject("ONH_Label_" + lastFridayStr); RemoveDrawObject("ONL_Label_" + lastFridayStr); }
				todaysONH = 0; todaysONL = 0;
				DateTime sessionEndTime = Time[0].Date.AddHours(OvernightEnd / 100).AddMinutes(OvernightEnd % 100); DateTime sessionStartTime = sessionEndTime.AddDays(-1).Date.AddHours(OvernightStart / 100).AddMinutes(OvernightStart % 100);
				for (int i = 0; i < 200; i++) { if (CurrentBar - i < 0) break; DateTime barTime = Time[i]; if (barTime >= sessionStartTime && barTime < sessionEndTime) { if (todaysONH == 0 || High[i] > todaysONH) todaysONH = High[i]; if (todaysONL == 0 || Low[i] < todaysONL) todaysONL = Low[i]; } if (barTime < sessionStartTime) break; }
				lastDayCalculated = ToDay(Time[0]); todaysONM = (todaysONH + todaysONL) / 2;
			}
			
			int timeOfBar = ToTime(Time[0]);
			DateTime orEnd = Time[0].Date.AddHours(OvernightEnd/100).AddMinutes(OvernightEnd%100).AddMinutes(OpeningRangeMinutes);
			if (ShowOpeningRange) {
				if (Bars.IsFirstBarOfSession) { openingRangeHigh = 0; openingRangeLow = 0; lastORDayCalculated = 0; RemoveDrawObject("ORH_" + ToDay(Time[1])); RemoveDrawObject("ORL_" + ToDay(Time[1])); }
				if (timeOfBar >= OvernightEnd && lastORDayCalculated != ToDay(Time[0])) {
					if (Time[0] < orEnd) { if (openingRangeHigh == 0 || High[0] > openingRangeHigh) openingRangeHigh = High[0]; if (openingRangeLow == 0 || Low[0] < openingRangeLow) openingRangeLow = Low[0]; }
					else if(openingRangeHigh > 0) { Draw.HorizontalLine(this, "ORH_" + ToDay(Time[0]), openingRangeHigh, OpeningRangeColor, OrLineStyle, 2); Draw.HorizontalLine(this, "ORL_" + ToDay(Time[0]), openingRangeLow, OpeningRangeColor, OrLineStyle, 2); lastORDayCalculated = ToDay(Time[0]); }
				}
			}
			
			if (lastDayCalculated > 0) {
				OvernightHigh[0] = todaysONH; OvernightLow[0] = todaysONL;
				if(ToDay(Time[0]) >= lastDayCalculated) {
					DayOfWeek day = new DateTime(lastDayCalculated/10000, (lastDayCalculated/100)%100, lastDayCalculated%100).DayOfWeek; Brush colorForThisDay = dailyBrushes[(int)day];
					if (ShowOvernightLines) { Draw.HorizontalLine(this, "ONH_" + lastDayCalculated, todaysONH, colorForThisDay, LineStyle, LineWidth); Draw.HorizontalLine(this, "ONL_" + lastDayCalculated, todaysONL, colorForThisDay, LineStyle, LineWidth); if(ShowMidpoint) Draw.HorizontalLine(this, "ONM_" + lastDayCalculated, todaysONM, MidpointColor, MidpointLineStyle, LineWidth);
					} else { RemoveDrawObject("ONH_" + lastDayCalculated); RemoveDrawObject("ONL_" + lastDayCalculated); RemoveDrawObject("ONM_" + lastDayCalculated); }
					if(ToDay(Time[0]) == lastDayCalculated && ToTime(Time[0]) >= OvernightEnd && ToTime(Time[1]) < OvernightEnd) {
						if (ShowLabels) { string dateString = ""; DateTime dateForLabel = new DateTime(lastDayCalculated/10000, (lastDayCalculated/100)%100, lastDayCalculated%100); if (UseAlternateLabelFormat) dateString = dateForLabel.ToString("M/d") + " "; else { if(LabelDateFormat == "DayOfWeek") dateString = dateForLabel.ToString("ddd") + " "; else if (LabelDateFormat == "ShortDate") dateString = dateForLabel.ToShortDateString() + " "; } string highLabelText, lowLabelText; if (UseAlternateLabelFormat) { highLabelText = string.Format("{0}ON High", dateString); lowLabelText  = string.Format("{0}ON Low", dateString); } else { highLabelText = string.Format("{0}ON High {1}", dateString, todaysONH); lowLabelText  = string.Format("{0}ON Low {1}", dateString, todaysONL); } double highLabelPrice = todaysONH + (LabelVerticalOffset * TickSize); double lowLabelPrice  = todaysONL - (LabelVerticalOffset * TickSize); Draw.Text(this, "ONH_Label_" + lastDayCalculated, false, highLabelText, -LabelHorizontalOffset, highLabelPrice, 0, LabelColor, LabelFont, TextAlignment.Left, null, Brushes.Transparent, 0); Draw.Text(this, "ONL_Label_" + lastDayCalculated, false, lowLabelText, -LabelHorizontalOffset, lowLabelPrice, 0, LabelColor, LabelFont, TextAlignment.Left, null, Brushes.Transparent, 0);
						} else { RemoveDrawObject("ONH_Label_" + lastDayCalculated); RemoveDrawObject("ONL_Label_" + lastDayCalculated); }
					}
				}
			}
			
			if (ShowSessionShading && ToTime(Time[0]) >= SessionEndTime && ToTime(Time[1]) < SessionEndTime) {
				double minutesInSession = (new TimeSpan(SessionEndTime / 100, SessionEndTime % 100, 0) - new TimeSpan(OvernightEnd / 100, OvernightEnd % 100, 0)).TotalMinutes; int barsInSession = (int)(minutesInSession / Bars.BarsPeriod.Value); Brush colorToUse = (Time[0].DayOfYear % 2 == 0) ? SessionColor1 : SessionColor2; Brush finalBrush = colorToUse.Clone(); finalBrush.Opacity = SessionOpacity / 100.0; finalBrush.Freeze(); string tag = "SessionBG_" + ToDay(Time[0]); Draw.Rectangle(this, tag, barsInSession, 500000, 0, 0, finalBrush);
			}
			
			if (EnableAlerts && IsFirstTickOfBar) {
				if (alertArmedONH && AlertOnONH && todaysONH > 0 && (CrossAbove(Close, todaysONH, 1) || CrossBelow(Close, todaysONH, 1))) { Alert("ONH_Cross", Priority.High, "Price crossing Overnight High (" + todaysONH + ")", NinjaTrader.Core.Globals.InstallDir + @"\sounds\" + AlertSound, 10, Brushes.Transparent, Brushes.Transparent); alertArmedONH = false; }
				if (alertArmedONL && AlertOnONL && todaysONL > 0 && (CrossAbove(Close, todaysONL, 1) || CrossBelow(Close, todaysONL, 1))) { Alert("ONL_Cross", Priority.High, "Price crossing Overnight Low (" + todaysONL + ")", NinjaTrader.Core.Globals.InstallDir + @"\sounds\" + AlertSound, 10, Brushes.Transparent, Brushes.Transparent); alertArmedONL = false; }
				if (alertArmedSMA && AlertOnDailySma && DailySmaValue[0] > 0 && (CrossAbove(Close, DailySmaValue, 1) || CrossBelow(Close, DailySmaValue, 1))) { Alert("SMA_Cross", Priority.High, "Price crossing Daily SMA (" + DailySmaValue[0].ToString("N2") + ")", NinjaTrader.Core.Globals.InstallDir + @"\sounds\" + AlertSound, 10, Brushes.Transparent, Brushes.Transparent); alertArmedSMA = false; }
			}
		}

		#region Properties
		[Browsable(false)][XmlIgnore] public Series<double> OvernightHigh { get; private set; }
		[Browsable(false)][XmlIgnore] public Series<double> OvernightLow { get; private set; }
		[Browsable(false)][XmlIgnore] public Series<double> DailySmaValue { get; private set; }
		[Browsable(false)][XmlIgnore] public Series<double> VolumeSpikePercent { get; private set; }

		// --- All Parameters ---
		[NinjaScriptProperty]
		[Display(Name="Show Overnight Lines", Order=1, GroupName="1. Overnight Levels")]
		public bool ShowOvernightLines { get; set; }
		[NinjaScriptProperty]
		[Display(Name="Show Midpoint Line", Order=2, GroupName="1. Overnight Levels")]
		public bool ShowMidpoint { get; set; }
		[NinjaScriptProperty]
		[Range(0, 2359)]
		[Display(Name="Overnight Start Time (HHmm)", Order=3, GroupName="1. Overnight Levels")]
		public int OvernightStart { get; set; }
		[NinjaScriptProperty]
		[Range(0, 2359)]
		[Display(Name="Overnight End Time (HHmm)", Order=4, GroupName="1. Overnight Levels")]
		public int OvernightEnd { get; set; }
		[NinjaScriptProperty]
		[Range(0, 2359)]
		[Display(Name="Session End Time (HHmm)", Order=5, GroupName="1. Overnight Levels")]
		public int SessionEndTime { get; set; }
		[NinjaScriptProperty]
		[Display(Name="Line Style", Order=6, GroupName="1. Overnight Levels")]
		public DashStyleHelper LineStyle { get; set; }
		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name="Line Width", Order=7, GroupName="1. Overnight Levels")]
		public int LineWidth { get; set; }
		[NinjaScriptProperty]
		[Display(Name="Show Current Week Only", Description="If checked, removes previous week's lines on Tuesday.", Order=8, GroupName="1. Overnight Levels")]
		public bool ShowCurrentWeekOnly { get; set; }
		[XmlIgnore]
		[Display(Name="Midpoint Color", Order=9, GroupName="1. Overnight Levels")]
		public Brush MidpointColor { get; set; }
		[Browsable(false)]
		public string MidpointColorSerializable { get { return Serialize.BrushToString(MidpointColor); } set { MidpointColor = Serialize.StringToBrush(value); } }
		[NinjaScriptProperty]
		[Display(Name="Midpoint Style", Order=10, GroupName="1. Overnight Levels")]
		public DashStyleHelper MidpointLineStyle { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Labels", Order=11, GroupName="2. Labels")]
		public bool ShowLabels { get; set; }
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.StringEditor")]
		[Display(Name="Label Date Format", Description="Type 'DayOfWeek' or 'ShortDate'. Used if Alternate Format is unchecked.", Order=12, GroupName="2. Labels")]
		public string LabelDateFormat { get; set; }
		[NinjaScriptProperty]
		[Display(Name="Use Alternate Label Format", Description="Uses a cleaner format, e.g., '8/8 ON High'", Order=13, GroupName="2. Labels")]
		public bool UseAlternateLabelFormat { get; set; }
		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name="Label Vertical Offset (Ticks)", Order=14, GroupName="2. Labels")]
		public int LabelVerticalOffset { get; set; }
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Label Horizontal Offset (Bars)", Order=15, GroupName="2. Labels")]
		public int LabelHorizontalOffset { get; set; }
		[XmlIgnore]
		[Display(Name="Label Color", Order=16, GroupName="2. Labels")]
		public Brush LabelColor { get; set; }
		[Browsable(false)]
		public string LabelColorSerializable { get { return Serialize.BrushToString(LabelColor); } set { LabelColor = Serialize.StringToBrush(value); } }
		[Display(Name="Label Font", Order=17, GroupName="2. Labels")]
		public Gui.Tools.SimpleFont LabelFont { get; set; }
		
		[XmlIgnore]
		[Display(Name="Sunday Color", Order = 18, GroupName = "3. Daily Colors")]
		public Brush SundayColor { get; set; }
		[Browsable(false)]
		public string SundayColorSerializable { get { return Serialize.BrushToString(SundayColor); } set { SundayColor = Serialize.StringToBrush(value); } }
		[XmlIgnore]
		[Display(Name="Monday Color", Order = 19, GroupName = "3. Daily Colors")]
		public Brush MondayColor { get; set; }
		[Browsable(false)]
		public string MondayColorSerializable { get { return Serialize.BrushToString(MondayColor); } set { MondayColor = Serialize.StringToBrush(value); } }
		[XmlIgnore]
		[Display(Name="Tuesday Color", Order = 20, GroupName = "3. Daily Colors")]
		public Brush TuesdayColor { get; set; }
		[Browsable(false)]
		public string TuesdayColorSerializable { get { return Serialize.BrushToString(TuesdayColor); } set { TuesdayColor = Serialize.StringToBrush(value); } }
		[XmlIgnore]
		[Display(Name="Wednesday Color", Order = 21, GroupName = "3. Daily Colors")]
		public Brush WednesdayColor { get; set; }
		[Browsable(false)]
		public string WednesdayColorSerializable { get { return Serialize.BrushToString(WednesdayColor); } set { WednesdayColor = Serialize.StringToBrush(value); } }
		[XmlIgnore]
		[Display(Name="Thursday Color", Order = 22, GroupName = "3. Daily Colors")]
		public Brush ThursdayColor { get; set; }
		[Browsable(false)]
		public string ThursdayColorSerializable { get { return Serialize.BrushToString(ThursdayColor); } set { ThursdayColor = Serialize.StringToBrush(value); } }
		[XmlIgnore]
		[Display(Name="Friday Color", Order = 23, GroupName = "3. Daily Colors")]
		public Brush FridayColor { get; set; }
		[Browsable(false)]
		public string FridayColorSerializable { get { return Serialize.BrushToString(FridayColor); } set { FridayColor = Serialize.StringToBrush(value); } }
		
		[NinjaScriptProperty]
		[Display(Name="Show Session Shading", Order=24, GroupName="4. Session Shading")]
		public bool ShowSessionShading { get; set; }
		[XmlIgnore]
		[Display(Name="Session Color 1", Order=25, GroupName="4. Session Shading")]
		public Brush SessionColor1 { get; set; }
		[Browsable(false)]
		public string SessionColor1Serializable { get { return Serialize.BrushToString(SessionColor1); } set { SessionColor1 = Serialize.StringToBrush(value); } }
		[XmlIgnore]
		[Display(Name="Session Color 2", Order=26, GroupName="4. Session Shading")]
		public Brush SessionColor2 { get; set; }
		[Browsable(false)]
		public string SessionColor2Serializable { get { return Serialize.BrushToString(SessionColor2); } set { SessionColor2 = Serialize.StringToBrush(value); } }
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Session Opacity %", Order=27, GroupName="4. Session Shading")]
		public int SessionOpacity { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Show Opening Range", Order=28, GroupName="5. Opening Range")]
		public bool ShowOpeningRange { get; set; }
		[NinjaScriptProperty]
		[Range(1, 120)]
		[Display(Name="OR Minutes", Order=29, GroupName="5. Opening Range")]
		public int OpeningRangeMinutes { get; set; }
		[XmlIgnore]
		[Display(Name="OR Color", Order=30, GroupName="5. Opening Range")]
		public Brush OpeningRangeColor { get; set; }
		[Browsable(false)]
		public string OpeningRangeColorSerializable { get { return Serialize.BrushToString(OpeningRangeColor); } set { OpeningRangeColor = Serialize.StringToBrush(value); } }
		[NinjaScriptProperty]
		[Display(Name="OR Line Style", Order=31, GroupName="5. Opening Range")]
		public DashStyleHelper OrLineStyle { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Volume Spikes", Order=32, GroupName="6. Volume Analysis")]
		public bool ShowVolumeSpikes { get; set; }
		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name="Volume Lookback Period", Order=33, GroupName="6. Volume Analysis")]
		public int VolumeLookback { get; set; }
		[NinjaScriptProperty]
		[Range(1, 10000)]
		[Display(Name="Volume Spike Threshold %", Order=34, GroupName="6. Volume Analysis")]
		public double VolumeSpikeThreshold { get; set; }
		[XmlIgnore]
		[Display(Name="Spike Color", Order=35, GroupName="6. Volume Analysis")]
		public Brush SpikeColor { get; set; }
		[Browsable(false)]
		public string SpikeColorSerializable { get { return Serialize.BrushToString(SpikeColor); } set { SpikeColor = Serialize.StringToBrush(value); } }
		[NinjaScriptProperty]
		[Display(Name="Show Spike Labels", Order=36, GroupName="6. Volume Analysis")]
		public bool ShowSpikeLabels { get; set; }
		[XmlIgnore]
		[Display(Name="Spike Label Color", Order=37, GroupName="6. Volume Analysis")]
		public Brush SpikeLabelColor { get; set; }
		[Browsable(false)]
		public string SpikeLabelColorSerializable { get { return Serialize.BrushToString(SpikeLabelColor); } set { SpikeLabelColor = Serialize.StringToBrush(value); } }
		
		[NinjaScriptProperty]
		[Display(Name="Show Daily SMA", Order=38, GroupName="7. Daily SMA")]
		public bool ShowDailySma { get; set; }
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SMA Period", Order=39, GroupName="7. Daily SMA")]
		public int SmaPeriod { get; set; }
		[XmlIgnore]
		[Display(Name="SMA Color", Order=40, GroupName="7. Daily SMA")]
		public Brush SmaColor { get; set; }
		[Browsable(false)]
		public string SmaColorSerializable { get { return Serialize.BrushToString(SmaColor); } set { SmaColor = Serialize.StringToBrush(value); } }
		[NinjaScriptProperty]
		[Display(Name="SMA Line Style", Order=41, GroupName="7. Daily SMA")]
		public DashStyleHelper SmaLineStyle { get; set; }
		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name="SMA Line Width", Order=42, GroupName="7. Daily SMA")]
		public int SmaLineWidth { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Enable Alerts", Order=43, GroupName="8. Alerts")]
		public bool EnableAlerts { get; set; }
		[NinjaScriptProperty]
		[Display(Name="Alert on ONH Cross", Order=44, GroupName="8. Alerts")]
		public bool AlertOnONH { get; set; }
		[NinjaScriptProperty]
		[Display(Name="Alert on ONL Cross", Order=45, GroupName="8. Alerts")]
		public bool AlertOnONL { get; set; }
		[NinjaScriptProperty]
		[Display(Name="Alert on Daily SMA Cross", Order=46, GroupName="8. Alerts")]
		public bool AlertOnDailySma { get; set; }
		[PropertyEditor("NinjaTrader.Gui.Tools.SoundEditor")]
		[Display(Name="Alert Sound", Order=47, GroupName="8. Alerts")]
		public string AlertSound { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private MarketOpsSuite[] cacheMarketOpsSuite;
		public MarketOpsSuite MarketOpsSuite(bool showOvernightLines, bool showMidpoint, int overnightStart, int overnightEnd, int sessionEndTime, DashStyleHelper lineStyle, int lineWidth, bool showCurrentWeekOnly, DashStyleHelper midpointLineStyle, bool showLabels, string labelDateFormat, bool useAlternateLabelFormat, int labelVerticalOffset, int labelHorizontalOffset, bool showSessionShading, int sessionOpacity, bool showOpeningRange, int openingRangeMinutes, DashStyleHelper orLineStyle, bool showVolumeSpikes, int volumeLookback, double volumeSpikeThreshold, bool showSpikeLabels, bool showDailySma, int smaPeriod, DashStyleHelper smaLineStyle, int smaLineWidth, bool enableAlerts, bool alertOnONH, bool alertOnONL, bool alertOnDailySma)
		{
			return MarketOpsSuite(Input, showOvernightLines, showMidpoint, overnightStart, overnightEnd, sessionEndTime, lineStyle, lineWidth, showCurrentWeekOnly, midpointLineStyle, showLabels, labelDateFormat, useAlternateLabelFormat, labelVerticalOffset, labelHorizontalOffset, showSessionShading, sessionOpacity, showOpeningRange, openingRangeMinutes, orLineStyle, showVolumeSpikes, volumeLookback, volumeSpikeThreshold, showSpikeLabels, showDailySma, smaPeriod, smaLineStyle, smaLineWidth, enableAlerts, alertOnONH, alertOnONL, alertOnDailySma);
		}

		public MarketOpsSuite MarketOpsSuite(ISeries<double> input, bool showOvernightLines, bool showMidpoint, int overnightStart, int overnightEnd, int sessionEndTime, DashStyleHelper lineStyle, int lineWidth, bool showCurrentWeekOnly, DashStyleHelper midpointLineStyle, bool showLabels, string labelDateFormat, bool useAlternateLabelFormat, int labelVerticalOffset, int labelHorizontalOffset, bool showSessionShading, int sessionOpacity, bool showOpeningRange, int openingRangeMinutes, DashStyleHelper orLineStyle, bool showVolumeSpikes, int volumeLookback, double volumeSpikeThreshold, bool showSpikeLabels, bool showDailySma, int smaPeriod, DashStyleHelper smaLineStyle, int smaLineWidth, bool enableAlerts, bool alertOnONH, bool alertOnONL, bool alertOnDailySma)
		{
			if (cacheMarketOpsSuite != null)
				for (int idx = 0; idx < cacheMarketOpsSuite.Length; idx++)
					if (cacheMarketOpsSuite[idx] != null && cacheMarketOpsSuite[idx].ShowOvernightLines == showOvernightLines && cacheMarketOpsSuite[idx].ShowMidpoint == showMidpoint && cacheMarketOpsSuite[idx].OvernightStart == overnightStart && cacheMarketOpsSuite[idx].OvernightEnd == overnightEnd && cacheMarketOpsSuite[idx].SessionEndTime == sessionEndTime && cacheMarketOpsSuite[idx].LineStyle == lineStyle && cacheMarketOpsSuite[idx].LineWidth == lineWidth && cacheMarketOpsSuite[idx].ShowCurrentWeekOnly == showCurrentWeekOnly && cacheMarketOpsSuite[idx].MidpointLineStyle == midpointLineStyle && cacheMarketOpsSuite[idx].ShowLabels == showLabels && cacheMarketOpsSuite[idx].LabelDateFormat == labelDateFormat && cacheMarketOpsSuite[idx].UseAlternateLabelFormat == useAlternateLabelFormat && cacheMarketOpsSuite[idx].LabelVerticalOffset == labelVerticalOffset && cacheMarketOpsSuite[idx].LabelHorizontalOffset == labelHorizontalOffset && cacheMarketOpsSuite[idx].ShowSessionShading == showSessionShading && cacheMarketOpsSuite[idx].SessionOpacity == sessionOpacity && cacheMarketOpsSuite[idx].ShowOpeningRange == showOpeningRange && cacheMarketOpsSuite[idx].OpeningRangeMinutes == openingRangeMinutes && cacheMarketOpsSuite[idx].OrLineStyle == orLineStyle && cacheMarketOpsSuite[idx].ShowVolumeSpikes == showVolumeSpikes && cacheMarketOpsSuite[idx].VolumeLookback == volumeLookback && cacheMarketOpsSuite[idx].VolumeSpikeThreshold == volumeSpikeThreshold && cacheMarketOpsSuite[idx].ShowSpikeLabels == showSpikeLabels && cacheMarketOpsSuite[idx].ShowDailySma == showDailySma && cacheMarketOpsSuite[idx].SmaPeriod == smaPeriod && cacheMarketOpsSuite[idx].SmaLineStyle == smaLineStyle && cacheMarketOpsSuite[idx].SmaLineWidth == smaLineWidth && cacheMarketOpsSuite[idx].EnableAlerts == enableAlerts && cacheMarketOpsSuite[idx].AlertOnONH == alertOnONH && cacheMarketOpsSuite[idx].AlertOnONL == alertOnONL && cacheMarketOpsSuite[idx].AlertOnDailySma == alertOnDailySma && cacheMarketOpsSuite[idx].EqualsInput(input))
						return cacheMarketOpsSuite[idx];
			return CacheIndicator<MarketOpsSuite>(new MarketOpsSuite(){ ShowOvernightLines = showOvernightLines, ShowMidpoint = showMidpoint, OvernightStart = overnightStart, OvernightEnd = overnightEnd, SessionEndTime = sessionEndTime, LineStyle = lineStyle, LineWidth = lineWidth, ShowCurrentWeekOnly = showCurrentWeekOnly, MidpointLineStyle = midpointLineStyle, ShowLabels = showLabels, LabelDateFormat = labelDateFormat, UseAlternateLabelFormat = useAlternateLabelFormat, LabelVerticalOffset = labelVerticalOffset, LabelHorizontalOffset = labelHorizontalOffset, ShowSessionShading = showSessionShading, SessionOpacity = sessionOpacity, ShowOpeningRange = showOpeningRange, OpeningRangeMinutes = openingRangeMinutes, OrLineStyle = orLineStyle, ShowVolumeSpikes = showVolumeSpikes, VolumeLookback = volumeLookback, VolumeSpikeThreshold = volumeSpikeThreshold, ShowSpikeLabels = showSpikeLabels, ShowDailySma = showDailySma, SmaPeriod = smaPeriod, SmaLineStyle = smaLineStyle, SmaLineWidth = smaLineWidth, EnableAlerts = enableAlerts, AlertOnONH = alertOnONH, AlertOnONL = alertOnONL, AlertOnDailySma = alertOnDailySma }, input, ref cacheMarketOpsSuite);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MarketOpsSuite MarketOpsSuite(bool showOvernightLines, bool showMidpoint, int overnightStart, int overnightEnd, int sessionEndTime, DashStyleHelper lineStyle, int lineWidth, bool showCurrentWeekOnly, DashStyleHelper midpointLineStyle, bool showLabels, string labelDateFormat, bool useAlternateLabelFormat, int labelVerticalOffset, int labelHorizontalOffset, bool showSessionShading, int sessionOpacity, bool showOpeningRange, int openingRangeMinutes, DashStyleHelper orLineStyle, bool showVolumeSpikes, int volumeLookback, double volumeSpikeThreshold, bool showSpikeLabels, bool showDailySma, int smaPeriod, DashStyleHelper smaLineStyle, int smaLineWidth, bool enableAlerts, bool alertOnONH, bool alertOnONL, bool alertOnDailySma)
		{
			return indicator.MarketOpsSuite(Input, showOvernightLines, showMidpoint, overnightStart, overnightEnd, sessionEndTime, lineStyle, lineWidth, showCurrentWeekOnly, midpointLineStyle, showLabels, labelDateFormat, useAlternateLabelFormat, labelVerticalOffset, labelHorizontalOffset, showSessionShading, sessionOpacity, showOpeningRange, openingRangeMinutes, orLineStyle, showVolumeSpikes, volumeLookback, volumeSpikeThreshold, showSpikeLabels, showDailySma, smaPeriod, smaLineStyle, smaLineWidth, enableAlerts, alertOnONH, alertOnONL, alertOnDailySma);
		}

		public Indicators.MarketOpsSuite MarketOpsSuite(ISeries<double> input , bool showOvernightLines, bool showMidpoint, int overnightStart, int overnightEnd, int sessionEndTime, DashStyleHelper lineStyle, int lineWidth, bool showCurrentWeekOnly, DashStyleHelper midpointLineStyle, bool showLabels, string labelDateFormat, bool useAlternateLabelFormat, int labelVerticalOffset, int labelHorizontalOffset, bool showSessionShading, int sessionOpacity, bool showOpeningRange, int openingRangeMinutes, DashStyleHelper orLineStyle, bool showVolumeSpikes, int volumeLookback, double volumeSpikeThreshold, bool showSpikeLabels, bool showDailySma, int smaPeriod, DashStyleHelper smaLineStyle, int smaLineWidth, bool enableAlerts, bool alertOnONH, bool alertOnONL, bool alertOnDailySma)
		{
			return indicator.MarketOpsSuite(input, showOvernightLines, showMidpoint, overnightStart, overnightEnd, sessionEndTime, lineStyle, lineWidth, showCurrentWeekOnly, midpointLineStyle, showLabels, labelDateFormat, useAlternateLabelFormat, labelVerticalOffset, labelHorizontalOffset, showSessionShading, sessionOpacity, showOpeningRange, openingRangeMinutes, orLineStyle, showVolumeSpikes, volumeLookback, volumeSpikeThreshold, showSpikeLabels, showDailySma, smaPeriod, smaLineStyle, smaLineWidth, enableAlerts, alertOnONH, alertOnONL, alertOnDailySma);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MarketOpsSuite MarketOpsSuite(bool showOvernightLines, bool showMidpoint, int overnightStart, int overnightEnd, int sessionEndTime, DashStyleHelper lineStyle, int lineWidth, bool showCurrentWeekOnly, DashStyleHelper midpointLineStyle, bool showLabels, string labelDateFormat, bool useAlternateLabelFormat, int labelVerticalOffset, int labelHorizontalOffset, bool showSessionShading, int sessionOpacity, bool showOpeningRange, int openingRangeMinutes, DashStyleHelper orLineStyle, bool showVolumeSpikes, int volumeLookback, double volumeSpikeThreshold, bool showSpikeLabels, bool showDailySma, int smaPeriod, DashStyleHelper smaLineStyle, int smaLineWidth, bool enableAlerts, bool alertOnONH, bool alertOnONL, bool alertOnDailySma)
		{
			return indicator.MarketOpsSuite(Input, showOvernightLines, showMidpoint, overnightStart, overnightEnd, sessionEndTime, lineStyle, lineWidth, showCurrentWeekOnly, midpointLineStyle, showLabels, labelDateFormat, useAlternateLabelFormat, labelVerticalOffset, labelHorizontalOffset, showSessionShading, sessionOpacity, showOpeningRange, openingRangeMinutes, orLineStyle, showVolumeSpikes, volumeLookback, volumeSpikeThreshold, showSpikeLabels, showDailySma, smaPeriod, smaLineStyle, smaLineWidth, enableAlerts, alertOnONH, alertOnONL, alertOnDailySma);
		}

		public Indicators.MarketOpsSuite MarketOpsSuite(ISeries<double> input , bool showOvernightLines, bool showMidpoint, int overnightStart, int overnightEnd, int sessionEndTime, DashStyleHelper lineStyle, int lineWidth, bool showCurrentWeekOnly, DashStyleHelper midpointLineStyle, bool showLabels, string labelDateFormat, bool useAlternateLabelFormat, int labelVerticalOffset, int labelHorizontalOffset, bool showSessionShading, int sessionOpacity, bool showOpeningRange, int openingRangeMinutes, DashStyleHelper orLineStyle, bool showVolumeSpikes, int volumeLookback, double volumeSpikeThreshold, bool showSpikeLabels, bool showDailySma, int smaPeriod, DashStyleHelper smaLineStyle, int smaLineWidth, bool enableAlerts, bool alertOnONH, bool alertOnONL, bool alertOnDailySma)
		{
			return indicator.MarketOpsSuite(input, showOvernightLines, showMidpoint, overnightStart, overnightEnd, sessionEndTime, lineStyle, lineWidth, showCurrentWeekOnly, midpointLineStyle, showLabels, labelDateFormat, useAlternateLabelFormat, labelVerticalOffset, labelHorizontalOffset, showSessionShading, sessionOpacity, showOpeningRange, openingRangeMinutes, orLineStyle, showVolumeSpikes, volumeLookback, volumeSpikeThreshold, showSpikeLabels, showDailySma, smaPeriod, smaLineStyle, smaLineWidth, enableAlerts, alertOnONH, alertOnONL, alertOnDailySma);
		}
	}
}

#endregion
