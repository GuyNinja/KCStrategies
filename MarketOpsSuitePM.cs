//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// MarketOpsSuitePM.cs - MarketOps Suite with Parameter Management
// Simple overnight levels, daily SMA, and volume analysis with interactive controls
//
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
using NinjaTrader.Core.FloatingPoint;
using System.Windows.Controls;
using System.Windows;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class MarketOpsSuitePM : Indicator
    {
        #region Variables
        // Core functionality
        private SMA dailySma;
        private SMA volumeSma;
        
        // Overnight levels
        private double overnightHigh = double.NaN;
        private double overnightLow = double.NaN;
        private double overnightMidpoint = double.NaN;
        private DateTime currentDate = DateTime.MinValue;
        
        // Interactive controls
        private Grid controlPanel;
        private Button btnOvernightLevels;
        private Button btnDailySma;
        private Button btnVolumeSpikes;
        private Button btnMidpoint;
        private TextBox txtSmaPeriod;
        private TextBox txtVolumeThreshold;
        private TextBox txtVolumePeriod;
        private TextBox txtLineWidth;
        
        // Control states
        private bool showOvernightLevelsEnabled = true;
        private bool showDailySmaEnabled = true;
        private bool showVolumeSpikesEnabled = true;
        private bool showMidpointEnabled = true;
        
        // Dynamic parameters
        private int dynamicSmaPeriod = 200;
        private double dynamicVolumeThreshold = 150.0;
        private int dynamicVolumePeriod = 20;
        private int dynamicLineWidth = 2;
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"MarketOps Suite with Parameter Management - Simple overnight levels, daily SMA, and volume analysis with interactive controls";
                Name = "MarketOpsSuitePM";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;
                
                // Default Parameters
                SmaPeriod = 200;
                VolumeSpikePeriod = 20;
                VolumeSpikeThreshold = 150.0;
                LineWidth = 2;
                
                // Visual Settings (initial states)
                ShowOvernightLevels = true;
                ShowDailySma = true;
                ShowVolumeSpikes = true;
                ShowMidpoint = true;
                
                // Colors
                OvernightLevelColor = Brushes.Orange;
                MidpointColor = Brushes.Yellow;
                SmaColor = Brushes.Blue;
                VolumeSpikeColor = Brushes.Purple;
                
                // Add single plot for SMA
                AddPlot(new Stroke(Brushes.Blue, DashStyleHelper.Solid, 2), PlotStyle.Line, "DailySMA");
            }
            else if (State == State.DataLoaded)
            {
                // Initialize indicators with default values
                dailySma = SMA(Close, SmaPeriod);
                volumeSma = SMA(Volume, VolumeSpikePeriod);
                
                // Set dynamic parameters to property values
                dynamicSmaPeriod = SmaPeriod;
                dynamicVolumeThreshold = VolumeSpikeThreshold;
                dynamicVolumePeriod = VolumeSpikePeriod;
                dynamicLineWidth = LineWidth;
                
                // Set control states
                showOvernightLevelsEnabled = ShowOvernightLevels;
                showDailySmaEnabled = ShowDailySma;
                showVolumeSpikesEnabled = ShowVolumeSpikes;
                showMidpointEnabled = ShowMidpoint;
            }
            else if (State == State.Historical)
            {
                if (ChartControl != null)
                {
                    CreateControlPanel();
                }
            }
            else if (State == State.Terminated)
            {
                if (controlPanel != null && ChartControl != null)
                {
                    ChartControl.Dispatcher.InvokeAsync(() =>
                    {
                        if (controlPanel.Parent is Panel parent)
                        {
                            parent.Children.Remove(controlPanel);
                        }
                    });
                }
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Math.Max(dynamicSmaPeriod, dynamicVolumePeriod)) return;

            // Update overnight levels
            UpdateOvernightLevels();
            
            // Update SMA plot
            if (showDailySmaEnabled && dailySma != null)
            {
                Values[0][0] = dailySma[0];
            }
            else
            {
                Values[0][0] = double.NaN;
            }
            
            // Check for volume spikes
            if (showVolumeSpikesEnabled)
            {
                CheckVolumeSpikes();
            }
            
            // Draw overnight levels
            if (showOvernightLevelsEnabled)
            {
                DrawOvernightLevels();
            }
        }

        private void CreateControlPanel()
        {
            ChartControl.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    // Create main control panel
                    controlPanel = new Grid
                    {
                        Background = new SolidColorBrush(Color.FromArgb(200, 30, 30, 30)),
                        Margin = new Thickness(10),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top
                    };

                    // Define grid structure
                    for (int i = 0; i < 4; i++)
                        controlPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    for (int i = 0; i < 4; i++)
                        controlPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    // Create toggle buttons
                    btnOvernightLevels = CreateToggleButton("ONH/ONL", showOvernightLevelsEnabled, OnOvernightLevelsClick);
                    btnDailySma = CreateToggleButton("Daily SMA", showDailySmaEnabled, OnDailySmaClick);
                    btnVolumeSpikes = CreateToggleButton("Vol Spikes", showVolumeSpikesEnabled, OnVolumeSpikesClick);
                    btnMidpoint = CreateToggleButton("Midpoint", showMidpointEnabled, OnMidpointClick);

                    // Create input boxes
                    txtSmaPeriod = CreateInputBox(dynamicSmaPeriod.ToString(), OnSmaPeriodChanged);
                    txtVolumeThreshold = CreateInputBox(dynamicVolumeThreshold.ToString("F0"), OnVolumeThresholdChanged);
                    txtVolumePeriod = CreateInputBox(dynamicVolumePeriod.ToString(), OnVolumePeriodChanged);
                    txtLineWidth = CreateInputBox(dynamicLineWidth.ToString(), OnLineWidthChanged);

                    // Create labels
                    var lblSma = CreateLabel("SMA Period:");
                    var lblVolThresh = CreateLabel("Vol Thresh:");
                    var lblVolPeriod = CreateLabel("Vol Period:");
                    var lblLineWidth = CreateLabel("Line Width:");

                    // Add controls to grid
                    Grid.SetRow(btnOvernightLevels, 0); Grid.SetColumn(btnOvernightLevels, 0);
                    Grid.SetRow(btnDailySma, 0); Grid.SetColumn(btnDailySma, 1);
                    Grid.SetRow(btnVolumeSpikes, 0); Grid.SetColumn(btnVolumeSpikes, 2);
                    Grid.SetRow(btnMidpoint, 0); Grid.SetColumn(btnMidpoint, 3);

                    Grid.SetRow(lblSma, 1); Grid.SetColumn(lblSma, 0);
                    Grid.SetRow(txtSmaPeriod, 1); Grid.SetColumn(txtSmaPeriod, 1);
                    Grid.SetRow(lblVolThresh, 1); Grid.SetColumn(lblVolThresh, 2);
                    Grid.SetRow(txtVolumeThreshold, 1); Grid.SetColumn(txtVolumeThreshold, 3);

                    Grid.SetRow(lblVolPeriod, 2); Grid.SetColumn(lblVolPeriod, 0);
                    Grid.SetRow(txtVolumePeriod, 2); Grid.SetColumn(txtVolumePeriod, 1);
                    Grid.SetRow(lblLineWidth, 2); Grid.SetColumn(lblLineWidth, 2);
                    Grid.SetRow(txtLineWidth, 2); Grid.SetColumn(txtLineWidth, 3);

                    controlPanel.Children.Add(btnOvernightLevels);
                    controlPanel.Children.Add(btnDailySma);
                    controlPanel.Children.Add(btnVolumeSpikes);
                    controlPanel.Children.Add(btnMidpoint);
                    controlPanel.Children.Add(lblSma);
                    controlPanel.Children.Add(txtSmaPeriod);
                    controlPanel.Children.Add(lblVolThresh);
                    controlPanel.Children.Add(txtVolumeThreshold);
                    controlPanel.Children.Add(lblVolPeriod);
                    controlPanel.Children.Add(txtVolumePeriod);
                    controlPanel.Children.Add(lblLineWidth);
                    controlPanel.Children.Add(txtLineWidth);

                    // Add to chart
                    if (ChartControl != null && ChartControl.Parent is Panel chartPanel)
                    {
                        chartPanel.Children.Add(controlPanel);
                    }
                }
                catch (Exception ex)
                {
                    Print($"Error creating control panel: {ex.Message}");
                }
            });
        }

        private Button CreateToggleButton(string text, bool isEnabled, RoutedEventHandler clickHandler)
        {
            var button = new Button
            {
                Content = text,
                Width = 70,
                Height = 25,
                Margin = new Thickness(2),
                Background = isEnabled ? Brushes.LightGreen : Brushes.LightGray,
                Foreground = Brushes.Black,
                FontSize = 10
            };
            button.Click += clickHandler;
            return button;
        }

        private TextBox CreateInputBox(string text, TextChangedEventHandler changeHandler)
        {
            var textBox = new TextBox
            {
                Text = text,
                Width = 50,
                Height = 20,
                Margin = new Thickness(2),
                Background = Brushes.White,
                Foreground = Brushes.Black,
                FontSize = 10
            };
            textBox.TextChanged += changeHandler;
            return textBox;
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Content = text,
                Foreground = Brushes.White,
                FontSize = 9,
                Margin = new Thickness(2),
                Padding = new Thickness(0)
            };
        }

        // Event handlers for buttons
        private void OnOvernightLevelsClick(object sender, RoutedEventArgs e)
        {
            showOvernightLevelsEnabled = !showOvernightLevelsEnabled;
            btnOvernightLevels.Background = showOvernightLevelsEnabled ? Brushes.LightGreen : Brushes.LightGray;
        }

        private void OnDailySmaClick(object sender, RoutedEventArgs e)
        {
            showDailySmaEnabled = !showDailySmaEnabled;
            btnDailySma.Background = showDailySmaEnabled ? Brushes.LightGreen : Brushes.LightGray;
        }

        private void OnVolumeSpikesClick(object sender, RoutedEventArgs e)
        {
            showVolumeSpikesEnabled = !showVolumeSpikesEnabled;
            btnVolumeSpikes.Background = showVolumeSpikesEnabled ? Brushes.LightGreen : Brushes.LightGray;
        }

        private void OnMidpointClick(object sender, RoutedEventArgs e)
        {
            showMidpointEnabled = !showMidpointEnabled;
            btnMidpoint.Background = showMidpointEnabled ? Brushes.LightGreen : Brushes.LightGray;
        }

        // Event handlers for text inputs
        private void OnSmaPeriodChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(txtSmaPeriod.Text, out int newPeriod) && newPeriod > 0 && newPeriod <= 500)
            {
                dynamicSmaPeriod = newPeriod;
                dailySma = SMA(Close, dynamicSmaPeriod);
            }
        }

        private void OnVolumeThresholdChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(txtVolumeThreshold.Text, out double newThreshold) && newThreshold > 0)
            {
                dynamicVolumeThreshold = newThreshold;
            }
        }

        private void OnVolumePeriodChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(txtVolumePeriod.Text, out int newPeriod) && newPeriod > 0 && newPeriod <= 100)
            {
                dynamicVolumePeriod = newPeriod;
                volumeSma = SMA(Volume, dynamicVolumePeriod);
            }
        }

        private void OnLineWidthChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(txtLineWidth.Text, out int newWidth) && newWidth > 0 && newWidth <= 10)
            {
                dynamicLineWidth = newWidth;
            }
        }

        private void UpdateOvernightLevels()
        {
            DateTime barDate = Time[0].Date;
            
            if (barDate != currentDate)
            {
                currentDate = barDate;
                overnightHigh = High[0];
                overnightLow = Low[0];
            }
            else
            {
                if (High[0] > overnightHigh) overnightHigh = High[0];
                if (Low[0] < overnightLow) overnightLow = Low[0];
            }
            
            if (!double.IsNaN(overnightHigh) && !double.IsNaN(overnightLow))
            {
                overnightMidpoint = (overnightHigh + overnightLow) / 2;
            }
        }

        private void DrawOvernightLevels()
        {
            if (double.IsNaN(overnightHigh) || double.IsNaN(overnightLow)) return;

            // Draw overnight high
            Draw.Line(this, "ONH_" + CurrentBar, false, 1, overnightHigh, 0, overnightHigh,
                OvernightLevelColor, DashStyleHelper.Dash, dynamicLineWidth);

            // Draw overnight low
            Draw.Line(this, "ONL_" + CurrentBar, false, 1, overnightLow, 0, overnightLow,
                OvernightLevelColor, DashStyleHelper.Dash, dynamicLineWidth);

            // Draw midpoint if enabled
            if (showMidpointEnabled && !double.IsNaN(overnightMidpoint))
            {
                Draw.Line(this, "ONM_" + CurrentBar, false, 1, overnightMidpoint, 0, overnightMidpoint,
                    MidpointColor, DashStyleHelper.Dot, Math.Max(1, dynamicLineWidth - 1));
            }
        }

        private void CheckVolumeSpikes()
        {
            if (CurrentBar < dynamicVolumePeriod || volumeSma == null) return;

            double avgVolume = volumeSma[0];
            double currentVolume = Volume[0];

            if (currentVolume > (avgVolume * dynamicVolumeThreshold / 100.0))
            {
                Draw.ArrowUp(this, "VolSpike_" + CurrentBar, false, 0, Low[0] - (3 * TickSize),
                    VolumeSpikeColor);
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(1, 500)]
        [Display(Name="SMA Period", Description="Period for Daily SMA calculation", Order=1, GroupName="Parameters")]
        public int SmaPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name="Volume Spike Period", Description="Period for volume spike calculation", Order=2, GroupName="Parameters")]
        public int VolumeSpikePeriod { get; set; }

        [NinjaScriptProperty]
        [Range(100.0, 500.0)]
        [Display(Name="Volume Spike Threshold", Description="Threshold percentage for volume spikes", Order=3, GroupName="Parameters")]
        public double VolumeSpikeThreshold { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name="Line Width", Description="Width of drawn lines", Order=4, GroupName="Parameters")]
        public int LineWidth { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Overnight Levels", Description="Show overnight high/low levels", Order=5, GroupName="Visual Settings")]
        public bool ShowOvernightLevels { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Daily SMA", Description="Show daily SMA line", Order=6, GroupName="Visual Settings")]
        public bool ShowDailySma { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Volume Spikes", Description="Show volume spike markers", Order=7, GroupName="Visual Settings")]
        public bool ShowVolumeSpikes { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Midpoint", Description="Show overnight midpoint line", Order=8, GroupName="Visual Settings")]
        public bool ShowMidpoint { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Overnight Level Color", Description="Color for overnight levels", Order=9, GroupName="Colors")]
        public Brush OvernightLevelColor { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Midpoint Color", Description="Color for midpoint line", Order=10, GroupName="Colors")]
        public Brush MidpointColor { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="SMA Color", Description="Color for SMA line", Order=11, GroupName="Colors")]
        public Brush SmaColor { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Volume Spike Color", Description="Color for volume spike markers", Order=12, GroupName="Colors")]
        public Brush VolumeSpikeColor { get; set; }

        // Serialization properties for Brush colors
        [Browsable(false)]
        public string OvernightLevelColorSerialize
        {
            get { return Serialize.BrushToString(OvernightLevelColor); }
            set { OvernightLevelColor = Serialize.StringToBrush(value); }
        }

        [Browsable(false)]
        public string MidpointColorSerialize
        {
            get { return Serialize.BrushToString(MidpointColor); }
            set { MidpointColor = Serialize.StringToBrush(value); }
        }

        [Browsable(false)]
        public string SmaColorSerialize
        {
            get { return Serialize.BrushToString(SmaColor); }
            set { SmaColor = Serialize.StringToBrush(value); }
        }

        [Browsable(false)]
        public string VolumeSpikeColorSerialize
        {
            get { return Serialize.BrushToString(VolumeSpikeColor); }
            set { VolumeSpikeColor = Serialize.StringToBrush(value); }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DailySMA => Values[0];
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private MarketOpsSuitePM[] cacheMarketOpsSuitePM;
		public MarketOpsSuitePM MarketOpsSuitePM(int smaPeriod, int volumeSpikePeriod, double volumeSpikeThreshold, int lineWidth, bool showOvernightLevels, bool showDailySma, bool showVolumeSpikes, bool showMidpoint, Brush overnightLevelColor, Brush midpointColor, Brush smaColor, Brush volumeSpikeColor)
		{
			return MarketOpsSuitePM(Input, smaPeriod, volumeSpikePeriod, volumeSpikeThreshold, lineWidth, showOvernightLevels, showDailySma, showVolumeSpikes, showMidpoint, overnightLevelColor, midpointColor, smaColor, volumeSpikeColor);
		}

		public MarketOpsSuitePM MarketOpsSuitePM(ISeries<double> input, int smaPeriod, int volumeSpikePeriod, double volumeSpikeThreshold, int lineWidth, bool showOvernightLevels, bool showDailySma, bool showVolumeSpikes, bool showMidpoint, Brush overnightLevelColor, Brush midpointColor, Brush smaColor, Brush volumeSpikeColor)
		{
			if (cacheMarketOpsSuitePM != null)
				for (int idx = 0; idx < cacheMarketOpsSuitePM.Length; idx++)
					if (cacheMarketOpsSuitePM[idx] != null && cacheMarketOpsSuitePM[idx].SmaPeriod == smaPeriod && cacheMarketOpsSuitePM[idx].VolumeSpikePeriod == volumeSpikePeriod && cacheMarketOpsSuitePM[idx].VolumeSpikeThreshold == volumeSpikeThreshold && cacheMarketOpsSuitePM[idx].LineWidth == lineWidth && cacheMarketOpsSuitePM[idx].ShowOvernightLevels == showOvernightLevels && cacheMarketOpsSuitePM[idx].ShowDailySma == showDailySma && cacheMarketOpsSuitePM[idx].ShowVolumeSpikes == showVolumeSpikes && cacheMarketOpsSuitePM[idx].ShowMidpoint == showMidpoint && cacheMarketOpsSuitePM[idx].OvernightLevelColor == overnightLevelColor && cacheMarketOpsSuitePM[idx].MidpointColor == midpointColor && cacheMarketOpsSuitePM[idx].SmaColor == smaColor && cacheMarketOpsSuitePM[idx].VolumeSpikeColor == volumeSpikeColor && cacheMarketOpsSuitePM[idx].EqualsInput(input))
						return cacheMarketOpsSuitePM[idx];
			return CacheIndicator<MarketOpsSuitePM>(new MarketOpsSuitePM(){ SmaPeriod = smaPeriod, VolumeSpikePeriod = volumeSpikePeriod, VolumeSpikeThreshold = volumeSpikeThreshold, LineWidth = lineWidth, ShowOvernightLevels = showOvernightLevels, ShowDailySma = showDailySma, ShowVolumeSpikes = showVolumeSpikes, ShowMidpoint = showMidpoint, OvernightLevelColor = overnightLevelColor, MidpointColor = midpointColor, SmaColor = smaColor, VolumeSpikeColor = volumeSpikeColor }, input, ref cacheMarketOpsSuitePM);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MarketOpsSuitePM MarketOpsSuitePM(int smaPeriod, int volumeSpikePeriod, double volumeSpikeThreshold, int lineWidth, bool showOvernightLevels, bool showDailySma, bool showVolumeSpikes, bool showMidpoint, Brush overnightLevelColor, Brush midpointColor, Brush smaColor, Brush volumeSpikeColor)
		{
			return indicator.MarketOpsSuitePM(Input, smaPeriod, volumeSpikePeriod, volumeSpikeThreshold, lineWidth, showOvernightLevels, showDailySma, showVolumeSpikes, showMidpoint, overnightLevelColor, midpointColor, smaColor, volumeSpikeColor);
		}

		public Indicators.MarketOpsSuitePM MarketOpsSuitePM(ISeries<double> input , int smaPeriod, int volumeSpikePeriod, double volumeSpikeThreshold, int lineWidth, bool showOvernightLevels, bool showDailySma, bool showVolumeSpikes, bool showMidpoint, Brush overnightLevelColor, Brush midpointColor, Brush smaColor, Brush volumeSpikeColor)
		{
			return indicator.MarketOpsSuitePM(input, smaPeriod, volumeSpikePeriod, volumeSpikeThreshold, lineWidth, showOvernightLevels, showDailySma, showVolumeSpikes, showMidpoint, overnightLevelColor, midpointColor, smaColor, volumeSpikeColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MarketOpsSuitePM MarketOpsSuitePM(int smaPeriod, int volumeSpikePeriod, double volumeSpikeThreshold, int lineWidth, bool showOvernightLevels, bool showDailySma, bool showVolumeSpikes, bool showMidpoint, Brush overnightLevelColor, Brush midpointColor, Brush smaColor, Brush volumeSpikeColor)
		{
			return indicator.MarketOpsSuitePM(Input, smaPeriod, volumeSpikePeriod, volumeSpikeThreshold, lineWidth, showOvernightLevels, showDailySma, showVolumeSpikes, showMidpoint, overnightLevelColor, midpointColor, smaColor, volumeSpikeColor);
		}

		public Indicators.MarketOpsSuitePM MarketOpsSuitePM(ISeries<double> input , int smaPeriod, int volumeSpikePeriod, double volumeSpikeThreshold, int lineWidth, bool showOvernightLevels, bool showDailySma, bool showVolumeSpikes, bool showMidpoint, Brush overnightLevelColor, Brush midpointColor, Brush smaColor, Brush volumeSpikeColor)
		{
			return indicator.MarketOpsSuitePM(input, smaPeriod, volumeSpikePeriod, volumeSpikeThreshold, lineWidth, showOvernightLevels, showDailySma, showVolumeSpikes, showMidpoint, overnightLevelColor, midpointColor, smaColor, volumeSpikeColor);
		}
	}
}

#endregion
