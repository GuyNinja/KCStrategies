// --- KCAlgoBase_Indicators.cs ---
// Version 6.5.9
// Key Changes:
// 1. RE-ENABLED: The VOLMA indicator has been re-enabled for use in the chop detection filter.

#region Using declarations
using System;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui; 
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators.AlgoTrades;
using NinjaTrader.NinjaScript.Indicators.BlueZ;
using NinjaTrader.NinjaScript.Indicators.FxStill.SmartMoney;
using NinjaTrader.NinjaScript.Indicators.ZombiePack9;
using NinjaTrader.NinjaScript.Indicators.TradeSaber;
using NinjaTrader.NinjaScript.Indicators.TradeSaber_SignalMod;
#endregion

namespace NinjaTrader.NinjaScript.Strategies.KCStrategies
{
    public abstract partial class KCAlgoBase : Strategy
    {
        #region Indicator Declarations
        
        // --- Core Indicators (used by multiple modules) ---
        [Browsable(false)][XmlIgnore] public DM DM1 { get; private set; }
        [Browsable(false)][XmlIgnore] public ATR ATR1 { get; private set; }  
		
        [Browsable(false)][XmlIgnore] public Series<double> regChanPlusRange;
        [Browsable(false)][XmlIgnore] public SMA regChanPlusAvgRange;
        
        // --- Market Regime Indicators ---
        protected ADX regimeAdx;
        protected Bollinger regimeBollinger;
        protected Series<double> regimeBBWidth;
        protected MIN regimeMinBBW;
        
        // --- Master Filter Indicators ---
        protected LinReg chopFilterLinReg;
        protected ADX chopFilterAdx;
		
        // --- Bot-Specific Indicators (Must be public for bot classes, but hidden from UI) ---
        [Browsable(false)][XmlIgnore] public RSI botRsi { get; set; }
        [Browsable(false)][XmlIgnore] public Stochastics botStochastics { get; set; }
        [Browsable(false)][XmlIgnore] public Bollinger botBollinger { get; set; }
        [Browsable(false)][XmlIgnore] public CMO botCmo { get; set; } 		
        [Browsable(false)][XmlIgnore] public HiLoBands botHiloBands { get; set; }
		[Browsable(false)][XmlIgnore] public LinReg botLinReg { get; set; }
        [Browsable(false)][XmlIgnore] public BlueZHMAHooks botHmaHooks { get; set; }		
        [Browsable(false)][XmlIgnore] public AuSuperSmootherFilter botSuperSmoother { get; set; }
        [Browsable(false)][XmlIgnore] public TrendMagic botTrendMagic { get; set; }
		[Browsable(false)][XmlIgnore] public WilliamsR botWilly { get; set; }      
        [Browsable(false)][XmlIgnore] public JBSignal botJbSignal { get; set; }		
		[Browsable(false)][XmlIgnore] public AndeanOscillatorSignalMod andeanOscillator { get; set; }		
		[Browsable(false)][XmlIgnore] public Bala2Channels Bala1 { get; set; }
		[Browsable(false)][XmlIgnore] public TSSuperTrend botTSSuperTrend { get; set; }
		[Browsable(false)][XmlIgnore] public T3TrendFilter botT3Filter { get; set; }
		[Browsable(false)][XmlIgnore] public CoralTrendIndicatorLB botCoral { get; set; }
		[Browsable(false)][XmlIgnore] public TEFASignalsPro botTefaPro { get; set; }
		[Browsable(false)][XmlIgnore] public MarketStructuresLite botMarketStructure { get; set; }
		[Browsable(false)][XmlIgnore] public ORB_TradeSaber botORB { get; set; }
		[Browsable(false)][XmlIgnore] public NTSvePivots botSvePivots { get; set; }
		[Browsable(false)][XmlIgnore] public Zombie9MACD botZombie9Macd { get; set; }
		[Browsable(false)][XmlIgnore] public SMA botZombie9Sma { get; set; }
		[Browsable(false)][XmlIgnore] public TTMSqueezeTradeSaber botTTMSqueeze { get; set; }
		[Browsable(false)][XmlIgnore] public Zombie3SMI botSMI { get; set; }
		[Browsable(false)][XmlIgnore] public SMAcompositeAverage botSmaComposite { get; set; }
		[Browsable(false)][XmlIgnore] public RangeFilter botRangeFilter { get; set; }
		[Browsable(false)][XmlIgnore] public ZigZag botZigZag { get; set; }
		[Browsable(false)][XmlIgnore] public KeltnerChannel botKeltner { get; set; }
		[Browsable(false)][XmlIgnore] public EngulfingBarTS botEngulfing { get; set; }
		[Browsable(false)][XmlIgnore] public CurrentDayOHL dailyOHLIndicator { get; set; }
		[Browsable(false)][XmlIgnore] public CurrentDayOHL botSessionBreaker { get; set; }
		[Browsable(false)][XmlIgnore] public CurrentDayOHL botSessionFader { get; set; }
        [Browsable(false)][XmlIgnore] public Momentum botMomentum { get; private set; }
		[Browsable(false)][XmlIgnore] public RegressionChannelPlus botRegChanPlus { get; set; }
		[Browsable(false)][XmlIgnore] public ParabolicSAR2 psar { get; set; }
		[Browsable(false)][XmlIgnore] public ParabolicSAR2 botPSAR { get; set; }
        [Browsable(false)][XmlIgnore] public TrendArchitectLite botTrendArchitect { get; set; }
        [Browsable(false)][XmlIgnore] public Swing botSwing { get; set; }
		[Browsable(false)][XmlIgnore] public MomentumVmaDriver botMomentumVmaDriver { get; set; }
		[Browsable(false)][XmlIgnore] public MomentumExtremesDriver filterMomentumExtremes { get; set; } // This is for the FILTER
		[Browsable(false)][XmlIgnore] public MomentumExtremesDriver botMomentumExtremes { get; set; }    // This is for the BOT
        [Browsable(false)][XmlIgnore] public Bollinger botExhaustionBB { get; set; }
		[Browsable(false)][XmlIgnore] public VOLMA chopVolma { get; set; } // RE-ENABLED
		[Browsable(false)][XmlIgnore] public PivotImpulseTrendLines botPivotImpulse { get; set; }
        #endregion

