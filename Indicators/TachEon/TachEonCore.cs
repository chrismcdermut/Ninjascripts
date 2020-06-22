#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Permissions;
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
using NinjaTrader.NinjaScript.DrawingTools;

using System.Text.RegularExpressions;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators.TachEon
{
    #region Enums

    public enum eTachEonScoreMethod
    {
        Dot = 0,
        ATR = 1,
        Tick = 2,
        Off
    }

    public enum eTachEonExtensionMethods
    {
        NumberOfSignals,
        Infinite,
        None
    }

    public enum eTachEonGhostDotMethods
    {
        PrintDotAndPriceLine,
        PrintDot,
        None
    }

    public enum eTachEonMultiSlope
    {
        None,
        SMA,
        EMA,
        HMA,
        WMA
    }

    #endregion

    #region Score

    internal abstract class Score
	{
	    public abstract bool IsSet();
	    public abstract bool IsSuccess();
	}

    internal class ScoreDot : Score
    {
        public readonly double Dot;

        private DateTime _timeDot = DateTime.MinValue;

        public DateTime TimeDot { get { return _timeDot; } }

        public void HitDot(DateTime time)
        {
            _timeDot = time;
        }

        public override bool IsSet()
        {
            return !_timeDot.Equals(DateTime.MinValue);
        }

        public override bool IsSuccess()
        {
            return _timeDot.Equals(DateTime.MinValue);
        }

        public ScoreDot(double dot)
        {
            Dot = dot;
        }
    }

    internal class ScoreTargetStop : Score
    {
        public readonly double Target;
        public readonly double Stop;

        private DateTime _timeTarget = DateTime.MinValue;

        public DateTime TimeTarget { get { return _timeTarget; } }

        public void HitTarget(DateTime time)
        {
            _timeTarget = time;
        }

        private DateTime _timeStop = DateTime.MinValue;

        public DateTime TimeStop { get { return _timeStop; } }

        public void HitStop(DateTime time)
        {
            _timeStop = time;
        }

        public override bool IsSet()
        {
            return !_timeTarget.Equals(DateTime.MinValue) || !_timeStop.Equals(DateTime.MinValue);
        }

        public override bool IsSuccess()
        {
            return !_timeTarget.Equals(DateTime.MinValue);
        }

        public ScoreTargetStop(double target, double stop)
        {
            Target = target;
            Stop = stop;
        }
    }

    #endregion

    #region Pattern

    internal abstract class Pattern
	{
		public abstract int GetPatternType();

        public abstract int GetPatternId(bool isBuy);

	    public int MaximumFavorableExcursion { get; set; }

	    public int MaximumAdverseExcursion { get; set; }

	    public ScoreDot ScoreDot;
	    public ScoreTargetStop ScoreTick;
	    public ScoreTargetStop ScoreAtr;
    }

    internal class PatternOne : Pattern
    {
        public override int GetPatternType()
        {
            return 1;
        }

        public override int GetPatternId(bool isBuy)
        {
            return isBuy ? 1 : 2;
        }

        public override string ToString()
        {
            return "Pattern 1/2";
        }
    }

    internal class PatternTwo : Pattern
    {
        public override int GetPatternType()
        {
            return 2;
        }

        public override int GetPatternId(bool isBuy)
        {
            return isBuy ? 5 : 4;
        }

        public readonly DateTime[] BarTime = { DateTime.MinValue, DateTime.MinValue, DateTime.MinValue };
        public readonly double[] BarValue = { double.MinValue, double.MinValue, double.MinValue };

        public readonly DateTime[] BarTargetTime = { DateTime.MinValue, DateTime.MinValue, DateTime.MinValue };
        public readonly double[] BarTargetValue = { double.MinValue, double.MinValue, double.MinValue };

        public bool IsSet(int index)
        {
            return !BarTime[index].Equals(DateTime.MinValue) && !BarTargetTime[index].Equals(DateTime.MinValue);
        }

        public DateTime GetTime(int index)
        {
            return BarTime[index];
        }

        public double GetValue(int index)
        {
            return BarValue[index];
        }

        public DateTime GetTargetTime(int index)
        {
            return BarTargetTime[index];
        }

        public double GetTargetValue(int index)
        {
            return BarTargetValue[index];
        }

        public void SetBar(int index, DateTime time, double value, DateTime targetTime, double targetValue)
        {
            BarTime[index] = time;
            BarValue[index] = value;
            BarTargetTime[index] = targetTime;
            BarTargetValue[index] = targetValue;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < 3; i++)
            {
                if (IsSet(i))
                {
                    sb.AppendFormat("{0}: {1} {2}   ", i, BarTime[i], BarValue[i]);
                }
            }
            return string.Format("Pattern 4/5: {0}", sb);
        }
    }

    internal class PatternThree : Pattern
    {
        public override int GetPatternType()
        {
            return 3;
        }

        public override int GetPatternId(bool isBuy)
        {
            return isBuy ? 7 : 6;
        }

        public readonly DateTime BarTime;
        public readonly double Low;
        public readonly double High;

        public PatternThree(DateTime time, double low, double high)
        {
            BarTime = time;
            Low = low;
            High = high;
        }

        public override string ToString()
        {
            return string.Format("Pattern 6/7: {0}  {1}-{2}", BarTime, Low, High);
        }
    }

    #endregion

    #region PatternScoreCard

    internal class PatternScoreCard
    {
        #region Excursion

        // index (trendOrNot): 0 = All, 1 = Trend
        // index (favOrAdv): 0 = Fav, 1 = Adv

        private readonly int[] _excursionTotals = { 0, 0 };
        private readonly double[,] _excursionValues = { { 0.0, 0.0 }, { 0.0, 0.0 } };

        public void AddExcursion(double mfe, double mae, bool withTrend)
        {
            _excursionTotals[0]++;
            _excursionValues[0, 0] += mfe;
            _excursionValues[0, 1] += mae;
            if (!withTrend) return;
            _excursionTotals[1]++;
            _excursionValues[1, 0] += mfe;
            _excursionValues[1, 1] += mae;
        }

        private double GetExcursion(int trendOrNot, int favOrAdv)
        {
            return _excursionTotals[trendOrNot] == 0 ? 0 : _excursionValues[trendOrNot, favOrAdv] / _excursionTotals[trendOrNot];
        }

        public double GetAverageMfe()
        {
            return GetExcursion(0, 0);
        }

        public double GetAverageMae()
        {
            return GetExcursion(0, 1);
        }

        public double GetAverageMfeWithTrend()
        {
            return GetExcursion(1, 0);
        }

        public double GetAverageMaeWithTrend()
        {
            return GetExcursion(1, 1);
        }

        public void ResetExcursion()
        {
            _excursionTotals[0] = _excursionTotals[1] = 0;
            _excursionValues[0, 0] = _excursionValues[0, 1] = _excursionValues[1, 0] = _excursionValues[1, 1] = 0.0;
        }

        #endregion

        #region Scores

        // index (scoreType): 0 = Dot, 1 = Atr, 2 = Tick, 3 = Off (just a placeholder)
        // index (trendOrNot): 0 = All, 1 = Trend

        public struct Card
        {
            public int Total;
            public int Success;
            public int TotalWithTrend;
            public int SuccessWithTrend;
        }

        private readonly int[,] _scoreTotals = { { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 } };
        private readonly int[,] _scoreSuccesses = { { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 } };

        private Card GetScore(int scoreType)
        {
            return new Card
            {
                Total = _scoreTotals[scoreType, 0],
                Success = _scoreSuccesses[scoreType, 0],
                TotalWithTrend = _scoreTotals[scoreType, 1],
                SuccessWithTrend = _scoreSuccesses[scoreType, 1]
            };
        }

        public Card GetScore(eTachEonScoreMethod tachEonScoreType)
        {
            return GetScore((int)tachEonScoreType);
        }

        private void Add(int scoreType, bool isSuccess, bool withTrend)
        {
            _scoreTotals[scoreType, 0]++;
            if (isSuccess) _scoreSuccesses[scoreType, 0]++;
            if (!withTrend) return;
            _scoreTotals[scoreType, 1]++;
            if (isSuccess) _scoreSuccesses[scoreType, 1]++;
        }

        public void AddCount(eTachEonScoreMethod tachEonScoreType, bool isSucess, bool withTrend)
        {
            Add((int)tachEonScoreType, isSucess, withTrend);
        }

        private void ResetScore(int scoreType)
        {
            _scoreTotals[scoreType, 0] = _scoreTotals[scoreType, 1] = 0;
            _scoreSuccesses[scoreType, 0] = _scoreSuccesses[scoreType, 1] = 0;
        }

        public void ResetDot()
        {
            ResetScore((int)eTachEonScoreMethod.Dot);
        }

        public void ResetAtr()
        {
            ResetScore((int)eTachEonScoreMethod.ATR);
        }

        public void ResetTick()
        {
            ResetScore((int)eTachEonScoreMethod.Tick);
        }

        #endregion

        public void Reset()
        {
            ResetExcursion();
            ResetDot();
            ResetAtr();
            ResetTick();
        }
    }

    #endregion

    #region Signal

    internal class Signal: IComparable, IDisposable
    {
        #region properties

        public readonly DateTime BarTime;
        public readonly DateTime EndTime;

        public readonly bool IsEven;
        public readonly bool IsEstimated;
        public readonly bool IsFuture;

        public bool IsBuySignal(bool isReversePolarity = false)
        {
            return (IsEven && !isReversePolarity) || (!IsEven && isReversePolarity);
        }

        public bool IsSellSignal(bool isReversePolarity = false)
        {
            return (IsEven && isReversePolarity) || (!IsEven && !isReversePolarity);
        }

        public int Trend { get; set; }
        public double Atr { get; set; }

        private Signal _parent;

        public Signal Parent
        {
            get { return _parent; }
        }

        private bool _hasData = false;
        public bool HasData { get { return _hasData; } }

        public int BarIndex { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }

        public Pattern BuyPattern;
        public Pattern BuyPatternNeutral;

        public Pattern SellPattern;
        public Pattern SellPatternNeutral;

        private bool _withinScoreTime = true;

        public bool IsWithinScoreTime
        {
            get { return _withinScoreTime; }
            set { _withinScoreTime = value; }
        }

        private int _childCount = 0;

        public int ChildCount { get { return _childCount; } }

        public int ChildNumber
        {
            get
            {
                var index = 0;
                var node = this;
                while (node != null && !node.IsPrimary)
                {
                    index++;
                    node = node._prev;
                }
                return index;
            }
        }

        public bool IsPrimary { get { return _parent == null; } }

        private Signal _prev = null;

        public Signal Previous
        {
            get { return _prev; }
        }

        public Signal PreviousPrimary
        {
            get
            {
                var node = IsPrimary ? _prev : this;
                while (node != null)
                {
                    if (node.IsPrimary)
                        return node;
                    node = node._prev;
                }
                return null;
            }
        }

        private Signal _next = null;

        public Signal Next
        {
            get { return _next; }
        }

        public Signal NextPrimary
        {
            get
            {
                var node = IsPrimary ? _next : this;
                while (node != null)
                {
                    if (node.IsPrimary)
                        return node;
                    node = node._next;
                }
                return null;
            }
        }

        private Signal ChildNumberWithData()
        {
            var childNum = ChildNumber;
            var node = this;
            while (node != null)
            {
                if (node._hasData
                    && node.ChildNumber == childNum
                    && node._next != null
                    && node._next._hasData
                    && node._prev != null
                    && node._prev._hasData)
                {
                    return node;
                }
                node = node.Previous;
            }
            return null;
        }

        public int BarsToPrevious
        {
            get
            {
                if (_prev != null)
                {
                    if (_hasData && _prev._hasData)
                        return BarIndex - _prev.BarIndex;

                    var node = ChildNumberWithData();
                    if (node != null)
                        return node.BarIndex - node._prev.BarIndex;
                }
                return 0;
            }
        }

        public int BarsToNext
        {
            get
            {
                if (_next != null)
                {
                    if (_hasData && _next._hasData)
                        return _next.BarIndex - BarIndex;

                    var node = ChildNumberWithData();
                    if (node != null)
                        return node._next.BarIndex - node.BarIndex;
                }
                return 0;
            }
        }

        #endregion

        #region public functions

        public Signal InsertBefore(Signal signal)
        {
            signal._prev = _prev;
            signal._next = this;
            if (_prev != null)
            {
                _prev._next = signal;
            }
            _prev = signal;
            return signal;
        }

        public Signal InsertAfter(Signal signal)
        {
            signal._next = _next;
            signal._prev = this;
            if (_next != null)
            {
                _next._prev = signal;
            }
            _next = signal;
            return signal;
        }

        public void Disconnect()
        {
            if (_prev != null)
                _prev._next = _next;
            if (_next != null)
                _next._prev = _prev;

            if (_parent != null)
            {
                _parent._childCount--;
                _parent = null;
            }
        }

        public void UpdateData(Bars bars)
        {
            _hasData = false;
            var index = bars.GetBar(BarTime);
            if (index == 0)//GetBar returns ZERO if time is older than first bar - check if that's the case
            {
                var isIntraday = bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Minute
                                 || bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Second;
                var time = isIntraday ? bars.GetTime(0) : bars.GetSessionEndTime(0);
                if (!time.Equals(BarTime)) return;
            }
            else if (index == bars.Count - 1)//GetBar returns last abs index in the case of a future time - check that case
            {
                var isIntraday = bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Minute
                                 || bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Second;
                var time = isIntraday ? bars.GetTime(bars.Count - 1) : bars.GetSessionEndTime(bars.Count - 1);
                if (!time.Equals(BarTime)) return;
            }
            BarIndex = index;
            Open = bars.GetOpen(BarIndex);
            High = bars.GetHigh(BarIndex);
            Low = bars.GetLow(BarIndex);
            Close = bars.GetClose(BarIndex);
            _hasData = true;
        }

        public string ToStringFormat(bool isReversionPolarity = false)
        {
            return string.Format("{0} Signal Time: {1}", (IsEven && !isReversionPolarity) || (!IsEven && isReversionPolarity) ? "Buy" : "Sell", BarTime);
        }

        #endregion

        #region constructors

        private Signal(DateTime barTime, DateTime endTime, bool isEven, Signal parent, bool isFuture, bool isEstimated)
        {
            BarTime = barTime;
            EndTime = endTime;
            _parent = parent;
            IsEven = isEven;
            IsFuture = isFuture;
            IsEstimated = isEstimated;
            if (_parent != null)
                _parent._childCount++;
        }

        //primary
        public Signal(DateTime barTime, DateTime endTime, bool isEven) : this(barTime, endTime, isEven, null, false, false) { }
        public Signal(DateTime barTime, DateTime endTime, bool isEven, bool isFuture, bool isEstimated) : this(barTime, endTime, isEven, null, isFuture, isEstimated) { }

        //secondary
        public Signal(DateTime barTime, bool isEven, Signal parent) : this(barTime, DateTime.MinValue, isEven, parent, false, false) { }
        public Signal(DateTime barTime, bool isEven, Signal parent, bool isFuture, bool isEstimated) : this(barTime, DateTime.MinValue, isEven, parent, isFuture, isEstimated) { }

        #endregion

        #region overrides

        //signals sort future to past
        public virtual int CompareTo(object obj)
        {
            if (obj == null) return 1;
            var otherObject = obj as Signal;
            if (otherObject != null)
                return otherObject.BarTime.CompareTo(BarTime);
            else
                throw new ArgumentException("Object is not a Signal.");
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            return ((Signal)obj).BarTime.CompareTo(BarTime) == 0;
        }

        public void Dispose()
        {
            _prev = _next = null;
        }

        public override string ToString()
        {
            return string.Format("Signal: {0}, IsEven: {1} ({2})", BarTime, IsEven, !IsPrimary ? "secondary" : ("children: " + _childCount.ToString()));
        }

        #endregion
    }

    #endregion

    #region IndicatorSettings

    internal class IndicatorSettings
    {
        #region properties

        public readonly int BarsIndex;
        public readonly int TrendValuesIndex;
        public readonly int BarsToNextIndex;
        public readonly int BarsFromPreviousIndex;
        public readonly int PatternIndex;

        private Processor _processor;

        public Processor Processor
        {
            get { return _processor; }
            set { _processor = value; }
        }

        private int _dotOffset = 3;

        public int DotOffset
        {
            get { return _dotOffset; }
            set
            {
                if (value != _dotOffset)
                {
                    _dotOffset = value;
                    if (_processor == null) return;
                    _processor.OnChangeDotOffset();
                }
            }
        }

        private int _atrValue = 7;

        public int AtrValue
        {
            get { return _atrValue; }
            set
            {
                if (value != _atrValue)
                {
                    _atrValue = value;
                    if (_processor == null) return;
                    _processor.OnChangeAtrValue();
                }
            }
        }

        private eTachEonMultiSlope _slopeMethod = eTachEonMultiSlope.EMA;

        public eTachEonMultiSlope SlopeMethod
        {
            get { return _slopeMethod; }
            set
            {
                if (value != _slopeMethod)
                {
                    _slopeMethod = value;
                    if (_processor == null) return;
                    _processor.OnChangeSlope();
                }
            }
        }

        private int _slopePeriod = 120;

        public int SlopePeriod
        {
            get { return _slopePeriod; }
            set
            {
                if (value != _slopePeriod)
                {
                    _slopePeriod = value;
                    if (_processor == null) return;
                    _processor.OnChangeSlope();
                }
            }
        }

        private readonly Dictionary<int, int> _swings = new Dictionary<int, int>();

        public int GetSwing(int patternType)
        {
            if (patternType < 1 || patternType > 3)
                throw new ArgumentException("Invalid patternType");
            return _swings[patternType];
        }

        public void SetSwing(int patternType, int input)
        {
            if (patternType < 1 || patternType > 3)
                throw new ArgumentException("Invalid patternType");
            if (_swings[patternType] != input)
            {
                _swings[patternType] = input;
                if (_processor == null) return;
                _processor.OnChangeSwing();
            }
        }

        private bool _isReversePolarity;

        public bool IsReversePolarity
        {
            get { return _isReversePolarity; }
            set
            {
                if (value != _isReversePolarity)
                {
                    _isReversePolarity = value;
                    if (_processor == null) return;
                    _processor.OnChangePolarity();
                }
            }
        }

        #endregion

        #region score properties

        public class ScoreSetting
        {
            private readonly IndicatorSettings _parent;

            private eTachEonScoreMethod _method = eTachEonScoreMethod.Dot;

            public eTachEonScoreMethod Method
            {
                get { return _method; }
                set
                {
                    if (value != _method)
                    {
                        _method = value;
                        _parent.OnChangeScoreMethod();
                    }
                }
            }

            private int _atrValue = 7;

            public int AtrValue
            {
                get { return _atrValue; }
                set
                {
                    if (value != _atrValue)
                    {
                        _atrValue = value;
                        _parent.OnChangeAtrValue();
                    }
                }
            }

            private double _atrTarget;

            public double AtrTarget
            {
                get { return _atrTarget; }
                set
                {
                    if (value != _atrTarget)
                    {
                        _atrTarget = value;
                        _parent.OnChangeTargetStopAtr();
                    }
                }
            }

            private double _atrStop;

            public double AtrStop
            {
                get { return _atrStop; }
                set
                {
                    if (value != _atrStop)
                    {
                        _atrStop = value;
                        _parent.OnChangeTargetStopAtr();
                    }
                }
            }

            private int _tickTarget;

            public int TickTarget
            {
                get { return _tickTarget; }
                set
                {
                    if (value != _tickTarget)
                    {
                        _tickTarget = value;
                        _parent.OnChangeTargetStopTick();
                    }
                }
            }

            private int _tickStop;

            public int TickStop
            {
                get { return _tickStop; }
                set
                {
                    if (value != _tickStop)
                    {
                        _tickStop = value;
                        _parent.OnChangeTargetStopTick();
                    }
                }
            }

            private int _dotOffset = 3;

            public int DotOffset
            {
                get { return _dotOffset; }
                set
                {
                    if (value != _dotOffset)
                    {
                        _dotOffset = value;
                        _parent.OnChangeDotOffset();
                    }
                }
            }

            public ScoreSetting(IndicatorSettings parent)
            {
                _parent = parent;
            }
        }

        public readonly Dictionary<int, ScoreSetting> ScoreSettings = new Dictionary<int, ScoreSetting>();

        private bool _isTimeRestricted;

        public bool IsTimeRestricted
        {
            get { return _isTimeRestricted; }
            set
            {
                if (value != _isTimeRestricted)
                {
                    _isTimeRestricted = value;
                    if (_processor != null)
                        _processor.OnChangeScoreTime();
                }
            }
        }

        private TimeSpan _timeStart = new TimeSpan(0, 0, 0);

        public TimeSpan TimeStart
        {
            get { return _timeStart; }
            set
            {
                if (!value.Equals(_timeStart))
                {
                    _timeStart = value;
                    if (_processor != null)
                        _processor.OnChangeScoreTime();
                }
            }
        }

        private TimeSpan _timeEnd = new TimeSpan(0, 0, 0);

        public TimeSpan TimeEnd
        {
            get { return _timeEnd; }
            set
            {
                if (!value.Equals(_timeEnd))
                {
                    _timeEnd = value;
                    if (_processor != null)
                        _processor.OnChangeScoreTime();
                }
            }
        }

        public static bool IsValidString(string time)
        {
            return Regex.IsMatch(time.Trim(), "^[0-9]{1,2}\\:?[0-9]{2}\\:?[0-9]{2}$", RegexOptions.Compiled);
        }

        public static TimeSpan ParseTime(TimeSpan current, string time)
        {
            var hours = current.Hours;
            var minutes = current.Minutes;
            var seconds = current.Seconds;
            if (IsValidString(time))
            {
                int tryParse;

                time = time.Trim();
                time = string.Concat(time.Split(":".ToCharArray()));
                if (time.Length == 5)
                    time = "0" + time;

                var sec = time.Substring(time.Length - 2);
                if (int.TryParse(sec, out tryParse))
                {
                    seconds = tryParse;
                    if (seconds > 59) seconds = 0;
                }

                var min = time.Substring(time.Length - 4, 2);
                if (int.TryParse(min, out tryParse))
                {
                    minutes = tryParse;
                    if (minutes > 59) minutes = 0;
                }

                var h = time.Substring(0, 2);
                if (int.TryParse(h, out tryParse))
                {
                    hours = tryParse;
                    if (hours > 23) hours = 0;
                }
            }
            return new TimeSpan(hours, minutes, seconds);
        }

        public static string FormatTime(TimeSpan time)
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds);
        }

        public string GetScoreStart()
        {
            return FormatTime(TimeStart);
        }

        public void SetScoreStart(string time)
        {
            TimeStart = ParseTime(TimeStart, time);
        }

        public string GetScoreEnd()
        {
            return FormatTime(TimeEnd);
        }

        public void SetScoreEnd(string time)
        {
            TimeEnd = ParseTime(TimeEnd, time);
        }

        public ScoreSetting GetScoreSettingByPatternId(int patternId)
        {
            switch (patternId)
            {
                case -1:
                case 0:
                    return ScoreSettings[0];
                case 1:
                case 2:
                    return ScoreSettings[1];
                case 4:
                case 5:
                    return ScoreSettings[2];
                case 6:
                case 7:
                    return ScoreSettings[3];
            }
            return null;
        }

        private bool _scoresRequireClose;

        public bool ScoresRequireClose
        {
            get { return _scoresRequireClose; }
            set
            {
                if (value == _scoresRequireClose) return;
                _scoresRequireClose = value;
                if (_processor != null)
                    _processor.OnChangeScoreRequireStop();
            }
        }

        #endregion

        public IndicatorSettings(int barsIndex = 0, int trendValuesIndex = -1, int barsToNext = -1, int barsFromPrevious = -1, int patternIndex = -1)
        {
            BarsIndex = barsIndex;
            TrendValuesIndex = trendValuesIndex;
            BarsToNextIndex = barsToNext;
            BarsFromPreviousIndex = barsFromPrevious;
            PatternIndex = patternIndex;
            for (var i = 0; i <= 3; i++)
            {
                _swings[i] = 5;
                ScoreSettings.Add(i, new ScoreSetting(this));
            }
        }

        #region OnChange events

        public virtual void OnChangeScoreMethod()
        {
            if (_processor == null) return;
            _processor.OnChangeScoreMethod();
        }

        public virtual void OnChangeDotOffset()
        {
            if (_processor == null) return;
            _processor.OnChangeDotOffset();
        }

        public virtual void OnChangeAtrValue()
        {
            if (_processor == null) return;
            _processor.OnChangeAtrValue();
        }

        public virtual void OnChangeTargetStopAtr()
        {
            if (_processor == null) return;
            _processor.OnChangeTargetStopAtr();
        }

        public virtual void OnChangeTargetStopTick()
        {
            if (_processor == null) return;
            _processor.OnChangeTargetStopTick();
        }

        #endregion
    }

    #endregion

    #region Processor

    internal abstract class Processor
    {
        protected readonly object _lock = new object();
        protected readonly Indicator _parent;
        protected readonly IndicatorSettings _settings;

        #region debug

        private readonly bool _debug = false;

        protected void Log(object toLog)
        {
            if (_debug)
                Code.Output.Process(string.Format("TachEon {2}: {1}: {0}", toLog, _parent.Instrument.FullName, DateTime.Now), PrintTo.OutputTab1);
        }

        protected void Log2(object toLog)
        {
            if (_debug)
                Code.Output.Process(string.Format("TachEon {2}: {1}: {0}", toLog, _parent.Instrument.FullName, DateTime.Now), PrintTo.OutputTab2);
        }

        #endregion

        #region to override

        protected abstract int NextSignalBarIndex(Signal signal);

        protected abstract int PreviousSignalBarIndex(Signal signal);

        protected abstract int Calculate();

        #endregion

        protected readonly bool _isIntraday;

        private static int TranslatePatternIdToIndex(int patternId)
        {
            if (patternId >= -1 && patternId <= 0)
            {
                return patternId + 1;
            }
            else if (patternId >= 1 && patternId <= 2)
            {
                return patternId + 1;
            }
            else if (patternId >= 4 && patternId <= 7)
            {
                return patternId;
            }

            return 0;
        }

        #region Signals

        private Signal _first = null;

        public Signal First
        {
            get { return _first; }
        }

        private Signal _last = null;

        public Signal Last
        {
            get { return _last; }
        }

        public Signal CurrentSignal
        {
            get
            {
                var bars = _parent.BarsArray[_settings.BarsIndex];
                var lastBarTime = _isIntraday ? bars.GetTime(bars.Count - 1) : bars.GetSessionEndTime(bars.Count - 1);
                var signal = Last;
                while (signal != null)
                {
                    if (signal.BarTime.CompareTo(lastBarTime) <= 0)
                        return signal;
                    signal = signal.Previous;
                }
                return null;
            }
        }

        #endregion

        #region signal add/remove/get

        protected Signal AddSignal(Signal signal)
        {
            lock (_lock)
            {
                if (_first == null && _last == null)
                {
                    _first = _last = signal;
                }
                else
                {
                    if (signal.BarTime.CompareTo(_first.BarTime) <= 0) // signal should be first 
                    {
                        _first = _first.InsertBefore(signal);
                    }
                    else if (signal.BarTime.CompareTo(_last.BarTime) >= 0) // signal should be last
                    {
                        _last = _last.InsertAfter(signal);
                    }
                    else
                    {
                        var move = _last;
                        while (move != null)
                        {
                            if (signal.BarTime.CompareTo(move.BarTime) >= 0)
                            {
                                move.InsertAfter(signal);
                                break;
                            }
                            move = move.Previous;
                        }
                        if (move == null)
                            _first = _first.InsertBefore(signal);
                    }
                }
            }

            return signal;
        }

        protected void RemoveSignal(Signal signal)
        {
            lock (_lock)
            {
                if (signal == _first)
                    _first = _first.Next;
                if (signal == _last)
                    _last = _last.Previous;
                signal.Disconnect();
            }
        }

        public Signal GetSignal(DateTime time)
        {
            Signal find = _last;
            while (find != null)
            {
                if (find.BarTime.CompareTo(time) <= 0)
                {
                    return find;
                }
                find = find.Previous;
            }
            return null;
        }

        #endregion

        #region scores

        private const int ScoreCount = 8;
        private readonly PatternScoreCard[] _scores = new PatternScoreCard[ScoreCount];

        public PatternScoreCard GetScoreCard(int patternId)
        {
            return _scores[TranslatePatternIdToIndex(patternId)];
        }

        #endregion

        #region reset

        public void ResetScore()
        {
            for (var i = 0; i < 6; i++)
            {
                _scores[i].Reset();
            }
        }

        public virtual void Reset()
        {
            lock (_lock)
            {
                ResetScore();
                var node = _first;
                while (node != null)
                {
                    var next = node.Next;
                    node.Dispose();
                    node = next;
                }
                _first = _last = null;
            }
        }

        #endregion

        protected Processor(Indicator parent, IndicatorSettings settings)
        {
            _parent = parent;
            _settings = settings;
            _settings.Processor = this;
            for (var i = 0; i < ScoreCount; i++)
            {
                _scores[i] =new PatternScoreCard();
            }
            var bars = _parent.BarsArray[_settings.BarsIndex];
            _isIntraday = bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Second
                         || bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Minute;
        }

        private void ScoreTally(bool indexesSet = false)
        {
            //var timer = DateTime.Now;

            var scores = new PatternScoreCard[8];
            for (var i = 0; i < ScoreCount; i++)
            {
                scores[i] = new PatternScoreCard();
            }

            var node = First;
            while (node != null)
            {
                var signal = node;
                node = node.Next;

                if (signal.BuyPattern == null || signal.SellPattern == null) continue;
                if (signal.BuyPatternNeutral == null || signal.SellPatternNeutral == null) continue;
                if (!signal.HasData) continue;

                signal.IsWithinScoreTime = !_settings.IsTimeRestricted;
                if (_settings.IsTimeRestricted)
                {
                    var signalTime = new TimeSpan(signal.BarTime.Hour, signal.BarTime.Minute, signal.BarTime.Second);
                    if (_settings.TimeStart.CompareTo(_settings.TimeEnd) <= 0)
                    {
                        if (signalTime.CompareTo(_settings.TimeStart) >= 0 && signalTime.CompareTo(_settings.TimeEnd) <= 0)
                            signal.IsWithinScoreTime = true;
                    }
                    else
                    {
                        if (signalTime.CompareTo(_settings.TimeStart) >= 0 || signalTime.CompareTo(_settings.TimeEnd) <= 0)
                            signal.IsWithinScoreTime = true;
                    }
                }

                if (!signal.IsWithinScoreTime) continue;

                var patterns = new Pattern[2];
                var withTrends = new bool[2];
                var scoreCards = new PatternScoreCard[2];

                if (signal.IsBuySignal(_settings.IsReversePolarity))
                {
                    patterns[0] = signal.BuyPattern;
                    withTrends[0] = patterns[0].GetPatternType() == 1 ? signal.Trend > 0 : signal.Trend < 0;
                    scoreCards[0] = scores[TranslatePatternIdToIndex(patterns[0].GetPatternId(true))];

                    patterns[1] = signal.BuyPatternNeutral;
                    withTrends[1] = signal.Trend > 0;
                    scoreCards[1] = scores[1];
                }
                else
                {
                    patterns[0] = signal.SellPattern;
                    withTrends[0] = patterns[0].GetPatternType() == 1 ? signal.Trend < 0 : signal.Trend > 0;
                    scoreCards[0] = scores[TranslatePatternIdToIndex(patterns[0].GetPatternId(false))];

                    patterns[1] = signal.SellPatternNeutral;
                    withTrends[1] = signal.Trend < 0;
                    scoreCards[1] = scores[0];
                }

                {
                    for(var i = 0; i < 2; i++)
                    {
                        var score = patterns[i].ScoreDot;
                        if (score != null)
                            scoreCards[i].AddCount(eTachEonScoreMethod.Dot, score.IsSuccess(), withTrends[i]);
                    }
                }
                {
                    for (var i = 0; i < 2; i++)
                    {
                        var score = patterns[i].ScoreAtr;
                        if (score != null)
                        {
                            if (score.IsSet())
                            {
                                scoreCards[i].AddCount(eTachEonScoreMethod.ATR, score.IsSuccess(), withTrends[i]);
                            }
                            else if (indexesSet)
                            {
                                var barOffset = FindBarOffset(signal);
                                if (barOffset < _parent.CurrentBar)
                                {
                                    OnBarUpdateScoreTargetStop(signal, barOffset, "atr");
                                    score = patterns[i].ScoreAtr;
                                    if (score.IsSet())
                                    {
                                        Log("found atr " + signal.BarTime);
                                        scoreCards[i].AddCount(eTachEonScoreMethod.ATR, score.IsSuccess(), withTrends[i]);
                                    }
                                }
                            }
                        }
                    }
                }
                {
                    for (var i = 0; i < 2; i++)
                    {
                        var score = patterns[i].ScoreTick;
                        if (score != null)
                        {
                            if (score.IsSet())
                            {
                                scoreCards[i].AddCount(eTachEonScoreMethod.Tick, score.IsSuccess(), withTrends[i]);
                            }
                            else if (indexesSet)
                            {
                                var barOffset = FindBarOffset(signal);
                                if (barOffset < _parent.CurrentBar)
                                {
                                    OnBarUpdateScoreTargetStop(signal, barOffset);
                                    score = patterns[i].ScoreTick;
                                    if (score.IsSet())
                                    {
                                        Log("found tick " + signal.BarTime);
                                        scoreCards[i].AddCount(eTachEonScoreMethod.Tick, score.IsSuccess(), withTrends[i]);
                                    }
                                }
                            }
                        }
                    }
                }
                for (var i = 0; i < 2; i++)
                {
                    scoreCards[i].AddExcursion(patterns[i].MaximumFavorableExcursion, patterns[i].MaximumAdverseExcursion, withTrends[i]);
                }
            }

            for (var i = 0; i < ScoreCount; i++)
            {
                _scores[i] = scores[i];
            }

            RefreshUI();

            //var time = (DateTime.Now - timer).TotalMilliseconds;
            //Log(string.Format("ScoreTally: {0}", time));
        }

        private void ProcessAll(Signal signal, int barOffset)
        {
            OnBarUpdatePattern(signal, barOffset);
            OnBarUpdateExport(signal, barOffset);

            OnBarUpdateExcursion(signal, barOffset);

            OnBarUpdateTrend(signal, barOffset);
            OnBarUpdateAtr(signal, barOffset);

            OnBarUpdateScoreDot(signal, barOffset);
            OnBarUpdateScoreTargetStop(signal, barOffset, "atr");
            OnBarUpdateScoreTargetStop(signal, barOffset);
        }

        private void ProcessExport(Signal signal, int barOffset)
        {
            OnBarUpdateExport(signal, barOffset);
        }

        private void ProcessScoreAll(Signal signal, int barOffset)
        {
            OnBarUpdateScoreDot(signal, barOffset);
            OnBarUpdateScoreTargetStop(signal, barOffset, "atr");
            OnBarUpdateScoreTargetStop(signal, barOffset);
        }

        private void ProcessScoreDot(Signal signal, int barOffset)
        {
            OnBarUpdateScoreDot(signal, barOffset);
        }

        private void ProcessScoreTargetStopAtr(Signal signal, int barOffset)
        {
            OnBarUpdateScoreTargetStop(signal, barOffset, "atr");
        }

        private void ProcessScoreTargetStopTick(Signal signal, int barOffset)
        {
            OnBarUpdateScoreTargetStop(signal, barOffset);
        }

        private int FindBarOffset(Signal signal, int barOffset = 0)
        {
            while (barOffset < _parent.CurrentBar && !_parent.Time[barOffset].Equals(signal.BarTime))
            {
                barOffset++;
                if (barOffset >= _parent.CurrentBar)
                {
                    return _parent.CurrentBar + 1;
                }
            }
            return barOffset;
        }

        protected virtual void Process(int count, Action<Signal, int> action = null)
        {
            var timer = DateTime.Now;
            var signal = CurrentSignal;
            if (signal == null) return;

            if (_parent.CurrentBar < 0) return;
            if (signal.BarIndex > _parent.CurrentBar) return;

            Action<Signal, int> function = action == null ? ProcessAll : action;

            var done = 0;
            var barOffset = 0;
            while (signal != null)
            {
                //while (barOffset < _parent.CurrentBar && !_parent.Time[barOffset].Equals(signal.BarTime)) //Parent.Time.Count
                //{
                //    barOffset++;
                //    if (barOffset >= _parent.CurrentBar)
                //    {
                //        signal = null;
                //        break;
                //    }
                //}
                barOffset = FindBarOffset(signal, barOffset);
                if (barOffset > _parent.CurrentBar)
                    break;

                done++;
                function(signal, barOffset);

                signal = signal.Previous;

                if (--count == 0) break;
            }

            ScoreTally(true);
            var time = (DateTime.Now - timer).TotalMilliseconds;
            Log(string.Format("Processed {1} signals in {0}", time, done));
        }

        protected virtual void RefreshUI()
        {

        }

        protected virtual void ProcessTrigger(int count, Action<Signal, int> action = null)
        {
            _parent.TriggerCustomEvent(o => { Process(count, action); }, _settings.BarsIndex, null);
        }

        #region OnBarUpdate Functions

        protected double _lastProcessedHigh = 0.0;
        protected double _lastProcessedLow = 0.0;
        protected int _lastProcessedBarIndex = -1;

        public virtual void OnBarUpdate(bool force = false)
        {
            //if (Last != null)
            {
                var bars = _parent.BarsArray[_settings.BarsIndex];
                var times = _parent.Times[_settings.BarsIndex];
                var highs = _parent.Highs[_settings.BarsIndex];
                var lows = _parent.Lows[_settings.BarsIndex];

                // first tick of bar
                if (_parent.CurrentBar != _lastProcessedBarIndex)
                {
                    var count = Calculate();
                    if (count > 0)
                    {
                        Process(count);
                    }
                }

                if (force || _lastProcessedHigh != highs[0] || _lastProcessedLow != lows[0])
                {
                    var signal = CurrentSignal;

                    if (signal != null)
                    {
                        // on the signal bar
                        if (signal.BarTime.Equals(times[0]))
                        {
                            signal.UpdateData(bars);
                        }
                        // on bar BEFORE next signal
                        else if (signal.Next != null && _parent.CurrentBar == NextSignalBarIndex(signal) - 1)
                        {
                            OnBarUpdatePattern(signal.Next, 0);
                        }
                        Process(1);
                    }
                }

                _lastProcessedHigh = highs[0];
                _lastProcessedLow = lows[0];
                _lastProcessedBarIndex = _parent.CurrentBar;
            }
        }

        protected virtual void OnBarUpdateTrend(Signal signal, int barOffset)
        {
            if (_settings.TrendValuesIndex < 0) return;
            if (barOffset + 1 < _parent.CurrentBar)
                signal.Trend = _parent.Values[_settings.TrendValuesIndex][barOffset] >= _parent.Values[_settings.TrendValuesIndex][barOffset + 1] ? 1 : -1;
        }

        protected virtual void OnBarUpdateAtr(Signal signal, int barOffset)
        {
            if (barOffset < _parent.CurrentBar)
                signal.Atr = _parent.ATR(_settings.AtrValue)[barOffset];
        }

        protected virtual void OnBarUpdatePattern(Signal signal, int barOffset)
        {
            signal.BuyPattern = signal.SellPattern = null;

            var futureSignal = !signal.HasData;

            var strength45 = _settings.GetSwing(2);
            var strength67 = _settings.GetSwing(3);

            #region pattern 4 BUY ON RED SIGNAL

            if (signal.SellPattern == null)
            {
                var pattern = new PatternTwo();
                var isValid = false;

                if (futureSignal)
                {
                    if (signal.Previous != null
                        && signal.Previous.HasData
                        && NextSignalBarIndex(signal.Previous) == _parent.CurrentBar + 1)
                    {
                        var lookBarOffset = barOffset;
                        if (lookBarOffset >= 0 && lookBarOffset <= _parent.CurrentBar) // within barcount range
                        {
                            var swing = _parent.Swing(_parent.Low, strength45).SwingLowBar(lookBarOffset, 1, 1000);
                            if (swing > 0) // got a valid swing
                            {
                                // look for lower bars between swing and current barOffset
                                var val = _parent.Low[swing];
                                var swingest = swing;
                                for (var ii = swing - 1; ii > lookBarOffset; ii--)
                                {
                                    if (_parent.Low[ii] <= val)
                                    {
                                        val = _parent.Low[ii];
                                        swingest = ii;
                                    }
                                }

                                if (_parent.Low[lookBarOffset] <= val)
                                {
                                    pattern.SetBar(0, _parent.Time[lookBarOffset], _parent.Low[lookBarOffset], _parent.Time[swingest], _parent.Low[swingest]);
                                    isValid = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // iterate bar before (1), current bar (+0), bar after (-1)
                    for (var i = 1; i >= -1; i--)
                    {
                        var lookBarOffset = barOffset + i;
                        if (lookBarOffset >= 0 && lookBarOffset <= _parent.CurrentBar) // within barcount range
                        {
                            var swing = _parent.Swing(_parent.Low, strength45).SwingLowBar(lookBarOffset, 1, 1000);
                            if (swing > 0) // got a valid swing
                            {
                                // look for lower bars between swing and current barOffset
                                var val = _parent.Low[swing];
                                var swingest = swing;
                                for (var ii = swing - 1; ii > lookBarOffset; ii--)
                                {
                                    if (_parent.Low[ii] <= val)
                                    {
                                        val = _parent.Low[ii];
                                        swingest = ii;
                                    }
                                }

                                if (_parent.Low[lookBarOffset] <= val)
                                {
                                    pattern.SetBar(i + 1, _parent.Time[lookBarOffset], _parent.Low[lookBarOffset], _parent.Time[swingest], _parent.Low[swingest]);
                                    isValid = true;
                                }
                            }
                        }
                    }
                }

                if (isValid)
                    signal.SellPattern = pattern;
            }

            #endregion pattern 4

            #region pattern 5 SELL ON GREEN SIGNAL

            if (signal.BuyPattern == null)
            {
                var pattern = new PatternTwo();
                var isValid = false;

                if (futureSignal)
                {
                    if (signal.Previous != null
                        && signal.Previous.HasData
                        && NextSignalBarIndex(signal.Previous) == _parent.CurrentBar + 1)
                    {
                        var lookBarOffset = barOffset;
                        if (lookBarOffset >= 0 && lookBarOffset <= _parent.CurrentBar) // within barcount range
                        {
                            var swing = _parent.Swing(_parent.High, strength45).SwingHighBar(lookBarOffset, 1, 1000);
                            if (swing > 0) // got a valid swing
                            {
                                // look for higher bars between swing and current barOffset
                                var val = _parent.High[swing];
                                var swingest = swing;
                                for (var ii = swing - 1; ii > lookBarOffset; ii--)
                                {
                                    if (_parent.High[ii] >= val)
                                    {
                                        val = _parent.High[ii];
                                        swingest = ii;
                                    }
                                }

                                if (_parent.High[lookBarOffset] >= val)
                                {
                                    pattern.SetBar(0, _parent.Time[lookBarOffset], _parent.High[lookBarOffset], _parent.Time[swingest], _parent.High[swingest]);
                                    isValid = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // iterate bar before (1), current bar (+0), bar after (-1)
                    for (var i = 1; i >= -1; i--)
                    {
                        var lookBarOffset = barOffset + i;
                        if (lookBarOffset >= 0 && lookBarOffset <= _parent.CurrentBar) // within barcount range
                        {
                            var swing = _parent.Swing(_parent.High, strength45).SwingHighBar(lookBarOffset, 1, 1000);
                            if (swing > 0) // got a valid swing
                            {
                                // look for higher bars between swing and current barOffset
                                var val = _parent.High[swing];
                                var swingest = swing;
                                for (var ii = swing - 1; ii > lookBarOffset; ii--)
                                {
                                    if (_parent.High[ii] >= val)
                                    {
                                        val = _parent.High[ii];
                                        swingest = ii;
                                    }
                                }

                                if (_parent.High[lookBarOffset] >= val)
                                {
                                    pattern.SetBar(i + 1, _parent.Time[lookBarOffset], _parent.High[lookBarOffset], _parent.Time[swingest], _parent.High[swingest]);
                                    isValid = true;
                                }
                            }
                        }
                    }
                }

                if (isValid)
                    signal.BuyPattern = pattern;
            }

            #endregion pattern 5

            if (futureSignal) return;

            #region pattern 6 BUY SIGNAL ON RED

            if (signal.SellPattern == null && signal.Previous != null)
            {
                var swing = _parent.Swing(_parent.Low, strength67).SwingLowBar(barOffset, 1, 1000);
                if (swing > 0) // got a valid swing
                {
                    // look for lower bars between swing and current barOffset
                    var val = _parent.Low[swing];
                    var swingest = swing;
                    for (var i = swing - 1; i > barOffset; i--)
                    {
                        if (_parent.Low[i] <= val)
                        {
                            val = _parent.Low[i];
                            swingest = i;
                        }
                    }

                    if (swingest < barOffset + (signal.BarIndex - signal.Previous.BarIndex)
                        && _parent.Low[barOffset] > _parent.Low[swingest]
                        && _parent.High[barOffset] < _parent.High[swingest])
                    {
                        signal.SellPattern = new PatternThree(_parent.Time[swingest], _parent.Low[swingest], _parent.High[swingest]);
                    }
                }
            }

            #endregion pattern 6

            #region pattern 7 SELL SIGNAL ON GREEN

            if (signal.BuyPattern == null && signal.Previous != null)
            {
                var swing = _parent.Swing(_parent.High, strength67).SwingHighBar(barOffset, 1, 1000);
                if (swing > 0) // got a valid swing
                {
                    // look for higher bars between swing and current barOffset
                    var val = _parent.High[swing];
                    var swingest = swing;
                    for (var i = swing - 1; i > barOffset; i--)
                    {
                        if (_parent.High[i] >= val)
                        {
                            val = _parent.High[i];
                            swingest = i;
                        }
                    }

                    if (swingest < barOffset + (signal.BarIndex - signal.Previous.BarIndex)//if (swingest < PreviousSignalBarIndex(signal) // signal.BarIndex - previous.BarIndex
                        && _parent.Low[barOffset] > _parent.Low[swingest]
                        && _parent.High[barOffset] < _parent.High[swingest])
                    {
                        signal.BuyPattern = new PatternThree(_parent.Time[swingest], _parent.Low[swingest], _parent.High[swingest]);
                    }
                }
            }

            #endregion pattern 7

            #region default patterns

            if (signal.BuyPattern == null)
                signal.BuyPattern = new PatternOne();

            if (signal.BuyPatternNeutral == null)
                signal.BuyPatternNeutral = new PatternOne();

            if (signal.SellPattern == null)
                signal.SellPattern = new PatternOne();

            if (signal.SellPatternNeutral == null)
                signal.SellPatternNeutral = new PatternOne();

            #endregion default
        }

        protected virtual void OnBarUpdateExport(Signal signal, int barOffset)
        {
            var dataIndex = (signal.Next != null && signal.Next.HasData) ? (barOffset - (signal.Next.BarIndex - signal.BarIndex)) : -1;
            var nextIndex = NextSignalBarIndex(signal) - signal.BarIndex;
            
            for (int i = barOffset, count = 0; i > dataIndex; i--, count++)
            {
                if (_settings.BarsToNextIndex >= 0)
                {
                    _parent.Values[_settings.BarsToNextIndex][i] = nextIndex - count;
                }
                if (_settings.BarsFromPreviousIndex >= 0)
                {
                    _parent.Values[_settings.BarsFromPreviousIndex][i] = count;
                }
                if (_settings.PatternIndex >= 0)
                {
                    if (signal.BuyPattern != null && signal.SellPattern != null)
                    {
                        _parent.Values[_settings.PatternIndex][i] = signal.IsBuySignal(_settings.IsReversePolarity)
                                ? signal.BuyPattern.GetPatternId(true)
                                : signal.SellPattern.GetPatternId(false);
                    }
                    else
                    {
                        _parent.Values[_settings.PatternIndex][i] = 0;
                    }
                }
            }
        }

        protected virtual void OnBarUpdateExcursion(Signal signal, int barOffset)
        {
            if (!signal.HasData) return;
            if (signal.Next == null) return;
            if (!signal.Next.HasData) return;
            if (signal.BuyPattern == null || signal.SellPattern == null) return;
            if (signal.BuyPatternNeutral == null || signal.SellPatternNeutral == null) return;

            var lo = signal.Close;
            var hi = signal.Close;
            var indexDiff = signal.Next.BarIndex - signal.BarIndex;
            for (var i = 1; i <= indexDiff; i++)
            {
                lo = Math.Min(lo, _parent.Low[barOffset - i]);
                hi = Math.Max(hi, _parent.High[barOffset - i]);
            }

            var up = (int)((hi - signal.Close) / _parent.TickSize);
            var down = (int)((signal.Close - lo) / _parent.TickSize);

            signal.BuyPatternNeutral.MaximumFavorableExcursion = signal.SellPatternNeutral.MaximumAdverseExcursion = up;
            signal.BuyPatternNeutral.MaximumAdverseExcursion = signal.SellPatternNeutral.MaximumFavorableExcursion = down;

            if (signal.BuyPattern.GetPatternType() == 1)
            {
                signal.BuyPattern.MaximumFavorableExcursion = up;
                signal.BuyPattern.MaximumAdverseExcursion = down;
            }
            else
            {
                signal.BuyPattern.MaximumFavorableExcursion = down;
                signal.BuyPattern.MaximumAdverseExcursion = up;
            }

            if (signal.SellPattern.GetPatternType() == 1)
            {
                signal.SellPattern.MaximumFavorableExcursion = down;
                signal.SellPattern.MaximumAdverseExcursion = up;
            }
            else
            {
                signal.SellPattern.MaximumFavorableExcursion = up;
                signal.SellPattern.MaximumAdverseExcursion = down;
            }
        }

        protected virtual void OnBarUpdateScore(Signal signal, int barOffset, string type = "all")
        {
            switch (type)
            {
                case "dot":
                    OnBarUpdateScoreDot(signal, barOffset);
                    break;
                case "atr":
                    OnBarUpdateScoreTargetStop(signal, barOffset, "atr");
                    break;
                case "tick":
                    OnBarUpdateScoreTargetStop(signal, barOffset);
                    break;
                default:
                    OnBarUpdateScoreDot(signal, barOffset);
                    OnBarUpdateScoreTargetStop(signal, barOffset, "atr");
                    OnBarUpdateScoreTargetStop(signal, barOffset);
                    break;
            }
        }

        protected virtual void OnBarUpdateScoreDot(Signal signal, int barOffset)
        {
            if (!signal.HasData) return;
            if (signal.BuyPattern == null || signal.SellPattern == null) return;
            if (signal.BuyPatternNeutral == null || signal.SellPatternNeutral == null) return;

            var dotDiff = _settings.DotOffset * _parent.TickSize;

            var buyScores = new ScoreDot[2];
            var sellScores = new ScoreDot[2];

            buyScores[0] = signal.BuyPattern.ScoreDot = signal.BuyPattern.GetPatternType() == 1 ? new ScoreDot(signal.Low - dotDiff) : new ScoreDot(signal.High + dotDiff);
            buyScores[1] = signal.BuyPatternNeutral.ScoreDot = new ScoreDot(signal.Low - dotDiff);

            sellScores[0] = signal.SellPattern.ScoreDot = signal.SellPattern.GetPatternType() == 1 ? new ScoreDot(signal.High + dotDiff) : new ScoreDot(signal.Low - dotDiff);
            sellScores[1] = signal.SellPatternNeutral.ScoreDot = new ScoreDot(signal.High + dotDiff);

            var indexDiff = (signal.Next != null && signal.Next.HasData ? signal.Next.BarIndex : _parent.Time.Count) - signal.BarIndex;
            for (var i = 1; i < indexDiff; i++)
            {
                var offset = barOffset - i;
                if (offset < 0) continue;

                for (var j = 0; j < 2; j++)
                {
                    var score = buyScores[j];
                    if (score.IsSet()) continue;

                    if ((j == 0 && signal.BuyPattern.GetPatternType() == 1) || j == 1)
                    {
                        if (_settings.ScoresRequireClose)
                        {
                            if (_parent.Close[offset] < score.Dot)
                                score.HitDot(_parent.Time[offset]);
                        }
                        else
                        {
                            if (_parent.Low[offset] < score.Dot)
                                score.HitDot(_parent.Time[offset]);
                        }
                    }
                    else
                    {
                        if (_settings.ScoresRequireClose)
                        {
                            if (_parent.Close[offset] > score.Dot)
                                score.HitDot(_parent.Time[offset]);
                        }
                        else
                        {
                            if (_parent.High[offset] > score.Dot)
                                score.HitDot(_parent.Time[offset]);
                        }
                    }
                }

                for (var j = 0; j < 2; j++)
                {
                    var score = sellScores[j];
                    if (score.IsSet()) continue;

                    if ((j == 0 && signal.SellPattern.GetPatternType() == 1) || j == 1)
                    {
                        if (_settings.ScoresRequireClose)
                        {
                            if (_parent.Close[offset] > score.Dot)
                                score.HitDot(_parent.Time[offset]);
                        }
                        else
                        {
                            if (_parent.High[offset] > score.Dot)
                                score.HitDot(_parent.Time[offset]);
                        }
                    }
                    else
                    {
                        if (_settings.ScoresRequireClose)
                        {
                            if (_parent.Close[offset] < score.Dot)
                                score.HitDot(_parent.Time[offset]);
                        }
                        else
                        {
                            if (_parent.Low[offset] < score.Dot)
                                score.HitDot(_parent.Time[offset]);
                        }
                    }
                }
            }
        }

        protected virtual void OnBarUpdateScoreTargetStop(Signal signal, int barOffset, string type = "tick")
        {
            if (!signal.HasData) return;
            if (signal.BuyPattern == null || signal.SellPattern == null) return;
            if (signal.BuyPatternNeutral == null || signal.SellPatternNeutral == null) return;

            var settingBuy = _settings.ScoreSettings[signal.BuyPattern.GetPatternType()];
            var settingSell = _settings.ScoreSettings[signal.SellPattern.GetPatternType()];
            var settingNeutral = _settings.ScoreSettings[0];

            double buyTarget, buyStop, buyTargetN, buyStopN;
            double sellTarget, sellStop, sellTargetN, sellStopN;
            //ScoreTargetStop buyScore, sellScore, buyScoreNeutral, sellScoreNeutral;

            var buyScores = new ScoreTargetStop[2];
            var sellScores = new ScoreTargetStop[2];

            switch (type)
            {
                case "atr":
                    buyTarget = settingBuy.AtrTarget;
                    buyStop = settingBuy.AtrStop;

                    sellTarget = settingSell.AtrTarget;
                    sellStop = settingSell.AtrStop;

                    buyTargetN = sellTargetN = settingNeutral.AtrTarget;
                    buyStopN = sellStopN = settingNeutral.AtrStop;

                    buyScores[0] = signal.BuyPattern.GetPatternType() == 1
                        ? signal.BuyPattern.ScoreAtr = new ScoreTargetStop(signal.Close + (buyTarget * _parent.TickSize), signal.Close - (buyStop * _parent.TickSize))
                        : signal.BuyPattern.ScoreAtr = new ScoreTargetStop(signal.Close - (buyTarget * _parent.TickSize), signal.Close + (buyStop * _parent.TickSize));

                    buyScores[1] = signal.BuyPatternNeutral.ScoreAtr = new ScoreTargetStop(signal.Close + (buyTargetN * _parent.TickSize), signal.Close - (buyStopN * _parent.TickSize));

                    sellScores[0] = signal.SellPattern.GetPatternType() == 1
                        ? signal.SellPattern.ScoreAtr = new ScoreTargetStop(signal.Close - (sellTarget * _parent.TickSize), signal.Close + (sellStop * _parent.TickSize))
                        : signal.SellPattern.ScoreAtr = new ScoreTargetStop(signal.Close + (sellTarget * _parent.TickSize), signal.Close - (sellStop * _parent.TickSize));

                    sellScores[1] = signal.SellPatternNeutral.ScoreAtr = new ScoreTargetStop(signal.Close - (sellTargetN * _parent.TickSize), signal.Close + (sellStopN * _parent.TickSize));
                    break;
                default://defaults to tick
                    buyTarget = settingBuy.TickTarget;
                    buyStop = settingBuy.TickStop;

                    sellTarget = settingSell.TickTarget;
                    sellStop = settingSell.TickStop;

                    buyTargetN = sellTargetN = settingNeutral.TickTarget;
                    buyStopN = sellStopN = settingNeutral.TickStop;

                    buyScores[0] = signal.BuyPattern.GetPatternType() == 1
                        ? signal.BuyPattern.ScoreTick = new ScoreTargetStop(signal.Close + (buyTarget * _parent.TickSize), signal.Close - (buyStop * _parent.TickSize))
                        : signal.BuyPattern.ScoreTick = new ScoreTargetStop(signal.Close - (buyTarget * _parent.TickSize), signal.Close + (buyStop * _parent.TickSize));

                    buyScores[1] = signal.BuyPatternNeutral.ScoreTick = new ScoreTargetStop(signal.Close + (buyTargetN * _parent.TickSize), signal.Close - (buyStopN * _parent.TickSize));

                    sellScores[0] = signal.SellPattern.GetPatternType() == 1
                        ? signal.SellPattern.ScoreTick = new ScoreTargetStop(signal.Close - (sellTarget * _parent.TickSize), signal.Close + (sellStop * _parent.TickSize))
                        : signal.SellPattern.ScoreTick = new ScoreTargetStop(signal.Close + (sellTarget * _parent.TickSize), signal.Close - (sellStop * _parent.TickSize));

                    sellScores[1] = signal.SellPatternNeutral.ScoreTick = new ScoreTargetStop(signal.Close - (sellTargetN * _parent.TickSize), signal.Close + (sellStopN * _parent.TickSize));
                    break;
            }

            var found = 0;
            while (barOffset-- > 0)
            {
                for (var i = 0; i < 2; i++)
                {
                    var score = buyScores[i];
                    if (score.IsSet()) continue;

                    if ((i == 0 && signal.BuyPattern.GetPatternType() == 1) || i == 1)
                    {
                        if (_settings.ScoresRequireClose)
                        {
                            if (_parent.Close[barOffset] <= score.Stop)
                            {
                                score.HitStop(_parent.Time[barOffset]);
                                found++;
                            }
                            else if (_parent.Close[barOffset] >= score.Target)
                            {
                                score.HitTarget(_parent.Time[barOffset]);
                                found++;
                            }
                        }
                        else
                        {
                            if (_parent.Low[barOffset] <= score.Stop)
                            {
                                score.HitStop(_parent.Time[barOffset]);
                                found++;
                            }
                            else if (_parent.High[barOffset] >= score.Target)
                            {
                                score.HitTarget(_parent.Time[barOffset]);
                                found++;
                            }
                        }
                    }
                    else
                    {
                        if (_settings.ScoresRequireClose)
                        {
                            if (_parent.Close[barOffset] >= score.Stop)
                            {
                                score.HitStop(_parent.Time[barOffset]);
                                found++;
                            }
                            else if (_parent.Close[barOffset] <= score.Target)
                            {
                                score.HitTarget(_parent.Time[barOffset]);
                                found++;
                            }
                        }
                        else
                        {
                            if (_parent.High[barOffset] >= score.Stop)
                            {
                                score.HitStop(_parent.Time[barOffset]);
                                found++;
                            }
                            else if (_parent.Low[barOffset] <= score.Target)
                            {
                                score.HitTarget(_parent.Time[barOffset]);
                                found++;
                            }
                        }
                    }
                }

                for (var i = 0; i < 2; i++)
                {
                    var score = sellScores[i];
                    if (score.IsSet()) continue;

                    if ((i == 0 && signal.SellPattern.GetPatternType() == 1) || i == 1)
                    {
                        if (_settings.ScoresRequireClose)
                        {
                            if (_parent.Close[barOffset] >= score.Stop)
                            {
                                score.HitStop(_parent.Time[barOffset]);
                                found++;
                            }
                            else if (_parent.Close[barOffset] <= score.Target)
                            {
                                score.HitTarget(_parent.Time[barOffset]);
                                found++;
                            }
                        }
                        else
                        {
                            if (_parent.High[barOffset] >= score.Stop)
                            {
                                score.HitStop(_parent.Time[barOffset]);
                                found++;
                            }
                            else if (_parent.Low[barOffset] <= score.Target)
                            {
                                score.HitTarget(_parent.Time[barOffset]);
                                found++;
                            }
                        }
                    }
                    else
                    {
                        if (_settings.ScoresRequireClose)
                        {
                            if (_parent.Close[barOffset] <= score.Stop)
                            {
                                score.HitStop(_parent.Time[barOffset]);
                                found++;
                            }
                            else if (_parent.Close[barOffset] >= score.Target)
                            {
                                score.HitTarget(_parent.Time[barOffset]);
                                found++;
                            }
                        }
                        else
                        {
                            if (_parent.Low[barOffset] <= score.Stop)
                            {
                                score.HitStop(_parent.Time[barOffset]);
                                found++;
                            }
                            else if (_parent.High[barOffset] >= score.Target)
                            {
                                score.HitTarget(_parent.Time[barOffset]);
                                found++;
                            }
                        }
                    }
                }

                if (found == 4)
                    break;
            }
        }

        #endregion

        #region OnChange events

        public virtual void OnChangeScoreMethod()
        {
            Log("OnChangeScoreMethod");
        }

        public virtual void OnChangeScoreTime()
        {
            Log("OnChangeScoreTime");
            ScoreTally();
        }

        public virtual void OnChangeSwing()
        {
            Log("OnChangeSwing");
            ProcessTrigger(0);
        }

        public virtual void OnChangeDotOffset()
        {
            Log("OnChangeDotOffset");
            ProcessTrigger(0, ProcessScoreDot);
        }

        public virtual void OnChangeAtrValue()
        {
            Log("OnChangeAtrValue");
        }

        public virtual void OnChangeTargetStopAtr()
        {
            Log("OnChangeTargetStopAtr");
            ProcessTrigger(0, ProcessScoreTargetStopAtr);
        }

        public virtual void OnChangeTargetStopTick()
        {
            Log("OnChangeTargetStopTick");
            ProcessTrigger(0, ProcessScoreTargetStopTick);
        }

        public virtual void OnChangeScoreRequireStop()
        {
            Log("OnChangeScoreRequireStop");
            ProcessTrigger(0, ProcessScoreAll);
        }

        public virtual void OnChangeSlope()
        {
            Log("OnChangeSlope");
        }

        public virtual void OnChangePolarity()
        {
            Log("OnChangePolarity");
            ProcessTrigger(0, ProcessExport);
        }

        #endregion

        #region export

        public virtual void ExportSignals(string path)
        {
            if (First == null) return;
            using (var file = new StreamWriter(path))
            {
                {
                    var sb = new StringBuilder();
                    sb.Append("Time,Type");
                    sb.Append(",Open,High,Low,Close");
                    sb.AppendFormat(",Trend({0} {1}),Atr({2})", _settings.SlopeMethod, _settings.SlopePeriod, _settings.AtrValue);
                    //sb.Append(string.Format(",Swing45({0})", fIndSetting.GetSwing(2)));
                    sb.Append(",Pattern,MFE,MAE");
                    sb.Append(",Dot,Time,Value");
                    sb.Append(",ATR,Time,Value");
                    sb.Append(",Tick,Time,Value");
                    file.WriteLine(sb.ToString());
                }
                var node = First;
                while (node != null)
                {
                    var signal = node;
                    node = node.Next;

                    if (signal.IsFuture && !_debug) continue;

                    var sb = new StringBuilder();
                    sb.AppendFormat("{0},{1}", signal.BarTime, signal.IsBuySignal(_settings.IsReversePolarity) ? "Buy" : "Sell");
                    if (signal.HasData)
                    {
                        sb.AppendFormat(",{0},{1},{2},{3}", signal.Open, signal.High, signal.Low, signal.Close);
                        sb.AppendFormat(",{0},{1}", signal.Trend > 0 ? "Up" : "Down", signal.Atr);
                    }
                    else
                    {
                        sb.Append(",,,,,,");
                    }

                    Pattern pattern = null;
                    var patternNum = "";
                    //if @green line
                    if (signal.IsBuySignal(_settings.IsReversePolarity))
                    {
                        pattern = signal.BuyPattern;
                        patternNum = pattern != null ? pattern.GetPatternId(true).ToString() : "1";
                    }
                    else
                    {
                        pattern = signal.SellPattern;
                        patternNum = pattern != null ? pattern.GetPatternId(false).ToString() : "2";
                    }
                    sb.Append(",").Append(patternNum);

                    if (pattern != null)
                    {
                        sb.AppendFormat(",{0},{1}", pattern.MaximumFavorableExcursion, pattern.MaximumAdverseExcursion);

                        if (pattern.ScoreDot != null && pattern.ScoreDot.IsSet())
                        {
                            sb.AppendFormat(",Dot,{0},{1:F2}", pattern.ScoreDot.TimeDot, pattern.ScoreDot.Dot);
                        }
                        else
                        {
                            sb.Append(",,,");
                        }

                        if (pattern.ScoreAtr != null && pattern.ScoreAtr.IsSet())
                        {
                            sb.AppendFormat(",{0},{1},{2:F2}", pattern.ScoreAtr.IsSuccess() ? "Target" : "Stop", pattern.ScoreAtr.IsSuccess() ? pattern.ScoreAtr.TimeTarget : pattern.ScoreAtr.TimeStop, pattern.ScoreAtr.IsSuccess() ? pattern.ScoreAtr.Target : pattern.ScoreAtr.Stop);
                        }
                        else
                        {
                            sb.Append(",,,");
                        }

                        if (pattern.ScoreTick != null && pattern.ScoreTick.IsSet())
                        {
                            sb.AppendFormat(",{0},{1},{2:F2}", pattern.ScoreTick.IsSuccess() ? "Target" : "Stop", pattern.ScoreTick.IsSuccess() ? pattern.ScoreTick.TimeTarget : pattern.ScoreTick.TimeStop, pattern.ScoreTick.IsSuccess() ? pattern.ScoreTick.Target : pattern.ScoreTick.Stop);
                        }
                        else
                        {
                            sb.Append(",,,");
                        }
                    }
                    else
                    {
                        sb.Append(",,,,,,,,,");
                    }

                    file.WriteLine(sb.ToString());
                }
            }
        }

        public virtual void ExportSignals()
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Comma delimited file (*.csv)|*.csv|Text file (*.txt)|*.txt",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                FileName = string.Format("ExportForceSignals_{0}_{1}_{2}", _parent.Instrument.FullName,
                    _parent.BarsArray[_settings.BarsIndex].BarsPeriod.BarsPeriodType,
                    _parent.BarsArray[_settings.BarsIndex].BarsPeriod.Value)
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                ExportSignals(saveFileDialog.FileName);
            }
        }

        #endregion
    }

    #endregion
}
