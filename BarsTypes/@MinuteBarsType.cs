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
	public class MinuteBarsType : BarsType
	{
		public override void ApplyDefaultBasePeriodValue(BarsPeriod period) {}

		public override void ApplyDefaultValue(BarsPeriod period)
		{
			period.Value = 1;
		}

		public override string ChartLabel(DateTime time) { return time.ToString("HH:mm"); }

		public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack)
		{ 
			int minutesPerWeek = 0; 
			lock (tradingHours.Sessions)
			{
				foreach (Session session in tradingHours.Sessions)
				{
					int beginDay	= (int) session.BeginDay;
					int endDay		= (int) session.EndDay;
					if (beginDay > endDay)
						endDay += 7;

					minutesPerWeek += (endDay - beginDay) * 1440 + ((session.EndTime / 100) * 60 + (session.EndTime % 100)) - ((session.BeginTime / 100) * 60 + (session.BeginTime % 100));
				}
			}

			return (int) Math.Max(1, Math.Ceiling(barsBack / Math.Max(1, minutesPerWeek / 7.0 / barsPeriod.Value) * 1.05));
		}

		public override double GetPercentComplete(Bars bars, DateTime now)
		{
			return now <= bars.LastBarTime ? 1.0 - (bars.LastBarTime.Subtract(now).TotalMinutes / bars.BarsPeriod.Value) : 1;
		}

		protected override void OnDataPoint(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isBar, double bid, double ask)
		{
			if (SessionIterator == null)
				SessionIterator = new SessionIterator(bars);

			if (bars.Count == 0)
				AddBar(bars, open, high, low, close, TimeToBarTime(bars, time, isBar), volume);
			else if (!isBar && time < bars.LastBarTime)
				UpdateBar(bars, high, low, close, bars.LastBarTime, volume);
			else if (isBar && time <= bars.LastBarTime)
				UpdateBar(bars, high, low, close, bars.LastBarTime, volume); 
			else
			{
				time		= TimeToBarTime(bars, time, isBar);
				AddBar(bars, open, high, low, close, time, volume);
			}
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name			= Custom.Resource.NinjaScriptBarsTypeMinute;
				BarsPeriod		= new BarsPeriod { BarsPeriodType = BarsPeriodType.Minute, Value = 1 };
				BuiltFrom		= BarsPeriodType.Minute;
				DaysToLoad		= 5;
				IsIntraday		= true;
				IsTimeBased		= true;
			}
			else if (State == State.Configure)
			{
				Name = string.Format(Core.Globals.GeneralOptions.CurrentCulture, Custom.Resource.DataBarsTypeMinute, BarsPeriod.Value, (BarsPeriod.MarketDataType != MarketDataType.Last ? string.Format(" - {0}", Core.Globals.ToLocalizedObject(BarsPeriod.MarketDataType, Core.Globals.GeneralOptions.CurrentUICulture)) : string.Empty));

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

			if (bars.IsResetOnNewTradingDay || (!bars.IsResetOnNewTradingDay && bars.Count == 0))
			{
				DateTime barTimeStamp = isBar
					? SessionIterator.ActualSessionBegin.AddMinutes(Math.Ceiling(Math.Ceiling(Math.Max(0, time.Subtract(SessionIterator.ActualSessionBegin).TotalMinutes)) / bars.BarsPeriod.Value) * bars.BarsPeriod.Value)
					: SessionIterator.ActualSessionBegin.AddMinutes(bars.BarsPeriod.Value + Math.Floor(Math.Floor(Math.Max(0, time.Subtract(SessionIterator.ActualSessionBegin).TotalMinutes)) / bars.BarsPeriod.Value) * bars.BarsPeriod.Value);
				if (bars.TradingHours.Sessions.Count > 0 && barTimeStamp > SessionIterator.ActualSessionEnd) // Cut last bar in session down to session end on odd session end time
					barTimeStamp = SessionIterator.ActualSessionEnd;
				return barTimeStamp;
			}
			else
			{
				DateTime lastBarTime	= bars.GetTime(bars.Count - 1);
				DateTime barTimeStamp	= isBar 
					? lastBarTime.AddMinutes(Math.Ceiling(Math.Ceiling(Math.Max(0, time.Subtract(lastBarTime).TotalMinutes)) / bars.BarsPeriod.Value) * bars.BarsPeriod.Value)
					: lastBarTime.AddMinutes(bars.BarsPeriod.Value + Math.Floor(Math.Floor(Math.Max(0, time.Subtract(lastBarTime).TotalMinutes)) / bars.BarsPeriod.Value) * bars.BarsPeriod.Value);
				if (bars.TradingHours.Sessions.Count > 0 && barTimeStamp > SessionIterator.ActualSessionEnd)
				{
					DateTime saveActualSessionEnd = SessionIterator.ActualSessionEnd;
					SessionIterator.GetNextSession(SessionIterator.ActualSessionEnd.AddSeconds(1), isBar);
					barTimeStamp = SessionIterator.ActualSessionBegin.AddMinutes((int) barTimeStamp.Subtract(saveActualSessionEnd).TotalMinutes);
				}
				return barTimeStamp;
			}
		}
	}
}
