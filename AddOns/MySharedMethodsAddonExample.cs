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
using NinjaTrader.Gui.Tools;
#endregion

// The MySharedMethodsAddonExample is created 3 times for the addons, indicators, and strategies namespaces.
// This script uses the same class name 3 times instead of different class names to demonstrate that the namespace keeps these classes separate.
namespace NinjaTrader.NinjaScript.AddOns
{
	public partial class MySharedMethods : NinjaTrader.NinjaScript.AddOnBase
	{
		// This double can be accessed from within another addon with MySharedMethods.SharedDouble
		// or can be accessed from any script using NinjaTrader.NinjaScript.AddOns.MySharedMethods.SharedDouble
		public static double SharedDouble
		{ get; set; }
	}
}

// This namespace holds the classes for indicators
namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator
	{
		// This double can be accessed from within any indicator with CalculateDelta()
		// or can be accessed from any script using NinjaTrader.NinjaScript.Indicators.CalculateDelta()
		public static double CalculateDelta(double firstPrice, double secondPrice)
		{
			return Math.Abs(firstPrice - secondPrice);
		}
		
		// inner nested organizing class
		public class MySharedMethods
		{
			// This double can be accessed from within any indicator with MySharedMethods.Median()
			// or can be accessed from any script using NinjaTrader.NinjaScript.Indicators.MySharedMethods.Median()
			public static double Median(ISeries<double> high, ISeries<double> low, int barsAgo)
			{
				return (((high[barsAgo] - low[barsAgo]) / 2) + low[barsAgo]);
			}
		}
	}
}

// This namespace holds the classes for strategies
namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy
	{
		// This double can be accessed from within any strategy with PrintPositionInfo()
		// or can be accessed from any script using NinjaTrader.NinjaScript.Strategies.PrintPositionInfo()
		public static void PrintPositionInfo(Position position)
        {
            NinjaTrader.Code.Output.Process(String.Format("{0}: {1} {2} at {3}", position.Instrument, position.Quantity, position.MarketPosition, position.AveragePrice), PrintTo.OutputTab1);
        }

		// inner nested organizing class
		public class MySharedMethods
		{
			// This double can be accessed from within any strategy with MySharedMethods.PrintPositionInfo()
			// or can be accessed from any script using NinjaTrader.NinjaScript.Strategies.MySharedMethods.PrintPositionInfo()
			public static void PrintPositionInfo(Position position)
			{
				NinjaTrader.Code.Output.Process(String.Format("{0}: {1} {2} at {3}", position.Instrument, position.Quantity, position.MarketPosition, position.AveragePrice), PrintTo.OutputTab1);
			}
		}
	}
}
