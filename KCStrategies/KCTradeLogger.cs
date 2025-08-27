// FILE: KCTradeLogger.cs
// VERSION 6.4.2 - Added BE_Trigger Logging
// Key Changes:
// 1. NEW FIELD: Added `BE_Trigger` to both the CSV and JSON log formats.

using System;
using System.IO;
using System.Linq;
using System.Text;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Strategies.KCStrategies
{
    // --- JSON Logger Components ---

    public class JsonTradeEntry
    {
        public string EntryType => "Trade";
        public string Strategy { get; set; }
        public string Instrument { get; set; }
        public string Account { get; set; }
        public int TradeNumber { get; set; }
        public DateTime EntryTime { get; set; }
        public MarketPosition Direction { get; set; }
        public double EntryPrice { get; set; }
        public long Quantity { get; set; }
        public DateTime ExitTime { get; set; }
        public double ExitPrice { get; set; }
        public string ExitName { get; set; }
        public double ProfitTicks { get; set; }
        public double ProfitCurrency { get; set; }
        public double Commission { get; set; }
        public double MfeTicks { get; set; }
        public double MaeTicks { get; set; }
        public string MarketRegime { get; set; }
        public string SignalSource { get; set; }
        public string StopType { get; set; }
        public string ProfitType { get; set; }
        public double ConfluenceScore { get; set; }
        public double AdxAtExit { get; set; }
        public double AtrAtExit { get; set; }
        public double MomentumAtExit { get; set; }
        public int BarsInTrade { get; set; }
        public double InitialSLTicks { get; set; }
        public double InitialTPTicks { get; set; }
        public double BeTriggerTicks { get; set; }
        public double SlippageTicks { get; set; }
    }

    public class TradeJsonLogger
    {
        private readonly string filePath;
        private static readonly object fileLock = new object();

        public TradeJsonLogger(string fullFilePath)
        {
            filePath = fullFilePath;
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private string ToJsonString(JsonTradeEntry entry)
        {
            string sanitizedExitName = entry.ExitName.Replace("\"", "\\\"");
            string sanitizedSignalSource = entry.SignalSource.Replace("\"", "\\\"");

            return "{"
                + $"\"EntryType\":\"{entry.EntryType}\","
                + $"\"Strategy\":\"{entry.Strategy}\","
                + $"\"Instrument\":\"{entry.Instrument}\","
                + $"\"Account\":\"{entry.Account}\","
                + $"\"TradeNumber\":{entry.TradeNumber},"
                + $"\"EntryTime\":\"{entry.EntryTime:o}\","
                + $"\"Direction\":\"{entry.Direction}\","
                + $"\"EntryPrice\":{entry.EntryPrice},"
                + $"\"Quantity\":{entry.Quantity},"
                + $"\"ExitTime\":\"{entry.ExitTime:o}\","
                + $"\"ExitPrice\":{entry.ExitPrice},"
                + $"\"ExitName\":\"{sanitizedExitName}\","
                + $"\"ProfitTicks\":{entry.ProfitTicks},"
                + $"\"ProfitCurrency\":{entry.ProfitCurrency},"
                + $"\"Commission\":{entry.Commission},"
                + $"\"MfeTicks\":{entry.MfeTicks},"
                + $"\"MaeTicks\":{entry.MaeTicks},"
                + $"\"MarketRegime\":\"{entry.MarketRegime}\","
                + $"\"SignalSource\":\"{sanitizedSignalSource}\","
                + $"\"StopType\":\"{entry.StopType}\","
                + $"\"ProfitType\":\"{entry.ProfitType}\","
                + $"\"ConfluenceScore\":{entry.ConfluenceScore:F0},"
                + $"\"AdxAtExit\":{entry.AdxAtExit:F2},"
                + $"\"AtrAtExit\":{entry.AtrAtExit:F2},"
                + $"\"MomentumAtExit\":{entry.MomentumAtExit:F2},"
                + $"\"BarsInTrade\":{entry.BarsInTrade},"
                + $"\"InitialSLTicks\":{entry.InitialSLTicks:F2},"
                + $"\"InitialTPTicks\":{entry.InitialTPTicks:F2},"
                + $"\"BeTriggerTicks\":{entry.BeTriggerTicks:F2},"
                + $"\"SlippageTicks\":{entry.SlippageTicks:F2}"
                + "}";
        }
        
        public bool LogTrade(string strategyName, Trade trade, double mfeTicks, double maeTicks, 
                             string marketRegime, string signalSource, string stopTypeUsed, string profitTypeUsed, double confluenceScore, double adxValue, double atrValue, double momentumValue, int barsInTrade, double initialSL, double initialTP, double beTrigger, double slippageTicks,
                             out string errorMessage)
        {
            errorMessage = string.Empty;
            if (trade?.Entry == null || trade.Exit == null)
            {
                errorMessage = "LogTrade failed: Trade or its Entry/Exit was null.";
                return false;
            }

            try
            {
                var logEntry = new JsonTradeEntry
                {
                    Strategy = strategyName, Instrument = trade.Entry.Instrument.FullName, Account = trade.Entry.Account.Name,
                    TradeNumber = trade.TradeNumber, EntryTime = trade.Entry.Time, Direction = trade.Entry.MarketPosition,
                    EntryPrice = trade.Entry.Price, Quantity = trade.Quantity, ExitTime = trade.Exit.Time,
                    ExitPrice = trade.Exit.Price, ExitName = trade.Exit.Name, ProfitTicks = trade.ProfitTicks,
                    ProfitCurrency = trade.ProfitCurrency, Commission = trade.Commission, MfeTicks = mfeTicks, MaeTicks = maeTicks,
                    MarketRegime = marketRegime, SignalSource = signalSource, StopType = stopTypeUsed, ProfitType = profitTypeUsed, ConfluenceScore = confluenceScore, AdxAtExit = adxValue, AtrAtExit = atrValue,
                    MomentumAtExit = momentumValue, BarsInTrade = barsInTrade, InitialSLTicks = initialSL, InitialTPTicks = initialTP, BeTriggerTicks = beTrigger,
                    SlippageTicks = slippageTicks
                };

                string jsonLine = ToJsonString(logEntry);

                lock (fileLock)
                {
                    File.AppendAllText(filePath, jsonLine + Environment.NewLine);
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error logging trade to JSON: {ex.Message}";
                return false;
            }
        }
    }

    // --- CSV Logger Component ---

    public class TradeLogger
    {
        private readonly string filePath;
        private static readonly object fileLock = new object();

        public TradeLogger(string fullFilePath)
        {
            filePath = fullFilePath;
            InitializeFile();
        }

        private void InitializeFile()
        {
            lock (fileLock)
            {
                try
                {
                    string directory = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    if (!File.Exists(filePath))
                    {
                        string header = "Strategy,Instrument,Account,TradeNum,EntryTime,Direction,EntryPrice,Quantity,ExitTime,ExitPrice,ExitName,ProfitTicks,ProfitCurrency,Commission,MFE_Ticks,MAE_Ticks,"
                                      + "MarketRegime,SignalSource,StopType,ProfitType,ConfluenceScore,ADX_at_Exit,ATR_at_Exit,Momentum_at_Exit,BarsInTrade,InitialSL_Ticks,InitialTP_Ticks,BE_Trigger_Ticks,SlippageTicks" 
                                      + Environment.NewLine;
                        File.WriteAllText(filePath, header);
                    }
                }
                catch { /* Fails silently on initialization, will be caught during logging */ }
            }
        }

        public bool LogTrade(string strategyName, Trade trade, double mfeTicks, double maeTicks, 
                             string marketRegime, string signalSource, string stopTypeUsed, string profitTypeUsed, double confluenceScore, double adxValue, double atrValue, double momentumValue, int barsInTrade, double initialSL, double initialTP, double beTrigger, double slippageTicks,
                             out string errorMessage)
        {
            errorMessage = string.Empty;
            if (trade == null || trade.Entry == null || trade.Exit == null)
            {
                errorMessage = "LogTrade failed: Provided trade object or its Entry/Exit was null.";
                return false;
            }

            try
            {
                var sb = new StringBuilder();
                sb.Append($"{strategyName},");
                sb.Append($"{trade.Entry.Instrument.FullName},");
                sb.Append($"{trade.Entry.Account.Name},");
                sb.Append($"{trade.TradeNumber},");
                sb.Append($"{trade.Entry.Time:yyyy-MM-dd HH:mm:ss},");
                sb.Append($"{trade.Entry.MarketPosition},");
                sb.Append($"{trade.Entry.Price:F2},");
                sb.Append($"{trade.Quantity},");
                sb.Append($"{trade.Exit.Time:yyyy-MM-dd HH:mm:ss},");
                sb.Append($"{trade.Exit.Price:F2},");
                sb.Append($"{trade.Exit.Name.Replace(",", ";")},");
                sb.Append($"{trade.ProfitTicks},");
                sb.Append($"{trade.ProfitCurrency:F2},");
                sb.Append($"{trade.Commission:F2},");
                sb.Append($"{mfeTicks:F0},");
                sb.Append($"{maeTicks:F0},");
                sb.Append($"{marketRegime},");
                sb.Append($"{signalSource.Replace(",", ";")},");
                sb.Append($"{stopTypeUsed},");
                sb.Append($"{profitTypeUsed},");
                sb.Append($"{confluenceScore:F0},");
                sb.Append($"{adxValue:F2},");
                sb.Append($"{atrValue:F2},");
                sb.Append($"{momentumValue:F2},");
                sb.Append($"{barsInTrade},");
                sb.Append($"{initialSL:F2},");
                sb.Append($"{initialTP:F2},");
                sb.Append($"{beTrigger:F2},");
                sb.Append($"{slippageTicks:F2}");

                sb.Append(Environment.NewLine);

                lock (fileLock)
                {
                    File.AppendAllText(filePath, sb.ToString());
                }
                return true;
            }
            catch (IOException ex)
            {
                errorMessage = $"Error logging trade to CSV: {ex.Message}";
                return false;
            }
        }
    }
}