// --- KCAlgoBase.cs ---
// Version 6.6.9 - Universal Trend Filtering
// Key Changes:
// 1. LOGIC CHANGE: Removed the exception for "Ranging" markets in the IsTrendingUp/Down methods.
//    The Master Trend Filter is now ALWAYS active, ensuring all trades respect the dominant trend, as requested.

#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using IOPath = System.IO.Path;
#endregion

//This is a partial class file.
//It contains the core strategy logic, state management, and lifecycle events.
namespace NinjaTrader.NinjaScript.Strategies.KCStrategies
{
    public abstract partial class KCAlgoBase : Strategy
    {
        #region Enums for State Management
        public enum TradingMode { Auto, Disabled, DisabledByChop }	
		public enum MasterTrendFilterMode { MED, VMA, Disabled }
        public enum StopManagementType { HighLowTrail, DynamicTrail, ParabolicTrail, ATRTrail, RegularTrail, FixedStop }
        public enum ProfitTargetType { RegChan, ATR, RiskRewardRatio, Fixed }	
		public enum MarketRegime { Undefined, Trending, Ranging, Breakout }	
		public enum TradeManagementMode { Static, Dynamic }
		public enum DynamicCalculationMode { Average, Median, Percentile }
        #endregion

        #region Variables
        private double lastEntryConfluenceScore = 0; 	
		private long uniqueTradeCounter = 0;
		private bool exitedThisBar = false;
		public MarketRegime currentRegime = MarketRegime.Undefined;		
		
		private double dynamicStopLossTicks = 0;
		private double dynamicProfitTargetTicks = 0;
		
		private double currentTradeInitialSLTicks = 0;
		private double currentTradeInitialTPTicks = 0;		
		private string currentTradeSignalSource = "---";
        private string currentTradeStopType = "---";
        private string currentTradeProfitType = "---";
        private double currentTradeBeTriggerTicks = 0;
        private double entryAtr = 0;
        private double currentTradeSlippageTicks = 0;
		
		private List<double> recentMfeTicks = new List<double>();
		private List<double> recentMaeTicks = new List<double>();
				
		[Browsable(false)][XmlIgnore]
		public TradingMode CurrentMode { get; private set; }
		public bool isLongEnabled;
		public bool isShortEnabled;		
		
		private DateTime			currentDate			=	NinjaTrader.Core.Globals.MinDate;
		private double				currentOpen			=	double.MinValue;
		private double				highOfDay			=	double.MinValue;
		private double				lowOfDay			=	double.MaxValue;
		private double				range;
		private DateTime			lastDate			= 	NinjaTrader.Core.Globals.MinDate;
		private SessionIterator		sessionIterator;

		private double currentTradeMaxDrawdownTicks = 0;
		private double currentTradeMaxProfitTicks = 0;
        private double highSinceEntry = 0;
        private double lowSinceEntry = 0;
		
        private DateTime lastEntryTime;
        private readonly TimeSpan tradeDelay = TimeSpan.FromSeconds(5);
		
		private List<(Func<DateTime> Start, Func<DateTime> End, Func<bool> IsEnabled)> tradingSessions;		
		private List<string> activeTradeSignalNames = new List<string>();
		private bool isManualTradeActive = false; 

	    [XmlIgnore]
	    protected bool profitTargetsSet = false;
		
	    private string LogFilePath;
	    private static readonly object LogLock = new object();
	    private bool loggerInitialized = false;		
		
		private TradeLogger tradeLogger;		
		private TradeJsonLogger tradeJsonLogger;

        private Dictionary<string, int> printedMessages = new Dictionary<string, int>();
	    private string calculatedChartTypeDisplay = string.Empty;
		
        protected bool vmaUp;
        protected bool vmaDown;
        protected bool momoUp;
        protected bool momoDown;
        protected bool momoExtremeUp;
        protected bool momoExtremeDown;
		
		protected double currentMomentum;
		protected double currentAdx;
		protected double currentAtr;

		protected bool priceUp;
		protected bool priceDown;
			
        protected bool longSignal;
        protected bool shortSignal;
        protected bool isLong;
        protected bool isShort;
        protected bool exitLong;
        protected bool exitShort;
        protected bool isFlat;
        protected double trailValueInTicks;
		
        private bool BE_Realized;
        private bool trailingDrawdownReached = false;
		private int highLowTrailCurrentLookback; 
		
		private StopManagementType userSelectedStopType;
		private bool isUserStopTypeSet = false;
		
		private int barsInCurrentTrade = 0;
		private int entryBarNumberOfTrade = -1;
		
        protected double entryPrice;
        private double currentPrice;
        private bool additionalContractExists;

        private int counterLong;
        private int counterShort;

        private readonly object orderLock = new object();
        private Dictionary<string, Order> activeOrders = new Dictionary<string, Order>();
        private DateTime lastOrderActionTime = DateTime.MinValue;
        private readonly TimeSpan minOrderActionInterval = TimeSpan.FromSeconds(1);
        protected bool orderErrorOccurred = false;

        private DateTime lastAccountReconciliationTime = DateTime.MinValue;
        private readonly TimeSpan accountReconciliationInterval = TimeSpan.FromMinutes(5);

		private double initialAccountSizeForBacktest;
		private bool initialCapitalCaptured = false;
		
        protected double maxProfit;

        private volatile bool _isBuyRequested = false;
        private volatile bool _isSellRequested = false;
	    private volatile bool _isAddOneRequested = false;
	    private volatile bool _isCloseOneRequested = false;
        private volatile bool _isMoveToBERequested = false;
        private volatile bool _isMoveToSwingPointRequested = false;
        private volatile bool _isMoveTS50PctRequested = false;
		private volatile bool _isErrorResetRequested = false;
		
        private int rejectedOrderCount = 0;
        private DateTime lastTickTime = DateTime.MinValue;
        private bool isHealthCheckDisabled = false;
		
		private int dataLossCounter = 0;
		
        protected double totalPnL;
        protected double cumPnL;
        protected double dailyPnL;
		
		private int sessionWins = 0;
		private int sessionLosses = 0;
		
        [Browsable(false)][XmlIgnore]
        public string a_trendStatus = "---";
        [Browsable(false)][XmlIgnore]
        public string a_signalStatus = "---";
		[Browsable(false)][XmlIgnore]
		public string a_lastSignalSource = "---";

		private Brush backgroundUpBrush;
		private Brush backgroundDownBrush;
		
		private List<ISignalBot> allBots;
        private List<ISignalBot> trendBots;
        private List<ISignalBot> rangeBots;
        private List<ISignalBot> breakoutBots;
        private List<ISignalBot> universalBots;
        #endregion

        public override string DisplayName { get { return Name; } }

        #region OnStateChange
		
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
				Description									= @"Base Strategy with OEB v.5.0.2 TradeSaber(Dre). and ArchReactor for KC (Khanh Nguyen)";
				Name										= "KCAlgoBase";
				BaseAlgoVersion								= "KCAlgoBase v6.6.9";
				Author										= "indiVGA, Khanh Nguyen, Oshi, Johny, based on ArchReactor";
				StrategyVersion								= "6.6.9 Aug 2025";
				Credits										= "";
				StrategyName 								= "";
				ChartType									= "Tbars 28";	
				paypal 										= "https://www.paypal.com/signin"; 		

                EntriesPerDirection 						= 10;
                Calculate									= Calculate.OnEachTick;
				EntryHandling 								= EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy 				= true;
                ExitOnSessionCloseSeconds 					= 30;
                IsFillLimitOnTouch 							= false;
                MaximumBarsLookBack 						= MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution 						= OrderFillResolution.Standard;
                Slippage 									= 0;
                StartBehavior 								= StartBehavior.WaitUntilFlat;
                TimeInForce 								= TimeInForce.Gtc;
                TraceOrders 								= false;
                RealtimeErrorHandling 						= RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling 							= StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade 						= 250; 
				IsInstantiatedOnEachOptimizationIteration 	= false;
				
                SetPropertyDefaults();				
				
