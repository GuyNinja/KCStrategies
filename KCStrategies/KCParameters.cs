// --- KCAlgoBase_Properties.cs ---
// Version 6.6.1 - Fix BE Logic Compile Errors
// Key Changes:
// 1. FIX: Added the BETriggerMode enum and the associated properties (BETriggerTicks) to this file, where they belong.
// 2. REFACTOR: ICustomTypeDescriptor is updated to hide/show the correct BE Trigger property based on the selected mode.

#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
#endregion

//This is a partial class file.
//It contains all NinjaScript properties (the user-configurable parameters) and the
//ICustomTypeDescriptor implementation for dynamically showing/hiding them in the UI.
namespace NinjaTrader.NinjaScript.Strategies.KCStrategies
{
    public abstract partial class KCAlgoBase : Strategy, ICustomTypeDescriptor
    {
		#region Enums
        public enum BETriggerMode { FixedTicks, ProfitTargetPercentage }
		#endregion

		#region Property Backing Fields
		private bool isEnableTime2;
        private bool isEnableTime3;
        private bool isEnableTime4;
        private bool isEnableTime5;
        private bool isEnableTime6 = true;
		#endregion
		
        #region Default Property Values
        protected virtual void SetPropertyDefaults()
        {			
			IsAutoScale                 = false;
	        DrawOnPricePanel            = false;
	        IsOverlay                   = true;
			
			// 01. Core Strategy Settings
			FilterMode 						= MasterTrendFilterMode.MED; 
			VmaSlopeThreshold				= 0.05;
			MedHmaPeriod 					= 233; 
			MedExtremesLookback 			= 14; 
			MedOffsetTicks 					= 0; 
			MedUpColor 						= Brushes.Lime; 
			MedDownColor 					= Brushes.Red;
            EnableConfluenceScoring   		= false;
            MinConfluenceScore        		= 70;
            ConfluenceAdxThreshold    		= 25;
            ManualRegimeOverride 			= MarketRegime.Undefined;
            EnableAutoRegimeDetection 		= true;
			EnableTrendBots 				= true;
			EnableRangeBots 				= true;
			EnableBreakoutBots 				= true;
			
			// 02. Core Analysis Indicators
			EnableMomo						= true;
			MomentumPeriod					= 9;
			MomoThreshold					= 10;
			EnableDM						= true;
			DmPeriod						= 14;
			AdxThreshold					= 25;
            RegimeAdxPeriod 				= 14;
            RegimeAdxTrendThreshold 		= 25;
			RegimeAdxTrendThreshold2 		= 50;
            RegimeAdxRangeThreshold 		= 25;
            RegimeBBPeriod 					= 20;
            RegimeBBStdDev 					= 2;
            RegimeSqueezeLookback 			= 50;

			// 03. Order Entry & Sizing
			OrderType						= OrderType.Limit;
			LimitOffset						= 4;	
			Contracts						= 1;
			EnableDynamicSizing 			= false;
			RiskPerTradePercent 			= 2;
			ManagementMode 					= TradeManagementMode.Static;
			DynamicInitialSL 				= 73;
			DynamicInitialTP 				= 150;
			DynamicSLPadding 				= 4;
			DynamicBurnInTrades 			= 30;
			DynamicAvgLookback 				= 20;
			DynamicRiskMode 				= DynamicCalculationMode.Percentile;
			DynamicRiskPercentile 			= 80;
			
			// 04. Trade Management (Risk)
			StopType 						= StopManagementType.DynamicTrail;				
			InitialStop						= 73;
			HighLowTrailInitialLookback		= 4;
			InitialTrailTicks 				= 40;
			FinalTrailTriggerPercent 		= 70;
			FinalTrailTicks 				= 20;
            PSARAcceleration            	= 0.01;
            PSARAccelerationMax         	= 0.2;
			TrailBarsLookback				= 0;
			ManualMoveStopLookback			= 0;
			AtrPeriod						= 20;
			atrMultiplier                   = 2.5;
			AtrTrailTriggerPercent			= 70;
			AtrFinalMultiplier				= 0.5;
			BESetAuto						= true;
			BreakevenTriggerMode 			= BETriggerMode.ProfitTargetPercentage;
			BETriggerTicks					= 20;
			BE_Offset						= 4;
			EnableAutoExit					= true;
			dailyLossProfit					= true;
			DailyProfitLimit				= 10000;
			DailyLossLimit					= 1000;				
			enableTrailingDrawdown 			= true;
			TrailingDrawdown				= 1000;
			
			// 05. Trade Management (Profit)
			PTType                          = ProfitTargetType.Fixed;
            ProfitTarget					= 120;
			RiskRewardRatio					= 1.3;
			EnableProfitTarget2				= true;
			Contracts2 						= 1;
            ProfitTarget2					= 90;
			EnableProfitTarget3				= true;
			Contracts3 					    = 1;
            ProfitTarget3					= 100;
			EnableProfitTarget4				= true;				
			Contracts4						= 1;
            ProfitTarget4					= 110;
			CloseQty						= 1;
			
			// 06. Market Condition Filters
            EnableChopDetection 			= true;
            ChopFilterPeriod 				= 20;
            ChopAdxThreshold 				= 25;
            FlatSlopeThreshold 				= 0.1;
            VolmaFilterPeriod 				= 20;
			ChopVolmaThreshold				= 1000;
            ExhaustionBBPeriod 				= 20;
            ExhaustionBBStdDev 				= 2.0;
            EnableOverboughtOversoldFilter 	= true; 
            RsiFilterPeriod                	= 14;
            RsiOverboughtLevel             	= 70;
            RsiOversoldLevel               	= 30;
            EnableMarketStructureFilter 	= false;
			enableDailyOpen					= false;
			
			// 07. Session & Time Controls
            EnableTradingDaysFilter 		= true;
            TradeOnMonday    				= true;
            TradeOnTuesday   				= true;
            TradeOnWednesday 				= true;
            TradeOnThursday 				= true;
            TradeOnFriday    				= true;
			Start							= DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
			End								= DateTime.Parse("11:30", System.Globalization.CultureInfo.InvariantCulture);
			Start2							= DateTime.Parse("11:30", System.Globalization.CultureInfo.InvariantCulture);
			End2							= DateTime.Parse("12:30", System.Globalization.CultureInfo.InvariantCulture);
			Start3							= DateTime.Parse("14:30", System.Globalization.CultureInfo.InvariantCulture);
			End3							= DateTime.Parse("16:00", System.Globalization.CultureInfo.InvariantCulture);
			Start4							= DateTime.Parse("04:00", System.Globalization.CultureInfo.InvariantCulture);
			End4							= DateTime.Parse("07:00", System.Globalization.CultureInfo.InvariantCulture);
			Start5							= DateTime.Parse("00:00", System.Globalization.CultureInfo.InvariantCulture);
			End5							= DateTime.Parse("02:00", System.Globalization.CultureInfo.InvariantCulture);
			Start6							= DateTime.Parse("00:00", System.Globalization.CultureInfo.InvariantCulture);
			End6							= DateTime.Parse("23:59", System.Globalization.CultureInfo.InvariantCulture);
			TradesPerDirection				= false;
			longPerDirection				= 5;
			shortPerDirection				= 5;
			
			// Bot Defaults (Grouped for simplicity, individual sections in UI)
			RegChanPlusPeriod 				= 35; 
			RegChanPlusStdDevWidth 			= 3; 
			RegChanPlusBandSmooth 			= 5; 
			RegChanPlusSlopeThreshold 		= 0.1; 
			RegChanPlusVolaLookback 		= 5;
			ShowRegChan 					= false;
			PullbackMedPeriod = 34; PullbackMedLookback = 14;  
			MvdVmaPeriod = 34; MvdVolatilityPeriod = 14; MvdExtremesLookback = 14; MvdOffsetTicks = 0; MvdUpColor = Brushes.DeepSkyBlue; MvdDownColor = Brushes.HotPink; MvdDriverWidth = 3;
			LinRegPeriod = 14; LinRegSlopeThreshold = 0.05;
			HmaHooksPeriod = 14; 
			SwingStrength = 5;
			CmoPeriod = 14; CmoOverboughtLevel = 50; CmoOversoldLevel = -50; LookbackPeriod = 14; SmoothingPeriod = 10;
			SMIEMAPeriod = 5;
            SuperTrendPeriod = 20; SuperTrendPoles = 2;
			JbSignalMacdFast = 5; JbSignalMacdSlow = 8; JbSignalMacdSmooth = 5; JbSignalWrPeriod = 21; JbSignalWrEmaPeriod = 13; JbSignalAlmaFastLen = 19; JbSignalAlmaSlowLen = 20;
			RsiPeriod = 14; RsiOverbought = 70; RsiOversold = 30;
			StochPeriodD = 3; StochPeriodK = 14; StochSmooth = 3;
			BollingerPeriod = 20; BollingerStdDev = 2;
			TrendMagicCciPeriod = 20; TrendMagicAtrPeriod = 14; TrendMagicAtrMult = 0.1;
			TSSuperTrendLength = 14; TSSuperTrendMultiplier = 2.618; TSSuperTrendMaType = MAType.HMA; TSSuperTrendSmooth = 14;
			T3FVolumeFactor = 0.5; T3FPeriod1 = 1; T3FPeriod2 = 1; T3FPeriod3 = 1; T3FPeriod4 = 1; T3FPeriod5 = 9;				
			wrPeriod = 14; wrUp = -20; wrDown = -80;
			AndeanLength = 9; AndeanSignalLength = 5; AndeanLookback = 1;	
			Bala_Dev1 = 2.0; Bala_XL1 = 50.0; Bala_XS1 = 50.0; Bala_RSIPeriod1 = 21; Bala_EMAPeriod1 = 21; Bala_Dev2 = 1.0; Bala_XL2 = 50.0; Bala_XS2 = 49.0; Bala_RSIPeriod2 = 21; Bala_EMAPeriod2 = 21;
			CoralSmoothingPeriod = 6; CoralConstantD = 1.0;				
			MarketStructurePeriod = 5; MarketStructureUseReversals = true; MarketStructureUseContinuations = false;
			SMIEMAPeriod1 = 25; SMIEMAPeriod2 = 2; SMIRange = 13; SMIEMAPeriod = 5;
			Zombie9MacdFast = 8; Zombie9MacdSlow = 21; Zombie9MacdSmooth = 5; Zombie9SmaPeriod = 618;
			TTMBollingerDeviation = 2; TTMBollingerPeriod = 21; TTMKeltnerMultiplier = 2; TTMKeltnerPeriod = 21; TTMMomentumPeriod = 14; TTMMomentumEMAPeriod = 14;
			RFSamplingPeriod = 10; RFRangeMultiplier = 1.0;
			ZigZagDeviationType = DeviationType.Points; ZigZagDeviationValue = 0.5; ZigZagUseHighLow = true;
			KeltnerOffsetMultiplier = 1.5; KeltnerPeriod = 10;
			EngulfingEngulfBody = false; EngulfingTickOffset = 0;
			ORBStartTime = new DateTime(2000, 1, 1, 9, 30, 0); ORBEndTime = new DateTime(2000, 1, 1, 9, 50, 0); ORBTimeZone = "Eastern Standard Time"; ORBUseBreakoutSignals = true; ORBUseReversalSignals = false;
			SvePivotsRangeType = NTSvePivotRange.Daily; SvePivotsCalcMode = NTSveHLCCalculationMode.CalcFromIntradayData; SvePivotsWidth = 100; SvePivotsUseR1 = true; SvePivotsUseR2 = true; SvePivotsUseR3 = true; SvePivotsUseS1 = true; SvePivotsUseS2 = true; SvePivotsUseS3 = true;
			TefaUseMACD = true; TefaUseWilliamsR = true; TefaUseCCI = true; TefaUseMomentum = true; TefaUseStochastics = true; TefaUseRangeFilter = true; TefaUseUltimateMA = true; TefaUseOpenCloseCondition = true; TefaUseHighLowCondition = false; TefaFast = 5; TefaSlow = 8; TefaSmooth = 5; TefaWilliamsRPeriod = 21; TefaWilliamsREMAPeriod = 13; TefaCCIPeriod = 21; TefaCCIEMAPeriod = 13; TefaMomentumPeriod = 21; TefaMomentumEMAPeriod = 13; TefaStochasticKPeriod = 21; TefaStochasticDPeriod = 13; TefaStochasticSmooth = 3; TefaSamplingPeriod = 10; TefaRangeMultiplier = 1.0; TefaSelectedMAType = MAType.SMA; TefaPeriodMA = 5; TefaLookBackBars = 2;
			ShowPivotImpulseLines = true; PIS_SwingStrength = 34; PIS_PivotLookback = 200;
			
			// 11. Visuals & Diagnostics
			showDailyPnl					= true;
			EnableTrendBackground			= true;
			FontSize						= 15;
			Transparency					= 50;
			PositionDailyPNL				= TextPosition.BottomLeft;	
			colorDailyProfitLoss			= Brushes.Cyan;
			showPnl							= false;
			PositionPnl						= TextPosition.TopLeft;
			colorPnl 						= Brushes.Yellow;
			ShowHistorical					= true;
			EnableLogging 					= false;
			EnableTradeLogging 				= true;
			TradeLogFileName   				= "KCAlgoBase_Trades.csv";				
			EnableJsonLogging 				= true;
			JsonLogFileName   				= "KCAlgoBase_Trades.jsonl";                
	        EnableHealthChecks 				= true;
	        DataLossTimeoutSeconds 			= 15;
	        MaxConsecutiveRejections 		= 4;
	        ShowOpen                    	= true;
	        ShowHigh                    	= true;
	        ShowLow                     	= true;
			ShowMomentumExtremesPlot 		= true; 
			ShowMomentumVmaPlot 			= true; 
			ShowPSARPlot					= true;				
			ShowDM							= false;
			ShowMomo						= true;
            ShowExhaustionBB 				= false;
			
			// 12. Advanced Execution
			EnableScaleInExecution 			= true;
			ScaleInChunks 					= 2;
			ScaleInDelaySeconds 			= 2;
			
			// 13. About
			BaseAlgoVersion					= "KCAlgoBase v6.6.1";
			Author							= "indiVGA, Khanh Nguyen, Oshi, Johny, based on ArchReactor";
			StrategyName 					= "KCAlgoBase";
			StrategyVersion					= "6.6.1 Aug 2025";
			Credits							= "";
			ChartType						= "Tbars 28";	
			paypal 							= "https://www.paypal.com/signin"; 		
			
			// Uncategorized legacy fields
			BE_Realized						= false;
			counterLong						= 0;
			counterShort					= 0;
			maxProfit 						= double.MinValue;
		}
		#endregion
		
