#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Core;
using BlueZ = NinjaTrader.NinjaScript.Indicators.BlueZ; // Alias for clarity
using RegressionChannel = NinjaTrader.NinjaScript.Indicators.RegressionChannel;
#endregion

namespace NinjaTrader.NinjaScript.Strategies.KCStrategies
{
    // VolumetricAlgoBaseOnExecutionUpdateB2: Base strategy using volumetric data and multiple indicators to determine trading signals.
    public class VolumetricAlgoBaseOnExecutionUpdate : Strategy
    {
        #region Variables
        
        ////////// Indicator & Signal Flags //////////
        private bool hmaHooksUp;                    // Upward HMA hook flag
        private bool hmaHooksDown;                  // Downward HMA hook flag
		
        // Williams Oracle Signal from TradeSaber
        private NinjaTrader.NinjaScript.Indicators.TradeSaber_SignalMod.TOWilliamsTraderOracleSignalMOD WilliamsR1;
        private bool WillyUp;                       // Williams signal for long
        private bool WillyDown;                     // Williams signal for short
		
        // Market conditions management
        private bool marketIsChoppy;                // True if market is considered choppy
        private bool autoDisabledByChop;            // True if auto trading was disabled because of choppiness
		
        // Hull Moving Average Hooks
        private BlueZ.BlueZHMAHooks hullMAHooks;
        private bool hmaUp;
        private bool hmaDown;

        // Buy/Sell Pressure indicator and series storage
        private BuySellPressure BuySellPressure1;		
        private bool buyPressureUp;
        private bool sellPressureUp;
		private Series<double> buyPressure;
		private Series<double> sellPressure;	
        
        // Footprint Order Flow indicator
		private FootprintOrderFlow footprint1;
		private bool orderFlowUp;
		private bool orderFlowDown;
		
		// VWAP Key Level
		private OrderFlowVWAP VWAP1;
		
		// Prior Day OHLC Key Levels
		private PriorDayOHLC priorOHLC1;

        // Regression Channels for trend detection
        private RegressionChannel RegressionChannel1, RegressionChannel2;
        private RegressionChannelHighLow RegressionChannelHighLow1;
        private bool regChanUp;
        private bool regChanDown;

        // Volume-based Moving Average
        private VMA VMA1;
        private bool volMaUp;
        private bool volMaDown;

        // Daily Pivot Levels
        private NTSvePivots pivots1;
        private double pivotPoint, s1, s2, s3, r1, r2, r3, s1m, s2m, s3m, r1m, r2m, r3m;
		
		// Too close to Key Level
		private bool isKeyLevelInProximityLong;
		private bool isKeyLevelInProximityShort;

        // Momentum indicator
		private Momentum Momentum1;
		private double momentum;		
        private bool momoUp;
        private bool momoDown;
		
        // ADX for trend strength
        private ADX ADX1;
		private double currentAdx;
        private bool adxUp;

        // ATR for volatility
        private ATR ATR1;
        private bool atrUp;

        // EMA filter conditions
        private bool aboveEMAHigh;
        private bool belowEMALow;

        // Overall Trend Conditions
        private bool uptrend;
        private bool downtrend;
        private bool priceUp;
        private bool priceDown;

        // Trade Execution Flags (input parameters may modify these externally)
        public bool isLong;
        public bool isShort;
        public bool isFlat;
        public bool longSignal;
        public bool shortSignal;
		private bool longEntrySubmitted = false;
		private bool shortEntrySubmitted = false;
		private int longEntryOrdersubmissionBar = -1;
		private int shortEntryOrdersubmissionBar = -1;
		private List<Order> longEntryOrders = new List<Order>();
		private List<Order> shortEntryOrders = new List<Order>();
		private Order longEntryOrder;
		private Order shortEntryOrder;

		
		private Dictionary<string, Order> activeEntryOrders = new Dictionary<string, Order>();
		private Dictionary<string, List<string>> associatedExitSignalNames = new Dictionary<string, List<string>>();
		private Dictionary<string, int> entryOrderFilledQuantity = new Dictionary<string, int>();
		private List<Order> activeStrategyOrders = new List<Order>();

		

        // Trailing Stop / Drawdown Settings
        private bool _beRealized;
		private double highestPriceSinceBE = double.MinValue; // For long trails
		private double lowestPriceSinceBE = double.MaxValue;  // For short trails
        private bool trailingDrawdownReached = false;
		
        // ProgressState reserved for potential future use
        private int ProgressState;

        // Trade and Contract Management
        private bool additionalContractExists;
        private bool tradesPerDirection;
        private int counterLong;
        private int counterShort;

        // Enabled trading session flags (for multiple time windows)
        private bool isEnableTime2;
        private bool isEnableTime3;
        private bool isEnableTime4;
        private bool isEnableTime5;
        private bool isEnableTime6;

        // Profit and Loss Tracking
        private double totalPnL;
        private double cumPnL;
        private double dailyPnL;
        private bool canTradeOK = true;
		
		// Trading mode flags
        private bool isAutoEnabled;
        private bool isLongEnabled;
        private bool isShortEnabled;
		private static bool emergencyCleanupRun = false;
		
		//		Chart Trader Buttons
        private System.Windows.Controls.RowDefinition addedRowDefinition = null;
        private Gui.Chart.ChartTab chartTab;
        private Gui.Chart.Chart parentChart;
        private System.Windows.Controls.Grid chartTraderGrid, chartTraderButtonsGrid, lowerButtonsGrid;
		
        //		New Toggle Buttons
        private System.Windows.Controls.Button autoBtn, longBtn, shortBtn, closeBtn;
        private bool panelActive;
        private System.Windows.Controls.TabItem tabItem;
        private System.Windows.Controls.Grid myGrid, customButtonsGrid;
		private bool   buttonsCreated = false; // Flag to prevent duplicate creation
		
//		private const string ManualButton = "ManualBtn";
//		private const string AutoButton = "AutoBtn";
		private const string LongButton = "LongBtn";
		private const string ShortButton = "ShortBtn";
		private const string CloseButton = "CloseBtn";
       

        // Trailing Drawdown variable
        private double maxProfit;  // Highest profit achieved for trailing drawdown logic

        #endregion

        #region Order Label Constants (for consistency)
        private const string LE1 = "LE1";
		private const string LE2 = "LE2";
		private const string LE3 = "LE3";
		private const string LE4 = "LE4";
        private const string SE1 = "SE1";
        private const string SE2 = "SE2";
        private const string SE3 = "SE3";
        private const string SE4 = "SE4";
		
		private const string LESTOP1 = "LESTOP1";
		private const string LESTOP2 = "LESTOP2";
		private const string LESTOP3 = "LESTOP3";
		private const string LESTOP4 = "LESTOP4";
        private const string SESTOP1 = "SESTOP1";
        private const string SESTOP2 = "SESTOP2";
        private const string SESTOP3 = "SESTOP3";
        private const string SESTOP4 = "SESTOP4";
		
        private const string QLE = "QLE";
        private const string QSE = "QSE";
		private const string Add1LE = "Add1LE";
		private const string Add1SE = "Add1SE";
        private const string ManualClose1 = "Manual Close 1";
        #endregion

		#region Constants
        // Add future constants here as required.
		#endregion
		
        public override string DisplayName { get { return Name; } }

        #region OnStateChange
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                // Strategy meta data
				Description = @"Base Strategy with OEB v.5.0.2 TradeSaber(Dre) and ArchReactor for KC (Khanh Nguyen)";
				Name = "VolumetricAlgoBaseOnExecutionUpdate";
				BaseAlgoVersion = "VolumetricAlgoBaseOnExecutionUpdate";
				Author = "indiVGA, Khanh Nguyen, Oshi, based on ArchReactor";
				Version = "Version 5.2 Apr. 2025";
				Credits = "";
				StrategyName = "";
				ChartType = "Orenko 34-40-40";	
				paypal = "https://www.paypal.com/signin"; 

                // Core strategy parameters
                EntriesPerDirection = 10;
                Calculate = Calculate.OnEachTick;
				EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = true;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;
				IsInstantiatedOnEachOptimizationIteration = false;
				
                // Default trading mode and indicator enabling
				isAutoEnabled = true;
				isLongEnabled = true;
				isShortEnabled = true;
				canTradeOK = true;
				
				OrderType = OrderType.Limit;
				
		        // Choppiness and slope parameters
		        SlopeLookBack = 4;
		        FlatSlopeFactor = 0.125; 
		        ChopAdxThreshold = 20;
				EnableChoppinessDetection = false;
		        marketIsChoppy = false;
		        autoDisabledByChop = false;
				enableBackgroundSignal = true;
				
				enableBuySellPressure = true;
				showBuySellPressure = false;
				
				HmaPeriod = 16;
				enableHmaHooks = true;
				showHmaHooks = true;
	
				RegChanPeriod = 40;
				RegChanWidth = 4;
				RegChanWidth2 = 3;
				enableRegChan1 = true;
				enableRegChan2 = true;
				showRegChan1 = true;
				showRegChan2 = true;
				showRegChanHiLo = true;

				enableVMA = true;
				showVMA = true;
				
				MomoUp = 1;
				MomoDown = -1;
				enableMomo = true;
				showMomo = false;
				
				adxPeriod = 7;
				adxThreshold = 25;
				adxThreshold2 = 50;
				adxExitThreshold = 45;
				enableADX = true;
				showAdx = false;
				
				emaLength = 110;
				enableEMAFilter = false;
				showEMA = false;
				
				AtrPeriod = 14;
				atrThreshold = 1.5;
				enableVolatility = true;
				
				iBarsSinceExit = 0;	
				
		
				LimitOffset = 8;
				TickMove = 4;								
							
				
				Contracts = 1;
				Contracts2 = 1;
				Contracts3 = 1;
				Contracts4 = 1;
				
				InitialStop = 97;
				
				ProfitTarget = 40;
				ProfitTarget2 = 44;
				ProfitTarget3 = 48;
				ProfitTarget4 = 52;
				
				EnableProfitTarget2 = true;
				EnableProfitTarget3 = true;
				EnableProfitTarget4 = true;
				
				//Cancel Orders after N Bars
				CancelAfterBars = 2;
				
				// Breakeven settings
				BESetAuto = true;
				BE_Trigger = 32;
				BE_Offset = 0;
				_beRealized = false;

				// Trailing stop settings
				EnableTickTrail = true;
				TickTrailAmount = 32;
				highestPriceSinceBE = double.MinValue;
				lowestPriceSinceBE = double.MaxValue;
				
				ProgressState = 0;
				
				
				// Trading session times
				Start = DateTime.Parse("06:30", System.Globalization.CultureInfo.InvariantCulture);
				End = DateTime.Parse("16:00", System.Globalization.CultureInfo.InvariantCulture);
				Start2 = DateTime.Parse("11:30", System.Globalization.CultureInfo.InvariantCulture);
				End2 = DateTime.Parse("13:00", System.Globalization.CultureInfo.InvariantCulture);
				Start3 = DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
				End3 = DateTime.Parse("18:00", System.Globalization.CultureInfo.InvariantCulture);
				Start4 = DateTime.Parse("00:00", System.Globalization.CultureInfo.InvariantCulture);
				End4 = DateTime.Parse("03:30", System.Globalization.CultureInfo.InvariantCulture);
				Start5 = DateTime.Parse("06:30", System.Globalization.CultureInfo.InvariantCulture);
				End5 = DateTime.Parse("13:00", System.Globalization.CultureInfo.InvariantCulture);
				Start6 = DateTime.Parse("00:00", System.Globalization.CultureInfo.InvariantCulture);
				End6 = DateTime.Parse("23:59", System.Globalization.CultureInfo.InvariantCulture);
				
				// PnL display settings
				showDailyPnl = true;
				PositionDailyPNL = TextPosition.BottomLeft;	
				colorDailyProfitLoss = Brushes.Cyan;
				
				showPnl = false;
				PositionPnl = TextPosition.TopLeft;
				colorPnl = Brushes.Yellow;
			
				// Daily PnL limits
				dailyLossProfit = true;
				DailyProfitLimit = 100000;
				DailyLossLimit = 1000;				
			
				maxProfit = double.MinValue;
				
				ShowHistorical = true;
				
				// Reinitialize some indicator settings for consistency
				HmaPeriod = 16;
				enableHmaHooks = true;
				showHmaHooks = true;
				
				RegChanPeriod = 40;
				RegChanWidth = 4;
				RegChanWidth2 = 3;
				enableRegChan1 = true;
				enableRegChan2 = true;
				showRegChan1 = true;
				showRegChan2 = true;
				showRegChanHiLo = true;
				
				MomoUp = 1;
				MomoDown = -1;
				enableMomo = true;
				showMomo = true;
				
				wrUp = -20;
				wrDown = -80;
				wrPeriod = 14;
				enableWilly = true;
				showWilly = false;
				
				enableOrderFlowSignals = true;
				showOrderFlowSignals = false;
				enableKeyLevelFilter = true;
				
				// Key Level Filter Defaults
                enableKeyLevelFilter = true; 
                keyLevelProximity = 100;   
				
				showVWAP = true;
				showPriorOHLC = true;
				showPivots = true;
				
