#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO; // Add this to your declarations to use StreamWriter
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
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class AuroraBacktestUnlocked : Strategy
	{
		private string path;
		private StreamWriter sw; // a variable for the StreamWriter that will be used 
		//private NinjaTrader.NinjaScript.Indicators.Sim22.Sim22_DeltaV3 Sim22_DeltaV31;
		//private NinjaTrader.NinjaScript.Indicators.TachEon.TachEonTimeWarpAurora indiTachAur;
		private Indicators.TachEon.TachEonTimeWarpAurora indiTachAur;
		protected override void OnStateChange()
		{
			Print(string.Format("ONSTATECHANGE RUNNING1"));
			
			if (State == State.SetDefaults)
			{
				Print(string.Format("STATE.SETDEFAULTS"));
				Description									= @"AuroraBacktest";
				Name										= "AuroraBacktestUnlocked";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				//indiTachAur                                 = NinjaTrader.NinjaScript.Indicators.TachEon.TachEonTimeWarpAurora();
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				path 			                            = NinjaTrader.Core.Globals.UserDataDir + "stratOutputs.csv"; // Define the Path to our test file
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
			}
			else if (State == State.Configure)
			{
				Print(string.Format("STATE.CONFIGURE"));
				//AddChartIndicator(indiTachAur);
			}
			else if (State == State.DataLoaded)
			{
				Print(string.Format("STATE.DATALOADED"));
				//AddChartIndicator(indiTachAur);
				 				 if( ChartControl != null )
                    {
                      foreach( NinjaTrader.Gui.NinjaScript.IndicatorRenderBase indicator in ChartControl.Indicators )
                         if( indicator.Name == "TachEonTimeWarpAurora" )
                           {
                              // indiTachAur = (TachEonTimeWarpAurora)indicator;
							 indiTachAur = (Indicators.TachEon.TachEonTimeWarpAurora)indicator;
                              break;
                           }
               	 }
			}
			else if(State == State.Terminated)
			{
				Print(string.Format("STATE.TERMINATED"));
				if (sw != null)
				{
					sw.Close();
					sw.Dispose();
					sw = null;
				}
			}
		}

		protected override void OnBarUpdate()
		{
			Print(string.Format("ONBARUPDATE RUNNING BEFORE GUARD"));
			if (BarsInProgress != 0) 
				return;
			if (CurrentBar < BarsRequiredToTrade)
			    return;
//			Print(string.Format("CurrentBar"));
//			Print(CurrentBar);
			Print(string.Format("ONBARUPDATE RUNNING"));
//			Print(string.Format("CurrentBar"));
//			Print(CurrentBar);
			
			Print(string.Format("indiTachAur.BarsToNextSignal[0].ToString"));
			Print(indiTachAur.BarsToNextSignal.ToString());
			
			sw = File.AppendText(path);  // Open the path for writing
			
			//sw.WriteLine(Time[0] + "," + Open[0] + "," + High[0] + "," + Low[0] + "," + Close[0]); // Append a new line to the file
			Print(string.Format("1ONBARUPDATE BEFORE PRINT indiTachAur.BarsToNextSignal.Count"));
			Print(indiTachAur.BarsToNextSignal.Count);
			Print(string.Format("1ONBARUPDATE BEFORE PRINT indiTachAur.BarsToNextSignal.GetValueAt(CurrenBar)"));
			Print(indiTachAur.BarsToNextSignal.GetValueAt(CurrentBar));

			Print(string.Format("ONBARUPDATE AFTER PRINT indiTachAur B4 DBOX and SW"));
			Print(string.Format(Time[0] + "," + Open[0] + "," + High[0] + "," + Low[0] + "," + Close[0] + "," + indiTachAur.TrendPlot.GetValueAt(CurrentBar) + "," + indiTachAur.BarsToNextSignal.GetValueAt(CurrentBar) + "," + indiTachAur.BarsFromPreviousSignal.GetValueAt(CurrentBar) + "," + indiTachAur.SignalPattern.GetValueAt(CurrentBar) + "," + indiTachAur.BuySignalStopLine.GetValueAt(CurrentBar)+ "," + indiTachAur.SellSignalStopLine.GetValueAt(CurrentBar) + "," + indiTachAur.DotPrice.GetValueAt(CurrentBar) + "," + indiTachAur.OpenPrice.GetValueAt(CurrentBar)));
			sw.WriteLine(Time[0] + "," + Open[0] + "," + High[0] + "," + Low[0] + "," + Close[0] + "," + indiTachAur.TrendPlot.GetValueAt(CurrentBar) + "," + indiTachAur.BarsToNextSignal.GetValueAt(CurrentBar) + "," + indiTachAur.BarsFromPreviousSignal.GetValueAt(CurrentBar) + "," + indiTachAur.SignalPattern.GetValueAt(CurrentBar) + "," + indiTachAur.BuySignalStopLine.GetValueAt(CurrentBar)+ "," + indiTachAur.SellSignalStopLine.GetValueAt(CurrentBar) + "," + indiTachAur.DotPrice.GetValueAt(CurrentBar) + "," + indiTachAur.OpenPrice.GetValueAt(CurrentBar)); // Append a new line to the file
			Print(string.Format("ONBARUPDATE AFTER WRITELINE"));
			
			sw.Close(); // Close the file to allow future calls to access the file again.
		}
	}
}