		#region Custom Property Manipulation (ICustomTypeDescriptor)
		
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            PropertyDescriptorCollection col = TypeDescriptor.GetProperties(GetType(), attributes);
            PropertyDescriptorCollection filteredCol = new PropertyDescriptorCollection(null);

			foreach(PropertyDescriptor d in col) filteredCol.Add(d);
			
            if (!EnableTradingDaysFilter)
            {
                filteredCol.Remove(filteredCol.Find("TradeOnMonday", true)); filteredCol.Remove(filteredCol.Find("TradeOnTuesday", true)); 
				filteredCol.Remove(filteredCol.Find("TradeOnWednesday", true)); filteredCol.Remove(filteredCol.Find("TradeOnThursday", true)); 
				filteredCol.Remove(filteredCol.Find("TradeOnFriday", true));
            }
			if (!TradesPerDirection) { filteredCol.Remove(filteredCol.Find("longPerDirection", true)); filteredCol.Remove(filteredCol.Find("shortPerDirection", true)); }
			if (!Time2) { filteredCol.Remove(filteredCol.Find("Start2", true)); filteredCol.Remove(filteredCol.Find("End2", true)); }
			if (!Time3) { filteredCol.Remove(filteredCol.Find("Start3", true)); filteredCol.Remove(filteredCol.Find("End3", true)); }
			if (!Time4) { filteredCol.Remove(filteredCol.Find("Start4", true)); filteredCol.Remove(filteredCol.Find("End4", true)); }
			if (!Time5) { filteredCol.Remove(filteredCol.Find("Start5", true)); filteredCol.Remove(filteredCol.Find("End5", true)); }
			if (!Time6) { filteredCol.Remove(filteredCol.Find("Start6", true)); filteredCol.Remove(filteredCol.Find("End6", true)); }
			if (!BESetAuto) 
			{ 
				filteredCol.Remove(filteredCol.Find("BreakevenTriggerMode", true));
				filteredCol.Remove(filteredCol.Find("BETriggerTicks", true)); 
				filteredCol.Remove(filteredCol.Find("BE_Offset", true)); 
			}
			
			if (PTType != ProfitTargetType.RiskRewardRatio && PTType != ProfitTargetType.ATR) 
			{ 
				filteredCol.Remove(filteredCol.Find("RiskRewardRatio", true)); 
			}
			
            if (PTType != ProfitTargetType.Fixed)
            {
                filteredCol.Remove(filteredCol.Find("ProfitTarget", true));
            }

            if (StopType != StopManagementType.ATRTrail) 
            { 
                filteredCol.Remove(filteredCol.Find("AtrPeriod", true)); 
                filteredCol.Remove(filteredCol.Find("atrMultiplier", true)); 
				filteredCol.Remove(filteredCol.Find("AtrTrailTriggerPercent", true)); 
                filteredCol.Remove(filteredCol.Find("AtrFinalMultiplier", true)); 
            }
            if (StopType != StopManagementType.DynamicTrail) {
                filteredCol.Remove(filteredCol.Find("InitialTrailTicks", true)); 
                filteredCol.Remove(filteredCol.Find("FinalTrailTriggerPercent", true));
				filteredCol.Remove(filteredCol.Find("FinalTrailTicks", true));
            }
            if (StopType != StopManagementType.ParabolicTrail) {
				filteredCol.Remove(filteredCol.Find("ShowPSARPlot", true)); 
				filteredCol.Remove(filteredCol.Find("PSARAcceleration", true));
				filteredCol.Remove(filteredCol.Find("PSARAccelerationMax", true));				
				filteredCol.Remove(filteredCol.Find("TrailBarsLookback", true)); 
            }
			if (StopType != StopManagementType.HighLowTrail) {
				filteredCol.Remove(filteredCol.Find("HighLowTrailInitialLookback", true));
			}
			
			if (ManagementMode == TradeManagementMode.Dynamic)
			{
			    filteredCol.Remove(filteredCol.Find("InitialStop", true));
			    filteredCol.Remove(filteredCol.Find("ProfitTarget", true));
				
				if (DynamicRiskMode != DynamicCalculationMode.Percentile)
				{
					filteredCol.Remove(filteredCol.Find("DynamicRiskPercentile", true));
				}
			}
			else // Static Mode
			{
			    filteredCol.Remove(filteredCol.Find("DynamicInitialSL", true));
			    filteredCol.Remove(filteredCol.Find("DynamicInitialTP", true));
			    filteredCol.Remove(filteredCol.Find("DynamicSLPadding", true));
				filteredCol.Remove(filteredCol.Find("DynamicAvgLookback", true));
				filteredCol.Remove(filteredCol.Find("DynamicBurnInTrades", true));
				filteredCol.Remove(filteredCol.Find("DynamicRiskMode", true));
				filteredCol.Remove(filteredCol.Find("DynamicRiskPercentile", true));
				filteredCol.Remove(filteredCol.Find("EnableDynamicRunner", true));
				filteredCol.Remove(filteredCol.Find("DynamicRunnerTrailTicks", true));
			}

