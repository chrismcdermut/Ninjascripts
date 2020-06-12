// 
// Copyright (C) 2020, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel;
using NinjaTrader;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.BarsTypes
{
	public class VolumeBarsType : BarsType
	{
		public override void ApplyDefaultBasePeriodValue(BarsPeriod period) {}

		public override void ApplyDefaultValue(BarsPeriod period)
		{
			period.Value = 1000;
		}

		public override string ChartLabel(DateTime time)
		{
			return time.ToString("T", Core.Globals.GeneralOptions.CurrentCulture);
		}

		public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack) { return 1; }
	
		public override double GetPercentComplete(Bars bars, DateTime now)
		{
			return bars.Count == 0 ? 0 : (double) bars.GetVolume(bars.Count - 1) / bars.BarsPeriod.Value;
		}

		protected override void OnDataPoint(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isBar, double bid, double ask)
		{
			if (SessionIterator == null)
				SessionIterator = new SessionIterator(bars);

			bool isNewSession = SessionIterator.IsNewSession(time, isBar);
			if (isNewSession)
				SessionIterator.GetNextSession(time, isBar);

			long barsPeriodValue = bars.BarsPeriod.Value;
			if (bars.Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency)
				barsPeriodValue = Core.Globals.FromCryptocurrencyVolume(bars.BarsPeriod.Value);

			if (bars.Count == 0)
			{
				while (volume > barsPeriodValue)
				{
					AddBar(bars, open, high, low, close, time, barsPeriodValue);
					volume -= barsPeriodValue;
				}
				if (volume > 0)
					AddBar(bars, open, high, low, close, time, volume);
			}
			else
			{
				long volumeTmp = 0;
				if (!bars.IsResetOnNewTradingDay || !isNewSession)
				{
					volumeTmp = Math.Min(barsPeriodValue - bars.GetVolume(bars.Count - 1), volume);
					if (volumeTmp > 0)
						UpdateBar(bars, high, low, close, time, volumeTmp);
				}

				volumeTmp = volume - volumeTmp;
				while (volumeTmp > 0)
				{
					AddBar(bars, open, high, low, close, time, Math.Min(volumeTmp, barsPeriodValue));
					volumeTmp -= barsPeriodValue;
				}
			}
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name			= Custom.Resource.NinjaScriptBarsTypeVolume;
				BarsPeriod		= new BarsPeriod { BarsPeriodType = BarsPeriodType.Volume, Value = 1000 };
				BuiltFrom		= BarsPeriodType.Tick;
				DaysToLoad		= 3;
				IsIntraday		= true;
				IsTimeBased		= false;
			}
			else if (State == State.Configure)
			{
				Name = string.Format(Core.Globals.GeneralOptions.CurrentCulture, Custom.Resource.DataBarsTypeVolume, BarsPeriod.Value, (BarsPeriod.MarketDataType != MarketDataType.Last ? string.Format(" - {0}", Core.Globals.ToLocalizedObject(BarsPeriod.MarketDataType, Core.Globals.GeneralOptions.CurrentUICulture)) : string.Empty));

				Properties.Remove(Properties.Find("BaseBarsPeriodType",			true));
				Properties.Remove(Properties.Find("BaseBarsPeriodValue",		true));
				Properties.Remove(Properties.Find("PointAndFigurePriceType",	true));
				Properties.Remove(Properties.Find("ReversalType",				true));
				Properties.Remove(Properties.Find("Value2",						true));
			}
		}
	}
}
