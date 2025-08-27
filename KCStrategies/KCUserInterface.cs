// --- KCAlgoBase_UI.cs ---
// Version 6.6.2 - UI Layout Adjustment
// Key Changes:
// 1. UI REORDER: Moved Order Type and Limit Offset to be above Stop Type.

#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.Strategies.KCStrategies
{
    public abstract partial class KCAlgoBase : Strategy
    {
		#region UI Variables
        private Chart chartWindow;
		private Grid chartTraderGrid, lowerButtonsGrid;
		private ScrollViewer panelScrollViewer;
		
		// --- Official NinjaTrader Controls ---
		private ComboBox accountSelector;
		private QuantityUpDown quantitySelector;
		
		// --- Custom Controls ---
		private ComboBox orderTypeSelector, profitTargetSelector, stopTypeSelector, beTriggerModeSelector;
		private TextBox limitOffsetTextBox, rrRatioTextBox, trailLookbackTextBox, closePartialTextBox, initialStopTextBox;
		private TextBox profitTargetTextBox, manualMoveStopLookbackTextBox, beTriggerTextBox, beOffsetTextBox;
		private TextBlock beTriggerLabel; // Need a reference to this label to change its text
		private Button limitOffsetUp, limitOffsetDown, rrRatioUp, rrRatioDown, trailLookbackUp, trailLookbackDown, closePartialUp, closePartialDown; 
		private Button initialStopUp, initialStopDown, profitTargetUp, profitTargetDown, manualMoveStopLookbackUp, manualMoveStopLookbackDown;
		private Button beTriggerUp, beTriggerDown, beOffsetUp, beOffsetDown;
        private Button autoBtn, longBtn, shortBtn, quickLongBtn, quickShortBtn;
        private Button add1Btn, close1Btn, BEBtn, TSBtn, moveTSBtn, moveToBEBtn;
        private Button moveTS50PctBtn, closeBtn, panicBtn, donateBtn, errorResetBtn;		
		private Button trendBotsBtn, rangeBotsBtn, breakoutBotsBtn;
		private Button regimeFilterOnBtn, regimeFilterOffBtn;
		private Button staticModeBtn, dynamicModeBtn;
		private Button autoSizeBtn, scaleInBtn;
        private bool panelActive;
		#endregion
		
		#region UI Control Name Constants
		private const string TrendBotsButton = "TrendBotsBtn";
		private const string RangeBotsButton = "RangeBotsBtn";
		private const string BreakoutBotsButton = "BreakoutBotsBtn";
		private const string RegimeFilterOnButton = "RegimeFilterOnBtn";
		private const string RegimeFilterOffButton = "RegimeFilterOffBtn";
		private const string StaticModeButton = "StaticModeBtn";
		private const string DynamicModeButton = "DynamicModeBtn";
		private const string AutoSizeButton = "AutoSizeBtn";
		private const string ScaleInButton = "ScaleInBtn";
		private const string ORDER_TYPE_SELECTOR_NAME = "orderTypeSelector";
		private const string PT_SELECTOR_NAME = "ptSelector";
		private const string STOP_SELECTOR_NAME = "stopSelector";
		private const string BE_TRIGGER_MODE_SELECTOR_NAME = "beTriggerModeSelector";
		private const string AutoButton = "AutoBtn";
		private const string LongButton = "LongBtn";
		private const string ShortButton = "ShortBtn";
		private const string QuickLongButton = "QuickLongBtn";
		private const string QuickShortButton = "QuickShortBtn";
		private const string AddOneButton = "Add1Btn";
		private const string CloseOneButton = "ClosePartialBtn";
		private const string BEButton = "BEBtn";
		private const string TSButton = "TSBtn";
		private const string MoveTSButton = "MoveTSBtn";
		private const string MoveTS50PctButton = "MoveTS50PctBtn";
		private const string MoveToBeButton = "MoveToBeBtn";
		private const string CloseButton = "CloseBtn";
		private const string PanicButton = "PanicBtn";		
		private const string DonateButton = "DonateBtn";
		private const string ErrorResetButton = "ErrorResetBtn";
		#endregion
		
		#region UI Creation and Management
		
		protected void CreateWPFControls()
	    {
	      chartWindow = Window.GetWindow(ChartControl.Parent) as Chart;
	      if (chartWindow == null) return;
		  
	      ChartTrader chartTrader = chartWindow.FindFirst("ChartWindowChartTraderControl") as ChartTrader;
	      if (chartTrader == null || chartTrader.Content == null)
	      {
	          Print("KCUserInterface UI Error: Chart Trader is not enabled for this chart. Please enable Chart Trader from the chart properties to see the custom UI panel.");
	          return;
	      }
	      
	      chartTraderGrid = chartTrader.Content as Grid;
	      if (chartTraderGrid == null)
	      {
	          Print("KCUserInterface UI Error: Could not find the main grid within Chart Trader. UI cannot be created.");
	          return;
	      }
		  
	      InitializeButtonDefinitions();
	      CreateAllControls();
		  UpdateManualAutoButtonStates();
		  UpdateRegimeFilterButtonStates();
		  UpdateManagementModeButtonStates();
	      InitializeUIGrid();
	
		  panelScrollViewer = new ScrollViewer()
			{
				Content = lowerButtonsGrid,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
				MaxHeight = 500 
			};
	
	      InsertCustomPanel();
	      chartWindow.MainTabControl.SelectionChanged += TabChangedHandler;
	    }

		private void CreateAllControls()
		{						
			Style basicBtnStyle	= Application.Current.FindResource("BasicEntryButton") as Style;
			
			accountSelector = new ComboBox { Margin = new Thickness(2), Height = 28 };
			quantitySelector = new QuantityUpDown { Margin = new Thickness(2), Height = 28, VerticalAlignment = VerticalAlignment.Center };
			
			accountSelector.ItemsSource = Account.All.ToList();
			accountSelector.DisplayMemberPath = "Name";
			accountSelector.SelectedItem = Account.All.FirstOrDefault(a => a.Name == Account.Name);
			accountSelector.SelectionChanged += OnAccountSelectorChanged;
			
			quantitySelector.Value = this.Contracts;
			quantitySelector.ValueChanged += OnQuantitySelectorChanged;

			orderTypeSelector = new ComboBox { Name = ORDER_TYPE_SELECTOR_NAME, Margin = new Thickness(2), Height = 28 };
			orderTypeSelector.ItemsSource = Enum.GetValues(typeof(OrderType)).Cast<OrderType>().Where(ot => ot == OrderType.Market || ot == OrderType.Limit);
			orderTypeSelector.SelectedItem = OrderType;
			orderTypeSelector.SelectionChanged += OnOrderTypeChanged;
			
			profitTargetSelector = new ComboBox { Name = PT_SELECTOR_NAME, Margin = new Thickness(2), Height = 28 };
			profitTargetSelector.ItemsSource = Enum.GetValues(typeof(ProfitTargetType));
			profitTargetSelector.SelectedItem = PTType;
			profitTargetSelector.SelectionChanged += OnProfitTargetSelectorChanged;
			
			stopTypeSelector = new ComboBox { Name = STOP_SELECTOR_NAME, Margin = new Thickness(2), Height = 28 };
			stopTypeSelector.ItemsSource = Enum.GetValues(typeof(StopManagementType));
			stopTypeSelector.SelectedItem = StopType;
			stopTypeSelector.SelectionChanged += OnStopTypeSelectorChanged;

			beTriggerModeSelector = new ComboBox { Name = BE_TRIGGER_MODE_SELECTOR_NAME, Margin = new Thickness(2), Height = 28 };
			beTriggerModeSelector.ItemsSource = Enum.GetValues(typeof(BETriggerMode));
			beTriggerModeSelector.SelectedItem = BreakevenTriggerMode;
			beTriggerModeSelector.SelectionChanged += OnBETriggerModeChanged;

			CreateNumericInput(nameof(initialStopTextBox), ref initialStopTextBox, ref initialStopUp, ref initialStopDown, InitialStop.ToString());
			CreateNumericInput(nameof(profitTargetTextBox), ref profitTargetTextBox, ref profitTargetUp, ref profitTargetDown, ProfitTarget.ToString());
			CreateNumericInput(nameof(limitOffsetTextBox), ref limitOffsetTextBox, ref limitOffsetUp, ref limitOffsetDown, LimitOffset.ToString());
			CreateNumericInput(nameof(rrRatioTextBox), ref rrRatioTextBox, ref rrRatioUp, ref rrRatioDown, RiskRewardRatio.ToString("F1"));
			CreateNumericInput(nameof(trailLookbackTextBox), ref trailLookbackTextBox, ref trailLookbackUp, ref trailLookbackDown, TrailBarsLookback.ToString());
			CreateNumericInput(nameof(closePartialTextBox), ref closePartialTextBox, ref closePartialUp, ref closePartialDown, "1");
			CreateNumericInput(nameof(manualMoveStopLookbackTextBox), ref manualMoveStopLookbackTextBox, ref manualMoveStopLookbackUp, ref manualMoveStopLookbackDown, ManualMoveStopLookback.ToString());
			CreateNumericInput(nameof(beTriggerTextBox), ref beTriggerTextBox, ref beTriggerUp, ref beTriggerDown, BETriggerTicks.ToString());
			CreateNumericInput(nameof(beOffsetTextBox), ref beOffsetTextBox, ref beOffsetUp, ref beOffsetDown, BE_Offset.ToString());

			autoBtn = CreateButton(AutoButton, basicBtnStyle);
			longBtn = CreateButton(LongButton, basicBtnStyle); shortBtn = CreateButton(ShortButton, basicBtnStyle);
			quickLongBtn = CreateButton(QuickLongButton, basicBtnStyle); quickShortBtn = CreateButton(QuickShortButton, basicBtnStyle);
			BEBtn = CreateButton(BEButton, basicBtnStyle); TSBtn = CreateButton(TSButton, basicBtnStyle);
			moveTSBtn = CreateButton(MoveTSButton, basicBtnStyle); moveTS50PctBtn = CreateButton(MoveTS50PctButton, basicBtnStyle);
			moveToBEBtn = CreateButton(MoveToBeButton, basicBtnStyle); add1Btn = CreateButton(AddOneButton, basicBtnStyle);
			close1Btn = CreateButton(CloseOneButton, basicBtnStyle); closeBtn = CreateButton(CloseButton, basicBtnStyle);
			panicBtn = CreateButton(PanicButton, basicBtnStyle); donateBtn = CreateButton(DonateButton, basicBtnStyle);
			errorResetBtn = CreateButton(ErrorResetButton, basicBtnStyle);			
			
			trendBotsBtn = CreateButton(TrendBotsButton, basicBtnStyle);
			rangeBotsBtn = CreateButton(RangeBotsButton, basicBtnStyle);
			breakoutBotsBtn = CreateButton(BreakoutBotsButton, basicBtnStyle);
			
			regimeFilterOnBtn = CreateButton(RegimeFilterOnButton, basicBtnStyle);
			regimeFilterOffBtn = CreateButton(RegimeFilterOffButton, basicBtnStyle);

			staticModeBtn = CreateButton(StaticModeButton, basicBtnStyle);
			dynamicModeBtn = CreateButton(DynamicModeButton, basicBtnStyle);
			
			autoSizeBtn = CreateButton(AutoSizeButton, basicBtnStyle);
			scaleInBtn = CreateButton(ScaleInButton, basicBtnStyle);
		}

	    private Button CreateButton(string name, Style style)
	    {
	      var def = buttonDefinitions.FirstOrDefault(b => b.Name == name);
	      if (def == null) return null;
	      var btn = new Button { Name = name, Content = def.Content, Height = 28, Margin = new Thickness(2), Style = style, IsEnabled = true, ToolTip = def.ToolTip, HorizontalAlignment = HorizontalAlignment.Stretch };
	      def.InitialDecoration?.Invoke(this, btn);
	      btn.Click += OnButtonClick;
	      return btn;
	    }
		
		private void CreateNumericInput(string name, ref TextBox textBox, ref Button upButton, ref Button downButton, string initialValue)
		{
			textBox = new TextBox
			{
				Name = name, Text = initialValue, Height = 28, Width = 40,
				VerticalContentAlignment = VerticalAlignment.Center, HorizontalContentAlignment = HorizontalAlignment.Center,
				BorderThickness = new Thickness(1), BorderBrush = Brushes.Gray,
				Background = Brushes.White, Foreground = Brushes.Black, FontWeight = FontWeights.Bold
			};
			textBox.PreviewKeyDown += OnNumericTextBoxPreviewKeyDown;
			textBox.LostFocus += OnNumericTextBoxLostFocus;
			
			upButton = new Button 
			{
				Name = $"{name}Up", Content = "  +  ", Width = 28, Height = 28, 
				Background = Brushes.DarkGreen, Foreground = Brushes.White, FontWeight = FontWeights.Bold,
				FontSize = 20, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center
			};
			upButton.Click += OnNumericUpDownClick;
			
			downButton = new Button 
			{ 
				Name = $"{name}Down", Content = "  -  ", Width = 28, Height = 28, 
				Background = Brushes.DarkRed, Foreground = Brushes.White, FontWeight = FontWeights.Bold,
				FontSize = 20, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
			};
			downButton.Click += OnNumericUpDownClick;
		}
		
		private void InitializeUIGrid()
		{
		    lowerButtonsGrid = new Grid { Margin = new Thickness(2, 0, 2, 0) };
		    for (int i = 0; i < 6; i++)
		        lowerButtonsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		
		    // --- Section 1: Trade Execution (Expander) ---
		    var tradeExecExpander = new Expander { Header = "Trade Execution", IsExpanded = true, FontWeight = FontWeights.Bold };
		    var tradeExecGrid = new Grid();
		    tradeExecGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		    tradeExecGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		    AddLabelAndControl(tradeExecGrid, "Account:", accountSelector, 0);
		    AddLabelAndControl(tradeExecGrid, "Quantity:", quantitySelector, 1);
		    tradeExecExpander.Content = tradeExecGrid;
		    Grid.SetRow(tradeExecExpander, 0);
		    lowerButtonsGrid.Children.Add(tradeExecExpander);
		    
		    // --- Section 2: Core Parameters (Expander) ---
		    var coreParamsExpander = new Expander { Header = "Core Parameters", IsExpanded = false, FontWeight = FontWeights.Bold };
		    var coreParamsGrid = new Grid();
		    coreParamsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		    coreParamsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			AddLabelAndControl(coreParamsGrid, "Order Type:", orderTypeSelector, 0);
		    AddLabelAndControl(coreParamsGrid, "Limit Offset (Ticks):", CreateNumericInputGrid(limitOffsetDown, limitOffsetTextBox, limitOffsetUp), 1);
		    AddLabelAndControl(coreParamsGrid, "Stop Type:", stopTypeSelector, 2);
		    AddLabelAndControl(coreParamsGrid, "Target Type:", profitTargetSelector, 3);
			AddLabelAndControl(coreParamsGrid, "BE Trigger Mode:", beTriggerModeSelector, 4);
			beTriggerLabel = AddLabelAndControl(coreParamsGrid, "BE Trigger (Ticks):", CreateNumericInputGrid(beTriggerDown, beTriggerTextBox, beTriggerUp), 5);
		    AddLabelAndControl(coreParamsGrid, "BE Offset (Ticks):", CreateNumericInputGrid(beOffsetDown, beOffsetTextBox, beOffsetUp), 6);
		    AddLabelAndControl(coreParamsGrid, "Initial Stop (Ticks):", CreateNumericInputGrid(initialStopDown, initialStopTextBox, initialStopUp), 7);
		    AddLabelAndControl(coreParamsGrid, "Profit Target (Ticks):", CreateNumericInputGrid(profitTargetDown, profitTargetTextBox, profitTargetUp), 8);
		    AddLabelAndControl(coreParamsGrid, "R:R Ratio:", CreateNumericInputGrid(rrRatioDown, rrRatioTextBox, rrRatioUp), 9);
		    AddLabelAndControl(coreParamsGrid, "Trail Lookback:", CreateNumericInputGrid(trailLookbackDown, trailLookbackTextBox, trailLookbackUp), 10);
		    AddLabelAndControl(coreParamsGrid, "Move Stop Lookback:", CreateNumericInputGrid(manualMoveStopLookbackDown, manualMoveStopLookbackTextBox, manualMoveStopLookbackUp), 11);		  
		    AddLabelAndControl(coreParamsGrid, "Close Quantity:", CreateNumericInputGrid(closePartialDown, closePartialTextBox, closePartialUp), 12);
		    coreParamsExpander.Content = coreParamsGrid;
		    Grid.SetRow(coreParamsExpander, 1);
		    lowerButtonsGrid.Children.Add(coreParamsExpander);
		
		    // --- Section 3: Mode & Category Control (Expander) ---
		    var modeControlExpander = new Expander { Header = "Mode & Category Control", IsExpanded = true, FontWeight = FontWeights.Bold };
		    var modeControlGrid = new Grid();
		    for (int i = 0; i < 3; i++) modeControlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		    modeControlGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		    modeControlGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			modeControlGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		    
			// Row 0
			AddButtonToGrid(modeControlGrid, autoBtn, 0, 0);
		    AddButtonToGrid(modeControlGrid, longBtn, 0, 1);
			AddButtonToGrid(modeControlGrid, shortBtn, 0, 2);
		    
			// Row 1
			AddButtonToGrid(modeControlGrid, trendBotsBtn, 1, 0);
			AddButtonToGrid(modeControlGrid, rangeBotsBtn, 1, 1);
		    AddButtonToGrid(modeControlGrid, breakoutBotsBtn, 1, 2);
			
			// Row 2
			var regimeGrid = new Grid();
			regimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			regimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			AddButtonToGrid(regimeGrid, regimeFilterOnBtn, 0, 0);
			AddButtonToGrid(regimeGrid, regimeFilterOffBtn, 0, 1);
			Grid.SetRow(regimeGrid, 2);
			Grid.SetColumn(regimeGrid, 0);
			Grid.SetColumnSpan(regimeGrid, 3);
			modeControlGrid.Children.Add(regimeGrid);
			
		    modeControlExpander.Content = modeControlGrid;
		    Grid.SetRow(modeControlExpander, 2);
		    lowerButtonsGrid.Children.Add(modeControlExpander);
		
		    // --- Section 4: Trade Management Mode ---
		    var tradeMgmtExpander = new Expander { Header = "Trade Management Mode", IsExpanded = true, FontWeight = FontWeights.Bold };
		    var tradeMgmtGrid = new Grid();
		    tradeMgmtGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		    tradeMgmtGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			tradeMgmtGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			tradeMgmtGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		    AddButtonToGrid(tradeMgmtGrid, staticModeBtn, 0, 0);
		    AddButtonToGrid(tradeMgmtGrid, dynamicModeBtn, 0, 1);
			AddButtonToGrid(tradeMgmtGrid, autoSizeBtn, 1, 0);
			AddButtonToGrid(tradeMgmtGrid, scaleInBtn, 1, 1);
		    tradeMgmtExpander.Content = tradeMgmtGrid;
		    Grid.SetRow(tradeMgmtExpander, 3);
		    lowerButtonsGrid.Children.Add(tradeMgmtExpander);
		    
		    // --- Section 5: In-Trade Management (Expander) ---
		    var inTradeExpander = new Expander { Header = "In-Trade Management", IsExpanded = true, FontWeight = FontWeights.Bold };
		    var inTradeGrid = new Grid();
		    for (int i = 0; i < 2; i++) inTradeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		    AddButtonToGrid(inTradeGrid, quickLongBtn, 0, 0); AddButtonToGrid(inTradeGrid, quickShortBtn, 0, 1);
		    AddButtonToGrid(inTradeGrid, add1Btn, 1, 0); AddButtonToGrid(inTradeGrid, close1Btn, 1, 1);
		    AddButtonToGrid(inTradeGrid, moveToBEBtn, 2, 0); AddButtonToGrid(inTradeGrid, moveTS50PctBtn, 2, 1);
		    AddButtonToGrid(inTradeGrid, moveTSBtn, 3, 0);
		    AddButtonToGrid(inTradeGrid, closeBtn, 3, 1);
		    inTradeExpander.Content = inTradeGrid;
		    Grid.SetRow(inTradeExpander, 4);
		    lowerButtonsGrid.Children.Add(inTradeExpander);
		    
		    // --- Section 6: System (Expander) ---
		    var systemExpander = new Expander { Header = "System", IsExpanded = true, FontWeight = FontWeights.Bold };
		    var systemGrid = new Grid();
		    for (int i = 0; i < 3; i++) systemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		    AddButtonToGrid(systemGrid, panicBtn, 0, 0);
		    AddButtonToGrid(systemGrid, errorResetBtn, 0, 1);
		    AddButtonToGrid(systemGrid, donateBtn, 0, 2);
		    systemExpander.Content = systemGrid;
		    Grid.SetRow(systemExpander, 5);
		    lowerButtonsGrid.Children.Add(systemExpander);

			UpdateBETriggerUI(); // Set initial state of the BE Trigger label
		}

		private Grid CreateNumericInputGrid(Button down, TextBox text, Button up)
		{
			Grid grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
			
			Grid.SetColumn(down, 0); grid.Children.Add(down);
			Grid.SetColumn(text, 1); grid.Children.Add(text);
			Grid.SetColumn(up, 2); grid.Children.Add(up);
			return grid;
		}
		
		private TextBlock AddLabelAndControl(Grid grid, string labelText, UIElement control, int row)
		{
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			var textBlock = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2,2,5,2) };
			Grid.SetRow(textBlock, row); Grid.SetColumn(textBlock, 0);
			Grid.SetRow(control, row); Grid.SetColumn(control, 1);
			grid.Children.Add(textBlock);
			grid.Children.Add(control);
			return textBlock;
		}
		
		private void AddButtonToGrid(Grid grid, Button button, int row, int column)
		{
			if(grid.RowDefinitions.Count <= row) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			Grid.SetRow(button, row);
			Grid.SetColumn(button, column);
			grid.Children.Add(button);
		}

		#endregion
		
		#region UI Event Handlers and State Management
		
		private void OnAccountSelectorChanged(object sender, SelectionChangedEventArgs e)
		{
			if (accountSelector.SelectedItem is Account selectedAccount && !IsInHitTest)
			{
				Account = selectedAccount;
			}
		}

		private void OnQuantitySelectorChanged(object sender, System.Windows.RoutedEventArgs e)
		{
			if(quantitySelector.Value > 0 && !IsInHitTest)
			{
				Contracts = quantitySelector.Value;
			}
		}
	
		#region Trade Management Mode UI Helpers
		
		public void SetManagementMode(TradeManagementMode newMode)
		{
		    if (ManagementMode == newMode) return;
		    ManagementMode = newMode;
		    if (State >= State.Historical && ChartControl != null)
		    {
		        ChartControl.Dispatcher.InvokeAsync(() => 
		        {
		            UpdateManagementModeButtonStates();
		            ForceRefresh();
		        });
		    }
		    Print($"{Time[0]}: Trade Management Mode switched to {newMode}.");
		}
		
		public void UpdateManagementModeButtonStates()
		{
		    if (staticModeBtn == null || dynamicModeBtn == null) return;

		    DecorateButton(staticModeBtn, 
		        ManagementMode == TradeManagementMode.Static ? ButtonState.Enabled : ButtonState.Disabled, "Static SL / TP", "Static SL / TP");
		
		    DecorateButton(dynamicModeBtn, 
		        ManagementMode == TradeManagementMode.Dynamic ? ButtonState.Enabled : ButtonState.Disabled, "Dynamic SL / TP", "Dynamic SL / TP");
		}
		
		#endregion

		private void ToggleRegimeFilter()
		{
			EnableAutoRegimeDetection = !EnableAutoRegimeDetection;
			UpdateRegimeFilterButtonStates();
			ForceRefresh();
		}

		private void OnNumericUpDownClick(object sender, RoutedEventArgs e)
		{
			Button btn = sender as Button;
			if (btn == null) return;
			
			if (btn.Name == $"{nameof(limitOffsetTextBox)}Up") UpdateNumericValue(limitOffsetTextBox, 1);
			else if (btn.Name == $"{nameof(limitOffsetTextBox)}Down") UpdateNumericValue(limitOffsetTextBox, -1);
			else if (btn.Name == $"{nameof(rrRatioTextBox)}Up") UpdateNumericValue(rrRatioTextBox, 0.1, "F1");
			else if (btn.Name == $"{nameof(rrRatioTextBox)}Down") UpdateNumericValue(rrRatioTextBox, -0.1, "F1");
			else if (btn.Name == $"{nameof(trailLookbackTextBox)}Up") UpdateNumericValue(trailLookbackTextBox, 1);
			else if (btn.Name == $"{nameof(trailLookbackTextBox)}Down") UpdateNumericValue(trailLookbackTextBox, -1);
			else if (btn.Name == $"{nameof(initialStopTextBox)}Up") UpdateNumericValue(initialStopTextBox, 1);
			else if (btn.Name == $"{nameof(initialStopTextBox)}Down") UpdateNumericValue(initialStopTextBox, -1);
			else if (btn.Name == $"{nameof(profitTargetTextBox)}Up") UpdateNumericValue(profitTargetTextBox, 1);
			else if (btn.Name == $"{nameof(profitTargetTextBox)}Down") UpdateNumericValue(profitTargetTextBox, -1);
			else if (btn.Name == $"{nameof(closePartialTextBox)}Up") UpdateNumericValue(closePartialTextBox, 1);
			else if (btn.Name == $"{nameof(closePartialTextBox)}Down") UpdateNumericValue(closePartialTextBox, -1);
			else if (btn.Name == $"{nameof(manualMoveStopLookbackTextBox)}Up") UpdateNumericValue(manualMoveStopLookbackTextBox, 1);
			else if (btn.Name == $"{nameof(manualMoveStopLookbackTextBox)}Down") UpdateNumericValue(manualMoveStopLookbackTextBox, -1);
			else if (btn.Name == $"{nameof(beTriggerTextBox)}Up") UpdateNumericValue(beTriggerTextBox, 1);
			else if (btn.Name == $"{nameof(beTriggerTextBox)}Down") UpdateNumericValue(beTriggerTextBox, -1);
			else if (btn.Name == $"{nameof(beOffsetTextBox)}Up") UpdateNumericValue(beOffsetTextBox, 1);
			else if (btn.Name == $"{nameof(beOffsetTextBox)}Down") UpdateNumericValue(beOffsetTextBox, -1);
		}

		private void UpdateNumericValue(TextBox textBox, double change, string format = "F0")
		{
			if(double.TryParse(textBox.Text, out double currentValue))
			{
				double newValue = currentValue + change;
				if (newValue < 0 && textBox != rrRatioTextBox) newValue = 0;
				textBox.Text = newValue.ToString(format);
				ParseTextBoxAndUpdateProperty(textBox);
			}
		}
		
		private void OnNumericTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
		{
			Key[] allowedKeys = 
			{
				Key.Back, Key.Delete, Key.Tab,
				Key.Left, Key.Right, Key.Home, Key.End,
				Key.Enter 
			};

			if (allowedKeys.Contains(e.Key))
			{
				if (e.Key == Key.Enter)
				{
					ParseTextBoxAndUpdateProperty(sender as TextBox);
					Keyboard.ClearFocus();
					e.Handled = true;
				}
				else
				{
					e.Handled = false;
				}
				return;
			}

			bool isDecimalKey = (e.Key == Key.Decimal || e.Key == Key.OemPeriod);
			if (isDecimalKey && (sender as TextBox)?.Name == nameof(rrRatioTextBox))
			{
				e.Handled = false;
				return;
			}

			bool isNumber = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
			
			e.Handled = !isNumber;
		}
		
		private void OnNumericTextBoxLostFocus(object sender, RoutedEventArgs e)
		{
			ParseTextBoxAndUpdateProperty(sender as TextBox);
		}

		private void ParseTextBoxAndUpdateProperty(TextBox textBox)
		{
			if (textBox == null) return;
			if (double.TryParse(textBox.Text, out double parsedValue))
			{
				if (textBox.Name == nameof(limitOffsetTextBox)) LimitOffset = (int)parsedValue;
				else if (textBox.Name == nameof(rrRatioTextBox)) RiskRewardRatio = parsedValue;
				else if (textBox.Name == nameof(trailLookbackTextBox)) TrailBarsLookback = (int)parsedValue;
				else if (textBox.Name == nameof(initialStopTextBox)) InitialStop = (int)parsedValue; 
				else if (textBox.Name == nameof(profitTargetTextBox)) ProfitTarget = parsedValue;
				else if (textBox.Name == nameof(closePartialTextBox)) CloseQty = (int)parsedValue;
				else if (textBox.Name == nameof(manualMoveStopLookbackTextBox)) ManualMoveStopLookback = (int)parsedValue;
				else if (textBox.Name == nameof(beTriggerTextBox)) BETriggerTicks = (int)parsedValue;
				else if (textBox.Name == nameof(beOffsetTextBox)) BE_Offset = (int)parsedValue;
			}
			else
			{
				if (textBox.Name == nameof(limitOffsetTextBox)) textBox.Text = LimitOffset.ToString();
				else if (textBox.Name == nameof(rrRatioTextBox)) textBox.Text = RiskRewardRatio.ToString("F1");
				else if (textBox.Name == nameof(trailLookbackTextBox)) textBox.Text = TrailBarsLookback.ToString();
				else if (textBox.Name == nameof(initialStopTextBox)) textBox.Text = InitialStop.ToString();
				else if (textBox.Name == nameof(profitTargetTextBox)) textBox.Text = ProfitTarget.ToString();
				else if (textBox.Name == nameof(closePartialTextBox)) textBox.Text = CloseQty.ToString();
				else if (textBox.Name == nameof(manualMoveStopLookbackTextBox)) textBox.Text = ManualMoveStopLookback.ToString();
				else if (textBox.Name == nameof(beTriggerTextBox)) textBox.Text = BETriggerTicks.ToString();
				else if (textBox.Name == nameof(beOffsetTextBox)) textBox.Text = BE_Offset.ToString();
			}
		}
		
		private void OnOrderTypeChanged(object sender, SelectionChangedEventArgs e)
		{
			if (orderTypeSelector.SelectedItem == null || IsInHitTest) return;
			OrderType = (OrderType)orderTypeSelector.SelectedItem;
		}
		
		private void OnProfitTargetSelectorChanged(object sender, SelectionChangedEventArgs e)
		{
			if (profitTargetSelector.SelectedItem == null || IsInHitTest) return;
			PTType = (ProfitTargetType)profitTargetSelector.SelectedItem;
			ForceRefresh();
		}

		private void OnStopTypeSelectorChanged(object sender, SelectionChangedEventArgs e)
		{
			if (stopTypeSelector.SelectedItem == null || IsInHitTest) return;
			StopType = (StopManagementType)stopTypeSelector.SelectedItem;
			ForceRefresh();
		}

		private void OnBETriggerModeChanged(object sender, SelectionChangedEventArgs e)
		{
			if (beTriggerModeSelector.SelectedItem == null || IsInHitTest) return;
			BreakevenTriggerMode = (BETriggerMode)beTriggerModeSelector.SelectedItem;
			UpdateBETriggerUI();
			ForceRefresh();
		}

		private void UpdateBETriggerUI()
		{
			if (beTriggerLabel == null || beTriggerTextBox == null) return;
		
			if (BreakevenTriggerMode == BETriggerMode.FixedTicks)
			{
				beTriggerLabel.Text = "BE Trigger (Ticks):";
				beTriggerTextBox.ToolTip = "The number of ticks in profit required to trigger the auto-breakeven.";
			}
			else // ProfitTargetPercentage
			{
				beTriggerLabel.Text = "BE Trigger (% of PT):";
				beTriggerTextBox.ToolTip = "The percentage of the profit target that must be reached to trigger auto-breakeven (e.g., 50).";
			}
		}

		protected void OnButtonClick(object sender, RoutedEventArgs rea)
		{
			if (!(sender is Button button)) return;
			buttonDefinitions.FirstOrDefault(b => b.Name == button.Name)?.ClickAction?.Invoke(this);
		}

		private void TabChangedHandler(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count <= 0) return;
			
			if (TabSelected()) InsertCustomPanel();
			else RemoveWPFControls();
		}		
		
		private bool TabSelected()
		{
			if (chartWindow == null) return false;
			foreach (TabItem tab in chartWindow.MainTabControl.Items)
				if ((tab.Content as ChartTab)?.ChartControl == ChartControl && tab == chartWindow.MainTabControl.SelectedItem) return true;
			return false;
		}

		private void InsertCustomPanel()
		{
		    if (panelActive || chartTraderGrid == null) return;
		    
			if (chartTraderGrid.Children.Contains(panelScrollViewer))
				return;
			
		    chartTraderGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
		    Grid.SetRow(panelScrollViewer, chartTraderGrid.RowDefinitions.Count - 1);
		    
		    if (chartTraderGrid.ColumnDefinitions.Count > 0)
		        Grid.SetColumnSpan(panelScrollViewer, chartTraderGrid.ColumnDefinitions.Count);
		    
		    chartTraderGrid.Children.Add(panelScrollViewer);
		    
		    panelActive = true;
		}

		private void RemoveWPFControls()
		{
		    if (!panelActive || chartTraderGrid == null) return;
		    
		    if (panelScrollViewer != null)
		        chartTraderGrid.Children.Remove(panelScrollViewer);
		    
		    if (chartTraderGrid.RowDefinitions.Count > 0)
		        chartTraderGrid.RowDefinitions.RemoveAt(chartTraderGrid.RowDefinitions.Count - 1);
		    
		    panelActive = false;
		}
		
		protected void DisposeWPFControls() 
		{
			if (chartWindow != null) chartWindow.MainTabControl.SelectionChanged -= TabChangedHandler;
			
			if (accountSelector != null) accountSelector.SelectionChanged -= OnAccountSelectorChanged;
			if (quantitySelector != null) quantitySelector.ValueChanged -= OnQuantitySelectorChanged;
			
			if (orderTypeSelector != null) orderTypeSelector.SelectionChanged -= OnOrderTypeChanged;
			if (profitTargetSelector != null) profitTargetSelector.SelectionChanged -= OnProfitTargetSelectorChanged;
			if (stopTypeSelector != null) stopTypeSelector.SelectionChanged -= OnStopTypeSelectorChanged;
			if (beTriggerModeSelector != null) beTriggerModeSelector.SelectionChanged -= OnBETriggerModeChanged;
			
			if (limitOffsetTextBox != null) { limitOffsetTextBox.PreviewKeyDown -= OnNumericTextBoxPreviewKeyDown; limitOffsetTextBox.LostFocus -= OnNumericTextBoxLostFocus; }
			if (rrRatioTextBox != null) { rrRatioTextBox.PreviewKeyDown -= OnNumericTextBoxPreviewKeyDown; rrRatioTextBox.LostFocus -= OnNumericTextBoxLostFocus; }
			if (trailLookbackTextBox != null) { trailLookbackTextBox.PreviewKeyDown -= OnNumericTextBoxPreviewKeyDown; trailLookbackTextBox.LostFocus -= OnNumericTextBoxLostFocus; }
			if (closePartialTextBox != null) { closePartialTextBox.PreviewKeyDown -= OnNumericTextBoxPreviewKeyDown; closePartialTextBox.LostFocus -= OnNumericTextBoxLostFocus; }
			if (initialStopTextBox != null) { initialStopTextBox.PreviewKeyDown -= OnNumericTextBoxPreviewKeyDown; initialStopTextBox.LostFocus -= OnNumericTextBoxLostFocus; }
			if (profitTargetTextBox != null) { profitTargetTextBox.PreviewKeyDown -= OnNumericTextBoxPreviewKeyDown; profitTargetTextBox.LostFocus -= OnNumericTextBoxLostFocus; }
			if (beTriggerTextBox != null) { beTriggerTextBox.PreviewKeyDown -= OnNumericTextBoxPreviewKeyDown; beTriggerTextBox.LostFocus -= OnNumericTextBoxLostFocus; }
			if (beOffsetTextBox != null) { beOffsetTextBox.PreviewKeyDown -= OnNumericTextBoxPreviewKeyDown; beOffsetTextBox.LostFocus -= OnNumericTextBoxLostFocus; }

			if (limitOffsetUp != null) limitOffsetUp.Click -= OnNumericUpDownClick;
			if (limitOffsetDown != null) limitOffsetDown.Click -= OnNumericUpDownClick;
			if (rrRatioUp != null) rrRatioUp.Click -= OnNumericUpDownClick;
			if (rrRatioDown != null) rrRatioDown.Click -= OnNumericUpDownClick;
			if (trailLookbackUp != null) trailLookbackUp.Click -= OnNumericUpDownClick;
			if (trailLookbackDown != null) trailLookbackDown.Click -= OnNumericUpDownClick;
			if (initialStopUp != null) initialStopUp.Click -= OnNumericUpDownClick;
			if (initialStopDown != null) initialStopDown.Click -= OnNumericUpDownClick;
			if (profitTargetUp != null) profitTargetUp.Click -= OnNumericUpDownClick;
			if (profitTargetDown != null) profitTargetDown.Click -= OnNumericUpDownClick;
			if (closePartialUp != null) closePartialUp.Click -= OnNumericUpDownClick;
			if (closePartialDown != null) closePartialDown.Click -= OnNumericUpDownClick;
			if (manualMoveStopLookbackTextBox != null) { manualMoveStopLookbackTextBox.PreviewKeyDown -= OnNumericTextBoxPreviewKeyDown; manualMoveStopLookbackTextBox.LostFocus -= OnNumericTextBoxLostFocus; }
			if (manualMoveStopLookbackUp != null) manualMoveStopLookbackUp.Click -= OnNumericUpDownClick;
			if (manualMoveStopLookbackDown != null) manualMoveStopLookbackDown.Click -= OnNumericUpDownClick;
			if (beTriggerUp != null) beTriggerUp.Click -= OnNumericUpDownClick;
			if (beTriggerDown != null) beTriggerDown.Click -= OnNumericUpDownClick;
			if (beOffsetUp != null) beOffsetUp.Click -= OnNumericUpDownClick;
			if (beOffsetDown != null) beOffsetDown.Click -= OnNumericUpDownClick;
			
			Action<Button> unsub = (b) => { if (b != null) b.Click -= OnButtonClick; };
			unsub(autoBtn); unsub(longBtn); unsub(shortBtn); unsub(quickLongBtn); unsub(quickShortBtn);
			unsub(add1Btn); unsub(close1Btn); unsub(BEBtn); unsub(TSBtn); unsub(moveTSBtn); unsub(moveTS50PctBtn);
			unsub(moveToBEBtn); unsub(closeBtn); unsub(panicBtn); unsub(donateBtn); unsub(errorResetBtn);
			unsub(regimeFilterOnBtn); unsub(regimeFilterOffBtn);
			
			RemoveWPFControls();
		}
		#endregion

		#region Button Definitions and Decorations
		private List<ButtonDefinition> buttonDefinitions;
		private class ButtonDefinition
		{
			public string Name { get; set; } public string Content { get; set; } public string ToolTip { get; set; }
			public Action<KCAlgoBase, Button> InitialDecoration { get; set; }
			public Action<KCAlgoBase> ClickAction { get; set; }
		}

		private void InitializeButtonDefinitions()
		{
			buttonDefinitions = new List<ButtonDefinition>
			{
				new ButtonDefinition { Name = AutoButton, Content = "AUTO", ToolTip = "Toggle Auto Trading",
					ClickAction = (s) => s.SetTradingMode(s.CurrentMode == TradingMode.Auto ? TradingMode.Disabled : TradingMode.Auto) },
				new ButtonDefinition { Name = LongButton, Content = "LONG", ToolTip = "Toggle Auto Longs",
					InitialDecoration = (s, b) => DecorateButton(b, s.isLongEnabled ? ButtonState.Enabled : ButtonState.Disabled, "LONG", "LONG"),
					ClickAction = (s) => { s.isLongEnabled = !s.isLongEnabled; DecorateButton(s.longBtn, s.isLongEnabled ? ButtonState.Enabled : ButtonState.Disabled, "LONG", "LONG"); } },
				new ButtonDefinition { Name = ShortButton, Content = "SHORT", ToolTip = "Toggle Auto Shorts",
					InitialDecoration = (s, b) => DecorateButton(b, s.isShortEnabled ? ButtonState.Enabled : ButtonState.Disabled, "SHORT", "SHORT"),
					ClickAction = (s) => { s.isShortEnabled = !s.isShortEnabled; DecorateButton(s.shortBtn, s.isShortEnabled ? ButtonState.Enabled : ButtonState.Disabled, "SHORT", "SHORT"); } },
				new ButtonDefinition { Name = QuickLongButton, Content = "Buy", ToolTip = "Manual Buy",
					InitialDecoration = (s, b) => DecorateButton(b, ButtonState.Neutral, "Buy", null, Brushes.White, Brushes.DarkGreen),
					ClickAction = (s) => { if (s.isFlat) s._isBuyRequested = true; } },
				new ButtonDefinition { Name = QuickShortButton, Content = "Sell", ToolTip = "Manual Sell",
					InitialDecoration = (s, b) => DecorateButton(b, ButtonState.Neutral, "Sell", null, Brushes.White, Brushes.DarkRed),
					ClickAction = (s) => { if (s.isFlat) s._isSellRequested = true; } },
				new ButtonDefinition { Name = AddOneButton, Content = "Add 1", ToolTip = "Add 1 contract",
					InitialDecoration = (s, b) => DecorateButton(b, ButtonState.Neutral, "Add 1", null, Brushes.White, Brushes.DarkGreen),
					ClickAction = (s) => s._isAddOneRequested = true },
				new ButtonDefinition { Name = CloseOneButton, Content = "Partial Close", ToolTip = "Partial Close contract (FIFO)",
					InitialDecoration = (s, b) => DecorateButton(b, ButtonState.Neutral, "Partial Close", null, Brushes.White, Brushes.DarkRed),
					ClickAction = (s) => s._isCloseOneRequested = true },
				new ButtonDefinition { Name = BEButton, Content = "Auto BE", ToolTip = "Toggle Auto Breakeven",
					InitialDecoration = (s, b) => DecorateButton(b, s.BESetAuto ? ButtonState.Enabled : ButtonState.Disabled, "Auto BE", "OFF"),
					ClickAction = (s) => { s.BESetAuto = !s.BESetAuto; DecorateButton(s.BEBtn, s.BESetAuto ? ButtonState.Enabled : ButtonState.Disabled, "Auto BE", "OFF"); } },
				new ButtonDefinition { Name = TSButton, Content = "Trailstop On", ToolTip = "Toggle Auto Trailing Stop",
					InitialDecoration = (s, b) => DecorateButton(b, s.StopType != StopManagementType.FixedStop ? ButtonState.Enabled : ButtonState.Disabled, "Trailstop On", "OFF"),
					ClickAction = (s) => { s.StopType = (s.StopType == StopManagementType.FixedStop ? StopManagementType.RegularTrail : StopManagementType.FixedStop); DecorateButton(s.TSBtn, s.StopType != StopManagementType.FixedStop ? ButtonState.Enabled : ButtonState.Disabled, "Trailstop On", "Trailstop Off"); s.ForceRefresh(); } },
				new ButtonDefinition { Name = MoveToBeButton, Content = "Breakeven", ToolTip = "Move stop to breakeven",
					InitialDecoration = (s, b) => DecorateButton(b, ButtonState.Neutral, "Breakeven", null, Brushes.Yellow, Brushes.DarkBlue),
					ClickAction = (s) => s._isMoveToBERequested = true },
				new ButtonDefinition { Name = MoveTSButton, Content = "Move Trailstop", ToolTip = "Move stop to swing point",
					InitialDecoration = (s, b) => DecorateButton(b, ButtonState.Neutral, "Move Trailstop", null, Brushes.Yellow, Brushes.DarkBlue),
					ClickAction = (s) => s._isMoveToSwingPointRequested = true },
				new ButtonDefinition { Name = MoveTS50PctButton, Content = "Move TS 50%", ToolTip = "Move stop 50% closer to price",
					InitialDecoration = (s, b) => DecorateButton(b, ButtonState.Neutral, "Move TS 50%", null, Brushes.Yellow, Brushes.DarkBlue),
					ClickAction = (s) => s._isMoveTS50PctRequested = true },
				new ButtonDefinition { Name = CloseButton, Content = "Close All", ToolTip = "Close all strategy positions",
					InitialDecoration = (s, b) => DecorateButton(b, ButtonState.Neutral, "Close All", null, Brushes.White, Brushes.DarkRed),
					ClickAction = (s) => s.CloseAllPositions() },
				new ButtonDefinition { Name = PanicButton, Content = "Panic", ToolTip = "FLATTEN ALL positions for account",
					InitialDecoration = (s, b) => DecorateButton(b, ButtonState.Neutral, "Panic", null, Brushes.Yellow, Brushes.DarkRed),
					ClickAction = (s) => s.FlattenAllPositions() },
				new ButtonDefinition { Name = DonateButton, Content = "Donate", ToolTip = "Support Developer",
					InitialDecoration = (s, b) => DecorateButton(b, ButtonState.Neutral, "Donate", null, Brushes.Yellow, Brushes.DarkBlue),
					ClickAction = (s) => { if(!string.IsNullOrWhiteSpace(s.paypal)) System.Diagnostics.Process.Start(s.paypal); } },
				new ButtonDefinition { Name = ErrorResetButton, Content = "Reset", ToolTip = "Click to reset the 'Order Error' status and resume trading.",
					InitialDecoration = (s, b) => DecorateButton(b, ButtonState.Neutral, "Reset", null, Brushes.White, Brushes.Indigo),
					ClickAction = (s) => s._isErrorResetRequested = true },
				new ButtonDefinition { Name = TrendBotsButton, Content = "Trend", ToolTip = "Toggle Trend Bots",
				    InitialDecoration = (s, b) => DecorateButton(b, s.EnableTrendBots ? ButtonState.Enabled : ButtonState.Disabled, "Trend", "Trend"),
				    ClickAction = (s) => { s.EnableTrendBots = !s.EnableTrendBots; DecorateButton(s.trendBotsBtn, s.EnableTrendBots ? ButtonState.Enabled : ButtonState.Disabled, "Trend", "Trend"); s.ForceRefresh(); } },				
				new ButtonDefinition { Name = RangeBotsButton, Content = "Range", ToolTip = "Toggle Range Bots",
				    InitialDecoration = (s, b) => DecorateButton(b, s.EnableRangeBots ? ButtonState.Enabled : ButtonState.Disabled, "Range", "Range"),
				    ClickAction = (s) => { s.EnableRangeBots = !s.EnableRangeBots; DecorateButton(s.rangeBotsBtn, s.EnableRangeBots ? ButtonState.Enabled : ButtonState.Disabled, "Range", "Range"); s.ForceRefresh(); } },				
				new ButtonDefinition { Name = BreakoutBotsButton, Content = "Breakout", ToolTip = "Toggle Breakout Bots",
				    InitialDecoration = (s, b) => DecorateButton(b, s.EnableBreakoutBots ? ButtonState.Enabled : ButtonState.Disabled, "Breakout", "Breakout"),
				    ClickAction = (s) => { s.EnableBreakoutBots = !s.EnableBreakoutBots; DecorateButton(s.breakoutBotsBtn, s.EnableBreakoutBots ? ButtonState.Enabled : ButtonState.Disabled, "Breakout", "Breakout"); s.ForceRefresh(); } },				
				new ButtonDefinition { Name = RegimeFilterOnButton, Content = "Regime Filter ON", ToolTip = "Toggle signal filtering based on the detected market regime.",
				    ClickAction = (s) => s.ToggleRegimeFilter() },
				new ButtonDefinition { Name = RegimeFilterOffButton, Content = "All Signals ON", ToolTip = "Toggle signal filtering based on the detected market regime.",
				    ClickAction = (s) => s.ToggleRegimeFilter() },
				new ButtonDefinition { Name = StaticModeButton, Content = "Static SL / TP", ToolTip = "Toggle between Static and Dynamic trade management",
		            ClickAction = (s) => s.ToggleManagementMode() },         
		        new ButtonDefinition { Name = DynamicModeButton, Content = "Dynamic SL / TP", ToolTip = "Toggle between Static and Dynamic trade management",
		            ClickAction = (s) => s.ToggleManagementMode() },
				new ButtonDefinition { Name = AutoSizeButton, Content = "Auto Sizing ON", ToolTip = "Toggle Dynamic Position Sizing",
				    InitialDecoration = (s, b) => DecorateButton(b, s.EnableDynamicSizing ? ButtonState.Enabled : ButtonState.Disabled, "Auto Sizing ON", "Auto Sizing OFF"),
				    ClickAction = (s) => { s.EnableDynamicSizing = !s.EnableDynamicSizing; DecorateButton(s.autoSizeBtn, s.EnableDynamicSizing ? ButtonState.Enabled : ButtonState.Disabled, "Auto Sizing ON", "Auto Sizing OFF"); s.ForceRefresh(); } },
				new ButtonDefinition { Name = ScaleInButton, Content = "Scale-In ON", ToolTip = "Toggle Scale-In Execution",
				    InitialDecoration = (s, b) => DecorateButton(b, s.EnableScaleInExecution ? ButtonState.Enabled : ButtonState.Disabled, "Scale-In ON", "Scale-In OFF"),
				    ClickAction = (s) => { s.EnableScaleInExecution = !s.EnableScaleInExecution; DecorateButton(s.scaleInBtn, s.EnableScaleInExecution ? ButtonState.Enabled : ButtonState.Disabled, "Scale-In ON", "Scale-In OFF"); s.ForceRefresh(); } }
			};
		}

		private enum ButtonState { Enabled, Disabled, Neutral }

        public void UpdateManualAutoButtonStates()
		{
		    if (autoBtn == null) return;
		    DecorateButton(autoBtn, CurrentMode == TradingMode.Auto ? ButtonState.Enabled : ButtonState.Disabled, "AUTO", "AUTO");
		    if (CurrentMode == TradingMode.DisabledByChop) { autoBtn.Content = "CHOP OFF"; autoBtn.Background = Brushes.Gray; }
		}

		public void UpdateRegimeFilterButtonStates()
		{
		    if (regimeFilterOnBtn == null || regimeFilterOffBtn == null) return;

			if (EnableAutoRegimeDetection)
			{
				DecorateButton(regimeFilterOnBtn, ButtonState.Enabled, "Regime Filter ON");
				DecorateButton(regimeFilterOffBtn, ButtonState.Disabled, "All Signals OFF"); 
			}
			else
			{
				DecorateButton(regimeFilterOnBtn, ButtonState.Disabled, "Regime Filter OFF");
				DecorateButton(regimeFilterOffBtn, ButtonState.Enabled, "All Signals ON");
			}
		}
		
		private void DecorateButton(Button button, ButtonState state, string contentOn, string contentOff = null, Brush foreground = null, Brush background = null)
		{
		    if (button == null) return;
		
		    switch (state)
		    {
		        case ButtonState.Enabled:
		            button.Content = contentOn;
		            button.Background = background ?? Brushes.DarkGreen;
		            button.Foreground = foreground ?? Brushes.White;
		            break;
		
		        case ButtonState.Disabled:
		            button.Content = contentOff ?? contentOn;
		            button.Background = background ?? Brushes.DarkGray;
		            button.Foreground = foreground ?? Brushes.Black;
		            break;
		
		        case ButtonState.Neutral:
		            button.Content = contentOn;
		            button.Background = background ?? Brushes.Gray;
		            button.Foreground = foreground ?? Brushes.White;
		            break;
		    }
		}
		#endregion
    }
}