// --- KCAlgoBase_OrderManagement.cs ---
// Version 6.6.9 - Unified Stop Management
// Key Changes:
// 1. FUNCTIONALITY FIX: Reverted OnEntryExecutionFilled to use `SetTrailStop()` for ALL stop types, including FixedStop.
//    This ensures that the automatic breakeven and all manual stop-moving buttons on the UI panel function correctly for every trade.
//    This unifies stop management to be client-side, providing maximum flexibility at the cost of requiring a stable connection.
// 2. DYNAMIC MODE FIX: Corrected SetDynamicTradeManagement to use the same client-side SetTrailStop and SetProfitTargets methods as Static mode. This resolves the issue where stops and targets would not appear on the chart in Dynamic mode.

#region Using declarations
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
#endregion

//This is a partial class file.
//It contains all logic related to order management, including submission, tracking,
//stop/target handling, and safety mechanisms like rogue order detection.
namespace NinjaTrader.NinjaScript.Strategies.KCStrategies
{
    public abstract partial class KCAlgoBase : Strategy
    {
        #region Order Label Constants
        protected const string LE = "LE";
		protected const string LE2 = "LE2";
		protected const string LE3 = "LE3";
		protected const string LE4 = "LE4";
        protected const string SE = "SE";
        protected const string SE2 = "SE2";
        protected const string SE3 = "SE3";
        protected const string SE4 = "SE4";
        protected const string QLE = "QLE";
        protected const string QSE = "QSE";
		protected const string Add1LE = "Add1LE";
		protected const string Add1SE = "Add1SE";
        #endregion

        #region Order Submission & Execution

		/// <summary>
		/// Submits an entry order with robust error handling and tracking. This is the centralized method for all entries.
		/// </summary>
		protected virtual Order SubmitEntryOrder(string orderLabel, OrderType orderType, int contracts, bool bypassThrottle = false)
		{
			Order submittedOrder = null;
			lock (orderLock)
			{
				if (!bypassThrottle && !CanSubmitOrder()) return null;
				try
				{
					bool isLongOrder = orderLabel.Contains("L") || orderLabel.Contains("Buy");
					bool isShortOrder = orderLabel.Contains("S") || orderLabel.Contains("Sell");

					if (isLongOrder)
					{
						if (orderType == OrderType.Market) submittedOrder = EnterLong(contracts, orderLabel);
						else if (orderType == OrderType.Limit) submittedOrder = EnterLongLimit(contracts, GetCurrentBid() - LimitOffset * TickSize, orderLabel);
					}
					else if (isShortOrder)
					{
						if (orderType == OrderType.Market) submittedOrder = EnterShort(contracts, orderLabel);
						else if (orderType == OrderType.Limit) submittedOrder = EnterShortLimit(contracts, GetCurrentAsk() + TickSize, orderLabel);
					}

					if (submittedOrder != null)
					{
						activeOrders[orderLabel] = submittedOrder;
					    lastOrderActionTime = DateTime.Now;

						if (!activeTradeSignalNames.Contains(orderLabel))
						{
							activeTradeSignalNames.Add(orderLabel);
						}
					}
					else 
					{ 
						Print($"ERROR: Order submission failed for label '{orderLabel}'. The order type may not be handled or the label is invalid.");
						orderErrorOccurred = true; 
					}
				}
				catch (Exception ex)
				{
					Print($"{Time[0]}: Error submitting {orderLabel} entry order: {ex.Message}");
					orderErrorOccurred = true;
				}
			}
			
			return submittedOrder;
		}

		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			base.OnExecutionUpdate(execution, executionId, price, quantity, marketPosition, orderId, time);
			
			if (execution.Order.OrderState == OrderState.Filled)
			{
				bool isEntry = execution.Name.Contains(LE) || execution.Name.Contains(SE) || execution.Name.Contains(QLE) || execution.Name.Contains(QSE);
				if (isEntry)
				{
					highSinceEntry = High[0];
					lowSinceEntry = Low[0];
				}
			}

