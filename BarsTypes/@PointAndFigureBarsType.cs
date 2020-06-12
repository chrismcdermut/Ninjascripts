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
	public class PointAndFigureBarsType : BarsType
	{
		private	enum	Trend			{ Up, Down, Undetermined }

		private			double			anchorPrice				= double.MinValue;
		private			double			boxSize					= double.MinValue;
		private			bool			endOfBar;
		private			DateTime		prevTime				= Core.Globals.MinDate;
		private			DateTime		prevTimeD				= Core.Globals.MinDate;
		private			double			reversalSize			= double.MinValue;
		private			int				tmpCount;
		private			int				tmpDayCount;
		private			double			tmpHigh					= double.MinValue;
		private			double			tmpLow					= double.MinValue;
		private			int				tmpTickCount;
		private			DateTime		tmpTime					= Core.Globals.MinDate;
		private			long			tmpVolume;
		private			Trend			trend					= Trend.Undetermined;
		private			long			volumeCount;

		public override void ApplyDefaultBasePeriodValue(BarsPeriod period)
		{
			switch (period.BaseBarsPeriodType)
			{
				case BarsPeriodType.Day		: period.BaseBarsPeriodValue = 1;		DaysToLoad = 365;	break;
				case BarsPeriodType.Minute	: period.BaseBarsPeriodValue = 1;		DaysToLoad = 5;		break;
				case BarsPeriodType.Month	: period.BaseBarsPeriodValue = 1;		DaysToLoad = 5475;	break;
				case BarsPeriodType.Second	: period.BaseBarsPeriodValue = 30;		DaysToLoad = 3;		break;
				case BarsPeriodType.Tick	: period.BaseBarsPeriodValue = 150;		DaysToLoad = 3;		break;
				case BarsPeriodType.Volume	: period.BaseBarsPeriodValue = 1000;	DaysToLoad = 3;		break;
				case BarsPeriodType.Week	: period.BaseBarsPeriodValue = 1;		DaysToLoad = 1825;	break;
				case BarsPeriodType.Year	: period.BaseBarsPeriodValue = 1;		DaysToLoad = 15000;	break;
			}
		}

		public override void ApplyDefaultValue(BarsPeriod period)
		{
			period.Value	= 2;
			period.Value2	= 3;
		}

		private void CalculatePfBar(Bars bars, double h, double l, double c, DateTime barTime, DateTime tTime)
		{
			if (BarsPeriod.PointAndFigurePriceType == PointAndFigurePriceType.Close)
			{
				switch (trend)
				{
					case Trend.Up:
						if (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, anchorPrice - reversalSize) <= 0)
						{
							double newHigh	= anchorPrice - boxSize;
							double newLow	= anchorPrice - reversalSize;
							while (bars.Instrument.MasterInstrument.Compare(newLow - boxSize, bars.LastPrice) >= 0) newLow -= boxSize;
							newHigh			= bars.Instrument.MasterInstrument.RoundToTickSize(newHigh);
							newLow			= bars.Instrument.MasterInstrument.RoundToTickSize(newLow);
							anchorPrice		= newLow;
							trend			= Trend.Down;
							AddBar(bars, newHigh, newHigh, newLow, newLow, barTime, volumeCount);
						}
						else
						{
							if (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, anchorPrice + boxSize) >= 0)
							{
								double newHigh	= anchorPrice + boxSize;
								while (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, newHigh + boxSize) >= 0) newHigh += boxSize;
								newHigh			= bars.Instrument.MasterInstrument.RoundToTickSize(newHigh);
								anchorPrice		= newHigh;
								UpdateBar(bars, newHigh, l, newHigh, barTime, volumeCount);
							}
							else
								UpdateBar(bars, h, l, c, barTime, volumeCount);
						}
						break;
					case Trend.Down:
						if (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, anchorPrice + reversalSize) >= 0)
						{
							double newLow	= anchorPrice + boxSize;
							double newHigh	= anchorPrice + reversalSize;
							while (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, newHigh + boxSize) >= 0) newHigh += boxSize;
							newHigh			= bars.Instrument.MasterInstrument.RoundToTickSize(newHigh);
							newLow			= bars.Instrument.MasterInstrument.RoundToTickSize(newLow);
							anchorPrice		= newHigh;
							trend			= Trend.Up;
							AddBar(bars, newLow, newHigh, newLow, newHigh, barTime, volumeCount);
						}
						else
						{
							if (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, anchorPrice - boxSize) <= 0)
							{
								double newLow	= anchorPrice - boxSize;
								while (bars.Instrument.MasterInstrument.Compare(newLow - boxSize, bars.LastPrice) >= 0) newLow -= boxSize;
								newLow			= bars.Instrument.MasterInstrument.RoundToTickSize(newLow);
								anchorPrice		= newLow;
								UpdateBar(bars, h, newLow, newLow, barTime, volumeCount);
							}
							else
								UpdateBar(bars, h, l, c, barTime, volumeCount);
						}
						break;
					default:
						if (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, anchorPrice + boxSize) >= 0)
						{
							double newHigh	= anchorPrice + boxSize;
							while (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, newHigh + boxSize) >= 0) newHigh += boxSize;
							newHigh			= bars.Instrument.MasterInstrument.RoundToTickSize(newHigh);
							anchorPrice		= newHigh;
							trend			= Trend.Up;
							UpdateBar(bars, newHigh, l, newHigh, barTime, volumeCount);
						}
						else
						{
							if (bars.Instrument.MasterInstrument.Compare(anchorPrice - boxSize, bars.LastPrice) >= 0)
							{
								double newLow	= anchorPrice - boxSize;
								while (bars.Instrument.MasterInstrument.Compare(newLow - boxSize, bars.LastPrice) >= 0) newLow -= boxSize;
								newLow			= bars.Instrument.MasterInstrument.RoundToTickSize(newLow);
								anchorPrice		= newLow;
								trend			= Trend.Down;
								UpdateBar(bars, h, newLow, newLow, barTime, volumeCount);
							}
							else
								UpdateBar(bars, anchorPrice, anchorPrice, anchorPrice, barTime, volumeCount);
						}
						break;
				}
			}
			else
			{
				switch (trend)
				{
					case Trend.Up:
						bool updatedUp = false;
						if (bars.Instrument.MasterInstrument.Compare(tmpHigh, anchorPrice + boxSize) >= 0)
						{
							double newHigh	= anchorPrice;
							while (bars.Instrument.MasterInstrument.Compare(tmpHigh, newHigh + boxSize) >= 0) newHigh += boxSize;
							newHigh			= bars.Instrument.MasterInstrument.RoundToTickSize(newHigh);
							updatedUp		= true;
							anchorPrice		= newHigh;
							long vol		= bars.Instrument.MasterInstrument.Compare(anchorPrice - reversalSize, tmpLow) >= 0 ? 0 : volumeCount;
							DateTime tt		= bars.Instrument.MasterInstrument.Compare(anchorPrice - reversalSize, tmpLow) >= 0 ? tTime : barTime;
							UpdateBar(bars, newHigh, l, newHigh, tt, vol);
						}
						if (bars.Instrument.MasterInstrument.Compare(anchorPrice - reversalSize, tmpLow) >= 0)
						{
							double newHigh	= anchorPrice - boxSize;
							double newLow	= anchorPrice - reversalSize;
							while (bars.Instrument.MasterInstrument.Compare(newLow - boxSize, tmpLow) >= 0) newLow -= boxSize;
							newHigh			= bars.Instrument.MasterInstrument.RoundToTickSize(newHigh);
							newLow			= bars.Instrument.MasterInstrument.RoundToTickSize(newLow);
							updatedUp		= true;
							anchorPrice		= newLow;
							trend			= Trend.Down;
							AddBar(bars, newHigh, newHigh, newLow, newLow, barTime, volumeCount);
						}
						if (!updatedUp)
						{
							UpdateBar(bars, h, l, c, barTime, volumeCount);
							anchorPrice = h;
						}
						break;
					case Trend.Down:
						bool updatedDn = false;
						if (bars.Instrument.MasterInstrument.Compare(tmpLow, anchorPrice - boxSize) <= 0)
						{
							double newLow	= anchorPrice;
							while (bars.Instrument.MasterInstrument.Compare(newLow - boxSize, tmpLow) >= 0) newLow -= boxSize;
							newLow			= bars.Instrument.MasterInstrument.RoundToTickSize(newLow);
							updatedDn		= true;
							anchorPrice		= newLow;
							long vol		= bars.Instrument.MasterInstrument.Compare(tmpHigh, anchorPrice + reversalSize) >= 0 ? 0 : volumeCount;
							DateTime tt		= bars.Instrument.MasterInstrument.Compare(anchorPrice - reversalSize, tmpLow) >= 0 ? tTime : barTime;
							UpdateBar(bars, h, newLow, newLow, tt, vol);
						}
						if (bars.Instrument.MasterInstrument.Compare(tmpHigh, anchorPrice + reversalSize) >= 0)
						{
							double newLow	= anchorPrice + boxSize;
							double newHigh	= anchorPrice + reversalSize;
							while (bars.Instrument.MasterInstrument.Compare(tmpHigh, newHigh + boxSize) >= 0) newHigh += boxSize;
							newHigh			= bars.Instrument.MasterInstrument.RoundToTickSize(newHigh);
							newLow			= bars.Instrument.MasterInstrument.RoundToTickSize(newLow);
							updatedDn		= true;
							anchorPrice		= newHigh;
							trend			= Trend.Up;
							AddBar(bars, newLow, newHigh, newLow, newHigh, barTime, volumeCount);
						}
						if (!updatedDn)
						{
							UpdateBar(bars, h, l, c, barTime, volumeCount);
							anchorPrice = l;
						}
						break;
					default:
						if (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, anchorPrice + boxSize) >= 0)
						{
							double newHigh	= anchorPrice + boxSize;
							while (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, newHigh + boxSize) >= 0) newHigh += boxSize;
							newHigh			= bars.Instrument.MasterInstrument.RoundToTickSize(newHigh);
							anchorPrice		= newHigh;
							trend			= Trend.Up;
							UpdateBar(bars, newHigh, l, newHigh, barTime, volumeCount);
						}
						else
						{
							if (bars.Instrument.MasterInstrument.Compare(anchorPrice - boxSize, bars.LastPrice) >= 0)
							{
								double newLow	= anchorPrice - boxSize;
								while (bars.Instrument.MasterInstrument.Compare(newLow - boxSize, bars.LastPrice) >= 0) newLow -= boxSize;
								newLow			= bars.Instrument.MasterInstrument.RoundToTickSize(newLow);
								anchorPrice		= newLow;
								trend			= Trend.Down;
								UpdateBar(bars, h, newLow, newLow, barTime, volumeCount);
							}
							else
								UpdateBar(bars, anchorPrice, anchorPrice, anchorPrice, barTime, volumeCount);
						}
						break;
				}
			}
		}

		public override string ChartLabel(DateTime time)
		{
			switch (BarsPeriod.BaseBarsPeriodType)
			{
				case BarsPeriodType.Day		: return BarsTypeDay.ChartLabel(time);
				case BarsPeriodType.Minute	: return BarsTypeMinute.ChartLabel(time);
				case BarsPeriodType.Month	: return BarsTypeMonth.ChartLabel(time);
				case BarsPeriodType.Second	: return BarsTypeSecond.ChartLabel(time);
				case BarsPeriodType.Tick	: return BarsTypeTick.ChartLabel(time);
				case BarsPeriodType.Volume	: return BarsTypeTick.ChartLabel(time);
				case BarsPeriodType.Week	: return BarsTypeDay.ChartLabel(time);
				case BarsPeriodType.Year	: return BarsTypeYear.ChartLabel(time);
				default						: return BarsTypeDay.ChartLabel(time);
			}
		}

		public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack)
		{
				switch (BarsPeriod.BaseBarsPeriodType)
				{
					case BarsPeriodType.Day		: return new DayBarsType()		.GetInitialLookBackDays(barsPeriod, tradingHours, barsBack);
					case BarsPeriodType.Minute	: return new MinuteBarsType()	.GetInitialLookBackDays(barsPeriod, tradingHours, barsBack);
					case BarsPeriodType.Month	: return new MonthBarsType()	.GetInitialLookBackDays(barsPeriod, tradingHours, barsBack);
					case BarsPeriodType.Second	: return new SecondBarsType()	.GetInitialLookBackDays(barsPeriod, tradingHours, barsBack);
					case BarsPeriodType.Tick	: return new TickBarsType()		.GetInitialLookBackDays(barsPeriod, tradingHours, barsBack);
					case BarsPeriodType.Volume	: return new VolumeBarsType()	.GetInitialLookBackDays(barsPeriod, tradingHours, barsBack);
					case BarsPeriodType.Week	: return new WeekBarsType()		.GetInitialLookBackDays(barsPeriod, tradingHours, barsBack);
					case BarsPeriodType.Year	: return new YearBarsType()		.GetInitialLookBackDays(barsPeriod, tradingHours, barsBack);
					default						: return new MinuteBarsType()	.GetInitialLookBackDays(barsPeriod, tradingHours, barsBack);
				}
		}

		public override double GetPercentComplete(Bars bars, DateTime now) { return 0; }

		protected override void OnDataPoint(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isBar, double bid, double ask)
		{
			if (SessionIterator == null)
				SessionIterator = new SessionIterator(bars);
			bool isNewSession	= SessionIterator.IsNewSession(time, isBar);
			if (isNewSession)
				SessionIterator.GetNextSession(time, isBar);

			#region Building Bars from Base Period

			if (bars.Count != tmpCount) // Reset cache when bars are trimmed
				if (bars.Count == 0)
				{
					tmpTime			= Core.Globals.MinDate;
					tmpVolume		= 0;
					tmpDayCount		= 0;
					tmpTickCount	= 0;
				}
				else
				{
					tmpTime			= bars.GetTime(bars.Count - 1);
					tmpVolume		= bars.GetVolume(bars.Count - 1);
					tmpTickCount	= bars.TickCount;
					tmpDayCount		= bars.DayCount;
					bars.LastPrice	= anchorPrice = bars.GetClose(bars.Count - 1);
				}

			switch (BarsPeriod.BaseBarsPeriodType)
			{
				case BarsPeriodType.Day:
					tmpTime = time.Date;
					if (!isBar)
					{
						tmpDayCount++;
						if (tmpTime < time.Date) tmpTime = time.Date; // Make sure timestamps are ascending
					}

					if (isBar && prevTimeD != tmpTime) tmpDayCount++;

					if (isBar && bars.Count > 0 && tmpTime == bars.LastBarTime.Date
						|| !isBar && bars.Count > 0 && tmpTime <= bars.LastBarTime.Date
						|| tmpDayCount < BarsPeriod.BaseBarsPeriodValue)
						endOfBar = false;
					else
					{
						prevTime	= prevTimeD == Core.Globals.MinDate ? tmpTime : prevTimeD;
						prevTimeD	= tmpTime;
						endOfBar	= true;
					}

					break;

				case BarsPeriodType.Minute:
					if (tmpTime == Core.Globals.MinDate)
						prevTime = tmpTime = TimeToBarTimeMinute(bars, time, isBar);

					if (isBar && time <= tmpTime || !isBar && time < tmpTime)
						endOfBar	= false;
					else
					{
						prevTime	= tmpTime;
						tmpTime		= TimeToBarTimeMinute(bars, time, isBar);
						endOfBar	= true;
					}
					break;

				case BarsPeriodType.Volume:
					if (tmpTime == Core.Globals.MinDate)
					{
						tmpVolume	= volume;
						endOfBar	= tmpVolume >= BarsPeriod.BaseBarsPeriodValue;
						prevTime	= tmpTime = time;
						if (endOfBar) 
							tmpVolume = 0;
						break;
					}

					tmpVolume += volume;
					endOfBar = tmpVolume >= BarsPeriod.BaseBarsPeriodValue;
					if (endOfBar)
					{
						prevTime = tmpTime;
						tmpVolume = 0;
						tmpTime = time;
					}
					break;

				case BarsPeriodType.Month:
					if (tmpTime == Core.Globals.MinDate)
						prevTime	= tmpTime = TimeToBarTimeMonth(time, BarsPeriod.BaseBarsPeriodValue);

					if (time.Month <= tmpTime.Month && time.Year == tmpTime.Year || time.Year < tmpTime.Year)
						endOfBar	= false;
					else
					{
						prevTime	= tmpTime;
						endOfBar	= true;
						tmpTime		= TimeToBarTimeMonth(time, BarsPeriod.BaseBarsPeriodValue);
					}
					break;

				case BarsPeriodType.Second:
					if (tmpTime == Core.Globals.MinDate)
					{
						prevTime = tmpTime = TimeToBarTimeSecond(bars, time, isBar);
					}
					if (time <= tmpTime)
						endOfBar	= false;
					else
					{
						prevTime	= tmpTime;
						tmpTime		= TimeToBarTimeSecond(bars, time, isBar);
						endOfBar	= true;
					}
					break;

				case BarsPeriodType.Tick:
					if (tmpTime == Core.Globals.MinDate || BarsPeriod.BaseBarsPeriodValue == 1)
					{
						prevTime		= tmpTime;
						if (prevTime == Core.Globals.MinDate)
							prevTime = time;
						tmpTime			= time;
						endOfBar		= BarsPeriod.BaseBarsPeriodValue == 1;
						break;
					}

					if (tmpTickCount < BarsPeriod.BaseBarsPeriodValue)
					{
						tmpTime			= time;
						endOfBar		= false;
						tmpTickCount++;
					}
					else
					{
						prevTime		= tmpTime;
						tmpTime			= time;
						endOfBar		= true;
						tmpTickCount	= 1;
					}
					break;

				case BarsPeriodType.Week:
					if (tmpTime == Core.Globals.MinDate)
						prevTime = tmpTime = TimeToBarTimeWeek(time.Date, tmpTime.Date, BarsPeriod.BaseBarsPeriodValue);
					if (time.Date <= tmpTime.Date)
						endOfBar	= false;
					else
					{
						prevTime	= tmpTime;
						endOfBar	= true;
						tmpTime		= TimeToBarTimeWeek(time.Date, tmpTime.Date, BarsPeriod.BaseBarsPeriodValue);
					}
					break;

				case BarsPeriodType.Year:
					if (tmpTime == Core.Globals.MinDate)
						prevTime	= tmpTime = TimeToBarTimeYear(time, BarsPeriod.BaseBarsPeriodValue);
					if (time.Year <= tmpTime.Year)
						endOfBar	= false;
					else
					{
						prevTime	= tmpTime;
						endOfBar	= true;
						tmpTime		= TimeToBarTimeYear(time, BarsPeriod.BaseBarsPeriodValue);
					}
					break;
			}
			#endregion
			#region P&F logic
			double tickSize		= bars.Instrument.MasterInstrument.TickSize;
			boxSize				= Math.Floor(10000000.0 * BarsPeriod.Value * tickSize) / 10000000.0;
			reversalSize		= BarsPeriod.Value2 * boxSize;

			if (bars.Count == 0 || IsIntraday && bars.IsResetOnNewTradingDay && isNewSession)
			{
				if (bars.Count > 0)
				{
					double		lastHigh	= bars.GetHigh(bars.Count - 1);
					double		lastLow		= bars.GetLow(bars.Count - 1);
					double		lastClose	= bars.GetClose(bars.Count - 1);
					DateTime	lastTime	= bars.GetTime(bars.Count - 1);
					bars.LastPrice			= anchorPrice = lastClose;

					if (bars.Count == tmpCount)
						CalculatePfBar(bars, lastHigh, lastLow, lastClose, prevTime == Core.Globals.MinDate ? time : prevTime, lastTime);
				}

				AddBar(bars, close, close, close, close, tmpTime, volume);

				anchorPrice				= close;
				trend					= Trend.Undetermined;
				prevTime				= tmpTime;
				volumeCount				= 0;
				bars.LastPrice			= close;
				tmpCount				= bars.Count;
				tmpHigh					= high;
				tmpLow					= low;
				return;
			}

			double		c		= bars.GetClose(bars.Count - 1);
			double		h		= bars.GetHigh(bars.Count - 1);
			double		l		= bars.GetLow(bars.Count - 1);
			DateTime	t		= bars.GetTime(bars.Count - 1);

			if (endOfBar)
			{
				CalculatePfBar(bars, h, l, c, prevTime, t);
				volumeCount		= volume;
				tmpHigh			= high;
				tmpLow			= low;
			}
			else
			{
				tmpHigh			= high > tmpHigh ? high : tmpHigh;
				tmpLow			= low < tmpLow ? low : tmpLow;
				volumeCount		+= volume;
			}

			bars.LastPrice			= close;
			tmpCount				= bars.Count;
			#endregion
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name						= Custom.Resource.NinjaScriptBarsTypePointAndFigure;
				BarsPeriod					= new BarsPeriod { BarsPeriodType = BarsPeriodType.PointAndFigure };
				DaysToLoad					= 5;
				DefaultChartStyle			= Gui.Chart.ChartStyleType.PointAndFigure;
			}
			else if (State == State.Configure)
			{
				switch (BarsPeriod.BaseBarsPeriodType)
				{
					case BarsPeriodType.Minute	: BuiltFrom = BarsPeriodType.Minute; IsIntraday = true; IsTimeBased = true; break;
					case BarsPeriodType.Second	: BuiltFrom = BarsPeriodType.Tick;	IsIntraday = true;	IsTimeBased = true; break;
					case BarsPeriodType.Tick	:
					case BarsPeriodType.Volume	: BuiltFrom = BarsPeriodType.Tick;	IsIntraday = true;	IsTimeBased = false; break;
					default						: BuiltFrom = BarsPeriodType.Day;	IsIntraday = false;	IsTimeBased = true; break;
				}

				switch (BarsPeriod.BaseBarsPeriodType)
				{
					case BarsPeriodType.Day		: Name = string.Format("{0} {1} PointAndFigure{2}",		BarsPeriod.BaseBarsPeriodValue, BarsPeriod.BaseBarsPeriodValue == 1 ? Resource.GuiDaily : Resource.GuiDay, BarsPeriod.MarketDataType != MarketDataType.Last ? string.Format(" - {0}", BarsPeriod.MarketDataType) : string.Empty);		break;
					case BarsPeriodType.Minute	: Name = string.Format("{0} Min PointAndFigure{1}",		BarsPeriod.BaseBarsPeriodValue, BarsPeriod.MarketDataType != MarketDataType.Last ? string.Format(" - {0}", BarsPeriod.MarketDataType) : string.Empty);																					break;
					case BarsPeriodType.Month	: Name = string.Format("{0} {1} PointAndFigure{2}",		BarsPeriod.BaseBarsPeriodValue, BarsPeriod.BaseBarsPeriodValue == 1 ? Resource.GuiMonthly : Resource.GuiMonth, BarsPeriod.MarketDataType != MarketDataType.Last ? string.Format(" - {0}", BarsPeriod.MarketDataType) : string.Empty);	break;
					case BarsPeriodType.Second	: Name = string.Format("{0} {1} PointAndFigure{2}",		BarsPeriod.BaseBarsPeriodValue, BarsPeriod.BaseBarsPeriodValue == 1 ? Resource.GuiSecond : Resource.GuiSeconds, BarsPeriod.MarketDataType != MarketDataType.Last ? string.Format(" - {0}", BarsPeriod.MarketDataType) : string.Empty);	break;
					case BarsPeriodType.Tick	: Name = string.Format("{0} Tick PointAndFigure{1}",	BarsPeriod.BaseBarsPeriodValue, BarsPeriod.MarketDataType != MarketDataType.Last ? string.Format(" - {0}", BarsPeriod.MarketDataType) : string.Empty);																					break;
					case BarsPeriodType.Volume	: Name = string.Format("{0} Volume PointAndFigure{1}",	BarsPeriod.BaseBarsPeriodValue, BarsPeriod.MarketDataType != MarketDataType.Last ? string.Format(" - {0}", BarsPeriod.MarketDataType) : string.Empty);																					break;
					case BarsPeriodType.Week	: Name = string.Format("{0} {1} PointAndFigure{2}",		BarsPeriod.BaseBarsPeriodValue, BarsPeriod.BaseBarsPeriodValue == 1 ? Resource.GuiWeekly : Resource.GuiWeeks, BarsPeriod.MarketDataType != MarketDataType.Last ? string.Format(" - {0}", BarsPeriod.MarketDataType) : string.Empty);	break;
					case BarsPeriodType.Year	: Name = string.Format("{0} {1} PointAndFigure{2}",		BarsPeriod.BaseBarsPeriodValue, BarsPeriod.BaseBarsPeriodValue == 1 ? Resource.GuiYearly : Resource.GuiYears, BarsPeriod.MarketDataType != MarketDataType.Last ? string.Format(" - {0}", BarsPeriod.MarketDataType) : string.Empty);	break;
				}

				Properties.Remove(Properties.Find("ReversalType", true));

				SetPropertyName("Value",	Custom.Resource.NinjaScriptBarsTypePointAndFigureBoxSize);
				SetPropertyName("Value2",	Custom.Resource.NinjaScriptBarsTypePointAndFigureReversal);
			}
		}

		private DateTime TimeToBarTimeMinute(Bars bars, DateTime time, bool isBar)
		{
			if (SessionIterator.IsNewSession(time, isBar))
				SessionIterator.GetNextSession(time, isBar);

			DateTime barTimeStamp = !isBar
										? SessionIterator.ActualSessionBegin.AddMinutes(bars.BarsPeriod.BaseBarsPeriodValue + Math.Floor(Math.Floor(Math.Max(0, time.Subtract(SessionIterator.ActualSessionBegin).TotalMinutes)) / bars.BarsPeriod.BaseBarsPeriodValue) * bars.BarsPeriod.BaseBarsPeriodValue)
										: SessionIterator.ActualSessionBegin.AddMinutes(Math.Ceiling(Math.Ceiling(Math.Max(0, time.Subtract(SessionIterator.ActualSessionBegin).TotalMinutes)) / bars.BarsPeriod.BaseBarsPeriodValue) * bars.BarsPeriod.BaseBarsPeriodValue);
			if (bars.TradingHours.Sessions.Count > 0 && barTimeStamp > SessionIterator.ActualSessionEnd)
				barTimeStamp = SessionIterator.ActualSessionEnd <= Core.Globals.MinDate ? barTimeStamp : SessionIterator.ActualSessionEnd;
			return barTimeStamp;
		}

		private static DateTime TimeToBarTimeMonth(DateTime time, int periodValue)
		{
			DateTime result = new DateTime(time.Year, time.Month, 1);
			for (int i = 0; i < periodValue; i++)
				result = result.AddMonths(1);

			return result.AddDays(-1);
		}

		private DateTime TimeToBarTimeSecond(Bars bars, DateTime time, bool isBar)
		{
			if (SessionIterator.IsNewSession(time, isBar))
				SessionIterator.GetNextSession(time, isBar);

			DateTime barTimeStamp = SessionIterator.ActualSessionBegin.AddSeconds(Math.Ceiling(Math.Ceiling(Math.Max(0, time.Subtract(SessionIterator.ActualSessionBegin).TotalSeconds)) / bars.BarsPeriod.BaseBarsPeriodValue) * bars.BarsPeriod.BaseBarsPeriodValue);
			if (bars.TradingHours.Sessions.Count > 0 && barTimeStamp > SessionIterator.ActualSessionEnd)
				barTimeStamp = SessionIterator.ActualSessionEnd <= Core.Globals.MinDate ? barTimeStamp : SessionIterator.ActualSessionEnd;
			return barTimeStamp;
		}

		private static DateTime TimeToBarTimeWeek(DateTime time, DateTime periodStart, int periodValue)
		{
			return periodStart.Date.AddDays(Math.Ceiling(Math.Ceiling(time.Date.Subtract(periodStart.Date).TotalDays) / (periodValue * 7)) * (periodValue * 7)).Date;
		}

		private static DateTime TimeToBarTimeYear(DateTime time, int periodValue)
		{
			DateTime result = new DateTime(time.Year, 1, 1);
			for (int i = 0; i < periodValue; i++)
				result = result.AddYears(1);

			return result.AddDays(-1);
		}
	}
}