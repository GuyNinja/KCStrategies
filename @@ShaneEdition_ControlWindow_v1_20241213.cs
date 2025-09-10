//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// ShaneEdition_ControlWindow_v1_20241213.cs - Master Control Panel AddOn
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.Gui.NinjaScript
{
    /// <summary>
    /// ShaneEdition Control Window - Master Control Panel
    /// Centralized control for all agents, systems, and monitoring
    /// Date: December 13, 2024
    /// </summary>
    public class ShaneEditionControlWindow : NTWindow, IWorkspacePersistence
    {
        #region Variables
        // Control window components
        private Grid mainGrid;
        private TabControl mainTabControl;
        
        // Agent control variables
        private Dictionary<string, bool> agentStates = new Dictionary<string, bool>();
        private Dictionary<string, Button> agentButtons = new Dictionary<string, Button>();
        private Dictionary<string, TextBlock> agentStatus = new Dictionary<string, TextBlock>();
        
        // System monitoring variables
        private bool newsDetectionEnabled = false;
        private bool systemOverwatchEnabled = false;
        private bool volumeAlertsEnabled = false;
        private bool chatbotSystemEnabled = false;
        
        // Status indicators
        private TextBlock systemStatusText;
        private TextBlock performanceText;
        private TextBlock alertCountText;
        
        // Control buttons
        private Button newsDetectionButton;
        private Button systemOverwatchButton;
        private Button volumeAlertsButton;
        private Button chatbotButton;
        
        // Performance monitoring
        private System.Windows.Threading.DispatcherTimer updateTimer;
        private DateTime lastUpdateTime;
        private int alertCount = 0;
        
        // Colors for states
        private SolidColorBrush enabledBrush = new SolidColorBrush(Colors.LimeGreen);
        private SolidColorBrush disabledBrush = new SolidColorBrush(Colors.DarkGray);
        private SolidColorBrush warningBrush = new SolidColorBrush(Colors.Orange);
        private SolidColorBrush errorBrush = new SolidColorBrush(Colors.Red);
        #endregion

        public ShaneEditionControlWindow()
        {
            // Set window properties following user's minimal approach
            Caption = "ShaneEdition Control v1";
            Width = 400;
            Height = 600;
            
            InitializeControlWindow();
            InitializeAgentStates();
            StartPerformanceMonitoring();
        }

        #region Initialization Methods
        private void InitializeControlWindow()
        {
            // Main grid for layout
            mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) }); // Status
            
            // Header
            CreateHeaderSection();
            
            // Main content with tabs
            CreateMainContent();
            
            // Status section
            CreateStatusSection();
            
            Content = mainGrid;
        }

        private void CreateHeaderSection()
        {
            Grid headerGrid = new Grid();
            headerGrid.Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));
            
            TextBlock titleText = new TextBlock
            {
                Text = "🚀 ShaneEdition Control Center",
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            headerGrid.Children.Add(titleText);
            Grid.SetRow(headerGrid, 0);
            mainGrid.Children.Add(headerGrid);
        }

        private void CreateMainContent()
        {
            mainTabControl = new TabControl();
            mainTabControl.Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
            
            // Agent Control Tab
            TabItem agentTab = new TabItem
            {
                Header = "🤖 Agents",
                Content = CreateAgentControlPanel()
            };
            
            // System Control Tab
            TabItem systemTab = new TabItem
            {
                Header = "⚙️ Systems",
                Content = CreateSystemControlPanel()
            };
            
            // Monitoring Tab
            TabItem monitorTab = new TabItem
            {
                Header = "📊 Monitor",
                Content = CreateMonitoringPanel()
            };
            
            // Chatbots Tab
            TabItem chatTab = new TabItem
            {
                Header = "🤖 AI Bots",
                Content = CreateChatbotPanel()
            };
            
            mainTabControl.Items.Add(agentTab);
            mainTabControl.Items.Add(systemTab);
            mainTabControl.Items.Add(monitorTab);
            mainTabControl.Items.Add(chatTab);
            
            Grid.SetRow(mainTabControl, 1);
            mainGrid.Children.Add(mainTabControl);
        }

        private void CreateStatusSection()
        {
            Grid statusGrid = new Grid();
            statusGrid.Background = new SolidColorBrush(Color.FromRgb(35, 35, 35));
            statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            // System status
            systemStatusText = new TextBlock
            {
                Text = "Status: Active",
                Foreground = enabledBrush,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            // Performance info
            performanceText = new TextBlock
            {
                Text = "CPU: 0%\nMEM: 0%",
                Foreground = Brushes.White,
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            // Alert count
            alertCountText = new TextBlock
            {
                Text = "Alerts: 0",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            Grid.SetColumn(systemStatusText, 0);
            Grid.SetColumn(performanceText, 1);
            Grid.SetColumn(alertCountText, 2);
            
            statusGrid.Children.Add(systemStatusText);
            statusGrid.Children.Add(performanceText);
            statusGrid.Children.Add(alertCountText);
            
            Grid.SetRow(statusGrid, 2);
            mainGrid.Children.Add(statusGrid);
        }
        #endregion

        #region Panel Creation Methods
        private Grid CreateAgentControlPanel()
        {
            Grid agentGrid = new Grid();
            agentGrid.Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            
            // Create scroll viewer for agent list
            ScrollViewer scrollViewer = new ScrollViewer();
            StackPanel agentStack = new StackPanel();
            agentStack.Margin = new Thickness(10);
            
            // Agent list following sequential pattern
            string[] agents = { "1Agent", "2Agent", "3Agent", "4Agent", "5Agent", 
                               "6Agent", "7Agent", "8Agent", "9Agent", "10Agent" };
            
            foreach (string agent in agents)
            {
                agentStack.Children.Add(CreateAgentControl(agent));
            }
            
            scrollViewer.Content = agentStack;
            agentGrid.Children.Add(scrollViewer);
            
            return agentGrid;
        }

        private Grid CreateSystemControlPanel()
        {
            Grid systemGrid = new Grid();
            systemGrid.Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            
            StackPanel systemStack = new StackPanel();
            systemStack.Margin = new Thickness(10);
            
            // News Detection System
            systemStack.Children.Add(CreateSystemControl(
                "📰 News Detection", 
                "Real-time news monitoring and impact analysis",
                ref newsDetectionButton,
                () => ToggleNewsDetection()));
            
            // System Overwatch
            systemStack.Children.Add(CreateSystemControl(
                "🔍 System Overwatch", 
                "Hardware monitoring and performance optimization",
                ref systemOverwatchButton,
                () => ToggleSystemOverwatch()));
            
            // Volume Alerts
            systemStack.Children.Add(CreateSystemControl(
                "📊 Volume Alerts", 
                "Volume surge detection and early warnings",
                ref volumeAlertsButton,
                () => ToggleVolumeAlerts()));
            
            systemGrid.Children.Add(systemStack);
            return systemGrid;
        }

        private Grid CreateMonitoringPanel()
        {
            Grid monitorGrid = new Grid();
            monitorGrid.Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            
            StackPanel monitorStack = new StackPanel();
            monitorStack.Margin = new Thickness(10);
            
            // Real-time performance metrics
            TextBlock perfTitle = new TextBlock
            {
                Text = "📈 Performance Metrics",
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            // Add monitoring components here
            TextBlock placeholder = new TextBlock
            {
                Text = "Real-time monitoring dashboard\n\n" +
                       "• Agent Performance Tracking\n" +
                       "• System Resource Usage\n" +
                       "• Alert History\n" +
                       "• Market Data Flow\n" +
                       "• Connection Status",
                Foreground = Brushes.LightGray,
                FontSize = 12
            };
            
            monitorStack.Children.Add(perfTitle);
            monitorStack.Children.Add(placeholder);
            
            monitorGrid.Children.Add(monitorStack);
            return monitorGrid;
        }

        private Grid CreateChatbotPanel()
        {
            Grid chatGrid = new Grid();
            chatGrid.Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            
            StackPanel chatStack = new StackPanel();
            chatStack.Margin = new Thickness(10);
            
            // Chatbot controls following your 3-tier system
            chatStack.Children.Add(CreateChatbotControl(
                "👨‍🏫 Baby Yoda", 
                "Trading teacher & strategy development"));
            
            chatStack.Children.Add(CreateChatbotControl(
                "🤖 Advanced Assistant", 
                "Auto alert builder & trade execution"));
            
            chatStack.Children.Add(CreateChatbotControl(
                "💻 Code Developer", 
                "System development & innovation"));
            
            chatGrid.Children.Add(chatStack);
            return chatGrid;
        }
        #endregion

        #region Control Creation Helpers
        private Border CreateAgentControl(string agentName)
        {
            Border border = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(5)
            };
            
            Grid agentGrid = new Grid();
            agentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            agentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            agentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            
            // Agent name
            TextBlock nameText = new TextBlock
            {
                Text = agentName,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            // Status text
            TextBlock statusText = new TextBlock
            {
                Text = "Disabled",
                Foreground = disabledBrush,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            // Toggle button
            Button toggleButton = new Button
            {
                Content = "OFF",
                Background = disabledBrush,
                Foreground = Brushes.White,
                BorderBrush = Brushes.Transparent,
                Width = 60,
                Height = 25
            };
            
            toggleButton.Click += (s, e) => ToggleAgent(agentName);
            
            Grid.SetColumn(nameText, 0);
            Grid.SetColumn(statusText, 1);
            Grid.SetColumn(toggleButton, 2);
            
            agentGrid.Children.Add(nameText);
            agentGrid.Children.Add(statusText);
            agentGrid.Children.Add(toggleButton);
            
            border.Child = agentGrid;
            
            // Store references
            agentButtons[agentName] = toggleButton;
            agentStatus[agentName] = statusText;
            
            return border;
        }

        private Border CreateSystemControl(string systemName, string description, ref Button button, Action toggleAction)
        {
            Border border = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 10, 0, 10),
                Padding = new Thickness(15),
                CornerRadius = new CornerRadius(5)
            };
            
            StackPanel systemStack = new StackPanel();
            
            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            
            TextBlock nameText = new TextBlock
            {
                Text = systemName,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            
            button = new Button
            {
                Content = "OFF",
                Background = disabledBrush,
                Foreground = Brushes.White,
                BorderBrush = Brushes.Transparent,
                Width = 60,
                Height = 30
            };
            
            button.Click += (s, e) => toggleAction();
            
            Grid.SetColumn(nameText, 0);
            Grid.SetColumn(button, 1);
            
            headerGrid.Children.Add(nameText);
            headerGrid.Children.Add(button);
            
            TextBlock descText = new TextBlock
            {
                Text = description,
                Foreground = Brushes.LightGray,
                FontSize = 11,
                Margin = new Thickness(0, 5, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            
            systemStack.Children.Add(headerGrid);
            systemStack.Children.Add(descText);
            
            border.Child = systemStack;
            return border;
        }

        private Border CreateChatbotControl(string botName, string description)
        {
            Border border = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 10, 0, 10),
                Padding = new Thickness(15),
                CornerRadius = new CornerRadius(5)
            };
            
            StackPanel botStack = new StackPanel();
            
            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            
            TextBlock nameText = new TextBlock
            {
                Text = botName,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            
            Button activateButton = new Button
            {
                Content = "Chat",
                Background = new SolidColorBrush(Colors.DodgerBlue),
                Foreground = Brushes.White,
                BorderBrush = Brushes.Transparent,
                Width = 60,
                Height = 30
            };
            
            activateButton.Click += (s, e) => ActivateChatbot(botName);
            
            Grid.SetColumn(nameText, 0);
            Grid.SetColumn(activateButton, 1);
            
            headerGrid.Children.Add(nameText);
            headerGrid.Children.Add(activateButton);
            
            TextBlock descText = new TextBlock
            {
                Text = description,
                Foreground = Brushes.LightGray,
                FontSize = 11,
                Margin = new Thickness(0, 5, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            
            botStack.Children.Add(headerGrid);
            botStack.Children.Add(descText);
            
            border.Child = botStack;
            return border;
        }
        #endregion

        #region Control Methods
        private void InitializeAgentStates()
        {
            string[] agents = { "1Agent", "2Agent", "3Agent", "4Agent", "5Agent", 
                               "6Agent", "7Agent", "8Agent", "9Agent", "10Agent" };
            
            foreach (string agent in agents)
            {
                agentStates[agent] = false;
            }
        }

        private void ToggleAgent(string agentName)
        {
            agentStates[agentName] = !agentStates[agentName];
            UpdateAgentDisplay(agentName);
            
            // Log agent state change
            NinjaTrader.Code.Output.Process($"ShaneEdition: {agentName} {(agentStates[agentName] ? "ENABLED" : "DISABLED")}", 
                                           PrintTo.OutputTab1);
        }

        private void UpdateAgentDisplay(string agentName)
        {
            if (agentButtons.ContainsKey(agentName) && agentStatus.ContainsKey(agentName))
            {
                bool isEnabled = agentStates[agentName];
                
                agentButtons[agentName].Content = isEnabled ? "ON" : "OFF";
                agentButtons[agentName].Background = isEnabled ? enabledBrush : disabledBrush;
                
                agentStatus[agentName].Text = isEnabled ? "Active" : "Disabled";
                agentStatus[agentName].Foreground = isEnabled ? enabledBrush : disabledBrush;
            }
        }

        private void ToggleNewsDetection()
        {
            newsDetectionEnabled = !newsDetectionEnabled;
            UpdateSystemButton(newsDetectionButton, newsDetectionEnabled);
            
            // TODO: Implement actual news detection toggle
            NinjaTrader.Code.Output.Process($"ShaneEdition: News Detection {(newsDetectionEnabled ? "ENABLED" : "DISABLED")}", 
                                           PrintTo.OutputTab1);
        }

        private void ToggleSystemOverwatch()
        {
            systemOverwatchEnabled = !systemOverwatchEnabled;
            UpdateSystemButton(systemOverwatchButton, systemOverwatchEnabled);
            
            // TODO: Implement actual system monitoring toggle
            NinjaTrader.Code.Output.Process($"ShaneEdition: System Overwatch {(systemOverwatchEnabled ? "ENABLED" : "DISABLED")}", 
                                           PrintTo.OutputTab1);
        }

        private void ToggleVolumeAlerts()
        {
            volumeAlertsEnabled = !volumeAlertsEnabled;
            UpdateSystemButton(volumeAlertsButton, volumeAlertsEnabled);
            
            // TODO: Implement actual volume alerts toggle
            NinjaTrader.Code.Output.Process($"ShaneEdition: Volume Alerts {(volumeAlertsEnabled ? "ENABLED" : "DISABLED")}", 
                                           PrintTo.OutputTab1);
        }

        private void UpdateSystemButton(Button button, bool isEnabled)
        {
            button.Content = isEnabled ? "ON" : "OFF";
            button.Background = isEnabled ? enabledBrush : disabledBrush;
        }

        private void ActivateChatbot(string botName)
        {
            // TODO: Implement chatbot activation
            NinjaTrader.Code.Output.Process($"ShaneEdition: Activating {botName} chatbot", 
                                           PrintTo.OutputTab1);
        }
        #endregion

        #region Performance Monitoring
        private void StartPerformanceMonitoring()
        {
            updateTimer = new System.Windows.Threading.DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromSeconds(1);
            updateTimer.Tick += UpdatePerformanceMetrics;
            updateTimer.Start();
        }

        private void UpdatePerformanceMetrics(object sender, EventArgs e)
        {
            // Simple performance monitoring
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                double cpuPercent = 0; // Simplified for now
                double memoryMB = process.WorkingSet64 / (1024 * 1024);
                
                performanceText.Text = $"CPU: {cpuPercent:F0}%\nMEM: {memoryMB:F0}MB";
                
                // Update system status based on performance
                if (memoryMB > 1000) // Example threshold
                {
                    systemStatusText.Text = "Status: High Memory";
                    systemStatusText.Foreground = warningBrush;
                }
                else
                {
                    systemStatusText.Text = "Status: Active";
                    systemStatusText.Foreground = enabledBrush;
                }
                
                alertCountText.Text = $"Alerts: {alertCount}";
            }
            catch (Exception ex)
            {
                // Handle any monitoring errors gracefully
                performanceText.Text = "Monitor: Error";
            }
        }
        #endregion

        #region Workspace Persistence
        public void RestoreFromTemplate(XDocument document, XElement element)
        {
            // Restore window state from workspace
            if (element != null)
            {
                // TODO: Implement state restoration
            }
        }

        public void SaveToTemplate(XDocument document, XElement element)
        {
            // Save window state to workspace
            if (element != null)
            {
                // TODO: Implement state saving
            }
        }

        public WorkspaceOptions WorkspaceOptions
        {
            get { return WorkspaceOptions.CloseOnLogout | WorkspaceOptions.Restore; }
        }
        #endregion

        protected override void OnClosed(EventArgs e)
        {
            if (updateTimer != null)
            {
                updateTimer.Stop();
                updateTimer = null;
            }
            
            base.OnClosed(e);
        }
    }

    public static class WindowHelpers
    {
        private static ShaneEditionControlWindow controlWindow;

        public static void OpenControlWindow()
        {
            if (controlWindow == null)
            {
                controlWindow = new ShaneEditionControlWindow();
                controlWindow.Show();
            }
            else
            {
                controlWindow.Activate();
            }
        }
    }
}

#region Menu Integration
namespace NinjaTrader.Gui.Tools
{
    public class ShaneEditionMenuItem : NTMenuItem
    {
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Open ShaneEdition Control Window";
                Name = "ShaneEdition Control";
                IsEnabled = true;
            }
        }

        protected override void OnMenuItemClick()
        {
            NinjaTrader.Gui.NinjaScript.WindowHelpers.OpenControlWindow();
        }
    }
}
#endregion
