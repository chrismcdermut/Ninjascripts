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
	public class MySharedMethodsStrategy : Strategy
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description							= @"Enter the description for your new custom Strategy here.";
				Name								= "MySharedMethodsStrategy";
				Calculate							= Calculate.OnBarClose;
				EntriesPerDirection					= 1;
				EntryHandling						= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy		= true;
				ExitOnSessionCloseSeconds			= 30;
				IsFillLimitOnTouch					= false;
				MaximumBarsLookBack					= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution					= OrderFillResolution.Standard;
				Slippage							= 0;
				StartBehavior						= StartBehavior.WaitUntilFlat;
				TimeInForce							= TimeInForce.Gtc;
				TraceOrders							= false;
				RealtimeErrorHandling				= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling					= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade					= 20;
			}
			else if (State == State.Configure)
			{
				// add MySharedMethodsIndicator to ensure that the indicator runs before the strategy begins to process in OnBarUpdate
				// currently this does not ensure that the indicator runs first when the strategy is reloaded instead of an instance newly added
				AddChartIndicator(MySharedMethodsIndicator());
			}
		}

		protected override void OnBarUpdate()
		{
			// the NinjaTrader.NinjaScript.AddOns.MySharedMethods.SharedDouble is set to 5 in the MySharedMethodsIndicator indicator
			Print("SharedDouble value: " + NinjaTrader.NinjaScript.AddOns.MySharedMethods.SharedDouble);
			
			if (Position.MarketPosition == MarketPosition.Flat)
				EnterLong();
			
			MySharedMethods.PrintPositionInfo(Position);
		}
	}
}