		    lock (orderLock)
		    {
		        string orderLabel = activeOrders.FirstOrDefault(x => x.Value.OrderId == orderId).Key;
		        if (!string.IsNullOrEmpty(orderLabel))
		        {
		            if (execution.Order.OrderState == OrderState.Filled || execution.Order.OrderState == OrderState.Cancelled || execution.Order.OrderState == OrderState.Rejected)
					{
						activeOrders.Remove(orderLabel);
					}
		        }
		        else
		        {
		            if (execution.Order.OrderState == OrderState.Working)
					{
						Print($"{Time[0]}: Execution update for untracked order {orderId}. Attempting to cancel.");
						try { CancelOrder(execution.Order); } catch { /* Ignore */ }
					}
		        }
		    }
			
			if (execution.Order.OrderState == OrderState.Filled && Position.MarketPosition != MarketPosition.Flat) entryBarNumberOfTrade = CurrentBar;
		}

		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
		{
			bool isEntryOrder = order.Name.Contains(LE) || order.Name.Contains(SE) || order.Name.Contains(QLE) || order.Name.Contains(QSE);
			if (orderState == OrderState.Filled && isEntryOrder)
			{
				OnEntryExecutionFilled(order.Name);
			}

		    var activeOrderEntry = activeOrders.FirstOrDefault(kvp => kvp.Key == order.Name);
		
		    if (activeOrderEntry.Value != null)
		    {
		        Order trackedOrder = activeOrderEntry.Value;
		
		        if (trackedOrder.IsBacktestOrder && State == State.Realtime)
		        {
		            Order realtimeOrder = GetRealtimeOrder(trackedOrder);
		            if (realtimeOrder != null)
		            {
		                activeOrders[order.Name] = realtimeOrder;
		                Print($"Updated historical order '{order.Name}' to its real-time reference.");
		            }
		        }
		    }
		}
		
        private bool CanSubmitOrder()
		{
			return (DateTime.Now - lastOrderActionTime) >= minOrderActionInterval;
		}
        #endregion
		
		#region Position and Order Management
		
		private void ManageStopLoss()
		{
		    if (isFlat) return;
		    
		    StopManagementType currentStopType = userSelectedStopType; 
		
		    if (EnableAutoRegimeDetection && currentRegime == MarketRegime.Ranging)
		        currentStopType = StopManagementType.FixedStop;
		    
		    if (currentStopType == StopManagementType.FixedStop)
		        return;
		    
		    double calculatedTrailValue = GetInitialStopInTicks();
			double pnlTicks = Position.GetUnrealizedProfitLoss(PerformanceUnit.Ticks, Close[0]);
			
		    switch (currentStopType)
		    {
		        case StopManagementType.ParabolicTrail:
					if (ProfitTarget > 0 && pnlTicks >= (ProfitTarget * 0.5))
					{
						if (psar != null && psar.IsValidDataPoint(TrailBarsLookback))
						{
							double psarLookbackPrice = psar[TrailBarsLookback];
							calculatedTrailValue = isLong ? (Close[0] - psarLookbackPrice) / TickSize : (psarLookbackPrice - Close[0]) / TickSize;
						}
					}
					else
					{
						if (psar != null && psar.IsValidDataPoint(0))
						{
							double psarCurrentPrice = psar[0];
							calculatedTrailValue = isLong ? (Close[0] - psarCurrentPrice) / TickSize : (psarCurrentPrice - Close[0]) / TickSize;
						}
					}
		            break;

		        case StopManagementType.DynamicTrail:
                    if (!BE_Realized)
                    {
                        return; 
                    }

                    double profitTarget = GetProfitTargetInTicks();
                    double ticksToTrail;

                    if (profitTarget > 0 && pnlTicks >= (profitTarget * (FinalTrailTriggerPercent / 100.0)))
                    {
                        ticksToTrail = FinalTrailTicks;
                    }
                    else
                    {
                        ticksToTrail = InitialTrailTicks;
                    }
                    
                    calculatedTrailValue = ticksToTrail;
		            break;
					
				case StopManagementType.HighLowTrail:
				    double target = GetProfitTargetInTicks();
				    if (target > 0)
				    {
				        if (pnlTicks >= target * 0.80) highLowTrailCurrentLookback = 0;
				        else if (pnlTicks >= target * 0.70) highLowTrailCurrentLookback = 1;
				        else if (pnlTicks >= target * 0.60) highLowTrailCurrentLookback = 2;
				    }

				    if (CurrentBar > highLowTrailCurrentLookback)
				    {
				        double stopPrice = isLong ? Low[highLowTrailCurrentLookback] : High[highLowTrailCurrentLookback];
				        calculatedTrailValue = isLong ? (Close[0] - stopPrice) / TickSize : (stopPrice - Close[0]) / TickSize;
				    }
				    break;
					
				case StopManagementType.ATRTrail:
					if (ATR1 == null) return;
				
					double ptForAtr = GetProfitTargetInTicks();
					double currentMultiplier = atrMultiplier;
				
					if (ptForAtr > 0 && pnlTicks >= (ptForAtr * (AtrTrailTriggerPercent / 100.0)))
					{
						currentMultiplier = AtrFinalMultiplier;
					}
				
					calculatedTrailValue = (ATR1[0] * currentMultiplier) / TickSize;
					break;
		    }
		    
			trailValueInTicks = Math.Max(1.0, calculatedTrailValue);
		
		    foreach (string label in GetRelevantOrderLabels())
		    {
		        SetTrailStop(label, CalculationMode.Ticks, trailValueInTicks, true);
		    }
		}