        #region Indicator Initialization and Calculation

        private void InitializeAllIndicators()
        {
			// === CORE & DISPLAY INDICATORS (Always initialized for panel accuracy) ===
			ATR1 = ATR(AtrPeriod);
			DM1 = DM(DmPeriod);
			botMomentum = Momentum(MomentumPeriod);
			botMomentumVmaDriver = MomentumVmaDriver(MvdVmaPeriod, MvdVolatilityPeriod, MvdExtremesLookback, MvdOffsetTicks, MvdUpColor, MvdDownColor, MvdDriverWidth);
			botMomentumVmaDriver.IsAutoScale = false;
			botMomentumVmaDriver.Plots[0].Brush = Brushes.Transparent;
			AddChartIndicator(botMomentumVmaDriver);
			
			// --- DECOUPLING LOGIC START ---
			// Initialize the SLOW (233) version for the Master Trend Filter
			filterMomentumExtremes = MomentumExtremesDriver(MedHmaPeriod, MedExtremesLookback, MedOffsetTicks, MedUpColor, MedDownColor, 3);
			filterMomentumExtremes.IsAutoScale = false;
			filterMomentumExtremes.Plots[0].Brush = Brushes.Transparent;
			AddChartIndicator(filterMomentumExtremes);

			// Initialize the FAST (34) version for the Pullback Bot, using its own dedicated parameters
			if(EnableMomentumExtremesBot)
			{
				botMomentumExtremes = MomentumExtremesDriver(PullbackMedPeriod, PullbackMedLookback, MedOffsetTicks, Brushes.Yellow, Brushes.Magenta, 3);
				botMomentumExtremes.IsAutoScale = false;
				if(ShowMomentumExtremesPlot)
					AddChartIndicator(botMomentumExtremes);
			}
			// --- DECOUPLING LOGIC END ---
			
            if (EnableExhaustionFilter)
            {
                botExhaustionBB = Bollinger(ExhaustionBBStdDev, ExhaustionBBPeriod);
	            if (ShowExhaustionBB)
	            {
	                botExhaustionBB.Plots[0].Brush = Brushes.Orange;
					botExhaustionBB.Plots[0].Width = 2;
	                botExhaustionBB.Plots[1].Brush = Brushes.Transparent;
					botExhaustionBB.Plots[2].Width = 2;
	                botExhaustionBB.Plots[2].Brush = Brushes.Orange;
	                AddChartIndicator(botExhaustionBB);
	            }
            }
			
			if (ShowDM) 
			{
				DM1.Plots[1].Brush = Brushes.Lime; DM1.Plots[2].Brush = Brushes.Red;
				DM1.Plots[0].Width = 2; DM1.Plots[1].Width = 2; DM1.Plots[2].Width = 2;
				AddChartIndicator(DM1);
			}
			if (ShowMomo) 
			{
				botMomentum.Plots[0].Width = 2;
				AddChartIndicator(botMomentum);
			}
			// =========================================================================

			if (EnableAutoRegimeDetection || EnableChopDetection)
			{
				regimeAdx = ADX(RegimeAdxPeriod);
				chopFilterAdx = regimeAdx;
			}
			if (EnableChopDetection)
            {
                chopFilterLinReg = LinReg(ChopFilterPeriod);
				chopVolma = VOLMA(VolmaFilterPeriod);
            }
			
            if (EnableAutoRegimeDetection)
            {
                regimeBollinger = Bollinger(RegimeBBStdDev, RegimeBBPeriod);
                regimeBBWidth = new Series<double>(this);
                regimeMinBBW = MIN(regimeBBWidth, RegimeSqueezeLookback);
            }
			
		    psar = ParabolicSAR2(PSARAcceleration, PSARAccelerationMax, PSARAcceleration);
		    if (StopType == StopManagementType.ParabolicTrail && ShowPSARPlot)
		    {
		        AddChartIndicator(psar);
		    }
	
			// --- BOT-SPECIFIC INITIALIZATIONS (Grouped by category for performance) ---
			
			if (EnablePivotImpulseBot)
			{
				if (ShowPivotImpulseLines)
				{
					// Initialize with visible colors and add to chart
					botPivotImpulse = PivotImpulseTrendLines(PIS_SwingStrength, PIS_PivotLookback, Brushes.Yellow, 2, Brushes.Lime, Brushes.Red, 2, Color.FromArgb(30, 0, 128, 0), Color.FromArgb(30, 139, 0, 0));
				}
				else
				{
					// Initialize with transparent colors for bot logic only
					botPivotImpulse = PivotImpulseTrendLines(PIS_SwingStrength, PIS_PivotLookback, Brushes.Transparent, 1, Brushes.Transparent, Brushes.Transparent, 1, Colors.Transparent, Colors.Transparent);
				}
				AddChartIndicator(botPivotImpulse);
			}

			if (EnableTrendBots)
			{
				if (EnableChaser)
				{
					botLinReg = LinReg(LinRegPeriod);
					if (ShowLinReg)
					{
						botLinReg.Plots[0].Brush = Brushes.Yellow;
						botLinReg.Plots[0].Width = 2;
						AddChartIndicator(botLinReg);
					}
				}
				if (EnableSwingStructureBot || EnableMarketStructureFilter)
				{
					botSwing = Swing(SwingStrength);
					if (ShowSwingPlot) AddChartIndicator(botSwing);
				}
				if (EnableHooker)
				{
					botHmaHooks = BlueZHMAHooks(HmaHooksPeriod, 0, false, false, true, Brushes.Lime, Brushes.Red);
					if (ShowHmaHooks) AddChartIndicator(botHmaHooks);
				}
				if (EnableKingKhanh) 
				{
					botRegChanPlus = RegressionChannelPlus(RegChanPlusPeriod, RegChanPlusStdDevWidth, RegChanPlusBandSmooth);
					regChanPlusRange = new Series<double>(this);
					regChanPlusAvgRange = SMA(regChanPlusRange, RegChanPlusVolaLookback);
					if (ShowRegChan) AddChartIndicator(botRegChanPlus);
				}
				if (EnablePSARBot)
				{
					botPSAR = psar;
				}
				if (EnableCoralBot)
				{
					botCoral = CoralTrendIndicatorLB(CoralSmoothingPeriod, CoralConstantD, false, false, PlotStyle.Line, 2);
					if (ShowCoralPlot) AddChartIndicator(botCoral);
				}
				if (EnableZombie9Bot)
				{
					botZombie9Macd = Zombie9MACD("Zombie9MACD", true, Zombie9MacdFast, Zombie9MacdSlow, Zombie9MacdSmooth);
					botZombie9Sma = SMA(Zombie9SmaPeriod);
					if (ShowZombie9Plots) { AddChartIndicator(botZombie9Macd); AddChartIndicator(botZombie9Sma); }
				}
				if (EnableSmaCompositeBot)
				{
					botSmaComposite = SMAcompositeAverage(); 
					if (ShowSmaCompositePlot) AddChartIndicator(botSmaComposite);
				}
				if (EnableRangeFilterBot)
				{
					botRangeFilter = RangeFilter(RFSamplingPeriod, RFRangeMultiplier);			    
					if (ShowRangeFilterPlot) AddChartIndicator(botRangeFilter);
				}
				if (EnableTefaProBot)
				{
					botTefaPro = TEFASignalsPro(TefaFast, TefaSlow, TefaSmooth, TefaWilliamsRPeriod, TefaWilliamsREMAPeriod, TefaCCIPeriod, TefaCCIEMAPeriod, TefaMomentumPeriod, TefaMomentumEMAPeriod, TefaStochasticKPeriod, TefaStochasticDPeriod, TefaStochasticSmooth, TefaSamplingPeriod, TefaRangeMultiplier, TefaSelectedMAType, TefaPeriodMA, 3, 0.7, 2, TimeFrameModeTEFASignalsPro.ChartTimeFrame, BarsPeriodType.Minute, 1, TefaLookBackBars, TefaUseMACD, TefaUseWilliamsR, TefaUseCCI, TefaUseMomentum, TefaUseStochastics, TefaUseRangeFilter, TefaUseUltimateMA, TefaUseOpenCloseCondition, TefaUseHighLowCondition, Brushes.Cyan, Brushes.Magenta, false, 0.25, 0.25, 12, 5);
					if (ShowTefaProPlot) AddChartIndicator(botTefaPro);
				}
				if (EnableSuperTrendBot)
				{
					botSuperSmoother = AuSuperSmootherFilter(SuperTrendPoles, SuperTrendPeriod);
					if (ShowSuperTrend) AddChartIndicator(botSuperSmoother);
				}
				if (EnableJohny5)
				{
					botJbSignal = JBSignal(JbSignalMacdFast, JbSignalMacdSlow, JbSignalMacdSmooth, JbSignalWrPeriod, JbSignalWrEmaPeriod, JbSignalAlmaFastLen, JbSignalAlmaSlowLen);
					if (ShowJohny5) AddChartIndicator(botJbSignal);
				}
				if (EnableTSSuperTrendBot)
				{				
					botTSSuperTrend = TSSuperTrend(SuperTrendMode.ATR, TSSuperTrendLength, TSSuperTrendMultiplier, TSSuperTrendMaType, TSSuperTrendSmooth, false, false, false);
					if (ShowTSSuperTrend) AddChartIndicator(botTSSuperTrend);
				}
				if (EnableMagicTrendy)
				{
					botTrendMagic = TrendMagic(TrendMagicCciPeriod, TrendMagicAtrPeriod, TrendMagicAtrMult, false);
					if (ShowTrendMagic) AddChartIndicator(botTrendMagic);
				}
				if (EnableT3FilterBot)
				{
					botT3Filter = T3TrendFilter(T3FVolumeFactor, T3FPeriod1, T3FPeriod2, T3FPeriod3, T3FPeriod4, T3FPeriod5, false);
					if (ShowT3Filter) AddChartIndicator(botT3Filter);
				}
			}
			
			if (EnableRangeBots || EnableOverboughtOversoldFilter)
			{
				if (EnableRsiBot || EnableOverboughtOversoldFilter)
				{
					botRsi = RSI(RsiFilterPeriod, 1);
					if (ShowRsi && EnableRsiBot) AddChartIndicator(botRsi);
				}
				if (EnableStochasticsBot)
				{
					botStochastics = Stochastics(StochPeriodD, StochPeriodK, StochSmooth);
					if (ShowStoch) AddChartIndicator(botStochastics);				
				}
				if (EnableSuperRex)
				{
					botCmo = CMO(CmoPeriod);				
					if (ShowCMO) AddChartIndicator(botCmo);
				}
				if (EnableCasher)
				{
					botHiloBands = HiLoBands(LookbackPeriod, SmoothingPeriod, 2);
					if (ShowHiLoBands)  AddChartIndicator(botHiloBands);
				}			
				if (EnableAndean) 
				{
					andeanOscillator = AndeanOscillatorSignalMod(AndeanLength, AndeanSignalLength);
					if (ShowAndean) AddChartIndicator(andeanOscillator);
				}
				if (EnableBalaBot) 
				{
					Bala1 = Bala2Channels(Bala_Dev1, Bala_XS1, Bala_XL1, Bala_RSIPeriod1, Bala_EMAPeriod1, Bala_Dev2, Bala_XS2, Bala_XL2, Bala_RSIPeriod2, Bala_EMAPeriod2, 0.1);
					if (ShowBalaBot) AddChartIndicator(Bala1);
				}
				if (EnableGliderBot)
				{
					botSMI = Zombie3SMI("Zombie3SMI", SMIEMAPeriod1, SMIEMAPeriod2, SMIRange, SMIEMAPeriod);
					if (ShowSMIPlot) AddChartIndicator(botSMI);
				}
				if (EnableZigZagBot)
				{
					botZigZag = ZigZag(ZigZagDeviationType, ZigZagDeviationValue, ZigZagUseHighLow);			    
					if (ShowZigZagPlot) AddChartIndicator(botZigZag);
				}
				if (EnableSessionFaderBot)
				{
					botSessionFader = CurrentDayOHL();
					if (ShowSessionFaderPlot) AddChartIndicator(botSessionFader);
				}
				if (EnableSmartMoneyBot)
				{
					botMarketStructure = MarketStructuresLite(MarketStructurePeriod, true, 10, 14, Brushes.DodgerBlue, Brushes.Crimson, 2, NinjaTrader.Gui.DashStyleHelper.Solid, true, Brushes.Green, Brushes.Red, Brushes.Gray, 50, true, "LE", "SE", MarketStructureUseContinuations, MarketStructureUseReversals);
					if (ShowMarketStructurePlot) AddChartIndicator(botMarketStructure);
				}
				if (EnablePivotty)
				{
					botSvePivots = NTSvePivots(true, SvePivotsRangeType, SvePivotsCalcMode, 0, 0, 0, SvePivotsWidth);
					if (ShowPivotty) AddChartIndicator(botSvePivots);
				}
				if (EnableWilly)
				{
					botWilly = WilliamsR(wrPeriod);
					if (ShowWilly) AddChartIndicator(botWilly);
				}
			}
			
			if (EnableBreakoutBots)
			{
				if (EnableBollingerBot)
				{
					botBollinger = (regimeBollinger != null) ? regimeBollinger : Bollinger(BollingerStdDev, BollingerPeriod);
					if (ShowExhaustionBB)
	                {
	                    botBollinger.Plots[0].Brush = Brushes.Orange;
						botBollinger.Plots[0].Width = 2;
	                    botBollinger.Plots[1].Brush = Brushes.Transparent;
						botBollinger.Plots[2].Width = 2;
	                    botBollinger.Plots[2].Brush = Brushes.Orange;
	                    AddChartIndicator(botBollinger);
	                }			
				}
				if (EnableKeltnerBot)
				{
					botKeltner = KeltnerChannel(KeltnerOffsetMultiplier, KeltnerPeriod);
					if (ShowKeltnerPlot) AddChartIndicator(botKeltner);
				}
				if (EnableSessionBreakerBot)
				{
					botSessionBreaker = CurrentDayOHL();
					if (ShowSessionBreakerPlot) AddChartIndicator(botSessionBreaker);
				}
				if (EnableORBBot)
				{
					string timeZoneString = "Local";
					if (!string.IsNullOrEmpty(ORBTimeZone))
					{
						try { TimeZoneInfo.FindSystemTimeZoneById(ORBTimeZone); timeZoneString = ORBTimeZone; }
						catch { Print($"Warning: ORB Time Zone '{ORBTimeZone}' not found. Defaulting to Local."); }
					}
					botORB = ORB_TradeSaber(true, 0.2, ORBStartTime, ORBEndTime, timeZoneString, "", true, "", "", true, "", "", false, false, "", "", false, "", "", "", "", "");
					if (ShowORBPlot) AddChartIndicator(botORB);
				}
				if (EnableTTMSqueezeBot)
				{
					botTTMSqueeze = TTMSqueezeTradeSaber(TTMBollingerDeviation, TTMBollingerPeriod, TTMKeltnerMultiplier, TTMKeltnerPeriod, TTMMomentumPeriod, TTMMomentumEMAPeriod, false, "", "", false, "", "", false, "", "", "", "", "");
					if (ShowTTMSqueezePlot) AddChartIndicator(botTTMSqueeze);
				}
			}
			
			if (EnableEngulfingReversalBot || EnableEngulfingContinuationBot)
			{
			    botEngulfing = EngulfingBarTS(0.0, 0.0, EngulfingTickOffset, EngulfingEngulfBody, ShowEngulfingPlot, Brushes.DarkCyan, Brushes.Indigo, ShowEngulfingPlot, Brushes.LightBlue, Brushes.Plum, false, Brushes.Green, false, Brushes.Green, false, "", "", "", "", "");
			    if(ShowEngulfingPlot) AddChartIndicator(botEngulfing);
			}
			
			if (EnableTrendArchitectBot)
			{
			    botTrendArchitect = TrendArchitectLite(true, 50, 30, true, ShowTrendArchitectPlot, 18, 0.5, 50, 3.0, 20, 0.85, 6.0, 10, 0.5, 20, 30, 25);
			    
			    if (ShowTrendArchitectPlot)
			    {
			        AddChartIndicator(botTrendArchitect);
			    }
			}
        }		
		
        #endregion
		
    }
}