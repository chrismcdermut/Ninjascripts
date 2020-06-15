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
		private string pathTXT;
		private string pathCSV;
		private StreamWriter sw; // a variable for the StreamWriter that will be used 
		private Indicators.TachEon.TachEonTimeWarpAurora indiTachAur;
		private string ntStockData;
		private string auroraStockData;
		private string fullPrintOut;
		private string labels;
		private int dataIndex;
		private DateTime localDate;
		protected override void OnStateChange()
		
		{
			Print(string.Format("ONSTATECHANGE RUNNING2"));
			
			if (State == State.SetDefaults)
			{
				Print(string.Format("STATE.SETDEFAULTS"));
				Description									= @"AuroraBacktest";
				Name										= "AuroraBacktestUnlocked";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				labels                                      = "Time,Open,High,Low,Close,TrendPlot,BarsToNextSignal,BarsFromPreviousSignal,SignalPattern,BuySignalStopLine,SellSignalStopLine,DotPrice,OpenPrice";
				localDate                                   = DateTime.Now;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				// pathTXT 			                        = NinjaTrader.Core.Globals.UserDataDir + "stratOutputs.txt"; // Define the Path to our test file
				// pathCSV 			                        = NinjaTrader.Core.Globals.UserDataDir + "stratOutputs.csv"; // Define the Path to our test file
                pathTXT 			                        = NinjaTrader.Core.Globals.UserDataDir +localDate.ToString()+ "stratOutputs.txt"; // Define the Path to our test file
				pathCSV 			                        = NinjaTrader.Core.Globals.UserDataDir +localDate.ToString()+ "stratOutputs.csv"; // Define the Path to our test file
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
				
				sw = File.AppendText(pathCSV);  // Open the path for writing
				sw.WriteLine(labels); // Append a new line to the file		
				sw.Close(); // Close the file to allow future calls to access the file again.

				sw = File.AppendText(pathTXT);  // Open the path for writing
				sw.WriteLine(labels); // Append a new line to the file		
				sw.Close(); // Close the file to allow future calls to access the file again.
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
				Print(string.Format("STATE.TERMINATED2"));
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
			if (BarsInProgress != 0 || CurrentBar < BarsRequiredToTrade) 
				return;

			 //indiTachAur.TrendPlot.GetValueAt(dataIndex)
			 Print(string.Format("indiTachAur.TrendPlot.ToString"));
			 Print(indiTachAur.TrendPlot.ToString());
			 Print(string.Format("indiTachAur.TrendPlot.Count"));
			 Print(indiTachAur.TrendPlot.Count);
			 //indiTachAur.BarsToNextSignal.GetValueAt(dataIndex)
			 Print(string.Format("indiTachAur.BarsToNextSignal.ToString"));
			 Print(indiTachAur.BarsToNextSignal.ToString());
			 Print(string.Format("indiTachAur.BarsToNextSignal.Count"));
			 Print(indiTachAur.BarsToNextSignal.Count);
			 //indiTachAur.BarsFromPreviousSignal.GetValueAt(dataIndex)
			 Print(string.Format("indiTachAur.BarsFromPreviousSignal.ToString"));
			 Print(indiTachAur.BarsFromPreviousSignal.ToString());
			 Print(string.Format("indiTachAur.BarsFromPreviousSignal.Count"));
			 Print(indiTachAur.BarsFromPreviousSignal.Count);
			 //indiTachAur.SignalPattern.GetValueAt(dataIndex)
			 Print(string.Format("indiTachAur.SignalPattern.ToString"));
			 Print(indiTachAur.SignalPattern.ToString());
			 Print(string.Format("indiTachAur.SignalPattern.Count"));
			 Print(indiTachAur.SignalPattern.Count);
			 //indiTachAur.BuySignalStopLine.GetValueAt(dataIndex)
			 Print(string.Format("indiTachAur.BuySignalStopLine.ToString"));
			 Print(indiTachAur.BuySignalStopLine.ToString());
			 Print(string.Format("indiTachAur.BuySignalStopLine.Count"));
			 Print(indiTachAur.BuySignalStopLine.Count);
			 //indiTachAur.SellSignalStopLine.GetValueAt(dataIndex)
			 Print(string.Format("indiTachAur.SellSignalStopLine.ToString"));
			 Print(indiTachAur.SellSignalStopLine.ToString());
			 Print(string.Format("indiTachAur.SellSignalStopLine.Count"));
			 Print(indiTachAur.SellSignalStopLine.Count);
			 //indiTachAur.DotPrice.GetValueAt(dataIndex)
			 Print(string.Format("indiTachAur.DotPrice.ToString"));
			 Print(indiTachAur.DotPrice.ToString());
			 Print(string.Format("indiTachAur.DotPrice.Count"));
			 Print(indiTachAur.DotPrice.Count);
			 //indiTachAur.OpenPrice.GetValueAt(dataIndex)
			 Print(string.Format("indiTachAur.OpenPrice.ToString"));
			 Print(indiTachAur.OpenPrice.ToString());
			 Print(string.Format("indiTachAur.OpenPrice.Count"));
			 Print(indiTachAur.OpenPrice.Count);
			
			//			Print(string.Format("CurrentBar"));
			//			Print(CurrentBar);
			Print(string.Format("1ONBARUPDATE BEFORE PRINT indiTachAur.BarsToNextSignal.GetValueAt(CurrenBar)"));
			Print(indiTachAur.BarsToNextSignal.GetValueAt(CurrentBar));

			dataIndex = CurrentBar;
			ntStockData = Time[0] + "," + Open[0] + "," + High[0] + "," + Low[0] + "," + Close[0];
			auroraStockData = indiTachAur.TrendPlot.GetValueAt(dataIndex) + "," + indiTachAur.BarsToNextSignal.GetValueAt(dataIndex) + "," + indiTachAur.BarsFromPreviousSignal.GetValueAt(dataIndex) + "," + indiTachAur.SignalPattern.GetValueAt(dataIndex) + "," + indiTachAur.BuySignalStopLine.GetValueAt(dataIndex)+ "," + indiTachAur.SellSignalStopLine.GetValueAt(dataIndex) + "," + indiTachAur.DotPrice.GetValueAt(dataIndex) + "," + indiTachAur.OpenPrice.GetValueAt(dataIndex);
			fullPrintOut = ntStockData + "," + auroraStockData;

			Print(string.Format(fullPrintOut));
			
			sw = File.AppendText(pathCSV);  // Open the path for writing
			sw.WriteLine(fullPrintOut); // Append a new line to the file		
			sw.Close(); // Close the file to allow future calls to access the file again.

			sw = File.AppendText(pathTXT);  // Open the path for writing
			sw.WriteLine(fullPrintOut); // Append a new line to the file		
			sw.Close(); // Close the file to allow future calls to access the file again.
		}
	}
}