                CurrentMode = TradingMode.Auto;
                isLongEnabled = true;
                isShortEnabled = true;				
				isManualTradeActive = false;
            }
            else if (State == State.Configure)
            {
				RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                AddDataSeries(BarsPeriodType.Tick, 1);
            }
            else if (State == State.DataLoaded)
            {					
				sessionIterator = new SessionIterator(Bars);
		        InitializeLoggers();
		        InitializeChartDisplayHelpers();
				InitializeBotFramework();
		        InitializeAllIndicators();

                tradingSessions = new List<(Func<DateTime> Start, Func<DateTime> End, Func<bool> IsEnabled)>
                {
                    (() => Start,  () => End,  () => true),
                    (() => Start2, () => End2, () => Time2),
                    (() => Start3, () => End3, () => Time3),
                    (() => Start4, () => End4, () => Time4),
                    (() => Start5, () => End5, () => Time5),
                    (() => Start6, () => End6, () => Time6)
                };
		
		        if (!isUserStopTypeSet)
		        {
		            userSelectedStopType = StopType;
		            isUserStopTypeSet = true;
		        }
		        
		        maxProfit = totalPnL;
				
				backgroundUpBrush = new SolidColorBrush(Color.FromArgb(32, Colors.Lime.R, Colors.Lime.G, Colors.Lime.B));
				backgroundUpBrush.Freeze();
				
				backgroundDownBrush = new SolidColorBrush(Color.FromArgb(32, Colors.Crimson.R, Colors.Crimson.G, Colors.Crimson.B));
				backgroundDownBrush.Freeze();							
            }
			else if (State == State.Historical)
			{
				if (ChartControl != null) Dispatcher.InvokeAsync((() => { CreateWPFControls(); }));	
				
				if (!Bars.BarsType.IsIntraday) Draw.TextFixed(this, "NinjaScriptInfo", "Error: Daily OHLC requires intra-day data.", TextPosition.BottomRight);
			}
			else if (State == State.Terminated)
			{
				if(ChartControl != null) ChartControl.Dispatcher.InvokeAsync(() => { DisposeWPFControls(); });
			}
        }
		#endregion

		#region Initialization
		
		private void InitializeLoggers()
		{
		    if (EnableTradeLogging && !string.IsNullOrEmpty(TradeLogFileName))
		    {
		        try
		        {
		            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NinjaTrader 8", TradeLogFileName);
		            tradeLogger = new TradeLogger(path);
		        }
		        catch (Exception ex) { Print($"Error initializing CSV Logger: {ex.Message}"); }
		    }
		    if (EnableJsonLogging && !string.IsNullOrEmpty(JsonLogFileName))
		    {
		        try
		        {
		            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NinjaTrader 8", JsonLogFileName);
		            tradeJsonLogger = new TradeJsonLogger(path);
		        }
		        catch (Exception ex) { Print($"Error initializing JSON Logger: {ex.Message}"); }
		    }
		}

		private void InitializeChartDisplayHelpers()
		{
		     if (ShowOpen || ShowHigh || ShowLow)
		     {
		        dailyOHLIndicator = CurrentDayOHL();
		        dailyOHLIndicator.ShowOpen = ShowOpen;
		        dailyOHLIndicator.ShowHigh = ShowHigh;
		        dailyOHLIndicator.Plots[1].Brush = Brushes.Lime;
		        dailyOHLIndicator.ShowLow  = ShowLow;
		        AddChartIndicator(dailyOHLIndicator);
		     }
		     
	        try
	        {
	            string barTypeName = "Unknown";
	
	            Type[] types = NinjaTrader.Core.Globals.AssemblyRegistry.GetDerivedTypes(typeof(BarsType));
	            for (int i = 0; i < types.Length; i++)
	            {
	                Type type = types[i];
	                if (type == null || type.FullName.IsNullOrEmpty()) continue;
	                var type2 = NinjaTrader.Core.Globals.AssemblyRegistry.GetType(type.FullName);
	                if (type2 == null) continue;
	                BarsType bar = Activator.CreateInstance(type2) as BarsType;
	                if (bar != null)
	                {
	                    bar.SetState(State.SetDefaults);
	                    int id = (int)bar.BarsPeriod.BarsPeriodType;
	                    if (id == (int)Bars.BarsPeriod.BarsPeriodType)
	                    {
	                        barTypeName = bar.Name;
	                        bar.SetState(State.Terminated);
	                        break;
	                    }
	                    bar.SetState(State.Terminated);
	                }
	            }
	
	            string displayString = $"{barTypeName} {Bars.BarsPeriod.Value}";
	
	            if (Bars.BarsPeriod.BaseBarsPeriodValue > 0 && Bars.BarsPeriod.BaseBarsPeriodValue != Bars.BarsPeriod.Value)
	                displayString += $"-{Bars.BarsPeriod.BaseBarsPeriodValue}";
	            
	            if (Bars.BarsPeriod.GetType().GetProperty("Value3") != null)
	            {
	                var value3Property = Bars.BarsPeriod.GetType().GetProperty("Value3");
	                if (value3Property != null)
	                {
	                    var value3 = (int)value3Property.GetValue(Bars.BarsPeriod);
	                    if (value3 > 0) displayString += $"-{value3}";
	                }
	            }

	            if (Bars.BarsPeriod.MarketDataType != MarketDataType.Unknown)
	                displayString += $" ({Bars.BarsPeriod.MarketDataType})";
	
	            calculatedChartTypeDisplay = displayString;
	        }
	        catch (Exception ex)
	        {
	            LogError("Failed to calculate chart type display", ex);
	            calculatedChartTypeDisplay = "Unknown Chart Type";
	        }	
		}

		#endregion
		
        #region OnBarUpdate
		
		protected override void OnBarUpdate()
		{	if (CurrentBars[0] < BarsRequiredToTrade || CurrentBars[1] < BarsRequiredToTrade)
				return;		
		
			if (!initialCapitalCaptured && Account.Get(AccountItem.NetLiquidation, Currency.UsDollar) > 0)
			{
				initialAccountSizeForBacktest = Account.Get(AccountItem.NetLiquidation, Currency.UsDollar);
				initialCapitalCaptured = true;
			}
			
			if (BarsInProgress != 0 || orderErrorOccurred) return;
			if(Time[0] - lastEntryTime < tradeDelay) return;
					
			ManageIndicatorVisibility();
			
			ProcessTickBasedLogic();

			if (IsFirstTickOfBar)
			{
				ProcessBarBasedLogic();
			}
		}
		
		private void ProcessTickBasedLogic()
		{
			UpdatePnL();
			CheckHealth();
			CheckTrailingDrawdown();
			
			if (!isFlat)
			{
				ManageStopLoss();
				if (BESetAuto && !BE_Realized) ManageAutoBreakeven();
			}
			
			ProcessButtonRequests();

			if (showDailyPnl) 
				DrawStrategyPnL();
		}
		
		private void ProcessBarBasedLogic()
		{
            if (BarsInProgress != 0)
                return;

			if (!isFlat)
			{
				if (High[0] > highSinceEntry) highSinceEntry = High[0];
				if (Low[0] < lowSinceEntry) lowSinceEntry = Low[0];
			}

			priceUp = Close[0] > Close[1];
			priceDown = Close[0] < Close[1];
			
			exitedThisBar = false;
			longSignal = false;
			shortSignal = false;
			
			UpdatePositionState();
			UpdateSessionAndBarMetrics();
			
			if (State == State.Realtime && DateTime.Now - lastAccountReconciliationTime > accountReconciliationInterval)
			{
				ReconcileAccountOrders();
				lastAccountReconciliationTime = DateTime.Now;
			}
	
			UpdateIndicatorValues();
			UpdateMarketState();
			
			if (CurrentMode == TradingMode.Auto)
			{
				ExecuteAutoTradingLogic();
			}
			
			UpdateBackgroundColor();

			if (showPnl) ShowPNLStatus();
			
			UpdateTradeCounters();
			ResetTradeStateIfFlat();
			CheckForCustomExits();
			KillSwitch();
		}
		
		private void CheckHealth()
		{
			if (State != State.Realtime || !EnableHealthChecks || isHealthCheckDisabled)
			{
				lastTickTime = DateTime.Now; 
				dataLossCounter = 0;
				return;
			}

			if (lastTickTime != DateTime.MinValue && (DateTime.Now - lastTickTime).TotalSeconds > DataLossTimeoutSeconds)
			{
				dataLossCounter++;
				Print($"HEALTH WARNING: No new tick in {DataLossTimeoutSeconds}s. Strike {dataLossCounter} of 3.");
				
				if (dataLossCounter >= 3)
				{
					Print($"HEALTH ALERT: 3 consecutive data loss timeouts. Disabling strategy.");
					isHealthCheckDisabled = true;
					SetTradingMode(TradingMode.Disabled);
				}
				lastTickTime = DateTime.Now;
			}
			else
			{
				dataLossCounter = 0;
				lastTickTime = DateTime.Now;
			}
	
			if (rejectedOrderCount >= MaxConsecutiveRejections)
			{
				Print($"HEALTH ALERT: {rejectedOrderCount} consecutive order rejections. Disabling strategy.");
				isHealthCheckDisabled = true;
				SetTradingMode(TradingMode.Disabled);
			}
		}

		private void UpdateSessionAndBarMetrics()
		{
			lastDate = currentDate;
			currentDate = sessionIterator.GetTradingDay(Time[0]);
			
			if (Bars.IsFirstBarOfSession)
			{
				ResetSessionMetrics();
			}

			if (currentOpen <= double.MinValue)
			{
				currentOpen = Open[0]; highOfDay = High[0]; lowOfDay = Low[0];
			}
			
			highOfDay = Math.Max(highOfDay, High[0]);
			lowOfDay = Math.Min(lowOfDay, Low[0]);
			range = (highOfDay - lowOfDay) / TickSize;
	
			if (!isFlat && entryBarNumberOfTrade > -1) { barsInCurrentTrade = CurrentBar - entryBarNumberOfTrade; } else { barsInCurrentTrade = 0; }
		}
		
		private void UpdateIndicatorValues()
		{
			if (ATR1 != null) { currentAtr = ATR1[0]; }
			if (DM1 != null) { currentAdx = DM1[0]; }
			if (botMomentum != null) { currentMomentum = botMomentum[0]; }
			
			momoUp = botMomentum != null && currentMomentum > MomoThreshold;
			momoDown = botMomentum != null && currentMomentum < -MomoThreshold;

			if (botMomentum != null && botMomentum[0] > botMomentum[1])
			{
				botMomentum.Plots[0].Brush = Brushes.Lime; 
				exitShort = true;
			}
			if (botMomentum != null && botMomentum[0] < botMomentum[1])
			{
				botMomentum.Plots[0].Brush = Brushes.Red; 
				exitLong = true;
			}

			momoExtremeUp = botMomentumExtremes != null && botMomentumExtremes[0] > botMomentumExtremes[1];
			momoExtremeDown = botMomentumExtremes != null && botMomentumExtremes[0] < botMomentumExtremes[1];
			
			vmaUp = botMomentumVmaDriver != null && botMomentumVmaDriver[0] > botMomentumVmaDriver[1];
			vmaDown = botMomentumVmaDriver != null && botMomentumVmaDriver[0] < botMomentumVmaDriver[1];
			
			if (EnableKingKhanh && botRegChanPlus != null)
				regChanPlusRange[0] = botRegChanPlus.UpperStdDevBand[0] - botRegChanPlus.LowerStdDevBand[0];
		}
		
		private void UpdateMarketState()
		{
			bool inChopZone = IsInChopZone();
			if (inChopZone)
			{
				currentRegime = MarketRegime.Undefined;
				if (CurrentMode == TradingMode.Auto) SetTradingMode(TradingMode.DisabledByChop);
			}
			else
			{
				if (CurrentMode == TradingMode.DisabledByChop) SetTradingMode(TradingMode.Auto);
				DetectMarketRegime();
			}
	
			SetDisplayStatus(inChopZone);
		}
		
		private void ExecuteAutoTradingLogic()
		{
			CheckForBotSignals();
			CheckForCustomSignals();
			
			if (isLongEnabled && IsLongEntryConditionMet()) 
				EnterLongPosition();
			
			if (isShortEnabled && IsShortEntryConditionMet()) 
				EnterShortPosition();
		}
		
		private void UpdateBackgroundColor()
		{
			if (!EnableTrendBackground) return;

			if (momoExtremeUp) BackBrush = backgroundUpBrush;
			else if (momoExtremeDown) BackBrush = backgroundDownBrush;
			else BackBrush = null;
		}
		
		private void UpdateTradeCounters()
		{
			if (TradesPerDirection && FilterMode != MasterTrendFilterMode.Disabled && botMomentumVmaDriver != null)
			{
				if (counterLong > 0 && Close[0] < botMomentumVmaDriver[0]) counterLong = 0;
				if (counterShort > 0 && Close[0] > botMomentumVmaDriver[0]) counterShort = 0;
			}
		}
		
		private void ResetTradeStateIfFlat()
		{
			if (!isFlat) return;
			
			trailValueInTicks = InitialStop;
			lock (orderLock) { activeOrders.Clear(); }
		}
		
		private void CheckForCustomExits()
		{
			if (ValidateExitLong()) 
			{
                ExitLong("Custom Exit");
				exitedThisBar = true;
			}
			
			if (ValidateExitShort())
			{
                ExitShort("Custom Exit");
				exitedThisBar = true;
			}
		}

		private void ResetSessionMetrics()
		{
			currentOpen = Open[0]; highOfDay = High[0]; lowOfDay = Low[0];
			SetTradingMode(TradingMode.Auto);
			isManualTradeActive = false; 
            EnableExhaustionFilter = false;
			
			if (State == State.Realtime && Account != null)
				cumPnL = Account.Get(AccountItem.RealizedProfitLoss, Currency.UsDollar);
			else
				cumPnL = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;

			maxProfit = totalPnL; 
			trailingDrawdownReached = false;
			
			sessionWins = 0;
			sessionLosses = 0;
		}
		
		#endregion
		
		#region Manage Indicator Visibilty
		private void ManageIndicatorVisibility()
		{
			if (!panelActive)
				return;
		
			bool showMVD = false;
			bool showMED = false;
		
			switch (FilterMode)
			{
				case MasterTrendFilterMode.VMA:
					showMVD = true;
					break;
				case MasterTrendFilterMode.MED:
					showMED = true;
					break;
				case MasterTrendFilterMode.Disabled:
				default:
					break;
			}
		
			if (botMomentumVmaDriver != null)
			{
				bool isVisible = botMomentumVmaDriver.Plots[0].Brush != Brushes.Transparent;
				if (isVisible && !showMVD)
				{
					botMomentumVmaDriver.Plots[0].Brush = Brushes.Transparent;
				}
				else if (!isVisible && showMVD)
				{
					if (botMomentumVmaDriver.MVD[0] > botMomentumVmaDriver.MVD[1])
						botMomentumVmaDriver.PlotBrushes[0][0] = botMomentumVmaDriver.UpColor;
					else
						botMomentumVmaDriver.PlotBrushes[0][0] = botMomentumVmaDriver.DownColor;
				}
			}
		
			if (filterMomentumExtremes != null)
			{
				bool isVisible = filterMomentumExtremes.Plots[0].Brush != Brushes.Transparent;
				if (isVisible && !showMED)
				{
					filterMomentumExtremes.Plots[0].Brush = Brushes.Transparent;
				}
				else if (!isVisible && showMED)
				{
					if (filterMomentumExtremes.MED[0] > filterMomentumExtremes.MED[1])
						filterMomentumExtremes.PlotBrushes[0][0] = filterMomentumExtremes.UpColor;
					else
						filterMomentumExtremes.PlotBrushes[0][0] = filterMomentumExtremes.DownColor;
				}
			}
		}
		#endregion
		
		#region OnPositionUpdate
		protected override void OnPositionUpdate(Position position, double averagePrice, int quantity, MarketPosition marketPosition)
		{			
		    if (position.MarketPosition == MarketPosition.Flat) 
		    {
				if (isManualTradeActive)
				{
					isManualTradeActive = false;
					SetTradingMode(TradingMode.Auto);
				}
		
				activeTradeSignalNames.Clear();
		
				UpdatePositionState();
				SetDisplayStatus(IsInChopZone());
				
		        BE_Realized = false;
				entryBarNumberOfTrade = -1;
				
		        if (SystemPerformance.AllTrades.Count > 0)
		        {
		            var lastTrade = SystemPerformance.AllTrades.Last();
					
					if (lastTrade.ProfitCurrency > 0)
						sessionWins++;
					else
						sessionLosses++;
					
		            double trueMfeTicks = 0;
		            double trueMaeTicks = 0;

		            if (lastTrade.Entry.MarketPosition == MarketPosition.Long)
		            {
		                trueMfeTicks = (highSinceEntry - lastTrade.Entry.Price) / TickSize;
		                trueMaeTicks = (lastTrade.Entry.Price - lowSinceEntry) / TickSize;
		            }
		            else // Short position
		            {
		                trueMfeTicks = (lastTrade.Entry.Price - lowSinceEntry) / TickSize;
		                trueMaeTicks = (highSinceEntry - lastTrade.Entry.Price) / TickSize;
		            }
					
					trueMfeTicks = Math.Max(0, trueMfeTicks);
            		trueMaeTicks = Math.Max(0, trueMaeTicks);
		
		            if (EnableTradeLogging && tradeLogger != null)
		            {
		                if (!tradeLogger.LogTrade(Name, lastTrade, trueMfeTicks, trueMaeTicks,
		                                           currentRegime.ToString(), currentTradeSignalSource, currentTradeStopType, currentTradeProfitType, lastEntryConfluenceScore, currentAdx, currentAtr, currentMomentum, 
												   barsInCurrentTrade, currentTradeInitialSLTicks, currentTradeInitialTPTicks, currentTradeBeTriggerTicks, currentTradeSlippageTicks, out string errorMsg))
		                {
		                    Print($"CSV Logger Error: {errorMsg}");
		                }
		            }
		            if (EnableJsonLogging && tradeJsonLogger != null)
		            {
		                if (!tradeJsonLogger.LogTrade(Name, lastTrade, trueMfeTicks, trueMaeTicks,
		                                             currentRegime.ToString(), currentTradeSignalSource, currentTradeStopType, currentTradeProfitType, lastEntryConfluenceScore, currentAdx, currentAtr, currentMomentum,
													 barsInCurrentTrade, currentTradeInitialSLTicks, currentTradeInitialTPTicks, currentTradeBeTriggerTicks, currentTradeSlippageTicks, out string jsonErrorMsg))
		                {
		                     Print($"JSON Logger Error: {jsonErrorMsg}");
		                }
		            }
		
		            if (ManagementMode == TradeManagementMode.Dynamic)
		            {
						if (trueMfeTicks > 0)
						{
							recentMfeTicks.Add(trueMfeTicks);
							if (recentMfeTicks.Count > DynamicAvgLookback) recentMfeTicks.RemoveAt(0);
						}
						recentMaeTicks.Add(trueMaeTicks);
						if (recentMaeTicks.Count > DynamicAvgLookback) recentMaeTicks.RemoveAt(0);
		
		                if (SystemPerformance.AllTrades.Count >= DynamicBurnInTrades)
		                {
		                    dynamicProfitTargetTicks = CalculateDynamicValue(recentMfeTicks, 0);
							dynamicStopLossTicks = CalculateDynamicValue(recentMaeTicks, DynamicSLPadding);
		
		                    Print($"Dynamic Learning: Next TP ({DynamicRiskMode}): {dynamicProfitTargetTicks:F0}. Next SL ({DynamicRiskMode}): {dynamicStopLossTicks:F0}.");
		                }
		                else
		                {
		                    Print($"Dynamic Burn-In: {SystemPerformance.AllTrades.Count}/{DynamicBurnInTrades} trades completed. Using initial SL/TP.");
		                }
		            }
		        }
		        
		        lastEntryConfluenceScore = 0;					
				currentTradeSignalSource = "---";
                currentTradeStopType = "---";
                currentTradeProfitType = "---";
                currentTradeBeTriggerTicks = 0;
			    currentTradeSlippageTicks = 0;
				a_lastSignalSource = "---";
		
		        double currentTotalPnL = (State == State.Realtime && Account != null) ? Account.Get(AccountItem.RealizedProfitLoss, Currency.UsDollar) : SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
		        double currentDailyPnL = currentTotalPnL - cumPnL;
		
		        if (enableTrailingDrawdown && trailingDrawdownReached && totalPnL > maxProfit - TrailingDrawdown)
		        {
		            trailingDrawdownReached = false; 
		            SetTradingMode(TradingMode.Auto);
		            Print("Trailing Drawdown Lifted. Strategy Re-Enabled!");
		        }
		        
		        if (dailyLossProfit && currentDailyPnL <= -DailyLossLimit) 
		            Print($"Daily Loss Limit of {DailyLossLimit:C} has been hit. No more new auto trades will be taken today.");
		        
		        if (dailyLossProfit && currentDailyPnL >= DailyProfitLimit) 
		            Print($"Daily Profit Limit of {DailyProfitLimit:C} has been hit. No more new auto trades will be taken today.");
		    }
			else // Position is NOT flat, update live MFE/MAE using tick data
			{
				double pnlInTicks = position.GetUnrealizedProfitLoss(PerformanceUnit.Ticks, Close[0]);
			    if (pnlInTicks > currentTradeMaxProfitTicks) currentTradeMaxProfitTicks = pnlInTicks;
			    if (pnlInTicks < currentTradeMaxDrawdownTicks) currentTradeMaxDrawdownTicks = pnlInTicks;
			}
		}

		private double CalculateDynamicValue(List<double> data, double padding)
		{
			if (data == null || !data.Any()) return 0;
			
			switch(DynamicRiskMode)
			{
				case DynamicCalculationMode.Median:
					var sortedData = data.OrderBy(n => n).ToList();
					int mid = sortedData.Count / 2;
					double median = (sortedData.Count % 2 != 0) ? sortedData[mid] : (sortedData[mid-1] + sortedData[mid]) / 2.0;
					return median + padding;
					
				case DynamicCalculationMode.Percentile:
					var sortedPData = data.OrderBy(n => n).ToList();
					if (!sortedPData.Any()) return 0;
					double index = (DynamicRiskPercentile / 100.0) * (sortedPData.Count - 1);
					if (index <= 0) return sortedPData.First() + padding;
					if (index >= sortedPData.Count - 1) return sortedPData.Last() + padding;
					
					int lowerIndex = (int)Math.Floor(index);
					double fraction = index - lowerIndex;
					double percentile = sortedPData[lowerIndex] + fraction * (sortedPData[lowerIndex + 1] - sortedPData[lowerIndex]);
					return percentile + padding;
					
				case DynamicCalculationMode.Average:
				default:
					return data.Average() + padding;
			}
		}
		#endregion
		
		#region Dynamic SL, TP, and Sizing
		private double GetInitialStopInTicks()
		{
		    if (ManagementMode == TradeManagementMode.Dynamic) return dynamicStopLossTicks;
		    return InitialStop;
		}
		
		private double GetProfitTargetInTicks()
		{
		    if (ManagementMode == TradeManagementMode.Dynamic) return dynamicProfitTargetTicks;
		    return ProfitTarget;
		}
		
		protected int CalculatePositionSize()
		{
		    if (!EnableDynamicSizing || Account == null) 
		        return Contracts;
		
		    double stopLossTicks;
		
		    if (StopType == StopManagementType.ATRTrail)
		    {
		        if (ATR1 == null)
		        {
		            PrintOnce($"PosSizeWarning-NoATR-{CurrentBar}", "Position Sizing Warning: ATR not ready. Defaulting to 1 contract.");
		            return 1;
		        }
		        stopLossTicks = Math.Max(4, (ATR(AtrPeriod)[0] * atrMultiplier) / TickSize);
		    }
		    else
		    {
		        stopLossTicks = GetInitialStopInTicks();
		    }
		
		    if (stopLossTicks <= 0) 
		    {
		        PrintOnce($"PosSizeWarning-ZeroStop-{CurrentBar}", "Position Sizing Warning: Cannot calculate size because stop loss is zero. Defaulting to 1 contract.");
		        return 1;
		    }
		    
		    double stopLossCurrency = stopLossTicks * Instrument.MasterInstrument.PointValue / Instrument.MasterInstrument.TickSize;
		    if (stopLossCurrency <= 0)
		    {
		        PrintOnce($"PosSizeWarning-ZeroRisk-{CurrentBar}", "Position Sizing Warning: Stop loss in currency is zero. Defaulting to 1 contract.");
		        return 1;
		    }
		
		    double accountValue;
		    if (State == State.Realtime)
		    {
		        accountValue = Account.Get(AccountItem.NetLiquidation, Currency.UsDollar);
		    }
		    else
		    {
		        accountValue = initialAccountSizeForBacktest + SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
		    }
		
		    double riskAmount = accountValue * (RiskPerTradePercent / 100.0);
		    int positionSize = (int)Math.Max(1.0, Math.Floor(riskAmount / stopLossCurrency));
		    
		    PrintOnce($"PosSizeCalc-{CurrentBar}", $"Position Sizing: Account={accountValue:C}, Risk %={RiskPerTradePercent}, Risk Amt={riskAmount:C}, SL={stopLossCurrency:C}/contract ({stopLossTicks} ticks). Calculated Size: {positionSize}");
		    
		    return positionSize;
		}
		#endregion
		
		#region Market Regime and State Management
		private void DetectMarketRegime()
		{
		    if (ManualRegimeOverride != MarketRegime.Undefined)
		    {
		        currentRegime = ManualRegimeOverride;
		        return;
		    }
		    
		    if (!EnableAutoRegimeDetection || CurrentBar < BarsRequiredToTrade || regimeAdx == null || regimeMinBBW == null || regimeBBWidth == null)
		    {
		        currentRegime = MarketRegime.Undefined;
		        return;
		    }
		    
		    double currentAdx = regimeAdx[0];
		    double currentBBW = regimeBBWidth[0]; 
		    double minBBW = regimeMinBBW[0];
		
		    if (currentAdx > RegimeAdxTrendThreshold)
		    {
		        currentRegime = MarketRegime.Trending;
	            EnableExhaustionFilter = false;
		    }
		    else if (currentBBW > 0 && minBBW > 0 && (currentBBW / minBBW) < 1.1)
		    {
		        currentRegime = MarketRegime.Breakout;
	            EnableExhaustionFilter = false;
		    }
		    else if (currentAdx < RegimeAdxRangeThreshold)
		    {
		        currentRegime = MarketRegime.Ranging;
	            EnableExhaustionFilter = true;
		    }
		    else
		    {
		        currentRegime = MarketRegime.Undefined;
		    }
		}
		
        private bool IsInChopZone()
        {
            if (!EnableChopDetection || chopFilterLinReg == null || chopFilterAdx == null || chopVolma == null)
                return false;
            
            double slope = chopFilterLinReg[0] - chopFilterLinReg[1];
            bool adxIsLow = chopFilterAdx[0] < ChopAdxThreshold;
            bool slopeIsFlat = Math.Abs(slope) < FlatSlopeThreshold;
			bool volumeIsLow = chopVolma[0] < ChopVolmaThreshold;
            
            return adxIsLow && slopeIsFlat && volumeIsLow;
        }

		#region Trend Checking 
		public bool IsTrendingUp()
		{
			// The trend filter is now always active, regardless of the detected market regime.
			
			bool isNotOverbought = !EnableOverboughtOversoldFilter || (botRsi != null && botRsi[0] < RsiOverboughtLevel);
			if (!isNotOverbought)
				return false;
			
			// Always use the selected filter.
			switch (FilterMode)
			{
				case MasterTrendFilterMode.VMA:
					if (botMomentumVmaDriver == null) return false;
					return (botMomentumVmaDriver.MVD[0] - botMomentumVmaDriver.MVD[1]) > VmaSlopeThreshold && Close[0] > botMomentumVmaDriver.MVD[0];
				
				case MasterTrendFilterMode.MED:
					if (filterMomentumExtremes == null) return false;
					return Close[0] > filterMomentumExtremes.MED[0];

				case MasterTrendFilterMode.Disabled:
				default:
					return true; 
			}
		}

		public bool IsTrendingDown()
		{
			// The trend filter is now always active, regardless of the detected market regime.

			bool isNotOversold = !EnableOverboughtOversoldFilter || (botRsi != null && botRsi[0] > RsiOversoldLevel);
			if (!isNotOversold)
				return false;
			
			// Always use the selected filter.
			switch (FilterMode)
			{
				case MasterTrendFilterMode.VMA:
					if (botMomentumVmaDriver == null) return false;
					return (botMomentumVmaDriver.MVD[0] - botMomentumVmaDriver.MVD[1]) < -VmaSlopeThreshold && Close[0] < botMomentumVmaDriver.MVD[0];

				case MasterTrendFilterMode.MED:
					if (filterMomentumExtremes == null) return false;
					return Close[0] < filterMomentumExtremes.MED[0];

				case MasterTrendFilterMode.Disabled:
				default:
					return true;
			}
		}
		#endregion
		
		private void UpdatePositionState()
		{
			isLong = Position.MarketPosition == MarketPosition.Long;
			isShort = Position.MarketPosition == MarketPosition.Short;
			isFlat = Position.MarketPosition == MarketPosition.Flat;
			entryPrice = Position.AveragePrice;
			currentPrice = Close[0];
		    additionalContractExists = Position.Quantity > 1;			
		}

		public void SetTradingMode(TradingMode newMode)
		{
		    CurrentMode = newMode;
		    if (State >= State.Historical && ChartControl != null)
            {
                ChartControl.Dispatcher.InvokeAsync(() => UpdateManualAutoButtonStates());
            }
		}
		
        public void ToggleManagementMode()
        {
            SetManagementMode(ManagementMode == TradeManagementMode.Static ? TradeManagementMode.Dynamic : TradeManagementMode.Static);
        }
		#endregion

        #region UI, Entry, and Helper methods
        private void ProcessButtonRequests()
		{
		    if (_isErrorResetRequested)
		    {
		        _isErrorResetRequested = false;
		        if (orderErrorOccurred)
		        {
		            orderErrorOccurred = false;
		            Print($"{Time[0]}: Manual error state reset. Strategy is now operational.");
		        }
		    }
		
		    if (isFlat)
		    {
		        if (_isBuyRequested)
		        {
		            _isBuyRequested = false; 
					a_lastSignalSource = "Manual Buy";
		            int contractsToTrade = CalculatePositionSize();
		            uniqueTradeCounter++;
		            string uniqueSignalName = QLE + "_" + uniqueTradeCounter;
		
					isManualTradeActive = true;
					SetTradingMode(TradingMode.Disabled);

		            SubmitEntryOrder(uniqueSignalName, this.OrderType, contractsToTrade);
					if (!EnableDynamicSizing)
						EnterMultipleLongContracts(uniqueTradeCounter);
		        }
		        else if (_isSellRequested)
		        {
		            _isSellRequested = false; 
					a_lastSignalSource = "Manual Sell";
		            int contractsToTrade = CalculatePositionSize();
		            uniqueTradeCounter++;
		            string uniqueSignalName = QSE + "_" + uniqueTradeCounter;
		
					isManualTradeActive = true;
					SetTradingMode(TradingMode.Disabled);

		            SubmitEntryOrder(uniqueSignalName, this.OrderType, contractsToTrade);
					if (!EnableDynamicSizing)
						EnterMultipleShortContracts(uniqueTradeCounter);
		        }
		    }
		    else
		    {
		        _isBuyRequested = false; _isSellRequested = false;
		        if (_isAddOneRequested) { _isAddOneRequested = false; AddOneEntry(); }
		        if (_isCloseOneRequested) { _isCloseOneRequested = false; SafePartialClose(); }
                if (_isMoveToBERequested) { _isMoveToBERequested = false; MoveToBreakeven(); }
                if (_isMoveToSwingPointRequested) { _isMoveToSwingPointRequested = false; MoveStopToSwingPoint(); }
                if (_isMoveTS50PctRequested) { _isMoveTS50PctRequested = false; MoveTrailingStopByPercentage(0.5); }
		    }
		}
		
        protected bool IsLongEntryConditionMet()
		{
			if (!longSignal)
				return false;

            // NEW: Add the Market Structure Filter
            // Veto the signal if price is already trading below the most recently confirmed swing high.
            if (EnableMarketStructureFilter && botSwing != null && High[0] > botSwing.SwingHigh[1])
            {
                PrintOnce($"FilterVeto-Structure-Long-{CurrentBar}", "Long signal VETOED by Market Structure Filter (Price below recent Swing High).");
                return false;
            }

			if (!IsTrendingUp())
			{
				PrintOnce($"FilterVeto-Long-{CurrentBar}", $"Long signal from '{a_lastSignalSource}' was VETOED by the Master Trend Filter.");
				return false;
			}

		    return isFlat && !exitedThisBar && CurrentMode == TradingMode.Auto
		        && (dailyLossProfit ? dailyPnL > -DailyLossLimit && dailyPnL < DailyProfitLimit : true)
		        && !trailingDrawdownReached && IsValidTradingDay() && checkTimers()
		        && (!TradesPerDirection || counterLong < longPerDirection)
		        && priceUp && longSignal;
		}
		
		protected bool IsShortEntryConditionMet()
		{
			if (!shortSignal)
				return false;

            // NEW: Add the Market Structure Filter
            // Veto the signal if price is already trading above the most recently confirmed swing low.
            if (EnableMarketStructureFilter && botSwing != null && Low[0] < botSwing.SwingLow[1])
            {
                PrintOnce($"FilterVeto-Structure-Short-{CurrentBar}", "Short signal VETOED by Market Structure Filter (Price above recent Swing Low).");
                return false;
            }
			
			if (!IsTrendingDown())
			{
				PrintOnce($"FilterVeto-Short-{CurrentBar}", $"Short signal from '{a_lastSignalSource}' was VETOED by the Master Trend Filter.");
				return false;
			}
			
		    return isFlat && !exitedThisBar && CurrentMode == TradingMode.Auto
				&& (dailyLossProfit ? dailyPnL > -DailyLossLimit && dailyPnL < DailyProfitLimit : true)
				&& !trailingDrawdownReached && IsValidTradingDay() && checkTimers()
				&& (!TradesPerDirection || counterShort < shortPerDirection)
				&& priceDown && shortSignal;
		}
		
        private async void ExecuteScaledEntry(bool isLong, int totalContracts)
		{
		    if (totalContracts <= 0) return;
		
		    uniqueTradeCounter++;
		    string baseSignalPrefix = (isLong ? LE : SE) + "_" + uniqueTradeCounter;
		
		    int chunkSize = (int)Math.Max(1, Math.Floor((double)totalContracts / ScaleInChunks));
		    int remainingContracts = totalContracts;
		    
		    for (int i = 0; i < ScaleInChunks && remainingContracts > 0; i++)
		    {
		        int contractsToTrade = Math.Min(chunkSize, remainingContracts);
		        string signalName = baseSignalPrefix + "_c" + i;
		        remainingContracts -= contractsToTrade;
		
		        SubmitEntryOrder(signalName, OrderType.Market, contractsToTrade, false);
		        
		        if (remainingContracts > 0)
		        {
		            await Task.Delay(TimeSpan.FromSeconds(ScaleInDelaySeconds));
		            if (Position.MarketPosition == MarketPosition.Flat)
		            {
		                Print("Scaled entry cancelled: position went flat during execution.");
		                break; 
		            }
		        }
		    }
		}

        private void EnterLongPosition()
		{
		    counterLong++; counterShort = 0;
		    currentTradeSignalSource = a_lastSignalSource; 
            currentTradeStopType = StopType.ToString();
            currentTradeProfitType = PTType.ToString();
            currentTradeBeTriggerTicks = BETriggerTicks;
		    int contractsToTrade = CalculatePositionSize();
		    if (contractsToTrade == 0) return;
		
			highLowTrailCurrentLookback = HighLowTrailInitialLookback;
			
		    if (EnableScaleInExecution && contractsToTrade > 1)
		    {
		        ExecuteScaledEntry(true, contractsToTrade);
		    }
		    else
		    {
		        uniqueTradeCounter++;
		        string baseSignalName = LE + "_" + uniqueTradeCounter;
		
		        SubmitEntryOrder(baseSignalName, OrderType, contractsToTrade);
				if (!EnableDynamicSizing)
					EnterMultipleLongContracts(uniqueTradeCounter);
		    }
		    
		    Draw.ArrowUp(this, "EntryArrow" + CurrentBar, false, 0, Low[0] - 10 * TickSize, Brushes.Cyan);
		    lastEntryTime = Time[0];
		}
		
		private void EnterShortPosition()
		{
		    counterLong = 0; counterShort++;
		    currentTradeSignalSource = a_lastSignalSource;
            currentTradeStopType = StopType.ToString();
            currentTradeProfitType = PTType.ToString();
            currentTradeBeTriggerTicks = BETriggerTicks;
		    int contractsToTrade = CalculatePositionSize();
		    if (contractsToTrade == 0) return;
		
			highLowTrailCurrentLookback = HighLowTrailInitialLookback;
			
		    if (EnableScaleInExecution && contractsToTrade > 1)
		    {
		        ExecuteScaledEntry(false, contractsToTrade);
		    }
		    else
		    {
		        uniqueTradeCounter++;
		        string baseSignalName = SE + "_" + uniqueTradeCounter;
		
		        SubmitEntryOrder(baseSignalName, OrderType, contractsToTrade);
				if (!EnableDynamicSizing)
					EnterMultipleShortContracts(uniqueTradeCounter);
		    }
		    
		    Draw.ArrowDown(this, "EntryArrow" + CurrentBar, false, 0, High[0] + 10 * TickSize, Brushes.Yellow);
		    lastEntryTime = Time[0];
		}
		
        private bool IsValidTradingDay()
        {
            if (!EnableTradingDaysFilter) return true;
            switch (Time[0].DayOfWeek)
            {
                case DayOfWeek.Monday:    return TradeOnMonday;
                case DayOfWeek.Tuesday:   return TradeOnTuesday;
                case DayOfWeek.Wednesday: return TradeOnWednesday;
                case DayOfWeek.Thursday:  return TradeOnThursday;
                case DayOfWeek.Friday:    return TradeOnFriday;
                default:                  return false;
            }
        }
		
        protected void PrintOnce(string key, string message)
        {
            if (Bars == null || CurrentBar < 0) return;
            if (!printedMessages.TryGetValue(key, out int lastPrintedBar) || lastPrintedBar != CurrentBar)
            {
                Print(message);
                printedMessages[key] = CurrentBar;
            }
        }		
		
		private string GetPrimaryActiveSignalName()
		{
		    if (isLong) return LE;
		    if (isShort) return SE;
		    return null;
		}
		
		protected bool checkTimers()
		{
            if (tradingSessions == null) return false;

            TimeSpan currentTime = Times[0][0].TimeOfDay;
            foreach (var session in tradingSessions)
            {
                if (session.IsEnabled() && currentTime >= session.Start().TimeOfDay && currentTime <= session.End().TimeOfDay)
                {
                    return true;
                }
            }
            return false;
		}

		protected string GetActiveTimer()
		{
            if (tradingSessions == null) return "No active timer";

            TimeSpan currentTime = Times[0][0].TimeOfDay;
            foreach (var session in tradingSessions)
            {
                if (session.IsEnabled() && currentTime >= session.Start().TimeOfDay && currentTime <= session.End().TimeOfDay)
                {
                    return $"{session.Start():HH\\:mm} - {session.End():HH\\:mm}";
                }
            }
            return "No active timer";
		}
		
	    protected void InitializeLogger()
	    {
	      if (!EnableLogging || loggerInitialized) return;
	      try
	      {
	          string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
	          LogFilePath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "KCStrategies", $"KCStrategies{timestamp}.log");
	          string logDir = IOPath.GetDirectoryName(LogFilePath);
	          if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
	          if (!File.Exists(LogFilePath)) File.WriteAllText(LogFilePath, $"=== KCStrategies Log Started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
	          loggerInitialized = true;
	      }
	      catch (Exception ex) { Print($"Error initializing logger: {ex.Message}"); }
	    }

	    protected void LogMessage(string message, string level = "INFO")
	    {
	      if (!EnableLogging || !loggerInitialized || (State != State.Realtime && level != "ERROR")) return;
	      try
	      {
	          string log = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] Bar {CurrentBar} @ {Time[0]}: {message}";
	          lock (LogLock) { File.AppendAllText(LogFilePath, log + Environment.NewLine); }
	      }
	      catch (Exception ex) { Print($"Error writing to log file: {ex.Message}"); }
	    }
	
	    protected void LogError(string message, Exception ex = null)
	    {
	      if (!EnableLogging) return;
	      string fullMessage = ex != null ? $"{message} Error: {ex.Message}\nStack Trace: {ex.StackTrace}" : message;
	      LogMessage(fullMessage, "ERROR");
	    }
		#endregion
		
		#region PNL, Drawing & KillSwitch
		
		private void UpdatePnL()
		{
			if (Account == null) return;
			
		    double accountRealized = (State == State.Realtime) ? Account.Get(AccountItem.RealizedProfitLoss, Currency.UsDollar) : SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
		    double accountUnrealized = (State == State.Realtime) ? Account.Get(AccountItem.UnrealizedProfitLoss, Currency.UsDollar) : (Position.MarketPosition != MarketPosition.Flat ? Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]) : 0);
		    totalPnL = accountRealized + accountUnrealized;
		    dailyPnL = totalPnL - cumPnL;
		
		    if (totalPnL > maxProfit) 
				maxProfit = totalPnL;
		}

        private void SetDisplayStatus(bool inChopZone)
        {
            if (inChopZone)
            {
                a_trendStatus = "Choppy";
            }
            else
            {
                switch (currentRegime)
			    {
			        case MarketRegime.Trending:
			            if (DM1 != null) a_trendStatus = DM1.DiPlus[0] > DM1.DiMinus[0] ? "Trending Up" : "Trending Down";
						else a_trendStatus = "Trending";
			            break;
			        case MarketRegime.Ranging:  a_trendStatus = "Ranging"; break;
			        case MarketRegime.Breakout: a_trendStatus = "Breakout Setup"; break;
			        default: a_trendStatus = "Neutral"; break;
			    }
            }

            a_signalStatus = "No Signal";
            switch (CurrentMode)
            {
                case TradingMode.Disabled: a_signalStatus = "Manual Mode"; break;
                case TradingMode.DisabledByChop: a_signalStatus = "Auto OFF (Chop)"; break;
                case TradingMode.Auto:
                    if (!checkTimers()) a_signalStatus = "Outside Hours";
                    else if (orderErrorOccurred) a_signalStatus = "Order Error!";
                    else if (enableTrailingDrawdown && trailingDrawdownReached) a_signalStatus = "Drawdown Limit Hit";
                    else if (dailyLossProfit && dailyPnL <= -DailyLossLimit) a_signalStatus = "Loss Limit Hit";
                    else if (dailyLossProfit && dailyPnL >= DailyProfitLimit) a_signalStatus = "Profit Limit Hit";
                    else if (!isFlat) a_signalStatus = "In Position";
					else a_signalStatus = "Armed";
                    break;
            }
        }
		
		protected void DrawStrategyPnL()
		{
		    double accountNetLiquidation = (Account != null) ? Account.Get(AccountItem.NetLiquidation, Currency.UsDollar) : 0;
		    double realizedPnL = (Account != null) ? Account.Get(AccountItem.RealizedProfitLoss, Currency.UsDollar) : SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
		    double unrealizedPnL = (Account != null) ? Account.Get(AccountItem.UnrealizedProfitLoss, Currency.UsDollar) : (Position.MarketPosition != MarketPosition.Flat ? Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]) : 0);
		
		    double currentDrawdown = Math.Max(0, maxProfit - totalPnL);
		    double remainingDrawdown = TrailingDrawdown - currentDrawdown;
		    
		    string adxStatus;
		    if (currentAdx > AdxThreshold && DM1 != null)
		    {
		        adxStatus = DM1.DiPlus[0] > DM1.DiMinus[0] ? $"Up ({currentAdx:F1})" : $"Down ({currentAdx:F1})";
		    }
		    else { adxStatus = $"Choppy ({currentAdx:F1})"; }
		    
		    string momoStatus = momoUp ? $"Up ({currentMomentum:F1})" : momoDown ? $"Down ({currentMomentum:F1})" : $"Reversal ({currentMomentum:F1})";	
		    string atrText = currentAtr.ToString("F2");
			string vmaText = (botMomentumVmaDriver != null) ? (vmaUp ? "Up" : vmaDown ? "Down" : "Neutral") : "---";
		    string momoExtremeText = momoExtremeUp ? "Up" : momoExtremeDown ? "Down" : "Neutral";          
		    
		    double profitFactor = SystemPerformance.AllTrades.TradesPerformance.ProfitFactor;
		    double totalSessionTrades = sessionWins + sessionLosses;
		    double winRate = (totalSessionTrades > 0) ? ((double)sessionWins / totalSessionTrades) : 0;
			
			string tradeInfoText;
			if (isFlat)
			{
				tradeInfoText = $"Next SL/TP:\t{dynamicStopLossTicks:F0} / {dynamicProfitTargetTicks:F0} Ticks\n";
			}
			else
			{
				double liveMfeTicks = 0;
				double liveMaeTicks = 0;
				if (Position.MarketPosition == MarketPosition.Long)
				{
					liveMfeTicks = (highSinceEntry - Position.AveragePrice) / TickSize;
					liveMaeTicks = (Position.AveragePrice - lowSinceEntry) / TickSize;
				}
				else if (Position.MarketPosition == MarketPosition.Short)
				{
					liveMfeTicks = (Position.AveragePrice - lowSinceEntry) / TickSize;
					liveMaeTicks = (highSinceEntry - Position.AveragePrice) / TickSize;
				}
				tradeInfoText = $"MAE: {Math.Max(0, liveMaeTicks):F0} | MFE: {Math.Max(0, liveMfeTicks):F0}\n" +
								$"Current SL/TP:\t{currentTradeInitialSLTicks:F0} / {currentTradeInitialTPTicks:F0} Ticks\n";
			}

		    string realTimeTradeText =
		    $"{(Account?.Name ?? "N/A")}\n" +
		    $"({Account?.Connection?.Options?.Name ?? "Connecting..."})\n" +
		    $"Chart: {calculatedChartTypeDisplay}\n" +
		    $"PnL Source: {(State == State.Realtime ? "Account" : "System")}\n" +
		    $"Account Value:\t{accountNetLiquidation:C}\n" +
		    $"-------------\n" +
		    $"Daily PnL:\t{dailyPnL:C}\n" +
		    $"Realized:\t{realizedPnL:C}\n" +
		    $"Unrealized:\t{unrealizedPnL:C}\n" +
		    $"Total PnL:\t{totalPnL:C}\n" +
		    $"Profit Factor:\t{profitFactor:F2}\n" + 
		    $"Win Rate:\t{winRate:P1} ({sessionWins}W / {sessionLosses}L)\n" + 
		    $"-------------\n" +
		    $"Max Profit:\t{(maxProfit == double.MinValue ? "N/A" : maxProfit.ToString("C"))}\n" +
		    $"Current DD:\t{currentDrawdown:C}\n" +
		    $"Remaining:\t{remainingDrawdown:C}\n" +
		    $"-------------\n" +
		    tradeInfoText +	
		    $"-------------\n" +	
		    $"High:\t\t{(highOfDay == double.MinValue ? 0.0 : highOfDay):F2}\n" +
		    $"Low:\t\t{(lowOfDay == double.MaxValue ? 0.0 : lowOfDay):F2}\n" +
		    $"Range:\t\t{range} Ticks\n" +
		    $"-------------\n" +
		    $"ADX:\t\t{adxStatus}\n" +
		    $"Momentum:\t{momoStatus}\n" +
		    $"ATR:\t\t{atrText}\n" +
		    $"VMA:\t\t{vmaText}\n" +
		    $"Momo Extremes:\t{momoExtremeText}\n" +
		    $"-------------\n" +
		    $"Bot Signal:\t{a_lastSignalSource}\n" +
		    $"Trend:\t\t{a_trendStatus}\n" +
		    $"Signal:\t\t{a_signalStatus}";
		    
		    SimpleFont font = new SimpleFont("Arial", FontSize);
		    Brush pnlColor = totalPnL == 0 ? Brushes.Cyan : totalPnL > 0 ? Brushes.Lime : Brushes.Pink;
		    
		    Draw.TextFixed(this, "realTimeTradeText", realTimeTradeText, PositionDailyPNL, pnlColor, font, null, Brushes.Black, Transparency, DashStyleHelper.Solid, Transparency, false, "");
		}

		protected void ShowPNLStatus() {
			string statusPnlText = $"Active Timer: {GetActiveTimer()}\n" 
				+ $"Longs: {counterLong}/{longPerDirection} | Shorts: {counterShort}/{shortPerDirection}\n";
			Draw.TextFixed(this, "statusPnl", statusPnlText, PositionPnl, colorPnl, new SimpleFont("Arial", 16), Brushes.Transparent, Brushes.Transparent, 0);
		}
		
		private void CheckTrailingDrawdown()
		{
			if (enableTrailingDrawdown && !trailingDrawdownReached && (maxProfit - totalPnL) >= TrailingDrawdown)
			{
				if (!isFlat)
				{
					if (isLong) ExitLong("Trailing DD Exit");
					else if (isShort) ExitShort("Trailing DD Exit");
				}
				SetTradingMode(TradingMode.Disabled);
				trailingDrawdownReached = true;
				Print($"TRAILING DRAWDOWN LIMIT HIT! Max Profit was {maxProfit:C}, current PnL is {totalPnL:C}.");
			}
		}

		protected void KillSwitch()
		{
		    Action closeAllAndDisable = () =>
		    {
				if (!isFlat)
				{
					if (isLong) ExitLong("Session Limit Exit");
					else if (isShort) ExitShort("Session Limit Exit");
				}
				SetTradingMode(TradingMode.Disabled);
		    };
		
		    if (dailyLossProfit && dailyPnL <= -DailyLossLimit)
		    {
		        closeAllAndDisable();
				Print($"DAILY LOSS LIMIT HIT! Daily PnL is {dailyPnL:C}.");
		    }
		
		    if (dailyLossProfit && dailyPnL >= DailyProfitLimit)
		    {
		        closeAllAndDisable();
				Print($"DAILY PROFIT LIMIT HIT! Daily PnL is {dailyPnL:C}.");
		    }
		}
		#endregion
		
		#region Virtual Overrides & Child Strategy Hooks
		protected virtual void addDataSeries()       {}
		protected virtual void CheckForCustomSignals() {}
		
        protected virtual bool ValidateExitLong()
        {
            if (!EnableAutoExit || botMomentum == null)
                return false;
            
            return (exitLong);
//            return (exitLong || (Low[0] < Low[1] && Low[1] >= Low[2]));
        }

        protected virtual bool ValidateExitShort()
        {
            if (!EnableAutoExit || botMomentum == null)
                return false;

            return (exitShort);
//            return (exitShort || (High[0] > High[1] && High[1] <= High[2]));
        }
		#endregion
		
		#region --- BOT FRAMEWORK ---
		
		private void InitializeBotFramework()
		{
			allBots = new List<ISignalBot>
			{
				// Universal Bots
                new KingKhanhBot(), 
                new PivotImpulseBot(),
                
				// Trend Bots
				new	MomentumVmaBot(), new TrendArchitectBot(),
				new HookerBot(), new MomoBot(), new CoralBot(), new ChaserBot(),
				new SuperTrendBot(), new Johny5Bot(), new TSSuperTrendBot(), new MagicTrendyBot(),
				new T3FilterBot(), new TefaProBot(), new PsarBot(), new RangeFilterBot(), new SmaCompositeBot(),
				new Zombie9Bot(), new EngulfingContinuationBot(), new ZigZagBot(), new MomentumExtremesBot(), 
				
				// Range Bots
				new PivottyBot(), new WillyBot(), new AndeanBot(), new CasherBot(), new StochasticsBot(), new BalaBot(),
				new SuperRexBot(), new GliderBot(), new SmartMoneyBot(), new EngulfingReversalBot(), new ORBReversalBot(),
				new RsiBot(), new SessionFaderBot(), new ReaperBot(),
				
				// Breakout Bots
				new SessionBreakerBot(), new ORBBreakoutBot(), new TtmSqueezeBot(),
				new BollingerBot(), new KeltnerBot()
			};
			
			foreach(var bot in allBots)
			{
				bot.Initialize(this);
			}
			
			trendBots = allBots.Where(b => b.Regime == MarketRegime.Trending).ToList();
			rangeBots = allBots.Where(b => b.Regime == MarketRegime.Ranging).ToList();
			breakoutBots = allBots.Where(b => b.Regime == MarketRegime.Breakout).ToList();
            universalBots = allBots.Where(b => b.Regime == MarketRegime.Undefined).ToList();
		}
		
		private void CheckForBotSignals()
		{
		    a_lastSignalSource = "---";
            lastEntryConfluenceScore = 0; 

            if (!EnableConfluenceScoring)
            {
                if (FindSignal(universalBots)) return;
		
                if (EnableAutoRegimeDetection)
                {
                    if (EnableTrendBots && currentRegime == MarketRegime.Trending) { FindSignal(trendBots); }
                    else if (EnableRangeBots && currentRegime == MarketRegime.Ranging) { FindSignal(rangeBots); }
                    else if (EnableBreakoutBots && currentRegime == MarketRegime.Breakout) { FindSignal(breakoutBots); }
                }
                else
                {
                    if (EnableTrendBots && FindSignal(trendBots)) return;
                    if (EnableRangeBots && FindSignal(rangeBots)) return;
                    if (EnableBreakoutBots && FindSignal(breakoutBots)) return;
                }
            }
            else
            {
                List<ISignalBot> firedBots = new List<ISignalBot>();

                firedBots.AddRange(FindAllSignals(universalBots));
                if (EnableAutoRegimeDetection)
                {
                    if (EnableTrendBots && currentRegime == MarketRegime.Trending) { firedBots.AddRange(FindAllSignals(trendBots)); }
                    else if (EnableRangeBots && currentRegime == MarketRegime.Ranging) { firedBots.AddRange(FindAllSignals(rangeBots)); }
                    else if (EnableBreakoutBots && currentRegime == MarketRegime.Breakout) { firedBots.AddRange(FindAllSignals(breakoutBots)); }
                }
                else
                {
                    if (EnableTrendBots) { firedBots.AddRange(FindAllSignals(trendBots)); }
                    if (EnableRangeBots) { firedBots.AddRange(FindAllSignals(rangeBots)); }
                    if (EnableBreakoutBots) { firedBots.AddRange(FindAllSignals(breakoutBots)); }
                }
                
                if (firedBots.Count == 0) return;

                ISignalBot bestBot = null;
                SignalDirection bestDirection = SignalDirection.NoSignal;
                double bestScore = -1;

                foreach (var bot in firedBots.Distinct())
                {
                    SignalDirection direction = bot.CheckSignal(0);
                    if (direction != SignalDirection.NoSignal)
                    {
                        double score = CalculateConfluenceScore(direction);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestBot = bot;
                            bestDirection = direction;
                        }
                    }
                }

                if (bestBot != null && bestScore >= MinConfluenceScore)
                {
                    a_lastSignalSource = $"{bestBot.Name} (Score: {bestScore:F0})";
                    lastEntryConfluenceScore = bestScore;
                    if (bestDirection == SignalDirection.Long) longSignal = true;
                    else if (bestDirection == SignalDirection.Short) shortSignal = true;
                }
            }
		}
		
        private double CalculateConfluenceScore(SignalDirection direction)
        {
            double score = 0;
        
            if (direction == SignalDirection.Long && vmaUp)
                score += 40;
            else if (direction == SignalDirection.Short && vmaDown)
                score += 40;
            else
                return 0;

            if (direction == SignalDirection.Long && momoUp)
                score += 30;
            else if (direction == SignalDirection.Short && momoDown)
                score += 30;
			else
				score -= 15;

            if (currentAdx > 35)
                score += 20;
            else if (currentAdx > ConfluenceAdxThreshold)
                score += 10;
            else if (currentAdx < 20)
                score -= 20;

            if (EnableOverboughtOversoldFilter && botRsi != null)
            {
                if (direction == SignalDirection.Long)
                {
                    if (botRsi[0] < RsiOverboughtLevel)
                        score += 10;
                    else
                        score -= 40;
                }
                else if (direction == SignalDirection.Short)
                {
                    if (botRsi[0] > RsiOversoldLevel)
                        score += 10;
                    else
                        score -= 40;
                }
            }
            else
            {
                score += 10;
            }
        
            return Math.Max(0, score);
        }
		
		private bool FindSignal(List<ISignalBot> botCollection)
		{
			foreach (var bot in botCollection)
			{
				SignalDirection direction = bot.CheckSignal(0);
				if (direction == SignalDirection.Long)
				{
					a_lastSignalSource = bot.Name;
					longSignal = true;
					return true;
				}
				if (direction == SignalDirection.Short)
				{
					a_lastSignalSource = bot.Name;
					shortSignal = true;
					return true;
				}
			}
			return false;
		}

        private List<ISignalBot> FindAllSignals(List<ISignalBot> botCollection)
        {
            List<ISignalBot> firedBots = new List<ISignalBot>();
            foreach (var bot in botCollection)
            {
                if (bot.CheckSignal(0) != SignalDirection.NoSignal)
                {
                    firedBots.Add(bot);
                }
            }
            return firedBots;
        }

		#endregion
    }
}