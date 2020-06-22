#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
	public class TachEonForceBacktestUnlocked : Strategy
	{
		private string fileName;
		private string localDate;
		private string ninjaDirectory;								
		private string pathCSV;
		private Indicators.TachEon.TachEonForce indiTachForce;
		// private Indicators.TachEon.TachEonTimeWarpAurora indiTachAur;
		private StreamWriter sw; // a variable for the StreamWriter that will be used for csv
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"TachEonForceBacktestUnlocked";
				Name										= "TachEonForceBacktestUnlocked";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.Infinite;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.ImmediatelySubmit;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 2;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				
				localDate									= DateTime.Now.ToString("yyyyMMddHH");
				fileName									= localDate + "outputs.csv"; //can add/remove localDate.ToString("yyyyMMddHH") from middle
				ninjaDirectory								= NinjaTrader.Core.Globals.UserDataDir + "bin/"+"Custom/"+"TestData/";
				pathCSV										= NinjaTrader.Core.Globals.UserDataDir + fileName; // Define the Path to our test file
			}
			else if (State == State.Configure)
			{
				/*Do something here eventually*/
			}			
			else if (State == State.DataLoaded)
			{
				if( ChartControl != null )
				{
					foreach( NinjaTrader.Gui.NinjaScript.IndicatorRenderBase indicator in ChartControl.Indicators )
					{
                        if( indicator.Name == "TachEonForce" )
						{
							indiTachForce = (Indicators.TachEon.TachEonForce)indicator;
							break;
						}
						// sw = File.AppendText(pathCSV);
				        // // sw.WriteLine(strategyLabels);
				        // // sw.WriteLine(strategyInfo);
				        // // sw.WriteLine(labels);
						// sw.WriteLine(indicator.Name);
				        // sw.Close();
					}
						
				}
			}
			else if(State == State.Terminated)
			{
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
			if (BarsInProgress != 0) 
				return;

			
		}
	}
}