		private void SetProfitTargets(string fromEntrySignal, double targetInTicks)
		{
		    try
		    {
		        if (targetInTicks <= 0)
				{
					Print($"ERROR: Profit target for signal '{fromEntrySignal}' resulted in {targetInTicks} ticks. No target will be set.");
					return;
				}
				
				string signalPrefix = fromEntrySignal.Split('_')[0];

				if (signalPrefix == LE || signalPrefix == SE || signalPrefix == QLE || signalPrefix == QSE)
				{
					currentTradeInitialTPTicks = targetInTicks;
				}

				if (signalPrefix == LE || signalPrefix == SE || signalPrefix == QLE || signalPrefix == QSE || signalPrefix == Add1LE || signalPrefix == Add1SE)
				{
					SetProfitTarget(fromEntrySignal, CalculationMode.Ticks, targetInTicks);
				}
				else if (EnableProfitTarget2 && (signalPrefix == LE2 || signalPrefix == SE2)) 
				{
					SetProfitTarget(fromEntrySignal, CalculationMode.Ticks, ProfitTarget2);
				}
				else if (EnableProfitTarget3 && (signalPrefix == LE3 || signalPrefix == SE3)) 
				{
					SetProfitTarget(fromEntrySignal, CalculationMode.Ticks, ProfitTarget3);
				}
				else if (EnableProfitTarget4 && (signalPrefix == LE4 || signalPrefix == SE4))
				{
					SetProfitTarget(fromEntrySignal, CalculationMode.Ticks, ProfitTarget4);
				}
		    }
		    catch (Exception ex) 
		    { 
		        Print($"Error in SetProfitTargets for signal {fromEntrySignal}: {ex.Message}\nStackTrace: {ex.StackTrace}");
		        orderErrorOccurred = true;
		    }
		}
		
		private double CalculateProfitTargetInTicks()
		{
			double calculatedTarget = 0;
			
			if(PTType == ProfitTargetType.RegChan)
			{
				if (botRegChanPlus != null && botRegChanPlus.IsValidDataPoint(0))
				{
					double targetPrice = isLong ? botRegChanPlus.UpperStdDevBand[0] : botRegChanPlus.LowerStdDevBand[0];
					double ticksToTarget = Math.Abs(targetPrice - Position.AveragePrice) / TickSize;
					calculatedTarget = (ticksToTarget >= ProfitTarget) ? ticksToTarget : ProfitTarget;
				}
			}
			else if (PTType == ProfitTargetType.RiskRewardRatio)
			{
				double stopDistanceForRR = 0;
				if (StopType == StopManagementType.ATRTrail && ATR1 != null && ATR1.IsValidDataPoint(0))
					stopDistanceForRR = (ATR1[0] * atrMultiplier) / TickSize;
				else
					stopDistanceForRR = GetInitialStopInTicks();
				
				if (stopDistanceForRR > 0)
					calculatedTarget = Math.Max(1.0, Math.Round(stopDistanceForRR * RiskRewardRatio));
			}
			else if (PTType == ProfitTargetType.ATR)
			{
                if (entryAtr > 0)
                    calculatedTarget = Math.Max(4, (entryAtr * RiskRewardRatio) / TickSize);
				else if (ATR1 != null && ATR1.IsValidDataPoint(0))
					calculatedTarget = Math.Max(4, (ATR1[0] * RiskRewardRatio) / TickSize);
			}
			else // Default to Fixed
			{
		        calculatedTarget = GetProfitTargetInTicks();
			}
			return calculatedTarget;
		}

