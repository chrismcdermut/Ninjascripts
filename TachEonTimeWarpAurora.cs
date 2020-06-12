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
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;

#endregion



#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		
		private TachEon.TachEonTimeWarpAurora[] cacheTachEonTimeWarpAurora;
		private TachEon.TachEonTimeWarpAurora_BH[] cacheTachEonTimeWarpAurora_BH;
		private TachEon.TachEonTimeWarpPanelAurora[] cacheTachEonTimeWarpPanelAurora;

		
		public TachEon.TachEonTimeWarpAurora TachEonTimeWarpAurora()
		{
			return TachEonTimeWarpAurora(Input);
		}

		public TachEon.TachEonTimeWarpAurora_BH TachEonTimeWarpAurora_BH()
		{
			return TachEonTimeWarpAurora_BH(Input);
		}

		public TachEon.TachEonTimeWarpPanelAurora TachEonTimeWarpPanelAurora()
		{
			return TachEonTimeWarpPanelAurora(Input);
		}


		
		public TachEon.TachEonTimeWarpAurora TachEonTimeWarpAurora(ISeries<double> input)
		{
			if (cacheTachEonTimeWarpAurora != null)
				for (int idx = 0; idx < cacheTachEonTimeWarpAurora.Length; idx++)
					if ( cacheTachEonTimeWarpAurora[idx].EqualsInput(input))
						return cacheTachEonTimeWarpAurora[idx];
			return CacheIndicator<TachEon.TachEonTimeWarpAurora>(new TachEon.TachEonTimeWarpAurora(), input, ref cacheTachEonTimeWarpAurora);
		}

		public TachEon.TachEonTimeWarpAurora_BH TachEonTimeWarpAurora_BH(ISeries<double> input)
		{
			if (cacheTachEonTimeWarpAurora_BH != null)
				for (int idx = 0; idx < cacheTachEonTimeWarpAurora_BH.Length; idx++)
					if ( cacheTachEonTimeWarpAurora_BH[idx].EqualsInput(input))
						return cacheTachEonTimeWarpAurora_BH[idx];
			return CacheIndicator<TachEon.TachEonTimeWarpAurora_BH>(new TachEon.TachEonTimeWarpAurora_BH(), input, ref cacheTachEonTimeWarpAurora_BH);
		}

		public TachEon.TachEonTimeWarpPanelAurora TachEonTimeWarpPanelAurora(ISeries<double> input)
		{
			if (cacheTachEonTimeWarpPanelAurora != null)
				for (int idx = 0; idx < cacheTachEonTimeWarpPanelAurora.Length; idx++)
					if ( cacheTachEonTimeWarpPanelAurora[idx].EqualsInput(input))
						return cacheTachEonTimeWarpPanelAurora[idx];
			return CacheIndicator<TachEon.TachEonTimeWarpPanelAurora>(new TachEon.TachEonTimeWarpPanelAurora(), input, ref cacheTachEonTimeWarpPanelAurora);
		}

	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		
		public Indicators.TachEon.TachEonTimeWarpAurora TachEonTimeWarpAurora()
		{
			return indicator.TachEonTimeWarpAurora(Input);
		}

		public Indicators.TachEon.TachEonTimeWarpAurora_BH TachEonTimeWarpAurora_BH()
		{
			return indicator.TachEonTimeWarpAurora_BH(Input);
		}

		public Indicators.TachEon.TachEonTimeWarpPanelAurora TachEonTimeWarpPanelAurora()
		{
			return indicator.TachEonTimeWarpPanelAurora(Input);
		}


		
		public Indicators.TachEon.TachEonTimeWarpAurora TachEonTimeWarpAurora(ISeries<double> input )
		{
			return indicator.TachEonTimeWarpAurora(input);
		}

		public Indicators.TachEon.TachEonTimeWarpAurora_BH TachEonTimeWarpAurora_BH(ISeries<double> input )
		{
			return indicator.TachEonTimeWarpAurora_BH(input);
		}

		public Indicators.TachEon.TachEonTimeWarpPanelAurora TachEonTimeWarpPanelAurora(ISeries<double> input )
		{
			return indicator.TachEonTimeWarpPanelAurora(input);
		}
	
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		
		public Indicators.TachEon.TachEonTimeWarpAurora TachEonTimeWarpAurora()
		{
			return indicator.TachEonTimeWarpAurora(Input);
		}

		public Indicators.TachEon.TachEonTimeWarpAurora_BH TachEonTimeWarpAurora_BH()
		{
			return indicator.TachEonTimeWarpAurora_BH(Input);
		}

		public Indicators.TachEon.TachEonTimeWarpPanelAurora TachEonTimeWarpPanelAurora()
		{
			return indicator.TachEonTimeWarpPanelAurora(Input);
		}


		
		public Indicators.TachEon.TachEonTimeWarpAurora TachEonTimeWarpAurora(ISeries<double> input )
		{
			return indicator.TachEonTimeWarpAurora(input);
		}

		public Indicators.TachEon.TachEonTimeWarpAurora_BH TachEonTimeWarpAurora_BH(ISeries<double> input )
		{
			return indicator.TachEonTimeWarpAurora_BH(input);
		}

		public Indicators.TachEon.TachEonTimeWarpPanelAurora TachEonTimeWarpPanelAurora(ISeries<double> input )
		{
			return indicator.TachEonTimeWarpPanelAurora(input);
		}

	}
}

#endregion
