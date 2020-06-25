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
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class MySharedMethodsIndicator : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description							= @"Enter the description for your new custom Indicator here.";
				Name								= "MySharedMethodsIndicator";
				Calculate							= Calculate.OnBarClose;
				IsOverlay							= false;
				DisplayInDataBox					= true;
				DrawOnPricePanel					= true;
				DrawHorizontalGridLines				= true;
				DrawVerticalGridLines				= true;
				PaintPriceMarkers					= true;
				ScaleJustification					= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive			= true;
			}
			else if (State == State.Historical)
			{
				ClearOutputWindow();
			}
		}

		protected override void OnBarUpdate()
		{
			// this MySharedMethods is in the Indicator namespace
			double median = MySharedMethods.Median(High, Low, 0);
			Print("Median value: " + median);
			
			// this MySharedMethods is in the Addons namespace
			// we set this in the indicator and then print the value from the strategy
			NinjaTrader.NinjaScript.AddOns.MySharedMethods.SharedDouble = 5;
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private MySharedMethodsIndicator[] cacheMySharedMethodsIndicator;
		public MySharedMethodsIndicator MySharedMethodsIndicator()
		{
			return MySharedMethodsIndicator(Input);
		}

		public MySharedMethodsIndicator MySharedMethodsIndicator(ISeries<double> input)
		{
			if (cacheMySharedMethodsIndicator != null)
				for (int idx = 0; idx < cacheMySharedMethodsIndicator.Length; idx++)
					if (cacheMySharedMethodsIndicator[idx] != null &&  cacheMySharedMethodsIndicator[idx].EqualsInput(input))
						return cacheMySharedMethodsIndicator[idx];
			return CacheIndicator<MySharedMethodsIndicator>(new MySharedMethodsIndicator(), input, ref cacheMySharedMethodsIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MySharedMethodsIndicator MySharedMethodsIndicator()
		{
			return indicator.MySharedMethodsIndicator(Input);
		}

		public Indicators.MySharedMethodsIndicator MySharedMethodsIndicator(ISeries<double> input )
		{
			return indicator.MySharedMethodsIndicator(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MySharedMethodsIndicator MySharedMethodsIndicator()
		{
			return indicator.MySharedMethodsIndicator(Input);
		}

		public Indicators.MySharedMethodsIndicator MySharedMethodsIndicator(ISeries<double> input )
		{
			return indicator.MySharedMethodsIndicator(input);
		}
	}
}

#endregion