        private void ManageAutoBreakeven()
		{
		    if (isFlat || !BESetAuto || BE_Realized || CurrentBar < TrailBarsLookback) return;
		
		    double triggerValue = 0;
		    if (BreakevenTriggerMode == BETriggerMode.FixedTicks)
		    {
		        triggerValue = BETriggerTicks;
		    }
		    else // ProfitTargetPercentage
		    {
		        if (currentTradeInitialTPTicks > 0)
		        {
		            triggerValue = currentTradeInitialTPTicks * (BETriggerTicks / 100.0);
		        }
		    }
		
		    if (triggerValue > 0 && Position.GetUnrealizedProfitLoss(PerformanceUnit.Ticks, Close[0]) >= triggerValue)
		    {
		        double beStopPrice = isLong 
		            ? Position.AveragePrice + (BE_Offset * TickSize) 
		            : Position.AveragePrice - (BE_Offset * TickSize);
		        
		        beStopPrice = Instrument.MasterInstrument.RoundToTickSize(beStopPrice);
		        
		        foreach (string tag in GetRelevantOrderLabels())
		        {
		            double ticks = isLong ? ((Close[0] - beStopPrice) / TickSize) : ((beStopPrice - Close[0]) / TickSize);
		            if (ticks > 0)
		            {
		                SetTrailingStop(tag, CalculationMode.Ticks, ticks, true);
		            }
		        }
		        BE_Realized = true;
		    }
		}

        #endregion
		
		#region Set Dynamic Trade Management
		protected void SetDynamicTradeManagement(string fromEntrySignal)
		{
		    if (isFlat) return;
		
		    double stopLossToUse = dynamicStopLossTicks > 0 ? dynamicStopLossTicks : DynamicInitialSL;
		    double profitTargetToUse = dynamicProfitTargetTicks > 0 ? dynamicProfitTargetTicks : DynamicInitialTP;
			
			double structuralStopTicks = 0;
			
			if (isLong && Low.IsValidDataPoint(2))
			{
				structuralStopTicks = (Position.AveragePrice - Low[2]) / TickSize;
			}
			else if (isShort && High.IsValidDataPoint(2))
			{
				structuralStopTicks = (High[2] - Position.AveragePrice) / TickSize;
			}

			if (structuralStopTicks > 0)
			{
				double originalSL = stopLossToUse;
				stopLossToUse = Math.Max(stopLossToUse, structuralStopTicks);
				
				if (stopLossToUse > originalSL)
					PrintOnce($"DynamicSL_Override_{CurrentBar}", $"Dynamic SL overridden. Using wider structural stop. Initial: {originalSL:F0}, Final: {stopLossToUse:F0} ticks.");
			}

		    currentTradeInitialSLTicks = stopLossToUse;
		    currentTradeInitialTPTicks = profitTargetToUse;
		
		    // FIX: Use client-side trail stop and the custom PT wrapper for consistency with Static mode.
            // This ensures UI buttons and auto-BE function correctly and that orders appear on the chart.
		    SetTrailStop(fromEntrySignal, CalculationMode.Ticks, stopLossToUse, true);
		    SetProfitTargets(fromEntrySignal, profitTargetToUse);
		    
		    PrintOnce($"DynamicTradeSetup-{CurrentBar}", $"Dynamic trade management set. Final SL: {stopLossToUse:F0}, TP: {profitTargetToUse:F0}.");
		}
		#endregion
		
        #region Manual Order Actions (from UI)