			if (!EnableDynamicSizing)
			{
				filteredCol.Remove(filteredCol.Find("RiskPerTradePercent", true));
			}
			
            if (FilterMode == MasterTrendFilterMode.VMA)
            {
				filteredCol.Remove(filteredCol.Find("MedHmaPeriod", true));
				filteredCol.Remove(filteredCol.Find("MedExtremesLookback", true));
				filteredCol.Remove(filteredCol.Find("MedOffsetTicks", true));
				filteredCol.Remove(filteredCol.Find("MedUpColor", true));
				filteredCol.Remove(filteredCol.Find("MedDownColor", true));
            }
            if (FilterMode == MasterTrendFilterMode.MED)
            {
                filteredCol.Remove(filteredCol.Find("VmaSlopeThreshold", true));
				filteredCol.Remove(filteredCol.Find("MvdVmaPeriod", true));
				filteredCol.Remove(filteredCol.Find("MvdVolatilityPeriod", true));
				filteredCol.Remove(filteredCol.Find("MvdExtremesLookback", true));
				filteredCol.Remove(filteredCol.Find("MvdOffsetTicks", true));
				filteredCol.Remove(filteredCol.Find("MvdUpColor", true));
				filteredCol.Remove(filteredCol.Find("MvdDownColor", true));
				filteredCol.Remove(filteredCol.Find("MvdDriverWidth", true));
            }
            
            return filteredCol;
        }

        public AttributeCollection GetAttributes() { return TypeDescriptor.GetAttributes(GetType()); }
        public string GetClassName() { return TypeDescriptor.GetClassName(GetType()); }
        public string GetComponentName() { return TypeDescriptor.GetComponentName(GetType()); }
        public TypeConverter GetConverter() { return TypeDescriptor.GetConverter(GetType()); }
        public EventDescriptor GetDefaultEvent() { return TypeDescriptor.GetDefaultEvent(GetType()); }
        public PropertyDescriptor GetDefaultProperty() { return TypeDescriptor.GetDefaultProperty(GetType()); }
        public object GetEditor(Type editorBaseType) { return TypeDescriptor.GetEditor(GetType(), editorBaseType); }
        public EventDescriptorCollection GetEvents(Attribute[] attributes) { return TypeDescriptor.GetEvents(GetType(), attributes); }
        public EventDescriptorCollection GetEvents() { return TypeDescriptor.GetEvents(GetType()); }
        public PropertyDescriptorCollection GetProperties() { return GetProperties(null); }
        public object GetPropertyOwner(PropertyDescriptor pd) { return this; }
		#endregion		
	
		#region Properties (User-Configurable Parameters)

