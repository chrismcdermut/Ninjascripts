// 
// Copyright (C) 2015, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class SampleBoolSeriesStrategy : Strategy
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Sample strategy demonstrating how to call an exposed BoolSeries object";
				Name						= "SampleBoolSeriesStrategy";
				Calculate					= Calculate.OnBarClose;
				EntriesPerDirection			= 1;
				EntryHandling				= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy= true;
				ExitOnSessionCloseSeconds	= 30;
				IsFillLimitOnTouch			= false;
				MaximumBarsLookBack			= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution			= OrderFillResolution.Standard;
				Slippage					= 0;
				StartBehavior				= StartBehavior.WaitUntilFlat;
				TimeInForce					= TimeInForce.Gtc;
				TraceOrders					= false;
				RealtimeErrorHandling		= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling			= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade			= 20;
			}
			else if (State == State.Configure)
			{
				AddChartIndicator(MACD(12,26,9));
				AddChartIndicator(SampleBoolSeries());
			}
		}

		protected override void OnBarUpdate()
		{
			/* When our indicator gives us a bull signal we enter long. Notice that we are accessing the
			public BoolSeries we made in the indicator. */
			if (SampleBoolSeries().BullIndication[0])
				EnterLong();
			
			// When our indicator gives us a bear signal we enter short
			if (SampleBoolSeries().BearIndication[0])
				EnterShort();
			
			/* NOTE: This strategy is based on reversals thus there are no explicit exit orders. When you
			are long you will be closed and reversed into a short when the bear signal is received. The vice
			versa is true if you are short. */
			
			/* Print our exposed variable. Because we manually kept it up-to-date it will print values that
			match the bars object. */
			Print(SampleBoolSeries().ExposedVariable);
		}
	}
}