        protected void MoveToBreakeven()
		{
		    if (isFlat || CurrentBar < TrailBarsLookback || TickSize <= 0 || Position.AveragePrice == 0) return;
		
		    double beStopPrice = isLong 
		        ? Position.AveragePrice + BE_Offset * TickSize 
		        : Position.AveragePrice - BE_Offset * TickSize;
		    
		    beStopPrice = Instrument.MasterInstrument.RoundToTickSize(beStopPrice);
		    
		    foreach (string label in GetRelevantOrderLabels())
		    {
		        double ticks = isLong 
		            ? (Close[0] - beStopPrice) / TickSize 
		            : (beStopPrice - Close[0]) / TickSize;
		        
		        if (ticks > 0)
		        {
		            SetTrailingStop(label, CalculationMode.Ticks, ticks, true);
		        }
		    }
		    BE_Realized = true;
		}

		
		private void MoveStopToSwingPoint()
		{
		    if (isFlat || CurrentBar < ManualMoveStopLookback) return;
		    
		    double newStopPrice = isLong ? Low[ManualMoveStopLookback] : High[ManualMoveStopLookback];
		    
		    foreach (string label in GetRelevantOrderLabels())
		    {
		        double ticks = isLong 
		            ? (Close[0] - newStopPrice) / TickSize 
		            : (newStopPrice - Close[0]) / TickSize;
		        
		        if (ticks > 0)
		        {
		            SetTrailingStop(label, CalculationMode.Ticks, ticks, true);
		        }
		    }
		}
		
		protected void MoveTrailingStopByPercentage(double percentage)
		{
		    if (percentage <= 0 || percentage >= 1 || isFlat || TickSize <= 0) return;
		
		    foreach (string tag in GetRelevantOrderLabels())
		    {
		        Order stopOrder = Orders.FirstOrDefault(o => o.FromEntrySignal == tag 
		                                                  && o.IsStopMarket 
		                                                  && (o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted));
		        
		        if (stopOrder != null)
		        {
		            double currentStopPrice = stopOrder.StopPrice;
		            double dist = Math.Abs(Close[0] - currentStopPrice);
		            
		            if (dist < TickSize) continue;
		            
		            double newStopPrice = isLong 
		                ? currentStopPrice + (dist * percentage) 
		                : currentStopPrice - (dist * percentage);
		            
		            newStopPrice = Instrument.MasterInstrument.RoundToTickSize(newStopPrice);
		            
		            double ticks = isLong 
		                ? (Close[0] - newStopPrice) / TickSize 
		                : (newStopPrice - Close[0]) / TickSize;
		            
		            if (ticks > 0)
		            {
		                SetTrailingStop(tag, CalculationMode.Ticks, ticks, true);
		            }
		        }
		    }
		}
		
		protected void AddOneEntry()
		{
		    if (isFlat || (Position.Quantity + 1 > EntriesPerDirection)) return;

		    string addLabel = (isLong ? Add1LE : Add1SE) + "_" + uniqueTradeCounter;
		    SubmitEntryOrder(addLabel, OrderType, 1);
		}
		
		#region Manual Order Actions (from UI)
		protected void SafePartialClose()
		{
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				Print("Cannot use Close Partial: Position is already flat.");
				return;
			}
			