		#region 01. Core Strategy Settings
		[NinjaScriptProperty, Display(Name = "Master Trend Filter", Order = 1, GroupName = "01. Core Strategy Settings", Description="Selects the master trend filter logic to apply during TRENDING markets.")]
        [RefreshProperties(RefreshProperties.All)] public MasterTrendFilterMode FilterMode { get; set; }
		[NinjaScriptProperty, Range(0.0, double.MaxValue), Display(Name = "VMA Slope Threshold", Description="The minimum slope the VMA must have to be considered trending. Helps filter chop.", Order = 4, GroupName = "01. Core Strategy Settings")] public double VmaSlopeThreshold { get; set; }
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name = "MED Filter: HMA Period", GroupName = "01. Core Strategy Settings", Order = 7)] public int MedHmaPeriod { get; set; }
		[NinjaScriptProperty, Range(2, int.MaxValue), Display(Name = "MED Filter: Extremes Lookback", GroupName = "01. Core Strategy Settings", Order = 8)] public int MedExtremesLookback { get; set; }
		[NinjaScriptProperty, Range(0, int.MaxValue), Display(Name = "MED Filter: Offset Ticks", GroupName = "01. Core Strategy Settings", Order = 9)] public int MedOffsetTicks { get; set; }
		[NinjaScriptProperty, XmlIgnore, Display(Name = "MED Filter: Up Color", GroupName = "01. Core Strategy Settings", Order = 10)] public Brush MedUpColor { get; set; }
		[Browsable(false)] public string MedUpColorSerializable { get { return Serialize.BrushToString(MedUpColor); } set { MedUpColor = Serialize.StringToBrush(value); } }
		[NinjaScriptProperty, XmlIgnore, Display(Name = "MED Filter: Down Color", GroupName = "01. Core Strategy Settings", Order = 11)] public Brush MedDownColor { get; set; }
		[Browsable(false)] public string MedDownColorSerializable { get { return Serialize.BrushToString(MedDownColor); } set { MedDownColor = Serialize.StringToBrush(value); } }
        [NinjaScriptProperty, Display(Name = "Enable Confluence Scoring", Order = 12, GroupName = "01. Core Strategy Settings", Description = "If true, it finds all signals on a bar and scores them based on market conditions to find the highest-probability trade.")]
        [RefreshProperties(RefreshProperties.All)] public bool EnableConfluenceScoring { get; set; }
        [NinjaScriptProperty, Range(0, 100), Display(Name = "Min Confluence Score", Order = 13, GroupName = "01. Core Strategy Settings", Description = "The minimum score (0-100) a signal must achieve to be considered for an entry.")]
        public int MinConfluenceScore { get; set; }
        [NinjaScriptProperty, Range(10, 50), Display(Name = "Confluence ADX Threshold", Order = 14, GroupName = "01. Core Strategy Settings", Description = "The ADX value used by the scoring engine to determine if the market is 'trending' vs 'choppy'.")]
        public int ConfluenceAdxThreshold { get; set; }
        [NinjaScriptProperty, Display(Name = "Manual Regime Override", Order = 15, GroupName = "01. Core Strategy Settings", Description="Set a specific regime to trade, ignoring the auto-detector. Set to 'Undefined' to use auto-detection.")]
        public MarketRegime ManualRegimeOverride { get; set; }
        [NinjaScriptProperty, Display(Name = "Enable Auto Regime Detection", Order = 16, GroupName = "01. Core Strategy Settings", Description="If true, the strategy will attempt to automatically classify the market state.")]
        public bool EnableAutoRegimeDetection { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Trend Bots", GroupName = "01. Core Strategy Settings", Order = 17)] public bool EnableTrendBots { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Range Bots", GroupName = "01. Core Strategy Settings", Order = 18)] public bool EnableRangeBots { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Breakout Bots", GroupName = "01. Core Strategy Settings", Order = 19)] public bool EnableBreakoutBots { get; set; }
		#endregion
		
		#region 02. Core Analysis Indicators
		[NinjaScriptProperty, Display(Name = "Enable Momentum", Order = 1, GroupName = "02. Core Analysis Indicators")]
		public bool EnableMomo { get; set; }
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name="Momentum Period", Order = 2, GroupName="02. Core Analysis Indicators")]
		public int MomentumPeriod { get; set; }
		[NinjaScriptProperty, Display(Name="Momo Power Threshold", Description="The minimum Momentum value required to confirm a strong trend.", Order = 3, GroupName="02. Core Analysis Indicators")]
		public double MomoThreshold { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable DM (ADX)", Order = 4, GroupName = "02. Core Analysis Indicators")]
		public bool EnableDM { get; set; }
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name = "DM Period", Order = 5, GroupName = "02. Core Analysis Indicators")]
		public int DmPeriod { get; set; }
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name = "ADX Trend Threshold", Order = 6, GroupName = "02. Core Analysis Indicators", Description="The ADX value above which the market is considered to be trending.")]
		public int AdxThreshold { get; set; }
		[NinjaScriptProperty, Display(Name = "Regime ADX Period", Order = 7, GroupName = "02. Core Analysis Indicators")]
        public int RegimeAdxPeriod { get; set; }
        [NinjaScriptProperty, Display(Name = "Regime ADX Min Trend Threshold", Order = 8, GroupName = "02. Core Analysis Indicators", Description="ADX value ABOVE which the market is considered TRENDING.")]
        public int RegimeAdxTrendThreshold { get; set; }
        [NinjaScriptProperty, Display(Name = "Regime ADX Max Trend Threshold", Order = 9, GroupName = "02. Core Analysis Indicators", Description="ADX value BELOW which the trade is likely profitable.")]
        public int RegimeAdxTrendThreshold2 { get; set; }
        [NinjaScriptProperty, Display(Name = "Regime ADX Range Threshold", Order = 10, GroupName = "02. Core Analysis Indicators", Description="ADX value BELOW which the market is considered RANGING.")]
        public int RegimeAdxRangeThreshold { get; set; }
        [NinjaScriptProperty, Display(Name = "Regime BB Period", Order = 11, GroupName = "02. Core Analysis Indicators")]
        public int RegimeBBPeriod { get; set; }
        [NinjaScriptProperty, Display(Name = "Regime BB StdDev", Order = 12, GroupName = "02. Core Analysis Indicators")]
        public double RegimeBBStdDev { get; set; }
        [NinjaScriptProperty, Display(Name = "Regime Squeeze Lookback", Order = 13, GroupName = "02. Core Analysis Indicators", Description="Looks for the tightest Bollinger Band Width over this many bars to identify a Breakout setup.")]
        public int RegimeSqueezeLookback { get; set; }
		#endregion

		#region 03. Order Entry & Sizing
		[NinjaScriptProperty, Display(Name = "Order Type", Order = 1, GroupName = "03. Order Entry & Sizing")] public OrderType OrderType { get; set; } 
		[NinjaScriptProperty, Range(0, int.MaxValue), Display(Name="Limit Order Offset", Order= 2, GroupName="03. Order Entry & Sizing", Description="Number of ticks away from the market to place a limit order.")] public int LimitOffset { get; set; }	
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name="Contracts", Order= 3, GroupName="03. Order Entry & Sizing")] public int Contracts { get; set; }	
		[NinjaScriptProperty, RefreshProperties(RefreshProperties.All), Display(Name = "Enable Dynamic Sizing", GroupName = "03. Order Entry & Sizing", Order = 4, Description = "Automatically calculate position size based on account risk.")]
		public bool EnableDynamicSizing { get; set; }
		[NinjaScriptProperty, Display(Name = "Risk Per Trade (%)", GroupName = "03. Order Entry & Sizing", Order = 5, Description = "The percentage of account net liquidation to risk on each trade."), Range(0.1, 100.0)]
		public double RiskPerTradePercent { get; set; }
		[NinjaScriptProperty, Display(Name = "Trade Management Mode", GroupName = "03. Order Entry & Sizing", Order = 6), RefreshProperties(RefreshProperties.All)]
		public TradeManagementMode ManagementMode { get; set; }
		[NinjaScriptProperty, Display(Name = "Dynamic: Initial SL Ticks (Fallback)", Description="The Stop Loss to use until the system has learned a value.", GroupName = "03. Order Entry & Sizing", Order = 7)]
		public double DynamicInitialSL { get; set; }
		[NinjaScriptProperty, Display(Name = "Dynamic: Initial TP Ticks (Fallback)", Description="The Profit Target to use until the system has learned a value.", GroupName = "03. Order Entry & Sizing", Order = 8)]
		public double DynamicInitialTP { get; set; }
		[NinjaScriptProperty, Display(Name = "Dynamic: SL Padding (Ticks)", Description="Extra ticks to add to the learned Max Drawdown for the stop.", GroupName = "03. Order Entry & Sizing", Order = 9)]
		public double DynamicSLPadding { get; set; }
		[NinjaScriptProperty, Display(Name = "Dynamic: Burn-In Trades", Description="The number of trades to execute before the dynamic SL/TP adjustments become active.", GroupName = "03. Order Entry & Sizing", Order = 10), Range(1, 100)]
		public int DynamicBurnInTrades { get; set; }
		[NinjaScriptProperty, Display(Name = "Dynamic: Averaging Lookback", Description="The number of recent trades to average for the dynamic SL/TP calculation.", GroupName = "03. Order Entry & Sizing", Order = 11), Range(1, 100)]
		public int DynamicAvgLookback { get; set; }
		[NinjaScriptProperty, Display(Name = "Dynamic: Calculation Mode", Description="The statistical method used for dynamic risk calculation.", GroupName = "03. Order Entry & Sizing", Order = 12), RefreshProperties(RefreshProperties.All)]
		public DynamicCalculationMode DynamicRiskMode { get; set; }
		[NinjaScriptProperty, Display(Name = "Dynamic: Percentile", Description="The percentile (1-99) to use for calculation. Higher values are more conservative.", GroupName = "03. Order Entry & Sizing", Order = 13), Range(1, 99)]
		public int DynamicRiskPercentile { get; set; }
		#endregion

		#region 04. Trade Management (Risk)
		[NinjaScriptProperty, RefreshProperties(RefreshProperties.All), Display(Name = "Stop Management Type", GroupName = "04a. Stop Management - General", Order = 1, Description="Determines the logic used for managing the stop loss after entry.")] public StopManagementType StopType { get; set; }
		[NinjaScriptProperty, Display(Name="Initial Stop (Fixed)", Order= 2, GroupName="04a. Stop Management - General", Description="The initial stop loss in ticks. Used for 'FixedStop' type and as a fallback for some trailing stop types.")] public double InitialStop { get; set; }
		[NinjaScriptProperty, Range(0, int.MaxValue), Display(Name="Manual Move Stop Lookback", Order= 3, GroupName="04a. Stop Management - General", Description="The number of bars to look back for a swing high/low when using the 'Move Trailstop' manual button.")] public int ManualMoveStopLookback { get; set; }
		[NinjaScriptProperty, RefreshProperties(RefreshProperties.All), Display(Name="Enable Auto Breakeven", Order= 4, GroupName="04a. Stop Management - General", Description="Automatically moves the stop loss to a breakeven point once the trigger is hit.")] public bool BESetAuto { get; set; }
		[NinjaScriptProperty, Display(Name="Breakeven Trigger Mode", Order=5, GroupName="04a. Stop Management - General"), RefreshProperties(RefreshProperties.All)]
		public BETriggerMode BreakevenTriggerMode { get; set; }
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name="BE Trigger Value", Order = 6, GroupName="04a. Stop Management - General", Description="The value (ticks or %) to trigger auto-breakeven.")]
		public int BETriggerTicks { get; set; }
		[NinjaScriptProperty, Display(Name="Breakeven Offset (Ticks)", Order = 7, GroupName="04a. Stop Management - General", Description="An offset in ticks for the breakeven price (e.g., a value of 4 sets the stop to Entry Price + 4 ticks).")]
		public int BE_Offset { get; set; }		
		[NinjaScriptProperty, Display(Name = "Enable Auto Exit (Momentum Cross)", Order = 8, GroupName = "04a. Stop Management - General", Description="If enabled, the position will be exited if momentum crosses its zero line against the trade direction.")]
		public bool EnableAutoExit { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Daily P/L Limit", Order = 9, GroupName = "04a. Stop Management - General", Description="Enables the daily profit and loss limits.")]
		public bool dailyLossProfit { get; set; }
		[NinjaScriptProperty, Range(0, double.MaxValue), Display(Name="Daily Profit Limit ($)", Order=10, GroupName="04a. Stop Management - General", Description="If the daily PnL exceeds this value, auto-trading is disabled for the day.")]
		public double DailyProfitLimit { get; set; }
		[NinjaScriptProperty, Range(0, double.MaxValue), Display(Name="Daily Loss Limit ($)", Order=11, GroupName="04a. Stop Management - General", Description="If the daily PnL falls below this value (e.g., -$2000), auto-trading is disabled for the day.")]
		public double DailyLossLimit { get; set; }	
		[NinjaScriptProperty, Display(Name = "Enable Trailing Drawdown", Order =12, GroupName = "04a. Stop Management - General", Description="Enables a trailing drawdown based on the peak profit reached during the session.")]
		public bool enableTrailingDrawdown { get; set; }
		[NinjaScriptProperty, Range(0, double.MaxValue), Display(Name="Trailing Drawdown ($)", Order=13, GroupName="04a. Stop Management - General", Description="The maximum allowed drawdown from the session's peak PnL. If hit, auto-trading is disabled.")]
		public double TrailingDrawdown { get; set; }

		[NinjaScriptProperty, Range(1, 20), Display(Name="Initial Lookback", Order = 1, GroupName="04c. Stop Management - High/Low Trailing")]
		public int HighLowTrailInitialLookback { get; set; }		
		
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name="Initial Trail (Ticks)", Order = 1, GroupName="04b. Stop Management - Dynamic Trailing", Description="The trailing stop distance in ticks, used after breakeven is achieved.")]
		public int InitialTrailTicks { get; set; }		
		[NinjaScriptProperty, Range(1, 99), Display(Name="Final Trail Trigger (%)", Order = 2, GroupName="04b. Stop Management - Dynamic Trailing", Description="The percentage of the profit target that must be reached to tighten the trail stop.")]
		public int FinalTrailTriggerPercent { get; set; }
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name="Final Trail (Ticks)", Order = 3, GroupName="04b. Stop Management - Dynamic Trailing", Description="The tighter trailing stop distance, used after the trigger percentage is hit.")]
		public int FinalTrailTicks { get; set; }
		
        [NinjaScriptProperty, Range(0, double.MaxValue), Display(Name = "PSAR Acceleration", GroupName = "04d. Stop Management - Parabolic Trailing", Order = 1)]
		public double PSARAcceleration { get; set; }
        [NinjaScriptProperty, Display(Name = "PSAR Accel Max", GroupName = "04d. Stop Management - Parabolic Trailing", Order = 2)]
		public double PSARAccelerationMax { get; set; }
		[NinjaScriptProperty, Display(Name="Trail Bars Lookback", Order = 3, GroupName="04d. Stop Management - Parabolic Trailing", Description="How many bars to look back for swing points for auto-breakeven.")]
		public int TrailBarsLookback { get; set; }		
		
		[NinjaScriptProperty, Display(Name="ATR Period", Order= 1, GroupName="04e. Stop Management - ATR Trailing")]
		public int AtrPeriod { get; set; }
		[NinjaScriptProperty, Display(Name="ATR Initial Multiplier", Order= 2, GroupName="04e. Stop Management - ATR Trailing")]
		public double atrMultiplier { get; set; }
		[NinjaScriptProperty, Range(1, 99), Display(Name="ATR Final Trail Trigger (%)", Description="The percentage of the profit target that must be reached to tighten the trail stop.", Order = 3, GroupName="04e. Stop Management - ATR Trailing")]
		public int AtrTrailTriggerPercent { get; set; }
		[NinjaScriptProperty, Range(0.1, double.MaxValue), Display(Name="ATR Final Multiplier", Description="The tighter ATR multiplier to use after the trigger percentage is hit.", Order= 4, GroupName="04e. Stop Management - ATR Trailing")]
		public double AtrFinalMultiplier { get; set; }
		#endregion
		
		#region 05. Trade Management (Profit)
		[NinjaScriptProperty, RefreshProperties(RefreshProperties.All), Display(Name="Profit Target Type", Order=1, GroupName="05. Trade Management (Profit)", Description="Determines how profit targets are calculated.\nFixed: User-defined tick values.\nRiskRewardRatio: Based on stop loss distance.\nATR: Based on ATR value.\nRegChan: Based on Regression Channel boundaries.")]
		public ProfitTargetType PTType { get; set; }
		[NinjaScriptProperty, Display(Name="Profit Target (Fixed)", Order=2, GroupName="05. Trade Management (Profit)", Description="The profit target in ticks (only used if Profit Target Type is 'Fixed').")] public double ProfitTarget { get; set; }
		[NinjaScriptProperty, Display(Name="Risk To Reward / ATR Multiplier", Order= 3, GroupName="05. Trade Management (Profit)", Description="The desired multiplier. Used for both RiskRewardRatio and ATR Profit Target types.")] public double RiskRewardRatio { get; set; }
		[NinjaScriptProperty, Display(Name="Enable PT 2", Order= 4, GroupName="05. Trade Management (Profit)")] public bool EnableProfitTarget2 { get; set; }
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name="Contracts 2", Order= 5, GroupName="05. Trade Management (Profit)")] public int Contracts2 { get; set; }	
		[NinjaScriptProperty, Display(Name="Profit Target 2", Order=6, GroupName="05. Trade Management (Profit)")] public double ProfitTarget2 { get; set; }
		[NinjaScriptProperty, Display(Name="Enable PT 3", Order= 7, GroupName="05. Trade Management (Profit)")] public bool EnableProfitTarget3 { get; set; }
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name="Contracts 3", Order= 8, GroupName="05. Trade Management (Profit)")] public int Contracts3 { get; set; }	
		[NinjaScriptProperty, Display(Name="Profit Target 3", Order=9, GroupName="05. Trade Management (Profit)")] public double ProfitTarget3 { get; set; }
		[NinjaScriptProperty, Display(Name="Enable PT 4", Order= 10, GroupName="05. Trade Management (Profit)")] public bool EnableProfitTarget4 { get; set; }				
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name="Contracts 4", Order= 11, GroupName="05. Trade Management (Profit)")] public int Contracts4 { get; set; }	
		[NinjaScriptProperty, Display(Name="Profit Target 4", Order=12, GroupName="05. Trade Management (Profit)")] public double ProfitTarget4 { get; set; }
		[NinjaScriptProperty, Display(Name="Close Quantity (Manual Button)", Order= 13, GroupName="05. Trade Management (Profit)", Description="The number of contracts to close when using the 'Partial Close' manual button.")] public int CloseQty { get; set; }
		#endregion
		
		#region 06. Market Condition Filters
		[NinjaScriptProperty, Display(Name = "Enable Chop Detection", Order = 1, GroupName = "06. Market Condition Filters", Description="If enabled, uses a LinReg slope and ADX filter to detect and avoid choppy market conditions. This can override all other entry signals.")] public bool EnableChopDetection { get; set; } 
		[NinjaScriptProperty, Range(2, 100), Display(Name="Chop Filter Period", Order=2, GroupName="06. Market Condition Filters")] public int ChopFilterPeriod { get; set; }
		[NinjaScriptProperty, Range(0.1, 1.0), Display(Name="Flat Slope Threshold", Order=3, GroupName="06. Market Condition Filters")] public double FlatSlopeThreshold { get; set; }
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name="Chop ADX Threshold", Order=4, GroupName="06. Market Condition Filters")] public int ChopAdxThreshold { get; set; }		
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name="Chop VOLMA Threshold", Order=5, GroupName="06. Market Condition Filters", Description="If VOLMA is below this value (along with other conditions), the market is considered choppy.")] public double ChopVolmaThreshold { get; set; }		
        [NinjaScriptProperty, Range(1, 200), Display(Name = "VOLMA Filter Period", Order = 6, GroupName = "06. Market Condition Filters", Description = "The lookback period for the VOLMA.")]
        public int VolmaFilterPeriod { get; set; }
        [Browsable(false)] [NinjaScriptProperty, Display(Name = "Enable Exhaustion Filter (BBands)", Order = 9, GroupName = "06. Market Condition Filters", Description = "If true, prevents long entries when price is above the upper Bollinger Band and short entries when price is below the lower Bollinger Band.")]
        public bool EnableExhaustionFilter { get; set; }
        [NinjaScriptProperty, Range(1, int.MaxValue), Display(Name = "Exhaustion BB Period", Order = 10, GroupName = "06. Market Condition Filters")]
        public int ExhaustionBBPeriod { get; set; }
        [NinjaScriptProperty, Range(0.1, 5), Display(Name = "Exhaustion BB StdDev", Order = 11, GroupName = "06. Market Condition Filters")]
        public double ExhaustionBBStdDev { get; set; }
        [NinjaScriptProperty, Display(Name = "Enable OB/OS Filter (RSI)", Order = 12, GroupName = "06. Market Condition Filters", Description = "If true, prevents long entries when RSI is overbought and short entries when RSI is oversold.")]
        public bool EnableOverboughtOversoldFilter { get; set; }
        [NinjaScriptProperty, Range(1, int.MaxValue), Display(Name = "RSI Filter Period", Order = 13, GroupName = "06. Market Condition Filters")]
        public int RsiFilterPeriod { get; set; }
        [NinjaScriptProperty, Range(1, 100), Display(Name = "RSI Overbought Level", Order = 14, GroupName = "06. Market Condition Filters")]
        public int RsiOverboughtLevel { get; set; }
        [NinjaScriptProperty, Range(1, 100), Display(Name = "RSI Oversold Level", Order = 15, GroupName = "06. Market Condition Filters")]
        public int RsiOversoldLevel { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Enable Market Structure Filter", Order = 16, GroupName = "06. Market Condition Filters", Description = "If true, prevents long entries below a recent swing high and short entries above a recent swing low.")]
        public bool EnableMarketStructureFilter { get; set; }
		[NinjaScriptProperty, Display(Name="Daily Open Trend Filter", Order= 17, GroupName="06. Market Condition Filters", Description="If true, long entries require price to be above the daily open, and shorts require price to be below.")] public bool enableDailyOpen { get; set; }
		#endregion
		
		#region 07. Session & Time Controls
        [NinjaScriptProperty, RefreshProperties(RefreshProperties.All), Display(Name = "Enable Trading Days Filter", GroupName = "07. Session & Time Controls", Order = 1)] public bool EnableTradingDaysFilter { get; set; }
        [NinjaScriptProperty, Display(Name = "Trade on Monday", GroupName = "07. Session & Time Controls", Order = 2)] public bool TradeOnMonday { get; set; }
        [NinjaScriptProperty, Display(Name = "Trade on Tuesday", GroupName = "07. Session & Time Controls", Order = 3)] public bool TradeOnTuesday { get; set; }
        [NinjaScriptProperty, Display(Name = "Trade on Wednesday", GroupName = "07. Session & Time Controls", Order = 4)] public bool TradeOnWednesday { get; set; }
        [NinjaScriptProperty, Display(Name = "Trade on Thursday", GroupName = "07. Session & Time Controls", Order = 5)] public bool TradeOnThursday { get; set; }
        [NinjaScriptProperty, Display(Name = "Trade on Friday", GroupName = "07. Session & Time Controls", Order = 6)] public bool TradeOnFriday { get; set; }
		[NinjaScriptProperty, PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey"), Display(Name="Start Trades", Order=7, GroupName="07. Session & Time Controls")]
		public DateTime Start { get; set; }
		[NinjaScriptProperty, PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey"), Display(Name="End Trades", Order=8, GroupName="07. Session & Time Controls")]
		public DateTime End { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Time 2", Description = "Enable 2 times.", Order=9, GroupName = "07. Session & Time Controls"), RefreshProperties(RefreshProperties.All)]
		public bool Time2 { get{return isEnableTime2;} set{isEnableTime2 = (value);} }
		[NinjaScriptProperty, PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey"), Display(Name="Start Time 2", Order=10, GroupName="07. Session & Time Controls")]
		public DateTime Start2 { get; set; }
		[NinjaScriptProperty, PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey"), Display(Name="End Time 2", Order=11, GroupName="07. Session & Time Controls")]
		public DateTime End2 { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Time 3", Description = "Enable 3 times.", Order=12, GroupName = "07. Session & Time Controls"), RefreshProperties(RefreshProperties.All)]
		public bool Time3 { get{return isEnableTime3;} set{isEnableTime3 = (value);} }
		[NinjaScriptProperty, PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey"), Display(Name="Start Time 3", Order=13, GroupName="07. Session & Time Controls")]
		public DateTime Start3 { get; set; }
		[NinjaScriptProperty, PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey"), Display(Name="End Time 3", Order=14, GroupName="07. Session & Time Controls")]
		public DateTime End3 { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Time 4", Description = "Enable 4 times.", Order=15, GroupName = "07. Session & Time Controls"), RefreshProperties(RefreshProperties.All)]
		public bool Time4 { get{return isEnableTime4;} set{isEnableTime4 = (value);} }
		[NinjaScriptProperty, PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey"), Display(Name="Start Time 4", Order=16, GroupName="07. Session & Time Controls")]
		public DateTime Start4 { get; set; }
		[NinjaScriptProperty, PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey"), Display(Name="End Time 4", Order=17, GroupName="07. Session & Time Controls")]
		public DateTime End4 { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Time 5", Description = "Enable 5 times.", Order=18, GroupName = "07. Session & Time Controls"), RefreshProperties(RefreshProperties.All)]
		public bool Time5 { get{return isEnableTime5;} set{isEnableTime5 = (value);} }
		[NinjaScriptProperty, PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey"), Display(Name="Start Time 5", Order=19, GroupName="07. Session & Time Controls")]
		public DateTime Start5 { get; set; }
		[NinjaScriptProperty, PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey"), Display(Name="End Time 5", Order=20, GroupName="07. Session & Time Controls")]
		public DateTime End5 { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Time 6", Description = "Enable 6 times.", Order =21, GroupName = "07. Session & Time Controls"), RefreshProperties(RefreshProperties.All)]
		public bool Time6 { get{return isEnableTime6;} set{isEnableTime6 = (value);} }
		[NinjaScriptProperty, PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey"), Display(Name="Start Time 6", Order=22, GroupName="07. Session & Time Controls")]
		public DateTime Start6 { get; set; }
		[NinjaScriptProperty, PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey"), Display(Name="End Time 6", Order=23, GroupName="07. Session & Time Controls")]
		public DateTime End6 { get; set; }
		[NinjaScriptProperty, RefreshProperties(RefreshProperties.All), Display(Name = "Enable Trades Per Direction", Order = 24, GroupName = "07. Session & Time Controls")] public bool TradesPerDirection { get; set; }
		[NinjaScriptProperty, Display(Name="Longs Per Direction", Order = 25, GroupName = "07. Session & Time Controls")] public int longPerDirection { get; set; }
		[NinjaScriptProperty, Display(Name="Shorts Per Direction", Order = 26, GroupName = "07. Session & Time Controls")] public int shortPerDirection { get; set; }
		#endregion
		
		#region 08. Bot Parameters - Trend Bots
		[NinjaScriptProperty, Display(Name = "Enable Reaper", GroupName = "08. Bot Parameters - Trend Bots", Order = 1)] public bool EnableReaperBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Trend Architect Bot", GroupName = "08. Bot Parameters - Trend Bots", Order = 4, Description = "Uses TrendArchitectLite's graded signals as an entry source.")] public bool EnableTrendArchitectBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Swing Structure Bot", GroupName = "08. Bot Parameters - Trend Bots", Order = 5)] public bool EnableSwingStructureBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Chaser", Order = 8, GroupName = "08. Bot Parameters - Trend Bots")] public bool EnableChaser { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Hooker", Order = 11, GroupName = "08. Bot Parameters - Trend Bots")] public bool EnableHooker { get; set; }
        [NinjaScriptProperty, Display(Name = "Enable SuperTrendBot", GroupName = "08. Bot Parameters - Trend Bots", Order = 12, Description="Enables the SuperTrend signal logic.")] public bool EnableSuperTrendBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable TSSuperTrend Bot", GroupName = "08. Bot Parameters - Trend Bots", Order = 13, Description = "Enables the TSSuperTrend signal logic.")] public bool EnableTSSuperTrendBot { get; set; }
        [NinjaScriptProperty, Display(Name = "Enable Johny5", GroupName = "08. Bot Parameters - Trend Bots", Order = 14, Description="Enables the Johny5 signal logic, which uses a combination of MACD, Williams%R, and ALMA.")] public bool EnableJohny5 { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable MagicTrendy", Order = 15, GroupName = "08. Bot Parameters - Trend Bots")] public bool EnableMagicTrendy { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Trendy", GroupName = "08. Bot Parameters - Trend Bots", Order = 16)] public bool EnableT3FilterBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Coral Bot", GroupName = "08. Bot Parameters - Trend Bots", Order = 17)] public bool EnableCoralBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable TEFA Pro Bot", GroupName = "08. Bot Parameters - Trend Bots", Order = 18)] public bool EnableTefaProBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Zombie9 Bot", GroupName = "08. Bot Parameters - Trend Bots", Order = 19)] public bool EnableZombie9Bot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable SMA Composite Bot", GroupName = "08. Bot Parameters - Trend Bots", Order = 20)] public bool EnableSmaCompositeBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Range Filter Bot", GroupName = "08. Bot Parameters - Trend Bots", Order = 21)] public bool EnableRangeFilterBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable PSAR Bot", GroupName = "08. Bot Parameters - Trend Bots", Order = 22)] public bool EnablePSARBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Engulfing Bot (Continuation)", GroupName = "08. Bot Parameters - Trend Bots", Order = 23)] public bool EnableEngulfingContinuationBot { get; set; }
		#endregion

		#region 08a. Bot Parameters - Universal
		[NinjaScriptProperty, Display(Name = "Enable KingKhanh", Order = 1, GroupName = "08a. Bot Parameters - Universal")] public bool EnableKingKhanh { get; set; }
		[NinjaScriptProperty, Display(Name = "Show RegChanPlus", Order = 2, GroupName = "08a. Bot Parameters - Universal")] public bool ShowRegChan { get; set; }
		[NinjaScriptProperty, Display(Name="RegChanPlus Period", Order=3, GroupName="08a. Bot Parameters - Universal")] public int RegChanPlusPeriod { get; set; }
		[NinjaScriptProperty, Display(Name="RegChanPlus StdDev Width", Order = 4, GroupName="08a. Bot Parameters - Universal")] public double RegChanPlusStdDevWidth { get; set; }
        [NinjaScriptProperty, Display(Name="RegChanPlus Slope Threshold", Order=5, GroupName="08a. Bot Parameters - Universal")] public double RegChanPlusSlopeThreshold { get; set; }
		[NinjaScriptProperty, Display(Name="Band Smooth Period", Order=6, GroupName="08a. Bot Parameters - Universal")] public int RegChanPlusBandSmooth { get; set; }		
        [NinjaScriptProperty, Display(Name="RegChanPlus Volatility Lookback", Order=7, GroupName="08a. Bot Parameters - Universal", Description="The lookback period for the channel's volatility expansion SMA.")] public int RegChanPlusVolaLookback { get; set; }
		
		[NinjaScriptProperty, Display(Name = "Enable Pivot Impulse Bot", GroupName = "08a. Bot Parameters - Universal", Order = 8)]
		public bool EnablePivotImpulseBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Show Pivot Impulse Lines", GroupName = "08a. Bot Parameters - Universal", Order = 9)]
		public bool ShowPivotImpulseLines { get; set; }
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name="PIS: Swing Strength", Description="Determines the significance of the pivots for the Impulse Line.", Order=10, GroupName="08a. Bot Parameters - Universal")]
		public int PIS_SwingStrength { get; set; }
		[NinjaScriptProperty, Range(10, int.MaxValue), Display(Name="PIS: Pivot Lookback", Description="Lookback period for the main trend line.", Order=11, GroupName="08a. Bot Parameters - Universal")]
		public int PIS_PivotLookback { get; set; }
		#endregion
		
		#region 08b. Bot Parameters - Momentum (VMA/MED)
		[NinjaScriptProperty, Display(Name = "Enable Momentum VMA Bot", GroupName = "08b. Bot Parameters - Momentum (VMA/MED)", Order = 1)] public bool EnableMomentumVmaBot { get; set; }
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name = "MVD: VMA Period", GroupName = "08b. Bot Parameters - Momentum (VMA/MED)", Order = 3)] public int MvdVmaPeriod { get; set; }
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name = "MVD: VMA Volatility Period", GroupName = "08b. Bot Parameters - Momentum (VMA/MED)", Order = 4)] public int MvdVolatilityPeriod { get; set; }
		[NinjaScriptProperty, Range(2, int.MaxValue), Display(Name = "MVD: Extremes Lookback", Description = "The lookback period for the MVD's highest high/lowest low calculation.", GroupName = "08b. Bot Parameters - Momentum (VMA/MED)", Order = 5)] public int MvdExtremesLookback { get; set; }
		[NinjaScriptProperty, Range(0, int.MaxValue), Display(Name = "MVD: Offset Ticks", GroupName = "08b. Bot Parameters - Momentum (VMA/MED)", Order = 6)] public int MvdOffsetTicks { get; set; }
		[NinjaScriptProperty, XmlIgnore, Display(Name = "MVD: Up Color", GroupName = "08b. Bot Parameters - Momentum (VMA/MED)", Order = 7)] public Brush MvdUpColor { get; set; }
		[Browsable(false)] public string MvdUpColorSerializable { get { return Serialize.BrushToString(MvdUpColor); } set { MvdUpColor = Serialize.StringToBrush(value); } }
		[NinjaScriptProperty, XmlIgnore, Display(Name = "MVD: Down Color", GroupName = "08b. Bot Parameters - Momentum (VMA/MED)", Order = 8)] public Brush MvdDownColor { get; set; }
		[Browsable(false)] public string MvdDownColorSerializable { get { return Serialize.BrushToString(MvdDownColor); } set { MvdDownColor = Serialize.StringToBrush(value); } }
		[NinjaScriptProperty, Range(1, 20), Display(Name = "MVD: Driver Width", GroupName = "08b. Bot Parameters - Momentum (VMA/MED)", Order = 9)] public int MvdDriverWidth { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Momentum Extremes Bot", GroupName = "08b. Bot Parameters - Momentum Extremes (Pullback)", Order = 10)] public bool EnableMomentumExtremesBot { get; set; }
		[NinjaScriptProperty, Range(1, int.MaxValue), Display(Name = "Pullback MED Period", GroupName = "08a. Bot Parameters - Momentum Extremes (Pullback)", Order = 11)] 
		public int PullbackMedPeriod { get; set; }		
		[NinjaScriptProperty, Range(2, int.MaxValue), Display(Name = "Pullback MED Lookback", GroupName = "08a. Bot Parameters - Momentum Extremes (Pullback)", Order = 12)] 
		public int PullbackMedLookback { get; set; }
		#endregion
		
		#region 09. Bot Parameters - Range Bots
		[NinjaScriptProperty, Display(Name = "Enable SessionFader", GroupName = "09. Bot Parameters - Range Bots", Order = 2)] public bool EnableSessionFaderBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable SuperRex", Order = 3, GroupName = "09. Bot Parameters - Range Bots")] public bool EnableSuperRex { get; set; }
        [NinjaScriptProperty, Display(Name = "Enable RSI Reversal Bot", GroupName = "09. Bot Parameters - Range Bots", Order = 4)] public bool EnableRsiBot { get; set; }	
        [NinjaScriptProperty, Display(Name = "Enable Stochastics Bot", GroupName = "09. Bot Parameters - Range Bots", Order = 5)] public bool EnableStochasticsBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Casher", Order = 6, GroupName = "09. Bot Parameters - Range Bots")] public bool EnableCasher { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Willy", Order = 7, GroupName = "09. Bot Parameters - Range Bots")] public bool EnableWilly { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable BalaBot", Order = 8, GroupName = "09. Bot Parameters - Range Bots")] public bool EnableBalaBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable SmartMoney", GroupName = "09. Bot Parameters - Range Bots", Order = 9)] public bool EnableSmartMoneyBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Pivotty", GroupName = "09. Bot Parameters - Range Bots", Order = 10)] public bool EnablePivotty { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Glider Bot", GroupName = "09. Bot Parameters - Range Bots", Order = 11)] public bool EnableGliderBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Engulfing Bot (Reversal)", GroupName = "09. Bot Parameters - Range Bots", Order = 12)] public bool EnableEngulfingReversalBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Andean", Order = 13, GroupName = "09. Bot Parameters - Range Bots")] public bool EnableAndean { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable ZigZag Bot", GroupName = "09. Bot Parameters - Range Bots", Order = 14)] public bool EnableZigZagBot { get; set; }
		#endregion
		
		#region 10. Bot Parameters - Breakout Bots
		[NinjaScriptProperty, Display(Name = "Enable Keltner Bot", GroupName = "10. Bot Parameters - Breakout Bots", Order = 1)] public bool EnableKeltnerBot { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable Session Breaker", GroupName = "10. Bot Parameters - Breakout Bots", Order = 2)] public bool EnableSessionBreakerBot { get; set; }
        [NinjaScriptProperty, Display(Name = "Enable Bollinger Breakout Bot", GroupName = "10. Bot Parameters - Breakout Bots", Order = 3)] public bool EnableBollingerBot { get; set; }	
		[NinjaScriptProperty, Display(Name = "Enable ORBot", GroupName = "10. Bot Parameters - Breakout Bots", Order = 4)] public bool EnableORBBot { get; set; }
		[NinjaScriptProperty, Display(Name = "ORB Start Time", GroupName = "10. Bot Parameters - Breakout Bots", Order = 5)] [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")] public DateTime ORBStartTime { get; set; }
		[NinjaScriptProperty, Display(Name = "ORB Start Time", GroupName = "10. Bot Parameters - Breakout Bots", Order = 6)] [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")] public DateTime ORBEndTime { get; set; }
		[NinjaScriptProperty, Display(Name = "Enable TTM Squeeze Bot", GroupName = "10. Bot Parameters - Breakout Bots", Order = 7)] public bool EnableTTMSqueezeBot { get; set; }
		#endregion
		
		#region 11. Visuals & Diagnostics
		[NinjaScriptProperty, Display(Name = "Show Daily PnL", Order = 1, GroupName = "11. Visuals & Diagnostics")] public bool showDailyPnl { get; set; }			
		[NinjaScriptProperty, Display(Name = "Enable Trend Background", Order = 2, GroupName = "11. Visuals & Diagnostics")] public bool EnableTrendBackground { get; set; }
		[NinjaScriptProperty, Display(Name = "Font Size", Order = 3, GroupName = "11. Visuals & Diagnostics")] public int FontSize { get; set; }
		[NinjaScriptProperty, Display(Name = "Text Panel Transparency", Order = 4, GroupName = "11. Visuals & Diagnostics")] public int Transparency { get; set; }
		[XmlIgnore, Display(Name = "Daily PnL Color", Order = 5, GroupName = "11. Visuals & Diagnostics")] public Brush colorDailyProfitLoss { get; set; }	
		[Browsable(false)] public string colorDailyProfitLossSerialize { get { return Serialize.BrushToString(colorDailyProfitLoss); } set { colorDailyProfitLoss = Serialize.StringToBrush(value); } }
		[NinjaScriptProperty, Display(Name="Daily PnL Position", Order = 6, GroupName = "11. Visuals & Diagnostics")] public TextPosition PositionDailyPNL { get; set; }
        [NinjaScriptProperty, Display(Name = "Show STATUS PANEL", Order = 7, GroupName = "11. Visuals & Diagnostics")] public bool showPnl { get; set; }		
		[XmlIgnore, Display(Name = "STATUS PANEL Color", Order = 8, GroupName = "11. Visuals & Diagnostics")] public Brush colorPnl { get; set; }				
		[Browsable(false)] public string colorPnlSerialize { get { return Serialize.BrushToString(colorPnl); } set { colorPnl = Serialize.StringToBrush(value); } }
		[NinjaScriptProperty, Display(Name="STATUS PANEL Position", Order = 9, GroupName = "11. Visuals & Diagnostics")] public TextPosition PositionPnl { get; set; }	
		[NinjaScriptProperty, Display(Name="Show Historical Trades", Order= 10, GroupName="11. Visuals & Diagnostics")] public bool ShowHistorical { get; set; }
	    [NinjaScriptProperty, Display(Name = "Enable Debug Logging", Order = 11, GroupName = "11. Visuals & Diagnostics", Description="Enables detailed logging to a file in the MyDocuments/KCStrategies folder. Use for debugging only, as it can impact performance.")] public bool EnableLogging { get; set; }
		[NinjaScriptProperty, Display(Name="Enable JSON Trade Logging", Order=12, GroupName="11. Visuals & Diagnostics")] public bool EnableJsonLogging { get; set; }
		[Display(Name="JSON Log File Name", Description="e.g., 'MyPerformanceLog.jsonl'. File will be saved in Documents/NinjaTrader 8/", Order=13, GroupName="11. Visuals & Diagnostics")]
		public string JsonLogFileName { get; set; }
		[NinjaScriptProperty, Display(Name="Enable CSV Trade Logging", Order=14, GroupName="11. Visuals & Diagnostics")] public bool EnableTradeLogging { get; set; }
		[Display(Name="CSV Log File Name", Description="e.g., 'MyPerformanceLog.csv'. File will be saved in Documents/NinjaTrader 8/", Order=15, GroupName="11. Visuals & Diagnostics")]
		public string TradeLogFileName { get; set; }
        [NinjaScriptProperty, Display(Name = "Enable Health Checks (Live)", Description = "Enables safety features like data loss and rejection detection. Recommended for live trading.", Order = 16, GroupName = "11. Visuals & Diagnostics")] public bool EnableHealthChecks { get; set; }
        [NinjaScriptProperty, Range(5, 60), Display(Name = "Data Loss Timeout (Sec)", Description = "Disables the strategy if no new tick is received for this many seconds during active hours.", Order = 17, GroupName = "11. Visuals & Diagnostics")] public int DataLossTimeoutSeconds { get; set; }
        [NinjaScriptProperty, Range(2, 10), Display(Name = "Max Order Rejections", Description = "Disables the strategy if this many consecutive orders are rejected.", Order = 18, GroupName = "11. Visuals & Diagnostics")] public int MaxConsecutiveRejections { get; set; }
		[Display(Name = "Show Open", GroupName = "11. Visuals & Diagnostics", Order = 19)] public bool ShowOpen { get; set; }
		[Display(Name = "Show High", GroupName = "11. Visuals & Diagnostics", Order = 20)] public bool ShowHigh { get; set; }
		[Display(Name = "Show Low", GroupName = "11. Visuals & Diagnostics", Order = 21)] public bool ShowLow { get; set; }
		[NinjaScriptProperty, Display(Name = "Show Momentum Extremes Plot", GroupName = "11. Visuals & Diagnostics", Order = 22)] public bool ShowMomentumExtremesPlot { get; set; }
		[NinjaScriptProperty, Display(Name = "Show Momentum VMA Plot", GroupName = "11. Visuals & Diagnostics", Order = 23)] public bool ShowMomentumVmaPlot { get; set; }
		[NinjaScriptProperty, Display(Name = "Show Parabolic SAR", Order = 24, GroupName = "11. Visuals & Diagnostics")] public bool ShowPSARPlot { get; set; }
		[NinjaScriptProperty, Display(Name = "Show DM", Order = 25, GroupName = "11. Visuals & Diagnostics")] public bool ShowDM { get; set; }
		[NinjaScriptProperty, Display(Name = "Show Momentum", Order = 26, GroupName = "11. Visuals & Diagnostics")] public bool ShowMomo { get; set; }
        [NinjaScriptProperty, Display(Name = "Show Exhaustion BB Plot", Order = 27, GroupName = "11. Visuals & Diagnostics")] public bool ShowExhaustionBB { get; set; }
		#endregion
		
		#region 12. Advanced Execution
        [NinjaScriptProperty, Display(Name = "Enable Scale-In Execution", Description = "If true, large orders will be broken into smaller pieces to reduce market impact.", Order = 1, GroupName = "12. Advanced Execution")]
        public bool EnableScaleInExecution { get; set; }
        [NinjaScriptProperty, Range(1, 10), Display(Name = "Scale-In Chunks", Description = "The number of pieces to break the order into.", Order = 2, GroupName = "12. Advanced Execution")]
        public int ScaleInChunks { get; set; }
        [NinjaScriptProperty, Range(1, 20), Display(Name = "Scale-In Delay (Seconds)", Description = "The delay between each piece of the scaled-in order.", Order = 3, GroupName = "12. Advanced Execution")]
        public int ScaleInDelaySeconds { get; set; }
		#endregion
		
		#region 13. About
		[NinjaScriptProperty, Display(Name="BaseAlgoVersion", Order=1, GroupName="13. About")] public string BaseAlgoVersion { get; set; }
		[NinjaScriptProperty, Display(Name="Author", Order=2, GroupName="13. About")] public string Author { get; set; }		
		[NinjaScriptProperty, Display(Name="StrategyName", Order=3, GroupName="13. About")] public string StrategyName { get; set; }
		[NinjaScriptProperty, Display(Name="StrategyVersion", Order =4, GroupName="13. About")] public string StrategyVersion { get; set; }
		[NinjaScriptProperty, Display(Name="Credits", Order=5, GroupName="13. About")] public string Credits { get; set; }
		[NinjaScriptProperty, Display(Name="Chart Type", Order=6, GroupName="13. About")] public string ChartType { get; set; }
		[NinjaScriptProperty, Display(Name = "PayPal Donation URL", Order = 7, GroupName = "13. About")] public string paypal { get; set; }
		#endregion
		
		#region Uncategorized Bot Parameters
		// These properties are not assigned to a group, so they will not appear in the UI.
		// They are kept here so that the strategy compiles correctly for bots that use them.
		[NinjaScriptProperty] public bool ShowTrendArchitectPlot { get; set; }
		[NinjaScriptProperty] public bool ShowSwingPlot { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int SwingStrength { get; set; }
		[NinjaScriptProperty] public bool ShowLinReg { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int LinRegPeriod { get; set; }
		[NinjaScriptProperty] public double LinRegSlopeThreshold { get; set; }
		[NinjaScriptProperty] public bool ShowHmaHooks { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int HmaHooksPeriod { get; set; }
		[NinjaScriptProperty] public bool ShowSuperTrend { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int SuperTrendPeriod { get; set; }
		[NinjaScriptProperty] [Range(2, 3)] public int SuperTrendPoles { get; set; }
		[NinjaScriptProperty] public bool ShowTSSuperTrend { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TSSuperTrendLength { get; set; }
		[NinjaScriptProperty] [Range(0.001, double.MaxValue)] public double TSSuperTrendMultiplier { get; set; }
		[NinjaScriptProperty] public MAType TSSuperTrendMaType { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TSSuperTrendSmooth { get; set; }
		[NinjaScriptProperty] public bool ShowJohny5 { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int JbSignalMacdFast { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int JbSignalMacdSlow { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int JbSignalMacdSmooth { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int JbSignalWrPeriod { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int JbSignalWrEmaPeriod { get; set; }
		[NinjaScriptProperty] public double JbSignalAlmaFastLen { get; set; }
		[NinjaScriptProperty] public double JbSignalAlmaSlowLen { get; set; }
		[NinjaScriptProperty] public bool ShowTrendMagic { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TrendMagicCciPeriod { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TrendMagicAtrPeriod { get; set; }
		[NinjaScriptProperty] [Range(0.00001, double.MaxValue)] public double TrendMagicAtrMult { get; set; }
		[NinjaScriptProperty] public bool ShowT3Filter { get; set; }
		[NinjaScriptProperty] [Range(0.1, double.MaxValue)] public double T3FVolumeFactor { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int T3FPeriod1 { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int T3FPeriod2 { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int T3FPeriod3 { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int T3FPeriod4 { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int T3FPeriod5 { get; set; }
		[NinjaScriptProperty] public bool ShowCoralPlot { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int CoralSmoothingPeriod { get; set; }
		[NinjaScriptProperty] [Range(0.0001, double.MaxValue)] public double CoralConstantD { get; set; }
		[NinjaScriptProperty] public bool ShowTefaProPlot { get; set; }
		[NinjaScriptProperty] public bool TefaUseMACD { get; set; }
		[NinjaScriptProperty] public bool TefaUseWilliamsR { get; set; }
		[NinjaScriptProperty] public bool TefaUseCCI { get; set; }
		[NinjaScriptProperty] public bool TefaUseMomentum { get; set; }
		[NinjaScriptProperty] public bool TefaUseStochastics { get; set; }
		[NinjaScriptProperty] public bool TefaUseRangeFilter { get; set; }
		[NinjaScriptProperty] public bool TefaUseUltimateMA { get; set; }
		[NinjaScriptProperty] public bool TefaUseOpenCloseCondition { get; set; }
		[NinjaScriptProperty] public bool TefaUseHighLowCondition { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TefaFast { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TefaSlow { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TefaSmooth { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TefaWilliamsRPeriod { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TefaWilliamsREMAPeriod { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TefaCCIPeriod { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TefaCCIEMAPeriod { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TefaMomentumPeriod { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TefaMomentumEMAPeriod { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TefaStochasticKPeriod { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TefaStochasticDPeriod { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TefaStochasticSmooth { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TefaSamplingPeriod { get; set; }
		[NinjaScriptProperty] [Range(0.1, double.MaxValue)] public double TefaRangeMultiplier { get; set; }
		[NinjaScriptProperty] public MAType TefaSelectedMAType { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TefaPeriodMA { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TefaLookBackBars { get; set; }
		[NinjaScriptProperty] public bool ShowZombie9Plots { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int Zombie9MacdFast { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int Zombie9MacdSlow { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int Zombie9MacdSmooth { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int Zombie9SmaPeriod { get; set; }
		[NinjaScriptProperty] public bool ShowSmaCompositePlot { get; set; }
		[NinjaScriptProperty] public bool ShowRangeFilterPlot { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int RFSamplingPeriod { get; set; }
		[NinjaScriptProperty] [Range(0.1, double.MaxValue)] public double RFRangeMultiplier { get; set; }
		[NinjaScriptProperty] public bool ShowSessionFaderPlot { get; set; }
		[NinjaScriptProperty] public bool ShowCMO { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int CmoPeriod { get; set; }
		[NinjaScriptProperty] public double CmoOverboughtLevel { get; set; }
		[NinjaScriptProperty] public double CmoOversoldLevel { get; set; }
		[NinjaScriptProperty] public bool ShowRsi { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int RsiPeriod { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int RsiOverbought { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int RsiOversold { get; set; }
		[NinjaScriptProperty] public bool ShowStoch { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int StochPeriodD { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int StochPeriodK { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int StochSmooth { get; set; }
		[NinjaScriptProperty] public bool ShowHiLoBands { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int LookbackPeriod { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int SmoothingPeriod { get; set; }
		[NinjaScriptProperty] public bool ShowWilly { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int wrPeriod { get; set; }
		[NinjaScriptProperty] [Range(-100, 0)] public int wrUp { get; set; }
		[NinjaScriptProperty] [Range(-100, 0)] public int wrDown { get; set; }
		[NinjaScriptProperty] public bool ShowBalaBot { get; set; }
		[NinjaScriptProperty] public double Bala_Dev1 { get; set; }
		[NinjaScriptProperty] public double Bala_XS1 { get; set; }
		[NinjaScriptProperty] public double Bala_XL1 { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int Bala_RSIPeriod1 { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int Bala_EMAPeriod1 { get; set; }
		[NinjaScriptProperty] public double Bala_Dev2 { get; set; }
		[NinjaScriptProperty] public double Bala_XS2 { get; set; }
		[NinjaScriptProperty] public double Bala_XL2 { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int Bala_RSIPeriod2 { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int Bala_EMAPeriod2 { get; set; }
		[NinjaScriptProperty] public bool ShowMarketStructurePlot { get; set; }
		[NinjaScriptProperty] [Range(3, int.MaxValue)] public int MarketStructurePeriod { get; set; }
		[NinjaScriptProperty] public bool MarketStructureUseReversals { get; set; }
		[NinjaScriptProperty] public bool MarketStructureUseContinuations { get; set; }
		[NinjaScriptProperty] public bool ShowPivotty { get; set; }
		[NinjaScriptProperty] public NTSvePivotRange SvePivotsRangeType { get; set; }
		[NinjaScriptProperty] public NTSveHLCCalculationMode SvePivotsCalcMode { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int SvePivotsWidth { get; set; }
		[NinjaScriptProperty] public bool SvePivotsUseR1 { get; set; }
		[NinjaScriptProperty] public bool SvePivotsUseR2 { get; set; }
		[NinjaScriptProperty] public bool SvePivotsUseR3 { get; set; }
		[NinjaScriptProperty] public bool SvePivotsUseS1 { get; set; }
		[NinjaScriptProperty] public bool SvePivotsUseS2 { get; set; }
		[NinjaScriptProperty] public bool SvePivotsUseS3 { get; set; }
		[NinjaScriptProperty] public bool ShowSMIPlot { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int SMIEMAPeriod1 { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int SMIEMAPeriod2 { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int SMIRange { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int SMIEMAPeriod { get; set; }
		[NinjaScriptProperty] public bool ShowEngulfingPlot { get; set; }
		[NinjaScriptProperty] public bool EngulfingEngulfBody { get; set; }
		[NinjaScriptProperty] [Range(0, int.MaxValue)] public int EngulfingTickOffset { get; set; }
		[NinjaScriptProperty] public bool ShowAndean { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int AndeanLength { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int AndeanSignalLength { get; set; }
		[NinjaScriptProperty] [Range(1, 10)] public int AndeanLookback { get; set; }
		[NinjaScriptProperty] public bool ShowZigZagPlot { get; set; }
		[NinjaScriptProperty] public DeviationType ZigZagDeviationType { get; set; }
		[NinjaScriptProperty] [Range(0.0001, double.MaxValue)] public double ZigZagDeviationValue { get; set; }
		[NinjaScriptProperty] public bool ZigZagUseHighLow { get; set; }
		[NinjaScriptProperty] public bool ShowKeltnerPlot { get; set; }
		[NinjaScriptProperty] [Range(0.01, int.MaxValue)] public double KeltnerOffsetMultiplier { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int KeltnerPeriod { get; set; }
		[NinjaScriptProperty] public bool ShowSessionBreakerPlot { get; set; }
		[NinjaScriptProperty] public bool ShowBollinger { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int BollingerPeriod { get; set; }
		[NinjaScriptProperty] [Range(0.000001, double.MaxValue)] public double BollingerStdDev { get; set; }
		[NinjaScriptProperty] public bool ShowORBPlot { get; set; }
		[NinjaScriptProperty] public string ORBTimeZone { get; set; }
		[NinjaScriptProperty] public bool ORBUseBreakoutSignals { get; set; }
		[NinjaScriptProperty] public bool ORBUseReversalSignals { get; set; }
		[NinjaScriptProperty] public bool ShowTTMSqueezePlot { get; set; }
		[NinjaScriptProperty] [Range(1, double.MaxValue)] public double TTMBollingerDeviation { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TTMBollingerPeriod { get; set; }
		[NinjaScriptProperty] [Range(1, double.MaxValue)] public double TTMKeltnerMultiplier { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TTMKeltnerPeriod { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TTMMomentumPeriod { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] public int TTMMomentumEMAPeriod { get; set; }
		#endregion
		
		#endregion			
    }
}