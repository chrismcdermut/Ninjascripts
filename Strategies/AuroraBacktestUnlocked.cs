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
		private string auroraStockData;
		private string barType;
		private string barValue;
		private int dataIndex;
		private int daysLoaded;
		private string fileName;
		private string fileDirectoryPath;
		private string fullPrintOut;
		private Indicators.TachEon.TachEonTimeWarpAurora indiTachAur;
		private string instrument;
		private string instrumentType;
		private string labels;
		private string localDate;
		private string ninjaDirectory;
		private string ntStockData;
		private string pathCSV;
		private string strategyInfo;
		private string strategyLabels;
		private StreamWriter sw; // a variable for the StreamWriter that will be used for csv 

///////////////////Parameters///////////////////
//TODO: Change types to string from ints
        #region Properties
        [NinjaScriptProperty]
		[Display(Name="LookbackSources", Description="Look back sources ie, 12345(trade day) 7142128(days ago)", Order=1, GroupName="Parameters")]
		public string LookbackSources
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="LookbackSetting", Description="Trading days vs days back", Order=2, GroupName="Parameters")]
		public string LookbackSetting
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="SwingStrength", Description="Chosen swing strength", Order=3, GroupName="Parameters")]
		public int SwingStrength
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="VolOffSetCheck", Description="Volatility offset is checked, ie, 1,0", Order=4, GroupName="Parameters")]
		public bool VolOffSetCheck
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="VolOffsetRatio", Description="Volatility offset ratio", Order=5, GroupName="Parameters")]
		public double VolOffsetRatio
		{ get; set; }
        #endregion

		protected override void OnStateChange()
		{
			Print(string.Format("ONSTATECHANGE"));	
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
				MaximumBarsLookBack							= MaximumBarsLookBack.Infinite;
				OrderFillResolution							= OrderFillResolution.Standard;				
				SwingStrength								= 5;
				LookbackSetting 							= "12345etc";
				LookbackSources							    = "daysAgo||TradingDays";
				VolOffSetCheck                              = true;
				VolOffsetRatio                              = 1.5;
				Slippage									= 0;
				StartBehavior								= StartBehavior.ImmediatelySubmit;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 2;
				IsInstantiatedOnEachOptimizationIteration	= true;

				daysLoaded									= 0;
				localDate									= DateTime.Now.ToString("yyyyMMddHH");
				instrument									= "UndefInstrument";
				barType										= "UndefBarType";
				barValue									= "UndefBarValue";
				instrumentType                              = "UndefInstrumentType";
				strategyLabels                              = "localDate,instrument,barValue,barType,daysLoaded,LookbackSetting,LookbackSources,SwingStrength,VolOffSetCheck,VolOffsetRatio";
				strategyInfo                                = localDate;
				fileName									= localDate + "outputs.csv"; //can add/remove localDate.ToString("yyyyMMddHH") from middle
				ninjaDirectory								= NinjaTrader.Core.Globals.UserDataDir + "bin/"+"Custom/"+"TestData/";
				pathCSV										= NinjaTrader.Core.Globals.UserDataDir + fileName; // Define the Path to our test file 
				labels										= "CurrentBar,Time,Open,High,Low,Close,TrendPlot,BarsToNextSignal,BarsFromPreviousSignal,SignalPattern,BuySignalStopLine,SellSignalStopLine,DotPrice,OpenPrice";
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
						if( indicator.Name == "TachEonTimeWarpAurora" )
						{
							indiTachAur = (Indicators.TachEon.TachEonTimeWarpAurora)indicator;
							break;
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
			if (BarsInProgress != 0 || CurrentBar < BarsRequiredToTrade) 
				return;
			
////////////Instrument Variable Setting/////////
			instrument									= Instrument != null ? Instrument.FullName : "UndefInstrument";
			fileDirectoryPath                           = ninjaDirectory+instrument;
			barType										= BarsPeriod.ToString();
			barValue									= BarsPeriod.Value.ToString();
			daysLoaded                                  = ChartBars.Properties.DaysBack;
			strategyInfo                                = strategyInfo+","+instrument+","+barValue+","+barType+","+daysLoaded+","+LookbackSetting+","+LookbackSources+","+SwingStrength+","+VolOffSetCheck+","+VolOffsetRatio;
			instrumentType                              = instrument+"."+BarsPeriod.ToString();
			pathCSV										= fileDirectoryPath +"/"+instrumentType+daysLoaded+"days"+"."+LookbackSources+LookbackSetting+SwingStrength+"ss"+VolOffsetRatio+"volr"+"."+fileName; // Define the Path to our test file

			if (!Directory.Exists(fileDirectoryPath))
			{
			    Directory.CreateDirectory(fileDirectoryPath);
			}

////////////Data Variable Setting////////////

			dataIndex = (CurrentBar-1);
			ntStockData = CurrentBar + "," + Time[0] + "," + Open[0] + "," + High[0] + "," + Low[0] + "," + Close[0];
			auroraStockData = indiTachAur.TrendPlot.GetValueAt(dataIndex) + "," + indiTachAur.BarsToNextSignal.GetValueAt(dataIndex) + "," + indiTachAur.BarsFromPreviousSignal.GetValueAt(dataIndex) + "," + indiTachAur.SignalPattern.GetValueAt(dataIndex) + "," + indiTachAur.BuySignalStopLine.GetValueAt(dataIndex)+ "," + indiTachAur.SellSignalStopLine.GetValueAt(dataIndex) + "," + indiTachAur.DotPrice.GetValueAt(dataIndex) + "," + indiTachAur.OpenPrice.GetValueAt(dataIndex);
			fullPrintOut = ntStockData + "," + auroraStockData;

////////////Printing area////////////

			if (!File.Exists(pathCSV))
			{
				sw = File.AppendText(pathCSV);
				sw.WriteLine(strategyLabels);
				sw.WriteLine(strategyInfo);
				sw.WriteLine(labels);
				sw.Close();
			}
			else
			{
				Print("string.Format(fullPrintOut)");
				Print(string.Format(fullPrintOut));
				sw = File.AppendText(pathCSV);
				sw.WriteLine(fullPrintOut); // Append a new line to the file		
				sw.Close(); // Close the file to allow future calls to access the file again.   
			}
		}
	}
}