			if (ChartControl != null)
			{
				ChartControl.Dispatcher.InvokeAsync(() => 
				{
					int quantityToClose = 1; 
					if (closePartialTextBox != null && int.TryParse(closePartialTextBox.Text, out int parsedQty))
					{
						quantityToClose = parsedQty > 0 ? parsedQty : 1;
					}
					
					if (Position.Quantity >= quantityToClose)
					{
						if (isLong)
						{
							ExitLong(quantityToClose, "Manual Partial Close", "");
							counterLong -= quantityToClose;
							if (counterLong < 0) counterLong = 0;
						}
						else if (isShort)
						{
							ExitShort(quantityToClose, "Manual Partial Close", "");
							counterShort -= quantityToClose;
							if (counterShort < 0) counterShort = 0;
						}
					}
					else
					{
						Print($"Cannot close {quantityToClose} contract(s). Position quantity ({Position.Quantity}) is less than requested.");
					}
				});
			}
		}
		
		protected void CloseAllPositions()
		{
			if (isFlat) return;

			if (isLong)
				ExitLong("Manual Close All", "");
			else if (isShort)
				ExitShort("Manual Close All", "");
			
            exitedThisBar = true;
		}
		
        protected void FlattenAllPositions()
        {
			if (Account == null) return;
			Account.Flatten(new List<Instrument> { Instrument });
		}
        #endregion
        #endregion

        #region Safety and Helper Methods
		
		private void ReconcileAccountOrders()
		{
		    lock (orderLock)
		    {
				if (Account == null) return;
				List<Order> accountOrders = Orders.Where(o => o.Instrument == Instrument && o.Account == Account).ToList();
				if (accountOrders.Count == 0) return;
				
				HashSet<string> strategyOrderIds = new HashSet<string>(activeOrders.Values.Select(o => o.OrderId));
				
				foreach (Order o in accountOrders)
				{
					if (o.OrderState == OrderState.Working && !strategyOrderIds.Contains(o.OrderId))
					{
						Print($"{Time[0]}: Rogue order {o.OrderId} detected. Cancelling.");
						try { CancelOrder(o); } catch { /* Ignore */ }
					}
				}
		    }
		}
		
		protected void SetTrailingStop(string fromEntrySignal, CalculationMode mode, double value, bool isSimulatedStop = true)
		{
		     lock(orderLock)
		     {
		         try
		         {
		             SetTrailStop(fromEntrySignal, mode, value, isSimulatedStop);
		         }
		         catch (Exception ex)
		         {
		             Print($"{Time[0]}: Error calling SetTrailStop for label '{fromEntrySignal}': {ex.Message}");
		             orderErrorOccurred = true;
		         }
		     }
		}
		
		protected List<string> GetRelevantOrderLabels()
		{
		    return new List<string>(activeTradeSignalNames);
		}
		
        private bool IsValidStopPlacement(double targetStopPrice, MarketPosition position, int bufferTicks = 4)
        {
            if (TickSize <= 0 || !IsMarketDataValid()) return false;
            if (position == MarketPosition.Long) return targetStopPrice < (GetCurrentAsk() - bufferTicks * TickSize);
            if (position == MarketPosition.Short) return targetStopPrice > (GetCurrentBid() + bufferTicks * TickSize);
            return false;
        }
		
        private bool IsMarketDataValid() { return GetCurrentBid() > 0 && GetCurrentAsk() > 0; }
		
		private void EnterMultipleLongContracts(long tradeId) 
		{
		    if (EnableProfitTarget2) EnterMultipleOrders(true, LE2 + "_" + tradeId, Contracts2);
		    if (EnableProfitTarget3) EnterMultipleOrders(true, LE3 + "_" + tradeId, Contracts3);
		    if (EnableProfitTarget4) EnterMultipleOrders(true, LE4 + "_" + tradeId, Contracts4);
		}
		
		private void EnterMultipleShortContracts(long tradeId) 
		{
		    if (EnableProfitTarget2) EnterMultipleOrders(false, SE2 + "_" + tradeId, Contracts2);
		    if (EnableProfitTarget3) EnterMultipleOrders(false, SE3 + "_" + tradeId, Contracts3);
		    if (EnableProfitTarget4) EnterMultipleOrders(false, SE4 + "_" + tradeId, Contracts4);
		}
			
		private void EnterMultipleOrders(bool isLong, string uniqueSignalName, int contracts)
		{
		    SubmitEntryOrder(uniqueSignalName, OrderType, contracts, true);
		}				
		
		private void OnEntryExecutionFilled(string entrySignalName)
		{
			if (ManagementMode == TradeManagementMode.Dynamic)
			{
				SetDynamicTradeManagement(entrySignalName);
				return;
			}
			
			// --- Static Mode: Unified Client-Side Stop Management ---
			double stopLossToUse = GetInitialStopInTicks();
			currentTradeInitialSLTicks = stopLossToUse;
			double profitTargetToUse = CalculateProfitTargetInTicks();
			
			// Always use SetTrailStop to ensure BE and manual moves work correctly for all stop types.
			// A non-moving trail stop is functionally a fixed stop, but remains client-side.
			SetTrailStop(entrySignalName, CalculationMode.Ticks, stopLossToUse, true);
			SetProfitTargets(entrySignalName, profitTargetToUse);
		}
        #endregion
    }
}