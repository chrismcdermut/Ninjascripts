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
	public class SecondBarsType : BarsType
	{
		public override void ApplyDefaultBasePeriodValue(BarsPeriod period) {}

		public override void ApplyDefaultValue(BarsPeriod period)
		{
			period.Value = 30;
		}

		public override string ChartLabel(DateTime time) { return time.ToString("HH:mm:ss"); }

		public override int GetInitialLookBackDays(BarsPeriod period, TradingHours tradingHours, int barsBack)
		{ 
			return (int) Math.Max(1, Math.Ceiling(barsBack / Math.Max(1, 8.0 * 60 * 60 / period.Value)) * 7.0 / 5.0);
		}

		public override double GetPercentComplete(Bars bars, DateTime now)
		{
			return now <= bars.LastBarTime ? 1.0 - bars.LastBarTime.Subtract(now).TotalSeconds / bars.BarsPeriod.Value : 1;
		}

		protected override void OnDataPoint(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isBar, double bid, double ask)
		{
			if (SessionIterator == null)
				SessionIterator = new SessionIterator(bars);

			if (bars.Count == 0 || time >= bars.LastBarTime)
			{
				DateTime barTime = TimeToBarTime(bars, time, isBar);
				AddBar(bars, open, high, low, close, barTime, volume);
			}
			else
				UpdateBar(bars, high, low, close, bars.LastBarTime, volume);
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name			= Custom.Resource.NinjaScriptBarsTypeSecond;
				BarsPeriod		= new BarsPeriod { BarsPeriodType = BarsPeriodType.Second };
				BuiltFrom		= BarsPeriodType.Tick;
				DaysToLoad		= 3;
				IsIntraday		= true;
				IsTimeBased		= true;
			}
			else if (State == State.Configure)
			{
				Name = string.Format("{0}{1}", string.Format(Core.Globals.GeneralOptions.CurrentCulture, Custom.Resource.DataBarsTypeSecond, BarsPeriod.Value), BarsPeriod.MarketDataType != MarketDataType.Last ? string.Format(" - {0}", Core.Globals.ToLocalizedObject(BarsPeriod.MarketDataType, Core.Globals.GeneralOptions.CurrentUICulture)) : string.Empty);

				Properties.Remove(Properties.Find("BaseBarsPeriodType",			true));
				Properties.Remove(Properties.Find("BaseBarsPeriodValue",		true));
				Properties.Remove(Properties.Find("PointAndFigurePriceType",	true));
				Properties.Remove(Properties.Find("ReversalType",				true));
				Properties.Remove(Properties.Find("Value2",						true));
			}
		}

		private DateTime TimeToBarTime(Bars bars, DateTime time, bool isBar)
		{
			if (SessionIterator.IsNewSession(time, isBar))
				SessionIterator.GetNextSession(time, isBar);

			if (bars.IsResetOnNewTradingDay || !bars.IsResetOnNewTradingDay && bars.Count == 0)
			{
				DateTime barTimeStamp = isBar
					? SessionIterator.ActualSessionBegin.AddSeconds(Math.Ceiling(Math.Ceiling(Math.Max(0, time.Subtract(SessionIterator.ActualSessionBegin).TotalSeconds)) / bars.BarsPeriod.Value) * bars.BarsPeriod.Value)
					: SessionIterator.ActualSessionBegin.AddSeconds(bars.BarsPeriod.Value + Math.Floor(Math.Floor(Math.Max(0, time.Subtract(SessionIterator.ActualSessionBegin).TotalSeconds)) / bars.BarsPeriod.Value) * bars.BarsPeriod.Value);
				if (bars.TradingHours.Sessions.Count > 0 && barTimeStamp > SessionIterator.ActualSessionEnd) // Cut last bar in session down to session end on odd session end time
					barTimeStamp = SessionIterator.ActualSessionEnd;
				return barTimeStamp;
			}
			else
			{
				DateTime lastBarTime	= bars.GetTime(bars.Count - 1);
				DateTime barTimeStamp	= isBar 
					? lastBarTime.AddSeconds(Math.Ceiling(Math.Ceiling(Math.Max(0, time.Subtract(lastBarTime).TotalSeconds)) / bars.BarsPeriod.Value) * bars.BarsPeriod.Value)
					: lastBarTime.AddSeconds(bars.BarsPeriod.Value + Math.Floor(Math.Floor(Math.Max(0, time.Subtract(lastBarTime).TotalSeconds)) / bars.BarsPeriod.Value) * bars.BarsPeriod.Value);
				if (bars.TradingHours.Sessions.Count > 0 && barTimeStamp > SessionIterator.ActualSessionEnd)
				{
					DateTime saveActualSessionEnd = SessionIterator.ActualSessionEnd;
					SessionIterator.GetNextSession(SessionIterator.ActualSessionEnd.AddSeconds(1), isBar);
					barTimeStamp = SessionIterator.ActualSessionBegin.AddSeconds((int) barTimeStamp.Subtract(saveActualSessionEnd).TotalSeconds);
				}
				return barTimeStamp;
			}
		}
	}
}
