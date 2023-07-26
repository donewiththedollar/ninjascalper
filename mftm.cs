#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public class MFTM : Strategy
    {
        private RSI rsi;
        private ChaikinMoneyFlow cmf;
        private TrendMagic tm;
        private int lastTrend;
		private bool initialPositionEntered;
		private bool positionClosed;
		private int trendChangeBarCount;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"MFTM.";
                Name = "MFTM";
                Calculate = Calculate.OnEachTick;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;

				positionClosed = false;
                TakeProfitTicks = 20;
                StopLossTicks = 20;
                UseMFIRSI = true;
            }
            else if (State == State.Configure)
            {
                rsi = RSI(14, 14);
                cmf = ChaikinMoneyFlow(14);
                tm = TrendMagic(14, 20, 1.0, false);
                AddChartIndicator(tm);
            }
			else if (State == State.DataLoaded)
			{
			    lastTrend = 0;  // Initialize lastTrend
			    initialPositionEntered = false;  // Initialize initialPositionEntered
			    trendChangeBarCount = 0;
			}
        }

        protected override void OnMarketData(MarketDataEventArgs e)
        {
            // Check if it is the first tick of the strategy
            if (BarsInProgress == 0 && !initialPositionEntered)
            {
                int firstTrend = (int) tm.TrendOutput[0];
                if (firstTrend == 1) {
                    EnterLong();
                } else if (firstTrend == -1) {
                    EnterShort();
                }
                initialPositionEntered = true;
            }
        }

		protected override void OnBarUpdate()
		{	
		    if (CurrentBars[0] < 1) return;

		    int currentTrend = (int) tm.TrendOutput[0];

		    Print("CurrentTrend: " + currentTrend);
		    Print("LastTrend: " + lastTrend);
		    Print("PositionClosed: " + positionClosed);
		    Print("MarketPosition: " + Position.MarketPosition);
		    Print("InitialPositionEntered: " + initialPositionEntered);

		    // If the trend changes, reset the count and place the stop loss.
		    if (currentTrend != lastTrend)
		    {
		        trendChangeBarCount = 0;
				if (initialPositionEntered) // If a position has been entered
				{
					SetStopLoss(CalculationMode.Ticks, StopLossTicks);
				}
		    }
		    else // If the trend is the same, increment the count.
		    {
		        trendChangeBarCount++;
		    }

		    if (Position.MarketPosition == MarketPosition.Flat && positionClosed == false)
		    {
		        if (currentTrend == 1)
		        {
		            EnterLong();
		            // Set profit target
		            SetProfitTarget(CalculationMode.Ticks, TakeProfitTicks);
		        }
		        else if (currentTrend == -1)
		        {
		            EnterShort();
		            // Set profit target
		            SetProfitTarget(CalculationMode.Ticks, TakeProfitTicks);
		        }
		    }
		    else if (initialPositionEntered) // If a position has been entered
		    {
		        // If the trend has been the same for two bars and position is long, enter another long position.
		        if (trendChangeBarCount >= 2 && Position.MarketPosition == MarketPosition.Long && currentTrend == 1 && positionClosed == true)
		        {
		            EnterLong();
		            // Set profit target
		            SetProfitTarget(CalculationMode.Ticks, TakeProfitTicks);
		            positionClosed = false;
		        }
		        // If the trend has been the same for two bars and position is short, enter another short position.
		        else if (trendChangeBarCount >= 2 && Position.MarketPosition == MarketPosition.Short && currentTrend == -1 && positionClosed == true)
		        {
		            EnterShort();
		            // Set profit target
		            SetProfitTarget(CalculationMode.Ticks, TakeProfitTicks);
		            positionClosed = false;
		        }
		    }

		    // If the position is flat, set positionClosed to true.
		    if (Position.MarketPosition == MarketPosition.Flat)
		    {
		        positionClosed = true;
		    }

		    // Display the current trend on the chart
		    string trend = currentTrend == 1 ? "Bullish" : "Bearish";
		    Draw.TextFixed(this, "TrendDisplay", "Current trend: " + trend, TextPosition.TopRight);

		    // Store the current trend for the next bar.
		    lastTrend = currentTrend;
		}

        #region Properties
        [NinjaScriptProperty]
        [Display(Name="TakeProfitTicks", Order=1, GroupName="Parameters")]
        public int TakeProfitTicks { get; set; }

        [NinjaScriptProperty]
        [Display(Name="StopLossTicks", Order=2, GroupName="Parameters")]
        public int StopLossTicks { get; set; }

        [NinjaScriptProperty]
        [Display(Name="UseMFIRSI", Order=3, GroupName="Parameters")]
        public bool UseMFIRSI { get; set; }
        #endregion
    }
}
