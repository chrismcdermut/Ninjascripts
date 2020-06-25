// 
// Copyright (C) 2016, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Xml.Serialization;
#endregion

// Add this to your declarations to use StreamWriter
using System.IO;

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class SampleStreamWriter : Indicator
	{
		private string path;
		private StreamWriter sw; // a variable for the StreamWriter that will be used 
		
		protected override void OnStateChange()
		{
			if(State == State.SetDefaults)
			{
				Calculate 		= Calculate.OnBarClose;
				Name			= "Sample stream writer";
				path 			= NinjaTrader.Core.Globals.UserDataDir + "MyTestFile.txt"; // Define the Path to our test file
			}
			// Necessary to call in order to clean up resources used by the StreamWriter object
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
			sw = File.AppendText(path);  // Open the path for writing
			sw.WriteLine(Time[0] + "," + Open[0] + "," + High[0] + "," + Low[0] + "," + Close[0]); // Append a new line to the file
			sw.Close(); // Close the file to allow future calls to access the file again.
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SampleStreamWriter[] cacheSampleStreamWriter;
		public SampleStreamWriter SampleStreamWriter()
		{
			return SampleStreamWriter(Input);
		}

		public SampleStreamWriter SampleStreamWriter(ISeries<double> input)
		{
			if (cacheSampleStreamWriter != null)
				for (int idx = 0; idx < cacheSampleStreamWriter.Length; idx++)
					if (cacheSampleStreamWriter[idx] != null &&  cacheSampleStreamWriter[idx].EqualsInput(input))
						return cacheSampleStreamWriter[idx];
			return CacheIndicator<SampleStreamWriter>(new SampleStreamWriter(), input, ref cacheSampleStreamWriter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SampleStreamWriter SampleStreamWriter()
		{
			return indicator.SampleStreamWriter(Input);
		}

		public Indicators.SampleStreamWriter SampleStreamWriter(ISeries<double> input )
		{
			return indicator.SampleStreamWriter(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SampleStreamWriter SampleStreamWriter()
		{
			return indicator.SampleStreamWriter(Input);
		}

		public Indicators.SampleStreamWriter SampleStreamWriter(ISeries<double> input )
		{
			return indicator.SampleStreamWriter(input);
		}
	}
}

#endregion