				isShortEnabled = true; // Initialize state
		        isLongEnabled = true;
		        buttonsCreated = false;
            }
            else if (State == State.Configure)
            {
                // Data configuration: set error handling and add the volumetric data series.
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelCloseIgnoreRejects;
				AddVolumetric(Instrument.FullName, BarsPeriod.BarsPeriodType, BarsPeriod.Value, VolumetricDeltaType.BidAsk, 1);
				AddDataSeries(Data.BarsPeriodType.Tick, 1);
				
				buyPressure = new Series<double>(this);
				sellPressure = new Series<double>(this);
				
				if (Account != null)
		        {
		            Account.PositionUpdate += Account_PositionUpdate;
		             if (TraceOrders) Print("Subscribed to Account.PositionUpdate");
		        }
		        else
		        {
		            Print("Warning: Account object was null during State.Configure. PositionUpdate subscription failed.");
		        }
		
				// Initialize dictionaries (can also be done in DataLoaded)
				activeEntryOrders = new Dictionary<string, Order>();
				entryOrderFilledQuantity = new Dictionary<string, int>();
				associatedExitSignalNames = new Dictionary<string, List<string>>();
				activeStrategyOrders = new List<Order>();
				longEntryOrders = new List<Order>();
				shortEntryOrders = new List<Order>();


				// --- Emergency WPF Removal --- //
//				if (!emergencyCleanupRun && ChartControl != null && ChartControl.Dispatcher != null)
//		        {
//		            ChartControl.Dispatcher.InvokeAsync(() => {
//		                 EmergencyRemoveStrategyButtons();
//		                 emergencyCleanupRun = true; // Mark as run
//		                 Print("Emergency Cleanup Executed via State.Configure. REMOVE THIS CALL LATER.");
//		             });
//		        }
				// --- Emergency WPF Removal --- //

            }
            else if (State == State.DataLoaded)
            {	
				ClearOutputWindow();
                // Initialize chart indicators using loaded data

				hullMAHooks = BlueZHMAHooks(Close, HmaPeriod, 0, false, false, true, Brushes.Lime, Brushes.Red);
				hullMAHooks.Plots[0].Brush = Brushes.White;
				hullMAHooks.Plots[0].Width = 2;
				if (showHmaHooks) AddChartIndicator(hullMAHooks);
	
				RegressionChannel1 = RegressionChannel(Close, RegChanPeriod, RegChanWidth);
				if (showRegChan1) AddChartIndicator(RegressionChannel1);
	
				RegressionChannel2 = RegressionChannel(Close, RegChanPeriod, RegChanWidth2);
				if (showRegChan2) AddChartIndicator(RegressionChannel2);
	
				RegressionChannelHighLow1 = RegressionChannelHighLow(Close, RegChanPeriod, RegChanWidth);
				if (showRegChanHiLo) AddChartIndicator(RegressionChannelHighLow1);
	
				BuySellPressure1 = BuySellPressure(Close);
				BuySellPressure1.Plots[0].Width = 2;
				BuySellPressure1.Plots[0].Brush = Brushes.Lime;
				BuySellPressure1.Plots[1].Width = 2;
				BuySellPressure1.Plots[1].Brush = Brushes.Red;
				if (showBuySellPressure) AddChartIndicator(BuySellPressure1);
				
				
				
				// Configure Footprint Order Flow indicator
				footprint1 = FootprintOrderFlow(
				    Close,
				    15, 70, 4, 2, 2, 4, 3, 4.0, 30, 100, 300, 2000, 8, 8,
				    false, false, true, 40,
				    System.Windows.Media.Brushes.DarkGreen,
				    System.Windows.Media.Brushes.Green,
				    System.Windows.Media.Brushes.Lime,
				    System.Windows.Media.Brushes.DarkRed,
				    System.Windows.Media.Brushes.Red,
				    System.Windows.Media.Brushes.Crimson,
				    System.Windows.Media.Brushes.Cyan,
				    System.Windows.Media.Brushes.Gray,
				    System.Windows.Media.Brushes.Blue,
				    System.Windows.Media.Brushes.Orange,
				    System.Windows.Media.Brushes.Purple,
				    System.Windows.Media.Brushes.Yellow,
				    System.Windows.Media.Brushes.Magenta,
				    System.Windows.Media.Brushes.Cyan,
				    System.Windows.Media.Brushes.LightGreen,
				    System.Windows.Media.Brushes.Red,
				    System.Windows.Media.Brushes.Teal,
				    System.Windows.Media.Brushes.Pink,
				    false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3
				);
				if (showOrderFlowSignals) AddChartIndicator(footprint1);
				
				VWAP1 = OrderFlowVWAP(Close, VWAPResolution.Tick, Bars.TradingHours, VWAPStandardDeviations.None, 1, 2, 3);
				if (showVWAP) AddChartIndicator(VWAP1);
				
				priorOHLC1 = PriorDayOHLC(Close);
				if (showPriorOHLC) AddChartIndicator(priorOHLC1);
			
				VMA1 = VMA(Close, 9, 9);
				VMA1.Plots[0].Brush = Brushes.SkyBlue;
				VMA1.Plots[0].Width = 3;
				if (showVMA) AddChartIndicator(VMA1);			
				
				ATR1 = ATR(AtrPeriod);						
				
				Momentum1 = Momentum(Close, 14);	
				Momentum1.Plots[0].Brush = Brushes.Yellow;
				Momentum1.Plots[0].Width = 2;
				if (showMomo) AddChartIndicator(Momentum1);
				
				// Reinitialize hullMAHooks to refresh indicator display
				hullMAHooks = BlueZHMAHooks(Close, HmaPeriod, 0, false, false, true, Brushes.Lime, Brushes.Red);
				hullMAHooks.Plots[0].Brush = Brushes.White;
				hullMAHooks.Plots[0].Width = 2;
				if (showHmaHooks) AddChartIndicator(hullMAHooks);
				
				WilliamsR1 = TOWilliamsTraderOracleSignalMOD(Close, 14, @"LongEntry", @"ShortEntry");
				WilliamsR1.Plots[0].Brush = Brushes.Yellow;
				WilliamsR1.Plots[0].Width = 1;
				if (showWilly) AddChartIndicator(WilliamsR1);	
				
				ADX1 = ADX(Close, adxPeriod);
				ADX1.Plots[0].Brush = Brushes.Yellow;
				ADX1.Plots[0].Width = 2;
				if (showAdx) AddChartIndicator(ADX1);
				
				pivots1 = NTSvePivots(Close, false, NTSvePivotRange.Daily, NTSveHLCCalculationMode.CalcFromIntradayData, 0, 0, 0, 250);
				pivots1.Plots[0].Width = 4;
				if (showPivots) AddChartIndicator(pivots1);
				
				if (showEMA) 
				{
					AddChartIndicator(EMA(High, emaLength));
					AddChartIndicator(EMA(Low, emaLength));
				}
				
				if (Account != null)
		        {
		            // Avoid double-subscribing if already done in Configure
		            // A robust way is to unsubscribe first, then subscribe
		            Account.PositionUpdate -= Account_PositionUpdate; // Remove first to be safe
		            Account.PositionUpdate += Account_PositionUpdate;
		             if (TraceOrders) Print("Ensured subscription to Account.PositionUpdate in DataLoaded");
		        }
		         else
		        {
		            Print("Warning: Account object was null during State.DataLoaded. PositionUpdate subscription may have failed.");
		        }
				
				// --- Add Button Creation ---
		        // Create buttons AFTER chart is ready and ONLY IF running on a chart
		        if (ChartControl != null && ChartControl.Dispatcher != null && !buttonsCreated)
		        {
		            // Use Dispatcher.InvokeAsync for UI operations
		            ChartControl.Dispatcher.InvokeAsync(() =>
		            {
		                CreateWPFControls(); // Call the main UI creation method from KCAlgoBase structure
		            });
		        }
		        // --- End Button Creation ---
				
            }
			else if (State == State.Historical)
			{
						
			}
			else if (State == State.Terminated)
			{
				// Cleanup when the strategy terminates
				// Unsubscribe from the PositionUpdate event to prevent memory leaks
		        if (Account != null)
		        {
		            Account.PositionUpdate -= Account_PositionUpdate;
		             if (TraceOrders) Print("Unsubscribed from Account.PositionUpdate");
		        }
		         // Optional: Clear dictionaries here as well, though going flat should handle it.
		         activeEntryOrders.Clear();
		         entryOrderFilledQuantity.Clear();
		         associatedExitSignalNames.Clear();
				
				// Clean up UI elements when strategy terminates
		        if (ChartControl != null && ChartControl.Dispatcher != null && buttonsCreated)
		        {
		             ChartControl.Dispatcher.InvokeAsync(() =>
		             {
		                RemoveWPFControls(); // Call cleanup method
		             });
		        }
		        // Reset flags
		        isLongEnabled = true;
		        isShortEnabled = true;
		        buttonsCreated = false;
			}
		}
		
		#endregion	
		
        #region OnBarUpdate
		protected override void OnBarUpdate()
        {
            // Reset trade availability at each bar
			canTradeOK = true;
			if (BarsInProgress != 0 || CurrentBars[0] < BarsRequiredToTrade )
				return;
					
			
			if (!ShowHistorical && State != State.Realtime) return;	
			
			if (Position.MarketPosition != MarketPosition.Flat)
			{
			    UpdateBreakeven();
				isFlat = false;
				if (_beRealized)
		        {
		            UpdateTrailingStop(); // Call the new method
		        }
			}
			else
			{
				isFlat = true;
				_beRealized = false;
				longEntryOrdersubmissionBar = -1;
    			shortEntryOrdersubmissionBar = -1;				
			}
			
			if (IsFirstTickOfBar)
            {
				if (CurrentBar >= longEntryOrdersubmissionBar + CancelAfterBars)
				{
					foreach (Order order in longEntryOrders.ToArray()) // using ToArray to iterate safely even if we remove items
					{
					    if (order != null && 
					       (order.OrderState == OrderState.Working || order.OrderState == OrderState.Accepted))
					    {
					        CancelOrder(order);
					        longEntryOrders.Remove(order);
					    }
					}
					longEntrySubmitted = false;
				}
				if (CurrentBar >= shortEntryOrdersubmissionBar + CancelAfterBars)
    			{
					foreach (Order order in shortEntryOrders.ToArray()) // using ToArray to iterate safely even if we remove items
					{
					    if (order != null && 
					       (order.OrderState == OrderState.Working || order.OrderState == OrderState.Accepted))
					    {
					        CancelOrder(order);
					        shortEntryOrders.Remove(order);
					    }
					}			       
					shortEntrySubmitted = false;
				}
			}
			

			// Calculate regression channel slopes and channel proximity
			bool channelSlopeUp = (RegressionChannel1.Middle[1] > RegressionChannel1.Middle[2]) && 
                                  (RegressionChannel1.Middle[2] <= RegressionChannel1.Middle[3]) || 
                                  (RegressionChannel1.Middle[0] > RegressionChannel1.Middle[1] && Low[0] > Low[2] && Low[2] <= RegressionChannel1.Lower[2]);
    		bool priceNearLowerChannel = (Low[0] > RegressionChannelHighLow1.Lower[2]);

			bool channelSlopeDown = (RegressionChannel1.Middle[1] < RegressionChannel1.Middle[2]) && 
                                    (RegressionChannel1.Middle[2] >= RegressionChannel1.Middle[3]) || 
                                    (RegressionChannel1.Middle[0] < RegressionChannel1.Middle[1] && High[0] < High[2] && High[2] >= RegressionChannel1.Upper[2]);
    		bool priceNearUpperChannel = (High[0] < RegressionChannelHighLow1.Upper[2]);

            regChanUp = enableRegChan1 ? (channelSlopeUp || priceNearLowerChannel) : true;
            regChanDown = enableRegChan1 ? (channelSlopeDown || priceNearUpperChannel) : true;
			
			// HMA hook condition calculations
			hmaHooksUp = !enableHmaHooks || ((Close[0] > hullMAHooks[0] && hullMAHooks.trend[0] == 1 && hullMAHooks.trend[1] == -1) 
				|| (hullMAHooks[0]  > hullMAHooks[1]));
			hmaHooksDown = !enableHmaHooks || ((Close[0] < hullMAHooks[0] && hullMAHooks.trend[0] == -1 && hullMAHooks.trend[1] == 1)  
				|| (hullMAHooks[0] < hullMAHooks[1]));
			
			// Momentum and Williams signals for entries
			momoUp = enableMomo ? Momentum1[0] > MomoUp && Momentum1[0] > Momentum1[1] : true;
			momoDown = enableMomo ? Momentum1[0] < MomoDown && Momentum1[0] < Momentum1[1] : true;
			WillyUp = enableWilly ? WilliamsR1[1] >= wrUp && Close[0] > Close[1] && High[1] > High[2] : true;
            WillyDown = enableWilly ? WilliamsR1[1] <= wrDown && Close[0] < Close[1] && Low[1] < Low[2] : true;
			
			longSignal = hmaHooksUp || regChanUp || WillyUp || momoUp;
            shortSignal = hmaHooksDown || regChanDown || WillyDown || momoDown;
			
			// ATR and ADX conditions for volatility and trend strength
			atrUp = enableVolatility ? ATR1[0] > atrThreshold : true;
			adxUp = !enableADX || (ADX1[0] > adxThreshold && ADX1[0] < adxThreshold2);
			
			// Update regression channel directional values based on current bar data
			regChanUp = RegressionChannel1.Middle[0] > RegressionChannel1.Middle[1];
			regChanDown = RegressionChannel1.Middle[0] < RegressionChannel1.Middle[1];
			
			// Update buy/sell pressure status
			BuySellPressure1.Update();
			buyPressureUp = !enableBuySellPressure || (BuySellPressure1.BuyPressure[0] > BuySellPressure1.SellPressure[0]);
			sellPressureUp = !enableBuySellPressure || (BuySellPressure1.SellPressure[0] > BuySellPressure1.BuyPressure[0]);
			
			// Update footprint order flow flags
			footprint1.Update();
			if (!enableOrderFlowSignals || footprint1.Values[15][1] == 1 || footprint1.Values[16][1] == 1)
				orderFlowUp = true;
			if (!enableOrderFlowSignals || footprint1.Values[15][1] == -1 || footprint1.Values[16][1] == -1)
				orderFlowDown = true;
			
			// Update pressure series data
			buyPressure[0] = BuySellPressure1.BuyPressure[0];
			sellPressure[0] = BuySellPressure1.SellPressure[0];
			
			// Determine HMA and VMA direction
			hmaUp = (hullMAHooks[0] > hullMAHooks[1]);
			hmaDown = (hullMAHooks[0] < hullMAHooks[1]);
			volMaUp = !enableVMA || VMA1[0] > VMA1[1];
			volMaDown = !enableVMA || VMA1[0] < VMA1[1];
			
			// Refresh momentum values
			momentum = Momentum1[0];
			momoUp = !enableMomo || (Momentum1[0] > MomoUp && Momentum1[0] > Momentum1[1]);
			momoDown = !enableMomo || (Momentum1[0] < MomoDown && Momentum1[0] < Momentum1[1]);
			
			// Apply EMA conditions if filter enabled
			aboveEMAHigh = !enableEMAFilter || Open[1] > EMA(High, emaLength)[1];
			belowEMALow = !enableEMAFilter || Open[1] < EMA(Low, emaLength)[1];
			
			currentAdx = ADX1[0];

            // Market choppiness detection: adjust auto trading based on channel flatness and ADX
			if (EnableChoppinessDetection)
			{
				marketIsChoppy = IsMarketChoppy();				
			}
			
			// Combine indicator outputs to determine overall trade trend
			uptrend = momoUp && buyPressureUp && hmaUp && volMaUp && regChanUp && adxUp && atrUp && aboveEMAHigh && orderFlowUp;
            downtrend = momoDown && sellPressureUp && hmaDown && volMaDown && regChanDown && adxUp && atrUp && belowEMALow && orderFlowDown;
			
			priceUp =  Close[0] > Close[1] && Close[0] > Open[0];		
			priceDown =  Close[0] < Close[1] && Close[0] < Open[0];
			
			// --- Key Level Proximity Check ---
           isKeyLevelInProximityLong = false;  // Reset Long flag
           isKeyLevelInProximityShort = false; // Reset Short flag
           List<double> currentKeyLevels = new List<double>();

           // Determine if we need to check levels at all
            bool checkLongProximity = enableKeyLevelFilter && longSignal && uptrend;
            bool checkShortProximity = enableKeyLevelFilter && shortSignal && downtrend;

            // Get levels ONLY if filter is enabled and either check is needed
            if (enableKeyLevelFilter && (checkLongProximity || checkShortProximity) && CurrentBar > 0)
            {
                currentKeyLevels = GetKeyLevels(); // Get levels just once if needed
            }

            // Perform the actual checks if required (and levels were fetched)
            if (checkLongProximity && currentKeyLevels != null)
            {
                isKeyLevelInProximityLong = IsLongBlockedByProximity(currentKeyLevels);
                if (isKeyLevelInProximityLong && TraceOrders)
                    Print($"{Time[0]} Key Level Proximity Detected for LONG (isKeyLevelInProximityLong = true)");
            }

            if (checkShortProximity && currentKeyLevels != null)
            {
                isKeyLevelInProximityShort = IsShortBlockedByProximity(currentKeyLevels);
                if (isKeyLevelInProximityShort && TraceOrders)
                    Print($"{Time[0]} Key Level Proximity Detected for SHORT (isKeyLevelInProximityShort = true)");
            }
            // --- End Key Level Proximity Check ---
			
			// Change chart background for visual trend signals
			if (enableBackgroundSignal)
			{
				if (uptrend)
				{				    
				    byte alpha = 32;
				    Color baseColor = Colors.Lime;
				    Color semiTransparentColor = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
				    SolidColorBrush semiTransparentBrush = new SolidColorBrush(semiTransparentColor);
				    semiTransparentBrush.Freeze();
				    BackBrush = semiTransparentBrush;
				}
				else if (downtrend)
				{				    
				    byte alpha = 32;
				    Color baseColor = Colors.Crimson;
				    Color semiTransparentColor = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
				    SolidColorBrush semiTransparentBrush = new SolidColorBrush(semiTransparentColor);
				    semiTransparentBrush.Freeze();
				    BackBrush = semiTransparentBrush;
				}
				else
				{
				    BackBrush = null;
				}
			}
			
			if (IsLongEntryConditionMet())
				ProcessLongEntry();
			if (IsShortEntryConditionMet())
				ProcessShortEntry();

			// Update session PnL: On first bar of the session, capture cumulative PnL.
		    if (Bars.IsFirstBarOfSession)
			{
				dailyPnL  = 0;
				cumPnL = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
			}
			
			dailyPnL = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - cumPnL;
				
		}
		#endregion
		
		#region Key Level Helper
        /// <summary>
        /// Gathers all valid key level prices from enabled indicators.
        /// </summary>
        /// <returns>A list of valid key level prices.</returns>
        private List<double> GetKeyLevels()
        {
            List<double> levels = new List<double>();

            // VWAP
            if (showVWAP && VWAP1.IsValidDataPoint(0))
                levels.Add(VWAP1[0]);

            // Prior Day OHLC
            if (priorOHLC1.PriorHigh.IsValidDataPoint(0)) levels.Add(priorOHLC1.PriorHigh[0]);
            if (priorOHLC1.PriorLow.IsValidDataPoint(0)) levels.Add(priorOHLC1.PriorLow[0]);
            if (priorOHLC1.PriorOpen.IsValidDataPoint(0)) levels.Add(priorOHLC1.PriorOpen[0]);
            if (priorOHLC1.PriorClose.IsValidDataPoint(0)) levels.Add(priorOHLC1.PriorClose[0]);
           
            // Pivots
		
           
            if (pivots1.Pp.IsValidDataPoint(0)) levels.Add(pivots1.Pp[0]);
            if (pivots1.S1.IsValidDataPoint(0)) levels.Add(pivots1.S1[0]);
//                if (pivots1.S2.IsValidDataPoint(0)) levels.Add(pivots1.S2[0]);
//                if (pivots1.S3.IsValidDataPoint(0)) levels.Add(pivots1.S3[0]);
            if (pivots1.R1.IsValidDataPoint(0)) levels.Add(pivots1.R1[0]);
//                if (pivots1.R2.IsValidDataPoint(0)) levels.Add(pivots1.R2[0]);
//                if (pivots1.R3.IsValidDataPoint(0)) levels.Add(pivots1.R3[0]);
            // Add Mid-Pivots if needed
//                if (pivots1.S1M.IsValidDataPoint(0)) levels.Add(pivots1.S1M[0]);
//                if (pivots1.S2M.IsValidDataPoint(0)) levels.Add(pivots1.S2M[0]);
//                if (pivots1.S3M.IsValidDataPoint(0)) levels.Add(pivots1.S3M[0]);
//                if (pivots1.R1M.IsValidDataPoint(0)) levels.Add(pivots1.R1M[0]);
//                if (pivots1.R2M.IsValidDataPoint(0)) levels.Add(pivots1.R2M[0]);
//                if (pivots1.R3M.IsValidDataPoint(0)) levels.Add(pivots1.R3M[0]);
            

            // Remove duplicates and sort (optional, but good practice)
            return levels.Distinct().OrderBy(l => l).ToList();
        }
        #endregion
		
		#region Key Level Proximity Checkers

        /// <summary>
        /// Checks if a potential LONG entry is within the specified proximity BELOW a KeyLevel ABOVE the current price.
        /// </summary>
        /// <param name="keyLevels">The list of key levels to check against.</param>
        /// <returns>True if a KeyLevel above is too close, otherwise false.</returns>
        private bool IsLongBlockedByProximity(List<double> keyLevels)
        {
            // Note: We don't need the enableKeyLevelFilter check here as OnBarUpdate already handles it
            //       before calling this function.

            double proximityAmount = keyLevelProximity * TickSize; // Use LONG proximity
            double currentPrice = Close[0]; // Using Close price as reference

            foreach (double level in keyLevels)
            {
                // Check levels ABOVE the current price
                if (level > currentPrice)
                {
                    // Is the distance to the level less than or equal to the proximity threshold?
                    if ((level - currentPrice) <= proximityAmount)
                    {
                        // Log is already handled in OnBarUpdate where the flag is set
                        // if (TraceOrders) Print($"{Time[0]} LONG BLOCKED by KeyLevel {level}. Proximity: {level - currentPrice:F2} (<= {proximityAmount:F2}). Current Price: {currentPrice:F2}.");
                        return true; // Blocked by a level above
                    }
                }
            }
            return false; // No blocking level found above
        }

        /// <summary>
        /// Checks if a potential SHORT entry is within the specified proximity ABOVE a KeyLevel BELOW the current price.
        /// </summary>
        /// <param name="keyLevels">The list of key levels to check against.</param>
        /// <returns>True if a KeyLevel below is too close, otherwise false.</returns>
        private bool IsShortBlockedByProximity(List<double> keyLevels)
        {
             // Note: We don't need the enableKeyLevelFilter check here as OnBarUpdate already handles it
             //       before calling this function.

            double proximityAmount = keyLevelProximity * TickSize; // Use SHORT proximity
            double currentPrice = Close[0]; // Using Close price as reference

            foreach (double level in keyLevels)
            {
                // Check levels BELOW the current price
                if (level < currentPrice)
                {
                    // Is the distance to the level less than or equal to the proximity threshold?
                    if ((currentPrice - level) <= proximityAmount)
                    {
                        // Log is already handled in OnBarUpdate where the flag is set
                        // if (TraceOrders) Print($"{Time[0]} SHORT BLOCKED by KeyLevel {level}. Proximity: {currentPrice - level:F2} (<= {proximityAmount:F2}). Current Price: {currentPrice:F2}.");
                        return true; // Blocked by a level below
                    }
                }
            }
            return false; // No blocking level found below
        }

        #endregion // End Key Level Proximity Checkers

		#region Breakeven Management
        // Methods to adjust orders to breakeven (if implemented) would be added here.
		private void UpdateBreakeven()
		{
		    if (TraceOrders)
		        Print($"{Time[0]} BE Check START: BESetAuto={BESetAuto}, _beRealized={_beRealized}, Pos={Position?.MarketPosition}");
		
		    // Guard: do nothing if BE is disabled, position is flat, or already processed
		    if (!BESetAuto || Position == null || Position.MarketPosition == MarketPosition.Flat || _beRealized)
		        return;
		
		    double currentClose = Close[0];
		    double avgPrice = Position.AveragePrice;
		    double profitTicks = 0;
		    bool triggerMet = false;
		    double newStopPrice = 0;
		
		    if (Position.MarketPosition == MarketPosition.Long)
		    {
		        profitTicks = (currentClose - avgPrice) / TickSize;
		        if (TraceOrders)
		            Print($"{Time[0]} BE Check LONG: Close={currentClose:F2}, AvgPx={avgPrice:F2}, ProfitTicks={profitTicks:F2}, Trigger={BE_Trigger}");
		        if (profitTicks >= BE_Trigger)
		        {
		            triggerMet = true;
		            newStopPrice = Instrument.MasterInstrument.RoundToTickSize(avgPrice + (BE_Offset * TickSize));
		        }
		    }
		    else // Short
		    {
		        profitTicks = (avgPrice - currentClose) / TickSize;
		        if (TraceOrders)
		            Print($"{Time[0]} BE Check SHORT: Close={currentClose:F2}, AvgPx={avgPrice:F2}, ProfitTicks={profitTicks:F2}, Trigger={BE_Trigger}");
		        if (profitTicks >= BE_Trigger)
		        {
		            triggerMet = true;
		            newStopPrice = Instrument.MasterInstrument.RoundToTickSize(avgPrice - (BE_Offset * TickSize));
		        }
		    }
		
		    if (triggerMet)
		    {
		        if (TraceOrders)
		            Print($"{Time[0]} BE Trigger MET! Target Stop Price: {newStopPrice:F2}");
		
		        bool changedAnyOrder = false;
		        string entryPrefix = (Position.MarketPosition == MarketPosition.Long) ? "LE" : "SE";
		
		        // Instead of solely relying on the activeEntryOrders (which may have been cleared), try to gather exit orders using your associatedExitSignalNames.
		        List<string> activeEntryKeys = associatedExitSignalNames.Keys.Where(k => k.StartsWith(entryPrefix)).ToList();
		
		        // ***** Fallback Check *****  
		        // If no active entry keys are found and the position is still open, fall back to closing all positions.
		        if (activeEntryKeys.Count == 0 && Position.MarketPosition != MarketPosition.Flat)
		        {
		            Print($"{Time[0]} BE Fallback: No active entry keys found. Initiating CloseAllPositionsNow().");
		            CloseAllPositionsNow();
		            return;
		        }
		
		        // Loop through all expected exit orders for each active entry key
		        foreach (string entryName in activeEntryKeys)
		        {
		            string stopSignalName = entryName + "Stop";
		
		            if (TraceOrders)
		                Print($"{Time[0]} BE: Searching for Working/Accepted stop order with SignalName='{stopSignalName}'");
		
		            // Use the Account.Orders collection to find the exit order (rather than relying on entry tracking)
		            Order stopOrderToModify = Account.Orders.FirstOrDefault(o =>
		                o.Name == stopSignalName &&
		                (o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted) &&
		                o.Instrument == Instrument &&
		                o.Account == Account);
		
		            if (stopOrderToModify != null)
		            {
		                if (TraceOrders)
		                    Print($"{Time[0]} BE: Found stop order {stopOrderToModify.Name} (ID: {stopOrderToModify.OrderId}), State={stopOrderToModify.OrderState}, Current StopPx={stopOrderToModify.StopPrice:F2}");
		                
		                if (stopOrderToModify.StopPrice != newStopPrice)
		                {
		                    if (TraceOrders)
		                        Print($"{Time[0]} BE: Attempting ChangeOrder for {stopOrderToModify.Name} to new stop price {newStopPrice:F2}");
		                    try
		                    {
		                        ChangeOrder(stopOrderToModify, stopOrderToModify.Quantity, 0, newStopPrice);
		                        Print($"{Time[0]}: Breakeven Changed stop order {stopOrderToModify.Name} to {newStopPrice:F2}");
		                        changedAnyOrder = true;
		                    }
		                    catch (Exception ex)
		                    {
		                        if (TraceOrders)
		                            Print($"{Time[0]} BE ERROR attempting ChangeOrder for {stopOrderToModify.Name}: {ex.Message}");
		                    }
		                }
		                else if (TraceOrders)
		                {
		                    Print($"{Time[0]} BE: Skipping ChangeOrder for {stopOrderToModify.Name}, price already at {newStopPrice:F2}.");
		                }
		            }
		            else
		            {
		                if (TraceOrders)
		                {
		                    Print($"{Time[0]} BE Warning: Could not find Working/Accepted stop order with name {stopSignalName}.");
		                    // Optionally, you can check for orders with this name in any state for additional debug logging
		                    Order debugOrder = Account.Orders.FirstOrDefault(o => o.Name == stopSignalName && o.Instrument == Instrument && o.Account == Account);
		                    if (debugOrder != null)
		                        Print($"{Time[0]} BE Debug: Found order {stopSignalName} but its state is {debugOrder.OrderState}.");
		                    else
		                        Print($"{Time[0]} BE Debug: Order {stopSignalName} not found in Account.Orders.");
		                }
		            }
		        }
		
		        // Fallback if no order was successfully changed, and position is still open
		        if (!changedAnyOrder && Position.MarketPosition != MarketPosition.Flat)
		        {
		            Print($"{Time[0]} BE Fallback: No exit orders were modified successfully. Initiating CloseAllPositionsNow().");
		            CloseAllPositionsNow();
		        }
		        else if (changedAnyOrder)
		        {
		            // Initialize trailing parameters if this is the first successful modification.
		            if (!_beRealized)
		            {
		                if (Position.MarketPosition == MarketPosition.Long)
		                {
		                    highestPriceSinceBE = High[0];
		                    if (TraceOrders)
		                        Print($"{Time[0]} BE: Initialized highestPriceSinceBE to {highestPriceSinceBE:F2}");
		                }
		                else
		                {
		                    lowestPriceSinceBE = Low[0];
		                    if (TraceOrders)
		                        Print($"{Time[0]} BE: Initialized lowestPriceSinceBE to {lowestPriceSinceBE:F2}");
		                }
		            }
		            _beRealized = true;
		            if (TraceOrders)
		                Print($"{Time[0]} BE: _beRealized set to true after modifying exit orders.");
		        }
		    }
		}

		#endregion
		
		#region TrailingStop Management
		private void UpdateTrailingStop()
		{
		    // --- Guard Clauses ---
		    // Exit if Tick Trail not enabled, OR breakeven not yet realized, OR flat/null Position
		    if (!EnableTickTrail || !_beRealized || Position == null || Position.MarketPosition == MarketPosition.Flat)
		        return;
		
		    double potentialNewTrailStop = 0;
		    bool trailStopNeedsUpdate = false;
		
		    // --- Logic for LONG position ---
		    if (Position.MarketPosition == MarketPosition.Long)
		    {
		        // Update the highest price seen since breakeven
		        highestPriceSinceBE = Math.Max(highestPriceSinceBE, High[0]);
		
		        // Calculate the potential new stop loss based on the highest high and trail amount
		        potentialNewTrailStop = Instrument.MasterInstrument.RoundToTickSize(highestPriceSinceBE - (TickTrailAmount * TickSize));
		
		        // Log calculation
		        if (TraceOrders) Print($"{Time[0]} TRAIL Check LONG: HighestSinceBE={highestPriceSinceBE:F2}, PotentialStop={potentialNewTrailStop:F2}, TrailTicks={TickTrailAmount}");
		
		        // Retrieve the CURRENT lowest stop price among all active stop orders
		        // (In case stops somehow got out of sync, we trail from the highest one)
		        double currentHighestStop = double.MinValue;
		        bool foundWorkingStop = false;
		
		        List<string> activeEntryKeys = associatedExitSignalNames.Keys.Where(k => k.StartsWith("LE")).ToList();
		        foreach (string entryName in activeEntryKeys)
		        {
		             string stopSignalName = entryName + "Stop";
		             Order stopOrder = Account.Orders.FirstOrDefault(o => o.Name == stopSignalName && (o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted) && o.Instrument == Instrument && o.Account == Account);
		             if (stopOrder != null)
		             {
		                 currentHighestStop = Math.Max(currentHighestStop, stopOrder.StopPrice);
		                 foundWorkingStop = true; // Found at least one potentially modifiable stop
		             }
		        }
		
		         if (!foundWorkingStop)
		         {
		              if (TraceOrders) Print($"{Time[0]} TRAIL Check LONG: No Working/Accepted stop orders found to trail.");
		              return; // Cannot trail if no stops are active
		         }
		
		        // Check if the potential new stop is higher than the current highest stop price
		        if (potentialNewTrailStop > currentHighestStop)
		        {
		            trailStopNeedsUpdate = true;
		            if (TraceOrders) Print($"{Time[0]} TRAIL LONG: Update needed. Potential Stop ({potentialNewTrailStop:F2}) > Current Highest Stop ({currentHighestStop:F2})");
		        }
		        else if(TraceOrders)
		        {
		             Print($"{Time[0]} TRAIL LONG: No update needed. Potential Stop ({potentialNewTrailStop:F2}) <= Current Highest Stop ({currentHighestStop:F2})");
		        }
		    }
		    // --- Logic for SHORT position ---
		    else // Position is Short
		    {
		        // Update the lowest price seen since breakeven
		        lowestPriceSinceBE = Math.Min(lowestPriceSinceBE, Low[0]);
		
		        // Calculate the potential new stop loss based on the lowest low and trail amount
		        potentialNewTrailStop = Instrument.MasterInstrument.RoundToTickSize(lowestPriceSinceBE + (TickTrailAmount * TickSize));
		
		        // Log calculation
		         if (TraceOrders) Print($"{Time[0]} TRAIL Check SHORT: LowestSinceBE={lowestPriceSinceBE:F2}, PotentialStop={potentialNewTrailStop:F2}, TrailTicks={TickTrailAmount}");
		
		         // Retrieve the CURRENT highest stop price among all active stop orders
		         double currentLowestStop = double.MaxValue;
		         bool foundWorkingStop = false;
		
		         List<string> activeEntryKeys = associatedExitSignalNames.Keys.Where(k => k.StartsWith("SE")).ToList();
		         foreach (string entryName in activeEntryKeys)
		         {
		              string stopSignalName = entryName + "Stop";
		              Order stopOrder = Account.Orders.FirstOrDefault(o => o.Name == stopSignalName && (o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted) && o.Instrument == Instrument && o.Account == Account);
		              if (stopOrder != null)
		              {
		                  currentLowestStop = Math.Min(currentLowestStop, stopOrder.StopPrice);
		                  foundWorkingStop = true;
		              }
		         }
		
		          if (!foundWorkingStop)
		          {
		               if (TraceOrders) Print($"{Time[0]} TRAIL Check SHORT: No Working/Accepted stop orders found to trail.");
		               return; // Cannot trail if no stops are active
		          }
		
		        // Check if the potential new stop is lower than the current lowest stop price
		        if (potentialNewTrailStop < currentLowestStop)
		        {
		            trailStopNeedsUpdate = true;
		             if (TraceOrders) Print($"{Time[0]} TRAIL SHORT: Update needed. Potential Stop ({potentialNewTrailStop:F2}) < Current Lowest Stop ({currentLowestStop:F2})");
		        }
		         else if(TraceOrders)
		         {
		             Print($"{Time[0]} TRAIL SHORT: No update needed. Potential Stop ({potentialNewTrailStop:F2}) >= Current Lowest Stop ({currentLowestStop:F2})");
		         }
		    }
		
		    // --- If Update Needed, Modify All Active Stop Orders ---
		    if (trailStopNeedsUpdate)
		    {
		         if (TraceOrders) Print($"{Time[0]} TRAIL: Applying update to stop price: {potentialNewTrailStop:F2}");
		
		         string entryPrefix = (Position.MarketPosition == MarketPosition.Long) ? "LE" : "SE";
		         List<string> activeEntryKeys = associatedExitSignalNames.Keys.Where(k => k.StartsWith(entryPrefix)).ToList();
		         int successCount = 0;
		
		         foreach (string entryName in activeEntryKeys)
		         {
		             string stopSignalName = entryName + "Stop";
		             // Find the specific order again (query might be slightly redundant but ensures we get the latest state)
		             Order stopOrderToModify = Account.Orders.FirstOrDefault(o => o.Name == stopSignalName && (o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted) && o.Instrument == Instrument && o.Account == Account);
		
		             if (stopOrderToModify != null)
		             {
		                 // Only modify if the price is actually different (safety check)
		                 if (stopOrderToModify.StopPrice != potentialNewTrailStop)
		                 {
		                     if (TraceOrders) Print($"{Time[0]} TRAIL: Attempting ChangeOrder for {stopOrderToModify.Name} (State={stopOrderToModify.OrderState}) to {potentialNewTrailStop:F2}");
		                     try
		                     {
		                         ChangeOrder(stopOrderToModify, stopOrderToModify.Quantity, 0, potentialNewTrailStop);
		                         Print($"{Time[0]}: Trailing Stop Changed order {stopOrderToModify.Name} to new stop price {potentialNewTrailStop:F2}.");
		                         successCount++;
		                     }
		                     catch (Exception ex)
		                     {
		                          if (TraceOrders) Print($"{Time[0]} TRAIL ERROR attempting ChangeOrder for {stopOrderToModify.Name}: {ex.Message}");
		                     }
		                 }
		                 else if (TraceOrders)
		                 {
		                     Print($"{Time[0]} TRAIL: Skipping ChangeOrder for {stopOrderToModify.Name}, price already at {potentialNewTrailStop:F2}.");
		                 }
		             }
		             // No need for an else here, we already checked if any working stops existed earlier
		         }
		         if (TraceOrders && successCount > 0) Print($"{Time[0]} TRAIL: Successfully changed {successCount} stop order(s).");
		    }
		}
		#endregion // End TrailingStop Management Region

		#region Long Entry			
        // Code for determining and executing long entries can be implemented here.
		private void ProcessLongEntry()
		{
			if (!longEntrySubmitted)
    		{
				longEntryOrders.Clear();
			    
				// Calculate the limit price for a long order: typically, you want to buy below the current market price.
			   	double longLimitPrice = Close[0] - (LimitOffset * TickSize);
			    
			    // Primary long entry order
			    Order order1 = EnterLongLimit(0, true, Contracts, longLimitPrice, LE1);
				if (order1 != null) longEntryOrders.Add(order1);
			    // Secondary Profit Target (if enabled)
			    if (EnableProfitTarget2)
			    {
			        Order order2 = EnterLongLimit(0, true, Contracts2, longLimitPrice, LE2);
					if (order2 != null) longEntryOrders.Add(order2); 
			    }
			    
			    // Third Profit Target (if enabled)
			    if (EnableProfitTarget3)
			    {
			        Order order3 = EnterLongLimit(0, true, Contracts3, longLimitPrice, LE3);
					if (order3 != null) longEntryOrders.Add(order3);
			    }
			    
			    // Fourth Profit Target (if enabled)
			    if (EnableProfitTarget4)
			    {
			        Order order4 = EnterLongLimit(0, true, Contracts4, longLimitPrice, LE4);
					if (order4 != null) longEntryOrders.Add(order4);
			    }
				// Only set submitted flag if we actually got orders (or intended to submit)
		        if (longEntryOrders.Count > 0)
		        {
		             longEntrySubmitted = true;
		             longEntryOrdersubmissionBar = CurrentBar;
		             if (TraceOrders) Print($"{Time[0]}: Long entry orders submitted (Count: {longEntryOrders.Count}). SubmissionBar: {longEntryOrdersubmissionBar}");
		        }
		        else {
		             if (TraceOrders) Print($"{Time[0]}: ProcessLongEntry called but no valid orders were generated/added.");
		             longEntrySubmitted = false;
		        }
			}
		}
		
		#endregion
		
		#region Short Entry
        // Code for determining and executing short entries can be implemented here.
		private void ProcessShortEntry()
		{
			if (!shortEntrySubmitted)
    		{
				shortEntryOrders.Clear();
				
			    // Calculate the limit price for a short order: typically, you want to sell above the current market price.
			    double shortLimitPrice = Close[0] + (LimitOffset * TickSize);
			    
			    // Primary short entry order
			    Order order1 = EnterShortLimit(0, true, Contracts, shortLimitPrice, SE1);
			    if (order1 != null) shortEntryOrders.Add(order1);
				
			    // Secondary Profit Target (if enabled) using a separate quantity and order label:
			    if (EnableProfitTarget2)
			    {
			        Order order2 = EnterShortLimit(0, true, Contracts2, shortLimitPrice, SE2);
					if (order2 != null) shortEntryOrders.Add(order2);
			    }
			    
			    // Third Profit Target (if enabled)
			    if (EnableProfitTarget3)
			    {
			        Order order3 = EnterShortLimit(0, true, Contracts3, shortLimitPrice, SE3);
					if (order3 != null) shortEntryOrders.Add(order3);
			    }
			    
			    // Fourth Profit Target (if enabled)
			    if (EnableProfitTarget4)
			    {
			        Order order4 = EnterShortLimit(0, true, Contracts4, shortLimitPrice, SE4);
					if (order4 != null) shortEntryOrders.Add(order4);
			    }
				
				if (shortEntryOrders.Count > 0)
		        {
		            shortEntrySubmitted = true;
		            shortEntryOrdersubmissionBar = CurrentBar;
		             if (TraceOrders) Print($"{Time[0]}: Short entry orders submitted (Count: {shortEntryOrders.Count}). SubmissionBar: {shortEntryOrdersubmissionBar}");
		        }
		         else {
		             if (TraceOrders) Print($"{Time[0]}: ProcessShortEntry called but no valid orders were generated/added.");
		             shortEntrySubmitted = false;
		        }
			}
		}
		
		#endregion
			
		#region Entry Condition Checkers
        private bool IsLongEntryConditionMet()
        {
			// Check all conditions for a long entry signal
            return longSignal
			   && isAutoEnabled
			   && isLongEnabled
			   && ((EnableChoppinessDetection && !marketIsChoppy) || !EnableChoppinessDetection)
               && isLongEnabled
               && checkTimers()
               && (dailyLossProfit ? dailyPnL > -DailyLossLimit && dailyPnL < DailyProfitLimit : true)
               && isFlat
               && uptrend
			   && (!enableKeyLevelFilter || !isKeyLevelInProximityLong)
               && !trailingDrawdownReached
               && (iBarsSinceExit > 0 ? BarsSinceExitExecution(0, "", 0) > iBarsSinceExit : BarsSinceExitExecution(0, "", 0) > 1 || BarsSinceExitExecution(0, "", 0) == -1)
               && canTradeOK;
              
        }

        private bool IsShortEntryConditionMet()
        {
            return shortSignal
				&& isAutoEnabled
				&& isShortEnabled
				&& ((EnableChoppinessDetection && !marketIsChoppy) || !EnableChoppinessDetection)
				&& isShortEnabled
				&& checkTimers()
				&& (dailyLossProfit ? dailyPnL > -DailyLossLimit && dailyPnL < DailyProfitLimit : true)
				&& isFlat
				&& downtrend
				&& (!enableKeyLevelFilter || !isKeyLevelInProximityShort)
				&& !trailingDrawdownReached
				&& (iBarsSinceExit > 0 ? BarsSinceExitExecution(0, "", 0) > iBarsSinceExit : BarsSinceExitExecution(0, "", 0) > 1 || BarsSinceExitExecution(0, "", 0) == -1)
				&& canTradeOK;
        }
		
		protected bool checkTimers()
		{
			// Check if the current time is within any enabled trading session window.
			if ((Times[0][0].TimeOfDay >= Start.TimeOfDay && Times[0][0].TimeOfDay < End.TimeOfDay) 
				|| (Time2 && Times[0][0].TimeOfDay >= Start2.TimeOfDay && Times[0][0].TimeOfDay <= End2.TimeOfDay)
				|| (Time3 && Times[0][0].TimeOfDay >= Start3.TimeOfDay && Times[0][0].TimeOfDay <= End3.TimeOfDay)
				|| (Time4 && Times[0][0].TimeOfDay >= Start4.TimeOfDay && Times[0][0].TimeOfDay <= End4.TimeOfDay)
				|| (Time5 && Times[0][0].TimeOfDay >= Start5.TimeOfDay && Times[0][0].TimeOfDay <= End5.TimeOfDay)
				|| (Time6 && Times[0][0].TimeOfDay >= Start6.TimeOfDay && Times[0][0].TimeOfDay <= End6.TimeOfDay))
			{
				return true;
			}
			else
			{
				return false;
			}			
		}
        #endregion
		
		#region Choppiness Detection
		private bool IsMarketChoppy()
		{
			bool choppy = false;
			if (CurrentBar >= Math.Max(RegChanPeriod, Math.Max(adxPeriod, SlopeLookBack)) - 1)
			{
			    double middleNow = RegressionChannel1.Middle[0];
			    double middleBefore = RegressionChannel1.Middle[SlopeLookBack];
			    double regChanSlope = (middleNow - middleBefore) / SlopeLookBack;
			    double flatSlopeThreshold = FlatSlopeFactor * TickSize;
			    bool isRegChanFlat = Math.Abs(regChanSlope) < flatSlopeThreshold;
			    bool adxIsLow = currentAdx < ChopAdxThreshold;
			    choppy = isRegChanFlat && adxIsLow;				
			}
			return choppy;
		}
		#endregion
				
		#region OnOrderUpdate + OnExecutionUpdate + Account_PositionUpdate

		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
		{
		    // === Start: Add activeStrategyOrders Tracking Logic ===
		    // (Keep this entire section exactly as it is)
		    if (order.Instrument == this.Instrument && order.Account == this.Account)
		    {
		        bool isActiveState = orderState == OrderState.Accepted ||
		                             orderState == OrderState.Working ||
		                             orderState == OrderState.Submitted ||
		                             orderState == OrderState.ChangePending ||
		                             orderState == OrderState.CancelPending ||
		                             orderState == OrderState.TriggerPending;
		
		        bool isTerminalState = Order.IsTerminalState(orderState);
		
		        if (isActiveState && !activeStrategyOrders.Contains(order))
		        {
		            activeStrategyOrders.Add(order);
		            if (TraceOrders) Print($"{time} Order Added to activeStrategyOrders: {order.Name} (ID: {order.OrderId}, State: {orderState}). Count: {activeStrategyOrders.Count}");
		        }
		        else if (isTerminalState && activeStrategyOrders.Contains(order))
		        {
		            activeStrategyOrders.Remove(order);
		            if (TraceOrders) Print($"{time} Order Removed from activeStrategyOrders: {order.Name} (ID: {order.OrderId}, State: {orderState}). Count: {activeStrategyOrders.Count}");
		        }
		    }
		    // === End: Add activeStrategyOrders Tracking Logic ===
		
		
		    // This part manages the dictionaries needed for submitting exits based on ENTRY fills.
		    bool isTrueEntryOrder = Regex.IsMatch(order.Name, @"^(LE|SE)\d+$");
		
		    if (isTrueEntryOrder)
		    {
		        // --- Handling for TRUE Entry Orders ---
		        // (Keep this entire section exactly as it is, including the rejection handling for entry orders)
		        // A: Add new/working TRUE entry orders...
		        if (orderState == OrderState.Accepted || orderState == OrderState.Working || orderState == OrderState.Submitted)
		        {
		            // ... existing code ...
		             if (!activeEntryOrders.ContainsKey(order.Name))
		              {
		                  activeEntryOrders.Add(order.Name, order);
		                  entryOrderFilledQuantity[order.Name] = order.Filled;
		                  if (!associatedExitSignalNames.ContainsKey(order.Name))
		                  {
		                      associatedExitSignalNames.Add(order.Name, new List<string>());
		                      if (TraceOrders) Print($"{time} {order.Name}: Initialized dict entry in associatedExitSignalNames.");
		                  }
		                  if (TraceOrders) Print($"{time} {order.Name}: Added/Updated ENTRY Order in activeEntryOrders dict. State: {orderState}");
		              } else {
		                   entryOrderFilledQuantity[order.Name] = order.Filled;
		              }
		        }
		        // B: Clean up ENTRY tracking dictionaries if cancelled BEFORE any fills
		        else if (orderState == OrderState.Cancelled && order.Filled == 0)
		        {
		            // ... existing code ...
		             if (activeEntryOrders.Remove(order.Name))
		              {
		                  entryOrderFilledQuantity.Remove(order.Name);
		                  associatedExitSignalNames.Remove(order.Name);
		                  if (TraceOrders) Print($"{time} {order.Name}: Removed ENTRY Order from tracking dicts (Cancelled, 0 fills).");
		              }
		        }
		        // C: Clean up ENTRY tracking dictionaries if rejected or errored
		        else if (orderState == OrderState.Rejected || error != ErrorCode.NoError)
		        {
		             // ... existing code ...
		             if (TraceOrders) Print($"{time} {order.Name} (Entry): Error/Rejected in dict tracking. State: {orderState}, Error: {error}, Native: {nativeError}");
		              if (activeEntryOrders.Remove(order.Name))
		              {
		                  entryOrderFilledQuantity.Remove(order.Name);
		                  associatedExitSignalNames.Remove(order.Name);
		                  if (TraceOrders) Print($"{time} {order.Name}: Removed ENTRY Order from tracking dicts (Rejected/Error).");
		              }
		        }
		    }
		    else // --- Handling for NON-Entry Orders (Exits: Stops, Targets) ---
		    {
		        // **** Errors creating or modifying orders ****
		        // Check specifically for rejections or errors on these non-entry orders
		        bool isErrorState = orderState == OrderState.Rejected || error != ErrorCode.NoError;
		
		        if (isErrorState)
		        {
		            // Log the raw rejection first
		            if (TraceOrders) Print($"{time} {order.Name} (Non-Entry): Received Error/Rejection. State: {orderState}, Error: {error}, Native: {nativeError}");
		
		            // Now, check if we should close the position because of this rejection
		            if (Position.MarketPosition != MarketPosition.Flat)
		            {
		                // Check if this rejected order was one of the expected exit orders for our current trade
		                bool isAssociatedExit = false;
		                foreach (var exitList in associatedExitSignalNames.Values)
		                {
		                    if (exitList.Contains(order.Name))
		                    {
		                        isAssociatedExit = true;
		                        break;
		                    }
		                }
		
		                if (isAssociatedExit)
		                {
		                    // --- Trigger position close ---
		                    Print($"{time} CRITICAL EXIT REJECTION: Associated exit order '{order.Name}' (State: {orderState}, Error: {error}) rejected while position is {Position.MarketPosition}. Initiating position close.");
		                    CloseAllPositionsNow();
		                    // Note: CloseAllPositionsNow() will handle cancelling any *other* remaining working orders.
		                    // The rejected order is already terminal. We don't need further action on 'order' itself.
		                }
		                else if (TraceOrders)
		                {
		                    // This case might happen if an order from a previous state or manual intervention gets rejected
		                    Print($"{time} Unassociated Rejection: Order '{order.Name}' rejected while position active, but not found in current associatedExitSignalNames. No automatic close triggered.");
		                }
		            }
		            else if (TraceOrders) // Log if rejected while flat (less critical)
		            {
		                Print($"{time} Rejection While Flat: Order '{order.Name}' rejected. State: {orderState}, Error: {error}.");
		            }
		        }
		        // else
		        // {
		        //     // Optional: Handle other non-entry order updates (e.g., Filled targets/stops) if needed for logic,
		        //     // but often OnExecutionUpdate is sufficient for fills.
		        //     // if(TraceOrders) Print($"{time} {order.Name} (Non-Entry): State updated to {orderState}");
		        // }
		        // **** End order error management ****
		    }
			if (error != ErrorCode.NoError)
		    {
		        // Log detailed error information.
		        Print($"{time} Order {order.Name} (ID: {order.OrderId}): Error detected. " +
		              $"State = {orderState}, Error = {error}, NativeError = {nativeError}");
		
		        // Check specific native error messages.
		        if (nativeError != null && nativeError.Contains("Stop price can't be changed"))
		        {
		            Print($"{time} Order {order.Name}: Invalid stop modification detected. " +
		                  "Initiating emergency close.");
		            CloseAllPositionsNow();
		        }
		        else if (nativeError != null && error.Equals("UnableToChangeOrder"))
		        {
		            Print($"{time} Order {order.Name}: Unable to change order. " +
		                  "Initiating emergency close.");
		            CloseAllPositionsNow();
		        }
		        else
		        {
		            // For any other error, you might log additional info or decide on a different action.
		            Print($"{time} Order {order.Name}: Unhandled error condition.");
		            // Optionally, you could call EmergencyClose() here as well, if you want to flatten any position on any error.
		        }
		    }			
		}
	
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
		    // Check if the execution belongs to an order we are actively tracking
		    if (activeEntryOrders.TryGetValue(execution.Order.Name, out Order entryOrder))
		    {
		        // Update the filled quantity for this specific order
		        if (!entryOrderFilledQuantity.ContainsKey(entryOrder.Name))
		            entryOrderFilledQuantity.Add(entryOrder.Name, 0);
		        entryOrderFilledQuantity[entryOrder.Name] += execution.Quantity;
		
		        // Log the execution details
		        if (TraceOrders) Print($"{Time[0]} {entryOrder.Name}: Received execution. Qty: {execution.Quantity}, Price: {execution.Price}. Total Filled for this Order: {entryOrderFilledQuantity[entryOrder.Name]}/{entryOrder.Quantity}. Order State: {entryOrder.OrderState}");
		
		        // --- Check if this specific entry order is now fully filled ---
		        int totalFilledForThisOrder = entryOrderFilledQuantity[entryOrder.Name];
		        bool isEntryOrderComplete = totalFilledForThisOrder >= entryOrder.Quantity;
		
		        // Handle potential cancellation after partial fill - also marks completion
		        if (!isEntryOrderComplete && entryOrder.OrderState == OrderState.Cancelled && entryOrder.Filled > 0 && totalFilledForThisOrder >= entryOrder.Filled)
		        {
		             if (TraceOrders) Print($"{Time[0]} {entryOrder.Name}: Order cancelled after partial fills ({totalFilledForThisOrder}/{entryOrder.Quantity}). Marking as complete for exit submission.");
		             isEntryOrderComplete = true; // Treat as complete for submitting exits for the filled amount
		             // Adjust the quantity to submit exits for if cancelled partially
		             // We should submit for entryOrder.Filled in this specific cancellation case
		        }
		
		        // --- If the entry order is fully filled, submit its exits ONCE ---
		        if (isEntryOrderComplete)
		        {
		            // Determine offsets and unique signal names only when submitting
		            double targetOffsetTicks = 0;
		            double stopOffsetTicks = InitialStop;
		            bool isLong = entryOrder.Name.StartsWith("LE");
		            string entryName = entryOrder.Name;
		            string stopSignalName = entryOrder.Name + "Stop";
		            string targetSignalName = entryOrder.Name + "Target";
		
		            // ** Important: Determine the quantity for the exit order **
		            // If cancelled partially, use the actual filled amount. Otherwise, use the full order quantity.
		            int exitQuantity = (entryOrder.OrderState == OrderState.Cancelled && entryOrder.Filled > 0) ? entryOrder.Filled : entryOrder.Quantity;
		
		            if (TraceOrders) Print($"{Time[0]} {entryOrder.Name}: Order COMPLETE. Preparing to submit exits for {exitQuantity} contracts.");
		
		
		            if (isLong)
		            {
		                // Determine Target Offset for Long
		                if (entryName.Equals("LE1", StringComparison.OrdinalIgnoreCase)) targetOffsetTicks = ProfitTarget;
		                else if (entryName.Equals("LE2", StringComparison.OrdinalIgnoreCase)) targetOffsetTicks = ProfitTarget2;
		                else if (entryName.Equals("LE3", StringComparison.OrdinalIgnoreCase)) targetOffsetTicks = ProfitTarget3;
		                else if (entryName.Equals("LE4", StringComparison.OrdinalIgnoreCase)) targetOffsetTicks = ProfitTarget4;
		                else targetOffsetTicks = ProfitTarget; // Default
		
		                // Calculate Prices based on final AverageFillPrice
		                double targetPrice = entryOrder.AverageFillPrice + targetOffsetTicks * TickSize;
		                double stopPrice = entryOrder.AverageFillPrice - stopOffsetTicks * TickSize;
		
		                // Submit Exits for the TOTAL filled quantity of this order
		                ExitLongStopMarket(0, true, exitQuantity, stopPrice, stopSignalName, entryOrder.Name);
		                ExitLongLimit(0, true, exitQuantity, targetPrice, targetSignalName, entryOrder.Name);
		
		                if (TraceOrders) Print($"{Time[0]} {entryOrder.Name}: Submitted LONG exits (Stop: {stopPrice}, Target: {targetPrice}) for COMPLETED order qty: {exitQuantity}. Signal Names: {stopSignalName}, {targetSignalName}. From Entry: {entryOrder.Name}");
		            }
		            else // Is Short
		            {
		                // Determine Target Offset for Short
		                 if (entryName.Equals("SE1", StringComparison.OrdinalIgnoreCase)) targetOffsetTicks = ProfitTarget;
		                else if (entryName.Equals("SE2", StringComparison.OrdinalIgnoreCase)) targetOffsetTicks = ProfitTarget2;
		                else if (entryName.Equals("SE3", StringComparison.OrdinalIgnoreCase)) targetOffsetTicks = ProfitTarget3;
		                else if (entryName.Equals("SE4", StringComparison.OrdinalIgnoreCase)) targetOffsetTicks = ProfitTarget4;
		                else targetOffsetTicks = ProfitTarget; // Default
		
		                // Calculate Prices
		                double targetPrice = entryOrder.AverageFillPrice - targetOffsetTicks * TickSize;
		                double stopPrice = entryOrder.AverageFillPrice + stopOffsetTicks * TickSize;
		
		                // Submit Exits for the TOTAL filled quantity of this order
		                ExitShortStopMarket(0, true, exitQuantity, stopPrice, stopSignalName, entryOrder.Name);
		                ExitShortLimit(0, true, exitQuantity, targetPrice, targetSignalName, entryOrder.Name);
		
		                 if (TraceOrders) Print($"{Time[0]} {entryOrder.Name}: Submitted SHORT exits (Stop: {stopPrice}, Target: {targetPrice}) for COMPLETED order qty: {exitQuantity}. Signal Names: {stopSignalName}, {targetSignalName}. From Entry: {entryOrder.Name}");
		            }
		
		             // Track submitted exit signal names (optional, place after submission)
		            if (associatedExitSignalNames.ContainsKey(entryOrder.Name))
		            {
		                if(!associatedExitSignalNames[entryOrder.Name].Contains(stopSignalName))
		                     associatedExitSignalNames[entryOrder.Name].Add(stopSignalName);
		                if(!associatedExitSignalNames[entryOrder.Name].Contains(targetSignalName))
		                     associatedExitSignalNames[entryOrder.Name].Add(targetSignalName);
		            }
		
		            // --- Clean up tracking for this specific entry order AFTER submitting exits ---
		            activeEntryOrders.Remove(entryOrder.Name);
		            // Optionally remove from entryOrderFilledQuantity too, or leave for debugging
		            // entryOrderFilledQuantity.Remove(entryOrder.Name);
		            if (TraceOrders) Print($"{Time[0]} {entryOrder.Name}: Removed from activeEntryOrders after submitting exits.");
		
		        } // End of if (isEntryOrderComplete)
		        else
		        {
		             // Entry order is only partially filled, just log and wait for more executions
		             if (TraceOrders) Print($"{Time[0]} {entryOrder.Name}: Partially filled ({totalFilledForThisOrder}/{entryOrder.Quantity}). Waiting for completion before submitting exits.");
		        }
		    }
		    else
		    {
		        // Execution was not for an order we are actively tracking (e.g., an exit fill)
		        if (TraceOrders) Print($"{Time[0]} Execution Update for Order '{execution.Order.Name}' (ID: {execution.OrderId}) - Not found in activeEntryOrders. MarketPos: {marketPosition}");
		    }
		}
		
		private void Account_PositionUpdate(object sender, PositionEventArgs e)
		{
		    if (e.Position != null && Instrument != null && e.Position.Instrument == Instrument)
		    {
		        if (e.MarketPosition == MarketPosition.Flat)
		        {
		            bool wasRealized = _beRealized; // Store state before reset
		            if (_beRealized)
		            {
		                if (TraceOrders) Print($"{Time[0]} {Instrument.FullName}: Position Flat. Resetting Breakeven flag (_beRealized = false).");
		                _beRealized = false;
		            }
		
		            // Reset trailing stop price trackers as well
		            if (highestPriceSinceBE != double.MinValue || lowestPriceSinceBE != double.MaxValue)
		            {
		                 if (TraceOrders) Print($"{Time[0]} {Instrument.FullName}: Position Flat. Resetting Trailing Stop price trackers.");
		                 highestPriceSinceBE = double.MinValue;
		                 lowestPriceSinceBE = double.MaxValue;
		            }
		
		
		            // --- Existing cleanup logic for dictionaries ---
		            if (activeEntryOrders.Count > 0 || entryOrderFilledQuantity.Count > 0 || associatedExitSignalNames.Count > 0 || activeStrategyOrders.Count > 0)
		            {
		                 if (TraceOrders) Print($"{Time[0]} {Instrument.FullName}: Position Flat. Clearing tracking dictionaries.");
		                 activeEntryOrders.Clear();
		                 entryOrderFilledQuantity.Clear();
		                 associatedExitSignalNames.Clear();
						 activeStrategyOrders.Clear();
		            }
		        }
		    }
		}
		
		#endregion
		
		#region Close All Positions Method
		
		// Keep your existing CloseAllPositionsNow method (ensure OrderState fix is applied)
		private void CloseAllPositionsNow()
		{
		    if (TraceOrders) Print($"{Time[0]}: CloseAllPositionsNow() called.");
		
		    // Step 1: Flatten Position (Submits market order to exit)
		    if (Position.MarketPosition == MarketPosition.Long)
		    {
		        // Using a distinct signal name helps identify this action in logs/executions
		        ExitLong("Manual Close Button", "");
		        if (TraceOrders) Print($"{Time[0]}: Submitted ExitLong due to CloseAllPositionsNow().");
		    }
		    else if (Position.MarketPosition == MarketPosition.Short)
		    {
		        ExitShort("Manual Close Button", "");
		        if (TraceOrders) Print($"{Time[0]}: Submitted ExitShort due to CloseAllPositionsNow().");
		    }
		    else
		    {
		         if (TraceOrders) Print($"{Time[0]}: Already flat, no position to close via ExitLong/Short.");
		    }
		
		
		    // Step 2: Cancel orders currently tracked by THIS strategy instance in the list
		    // Create a temporary copy of the list to iterate over safely,
		    // as the original list might be modified by OnOrderUpdate if cancellations process quickly.
		    List<Order> ordersToCancel = Account.Orders.Where(o =>
                    (o.Name.StartsWith("LE") || o.Name.StartsWith("SE")) &&
                    (o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted ||
                     o.OrderState == OrderState.Submitted || o.OrderState == OrderState.TriggerPending ||
                     o.OrderState == OrderState.ChangePending || o.OrderState == OrderState.CancelPending) &&
                    o.Instrument == Instrument &&
                    o.Account == Account).ToList();
		
		    if (TraceOrders) Print($"{Time[0]}: Attempting to cancel {ordersToCancel.Count} orders currently tracked in Account orders");
		    int cancelCount = 0;
		
		    if (ordersToCancel.Count > 0)
		    {
		        foreach (Order order in ordersToCancel)
		        {
		             // Double-check state just before cancelling, as it might have changed
		             // since being added to the list or since the loop started.
		             if (order.OrderState == OrderState.Working ||
		                 order.OrderState == OrderState.Accepted ||
		                 order.OrderState == OrderState.Submitted ||
		                 order.OrderState == OrderState.TriggerPending || // Include states that might be pending activation
		                 order.OrderState == OrderState.ChangePending ||
		                 order.OrderState == OrderState.CancelPending )
		             {
		                try
		                {
		                    if (TraceOrders) Print($"--> Attempting cancel for tracked Order: {order.Name} (ID: {order.OrderId}, State: {order.OrderState})");
		                    CancelOrder(order);
		                    cancelCount++;
		                    // IMPORTANT: Do NOT remove from activeStrategyOrders here.
		                    // Let OnOrderUpdate handle removal when the final 'Cancelled' state update arrives.
		                }
		                catch (Exception ex)
		                {
		                    Print($"{Time[0]}: ERROR cancelling tracked order {order.Name} (ID: {order.OrderId}): {ex.Message}");
		                    // If cancel fails, the order might still be active. OnOrderUpdate will keep it in the list
		                    // until a terminal state (like Rejected or maybe eventually Cancelled) is received.
		                }
		            } else {
		                 // Order was in the list but is no longer in a cancellable state (might have just filled/cancelled naturally)
		                 if (TraceOrders) Print($"--> Skipping cancel for tracked order {order.Name} (ID: {order.OrderId}) as its current state ({order.OrderState}) is not cancellable.");
		            }
		        }
		    }
		    else if (TraceOrders)
		    {
		        Print($"{Time[0]}: No orders currently tracked in activeStrategyOrders list to attempt cancellation.");
		    }
		
		    if (TraceOrders) Print($"{Time[0]}: Finished CloseAllPositionsNow(). Initiated cancellation for {cancelCount} tracked orders.");
		
		    // Note: Clear Dictionaries in Account_PositionUpdate when MarketPosition becomes Flat.		    
		}
		
		#endregion
		
		#region WPF Button Creation and Management

        // Main method to create the buttons
        private void CreateWPFControls()
        {
            // Use Dispatcher correctly at the start if not already on UI thread
            // (Usually called from Dispatcher.InvokeAsync, so likely safe, but good practice)
             if (ChartControl == null || !ChartControl.Dispatcher.CheckAccess())
             {
                 if(ChartControl != null) ChartControl.Dispatcher.InvokeAsync(() => CreateWPFControls());
                 return;
             }

            if (buttonsCreated) // Prevent duplicates if called again unexpectedly
            {
                 if (TraceOrders) Print("CreateWPFControls: Skipping, buttonsCreated is already true.");
                 return;
            }

            try
            {
                parentChart = System.Windows.Window.GetWindow(ChartControl.Parent) as Gui.Chart.Chart;
                if (parentChart == null)
                {
                    Print("CreateWPFControls Error: parentChart could not be found.");
                    return;
                }

                // Find the main ChartTrader content grid using your confirmed working method
                // Added null-conditional operator ?. for safety
                chartTraderGrid = (parentChart.FindFirst("ChartWindowChartTraderControl") as Gui.Chart.ChartTrader)?.Content as System.Windows.Controls.Grid;
                if (chartTraderGrid == null)
                {
                    Print("CreateWPFControls Error: chartTraderGrid was null (ChartTrader content not found).");
                    return;
                }

                // Find the specific grid where NT buttons usually go
                // Try finding by specific name first (more robust if the name is stable)
                 chartTraderButtonsGrid = chartTraderGrid.FindFirst("chartTraderButtonsGrid") as Grid; // Check this name with Snoop if unsure
                if (chartTraderButtonsGrid == null)
                {
                    // Fallback to your index method if name isn't found
                    if (chartTraderGrid.Children.Count > 0 && chartTraderGrid.Children[0] is Grid) {
                        chartTraderButtonsGrid = chartTraderGrid.Children[0] as Grid;
                        if (TraceOrders) Print("CreateWPFControls: Found chartTraderButtonsGrid by index fallback.");
                    }
                } else {
                     if (TraceOrders) Print("CreateWPFControls: Found chartTraderButtonsGrid by name.");
                }

                if (chartTraderButtonsGrid == null)
                {
                    Print("CreateWPFControls Error: Could not find target 'chartTraderButtonsGrid'. Buttons not added.");
                    return;
                }


                // --- Create Our Custom Grid ---
                customButtonsGrid = new Grid
                {
                    Name = "customStrategyButtonsGrid", // Important for removal
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(2, 5, 2, 0) // Standard margin
                };

                // Define Columns (2 columns, equal width)
                customButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                customButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Define Rows (3 rows needed: Auto, Long/Short, Close)
                customButtonsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 0
                customButtonsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 1
                customButtonsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 2

                // --- Create Buttons ---
                Thickness buttonMargin = new Thickness(2);

                // 1. Auto Button (Row 0, Span 2 Columns)
                autoBtn = new Button { Name = "autoBtn", Content = "AUTO", Margin = buttonMargin, Padding = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
                autoBtn.Click += AutoBtn_Click;
                Grid.SetRow(autoBtn, 0);
                Grid.SetColumn(autoBtn, 0);
                Grid.SetColumnSpan(autoBtn, 2);
                customButtonsGrid.Children.Add(autoBtn);

                // 2. Short Button (Row 1, Col 0)
                shortBtn = new Button { Name = "shortBtn", Content = "SHORTS", Margin = buttonMargin, Padding = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
                shortBtn.Click += ShortBtn_Click;
                Grid.SetRow(shortBtn, 1);
                Grid.SetColumn(shortBtn, 0);
                Grid.SetColumnSpan(shortBtn, 1);
                customButtonsGrid.Children.Add(shortBtn);

                // 3. Long Button (Row 1, Col 1)
                longBtn = new Button { Name = "longBtn", Content = "LONGS", Margin = buttonMargin, Padding = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
                longBtn.Click += LongBtn_Click;
                Grid.SetRow(longBtn, 1);
                Grid.SetColumn(longBtn, 1);
                Grid.SetColumnSpan(longBtn, 1);
                customButtonsGrid.Children.Add(longBtn);

                // 4. Close Button (Row 2, Span 2 Columns)
                closeBtn = new Button { Name = "closeBtn", Content = "CLOSE", Foreground = Brushes.White, Background = Brushes.DimGray, Margin = buttonMargin, Padding = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
                closeBtn.Click += CloseBtn_Click;
                Grid.SetRow(closeBtn, 2); // Set to Row 2
                Grid.SetColumn(closeBtn, 0);
                Grid.SetColumnSpan(closeBtn, 2);
                customButtonsGrid.Children.Add(closeBtn);

                // --- Add our grid to the Chart Trader ---
                // **Store the RowDefinition before adding it**
                addedRowDefinition = new RowDefinition { Height = GridLength.Auto };
                chartTraderButtonsGrid.RowDefinitions.Add(addedRowDefinition);

                int targetRow = chartTraderButtonsGrid.RowDefinitions.Count - 1;
                Grid.SetRow(customButtonsGrid, targetRow);

                // ***** FIX for WIDTH *****
                Grid.SetColumn(customButtonsGrid, 0); // Place in first column of parent
                int parentColumnCount = chartTraderButtonsGrid.ColumnDefinitions.Count;
                Grid.SetColumnSpan(customButtonsGrid, Math.Max(1, parentColumnCount)); // Span ALL columns
                // **************************

                chartTraderButtonsGrid.Children.Add(customButtonsGrid);


                // --- Set Initial Button States ---
                UpdateAutoButtonAppearance();
                UpdateLongButtonAppearance();
                UpdateShortButtonAppearance();

                buttonsCreated = true; // Set flag AFTER successful creation
                if (TraceOrders) Print("CreateWPFControls: Successfully created WPF controls.");

            }
            catch (Exception e)
            {
                Print("CreateWPFControls Error: " + e.Message + Environment.NewLine + e.StackTrace);
                // Attempt cleanup if creation failed partially
                RemoveWPFControls(); // Call cleanup on error
            }
        }

        /// <summary>
        /// Removes strategy-specific WPF controls from the chart trader.
        /// This version re-finds parent controls and searches for the custom grid by name
        /// to ensure reliable cleanup during strategy reloads.
        /// </summary>
        private void RemoveWPFControls()
        {
            // Use Dispatcher for thread safety
            if (ChartControl == null || ChartControl.Dispatcher == null)
                return;
            if (!ChartControl.Dispatcher.CheckAccess())
            {
                ChartControl.Dispatcher.InvokeAsync(() => RemoveWPFControls());
                return;
            }

            // --- Don't rely solely on buttonsCreated flag, attempt cleanup anyway ---
            // if (!buttonsCreated) return; // Removed this check for more robust cleanup

            if (TraceOrders) Print("RemoveWPFControls: Attempting cleanup...");

            try
            {
                // --- Step 1: Re-find the Parent Grid (chartTraderButtonsGrid) ---
                // This is crucial because member variables might be stale during Terminate/Reload
                Gui.Chart.Chart currentChart = System.Windows.Window.GetWindow(ChartControl.Parent) as Gui.Chart.Chart;
                Grid parentButtonGrid = null; // The grid holding NT's buttons

                if (currentChart != null)
                {
                    Grid mainTraderGrid = (currentChart.FindFirst("ChartWindowChartTraderControl") as Gui.Chart.ChartTrader)?.Content as System.Windows.Controls.Grid;
                    if (mainTraderGrid != null)
                    {
                        // Try finding the NT button grid by name first
                         parentButtonGrid = mainTraderGrid.FindFirst("chartTraderButtonsGrid") as Grid;
                         if (parentButtonGrid == null && mainTraderGrid.Children.Count > 0 && mainTraderGrid.Children[0] is Grid)
                         {
                             // Fallback to index if name not found
                             parentButtonGrid = mainTraderGrid.Children[0] as Grid;
                              if (TraceOrders) Print("RemoveWPFControls: Found parentButtonGrid by index fallback.");
                         } else if (parentButtonGrid != null) {
                             if (TraceOrders) Print("RemoveWPFControls: Found parentButtonGrid by name.");
                         }
                    }
                }

                if (parentButtonGrid == null)
                {
                    if (TraceOrders) Print("RemoveWPFControls: Could not find parent 'chartTraderButtonsGrid' during removal. Cleanup might be incomplete.");
                    // Proceed to detach handlers etc. anyway
                }

                // --- Step 2: Find and Remove Our Specific Grid by Name ---
                Grid gridToRemove = null;
                if (parentButtonGrid != null) // Only search if parent was found
                {
                    gridToRemove = parentButtonGrid.Children.OfType<Grid>()
                                        .FirstOrDefault(g => g.Name == "customStrategyButtonsGrid");

                    if (gridToRemove != null)
                    {
                        if (TraceOrders) Print($"RemoveWPFControls: Found grid '{gridToRemove.Name}'. Removing...");
                        parentButtonGrid.Children.Remove(gridToRemove);
                        if (TraceOrders) Print($"RemoveWPFControls: Successfully removed '{gridToRemove.Name}'.");

                        // --- Step 3: Remove the specific RowDefinition IF it was stored AND exists in the found parent ---
                        if (addedRowDefinition != null && parentButtonGrid.RowDefinitions.Contains(addedRowDefinition))
                        {
                            parentButtonGrid.RowDefinitions.Remove(addedRowDefinition);
                            if (TraceOrders) Print("RemoveWPFControls: Removed added RowDefinition.");
                        }
                        else if (addedRowDefinition != null)
                        {
                             if (TraceOrders) Print("RemoveWPFControls: Warning - addedRowDefinition not found in parent grid's RowDefinitions during removal.");
                        }
                    }
                    else
                    {
                        if (TraceOrders) Print("RemoveWPFControls: Grid 'customStrategyButtonsGrid' not found in parent's children.");
                    }
                }
                 // --- End Grid/Row Removal ---


                 // --- Step 4: Detach handlers and nullify references from THIS instance ---
                 // This is crucial to prevent memory leaks, do it regardless of UI removal success
                 if (autoBtn != null) autoBtn.Click -= AutoBtn_Click;
                 if (longBtn != null) longBtn.Click -= LongBtn_Click;
                 if (shortBtn != null) shortBtn.Click -= ShortBtn_Click;
                 if (closeBtn != null) closeBtn.Click -= CloseBtn_Click;
                 if (TraceOrders) Print("RemoveWPFControls: Detached event handlers.");

                autoBtn = null;
                longBtn = null;
                shortBtn = null;
                closeBtn = null;
                customButtonsGrid = null;
                addedRowDefinition = null; // Clear the stored row reference
                // Don't nullify parentChart, chartTraderGrid, chartTraderButtonsGrid here,
                // as they were found dynamically or belong to NT.

                buttonsCreated = false; // Reset the flag *after* cleanup attempt
                if (TraceOrders) Print("RemoveWPFControls: Cleanup finished, buttonsCreated set to false.");
            }
            catch (Exception e)
            {
                Print("RemoveWPFControls Error: " + e.Message + Environment.NewLine + e.StackTrace);
                buttonsCreated = false; // Ensure flag is reset even on error
                addedRowDefinition = null; // Ensure reference is cleared on error
            }
        }

      

        

        // --- Button Click Handlers ---

        private void AutoBtn_Click(object sender, RoutedEventArgs e)
        {
            isAutoEnabled = !isAutoEnabled;
            UpdateAutoButtonAppearance();
             if (TraceOrders) Print($"AUTO Button Clicked. isAutoEnabled set to: {isAutoEnabled}");
        }

        private void LongBtn_Click(object sender, RoutedEventArgs e)
        {
            isLongEnabled = !isLongEnabled;
            UpdateLongButtonAppearance();
            if (TraceOrders) Print($"LONGS Button Clicked. isLongEnabled set to: {isLongEnabled}");
        }

        private void ShortBtn_Click(object sender, RoutedEventArgs e)
        {
            isShortEnabled = !isShortEnabled;
            UpdateShortButtonAppearance();
             if (TraceOrders) Print($"SHORTS Button Clicked. isShortEnabled set to: {isShortEnabled}");
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            // Call the method to close positions and cancel orders
            CloseAllPositionsNow();
        }

        // --- Button Appearance Update Methods ---

        private void UpdateAutoButtonAppearance()
        {
            // Ensure update happens on UI thread if called from elsewhere (though clicks are already on UI thread)
            if (autoBtn == null) return;
            Action update = () => {
                if (isAutoEnabled)
                {
                    autoBtn.Content = "AUTO ON";
                    autoBtn.Background = Brushes.Green;
                    autoBtn.Foreground = Brushes.White;
                }
                else
                {
                    autoBtn.Content = "AUTO OFF";
                    autoBtn.Background = Brushes.Red;
                    autoBtn.Foreground = Brushes.White;
                }
            };
             if (autoBtn.Dispatcher.CheckAccess()) update();
             else autoBtn.Dispatcher.InvokeAsync(update); // Use InvokeAsync for safety
        }

        private void UpdateLongButtonAppearance()
        {
            if (longBtn == null) return;
             Action update = () => {
                if (isLongEnabled)
                {
                    longBtn.Content = "LONGS ON";
                    longBtn.Background = Brushes.Green;
                     longBtn.Foreground = Brushes.White;
                }
                else
                {
                    longBtn.Content = "LONGS OFF";
                    longBtn.Background = Brushes.Red;
                    longBtn.Foreground = Brushes.White;
                }
            };
             if (longBtn.Dispatcher.CheckAccess()) update();
             else longBtn.Dispatcher.InvokeAsync(update);
        }

        private void UpdateShortButtonAppearance()
        {
            if (shortBtn == null) return;
             Action update = () => {
                if (isShortEnabled)
                {
                    shortBtn.Content = "SHORTS ON";
                    shortBtn.Background = Brushes.Green;
                    shortBtn.Foreground = Brushes.White;
                }
                else
                {
                    shortBtn.Content = "SHORTS OFF";
                    shortBtn.Background = Brushes.Red;
                    shortBtn.Foreground = Brushes.White;
                }
            };
             if (shortBtn.Dispatcher.CheckAccess()) update();
             else shortBtn.Dispatcher.InvokeAsync(update);
        }

        #endregion // End WPF Button Creation and Management
		
		#region Emergency WPF Cleanup
		
		/// <summary>
		/// EMERGENCY CLEANUP: Finds and removes ALL Grids named "customStrategyButtonsGrid"
		/// from the Chart Trader Button Grid. Call this ONCE manually if duplicates appear.
		/// </summary>
		private void EmergencyRemoveStrategyButtons()
		{
		    // Guards - need ChartControl and Dispatcher
		    if (ChartControl == null || ChartControl.Dispatcher == null)
		    {
		        Print("EmergencyRemove: ChartControl or Dispatcher is null.");
		        return;
		    }
		
		    // Ensure execution on the UI thread
		    if (!ChartControl.Dispatcher.CheckAccess())
		    {
		        ChartControl.Dispatcher.InvokeAsync(() => EmergencyRemoveStrategyButtons());
		        return;
		    }
		
		    Print("EmergencyRemove: Attempting cleanup...");
		
		    try
		    {
		        // --- Step 1: Find the Parent Grid (chartTraderButtonsGrid) ---
		        // Use the robust finding logic established previously
		        Chart wnd = System.Windows.Window.GetWindow(ChartControl.Parent) as Gui.Chart.Chart;
		        Grid parentButtonGrid = null;
		        Grid mainTraderGrid = null;
		
		        if (wnd != null)
		        {
		            mainTraderGrid = wnd.FindFirst("ChartWindowChartTraderGrid") as Grid;
		            if(mainTraderGrid == null)
		               mainTraderGrid = (wnd.FindFirst("ChartWindowChartTraderControl") as Gui.Chart.ChartTrader)?.Content as System.Windows.Controls.Grid;
		        }
		        else
		        {
		            NinjaTrader.Gui.Chart.Chart chart = System.Windows.Window.GetWindow(ChartControl.Parent) as NinjaTrader.Gui.Chart.Chart;
		            if (chart != null)
		                mainTraderGrid = (chart.FindFirst("ChartWindowChartTraderControl") as Gui.Chart.ChartTrader)?.Content as System.Windows.Controls.Grid;
		        }
		
		        if (mainTraderGrid != null)
		        {
		            parentButtonGrid = mainTraderGrid.FindFirst("chartTraderButtonsGrid") as Grid; // Try name
		            if (parentButtonGrid == null && mainTraderGrid.Children.Count > 0) // Fallback index (safer version)
		                parentButtonGrid = mainTraderGrid.Children.OfType<Grid>().FirstOrDefault();
		        }
		
		        if (parentButtonGrid == null)
		        {
		            Print("EmergencyRemove: Could not find parent 'chartTraderButtonsGrid'. Cannot clean up.");
		            return;
		        }
		        Print($"EmergencyRemove: Found parent grid '{parentButtonGrid.Name ?? "Unnamed"}'.");
		
		        // --- Step 2: Find ALL instances of our custom grid by name ---
		        // Use ToList() to create a copy, allowing safe removal while iterating
		        List<Grid> gridsToRemove = parentButtonGrid.Children.OfType<Grid>()
		                                     .Where(g => g.Name == "customStrategyButtonsGrid")
		                                     .ToList();
		
		        if (gridsToRemove.Count == 0)
		        {
		            Print("EmergencyRemove: No grids named 'customStrategyButtonsGrid' found to remove.");
		            return;
		        }
		
		        Print($"EmergencyRemove: Found {gridsToRemove.Count} grid(s) named 'customStrategyButtonsGrid'. Removing...");
		
		        // --- Step 3: Remove them ---
		        int removedCount = 0;
		        foreach (Grid grid in gridsToRemove)
		        {
		            parentButtonGrid.Children.Remove(grid);
		            removedCount++;
		        }
		
		        Print($"EmergencyRemove: Successfully removed {removedCount} grid(s).");
		
		        // Note: This emergency function does NOT attempt to remove the associated RowDefinitions
		        // as we don't have references to them from potentially old instances.
		        // Removing just the grids is usually sufficient for a visual cleanup.
		
		    }
		    catch (Exception e)
		    {
		        Print($"EmergencyRemove: Error during cleanup: {e.Message}{Environment.NewLine}{e.StackTrace}");
		    }
		}
		
		#endregion
		
		#region Properties

		#region 01a. Release Notes
		[ReadOnly(true)]
		[NinjaScriptProperty]
		[Display(Name="BaseAlgoVersion", Order=1, GroupName="01a. Release Notes")]
		public string BaseAlgoVersion { get; set; }
		
		[ReadOnly(true)]
		[NinjaScriptProperty]
		[Display(Name="Author", Order=2, GroupName="01a. Release Notes")]
		public string Author { get; set; }		
		
		[ReadOnly(true)]
		[NinjaScriptProperty]
		[Display(Name="StrategyName", Order=3, GroupName="01a. Release Notes")]
		public string StrategyName { get; set; }
		
		[ReadOnly(true)]
		[NinjaScriptProperty]
		[Display(Name="Version", Order =4, GroupName="01a. Release Notes")]
		public string Version { get; set; }
		
		[ReadOnly(true)]
		[NinjaScriptProperty]
		[Display(Name="Credits", Order=5, GroupName="01a. Release Notes")]
		public string Credits { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Chart Type", Order=6, GroupName="01a. Release Notes")]
		public string ChartType { get; set; }
		#endregion
		
		#region 01b. Support Developer
		[NinjaScriptProperty]
		[Display(Name = "PayPal Donation URL", Order = 1, GroupName = "01b. Support Developer", Description = "https://www.paypal.com/signin")]
		public string paypal { get; set; }
		#endregion

		#region 02. Order Settings	
		[NinjaScriptProperty]
        [Display(Name = "Order Type (Market/Limit)", Order = 2, GroupName = "02. Order Settings")]
        public OrderType OrderType { get; set; } 
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Limit Order Offset", Order= 3, GroupName="02. Order Settings")]
		public double LimitOffset { get; set; }	
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Contracts", Order= 4, GroupName="02. Order Settings")]
		public int Contracts { get; set; }	
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Tick Move (Button Click)", Order= 5, GroupName="02. Order Settings")]
		public int TickMove { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Initial Stop (Ticks)", Order= 6, GroupName="02. Order Settings")]
		public int InitialStop { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Profit Target", Order=7, GroupName="02. Order Settings")]
		public double ProfitTarget { get; set; }
		
		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]	
		[Display(Name="Enable Profit Target 2", Order= 8, GroupName="02. Order Settings")]
		public bool EnableProfitTarget2 { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Contract 2", Order= 9, GroupName="02. Order Settings")]
		public int Contracts2 { get; set; }	
		
		[NinjaScriptProperty]
		[Display(Name="Profit Target 2", Order=10, GroupName="02. Order Settings")]
		public double ProfitTarget2 { get; set; }
		
		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]	
		[Display(Name="Enable Profit Target 3", Order= 11, GroupName="02. Order Settings")]
		public bool EnableProfitTarget3 { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Contract 3", Order= 12, GroupName="02. Order Settings")]
		public int Contracts3 { get; set; }	
		
		[NinjaScriptProperty]
		[Display(Name="Profit Target3", Order=13, GroupName="02. Order Settings")]
		public double ProfitTarget3 { get; set; }
		
		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]	
		[Display(Name="Enable Profit Target 4", Order= 14, GroupName="02. Order Settings")]
		public bool EnableProfitTarget4 { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Contract 4", Order= 15, GroupName="02. Order Settings")]
		public int Contracts4 { get; set; }	
		
		[NinjaScriptProperty]
		[Display(Name="Profit Target4", Order=16, GroupName="02. Order Settings")]
		public double ProfitTarget4 { get; set; }	
		#endregion
		
		#region 03. Order Management		
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Cancel Order After Bars", Order = 1, GroupName="03. Order Management")]
		public int CancelAfterBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name="ATR Period", Order= 2, GroupName="03. Order Management")]
		public int AtrPeriod { get; set; }
		
		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]	
		[Display(Name="Enable Breakeven", Order= 6, GroupName="03. Order Management")]	
		public bool BESetAuto { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Breakeven Trigger", Order = 7, Description="In Ticks", GroupName="03. Order Management")]
		public int BE_Trigger { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Breakeven Offset", Order = 8, Description="In Ticks", GroupName="03. Order Management")]
		public int BE_Offset { get; set; }	
		
		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]	
		[Display(Name="Enable Tick Trailing Stop", Order= 9, GroupName="03. Order Management")]	
		public bool EnableTickTrail { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Tick Trail Amount", Order = 9, Description="In Ticks", GroupName="03. Order Management")]
		public int TickTrailAmount { get; set; }	
		
		[NinjaScriptProperty]
		[Display(Name = "Enable Background Color Signal", Description = "Enable Exit", Order = 10, GroupName="03. Order Management")]
		[RefreshProperties(RefreshProperties.All)]
		public bool enableBackgroundSignal { get; set; }
		
		#endregion				
		
		#region 05. Profit/Loss Limit	
		[NinjaScriptProperty]
		[Display(Name = "Enable Daily Loss / Profit ", Description = "Enable or disable daily PnL control", Order =1, GroupName="05. Profit/Loss Limit")]
		[RefreshProperties(RefreshProperties.All)]
		public bool dailyLossProfit { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Daily Profit Limit ($)", Description="Enter a positive integer", Order=2, GroupName="05. Profit/Loss Limit")]
		public double DailyProfitLimit { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Daily Loss Limit ($)", Description="Enter a positive integer", Order=3, GroupName="05. Profit/Loss Limit")]
		public double DailyLossLimit { get; set; }	
		
		#endregion
		
		#region Other Trade Controls
		
		[NinjaScriptProperty]
		[Display(Name="Bars Since Exit", Description = "Number of bars that have elapsed since the last specified exit. 0 == Not used. >1 == Use number of bars specified ", Order=4, GroupName="07. Other Trade Controls" )]
		public int iBarsSinceExit
		{ get; set; }
		
		#endregion
		
		#region 08b. Default Settings			
		[NinjaScriptProperty]
		[Display(Name = "Enable Buy Sell Pressure", Order = 1, GroupName="08b. Default Settings")]
		public bool enableBuySellPressure { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "Show Buy Sell Pressure", Order = 2, GroupName="08b. Default Settings")]
		public bool showBuySellPressure { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "Enable VMA", Order = 3, GroupName="08b. Default Settings")]
		public bool enableVMA { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "Show VMA", Order = 4, GroupName="08b. Default Settings")]
		public bool showVMA { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "Enable Hooker", Order = 5, GroupName="08b. Default Settings")]
		public bool enableHmaHooks { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "Show HMA Hooks", Order = 6, GroupName="08b. Default Settings")]
		public bool showHmaHooks { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "HMA Period", Order = 7, GroupName="08b. Default Settings")]
		public int HmaPeriod { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "Enable KingKhanh", Order = 8, GroupName="08b. Default Settings")]
		public bool enableRegChan1 { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "Enable Inner Regression Channel", Order = 9, GroupName="08b. Default Settings")]
		public bool enableRegChan2 { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "Show Outer Regression Channel", Order = 10, GroupName="08b. Default Settings")]
		public bool showRegChan1 { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "Show Inner Regression Channel", Order = 11, GroupName="08b. Default Settings")]
		public bool showRegChan2 { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "Show High and Low Lines", Order = 12, GroupName="08b. Default Settings")]
		public bool showRegChanHiLo { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Regression Channel Period", Order = 13, GroupName="08b. Default Settings")]
		public int RegChanPeriod { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Outer Regression Channel Width", Order = 14, GroupName="08b. Default Settings")]
		public double RegChanWidth { get; set; }
			
		[NinjaScriptProperty]
		[Display(Name = "Inner Regression Channel Width", Order = 15, GroupName="08b. Default Settings")]
		public double RegChanWidth2 { get; set; }
	
		[NinjaScriptProperty]
        [Display(Name = "Enable Momo", Order = 16, GroupName="08b. Default Settings")]
        public bool enableMomo { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name = "Show Momentum", Order = 17, GroupName="08b. Default Settings")]
        public bool showMomo { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Momo Up", Order = 18, GroupName="08b. Default Settings")]
		public int MomoUp { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Momo Down", Order = 19, GroupName="08b. Default Settings")]
		public int MomoDown { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name = "Enable ADX", Order = 20, GroupName="08b. Default Settings")]
        public bool enableADX { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name = "Show ADX", Order = 21, GroupName="08b. Default Settings")]
        public bool showAdx { get; set; }
		
		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "ADX Period", Order = 22, GroupName="08b. Default Settings")]
        public int adxPeriod { get; set; }
		
		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "ADX Threshold 1", Order = 23, GroupName="08b. Default Settings")]
        public int adxThreshold { get; set; }
		
		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "ADX Threshold 2", Order = 24, GroupName="08b. Default Settings")]
        public int adxThreshold2 { get; set; }
		
		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "ADX Exit Threshold", Order = 25, GroupName="08b. Default Settings")]
        public int adxExitThreshold { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name = "Enable Volatility", Order = 26, GroupName="08b. Default Settings")]
        public bool enableVolatility { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name="Volatility Threshold", Order = 27, GroupName="08b. Default Settings")]
        public double atrThreshold { get; set; }		
		
		[NinjaScriptProperty]
        [Display(Name = "Enable EMA Filter", Order = 28, GroupName="08b. Default Settings")]
        public bool enableEMAFilter { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name = "Show EMA", Order = 29, GroupName="08b. Default Settings")]
        public bool showEMA { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="EMA Length", Order = 30, GroupName="08b. Default Settings")]
		public int emaLength { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name = "Enable OrderFlow Signals", Order = 32, GroupName="08b. Default Settings")]
        public bool enableOrderFlowSignals { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name = "Show OrderFlowSignals", Order = 33, GroupName="08b. Default Settings")]
        public bool showOrderFlowSignals { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name = "Enable Key Level Filter", Order = 34, GroupName="08b. Default Settings")]
        public bool enableKeyLevelFilter { get; set; }
		
		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Key Level Proximity (Ticks)", Description="Do not trade if price is within X ticks of a key level.", Order = 35, GroupName = "08b. Default Settings")] // Added 34a for ordering
        public int keyLevelProximity { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name = "Show Prior Day OHLC", Order = 36, GroupName="08b. Default Settings")]
        public bool showPriorOHLC { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name = "Show VWAP", Order = 37, GroupName="08b. Default Settings")]
        public bool showVWAP { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name = "Show Pivots", Order = 38, GroupName="08b. Default Settings")]
        public bool showPivots { get; set; }
		
		#endregion	
		
		#region 09. Market Condition		
		[NinjaScriptProperty]
		[Display(Name = "Enable Choppiness Detection", Order = 1, GroupName="09. Market Condition")]
		public bool EnableChoppinessDetection { get; set; } 
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Regression Channel Look Back Period", Description="Period for Regression Channel used in chop detection.", Order=2, GroupName="09. Market Condition")]
		public int SlopeLookBack { get; set; }
		
		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name="Flat Slope Factor", Description="Indicates how flat the regression channel is.", Order=3, GroupName="09. Market Condition")]
		public double FlatSlopeFactor { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Chop ADX Threshold", Description="ADX value below which the market is considered choppy.", Order=4, GroupName="09. Market Condition")]
		public int ChopAdxThreshold { get; set; }
		#endregion

		#region 10. Timeframes
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Start Trades", Order=1, GroupName="10. Timeframes")]
		public DateTime Start { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="End Trades", Order=2, GroupName="10. Timeframes")]
		public DateTime End { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Enable Time 2", Description = "Enable second trading time window.", Order=3, GroupName="10. Timeframes")]
		[RefreshProperties(RefreshProperties.All)]
		public bool Time2
		{
		 	get { return isEnableTime2; } 
			set { isEnableTime2 = value; }
		}
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Start Time 2", Order=4, GroupName="10. Timeframes")]
		public DateTime Start2 { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="End Time 2", Order=5, GroupName="10. Timeframes")]
		public DateTime End2 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Enable Time 3", Description = "Enable third trading time window.", Order=6, GroupName="10. Timeframes")]
		[RefreshProperties(RefreshProperties.All)]
		public bool Time3
		{
		 	get { return isEnableTime3; } 
			set { isEnableTime3 = value; }
		}
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Start Time 3", Order=7, GroupName="10. Timeframes")]
		public DateTime Start3 { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="End Time 3", Order=8, GroupName="10. Timeframes")]
		public DateTime End3 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Enable Time 4", Description = "Enable fourth trading time window.", Order=9, GroupName="10. Timeframes")]
		[RefreshProperties(RefreshProperties.All)]
		public bool Time4
		{
		 	get { return isEnableTime4; } 
			set { isEnableTime4 = value; }
		}
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Start Time 4", Order=10, GroupName="10. Timeframes")]
		public DateTime Start4 { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="End Time 4", Order=11, GroupName="10. Timeframes")]
		public DateTime End4 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Enable Time 5", Description = "Enable fifth trading time window.", Order=12, GroupName="10. Timeframes")]
		[RefreshProperties(RefreshProperties.All)]
		public bool Time5
		{
		 	get { return isEnableTime5; } 
			set { isEnableTime5 = value; }
		}
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Start Time 5", Order=13, GroupName="10. Timeframes")]
		public DateTime Start5 { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="End Time 5", Order=14, GroupName="10. Timeframes")]
		public DateTime End5 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Enable Time 6", Description = "Enable sixth trading time window.", Order=15, GroupName="10. Timeframes")]
		[RefreshProperties(RefreshProperties.All)]
		public bool Time6
		{
		 	get { return isEnableTime6; } 
			set { isEnableTime6 = value; }
		}
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Start Time 6", Order=16, GroupName="10. Timeframes")]
		public DateTime Start6 { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="End Time 6", Order=17, GroupName="10. Timeframes")]
		public DateTime End6 { get; set; }
		#endregion
		
		#region 11. Status Panel	
		[NinjaScriptProperty]
        [Display(Name = "Show Daily PnL", Order = 1, GroupName="11. Status Panel")]
        public bool showDailyPnl { get; set; }			
		
		[XmlIgnore()]
		[Display(Name = "Daily PnL Color", Order = 2, GroupName="11. Status Panel")]
		public Brush colorDailyProfitLoss { get; set; }	
		
		[NinjaScriptProperty]
		[Display(Name="Daily PnL Position", Description = "Daily PnL display position", Order = 3, GroupName="11. Status Panel")]
		public TextPosition PositionDailyPNL { get; set; }
		
		[Browsable(false)]
		public string colorDailyProfitLossSerialize
		{
			get { return Serialize.BrushToString(colorDailyProfitLoss); }
   			set { colorDailyProfitLoss = Serialize.StringToBrush(value); }
		}
		
        [NinjaScriptProperty]
        [Display(Name = "Show STATUS PANEL", Order = 4, GroupName="11. Status Panel")]
        public bool showPnl { get; set; }		

		[XmlIgnore()]
		[Display(Name = "STATUS PANEL Color", Order = 5, GroupName="11. Status Panel")]
		public Brush colorPnl { get; set; }				
		
		[NinjaScriptProperty]
		[Display(Name="STATUS PANEL Position", Description = "Status PnL display position", Order = 6, GroupName="11. Status Panel")]
		public TextPosition PositionPnl { get; set; }	
		
		[Browsable(false)]
		public string colorPnlSerialize
		{
			get { return Serialize.BrushToString(colorPnl); }
   			set { colorPnl = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[Display(Name="Show Historical Trades", Description = "Display theoretical historical trades", Order=7, GroupName="11. Status Panel")]
		public bool ShowHistorical { get; set; }
		#endregion

		#region Properties from Strategy
		[NinjaScriptProperty]
        [Display(Name = "Enable Willy", Order = 16, GroupName="08a. Strategy Settings")]
        public bool enableWilly { get; set; }
        
		[NinjaScriptProperty]
        [Display(Name = "Show Willy", Order = 17, GroupName="08a. Strategy Settings")]
        public bool showWilly { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Willy Period", Order = 18, GroupName="08a. Strategy Settings")]
		public int wrPeriod { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Willy Up", Order = 19, GroupName="08a. Strategy Settings")]
		public int wrUp { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Willy Down", Order = 20, GroupName="08a. Strategy Settings")]
		public int wrDown { get; set; }
		#endregion
		
		#region Debugging
		
		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]	
		[Display(Name="Enable Trace Orders", Order= 8, GroupName="02. Ord")]
		public bool TraceOrders { get; set; }
		
		#endregion
		
		#endregion
    }
}