/////////////Debugging/////////////

			// Print(string.Format("CurrentBar"));
			// Print(CurrentBar);
			// //indiTachAur.TrendPlot.GetValueAt(dataIndex)
			// Print(string.Format("indiTachAur.TrendPlot.ToString"));
			// Print(indiTachAur.TrendPlot.ToString());
			// Print(string.Format("indiTachAur.TrendPlot.Count"));
			// Print(indiTachAur.TrendPlot.Count);
			// //indiTachAur.BarsToNextSignal.GetValueAt(dataIndex)
			// Print(string.Format("indiTachAur.BarsToNextSignal.ToString"));
			// Print(indiTachAur.BarsToNextSignal.ToString());
			// Print(string.Format("indiTachAur.BarsToNextSignal.Count"));
			// Print(indiTachAur.BarsToNextSignal.Count);
			// //indiTachAur.BarsFromPreviousSignal.GetValueAt(dataIndex)
			// Print(string.Format("indiTachAur.BarsFromPreviousSignal.ToString"));
			// Print(indiTachAur.BarsFromPreviousSignal.ToString());
			// Print(string.Format("indiTachAur.BarsFromPreviousSignal.Count"));
			// Print(indiTachAur.BarsFromPreviousSignal.Count);
			// //indiTachAur.SignalPattern.GetValueAt(dataIndex)
			// Print(string.Format("indiTachAur.SignalPattern.ToString"));
			// Print(indiTachAur.SignalPattern.ToString());
			// Print(string.Format("indiTachAur.SignalPattern.Count"));
			// Print(indiTachAur.SignalPattern.Count);
			// //indiTachAur.BuySignalStopLine.GetValueAt(dataIndex)
			// Print(string.Format("indiTachAur.BuySignalStopLine.ToString"));
			// Print(indiTachAur.BuySignalStopLine.ToString());
			// Print(string.Format("indiTachAur.BuySignalStopLine.Count"));
			// Print(indiTachAur.BuySignalStopLine.Count);
			// //indiTachAur.SellSignalStopLine.GetValueAt(dataIndex)
			// Print(string.Format("indiTachAur.SellSignalStopLine.ToString"));
			// Print(indiTachAur.SellSignalStopLine.ToString());
			// Print(string.Format("indiTachAur.SellSignalStopLine.Count"));
			// Print(indiTachAur.SellSignalStopLine.Count);
			// //indiTachAur.DotPrice.GetValueAt(dataIndex)
			// Print(string.Format("indiTachAur.DotPrice.ToString"));
			// Print(indiTachAur.DotPrice.ToString());
			// Print(string.Format("indiTachAur.DotPrice.Count"));
			// Print(indiTachAur.DotPrice.Count);
			// //indiTachAur.OpenPrice.GetValueAt(dataIndex)
			// Print(string.Format("indiTachAur.OpenPrice.ToString"));
			// Print(indiTachAur.OpenPrice.ToString());
			// Print(string.Format("indiTachAur.OpenPrice.Count"));
			// Print(indiTachAur.OpenPrice.Count);
				
			// Print(string.Format("indiTachAur.BarsToNextSignal.GetValueAt(CurrenBar)"));
			// Print(indiTachAur.BarsToNextSignal.GetValueAt(CurrentBar));