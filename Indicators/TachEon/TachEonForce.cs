#region Using declarations
using Microsoft.Win32;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript.AddOns.DtwAddOns;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui.FxBoard;

#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators.TachEon
{
    [CategoryOrder("Basics", 1000)]//NamingCategoryBasics
    [CategoryOrder("Force Signals", 2000)]//NamingCategoryForceSignals
    [CategoryOrder("Angles", 5000)]//NamingCategoryAngles
    [CategoryOrder("Dots", 6000)]//NamingCategoryDots
    [CategoryOrder("Open Price Lines", 7000)]//NamingCategoryOpenPriceLines
    [CategoryOrder("Swings", 7100)]//NamingCategorySwings
    [CategoryOrder("Patterns", 7200)]//NamingCategoryPatterns
    [CategoryOrder("Scores", 7300)]//NamingCategoryScores
    [CategoryOrder("Slope", 8000)]//NamingCategorySlope
    [CategoryOrder("Other", 9000)]//NamingCategoryOther
    [CategoryOrder("Programmatic", 10000)]//NamingCategoryProg
    [TypeConverter("NinjaTrader.NinjaScript.Indicators.TachEon.ForceConverter")]
    public class TachEonForce : Indicator
    {
        #region Global Variables

        /// <summary>
        /// global boolean for beta
        /// </summary>
        private const bool __betaStuff = true;

        /// <summary>
        /// global boolean for info tab
        /// </summary>
        private static bool __infoEnabled = true;

        /// <summary>
        /// originally used for logging purposes, but logging not enabled at this point
        /// </summary>
        private string __internalId = Guid.NewGuid().ToString("N");

        #region naming const
        /// <summary>
        /// variables that are used for indicator property panel
        /// they have to be const (not static) 
        /// this is an attempt to keep the language used through the UI consistent (no strings, just constants)
        /// </summary>
        private const string NamingCategoryBasics = "Basics";
        private const string NamingCategoryForceSignals = "Force Signals";
        private const string NamingCategoryAngles = "Angles";
        private const string NamingCategoryDots = "Dots";
        private const string NamingCategoryOpenPriceLines = "Open Price Lines";
        private const string NamingCategoryPatterns = "Patterns";
        private const string NamingCategoryScores = "Scores";
        private const string NamingCategorySwings = "Swings";
        private const string NamingCategorySlope = "Slope";
        private const string NamingCategoryOther = "Other";
        private const string NamingCategoryProg = "Programmatic";

        private const string NamingStyle = "Style ";
        private const string NamingWidth = "Width ";
        private const string NamingColor = "Color ";

        private const string NamingBuy = "Buy ";
        private const string NamingSell = "Sell ";
        private const string NamingOffset = "Tick Offset ";

        private const string NamingAnchor = "Anchor ";
        private const string NamingForceLine = "Force ";
        private const string NamingResAngleRay = "Ray ";
        private const string NamingResAngle = "ResAngle ";
        private const string NamingResAngleReverse = "Reverse ";

        private const string NamingScoreSuccess = "Success ";
        private const string NamingScoreFailure = "Failure ";
        #endregion

        #region naming static
        /// <summary>
        /// while these are not used in the indicator property window,
        /// need constant string values for consistency
        /// </summary>
        private static string NamingDot = "Dot";
        private static string NamingOpl = "Opl";
        private static string NamingHandle = "Handle ";
        private static string NamingPattern = "Pattern ";

        private static string NamingChart = "Chart ";

        #endregion

        #region fomatting statics
        private static string FormatSerialDate = "yyyyMMddTHHmmss";

        private static string ForSerialClass = "^";
        private static string ForSerialVersion = ":";
        private static string ForSerialItem = ",";
        #endregion

        /// <summary>
        /// used for the loading/saving of files
        /// </summary>
        private static string __defaultFilePath = Path.Combine(Core.Globals.UserDataDir, "TachEon", "Force");
        private static string __defaultFileExtension = ".txt";

        private static string __dirForSignals = "Lines";
        private static string __dirForAngles = "Angles";

        /// <summary>
        /// <see cref="OnStateChange"/> "turns on" these functions (or at least the part that needs certain objects created (like brushes, etc)
        /// </summary>
        private bool __onRender;
        private bool __onBarUpdate;

        /// <summary>
        /// chart interval must be time based, but if intraday, the bartime is different for non-intraday bars
        /// </summary>
        /// <param name="bars"></param>
        private bool IsIntraday(Bars bars)
        {
            switch (bars.BarsPeriod.BarsPeriodType)
            {
                case BarsPeriodType.Second:
                    return true;
                case BarsPeriodType.Minute:
                    return true;
            }
            return false;
        }

        #endregion Global Variables

        #region class ForceException

        /*
         * Customized exception to trap for future logging, etc
         */

        private class ForceException : Exception
        {
            public ForceException(TachEonForce parent, string msg) : base(msg)
            {
                if (__debugging)
                    parent.UserMessage(string.Format("Error: {0}", msg));
                else
                    parent.Log(msg);
            }

            public ForceException(string msg) : base(msg)
            {
                //TODO log errors
                throw new Exception(msg);
            }
        }

        #endregion class ForceException

        #region DEBUGGING

        /// <summary>
        /// logging to console is only done if debugging is on
        /// </summary>
        private static bool __debugging = false;

        protected void Log(object toLog)
        {
            if (!__debugging) return;
            if (Dispatcher == null) return;
            if (Dispatcher.CheckAccess())
            {
                Code.Output.Process(string.Format("TachEonForce {2}: {1}: {0}", toLog.ToString(), Instrument.FullName, DateTime.Now), PrintTo.OutputTab1);
            }
            else
            {
                Dispatcher.InvokeAsync(() =>
                {
                    Code.Output.Process(string.Format("TachEonForce {2}: {1}: {0}", toLog.ToString(), Instrument.FullName, DateTime.Now), PrintTo.OutputTab1);
                });
            }
        }

        /// <summary>
        /// when <see cref="__debugging"/>, there is a button added to the chart that calls this function
        /// add whatever here
        /// </summary>
        private void DebugAction()
        {
            __processor.OnBarUpdate(true);
            //TriggerCustomEvent(o =>
            //{
            //    Log(Time[0]);
            //    for (var i = 0; i < 40; i++)
            //    {
            //        Log("" + Time[i] + ": " + Values[3][i] + "     --- " + Values[1][i] + " - " + Values[2][i]);
            //    }

            //}, 0, null);
            //var signal = __processor.Last;
            //var count = 5;
            //while (count > 0)
            //{
            //    Log(signal.BarTime + " - " + signal.IsFuture);
            //    if (!signal.IsFuture) count--;
            //    signal = signal.Previous;
            //}
            //var signal = __processor.CurrentSignal;
            //var count = 10;
            //while (count-- > 0)
            //{
            //    Log(signal.BarTime + " isBuy: " + signal.IsBuySignal(__settings.IsReversePolarity));
            //    Log("  buyPatternId " + signal.BuyPattern.GetPatternId(signal.IsBuySignal(__settings.IsReversePolarity)));
            //    Log("  sellPatternId " + signal.SellPattern.GetPatternId(signal.IsBuySignal(__settings.IsReversePolarity)));
            //    signal = signal.Previous;
            //}

            //Log(signal.BarTime + " isBuy: " + signal.Previous.IsBuySignal(__settings.IsReversePolarity));
            //Log("  buyPatternId " + signal.Previous.BuyPattern.GetPatternId(signal.Previous.IsBuySignal(__settings.IsReversePolarity)));
            //Log("  sellPatternId " + signal.Previous.SellPattern.GetPatternId(signal.Previous.IsBuySignal(__settings.IsReversePolarity)));
        }

        #endregion DEBUGGING

        #region LOGGING

        /// <summary>
        /// not fully tested yet
        /// </summary>
        private static bool __logging = false;

        private class ForceLog
        {
            private static Dictionary<string, object> Locks = new Dictionary<string, object>();

            private object _lock;
            private readonly string _path;
            private readonly string _id;

            public ForceLog(string path, string id)
            {
                _path = Path.Combine(__defaultFilePath, "Logs", string.Format("{0}.txt", path));
                _id = id;
                if (__logging)
                {
                    lock (Locks)
                    {
                        if (Locks.ContainsKey(_id))
                            _lock = Locks[_id];
                        else
                        {
                            _lock = new object();
                            Locks.Add(_id, _lock);
                        }
                        var fileInfo = new FileInfo(_path);
                        if (!fileInfo.Exists)
                            Directory.CreateDirectory(fileInfo.Directory.FullName);
                    }
                }
            }

            public void Log(object toLog)
            {
                if (!__logging) return;
                lock (_lock)
                {
                    try
                    {
                        using (var writer = File.AppendText(_path))
                        {
                            writer.WriteLine(string.Format("{0}:{1}:{2}", _id, DateTime.Now.ToString(FormatSerialDate), toLog.ToString()));
                        }
                    }
                    catch (Exception err)
                    {
                        var help = err.Message;
                    }
                }
            }
        }

        private ForceLog __log;

        #endregion LOGGING

        #region Enums

        public enum eForceLineCalcMethod
        {
            Fibs,
            BarCount
        }

        public enum eForceMessages
        {
            ChartTopLeft,
            ChartTopRight,
            ChartBottomLeft,
            ChartBottomRight,
            ChartCenter,
            //Log,
            Output1,
            Output2
        }

        #endregion Enums

        #region user messages

        private void UserMessageSync(string txt)
        {
            if (Dispatcher == null) return;
            try
            {
                txt = string.Format("TachEonForce Indicator: {0}", txt);
                switch (_userMessagePlacement)
                {
                    case eForceMessages.Output1:
                        Code.Output.Process(txt, PrintTo.OutputTab1);
                        break;
                    case eForceMessages.Output2:
                        Code.Output.Process(txt, PrintTo.OutputTab2);
                        break;
                    case eForceMessages.ChartTopLeft:
                        Draw.TextFixed(this, "userMsg", txt, TextPosition.TopLeft);
                        break;
                    case eForceMessages.ChartTopRight:
                        Draw.TextFixed(this, "userMsg", txt, TextPosition.TopRight);
                        break;
                    case eForceMessages.ChartBottomLeft:
                        Draw.TextFixed(this, "userMsg", txt, TextPosition.BottomLeft);
                        break;
                    case eForceMessages.ChartBottomRight:
                        Draw.TextFixed(this, "userMsg", txt, TextPosition.BottomRight);
                        break;
                    case eForceMessages.ChartCenter:
                        Draw.TextFixed(this, "userMsg", txt, TextPosition.Center);
                        break;
                    default:
                        Code.Output.Process(txt, PrintTo.OutputTab1);
                        break;
                }
            }
            catch (Exception err)
            {
                throw new ForceException(string.Format("UserMessage: {0}", err.Message));
            }

        }

        private void UserMessage(string txt)
        {
            if (Dispatcher == null) return;
            try
            {
                if (Dispatcher.CheckAccess())
                {
                    UserMessageSync(txt);
                }
                else
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        UserMessageSync(txt);
                    });
                }
            }
            catch (Exception err)
            {
                throw new ForceException(string.Format("UserMessage: {0}", err.Message));
            }

        }

        #endregion user messages

        #region class ForceHistory
        //NONE
        #endregion class ForceHistory

        #region class ForceExtensionLine

        /// <summary>
        /// dota/open price lines/angles can be drawn with certain parameters
        /// to keep it co-located, used a class
        /// </summary>
        private class ForceExtensionLine
        {
            public eTachEonExtensionMethods Method;
            public int Count;
        }

        private ForceExtensionLine __angleExtension;
        private ForceExtensionLine __dotExtension;
        private ForceExtensionLine __oplExtension;

        #endregion class ForceExtensionLine

        #region class ForceAnchor

        /// <summary>
        /// class ForceAnchor contains the data to compute signals and angles
        /// as well as the user editable handles
        /// 
        /// in simplest form, this is just a start and end time/price
        /// 
        /// a hack serialization is used to be able to store to the workspace as well as saving/loading data via files
        /// </summary>
        private class ForceAnchor : IDisposable, ICloneable
        {
            #region properties

            private DateTime _startTime = DateTime.MinValue;
            public DateTime StartTime { get { return _startTime; } }

            private DateTime _endTime = DateTime.MinValue;
            public DateTime EndTime { get { return _endTime; } }

            private double _startPrice = double.MinValue;
            public double StartPrice { get { return _startPrice; } }

            private double _endPrice = double.MinValue;
            public double EndPrice { get { return _endPrice; } }

            public bool IsSet { get { return !_startTime.Equals(DateTime.MinValue) && !_endTime.Equals(DateTime.MinValue) && !_startPrice.Equals(double.MinValue) && !_endPrice.Equals(double.MinValue); } }

            public bool IsLeftToRight { get { return _startTime.CompareTo(_endTime) < 0; } }

            public event EventHandler OnReset;
            public event EventHandler OnChange;
            public event EventHandler OnStateChange;

            #endregion

            #region public functions

            #region set properties

            private void set(DateTime startTime, double startPrice, DateTime endTime, double endPrice)
            {
                _startTime = startTime;
                _startPrice = startPrice;
                _endTime = endTime;
                _endPrice = endPrice;
                if (OnChange == null) return;
                OnChange(this, EventArgs.Empty);
            }

            public void Set(DateTime startTime, double startPrice, DateTime endTime, double endPrice)
            {
                set(startTime, startPrice, endTime, endPrice);
            }

            public void SetStart(DateTime time, double price)
            {
                set(time, price, _endTime, _endPrice);
            }

            public void SetStart(DateTime time)
            {
                set(time, _startPrice, _endTime, _endPrice);
            }

            public void SetStart(double price)
            {
                set(_startTime, price, _endTime, _endPrice);
            }

            public void SetEnd(DateTime time, double price)
            {
                set(_startTime, _startPrice, time, price);
            }

            public void SetEnd(DateTime time)
            {
                set(_startTime, _startPrice, time, _endPrice);
            }

            public void SetEnd(double price)
            {
                set(_startTime, _startPrice, _endTime, price);
            }

            #endregion

            #region swapping start/end properties

            public void Reverse()
            {
                set(_endTime, _endPrice, _startTime, _startPrice);
            }

            public void Normalize()
            {
                if (_startTime.CompareTo(_endTime) > 0)
                {
                    Reverse();
                }
            }

            #endregion

            public void Reset()
            {
                _startTime = DateTime.MinValue;
                _startPrice = double.MinValue;
                _endTime = DateTime.MinValue;
                _endPrice = double.MinValue;
                if (OnReset == null) return;
                OnReset(this, EventArgs.Empty);
            }

            #region serialize

            public string ToSerialString()
            {
                if (IsSet)
                {
                    return string.Format("{0}@{1}>{2}@{3}",
                        StartPrice.ToString(CultureInfo.InvariantCulture),
                        StartTime.ToUniversalTime().ToString(FormatSerialDate),
                        EndPrice.ToString(CultureInfo.InvariantCulture),
                        EndTime.ToUniversalTime().ToString(FormatSerialDate));
                }
                return "";
            }

            public bool FromSerialString(string str)
            {
                str = str.Trim();
                var anchors = str.Split(new string[] { ">" }, StringSplitOptions.None);
                if (anchors.Length == 2)
                {
                    var start = anchors[0].Split(new string[] { "@" }, StringSplitOptions.None);
                    var end = anchors[1].Split(new string[] { "@" }, StringSplitOptions.None);
                    if (start.Length == 2 && end.Length == 2)
                    {
                        DateTime startTime, endTime;
                        double startPrice, endPrice;
                        if (DateTime.TryParseExact(start[1], FormatSerialDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out startTime)
                            && double.TryParse(start[0], NumberStyles.Any, CultureInfo.InvariantCulture, out startPrice)
                            && DateTime.TryParseExact(end[1], FormatSerialDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out endTime)
                            && double.TryParse(end[0], NumberStyles.Any, CultureInfo.InvariantCulture, out endPrice))
                        {
                            set(startTime.ToLocalTime(), startPrice, endTime.ToLocalTime(), endPrice);
                            return true;
                        }
                    }
                }
                return false;
            }

            public static bool IsValidSerialString(string str)
            {
                str = str.Trim();
                var regex = new Regex(@"^[\d]+(\.[\d]+)?@[\d]{8}T[\d]{6}\>[\d]+(\.[\d]+)?@[\d]{8}T[\d]{6}$", RegexOptions.Compiled);
                return regex.IsMatch(str);
            }

            #endregion

            #endregion

            #region constructors / overrides

            public ForceAnchor()
            {
                _state = DrawingState.Normal;
            }

            protected ForceAnchor(ForceAnchor copy) : this()
            {
                _startTime = copy._startTime;
                _endTime = copy._endTime;
                _startPrice = copy._startPrice;
                _endPrice = copy._endPrice;
            }

            public override bool Equals(Object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;
                var otherObj = obj as ForceAnchor;
                return _startTime.Equals(otherObj._startTime)
                    && _endTime.Equals(otherObj._endTime)
                    && _startPrice.Equals(otherObj._startPrice)
                    && _endPrice.Equals(otherObj._endPrice);
            }

            public object Clone()
            {
                return new ForceAnchor(this);
            }

            public override string ToString()
            {
                return string.Format("{0} @ {1} - {2} @ {3}", _startPrice, _startTime, _endPrice, _endTime);
            }

            public void Dispose()
            {
                foreach (var d in OnReset.GetInvocationList())
                {
                    OnReset -= (EventHandler)d;
                }
                foreach (var d in OnChange.GetInvocationList())
                {
                    OnChange -= (EventHandler)d;
                }
                foreach (var d in OnStateChange.GetInvocationList())
                {
                    OnStateChange -= (EventHandler)d;
                }
            }

            #endregion

            #region drawing info

            public enum DrawingState
            {
                Normal,
                Building,
                EditingStart,
                EditingEnd,
                Moving
            }

            private DrawingState _state;

            public DrawingState State
            {
                get { return _state; }
                set
                {
                    if (_state != value)
                    {
                        _state = value;
                        if (OnStateChange == null) return;
                        OnStateChange(this, EventArgs.Empty);
                    }
                }
            }

            public SharpDX.Direct2D1.Ellipse CenterHandle;
            public SharpDX.Direct2D1.Ellipse CenterHandleFrozen;
            public SharpDX.Direct2D1.Ellipse StartHandle;
            public SharpDX.Direct2D1.Ellipse StartHandleFrozen;
            public SharpDX.Direct2D1.Ellipse EndHandle;
            public SharpDX.Direct2D1.Ellipse EndHandleFrozen;

            public bool IsDrawn;

            // determine which drawing state is being invoked based on the postion of the incoming mouse coordinate
            public DrawingState WhichHandle(System.Windows.Point click)
            {
                var handle = CenterHandle;
                var width = handle.RadiusX;
                if (click.X >= handle.Point.X - width
                    && click.X <= handle.Point.X + width
                    && click.Y >= handle.Point.Y - width
                    && click.Y <= handle.Point.Y + width)
                {
                    return DrawingState.Moving;
                }
                handle = StartHandle;
                width = handle.RadiusX;
                if (click.X >= handle.Point.X - width
                    && click.X <= handle.Point.X + width
                    && click.Y >= handle.Point.Y - width
                    && click.Y <= handle.Point.Y + width)
                {
                    return DrawingState.EditingStart;
                }
                handle = EndHandle;
                width = handle.RadiusX;
                if (click.X >= handle.Point.X - width
                    && click.X <= handle.Point.X + width
                    && click.Y >= handle.Point.Y - width
                    && click.Y <= handle.Point.Y + width)
                {
                    return DrawingState.EditingEnd;
                }
                return DrawingState.Normal;
            }

            #endregion
        }

        #endregion class ForceAnchor

        #region class ForceEntity / ForceEntitySignal / ForceEntityAngle

        /// <summary>
        /// base class for the ForceEntity
        /// the entity contains the info for drawing/calculating
        /// hack serialization is used for storage to workspace and files
        /// </summary>
        private class ForceEntity : IDisposable
        {
            private static int Version = 1;

            #region Properties

            //protected object Lock = new object();

            private string _instrument;

            public string Instrument
            {
                get { return _instrument; }
                protected set { _instrument = value; }
            }

            private BarsPeriodType _periodType;

            public BarsPeriodType PeriodType
            {
                get { return _periodType; }
                protected set { _periodType = value; }
            }

            private int _periodValue;

            public int PeriodValue
            {
                get { return _periodValue; }
                protected set { _periodValue = value; }
            }

            private ForceAnchor _anchor;

            public ForceAnchor Anchor
            {
                get { return _anchor; }
                protected set { _anchor = value; }
            }

            private ForceEntity _from = null;

            public ForceEntity From { get { return _from; } }

            public virtual event EventHandler OnLoad;

            protected void RaiseOnLoad()
            {
                if (OnLoad == null) return;
                OnLoad(this, EventArgs.Empty);
            }

            public virtual event EventHandler OnReset;

            protected void RaiseOnReset()
            {
                if (OnReset == null) return;
                OnReset(this, EventArgs.Empty);
            }

            public virtual event EventHandler OnAnchorChange;

            protected void RaiseOnAnchorChange()
            {
                if (OnAnchorChange == null) return;
                OnAnchorChange(this, EventArgs.Empty);
            }

            #endregion

            #region constructors / overrides

            public ForceEntity()
            {
                Anchor = new ForceAnchor();
            }

            public ForceEntity(string instrument, BarsPeriodType perType, int perVal)
            {
                _instrument = instrument;
                _periodType = perType;
                _periodValue = perVal;
                Anchor = new ForceAnchor();
            }

            protected ForceEntity(ForceEntity copy)
            {
                _instrument = copy._instrument;
                _periodType = copy._periodType;
                _periodValue = copy._periodValue;
                Anchor = (ForceAnchor)copy.Anchor.Clone();
                _from = copy._from;
            }

            public override string ToString()
            {
                return string.Format("ForceEntity: {0}", Anchor);
            }

            public void Dispose()
            {
                if (OnReset != null)
                {
                    foreach (var d in OnReset.GetInvocationList())
                    {
                        OnReset -= (EventHandler)d;
                    }
                }

                if (OnAnchorChange != null)
                {
                    foreach (var d in OnAnchorChange.GetInvocationList())
                    {
                        OnAnchorChange -= (EventHandler)d;
                    }
                }

                if (OnLoad != null)
                {
                    foreach (var d in OnLoad.GetInvocationList())
                    {
                        OnLoad -= (EventHandler)d;
                    }
                }
            }

            #endregion

            #region public functions

            public void Set(string instrument, BarsPeriodType perType, int perVal)
            {
                _instrument = instrument;
                _periodType = perType;
                _periodValue = perVal;
            }

            public virtual void Reset()
            {
                //lock (Lock)
                {
                    Anchor.Reset();
                    if (OnReset == null) return;
                    OnReset(this, EventArgs.Empty);
                }
            }

            protected virtual int AnchorBarMin()
            {
                return 1;
            }

            private void AnchorTime(Bars bars, int left, int right)
            {
                var isIntraday = bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Minute
                                 || bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Second;
                if (Anchor.IsLeftToRight)
                {
                    var startIndex = bars.GetBar(Anchor.StartTime);
                    var endIndex = bars.GetBar(Anchor.EndTime);
                    if ((endIndex + right) - (startIndex + left) < AnchorBarMin())
                        return;

                    if (left != 0 && right != 0)
                    {
                        if (startIndex + left >= 0 && endIndex + right < bars.Count)
                        {
                            Anchor.SetStart(isIntraday ? bars.GetTime(startIndex + left) : bars.GetSessionEndTime(startIndex + left));
                            Anchor.SetEnd(isIntraday ? bars.GetTime(endIndex + right) : bars.GetSessionEndTime(endIndex + right));
                        }
                    }
                    else
                    {
                        if (left != 0 && startIndex + left >= 0)
                        {
                            Anchor.SetStart(isIntraday ? bars.GetTime(startIndex + left) : bars.GetSessionEndTime(startIndex + left));
                        }
                        if (right != 0 && endIndex + right < bars.Count)
                        {
                            Anchor.SetEnd(isIntraday ? bars.GetTime(endIndex + right) : bars.GetSessionEndTime(endIndex + right));
                        }
                    }
                }
                else
                {
                    var startIndex = bars.GetBar(Anchor.EndTime);
                    var endIndex = bars.GetBar(Anchor.StartTime);
                    if ((endIndex + right) - (startIndex + left) < AnchorBarMin())
                        return;

                    if (left != 0 && right != 0)
                    {
                        if (startIndex + left >= 0 && endIndex + right < bars.Count)
                        {
                            Anchor.SetEnd(isIntraday ? bars.GetTime(startIndex + left) : bars.GetSessionEndTime(startIndex + left));
                            Anchor.SetStart(isIntraday ? bars.GetTime(endIndex + right) : bars.GetSessionEndTime(endIndex + right));
                        }
                    }
                    else
                    {
                        if (left != 0 && startIndex + left >= 0)
                        {
                            Anchor.SetEnd(isIntraday ? bars.GetTime(startIndex + left) : bars.GetSessionEndTime(startIndex + left));
                        }
                        if (right != 0 && endIndex + right < bars.Count)
                        {
                            Anchor.SetStart(isIntraday ? bars.GetTime(endIndex + right) : bars.GetSessionEndTime(endIndex + right));
                        }
                    }
                }

                if (OnAnchorChange == null) return;
                OnAnchorChange(this, EventArgs.Empty);
            }

            public void AnchorTimeLeft(Bars bars)
            {
                AnchorTime(bars, -1, -1);
            }

            public void AnchorTimeExpandLeft(Bars bars)
            {
                AnchorTime(bars, -1, 0);
            }

            public void AnchorTimeContractLeft(Bars bars)
            {
                AnchorTime(bars, 1, 0);
            }

            public void AnchorTimeRight(Bars bars)
            {
                AnchorTime(bars, 1, 1);
            }

            public void AnchorTimeExpandRight(Bars bars)
            {
                AnchorTime(bars, 0, 1);
            }

            public void AnchorTimeContractRight(Bars bars)
            {
                AnchorTime(bars, 0, -1);
            }

            #region serialize

            public virtual string ToSerialString()
            {
                return string.Format("FE_{0}{1}{2}{3}{4}{5}{6}{7}{8}",
                    Version.ToString(CultureInfo.InvariantCulture),
                    ForSerialVersion,
                    _instrument,
                    ForSerialItem,
                    _periodType.ToString(),
                    ForSerialItem,
                    _periodValue.ToString(CultureInfo.InvariantCulture),
                    ForSerialItem,
                    Anchor.ToSerialString());
            }

            public virtual void FromSerialString(string str)
            {
                str = str.Trim();
                if (!str.StartsWith("FE_"))
                {
                    str = str.Substring(str.IndexOf("FE_"));
                }
                var myParts = str.Split(new string[] { ForSerialVersion }, StringSplitOptions.None);
                if (myParts.Length == 2)
                {
                    var parts = myParts[1].Split(new string[] { ForSerialItem }, StringSplitOptions.None);
                    try
                    {
                        _from = new ForceEntity
                        {
                            _instrument = parts[0],
                            _periodType = (BarsPeriodType)Enum.Parse(typeof(BarsPeriodType), parts[1]),
                            _periodValue = int.Parse(parts[2], CultureInfo.InvariantCulture)
                        };
                        _from.Anchor.FromSerialString(parts[3]);
                    }
                    catch (ArgumentException)
                    {
                        _from = null;
                    }
                    if (_from != null)
                    {
                        if (_periodValue == 0)
                        {
                            _instrument = _from._instrument;
                            _periodType = _from._periodType;
                            _periodValue = _from._periodValue;
                        }
                        Anchor.FromSerialString(parts[3]);
                    }
                }
            }

            public static bool IsValidSerialString(string str)
            {
                str = str.Trim();
                var parts = str.Split(new string[] { ForSerialItem }, StringSplitOptions.None);
                if (parts.Length == 4 && ForceAnchor.IsValidSerialString(parts[3]))
                {
                    var regex = new Regex(@"^FE_[\d]+\" + ForSerialVersion + ".+", RegexOptions.Compiled);
                    if (regex.IsMatch(parts[0]))
                    {
                        try
                        {
                            var type = (BarsPeriodType)Enum.Parse(typeof(BarsPeriodType), parts[1]);
                            regex = new Regex(@"^[\d]+", RegexOptions.Compiled);
                            return regex.IsMatch(parts[2]);
                        }
                        catch (ArgumentException)
                        {
                        }
                    }
                }
                return false;
            }

            #endregion

            // compares the timeframe to verify whether this entity can work with the indicator
            public virtual bool IsCompatible(TachEonForce indicator)
            {
                var parentType = indicator.BarsArray[0].BarsPeriod.BarsPeriodType;
                var parentValue = indicator.BarsArray[0].BarsPeriod.Value;
                if (PeriodType == parentType && PeriodValue == parentValue)
                    return true;
                else
                {
                    switch (parentType)
                    {
                        case BarsPeriodType.Second:
                            if (PeriodType == BarsPeriodType.Second)
                            {
                                if (PeriodValue >= parentValue)
                                {
                                    if (parentValue % PeriodValue == 0)
                                        return true;
                                }
                            }
                            else if (PeriodType == BarsPeriodType.Minute)
                            {
                                if (60 % PeriodValue == 0)//if (parentValue % (PeriodValue * 60) == 0)
                                    return true;
                            }
                            else
                            {
                                return true;
                            }
                            break;
                        case BarsPeriodType.Minute:
                            if (PeriodType == BarsPeriodType.Minute)
                            {
                                if (PeriodValue >= parentValue)
                                {
                                    if (PeriodValue % parentValue == 0)
                                        return true;
                                }
                            }
                            else if (PeriodType != BarsPeriodType.Second)
                            {
                                return true;
                            }
                            break;
                        case BarsPeriodType.Day:
                            if (PeriodType == BarsPeriodType.Week || PeriodType == BarsPeriodType.Month || PeriodType == BarsPeriodType.Year)
                            {
                                return true;
                            }
                            break;
                    }
                }
                return false;
            }

            #endregion

        }

        /// <summary>
        /// class that contains more info pertinent to the Force Signal
        /// </summary>
        private class ForceEntitySignal : ForceEntity, ICloneable
        {
            public static int Version = 1;

            #region properties

            private eForceLineCalcMethod _calcMethod;

            public eForceLineCalcMethod CalculationMethod
            {
                get { return _calcMethod; }
                set
                {
                    if (_calcMethod != value)
                    {
                        _calcMethod = value;
                        //if (OnMethodChange == null) return;
                        //OnMethodChange(this, EventArgs.Empty);
                    }
                }
            }

            private bool _resetsToAnchorTime;

            public bool ResetsToAnchorTime
            {
                get { return _resetsToAnchorTime; }
                set
                {
                    if (_resetsToAnchorTime != value)
                    {
                        _resetsToAnchorTime = value;
                        RaiseOnAnchorChange();
                    }
                }
            }

            private int _barCount = 0;

            public int BarCount
            {
                get { return _barCount; }
                set
                {
                    if (_barCount != value)
                    {
                        _barCount = value;
                        //if (OnBarCountChange == null) return;
                        //OnBarCountChange(this, EventArgs.Empty);
                    }
                }
            }

            private int _pulseDivision = 3;

            public int PulseDivision
            {
                get { return _pulseDivision; }
                set
                {
                    if (value % 2 == 1)
                        _pulseDivision = value;
                }
            }

            private int _polarity = 1;

            public int Polarity
            {
                get { return _polarity; }
                set
                {
                    if (Math.Abs(value) == 1 && value != _polarity)
                    {
                        _polarity = value;
                        //if (OnPolarityChange == null) return;
                        //OnPolarityChange(this, EventArgs.Empty);
                    }
                }
            }

            public bool IsReversePolarity { get { return _polarity < 0; } }

            //public virtual event EventHandler OnMethodChange;
            //public virtual event EventHandler OnResetChange;
            //public virtual event EventHandler OnBarCountChange;
            //public virtual event EventHandler OnPolarityChange;

            #endregion

            #region public functions

            public void SetPolarityToPositive()
            {
                Polarity = 1;
            }

            public void SetPolarityToNegative()
            {
                Polarity = -1;
            }

            public void SwitchPolarity()
            {
                Polarity = Polarity * -1;
            }

            public override void Reset()
            {
                //lock (Lock)
                {
                    _polarity = 1;
                }
                base.Reset();
            }

            #region serialize

            public override string ToSerialString()
            {
                var line = string.Format("{0}{1}{2:D4}{3:D4}{4}",
                    IsReversePolarity ? "-" : "+",
                    CalculationMethod == eForceLineCalcMethod.BarCount ? 1 : 0,
                    BarCount,
                    PulseDivision,
                    ResetsToAnchorTime ? "+" : "-");
                return string.Format("FEP_{0}{1}{2}{3}{4}",
                    Version.ToString(CultureInfo.InvariantCulture),
                    ForSerialVersion,
                    line,
                    ForSerialClass,
                    base.ToSerialString());
            }

            public override void FromSerialString(string str)
            {
                str = str.Trim();
                var classParts = str.Split(new string[] { ForSerialClass }, StringSplitOptions.None);
                if (classParts.Length == 2)
                {
                    base.FromSerialString(classParts[1]);

                    if (From != null)
                    {
                        var myParts = classParts[0].Split(new string[] { ForSerialVersion }, StringSplitOptions.None);
                        if (myParts.Length == 2)
                        {
                            var parts = myParts[1].Split(new string[] { ForSerialItem }, StringSplitOptions.None);

                            if (parts.Length > 0)
                            {
                                if (parts[0].Length == 10)
                                {
                                    _polarity = parts[0].Substring(0, 1).Equals("-") ? -1 : 1;
                                    CalculationMethod = parts[0].Substring(1, 1).Equals("1") ? eForceLineCalcMethod.BarCount : eForceLineCalcMethod.Fibs;
                                    BarCount = int.Parse(parts[0].Substring(2, 4));
                                    PulseDivision = int.Parse(parts[0].Substring(6));
                                }
                                else if (parts[0].Length == 11)
                                {
                                    _polarity = parts[0].Substring(0, 1).Equals("-") ? -1 : 1;
                                    CalculationMethod = parts[0].Substring(1, 1).Equals("1") ? eForceLineCalcMethod.BarCount : eForceLineCalcMethod.Fibs;
                                    BarCount = int.Parse(parts[0].Substring(2, 4));
                                    PulseDivision = int.Parse(parts[0].Substring(6, 4));
                                    _resetsToAnchorTime = parts[0].Substring(10).Equals("+");
                                }
                            }
                        }
                        //Anchor.OnAnchorChange
                    }
                }
            }

            new public static bool IsValidSerialString(string str)
            {
                if (str == null) return false;
                str = str.Trim();
                var classParts = str.Split(new string[] { ForSerialClass }, StringSplitOptions.None);
                if (classParts.Length == 2)
                {
                    if (ForceEntity.IsValidSerialString(classParts[1]))
                    {
                        var regex = new Regex(@"^FEP_[\d]+\" + ForSerialVersion + @"(\+|\-)(0|1)[\d]{8}", RegexOptions.Compiled);
                        return regex.IsMatch(classParts[0]);
                    }
                }
                return false;
            }

            #endregion

            #endregion

            #region constructors / overrides

            private void OnAnchorChange(object sender, EventArgs e)
            {
                if (Anchor.State != ForceAnchor.DrawingState.Normal) return;
                RaiseOnAnchorChange();
            }

            public ForceEntitySignal()
            {
                Anchor.OnStateChange += OnAnchorChange;
            }

            public ForceEntitySignal(string instrument, BarsPeriodType perType, int perVal)
                : base(instrument, perType, perVal)
            {
                Anchor.OnStateChange += OnAnchorChange;
            }

            protected ForceEntitySignal(ForceEntitySignal copy)
                : base(copy)
            {
                _calcMethod = copy._calcMethod;
                _barCount = copy._barCount;
                _pulseDivision = copy._pulseDivision;
                _polarity = copy._polarity;
                Anchor.OnStateChange += OnAnchorChange;
            }

            public object Clone()
            {
                return new ForceEntitySignal(this);
            }

            protected override int AnchorBarMin()
            {
                return CalculationMethod == eForceLineCalcMethod.BarCount ? 1 : PulseDivision + 1;
            }

            #endregion

        }

        /// <summary>
        /// class that contains more info pertinent to the Resonant Angles
        /// </summary>
        private class ForceEntityAngle : ForceEntity, ICloneable
        {
            public static int Version = 1;

            #region properties

            private double _angle = double.MinValue;
            public double Angle
            {
                get { return _angle; }
            }

            public bool HasAngle { get { return !_angle.Equals(double.MinValue); } }

            private int _barDelta = 0;
            public int BarDelta
            {
                get { return _barDelta; }
                set
                {
                    _barDelta = value;
                    if (_barDelta == 0 || _tickDelta.Equals(double.MinValue)) return;
                    _angle = _tickDelta / _barDelta;
                }
            }

            private double _tickDelta = double.MinValue;
            public double TickDelta
            {
                get { return _tickDelta; }
                set
                {
                    _tickDelta = value;
                    if (_barDelta == 0 || _tickDelta.Equals(double.MinValue)) return;
                    _angle = _tickDelta / _barDelta;
                }
            }

            #endregion

            #region public functions

            public override void Reset()
            {
                //lock (Lock)
                {
                    _angle = double.MinValue;
                }
                base.Reset();
            }

            #region serialize

            public override string ToSerialString()
            {
                return string.Format("FEA_{0}{1}{2}{3}{4}{5}{6}{7}{8}",
                    Version.ToString(CultureInfo.InvariantCulture),
                    ForSerialVersion,
                    _barDelta.ToString(CultureInfo.InvariantCulture),
                    ForSerialItem,
                    _tickDelta.Equals(double.MinValue) ? "" : _tickDelta.ToString(CultureInfo.InvariantCulture),
                    ForSerialItem, _angle.Equals(double.MinValue) ? "" : _angle.ToString(CultureInfo.InvariantCulture),
                    ForSerialClass,
                    base.ToSerialString());
            }

            public override void FromSerialString(string str)
            {
                str = str.Trim();
                var classParts = str.Split(new string[] { ForSerialClass }, StringSplitOptions.None);
                if (classParts.Length == 2)
                {
                    base.FromSerialString(classParts[1]);

                    if (From != null)
                    {
                        var myParts = classParts[0].Split(new string[] { ForSerialVersion }, StringSplitOptions.None);
                        if (myParts.Length == 2)
                        {
                            var parts = myParts[1].Split(new string[] { ForSerialItem }, StringSplitOptions.None);
                            if (parts.Length == 3)
                            {
                                _barDelta = int.Parse(parts[0], CultureInfo.InvariantCulture);
                                _tickDelta = parts[1].Length > 0 ? double.Parse(parts[1], CultureInfo.InvariantCulture) : double.MinValue;
                                _angle = parts[2].Length > 0 ? double.Parse(parts[2], CultureInfo.InvariantCulture) : double.MinValue;
                            }
                        }
                    }
                }
            }

            new public static bool IsValidSerialString(string str)
            {
                if (str == null) return false;
                str = str.Trim();
                var classParts = str.Split(new string[] { ForSerialClass }, StringSplitOptions.None);
                if (classParts.Length == 2)
                {
                    if (ForceEntity.IsValidSerialString(classParts[1]))
                    {
                        var regex = new Regex(@"^FEA_[\d]+\" + ForSerialVersion + @"[\d]+" + ForSerialItem + @"([\d]+(\.[\d]+)?)?" + ForSerialItem + @"([\d]+(\.[\d]+)?)?", RegexOptions.Compiled);
                        return regex.IsMatch(classParts[0]);
                    }
                }
                return false;
            }

            #endregion

            #endregion

            #region constructors / overrides

            public ForceEntityAngle() : base() { }

            public ForceEntityAngle(string instrument, BarsPeriodType perType, int perVal) : base(instrument, perType, perVal) { }

            protected ForceEntityAngle(ForceEntityAngle copy) : base(copy)
            {
                _angle = copy._angle;
                _barDelta = copy._barDelta;
                _tickDelta = copy._tickDelta;
            }

            public object Clone()
            {
                return new ForceEntityAngle(this);
            }

            #endregion

        }

        #region  global properties

        /// <summary>
        /// for workspace persistance
        /// </summary>
        private ForceEntitySignal __entitySignal;

        [Browsable(false)]
        public string EntityPulseSerialize
        {
            get
            {
                return __entitySignal == null ? "" : __entitySignal.ToSerialString();
            }
            set
            {
                if (value.Length > 0)
                {
                    if (__entitySignal == null) __entitySignal = new ForceEntitySignal();
                    __entitySignal.FromSerialString(value);
                }
            }
        }

        private ForceEntityAngle __entityAngle;

        [Browsable(false)]
        public string EntityAngleSerialize
        {
            get
            {
                return __entityAngle == null ? "" : __entityAngle.ToSerialString();
            }
            set
            {
                if (value.Length > 0)
                {
                    if (__entityAngle == null) __entityAngle = new ForceEntityAngle();
                    __entityAngle.FromSerialString(value);
                }
            }
        }

        #endregion

        #endregion class ForceEntity / ForceEntityPulse / ForceEntityAngle

        #region subclass ForceProcessor

        private class ForceProcessor : Processor
        {
            public readonly ForceEntitySignal Entity;
            private int _futureLineCount;

            #region info signal
            /// <summary>
            /// node to track a user's interest (by ctrl-clicking)
            /// </summary>
            private Signal _info = null;

            public Signal InfoSignal { get { return _info; } }

            public void SetInfoSignal(DateTime time)
            {
                Log2(time);
                var signal = GetSignal(time);
                Log2(signal != null ? signal.BarTime.ToString() : "null");
                Log2(signal.HasData + ": " + signal.BarIndex);
                if (signal != null && !signal.HasData)
                {
                    signal = null;
                }
                _info = signal;
            }
            #endregion

            #region last calc
            /// <summary>
            /// to mimimize unnecessary calculations, track the PulseEntity
            /// <see cref="CalculateSignals"/>
            /// </summary>
            private string _lastCalc = "-";

            public void SetLastCalc()
            {
                _lastCalc = Entity.ToSerialString();
            }

            public bool IsLastCalc()
            {
                return Entity.ToSerialString().Equals(_lastCalc);
            }

            #endregion

            private void OnEntityReset(object sender, EventArgs args)
            {
                Log("OnEntityReset");
                Reset();
            }

            private void OnChange()
            {
                Calculate();
                ProcessTrigger(0);
            }

            private void OnAnchorChange(object sender, EventArgs args)
            {
                Log("OnAnchorChange");
                OnChange();
            }

            protected override void RefreshUI()
            {
                if (_parent.Dispatcher == null) return;
                if (_parent.Dispatcher.CheckAccess())
                {
                    ((TachEonForce)_parent).RefreshUI();
                    _parent.ForceRefresh();
                }
                else
                {
                    _parent.Dispatcher.InvokeAsync(() =>
                    {
                        ((TachEonForce)_parent).RefreshUI();
                        _parent.ForceRefresh();
                    });
                }
            }

            public ForceProcessor(TachEonForce parent, IndicatorSettings settings, ForceEntitySignal entity, int futureLine) : base(parent, settings)
            {
                Entity = entity;
                _futureLineCount = futureLine;
                settings.IsReversePolarity = entity.IsReversePolarity;

                Entity.OnReset += OnEntityReset;
                Entity.OnAnchorChange += OnAnchorChange;
                //Entity.OnBarCountChange += OnBarCountChange;

                if (Entity.Anchor.IsSet)
                    OnAnchorChange(this, EventArgs.Empty);
            }

            public void Run()
            {
                OnChange();
            }

            public override void Reset()
            {
                base.Reset();
                _info = null;
                _lastCalc = "-";
            }

            private int[] GetSpacingArray()
            {
                if (First != null && First.HasData && First.NextPrimary != null && First.NextPrimary.HasData)
                {
                    var pulseDivision = Entity.CalculationMethod == eForceLineCalcMethod.BarCount ? 1 : Entity.PulseDivision;
                    var array = new int[pulseDivision];
                    var signal = First;
                    for (var i = 0; i < pulseDivision; i++)
                    {
                        array[i] = signal.Next.BarIndex - signal.BarIndex;
                        signal = signal.Next;
                    }
                    return array;
                }
                return new int[] { 0 };
            }

            protected override int NextSignalBarIndex(Signal signal)
            {
                var spacing = GetSpacingArray();
                return signal.BarIndex + spacing[signal.ChildNumber];
            }

            protected override int PreviousSignalBarIndex(Signal signal)
            {
                var spacing = GetSpacingArray();
                return signal.BarIndex - spacing[signal.ChildNumber];
            }

            #region calculate signals

            protected override int Calculate()
            {
                //Parent.Log("Calculate called");
                var newSignals = 0;

                var bars = _parent.BarsArray[_settings.BarsIndex];
                if (bars.Count == 0) return newSignals;
                if (Entity == null) return newSignals;
                if (!Entity.Anchor.IsSet) return newSignals;
                if (Entity.Anchor.State != ForceAnchor.DrawingState.Normal) return newSignals;

                var barsCount = bars.Count - 1;
                var firstBarTime = _isIntraday ? bars.GetTime(0) : bars.GetSessionEndTime(0);
                var lastBarTime = _isIntraday ? bars.GetTime(barsCount) : bars.GetSessionEndTime(barsCount);

                // the anchor sits to the left of the oldest bar or the right of the newest - GET OUT
                if (Entity.Anchor.StartTime.CompareTo(firstBarTime) < 0
                    || Entity.Anchor.StartTime.CompareTo(lastBarTime) > 0
                    || Entity.Anchor.EndTime.CompareTo(firstBarTime) < 0
                    || Entity.Anchor.EndTime.CompareTo(lastBarTime) > 0)
                {
                    Reset();
                    return newSignals;
                }

                var isReverse = !Entity.Anchor.IsLeftToRight;
                var pulseDivision = Entity.CalculationMethod == eForceLineCalcMethod.BarCount ? 1 : Entity.PulseDivision;
                var futureCount = _futureLineCount;

                var startBarIndex = bars.GetBar(Entity.Anchor.StartTime);
                var endBarIndex = bars.GetBar(Entity.Anchor.EndTime);
                var diffBarIndex = Math.Abs(startBarIndex - endBarIndex);

                var spacing = new int[pulseDivision];
                spacing[0] = diffBarIndex;
                for (var i = 1; i < pulseDivision; i++)
                {
                    spacing[i] = (int)Math.Floor(diffBarIndex * (float)i / pulseDivision);
                }

                // reset info (for resetting anchor times)
                var resetStartSecs = Entity.Anchor.StartTime.Second + (Entity.Anchor.StartTime.Minute * 60) + (Entity.Anchor.StartTime.Hour * 3600);
                var resetEndSecs = Entity.Anchor.EndTime.Second + (Entity.Anchor.EndTime.Minute * 60) + (Entity.Anchor.EndTime.Hour * 3600);
                var resetIsReversed = resetStartSecs > resetEndSecs;
                var resetActive = false;
                var anchorReset = _isIntraday && Entity.ResetsToAnchorTime;

                Signal signal = null, signalToStartCheck = null;
                var acquiredLock = false;

                try
                {
                    Monitor.Enter(this, ref acquiredLock);

                    int barIndex, polarityIndex;

                    // just do the bare minimum if the anchor has not changed
                    if (IsLastCalc() && Last != null)
                    {
                        signal = Last;
                        while (signal != null)
                        {
                            if (signal.IsEstimated
                                || (!signal.IsPrimary && signal.Parent.IsEstimated))
                            {
                                var tmp = signal.Previous;
                                RemoveSignal(signal);
                                signal = tmp;
                            }
                            else
                            {
                                break;
                            }
                        }

                        signal = signalToStartCheck = signal.IsPrimary ? signal : signal.Parent;

                        barIndex = signal.BarIndex;
                        polarityIndex = signal.IsEven ? 1 : 0;

                        if (anchorReset)
                        {
                            var secsStart = signal.BarTime.Second + (signal.BarTime.Minute * 60) +
                                            (signal.BarTime.Hour * 3600);
                            var isReset = resetIsReversed
                                ? (secsStart >= resetStartSecs || secsStart < resetEndSecs)
                                : (secsStart >= resetStartSecs && secsStart < resetEndSecs);

                            if (!isReset)
                            {
                                var secsEnd = signal.EndTime.Second + (signal.EndTime.Minute * 60) +
                                              (signal.EndTime.Hour * 3600);
                                resetActive = resetIsReversed
                                    ? (secsEnd > resetStartSecs || secsEnd < resetEndSecs)
                                    : (secsEnd > resetStartSecs && secsEnd < resetEndSecs);
                            }
                        }
                    }
                    // full compute
                    else
                    {
                        Reset();

                        if (diffBarIndex < pulseDivision)
                        {
                            // the anchor "zone" is too small to calculate signals - GET OUT
                            Entity.Reset();
                            return newSignals;
                        }

                        //set barcount bc this may be loaded from a higher timeframe
                        if (Entity.CalculationMethod == eForceLineCalcMethod.BarCount
                            && Entity.BarCount != diffBarIndex)
                        {
                            Entity.BarCount = diffBarIndex;
                        }

                        if (Entity.ResetsToAnchorTime)
                        {
                            polarityIndex = !isReverse ? 0 : 1;
                            barIndex = GetFirstOccurenceOfBarTime(bars, Entity.Anchor.StartTime);
                        }
                        else
                        {
                            var countOfLeftSignals = startBarIndex / diffBarIndex;
                            polarityIndex = countOfLeftSignals % 2 == 0 ? 0 : 1;
                            barIndex = startBarIndex % diffBarIndex;
                        }

                        var startTime = _isIntraday ? bars.GetTime(barIndex) : bars.GetSessionEndTime(barIndex);
                        var endTime = _isIntraday ? bars.GetTime(barIndex + diffBarIndex) : bars.GetSessionEndTime(barIndex + diffBarIndex);

                        signal = signalToStartCheck = new Signal(startTime, endTime, polarityIndex++ % 2 == 0);
                        signal.UpdateData(bars);
                        AddSignal(signal);
                    }

                    var emergencyOut1 = 0;
                    var tmpF = 0;
                    while (futureCount > 0)
                    {
                        if (emergencyOut1++ > 10000000)
                        {
                            Log2("Break 1;----------------------------");
                            break;
                        }

                        if (pulseDivision > 1)
                        {
                            polarityIndex += signal.ChildCount;

                            for (var i = signal.ChildCount + 1; i < pulseDivision; i++)
                            {
                                var fibIndex = barIndex + spacing[i];
                                var isFuture = fibIndex > barsCount;
                                var fibTime = isFuture
                                    ? EstimateFutureTime(bars, signal.BarTime, spacing[i])
                                    : (_isIntraday ? bars.GetTime(fibIndex) : bars.GetSessionEndTime(fibIndex));

                                if (anchorReset && resetActive)
                                {
                                    var secsStart = fibTime.Second + (fibTime.Minute * 60) + (fibTime.Hour * 3600);
                                    var isReset = resetIsReversed
                                        ? (secsStart >= resetStartSecs || secsStart < resetEndSecs)
                                        : (secsStart >= resetStartSecs && secsStart < resetEndSecs);
                                    if (isReset)
                                    {
                                        break;
                                    }
                                }

                                var secSignal = new Signal(fibTime, polarityIndex++ % 2 == 0, signal, isFuture,
                                    isFuture);
                                if (isFuture)
                                {
                                    futureCount--;
                                }
                                else
                                {
                                    secSignal.UpdateData(bars);
                                    newSignals++;
                                }

                                AddSignal(secSignal);

                                if (futureCount == 0)
                                    break;
                            }
                        }

                        barIndex += spacing[0];

                        if (futureCount > 0)
                        {
                            var startTime = signal.EndTime;
                            var isFuture = barIndex > barsCount;

                            var nextIndex = barIndex + spacing[0];
                            var isEstimated = nextIndex > barsCount;
                            var nextTime = isEstimated
                                ? EstimateFutureTime(bars, startTime, spacing[0])
                                : (_isIntraday ? bars.GetTime(nextIndex) : bars.GetSessionEndTime(nextIndex));

                            if (anchorReset)
                            {
                                var secsStart = startTime.Second + (startTime.Minute * 60) + (startTime.Hour * 3600);
                                var isReset = resetIsReversed
                                    ? (secsStart >= resetStartSecs || secsStart < resetEndSecs)
                                    : (secsStart >= resetStartSecs && secsStart < resetEndSecs);

                                if (!isReset)
                                {
                                    var secsEnd = nextTime.Second + (nextTime.Minute * 60) + (nextTime.Hour * 3600);
                                    resetActive = resetIsReversed
                                        ? (secsEnd > resetStartSecs || secsEnd < resetEndSecs)
                                        : (secsEnd > resetStartSecs && secsEnd < resetEndSecs);
                                }
                                else
                                {
                                    var resetTime = MakeNewTimeWithDate(startTime, Entity.Anchor.StartTime);
                                    if (resetTime.CompareTo(lastBarTime) > 0)
                                    {
                                        startTime = resetTime;
                                        barIndex = barsCount + 1;
                                        nextTime = EstimateFutureTime(bars, startTime, spacing[0]);
                                        polarityIndex = !isReverse ? 0 : 1;
                                        resetActive = false;
                                    }
                                    else
                                    {
                                        var resetIndex = bars.GetBar(resetTime);

                                        var checkTime = _isIntraday ? bars.GetTime(resetIndex) : bars.GetSessionEndTime(resetIndex);
                                        //if (checkTime.Equals(resetTime))
                                        {
                                            startTime = resetTime;
                                            barIndex = resetIndex;
                                            nextIndex = barIndex + spacing[0];
                                            nextTime = nextIndex > barsCount
                                                ? EstimateFutureTime(bars, startTime, spacing[0])
                                                : (_isIntraday
                                                    ? bars.GetTime(nextIndex)
                                                    : bars.GetSessionEndTime(nextIndex));
                                            polarityIndex = !isReverse ? 0 : 1;
                                            resetActive = false;
                                        }
                                        //else
                                        //{
                                        //    Reset();
                                        //    Log2("resetTime: " + resetTime);
                                        //    Log2("resetIndex: " + resetIndex);
                                        //    Log2("checkTime: " + checkTime);
                                        //    //throw new ForceException(string.Format("CalculateSignals: {0}", "Bar Reset invalid"));
                                        //}
                                    }
                                }
                            }

                            signal = new Signal(startTime, nextTime, polarityIndex++ % 2 == 0, isFuture, isEstimated);
                            if (isFuture)
                            {
                                futureCount--;
                                tmpF++;
                            }
                            else
                            {
                                signal.UpdateData(bars);
                                newSignals++;
                            }

                            AddSignal(signal);
                        }
                    }

                    Log2(Last);
                    Log2("count: " + tmpF);

                    SetLastCalc();
                }
                catch (ForceException err)
                {
                    throw;
                }
                catch (Exception err)
                {
                    throw new ForceException(string.Format("CalculateSignals: {0}", err.ToString()));
                }
                finally
                {
                    if (acquiredLock)
                        Monitor.Exit(this);
                }

                #region verify the LinkedList is in correct order, if not, redo??

                var correctlyOrdered = true;
                //this is just checking to verify the signals are LINKED in the right order
                //if ForceAnalysis isn't doing this right, fix that
                var check = signalToStartCheck;
                Signal prev = null;
                var sb = new StringBuilder();
                while (check != null)
                {
                    if (prev != null)
                    {
                        if (prev.BarTime.CompareTo(check.BarTime) >= 0)
                        {
                            correctlyOrdered = false;
                            //Log("Prev: " + prev.BarTime + "  check: " + check.BarTime);
                            break;
                        }
                    }
                    prev = check;
                    check = check.Next;
                }

                if (!correctlyOrdered)
                {
                    //Log("INCORRENT signal order");// throw new Exception("INCORRECT signal order.");
                }

                #endregion

                return newSignals;
            }

            private DateTime EstimateFutureTime(Bars bars, DateTime time, int barCount)
            {
                var lastBarTime = _isIntraday ? bars.GetTime(bars.Count - 1) : bars.GetSessionEndTime(bars.Count - 1);
                if (time.CompareTo(lastBarTime) < 0)
                {
                    var index = bars.GetBar(time);
                    time = lastBarTime;
                    barCount -= (bars.Count - 1 - index);
                }
                switch (bars.BarsPeriod.BarsPeriodType)
                {
                    case BarsPeriodType.Second:
                        return time.AddSeconds(bars.BarsPeriod.Value * barCount);
                    case BarsPeriodType.Minute:
                        return time.AddMinutes(bars.BarsPeriod.Value * barCount);
                    case BarsPeriodType.Day:
                        var weeks = barCount / 5;
                        time = time.AddDays(bars.BarsPeriod.Value * weeks * 7);
                        var days = barCount % 5;
                        while (days > 0)
                        {
                            time = time.AddDays(time.DayOfWeek == DayOfWeek.Friday ? 3 : 1);
                            days--;
                        }
                        return time;
                    case BarsPeriodType.Week:
                        return time.AddDays(bars.BarsPeriod.Value * barCount * 7);
                    case BarsPeriodType.Month:
                        return time.AddMonths(bars.BarsPeriod.Value * barCount);
                    case BarsPeriodType.Year:
                        return time.AddYears(bars.BarsPeriod.Value * barCount);
                    default:
                        return time;
                }
            }

            private DateTime MakeNewTimeWithDate(DateTime date, DateTime time)
            {
                return new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second);
            }

            private int GetFirstOccurenceOfBarTime(Bars bars, DateTime time)
            {
                var leftMostTime = _isIntraday ? bars.GetTime(0) : bars.GetSessionEndTime(0);
                var barTime = new DateTime(leftMostTime.Year, leftMostTime.Month, leftMostTime.Day, time.Hour, time.Minute, time.Second);

                if (leftMostTime.Equals(barTime))
                    return 0;

                var barIndex = bars.GetBar(barTime);
                if (barIndex == bars.Count - 1)
                    return -1;

                while (barIndex == 0)
                {
                    barTime = barTime.AddDays(1);
                    barIndex = bars.GetBar(barTime);
                    if (barIndex == bars.Count - 1)
                        return -1;
                }

                return barIndex;
            }

            #endregion
        }

        private readonly IndicatorSettings __settings = new IndicatorSettings(0, 0, 1, 2, 3);
        private ForceProcessor __processor;

        #endregion

        #region ForceWindows

        /// <summary>
        /// modal window for loading files for signals/angles
        /// </summary>
        private class ForceLoadDialog : Window
        {
            public class FileItem
            {
                public string Instrument { get; set; }
                public BarsPeriodType PeriodType { get; set; }
                public int PeriodValue { get; set; }
                public string Timeframe { get; set; }
                public string EntitySerial { get; set; }
                public string FilePath { get; set; }
                public string FileName { get; set; }
                public string Notes { get; set; }
                public bool IsValid { get; set; }

                public FileItem(string instrument, BarsPeriodType perType, int perVal)
                {
                    Instrument = instrument;
                    PeriodType = perType;
                    PeriodValue = perVal;
                    Timeframe = string.Format("{0} ({1})", perType, PeriodValue);
                    IsValid = false;
                }

                public override string ToString()
                {
                    return EntitySerial;
                }
            }

            private TachEonForce _parent;
            private string _dir;

            private DataGrid _datagrid;
            private Button _load;
            private Button _delete;
            private Button _clipboard;

            private FileItem _fileItem;

            public string EntitySerial { get; private set; }

            public readonly ObservableCollection<FileItem> List = new ObservableCollection<FileItem>();

            private int SecondsInPeriodType(BarsPeriodType type)
            {
                switch (type)
                {
                    case BarsPeriodType.Second:
                        return 1;
                    case BarsPeriodType.Minute:
                        return 60;
                    case BarsPeriodType.Day:
                        return 24 * 60 * 60;
                    case BarsPeriodType.Week:
                        return 5 * 24 * 60 * 60;
                    default:
                        return 0;
                }
            }

            private void LoadFiles()
            {
                List.Clear();
                try
                {
                    var filepath = Path.Combine(__defaultFilePath, _dir);
                    foreach (var f in Directory.GetFiles(filepath))
                    {
                        var name = Path.GetFileName(f);

                        var lines = File.ReadAllLines(f);
                        var sb = new StringBuilder();
                        for (var i = 2; i < lines.Length; i++)
                        {
                            sb.AppendLine(lines[i]);
                        }

                        var instrParts = lines[0].Split(",".ToCharArray());
                        if (instrParts.Length == 3)
                        {
                            BarsPeriodType barPerType;
                            if (Enum.TryParse<BarsPeriodType>(instrParts[1], out barPerType))
                            {
                                int perVal;
                                if (int.TryParse(instrParts[2], out perVal))
                                {
                                    var item = new FileItem(instrParts[0], barPerType, perVal)
                                    {
                                        EntitySerial = lines[1],
                                        Notes = sb.ToString().Trim(),
                                        FilePath = f,
                                        FileName = name.Substring(0, name.Length - __defaultFileExtension.Length)
                                    };

                                    var entity = new ForceEntity();
                                    entity.FromSerialString(item.EntitySerial);
                                    item.IsValid = entity.IsCompatible(_parent);

                                    if (item.IsValid)
                                        List.Add(item);
                                }

                            }
                        }
                    }
                }
                catch (Exception) { }
            }

            public ForceLoadDialog(TachEonForce parent, ChartControl chartControl, string dir)
            {
                _parent = parent;
                _dir = dir;

                LoadFiles();

                SizeToContent = SizeToContent.WidthAndHeight;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                Background = chartControl.Properties.ChartBackground;
                Foreground = chartControl.Properties.ChartText;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                BorderBrush = chartControl.Properties.ChartText;
                BorderThickness = new Thickness(1);

                var grid = new Grid
                {
                    Name = "ForceGridLoad",
                    Background = Brushes.Transparent,
                    Margin = new Thickness(15)
                };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                #region scrollviewer

                var scroll = new ScrollViewer
                {
                    Height = 250,
                    Width = 600,
                    Background = Brushes.Transparent
                };
                scroll.SetValue(Grid.RowProperty, 0);

                #region datagrid
                _datagrid = new DataGrid
                {
                    Name = "List",
                    AutoGenerateColumns = false,
                    IsReadOnly = false,
                    CanUserAddRows = false,
                    CanUserDeleteRows = true,
                    CanUserSortColumns = true,
                    SelectionMode = DataGridSelectionMode.Single,
                    SelectionUnit = DataGridSelectionUnit.FullRow,
                    Background = Brushes.Transparent,
                    ItemsSource = List,
                    SelectedItem = null
                };
                _datagrid.SelectionChanged += _datagrid_SelectionChanged;
                //_datagrid.MouseDoubleClick += _datagrid_MouseDoubleClick;
                _datagrid.CellEditEnding += _datagrid_CellEditEnding;
                scroll.Content = _datagrid;

                var headerStyle = new Style();
                //headerStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Red));

                var cellStyle = new Style();
                //cellStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Green));

                _datagrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Filename",
                    HeaderStyle = headerStyle,
                    CellStyle = cellStyle,
                    IsReadOnly = true,
                    Width = DataGridLength.Auto,
                    SortDirection = ListSortDirection.Ascending,
                    Binding = new Binding("FileName")
                });

                _datagrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Instrument",
                    HeaderStyle = headerStyle,
                    CellStyle = cellStyle,
                    IsReadOnly = true,
                    Width = DataGridLength.Auto,
                    Binding = new Binding("Instrument")
                });

                _datagrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Time Frame",
                    HeaderStyle = headerStyle,
                    CellStyle = cellStyle,
                    IsReadOnly = true,
                    Width = DataGridLength.Auto,
                    Binding = new Binding("Timeframe")
                });

                _datagrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Notes",
                    HeaderStyle = headerStyle,
                    CellStyle = cellStyle,
                    IsReadOnly = false,
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                    Binding = new Binding("Notes")
                });
                #endregion

                grid.Children.Add(scroll);

                #endregion

                #region button controls

                var btnGrid = new Grid();
                btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                btnGrid.SetValue(Grid.RowProperty, 1);
                grid.Children.Add(btnGrid);

                //clipboard
                {
                    var clipped = Clipboard.GetText();
                    var canPaste = false;
                    if (_dir == __dirForSignals)
                    {
                        canPaste = ForceEntitySignal.IsValidSerialString(clipped);
                    }
                    else if (_dir == __dirForAngles)
                    {
                        canPaste = ForceEntityAngle.IsValidSerialString(clipped);
                    }
                    if (canPaste)
                    {
                        var entity = new ForceEntity();
                        entity.FromSerialString(clipped);
                        if (!entity.IsCompatible(_parent))
                        {
                            canPaste = false;
                        }
                    }

                    _clipboard = new Button
                    {
                        Name = "Paste",
                        Content = "P",
                        Margin = new Thickness(0, 8, 0, 0),
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        MinHeight = 10,
                        Height = 25,
                        MinWidth = 10,
                        Width = 30,
                        IsEnabled = canPaste
                    };
                    _clipboard.Click += _paste_Click;
                    _clipboard.SetValue(Grid.ColumnProperty, 0);
                    btnGrid.Children.Add(_clipboard);
                }

                var controls = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 8, 0, 0)
                };
                controls.SetValue(Grid.ColumnProperty, 1);
                btnGrid.Children.Add(controls);

                _load = new Button
                {
                    Name = "Load",
                    Content = "Load",
                    IsDefault = true,
                    IsEnabled = false,
                    Margin = new Thickness(2, 0, 2, 0)
                };
                _load.Click += _load_Click;
                controls.Children.Add(_load);

                _delete = new Button
                {
                    Name = "Delete",
                    Content = "Delete",
                    IsEnabled = false,
                    Margin = new Thickness(2, 0, 2, 0)
                };
                _delete.Click += _delete_Click;
                controls.Children.Add(_delete);

                var cancel = new Button
                {
                    Name = "Cancel",
                    Content = "Cancel",
                    IsCancel = true,
                    Margin = new Thickness(2, 0, 2, 0)
                };
                controls.Children.Add(cancel);

                #endregion

                Content = grid;

                Closed += ForceLoadDialog_Closed;
            }

            private void ForceLoadDialog_Closed(object sender, EventArgs e)
            {
                if (_datagrid != null)
                {
                    _datagrid.SelectionChanged -= _datagrid_SelectionChanged;
                    //_datagrid.MouseDoubleClick -= _datagrid_MouseDoubleClick;
                    _datagrid.CellEditEnding -= _datagrid_CellEditEnding;
                }
                if (_load != null)
                {
                    _load.Click -= _load_Click;
                }

                if (_delete != null)
                {
                    _delete.Click -= _delete_Click;
                }
                if (_clipboard != null)
                {
                    _clipboard.Click -= _paste_Click;
                }
            }

            private void _datagrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
            {
                if (e.EditAction == DataGridEditAction.Cancel) return;
                var col = e.Column as DataGridBoundColumn;
                if (col == null) return;

                var binding = col.Binding as Binding;
                if (binding == null) return;

                var rowIndex = e.Row.GetIndex();
                var el = e.EditingElement as TextBox;

                var bindingPath = binding.Path.Path;
                if (bindingPath == "FileName")
                {
                    _parent.Log(el.Text + " as filename");
                }
                else if (bindingPath == "Notes")
                {
                    var item = List[rowIndex];
                    try
                    {
                        var lines = File.ReadAllLines(item.FilePath);
                        var file = new StreamWriter(item.FilePath);
                        using (file)
                        {
                            file.WriteLine(lines[0]);
                            file.WriteLine(lines[1]);
                            file.WriteLine(el.Text);
                        }
                    }
                    catch (Exception)
                    { }
                }
            }

            private void _datagrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                if (e.AddedItems.Count == 1)
                {
                    _fileItem = (FileItem)e.AddedItems[0];
                    if (_load != null)
                        _load.IsEnabled = true;
                    if (_delete != null)
                        _delete.IsEnabled = true;
                    return;
                }
                EntitySerial = "";
                if (_load != null)
                    _load.IsEnabled = false;
                if (_delete != null)
                    _delete.IsEnabled = false;
            }

            private void _datagrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
            {
                if (_fileItem == null) return;
                EntitySerial = _fileItem.EntitySerial;
                DialogResult = true;
            }

            private void _load_Click(object sender, RoutedEventArgs e)
            {
                if (_fileItem == null) return;
                EntitySerial = _fileItem.EntitySerial;
                DialogResult = true;
            }

            private void _delete_Click(object sender, RoutedEventArgs e)
            {
                if (_fileItem == null) return;
                var ask = MessageBox.Show(string.Format(@"Are you sure you want to delete {0}?", _fileItem.FileName), "TachEonForce Delete", MessageBoxButton.YesNoCancel);
                if (ask != MessageBoxResult.Yes) return;
                File.Delete(_fileItem.FilePath);
                List.Remove(_fileItem);
                _fileItem = null;
            }

            private void _paste_Click(object sender, RoutedEventArgs e)
            {
                var clipped = Clipboard.GetText();
                var canPaste = false;
                if (_dir == __dirForSignals)
                {
                    canPaste = ForceEntitySignal.IsValidSerialString(clipped);
                }
                else if (_dir == __dirForAngles)
                {
                    canPaste = ForceEntityAngle.IsValidSerialString(clipped);
                }
                if (canPaste)
                {
                    var entity = new ForceEntity();
                    entity.FromSerialString(clipped);
                    if (entity.IsCompatible(_parent))
                    {
                        EntitySerial = clipped;
                        DialogResult = true;
                    }
                }
            }
        }

        /// <summary>
        /// modal window for saving files for lines/angles
        /// </summary>
        private class ForceSaveDialog : Window
        {
            private TachEonForce _parent;
            private ForceEntity _entity;
            private string _dir;

            private TextBox _fileName;
            private TextBox _notes;
            private Button _save;
            private Button _clipboard;

            public ForceSaveDialog(TachEonForce parent, ChartControl chartControl, ForceEntity entity, string dir)
            {
                _parent = parent;
                _entity = entity;
                _dir = dir;

                SizeToContent = SizeToContent.WidthAndHeight;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                Background = chartControl.Properties.ChartBackground;
                Foreground = chartControl.Properties.ChartText;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                BorderBrush = chartControl.Properties.ChartText;
                BorderThickness = new Thickness(1);

                var grid = new Grid
                {
                    Name = "ForceGridSave",
                    Background = Brushes.Transparent,
                    Margin = new Thickness(15)
                };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                #region filename
                var label = new Label
                {
                    Content = "File Name:",
                    HorizontalContentAlignment = HorizontalAlignment.Right,
                    Foreground = chartControl.Properties.ChartText
                };
                label.SetValue(Grid.RowProperty, 0);
                label.SetValue(Grid.ColumnProperty, 0);

                grid.Children.Add(label);

                var file = string.Format("{0}_{1}_{2}", parent.Instrument.FullName, parent.BarsArray[0].BarsPeriod.BarsPeriodType, parent.BarsArray[0].BarsPeriod.Value);

                _fileName = new TextBox
                {
                    Name = "FileName",
                    Text = file
                };
                _fileName.SetValue(Grid.RowProperty, 0);
                _fileName.SetValue(Grid.ColumnProperty, 1);

                grid.Children.Add(_fileName);
                #endregion

                #region notes
                label = new Label
                {
                    Content = "Notes:",
                    HorizontalContentAlignment = HorizontalAlignment.Right,
                    Foreground = chartControl.Properties.ChartText
                };
                label.SetValue(Grid.RowProperty, 1);
                label.SetValue(Grid.ColumnProperty, 0);

                grid.Children.Add(label);

                _notes = new TextBox
                {
                    Name = "Notes",
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.WrapWithOverflow,
                    Height = 100,
                    Margin = new Thickness(0, 8, 0, 0)
                };
                _notes.SetValue(Grid.RowProperty, 1);
                _notes.SetValue(Grid.ColumnProperty, 1);

                grid.Children.Add(_notes);
                #endregion

                #region button controls
                _clipboard = new Button
                {
                    Name = "Copy",
                    Content = "C",
                    Margin = new Thickness(0, 8, 0, 0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    MinHeight = 10,
                    Height = 25,
                    MinWidth = 10,
                    Width = 30
                };
                _clipboard.Click += _copy_click;
                _clipboard.SetValue(Grid.RowProperty, 2);
                _clipboard.SetValue(Grid.ColumnProperty, 0);
                grid.Children.Add(_clipboard);

                var controls = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 8, 0, 0)
                };
                controls.SetValue(Grid.RowProperty, 2);
                controls.SetValue(Grid.ColumnProperty, 1);

                _save = new Button
                {
                    Name = "Save",
                    Content = "Save",
                    IsDefault = true,
                    Margin = new Thickness(2, 0, 2, 0)
                };
                _save.Click += _save_click;
                controls.Children.Add(_save);

                var cancel = new Button
                {
                    Name = "Cancel",
                    Content = "Cancel",
                    IsCancel = true,
                    Margin = new Thickness(2, 0, 2, 0)
                };
                controls.Children.Add(cancel);

                grid.Children.Add(controls);
                #endregion

                Content = grid;

                ContentRendered += ForceSaveDialog_ContentRendered;
                Closed += ForceSaveDialog_Closed;
            }

            private void ForceSaveDialog_Closed(object sender, EventArgs e)
            {
                if (_clipboard != null)
                {
                    _clipboard.Click -= _copy_click;
                }
                if (_save != null)
                {
                    _save.Click -= _save_click;
                }
            }

            private void ForceSaveDialog_ContentRendered(object sender, EventArgs e)
            {
                if (_fileName != null)
                {
                    _fileName.Focus();
                    _fileName.SelectAll();
                }
            }

            private void _copy_click(object sender, RoutedEventArgs e)
            {
                Clipboard.SetText(_entity.ToSerialString());
                DialogResult = true;
            }

            private void _save_click(object sender, RoutedEventArgs e)
            {
                try
                {
                    var filename = Path.Combine(__defaultFilePath, _dir, string.Format("{0}{1}", _fileName.Text, __defaultFileExtension));
                    var fileInfo = new FileInfo(filename);
                    if (!fileInfo.Exists)
                        Directory.CreateDirectory(fileInfo.Directory.FullName);

                    if (File.Exists(filename))
                    {
                        var ask = MessageBox.Show(string.Format("{0} already exists. Replace it?", _fileName.Text), "TachEonForce Save", MessageBoxButton.YesNo);
                        if (ask == MessageBoxResult.No)
                            return;
                    }
                    var file = new StreamWriter(filename);
                    using (file)
                    {
                        file.WriteLine(string.Format("{0},{1},{2}", _parent.Instrument.FullName, _parent.BarsArray[0].BarsPeriod.BarsPeriodType, _parent.BarsArray[0].BarsPeriod.Value));
                        file.WriteLine(_entity.ToSerialString());
                        file.WriteLine(_notes.Text);
                    }
                    DialogResult = true;
                }
                catch (Exception err)
                {
                    throw new ForceException("Error: " + err.Message);
                }
            }
        }

        #endregion ForceWindows

        #region UI create/dispose/etc

        private readonly SortedSet<IDtwUiChartElement> _chartItems = new SortedSet<IDtwUiChartElement>();
        private readonly List<IDtwUiElement> _uiItems = new List<IDtwUiElement>();

        private void StylesUI(ChartControl chartControl, DtwUiChartPanel uiChartPanel, bool compact)
        {
            //BUTTON
            var style = new Style();
            style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            style.Setters.Add(new Setter(TextBlock.ForegroundProperty, chartControl.Properties.ChartText));
            style.Setters.Add(new Setter(UIElement.FocusableProperty, false));
            style.Setters.Add(new Setter(FrameworkElement.MarginProperty, compact ? new Thickness(6, 1, 6, 1) : new Thickness(6, 2, 6, 2)));//1 or 2
            style.Setters.Add(new Setter(Control.PaddingProperty, compact ? new Thickness(6, 1, 6, 1) : new Thickness(6, 2, 6, 2)));//1 or 2
            style.Setters.Add(new Setter(TextBlock.FontFamilyProperty, chartControl.Properties.LabelFont.Family));
            uiChartPanel.Grid.Resources.Add(typeof(Button), style);

            //GROUPBOX
            style = new Style();
            style.Setters.Add(new Setter(FrameworkElement.MarginProperty, compact ? new Thickness(1, 1, 1, 0) : new Thickness(1)));//1 or 1,0,1,0
            style.Setters.Add(new Setter(Control.PaddingProperty, compact ? new Thickness(1) : new Thickness(3)));//3 or 1,0,1,0
            style.Setters.Add(new Setter(TextBlock.ForegroundProperty, chartControl.Properties.ChartText));
            style.Setters.Add(new Setter(TextBlock.FontSizeProperty, chartControl.Properties.LabelFont.Size));
            style.Setters.Add(new Setter(TextBlock.FontFamilyProperty, chartControl.Properties.LabelFont.Family));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, chartControl.Properties.ChartText));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            uiChartPanel.Grid.Resources.Add(typeof(GroupBox), style);

            //RADIO and CHECK
            style = new Style();
            style.Setters.Add(new Setter(UIElement.FocusableProperty, false));
            style.Setters.Add(new Setter(FrameworkElement.MarginProperty, compact ? new Thickness(1) : new Thickness(1, 3, 1, 3)));//1 or 1,3,1,3
            style.Setters.Add(new Setter(Control.PaddingProperty, compact ? new Thickness(1) : new Thickness(2)));//1 or 2
            style.Setters.Add(new Setter(TextBlock.ForegroundProperty, chartControl.Properties.ChartText));
            style.Setters.Add(new Setter(TextBlock.FontSizeProperty, chartControl.Properties.LabelFont.Size));
            style.Setters.Add(new Setter(TextBlock.FontFamilyProperty, chartControl.Properties.LabelFont.Family));
            uiChartPanel.Grid.Resources.Add(typeof(RadioButton), style);
            uiChartPanel.Grid.Resources.Add(typeof(CheckBox), style);

            //LABEL
            style = new Style();
            style.Setters.Add(new Setter(TextBlock.ForegroundProperty, chartControl.Properties.ChartText));
            style.Setters.Add(new Setter(TextBlock.FontSizeProperty, chartControl.Properties.LabelFont.Size));
            style.Setters.Add(new Setter(TextBlock.FontFamilyProperty, chartControl.Properties.LabelFont.Family));
            uiChartPanel.Grid.Resources.Add(typeof(Label), style);

            //BORDER
            style = new Style();
            style.Setters.Add(new Setter(Border.BorderBrushProperty, chartControl.Properties.ChartText));
            //style.Setters.Add(new Setter(TextBlock.FontSizeProperty, chartControl.Properties.LabelFont.Size));
            //style.Setters.Add(new Setter(TextBlock.FontFamilyProperty, chartControl.Properties.LabelFont.Family));
            uiChartPanel.Grid.Resources.Add(typeof(Border), style);

            //TEXTBOX
            style = new Style();
            style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            style.Setters.Add(new Setter(TextBlock.ForegroundProperty, chartControl.Properties.ChartText));
            style.Setters.Add(new Setter(TextBlock.FontFamilyProperty, chartControl.Properties.LabelFont.Family));
            //style.Setters.Add(new Setter(TextBox.WidthProperty, 40));
            //style.Setters.Add(new Setter(TextBox.FontSizeProperty, 16));
            //panel.Grid.Resources.Add(typeof(TextBox), style);
            /*
             * var textBox = new TextBox
                        {
                            FontSize = 20,
                            TextAlignment = System.Windows.TextAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Width = 50,
                            Margin = new Thickness(10, 0, 0, 0)
                        };*/
            //gridTab.Resources.Add(typeof(DatePicker), style);
        }

        /// <summary>
        /// this function is LONG b/c its building the UI
        /// instead of passing variables to a bunch of smaller functions, its broken into regions
        /// </summary>
        /// <param name="chartControl"></param>
        private void CreateUI(ChartControl chartControl)
        {
            try
            {
                chartControl.MouseLeftButtonDown += Chart_MouseDown;
                //chartControl.MouseLeftButtonUp += chart_MouseUp;
                chartControl.MouseMove += Chart_MouseMove;
                //chartControl.PreviewKeyDown += Chart_PreviewKeyDown;
                if (__infoEnabled)
                {
                    chartControl.MouseDown += Chart_MouseDownInfo;
                }

                var chartWindow = Window.GetWindow(chartControl.Parent) as Chart;
                if (chartWindow == null) return;

                //chartWindow.PreviewKeyDown += Chart_PreviewKeyDown;

                chartWindow.MainTabControl.SelectionChanged += TabSelectionChangedHandler;

                var sortOrder = 0;

                var uiChartPanel = new DtwUiChartPanel(chartWindow, sortOrder++,
                    o =>
                    {

                    }, _panelSize, _panelVisible,
                    (o, e) =>
                    {
                        _panelSize -= (int)e.HorizontalChange;
                    });
                _chartItems.Add(uiChartPanel);

                #region chart left-hand panel

                StylesUI(chartControl, uiChartPanel, true);

                var tabControl = new TabControl
                {
                    TabStripPlacement = Dock.Bottom,
                    Background = chartControl.Properties.ChartBackground,
                    Padding = new Thickness(5, 0, 5, 4)
                };

                {
                    Grid.SetColumn(tabControl, 0);

                    var colDef = new ColumnDefinition
                    {
                        Width = new GridLength(1, GridUnitType.Star)
                    };

                    uiChartPanel.Grid.ColumnDefinitions.Add(colDef);
                    uiChartPanel.Grid.Children.Add(tabControl);

                    #region main tab
                    {
                        var panelTab = new TabItem
                        {
                            Header = "Main"
                        };
                        tabControl.Items.Add(panelTab);

                        {
                            var gridTab = new Grid();

                            var scroll = new ScrollViewer
                            {
                                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                Content = gridTab
                            };

                            panelTab.Content = scroll;

                            #region GROUPBOX METHOD
                            {
                                var groupBox = new GroupBox { Header = "Method" };
                                gridTab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                groupBox.SetValue(Grid.RowProperty, gridTab.RowDefinitions.Count - 1);
                                gridTab.Children.Add(groupBox);

                                var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
                                groupBox.Content = grid;

                                var stackPanel = new StackPanel
                                {
                                    Orientation = Orientation.Vertical,
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Margin = new Thickness(0, 0, 5, 0)
                                };
                                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                                stackPanel.SetValue(Grid.ColumnProperty, grid.ColumnDefinitions.Count - 1);
                                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                stackPanel.SetValue(Grid.RowProperty, grid.RowDefinitions.Count - 1);
                                grid.Children.Add(stackPanel);

                                foreach (var i in "Fibs~Bar Count".Split("~".ToCharArray()))
                                {
                                    var rb = new RadioButton
                                    {
                                        Name = "CalcType" + i.Replace(" ", ""),
                                        GroupName = "CalcType",
                                        Content = i
                                    };
                                    stackPanel.Children.Add(rb);

                                    var radio = new DtwUiRadioButton(rb,
                                        item =>
                                        {
                                            var val = item.Name.Replace(item.GroupName, "");
                                            item.IsChecked = val.Equals(__entitySignal.CalculationMethod.ToString());
                                        },
                                        (o, e) =>
                                        {
                                            var item = o as RadioButton;
                                            if (item == null) return;

                                            var val = item.Name.Replace(item.GroupName, "");
                                            var method = (eForceLineCalcMethod)Enum.Parse(typeof(eForceLineCalcMethod), val);
                                            if (__entitySignal.CalculationMethod != method)
                                            {
                                                __entitySignal.CalculationMethod = method;
                                                if (__entitySignal.Anchor.IsSet)
                                                {
                                                    var bars = BarsArray[__settings.BarsIndex];
                                                    var reset = false;
                                                    if (bars == null)
                                                    {
                                                        reset = true;
                                                    }
                                                    else
                                                    {
                                                        switch (__entitySignal.CalculationMethod)
                                                        {
                                                            case eForceLineCalcMethod.Fibs:
                                                                if (__entitySignal.BarCount < __entitySignal.PulseDivision)
                                                                    reset = true;
                                                                break;
                                                            case eForceLineCalcMethod.BarCount:
                                                                var startBarIndex = bars.GetBar(__entitySignal.Anchor.StartTime);
                                                                if (startBarIndex < bars.Count - 1 - __entitySignal.BarCount)
                                                                {
                                                                    var endTime = IsIntraday(bars) ? bars.GetTime(startBarIndex + __entitySignal.BarCount) : bars.GetSessionEndTime(startBarIndex + __entitySignal.BarCount);
                                                                    __entitySignal.Anchor.SetEnd(endTime, __entitySignal.Anchor.StartPrice);
                                                                }
                                                                else
                                                                {
                                                                    reset = true;
                                                                }
                                                                break;
                                                        }
                                                    }
                                                    if (reset)
                                                    {
                                                        __entitySignal.Reset();
                                                    }
                                                    else
                                                    {
                                                        __processor.Run();
                                                    }
                                                }

                                                RefreshUI();
                                            }
                                        });
                                    _uiItems.Add(radio);
                                }

                                var textBox = new TextBox
                                {
                                    TextAlignment = System.Windows.TextAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Width = 40,
                                    Margin = new Thickness(10, 0, 0, 0),
                                    Text = __entitySignal.BarCount.ToString()
                                };
                                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                                textBox.SetValue(Grid.ColumnProperty, grid.ColumnDefinitions.Count - 1);
                                stackPanel.SetValue(Grid.RowProperty, grid.RowDefinitions.Count - 1);
                                grid.Children.Add(textBox);

                                var holdBoxSize = __entitySignal.BarCount;
                                var txtNum = new DtwUiTextBoxInteger(textBox,
                                item =>
                                {
                                    item.Visibility = __entitySignal.CalculationMethod == eForceLineCalcMethod.BarCount ? Visibility.Visible : Visibility.Hidden;
                                    if (holdBoxSize != __entitySignal.BarCount)
                                    {
                                        holdBoxSize = __entitySignal.BarCount;
                                        item.Text = __entitySignal.BarCount.ToString();
                                    }
                                },
                                val =>
                                {
                                    if (val == 0)
                                    {
                                        textBox.Text = __entitySignal.BarCount.ToString();
                                        return;
                                    }
                                    if (val != __entitySignal.BarCount)
                                    {
                                        var bars = BarsArray[__settings.BarsIndex];
                                        if (bars == null) return;
                                        var startBarIndex = bars.GetBar(__entitySignal.Anchor.StartTime);
                                        if (startBarIndex < bars.Count - 1 - val)
                                        {
                                            var endTime = IsIntraday(bars) ? bars.GetTime(startBarIndex + val) : bars.GetSessionEndTime(startBarIndex + val);
                                            __entitySignal.Anchor.SetEnd(endTime, __entitySignal.Anchor.EndPrice);
                                            __entitySignal.BarCount = val;
                                            __processor.Run();
                                            ForceRefresh();
                                            RefreshUI();
                                        }
                                    }
                                },
                                val =>
                                {
                                    return val != __entitySignal.BarCount;
                                });
                                _uiItems.Add(txtNum);

                                if (__betaStuff)
                                {
                                    var checkBox = new CheckBox { Content = "Resets to Anchor Time" };
                                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                    checkBox.SetValue(Grid.RowProperty, grid.RowDefinitions.Count - 1);
                                    checkBox.SetValue(Grid.ColumnSpanProperty, 2);
                                    grid.Children.Add(checkBox);

                                    var check = new DtwUiCheckBox(checkBox,
                                    item =>
                                    {
                                        var bars = BarsArray[__settings.BarsIndex];
                                        item.IsEnabled = bars == null ? false : IsIntraday(bars);
                                        item.IsChecked = __entitySignal.ResetsToAnchorTime ? true : false;
                                    },
                                    (o, e) =>
                                    {
                                        var item = o as CheckBox;
                                        if (item == null) return;

                                        var val = item.IsChecked.HasValue ? item.IsChecked.Value : false;

                                        if (__entitySignal.ResetsToAnchorTime != val)
                                        {
                                            __entitySignal.ResetsToAnchorTime = val;
                                            __processor.Run();
                                        }
                                    });
                                    _uiItems.Add(check);
                                }
                            }
                            #endregion

                            #region setup for DRAW/RESET

                            /// <remarks>
                            /// for code maint reduction, since draw/reset is created twice
                            /// </remarks>
                            Action<string, ForceEntity, ForceEntity, StackPanel> drawReset = (prefix, entity, otherEntity, parent) =>
                            {
                                var stack = new StackPanel
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalAlignment = HorizontalAlignment.Center
                                };
                                parent.Children.Add(stack);

                                {
                                    var btn = new Button
                                    {
                                        Content = "Draw"
                                    };
                                    stack.Children.Add(btn);

                                    var background = btn.Background;
                                    var foreground = btn.Foreground;
                                    var uib = new DtwUiButton(btn,
                                    item =>
                                    {
                                        var anchor = entity.Anchor;
                                        if (anchor.State == ForceAnchor.DrawingState.Normal)
                                        {
                                            item.Background = background;
                                            item.Foreground = foreground;
                                        }
                                        else
                                        {
                                            item.Background = chartControl.Properties.ChartText;
                                            item.Foreground = chartControl.Properties.ChartBackground;
                                        }
                                    },
                                    (o, e) =>
                                    {
                                        if (otherEntity.Anchor.State != ForceAnchor.DrawingState.Normal)
                                        {
                                            otherEntity.Anchor.State = ForceAnchor.DrawingState.Normal;
                                        }

                                        var anchor = entity.Anchor;
                                        switch (anchor.State)
                                        {
                                            case ForceAnchor.DrawingState.Normal:
                                                entity.Reset();
                                                anchor.State = ForceAnchor.DrawingState.Building;
                                                break;
                                            case ForceAnchor.DrawingState.Building:
                                                entity.Reset();
                                                anchor.State = ForceAnchor.DrawingState.Normal;
                                                break;
                                            default:
                                                anchor.State = ForceAnchor.DrawingState.Normal;
                                                break;
                                        }
                                        RefreshUI();
                                        ForceRefresh();
                                    });
                                    _uiItems.Add(uib);
                                }
                                {
                                    var btn = new Button
                                    {
                                        Content = "Reset"
                                    };
                                    stack.Children.Add(btn);

                                    var uib = new DtwUiButton(btn,
                                    item =>
                                    {
                                        item.IsEnabled = entity.Anchor.IsSet;
                                    },
                                    (o, e) =>
                                    {
                                        __entitySignal.Anchor.State = ForceAnchor.DrawingState.Normal;
                                        __entityAngle.Anchor.State = ForceAnchor.DrawingState.Normal;

                                        entity.Reset();
                                        RefreshUI();
                                        ForceRefresh();
                                    });
                                    _uiItems.Add(uib);
                                }
                            };

                            #endregion

                            #region setup for LOAD/SAVE

                            /// <remarks>
                            /// for code maint reduction, since load/save is created twice
                            /// </remarks>
                            Action<string, ForceEntity, StackPanel> loadSave = (prefix, entity, parent) =>
                            {
                                var stack = new StackPanel
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalAlignment = HorizontalAlignment.Center
                                };
                                parent.Children.Add(stack);

                                {
                                    var btn = new Button
                                    {
                                        Content = "Load"
                                    };
                                    stack.Children.Add(btn);

                                    var uib = new DtwUiButton(btn,
                                    item =>
                                    {
                                        item.IsEnabled = true;
                                    },
                                    (o, e) =>
                                    {
                                        __entitySignal.Anchor.State = ForceAnchor.DrawingState.Normal;
                                        __entityAngle.Anchor.State = ForceAnchor.DrawingState.Normal;

                                        var dir = prefix == "Force" ? __dirForSignals : __dirForAngles;

                                        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                                        {
                                            var clipped = Clipboard.GetText();
                                            var canPaste = false;
                                            if (dir == __dirForSignals)
                                            {
                                                canPaste = ForceEntitySignal.IsValidSerialString(clipped);
                                            }
                                            else if (dir == __dirForAngles)
                                            {
                                                canPaste = ForceEntityAngle.IsValidSerialString(clipped);
                                            }
                                            if (canPaste)
                                            {
                                                var testEntity = new ForceEntity();
                                                testEntity.FromSerialString(clipped);
                                                if (!testEntity.IsCompatible(this))
                                                {
                                                    canPaste = false;
                                                }
                                            }
                                            if (canPaste)
                                            {
                                                entity.FromSerialString(clipped);

                                                if (dir == __dirForSignals)
                                                {
                                                    __processor.Run();
                                                }
                                                RefreshUI();
                                                ForceRefresh();
                                                return;
                                            }
                                        }

                                        var window = new ForceLoadDialog(this, chartControl, dir);
                                        if (window.ShowDialog() == true)
                                        {
                                            OnRenderTargetChanged();
                                            entity.FromSerialString(window.EntitySerial);
                                            __processor.Run();
                                            RefreshUI();
                                            ForceRefresh();
                                        }
                                    });
                                    _uiItems.Add(uib);
                                }
                                {
                                    var btn = new Button
                                    {
                                        Content = "Save"
                                    };
                                    stack.Children.Add(btn);

                                    var uib = new DtwUiButton(btn,
                                    item =>
                                    {
                                        item.IsEnabled = entity.Anchor.IsSet;
                                    },
                                    (o, e) =>
                                    {
                                        __entitySignal.Anchor.State = ForceAnchor.DrawingState.Normal;
                                        __entityAngle.Anchor.State = ForceAnchor.DrawingState.Normal;

                                        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                                        {
                                            Clipboard.SetText(entity.ToSerialString());
                                            return;
                                        }

                                        var dir = prefix == "Force" ? __dirForSignals : __dirForAngles;

                                        var window = new ForceSaveDialog(this, chartControl, entity, dir);
                                        if (window.ShowDialog() == true)
                                        {
                                            RefreshUI();
                                        }
                                    });
                                    _uiItems.Add(uib);
                                }
                            };

                            #endregion

                            #region GROUPBOX FORCE
                            {
                                var groupBox = new GroupBox { Header = "Force" };
                                gridTab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                groupBox.SetValue(Grid.RowProperty, gridTab.RowDefinitions.Count - 1);
                                gridTab.Children.Add(groupBox);

                                //anchor warning
                                var uigb = new DtwUiGroupBox(groupBox,
                                item =>
                                {
                                    if (__entitySignal.Anchor.IsSet)
                                    {
                                        var bars = BarsArray[__settings.BarsIndex];
                                        if (bars == null) return;

                                        var firstBar = IsIntraday(bars) ? bars.GetTime(0) : bars.GetSessionEndTime(0);
                                        var lastBar = IsIntraday(bars) ? bars.GetTime(bars.Count - 1) : bars.GetSessionEndTime(bars.Count - 1);
                                        if (__entitySignal.Anchor.StartTime.CompareTo(firstBar) < 0 || __entitySignal.Anchor.StartTime.CompareTo(lastBar) > 0 || __entitySignal.Anchor.EndTime.CompareTo(firstBar) < 0 || __entitySignal.Anchor.EndTime.CompareTo(lastBar) > 0)
                                        {
                                            item.Background = Brushes.Red;
                                            return;
                                        }
                                    }

                                    item.Background = Brushes.Transparent;
                                });
                                _uiItems.Add(uigb);

                                var stackPanel = new StackPanel
                                {
                                    HorizontalAlignment = HorizontalAlignment.Center
                                };
                                groupBox.Content = stackPanel;

                                drawReset("Force", __entitySignal, __entityAngle, stackPanel);

                                var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                                stackPanel.Children.Add(sp);

                                {
                                    var checkBox = new CheckBox { Content = "Reverse Polarity" };
                                    sp.Children.Add(checkBox);

                                    var check = new DtwUiCheckBox(checkBox,
                                    item =>
                                    {
                                        item.IsChecked = __entitySignal.IsReversePolarity ? true : false;
                                    },
                                    (o, e) =>
                                    {
                                        var item = o as CheckBox;
                                        if (item == null) return;

                                        if (item.IsChecked.HasValue ? item.IsChecked.Value : false)
                                            __entitySignal.SetPolarityToNegative();
                                        else
                                            __entitySignal.SetPolarityToPositive();

                                        __settings.IsReversePolarity = __entitySignal.IsReversePolarity;

                                        RefreshUI();
                                        ForceRefresh();
                                    });
                                    _uiItems.Add(check);
                                }
                                {
                                    var checkBox = new CheckBox { Content = "Render Dates" };
                                    sp.Children.Add(checkBox);

                                    var check = new DtwUiCheckBox(checkBox,
                                    item =>
                                    {
                                        item.IsChecked = _renderLineDates ? true : false;
                                    },
                                    (o, e) =>
                                    {
                                        var item = o as CheckBox;
                                        if (item == null) return;

                                        _renderLineDates = item.IsChecked.HasValue ? item.IsChecked.Value : false;

                                        RefreshUI();
                                        ForceRefresh();
                                    });
                                    _uiItems.Add(check);
                                }

                                loadSave("Force", __entitySignal, stackPanel);
                            }
                            #endregion

                            #region GROUPBOX ANGLE
                            {
                                var groupBox = new GroupBox { Header = "Angle" };
                                gridTab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                groupBox.SetValue(Grid.RowProperty, gridTab.RowDefinitions.Count - 1);
                                gridTab.Children.Add(groupBox);

                                var stackPanel = new StackPanel
                                {
                                    HorizontalAlignment = HorizontalAlignment.Center
                                };
                                groupBox.Content = stackPanel;

                                drawReset("Angle", __entityAngle, __entitySignal, stackPanel);

                                var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                                stackPanel.Children.Add(sp);

                                {
                                    var checkBox = new CheckBox { Content = "Render Angle" };
                                    sp.Children.Add(checkBox);

                                    var check = new DtwUiCheckBox(checkBox,
                                    item =>
                                    {
                                        item.IsChecked = _angleRenderFoward ? true : false;
                                    },
                                    (o, e) =>
                                    {
                                        var item = o as CheckBox;
                                        if (item == null) return;

                                        _angleRenderFoward = item.IsChecked.HasValue ? item.IsChecked.Value : false;

                                        RefreshUI();
                                        ForceRefresh();
                                    });
                                    _uiItems.Add(check);
                                }
                                {
                                    var checkBox = new CheckBox { Content = "Render Reverse" };
                                    sp.Children.Add(checkBox);

                                    var check = new DtwUiCheckBox(checkBox,
                                    item =>
                                    {
                                        item.IsChecked = _angleRenderReverse ? true : false;
                                    },
                                    (o, e) =>
                                    {
                                        var item = o as CheckBox;
                                        if (item == null) return;

                                        _angleRenderReverse = item.IsChecked.HasValue ? item.IsChecked.Value : false;

                                        RefreshUI();
                                        ForceRefresh();
                                    });
                                    _uiItems.Add(check);
                                }
                                {
                                    var checkBox = new CheckBox();
                                    sp.Children.Add(checkBox);

                                    var check = new DtwUiCheckBox(checkBox,
                                    item =>
                                    {
                                        item.IsChecked = _angleStart ? true : false;

                                        item.Content = _angleStart ? "Start On Open" : "Start On Close";
                                    },
                                    (o, e) =>
                                    {
                                        var item = o as CheckBox;
                                        if (item == null) return;

                                        _angleStart = item.IsChecked.HasValue ? item.IsChecked.Value : false;

                                        RefreshUI();
                                        ForceRefresh();
                                    });
                                    _uiItems.Add(check);
                                }

                                loadSave("Angle", __entityAngle, stackPanel);
                            }
                            #endregion

                            #region setup for eForceExtensionLines
                            {
                                /// <remarks>
                                /// for code maint reduction, since extension lines exist multiple places
                                /// </remarks>
                                Action<string, ForceExtensionLine> maker = (prefix, extLine) =>
                                {
                                    var groupBox = new GroupBox
                                    {
                                        Header = prefix + (prefix == "Angle" ? " Line" : " Price Line")
                                    };
                                    gridTab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                    groupBox.SetValue(Grid.RowProperty, gridTab.RowDefinitions.Count - 1);
                                    gridTab.Children.Add(groupBox);

                                    var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
                                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                                    groupBox.Content = grid;

                                    var index = 0;
                                    foreach (var i in "Number Of Signals~Infinite~None".Split("~".ToCharArray()))
                                    {
                                        var rb = new RadioButton
                                        {
                                            Name = prefix + i.Replace(" ", ""),
                                            GroupName = prefix,
                                            Content = i
                                        };
                                        rb.SetValue(Grid.RowProperty, index);
                                        rb.SetValue(Grid.ColumnProperty, 0);
                                        if (index == 0) rb.SetValue(Grid.ColumnSpanProperty, 2);
                                        grid.Children.Add(rb);

                                        var radio = new DtwUiRadioButton(rb,
                                        item =>
                                        {
                                            var val = item.Name.Replace(item.GroupName, "");
                                            item.IsChecked = val.Equals(extLine.Method.ToString());
                                        },
                                        (o, e) =>
                                        {
                                            var item = o as RadioButton;
                                            if (item == null) return;

                                            var val = item.Name.Replace(item.GroupName, "");
                                            var method = (eTachEonExtensionMethods)Enum.Parse(typeof(eTachEonExtensionMethods), val);
                                            if (extLine.Method != method)
                                            {
                                                extLine.Method = method;
                                                RefreshUI();
                                                ForceRefresh();
                                            }
                                        });
                                        _uiItems.Add(radio);

                                        index++;
                                    }

                                    var textBox = new TextBox
                                    {
                                        TextAlignment = System.Windows.TextAlignment.Center,
                                        VerticalAlignment = VerticalAlignment.Center,
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        Width = 40,
                                        Margin = new Thickness(5, 0, 5, 0),
                                        Text = extLine.Count.ToString()
                                    };
                                    textBox.SetValue(Grid.RowProperty, 1);
                                    textBox.SetValue(Grid.RowSpanProperty, 2);
                                    textBox.SetValue(Grid.ColumnProperty, 1);
                                    grid.Children.Add(textBox);

                                    var txtNum = new DtwUiTextBoxInteger(textBox,
                                    item =>
                                    {
                                        item.Visibility = extLine.Method == eTachEonExtensionMethods.NumberOfSignals ? Visibility.Visible : Visibility.Hidden;
                                    },
                                    val =>
                                    {
                                        if (val != extLine.Count)
                                        {
                                            extLine.Count = val;
                                            RefreshUI();
                                            ForceRefresh();
                                        }
                                    },
                                    val =>
                                    {
                                        return val != extLine.Count;
                                    });
                                    _uiItems.Add(txtNum);
                                };

                                maker("Angle", __angleExtension);
                                maker("Dot", __dotExtension);
                                maker("Open", __oplExtension);
                            }
                            #endregion

                            #region GROUPBOX ANCHORS
                            {
                                Action<string, StackPanel> maker = (lbl, parent) =>
                                {
                                    var btn = new Button
                                    {
                                        Content = lbl
                                    };
                                    parent.Children.Add(btn);

                                    _uiItems.Add(new DtwUiButton(btn,
                                    item =>
                                    {
                                        item.IsEnabled = __entitySignal.Anchor.IsSet;
                                    },
                                    (o, e) =>
                                    {
                                        if (ChartPanel == null) return;

                                        var bars = BarsArray[__settings.BarsIndex];
                                        if (bars == null) return;

                                        if (__processor.CurrentSignal == null) return;

                                        var startPrice = __entitySignal.Anchor.StartPrice;
                                        var endPrice = __entitySignal.Anchor.EndPrice;
                                        var isReverse = __entitySignal.Anchor.StartTime.CompareTo(__entitySignal.Anchor.EndTime) > 0;

                                        Signal lastLine = null;
                                        var latestBar = IsIntraday(bars) ? bars.GetTime(bars.Count - 1) : bars.GetSessionEndTime(bars.Count - 1);
                                        var middleBar = chartControl.GetTimeBySlotIndex(chartControl.GetSlotIndexByX((ChartPanel.X + ChartPanel.W) / 7 * 5));
                                        var compareBar = latestBar;
                                        if (lbl == "Locate" && latestBar.CompareTo(middleBar) >= 0)
                                        {
                                            compareBar = middleBar;
                                        }

                                        var link = __processor.Last;
                                        if (IsIntraday(bars) && __entitySignal.ResetsToAnchorTime)
                                        {
                                            while (link != null)
                                            {
                                                var line = link;
                                                link = link.Previous;
                                                if (line.IsEstimated)
                                                    continue;
                                                if (!line.IsPrimary)
                                                    continue;
                                                if (line.BarTime.Hour != __entitySignal.Anchor.StartTime.Hour)
                                                    continue;
                                                if (line.BarTime.Minute != __entitySignal.Anchor.StartTime.Minute)
                                                    continue;
                                                if (line.BarTime.Second != __entitySignal.Anchor.StartTime.Second)
                                                    continue;
                                                lastLine = line;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            while (link != null)
                                            {
                                                var line = link;
                                                link = link.Previous;

                                                if ((isReverse ? line.BarTime.CompareTo(compareBar) : line.EndTime.CompareTo(compareBar)) > 0)
                                                    continue;
                                                if (!line.IsPrimary)
                                                    continue;
                                                if (isReverse && line.IsEven)
                                                    continue;
                                                if (!isReverse && !line.IsEven)
                                                    continue;
                                                lastLine = line;
                                                break;
                                            }
                                        }

                                        if (lastLine != null)
                                        {
                                            var distance = TickSize * 3;
                                            var startTime = isReverse ? lastLine.EndTime : lastLine.BarTime;
                                            var endTime = isReverse ? lastLine.BarTime : lastLine.EndTime;

                                            var startBarIdx = bars.GetBar(startTime);
                                            if (startBarIdx >= 0 && startBarIdx <= bars.Count - 1)
                                            {
                                                startPrice = bars.GetLow(startBarIdx) - distance;
                                            }
                                            var endBarIdx = bars.GetBar(endTime);
                                            if (endBarIdx >= 0 && endBarIdx <= bars.Count - 1)
                                            {
                                                endPrice = bars.GetLow(endBarIdx) - distance;
                                            }

                                            __entitySignal.Anchor.Set(startTime, startPrice, endTime, endPrice);

                                            if (__entityAngle.HasAngle)
                                            {
                                                var ascendingAngle = __entityAngle.Anchor.StartPrice <= __entityAngle.Anchor.EndPrice ? 1 : -1;
                                                if (startBarIdx - __entityAngle.BarDelta >= 0)
                                                {
                                                    var angleStartIdx = startBarIdx - __entityAngle.BarDelta;
                                                    var angleStartTime = IsIntraday(bars) ? bars.GetTime(angleStartIdx) : bars.GetSessionEndTime(angleStartIdx);
                                                    var angleStartPrice = bars.GetHigh(angleStartIdx) + distance;
                                                    __entityAngle.Anchor.Set(angleStartTime, angleStartPrice, IsIntraday(bars) ? bars.GetTime(startBarIdx) : bars.GetSessionEndTime(startBarIdx), angleStartPrice + (__entityAngle.TickDelta * TickSize * ascendingAngle));
                                                }
                                                else if (endBarIdx - __entityAngle.BarDelta >= 0)
                                                {
                                                    var angleStartIdx = endBarIdx - __entityAngle.BarDelta;
                                                    var angleStartTime = IsIntraday(bars) ? bars.GetTime(angleStartIdx) : bars.GetSessionEndTime(angleStartIdx);
                                                    var angleStartPrice = bars.GetHigh(angleStartIdx) + distance;
                                                    __entityAngle.Anchor.Set(angleStartTime, angleStartPrice, IsIntraday(bars) ? bars.GetTime(startBarIdx) : bars.GetSessionEndTime(startBarIdx), angleStartPrice + (__entityAngle.TickDelta * TickSize * ascendingAngle));
                                                }
                                            }
                                        }
                                    }));
                                };

                                var groupBox = new GroupBox { Header = "Anchors" };
                                gridTab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                groupBox.SetValue(Grid.RowProperty, gridTab.RowDefinitions.Count - 1);
                                gridTab.Children.Add(groupBox);

                                var stackPanel = new StackPanel
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Margin = new Thickness(4, 2, 4, 2)
                                };
                                groupBox.Content = stackPanel;

                                maker("Locate", stackPanel);
                                maker("Update", stackPanel);
                            }
                            #endregion

                        }
                    }
                    #endregion

                    #region patterns tab
                    {
                        {
                            var panelTab = new TabItem
                            {
                                Header = "Patterns"
                            };
                            tabControl.Items.Add(panelTab);
                            {
                                var gridTab = new Grid();

                                var scroll = new ScrollViewer
                                {
                                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    Content = gridTab
                                };

                                panelTab.Content = scroll;

                                #region GROUPBOX signal mover
                                {
                                    var groupBox = new GroupBox { Header = "Signal Mover" };
                                    gridTab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                    groupBox.SetValue(Grid.RowProperty, gridTab.RowDefinitions.Count - 1);
                                    gridTab.Children.Add(groupBox);

                                    var stackPanel = new StackPanel
                                    {
                                        Orientation = Orientation.Vertical,
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        Margin = new Thickness(2, 0, 2, 1)
                                    };
                                    groupBox.Content = stackPanel;

                                    var stackSignal = new StackPanel
                                    {
                                        Orientation = Orientation.Horizontal,
                                        HorizontalAlignment = HorizontalAlignment.Center
                                    };
                                    stackPanel.Children.Add(stackSignal);

                                    Action<string, string, StackPanel> signalMove = (which, action, parent) =>
                                    {
                                        var btn = new Button
                                        {
                                            Content = action,
                                            Margin = new Thickness(action == "<<" ? 10 : 1, 0, action == ">>" ? 10 : 1, 0),
                                            Padding = new Thickness(2),
                                            Width = 20,
                                            HorizontalContentAlignment = HorizontalAlignment.Center
                                        };
                                        parent.Children.Add(btn);

                                        var uib = new DtwUiButton(btn,
                                            item =>
                                            {
                                                item.IsEnabled = __entitySignal.Anchor.IsSet;
                                            },
                                            (o, e) =>
                                            {
                                                var bars = BarsArray[__settings.BarsIndex];
                                                switch (which)
                                                {
                                                    case "left":
                                                        switch (action)
                                                        {
                                                            case "<<":
                                                                __entitySignal.AnchorTimeLeft(bars);
                                                                break;
                                                            case "<":
                                                                __entitySignal.AnchorTimeExpandLeft(bars);
                                                                break;
                                                            case ">":
                                                                __entitySignal.AnchorTimeContractLeft(bars);
                                                                break;
                                                        }
                                                        break;
                                                    case "right":
                                                        switch (action)
                                                        {
                                                            case ">>":
                                                                __entitySignal.AnchorTimeRight(bars);
                                                                break;
                                                            case "<":
                                                                __entitySignal.AnchorTimeContractRight(bars);
                                                                break;
                                                            case ">":
                                                                __entitySignal.AnchorTimeExpandRight(bars);
                                                                break;
                                                        }
                                                        break;
                                                }

                                                RefreshUI();
                                            });
                                        _uiItems.Add(uib);
                                    };

                                    signalMove("left", "<", stackSignal);
                                    signalMove("left", ">", stackSignal);
                                    signalMove("left", "<<", stackSignal);

                                    signalMove("right", ">>", stackSignal);
                                    signalMove("right", "<", stackSignal);
                                    signalMove("right", ">", stackSignal);
                                }
                                #endregion

                                #region GROUPBOX PATTERN

                                Action<string, int, StackPanel> scoreBar = (which, id, parent) =>
                                {
                                    var setting = __settings.GetScoreSettingByPatternId(id);

                                    #region label and mfe/mae
                                    {
                                        var grid = new Grid
                                        {
                                            Margin = new Thickness(0)
                                        };
                                        parent.Children.Add(grid);

                                        var lbl = new Label
                                        {
                                            Content = (id <= 0 ? (id == 0 ? "Buy" : "Sell") : id.ToString()) +
                                                      ((which == "all" ? " all" : " wt") + ")"),
                                            Margin = new Thickness(2, 0, 5, 0),
                                            Padding = new Thickness(1)
                                        };
                                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                                        lbl.SetValue(Grid.ColumnProperty, grid.ColumnDefinitions.Count - 1);
                                        grid.Children.Add(lbl);

                                        var textBlock = new TextBlock
                                        {
                                            TextAlignment = System.Windows.TextAlignment.Center,
                                            VerticalAlignment = VerticalAlignment.Center,
                                            HorizontalAlignment = HorizontalAlignment.Center,
                                            MinWidth = 100,
                                            Margin = new Thickness(1)
                                        };
                                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                                        textBlock.SetValue(Grid.ColumnProperty, grid.ColumnDefinitions.Count - 1);
                                        grid.Children.Add(textBlock);

                                        var txt = new DtwUiTextBlock(textBlock,
                                            item =>
                                            {
                                                if (__processor == null) return;
                                                var score = __processor.GetScoreCard(id);
                                                if (score != null)
                                                {
                                                    switch (which)
                                                    {
                                                        case "all":
                                                            item.Text = string.Format("{0:F2}  /  {1:F2}", score.GetAverageMfe(), score.GetAverageMae());
                                                            break;
                                                        case "trend":
                                                            item.Text = string.Format("{0:F2}  /  {1:F2}", score.GetAverageMfeWithTrend(), score.GetAverageMaeWithTrend());
                                                            break;
                                                    }
                                                }
                                            });
                                        _uiItems.Add(txt);
                                    }
                                    #endregion

                                    #region progress bar
                                    {
                                        var grid = new Grid
                                        {
                                            Margin = new Thickness(2)
                                        };
                                        parent.Children.Add(grid);

                                        var progress = new ProgressBar
                                        {
                                            Minimum = 0,
                                            Maximum = 100
                                        };
                                        grid.Children.Add(progress);

                                        var prg = new DtwUiProgress(progress,
                                            item =>
                                            {
                                                item.Value = 0;

                                                if (__processor == null) return;
                                                var score = __processor.GetScoreCard(id);
                                                if (score != null)
                                                {
                                                    var tally = score.GetScore(setting.Method);
                                                    switch (which)
                                                    {
                                                        case "all":
                                                            item.Value = tally.Total == 0 ? 0 : (tally.Success / (double)tally.Total * 100);
                                                            break;
                                                        case "trend":
                                                            item.Value = tally.TotalWithTrend == 0 ? 0 : (tally.SuccessWithTrend / (double)tally.TotalWithTrend * 100);
                                                            break;
                                                    }
                                                }
                                            });
                                        _uiItems.Add(prg);

                                        var textBlock = new TextBlock
                                        {
                                            TextAlignment = System.Windows.TextAlignment.Center,
                                            VerticalAlignment = VerticalAlignment.Center,
                                            HorizontalAlignment = HorizontalAlignment.Center,
                                            MinWidth = 100,
                                            Foreground = Brushes.Black
                                        };
                                        grid.Children.Add(textBlock);

                                        var txt = new DtwUiTextBlock(textBlock,
                                            item =>
                                            {
                                                item.Text = "";

                                                if (__processor == null) return;
                                                var score = __processor.GetScoreCard(id);
                                                if (score != null)
                                                {
                                                    var tally = score.GetScore(setting.Method);
                                                    //Log(tally.Total + ", " + tally.Success);
                                                    switch (which)
                                                    {
                                                        case "all":
                                                            item.Text = string.Format("{0} / {1} ({2:F0}%)", tally.Success, tally.Total, tally.Total == 0 ? 0 : (tally.Success / (double)tally.Total * 100));
                                                            break;
                                                        case "trend":
                                                            item.Text = string.Format("{0} / {1} ({2:F0}%)", tally.SuccessWithTrend, tally.TotalWithTrend, tally.TotalWithTrend == 0 ? 0 : (tally.SuccessWithTrend / (double)tally.TotalWithTrend * 100));
                                                            break;
                                                    }
                                                }
                                            });
                                        _uiItems.Add(txt);
                                    }
                                    #endregion
                                };

                                Action<int, StackPanel> scoreDisplay = (id, parent) =>
                                {
                                    var setting = __settings.GetScoreSettingByPatternId(id);

                                    var border = new Border
                                    {
                                        BorderThickness = new Thickness(1),
                                        Margin = new Thickness(1),
                                        Padding = new Thickness(1),
                                        CornerRadius = new CornerRadius(2)
                                    };
                                    parent.Children.Add(border);

                                    var stackPanel = new StackPanel
                                    {
                                        VerticalAlignment = VerticalAlignment.Center,
                                        HorizontalAlignment = HorizontalAlignment.Stretch
                                    };
                                    border.Child = stackPanel;

                                    scoreBar("trend", id, stackPanel);
                                    scoreBar("all", id, stackPanel);
                                };

                                Action<int> pattern = (id) =>
                                {
                                    //id == 0 means the pattern-neutral pattern and must be handled differently
                                    var patternType = id == 0 ? 0 : (id == 1 ? 1 : (id == 4 ? 2 : 3));
                                    var setting = __settings.GetScoreSettingByPatternId(id);

                                    var stackPattern = new StackPanel
                                    {
                                        Orientation = Orientation.Vertical,
                                        Margin = new Thickness(1)
                                    };

                                    var groupPattern = new GroupBox { Header = id == 0 ? "Pattern Neutral" : string.Format(@"Pattern {0} / {1}", id, id + 1) };
                                    gridTab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                    groupPattern.SetValue(Grid.RowProperty, gridTab.RowDefinitions.Count - 1);
                                    gridTab.Children.Add(groupPattern);
                                    groupPattern.Content = stackPattern;

                                    var gridStats = new Grid
                                    {
                                        //Background = Brushes.Blue
                                    };
                                    stackPattern.Children.Add(gridStats);

                                    #region method/target/stop AND options

                                    var stackMethodTargetStop = new DockPanel
                                    {
                                        //Orientation = Orientation.Vertical,
                                        VerticalAlignment = VerticalAlignment.Stretch,
                                        HorizontalAlignment = HorizontalAlignment.Stretch,
                                        Margin = new Thickness(1, 3, 5, 1)
                                    };
                                    gridStats.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                                    stackMethodTargetStop.SetValue(Grid.ColumnProperty, gridStats.ColumnDefinitions.Count - 1);
                                    gridStats.Children.Add(stackMethodTargetStop);

                                    #region render
                                    //if (id > 0)
                                    {
                                        var checkBox = new CheckBox
                                        {
                                            Content = "Render",
                                            Margin = new Thickness(0, 0, 0, 8),
                                            VerticalAlignment = VerticalAlignment.Center
                                        };
                                        //stackMethodTargetStop.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                        //checkBox.SetValue(Grid.RowProperty, stackMethodTargetStop.RowDefinitions.Count - 1);
                                        checkBox.SetValue(DockPanel.DockProperty, Dock.Top);
                                        stackMethodTargetStop.Children.Add(checkBox);

                                        var check = new DtwUiCheckBox(checkBox,
                                        item =>
                                        {
                                            switch (patternType)
                                            {
                                                case 0:
                                                    item.IsChecked = _patternPrintNeutral ? true : false;
                                                    break;
                                                case 1:
                                                    item.IsChecked = _patternPrint12 ? true : false;
                                                    item.IsEnabled = !_patternPrintNeutral;
                                                    break;
                                                case 2:
                                                    item.IsChecked = _patternPrint45 ? true : false;
                                                    item.IsEnabled = !_patternPrintNeutral;
                                                    break;
                                                case 3:
                                                    item.IsChecked = _patternPrint67 ? true : false;
                                                    item.IsEnabled = !_patternPrintNeutral;
                                                    break;
                                            }
                                        },
                                        (o, e) =>
                                        {
                                            var item = o as CheckBox;
                                            if (item == null) return;

                                            switch (patternType)
                                            {
                                                case 0:
                                                    _patternPrintNeutral = item.IsChecked.HasValue ? item.IsChecked.Value : false;
                                                    break;
                                                case 1:
                                                    _patternPrint12 = item.IsChecked.HasValue ? item.IsChecked.Value : false;
                                                    break;
                                                case 2:
                                                    _patternPrint45 = item.IsChecked.HasValue ? item.IsChecked.Value : false;
                                                    break;
                                                case 3:
                                                    _patternPrint67 = item.IsChecked.HasValue ? item.IsChecked.Value : false;
                                                    break;
                                            }

                                            RefreshUI();
                                            ForceRefresh();
                                        });
                                        _uiItems.Add(check);
                                    }
                                    #endregion

                                    #region method
                                    var combo = new ComboBox
                                    {
                                        SelectedItem = setting.Method.ToString(),
                                        Margin = new Thickness(1, 0, 3, 3),
                                        Padding = new Thickness(4, 3, 0, 3),
                                        Width = 50
                                    };

                                    //stackMethodTargetStop.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                    //combo.SetValue(Grid.RowProperty, stackMethodTargetStop.RowDefinitions.Count - 1);
                                    combo.SetValue(DockPanel.DockProperty, Dock.Top);
                                    stackMethodTargetStop.Children.Add(combo);

                                    var cb = new DtwUiComboBox(combo,
                                        null,
                                        (o, e) =>
                                        {
                                            var item = o as ComboBox;
                                            if (item == null) return;

                                            var val = e.AddedItems[0].ToString();
                                            var method = (eTachEonScoreMethod)Enum.Parse(typeof(eTachEonScoreMethod), val);
                                            if (setting.Method != method)
                                            {
                                                setting.Method = method;
                                                RefreshUI();
                                                ForceRefresh();
                                            }
                                        });
                                    _uiItems.Add(cb);

                                    foreach (var i in "Dot~ATR~Tick~Off".Split("~".ToCharArray()))
                                    {
                                        combo.Items.Add(i);
                                    }
                                    #endregion

                                    #region swing
                                    {
                                        if (id > 1)
                                        {
                                            var stackOptions = new StackPanel
                                            {
                                                Orientation = Orientation.Vertical,
                                                VerticalAlignment = VerticalAlignment.Top,
                                                HorizontalAlignment = HorizontalAlignment.Center,
                                                Margin = new Thickness(1, 2, 1, 2)
                                            };
                                            //stackMethodTargetStop.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                            //stackOptions.SetValue(Grid.RowProperty, stackMethodTargetStop.RowDefinitions.Count - 1);
                                            stackOptions.SetValue(DockPanel.DockProperty, Dock.Bottom);
                                            stackMethodTargetStop.Children.Add(stackOptions);

                                            var lbl = new TextBlock
                                            {
                                                Text = "Swing: ",
                                                TextAlignment = System.Windows.TextAlignment.Left,
                                                VerticalAlignment = VerticalAlignment.Center,
                                                Margin = new Thickness(0, 0, 0, 3)
                                            };
                                            stackOptions.Children.Add(lbl);

                                            var txt = new TextBox
                                            {
                                                TextAlignment = System.Windows.TextAlignment.Center,
                                                VerticalAlignment = VerticalAlignment.Center,
                                                HorizontalAlignment = HorizontalAlignment.Center,
                                                Width = 30,
                                                Padding = new Thickness(1),
                                                Text = __settings.GetSwing(patternType).ToString()
                                            };
                                            stackOptions.Children.Add(txt);

                                            var txtNum = new DtwUiTextBoxInteger(txt,
                                            null,
                                            val =>
                                            {
                                                var existing = __settings.GetSwing(patternType);
                                                if (val != existing)
                                                {
                                                    __settings.SetSwing(patternType, val);
                                                }
                                            },
                                            val =>
                                            {
                                                return val != __settings.GetSwing(patternType);
                                            });
                                            _uiItems.Add(txtNum);
                                        }
                                    }
                                    #endregion

                                    #region target/stop

                                    var theGrid = new Grid
                                    {
                                        //Orientation = Orientation.Vertical
                                    };
                                    stackMethodTargetStop.Children.Add(theGrid);

                                    for (var i = 0; i < 2; i++)
                                    {
                                        var which = i;
                                        var tsGrid = new Grid
                                        {
                                            HorizontalAlignment = HorizontalAlignment.Right
                                        };

                                        //stackMethodTargetStop.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                        //stackMethodTargetStop.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                                        //tsGrid.SetValue(Grid.RowProperty, stackMethodTargetStop.RowDefinitions.Count - 1);
                                        //tsGrid.SetValue(DockPanel.DockProperty, Dock.Top);
                                        theGrid.Children.Add(tsGrid);

                                        var tsItem = new DtwUiGrid(tsGrid,
                                            item =>
                                            {
                                                if (which == 0 && setting.Method == eTachEonScoreMethod.ATR)
                                                {
                                                    item.Visibility = Visibility.Visible;
                                                }
                                                else if (which == 1 && setting.Method == eTachEonScoreMethod.Tick)
                                                {
                                                    item.Visibility = Visibility.Visible;
                                                }
                                                else
                                                {
                                                    item.Visibility = Visibility.Collapsed;
                                                }
                                            });
                                        _uiItems.Add(tsItem);

                                        //target
                                        {
                                            var stackPanel = new StackPanel
                                            {
                                                Orientation = Orientation.Horizontal,
                                                HorizontalAlignment = HorizontalAlignment.Right,
                                                VerticalAlignment = VerticalAlignment.Center,
                                                Margin = new Thickness(0, 0, 3, 0)
                                            };
                                            tsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                            stackPanel.SetValue(Grid.RowProperty, tsGrid.RowDefinitions.Count - 1);
                                            tsGrid.Children.Add(stackPanel);

                                            var label = new Label
                                            {
                                                Content = "T",
                                                HorizontalContentAlignment = HorizontalAlignment.Right,
                                                Foreground = chartControl.Properties.ChartText
                                            };
                                            stackPanel.Children.Add(label);

                                            var textBox = new TextBox
                                            {
                                                TextAlignment = System.Windows.TextAlignment.Center,
                                                VerticalAlignment = VerticalAlignment.Center,
                                                HorizontalAlignment = HorizontalAlignment.Center,
                                                Width = 30,
                                                Padding = new Thickness(1)
                                            };
                                            stackPanel.Children.Add(textBox);

                                            if (which == 0)
                                            {
                                                textBox.Text = setting.AtrTarget.ToString();
                                                var txtNum = new DtwUiTextBoxDecimal(textBox,
                                                null,
                                                val =>
                                                {
                                                    var existing = setting.AtrTarget;
                                                    if (val != existing)
                                                    {
                                                        setting.AtrTarget = val;
                                                    }
                                                },
                                                val =>
                                                {
                                                    return val != setting.AtrTarget;
                                                });
                                                _uiItems.Add(txtNum);
                                            }
                                            else
                                            {
                                                textBox.Text = setting.TickTarget.ToString();
                                                var txtNum = new DtwUiTextBoxInteger(textBox,
                                                null,
                                                val =>
                                                {
                                                    var existing = setting.TickTarget;
                                                    if (val != existing)
                                                    {
                                                        setting.TickTarget = val;
                                                    }
                                                },
                                                val =>
                                                {
                                                    return val != setting.TickTarget;
                                                });
                                                _uiItems.Add(txtNum);
                                            }
                                        }
                                        //stop
                                        {
                                            var stackPanel = new StackPanel
                                            {
                                                Orientation = Orientation.Horizontal,
                                                HorizontalAlignment = HorizontalAlignment.Right,
                                                VerticalAlignment = VerticalAlignment.Center,
                                                Margin = new Thickness(0, 0, 3, 0)
                                            };
                                            tsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                            stackPanel.SetValue(Grid.RowProperty, tsGrid.RowDefinitions.Count - 1);
                                            tsGrid.Children.Add(stackPanel);

                                            var label = new Label
                                            {
                                                Content = "S",
                                                HorizontalContentAlignment = HorizontalAlignment.Right,
                                                Foreground = chartControl.Properties.ChartText
                                            };
                                            stackPanel.Children.Add(label);

                                            var textBox = new TextBox
                                            {
                                                TextAlignment = System.Windows.TextAlignment.Center,
                                                VerticalAlignment = VerticalAlignment.Center,
                                                HorizontalAlignment = HorizontalAlignment.Center,
                                                Width = 30,
                                                Padding = new Thickness(1)
                                            };
                                            stackPanel.Children.Add(textBox);

                                            if (which == 0)
                                            {
                                                textBox.Text = setting.AtrStop.ToString();
                                                var txtNum = new DtwUiTextBoxDecimal(textBox,
                                                null,
                                                val =>
                                                {
                                                    var existing = setting.AtrStop;
                                                    if (val != existing)
                                                    {
                                                        setting.AtrStop = val;
                                                    }
                                                },
                                                val =>
                                                {
                                                    return val != setting.AtrStop;
                                                });
                                                _uiItems.Add(txtNum);
                                            }
                                            else
                                            {
                                                textBox.Text = setting.TickStop.ToString();
                                                var txtNum = new DtwUiTextBoxInteger(textBox,
                                                null,
                                                val =>
                                                {
                                                    var existing = setting.TickStop;
                                                    if (val != existing)
                                                    {
                                                        setting.TickStop = val;
                                                    }
                                                },
                                                val =>
                                                {
                                                    return val != setting.TickStop;
                                                });
                                                _uiItems.Add(txtNum);
                                            }
                                        }
                                    }
                                    #endregion

                                    #endregion

                                    #region stats

                                    var stackStats = new StackPanel
                                    {
                                        Orientation = Orientation.Vertical,
                                        VerticalAlignment = VerticalAlignment.Center,
                                        Margin = new Thickness(1)
                                    };
                                    gridStats.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                                    stackStats.SetValue(Grid.ColumnProperty, gridStats.ColumnDefinitions.Count - 1);
                                    gridStats.Children.Add(stackStats);

                                    scoreDisplay(id, stackStats);
                                    //neutral pattern is a different index
                                    scoreDisplay(id + (id == 0 ? -1 : 1), stackStats);

                                    #endregion
                                };

                                //each pass creates new Groupbox for a pattern set
                                pattern(1);
                                pattern(4);
                                pattern(6);

                                //pattern 0 represents the Neutral patterns
                                pattern(0);

                                #endregion

                                #region GROUPBOX scoring timeframe
                                {
                                    var groupBox = new GroupBox { Header = "Trading Times" };
                                    gridTab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                    groupBox.SetValue(Grid.RowProperty, gridTab.RowDefinitions.Count - 1);
                                    gridTab.Children.Add(groupBox);

                                    var stackTime = new StackPanel
                                    {
                                        Orientation = Orientation.Horizontal,
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        VerticalAlignment = VerticalAlignment.Center,
                                        Margin = new Thickness(2, 1, 2, 1)
                                    };
                                    groupBox.Content = stackTime;

                                    var checkBox = new CheckBox
                                    {
                                        Content = "",
                                        Margin = new Thickness(5, 0, 5, 0)
                                    };
                                    stackTime.Children.Add(checkBox);

                                    var check = new DtwUiCheckBox(checkBox,
                                        item =>
                                        {
                                            item.IsChecked = __settings.IsTimeRestricted;
                                        },
                                        (o, e) =>
                                        {
                                            var item = o as CheckBox;
                                            if (item == null) return;

                                            __settings.IsTimeRestricted = item.IsChecked.HasValue ? item.IsChecked.Value : false;
                                            RefreshUI();
                                            ForceRefresh();
                                        });
                                    _uiItems.Add(check);

                                    for (var i = 0; i < 2; i++)
                                    {
                                        var isStart = i == 0;

                                        var textBox = new TextBox
                                        {
                                            TextAlignment = System.Windows.TextAlignment.Center,
                                            VerticalAlignment = VerticalAlignment.Center,
                                            HorizontalAlignment = HorizontalAlignment.Center,
                                            MinWidth = 60,
                                            Margin = new Thickness(2, 0, 2, 0),
                                            Padding = new Thickness(1),
                                            Text = IndicatorSettings.FormatTime(isStart ? __settings.TimeStart : __settings.TimeEnd)
                                        };
                                        stackTime.Children.Add(textBox);

                                        var txtNum = new DtwUiTextBoxTime(textBox,
                                            item =>
                                            {
                                                item.IsEnabled = __settings.IsTimeRestricted;
                                            },
                                            val =>
                                            {
                                                if (IndicatorSettings.IsValidString(val))
                                                {
                                                    if (isStart)
                                                    {
                                                        __settings.SetScoreStart(val);
                                                    }
                                                    else
                                                    {
                                                        __settings.SetScoreEnd(val);
                                                    }
                                                }
                                                if (isStart)
                                                {
                                                    textBox.Text = IndicatorSettings.FormatTime(__settings.TimeStart);
                                                }
                                                else
                                                {
                                                    textBox.Text = IndicatorSettings.FormatTime(__settings.TimeEnd);
                                                }
                                            },
                                            val =>
                                            {
                                                if (isStart)
                                                {
                                                    return !__settings.GetScoreStart().Equals(val);
                                                }
                                                else
                                                {
                                                    return !__settings.GetScoreEnd().Equals(val);
                                                }
                                            }, isStart ? __settings.GetScoreStart() : __settings.GetScoreEnd());
                                        _uiItems.Add(txtNum);

                                        if (isStart)
                                        {
                                            var toLabel = new TextBlock
                                            {
                                                Text = "-",
                                                Margin = new Thickness(2, 0, 2, 0)
                                            };
                                            stackTime.Children.Add(toLabel);
                                            //var toLbl = new DtwUiTextBlock(toLabel, null);
                                        }
                                    }
                                }
                                #endregion

                                #region GROUPBOX OTHER
                                {
                                    var groupBox = new GroupBox { Header = "Other" };
                                    gridTab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                    groupBox.SetValue(Grid.RowProperty, gridTab.RowDefinitions.Count - 1);
                                    gridTab.Children.Add(groupBox);

                                    var stackPanel = new StackPanel
                                    {
                                        Orientation = Orientation.Vertical,
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        Margin = new Thickness(2, 0, 2, 1)
                                    };
                                    groupBox.Content = stackPanel;

                                    {
                                        var stackDot = new StackPanel
                                        {
                                            Orientation = Orientation.Horizontal,
                                            HorizontalAlignment = HorizontalAlignment.Center,
                                            Margin = new Thickness(0, 0, 0, 5)
                                        };
                                        stackPanel.Children.Add(stackDot);

                                        var label = new Label
                                        {
                                            Content = "Dot Offset",
                                            HorizontalContentAlignment = HorizontalAlignment.Right,
                                            Foreground = chartControl.Properties.ChartText,
                                            Margin = new Thickness(0, 0, 2, 0)
                                        };
                                        stackDot.Children.Add(label);

                                        var textBox = new TextBox
                                        {
                                            TextAlignment = System.Windows.TextAlignment.Center,
                                            VerticalAlignment = VerticalAlignment.Center,
                                            HorizontalAlignment = HorizontalAlignment.Center,
                                            Width = 30,
                                            Padding = new Thickness(1),
                                            Text = __settings.DotOffset.ToString()
                                        };
                                        stackDot.Children.Add(textBox);

                                        var txtNum = new DtwUiTextBoxInteger(textBox,
                                        null,
                                        val =>
                                        {
                                            if (val != __settings.DotOffset)
                                            {
                                                __settings.DotOffset = val;
                                            }
                                        },
                                        val =>
                                        {
                                            return val != __settings.DotOffset;
                                        });
                                        _uiItems.Add(txtNum);
                                    }
                                    {
                                        var checkBox = new CheckBox
                                        {
                                            Content = "Render Scores",
                                            Margin = new Thickness(5, 0, 5, 0)
                                        };
                                        stackPanel.Children.Add(checkBox);

                                        var check = new DtwUiCheckBox(checkBox,
                                        item =>
                                        {
                                            item.IsChecked = _scoreRender ? true : false;
                                        },
                                        (o, e) =>
                                        {
                                            var item = o as CheckBox;
                                            if (item == null) return;

                                            _scoreRender = item.IsChecked.HasValue ? item.IsChecked.Value : false;

                                            RefreshUI();
                                            ForceRefresh();
                                        });
                                        _uiItems.Add(check);
                                    }
                                    {
                                        var checkBox = new CheckBox
                                        {
                                            Content = "Require Close",
                                            Margin = new Thickness(5, 0, 5, 0)
                                        };
                                        stackPanel.Children.Add(checkBox);

                                        var check = new DtwUiCheckBox(checkBox,
                                            item =>
                                            {
                                                item.IsChecked = ScoresRequireClose ? true : false;
                                            },
                                            (o, e) =>
                                            {
                                                var item = o as CheckBox;
                                                if (item == null) return;

                                                ScoresRequireClose = item.IsChecked.HasValue ? item.IsChecked.Value : false;

                                                RefreshUI();
                                                ForceRefresh();
                                            });
                                        _uiItems.Add(check);
                                    }
                                }
                                #endregion
                            }
                        }
                    }
                    #endregion

                    #region info tab
                    {
                        if (__infoEnabled)
                        {
                            var panelTab = new TabItem
                            {
                                Header = "Info"
                            };
                            tabControl.Items.Add(panelTab);
                            {
                                var gridTab = new Grid();

                                var scroll = new ScrollViewer
                                {
                                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    Content = gridTab
                                };

                                panelTab.Content = scroll;

                                var groupBox = new GroupBox
                                {
                                    Header = "Information"
                                };
                                gridTab.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                                groupBox.SetValue(Grid.RowProperty, gridTab.RowDefinitions.Count - 1);
                                gridTab.Children.Add(groupBox);

                                var textBlock = new TextBlock
                                {
                                    FontSize = chartControl.Properties.LabelFont.Size,
                                    FontFamily = chartControl.Properties.LabelFont.Family,
                                    TextAlignment = System.Windows.TextAlignment.Left,
                                    VerticalAlignment = VerticalAlignment.Top,
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Padding = new Thickness(2),
                                    Margin = new Thickness(0),
                                    //IsReadOnly = true
                                };
                                groupBox.Content = textBlock;

                                Action<Signal, StringBuilder, string> infoText = (line, sb, prefix) =>
                                {
                                    const string space = "    ";

                                    bool isBuy = (line.IsEven && !__entitySignal.IsReversePolarity)
                                                 || (!line.IsEven && __entitySignal.IsReversePolarity);

                                    sb.AppendFormat("{3} Signal: {2}",
                                        Environment.NewLine,
                                        space,
                                        line.BarTime.ToString(),
                                        isBuy ? "Buy" : "Sell"
                                        ).AppendLine();

                                    sb.AppendFormat("{1}Atr ({3}): {2:F2}",
                                        Environment.NewLine,
                                        space,
                                        line.Atr,
                                        __settings.AtrValue
                                        ).AppendLine();

                                    Pattern pattern = null;//isBuy && line.Trend == 1 ? "With" : "Against",

                                    //if @green line
                                    if (line.IsBuySignal(__entitySignal.IsReversePolarity))
                                    {
                                        pattern = line.BuyPattern;
                                        if (pattern != null)
                                        {
                                            bool isDefaultPattern = pattern.GetPatternType() == 1;

                                            sb.AppendFormat("{3} Pattern: {2}{0}{1}{4}",
                                                Environment.NewLine,
                                                space,
                                                pattern.GetPatternId(true).ToString(),
                                                isDefaultPattern ? "Buy" : "Sell",
                                                (isDefaultPattern && line.Trend == 1) || (!isDefaultPattern && line.Trend != 1) ? "With Trend" : "Counter Trend"
                                                ).AppendLine();

                                            switch (pattern.GetPatternType())
                                            {
                                                case 2:
                                                    var p2 = pattern as PatternTwo;
                                                    if (p2 != null)
                                                    {
                                                        for (var i = 2; i >= 0; i--)
                                                        {
                                                            if (p2.IsSet(i))
                                                            {
                                                                sb.AppendFormat("{1}Bar {4} {2}: {3}", Environment.NewLine, space, p2.GetTargetTime(i), p2.GetTargetValue(i), i == 0 ? "after" : (i == 2 ? "before" : "signal")).AppendLine();
                                                            }
                                                        }
                                                    }
                                                    break;
                                                case 3:
                                                    var p3 = pattern as PatternThree;
                                                    if (p3 != null)
                                                    {
                                                        sb.AppendFormat("{1}Engulfing bar {2}: {3} - {4}", Environment.NewLine, space, p3.BarTime, p3.Low, p3.High).AppendLine();
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        pattern = line.SellPattern;
                                        if (pattern != null)
                                        {
                                            bool isDefaultPattern = pattern.GetPatternType() == 1;

                                            sb.AppendFormat("{3} Pattern: {2}{0}{1}{4}",
                                                Environment.NewLine,
                                                space,
                                                pattern.GetPatternId(false).ToString(),
                                                isDefaultPattern ? "Sell" : "Buy",
                                                (isDefaultPattern && line.Trend == -1) || (!isDefaultPattern && line.Trend == 1) ? "With Trend" : "Counter Trend"
                                                ).AppendLine();

                                            switch (pattern.GetPatternType())
                                            {
                                                case 2:
                                                    var p2 = pattern as PatternTwo;
                                                    if (p2 != null)
                                                    {
                                                        for (var i = 2; i >= 0; i--)
                                                        {
                                                            if (p2.IsSet(i))
                                                            {
                                                                sb.AppendFormat("{1}Bar {4} {2}: {3}", Environment.NewLine, space, p2.GetTargetTime(i), p2.GetTargetValue(i), i == 0 ? "after" : (i == 2 ? "before" : "signal")).AppendLine();
                                                            }
                                                        }
                                                    }
                                                    break;
                                                case 3:
                                                    var p3 = pattern as PatternThree;
                                                    if (p3 != null)
                                                    {
                                                        sb.AppendFormat("{1}Engulfing bar {2}: {3} - {4}", Environment.NewLine, space, p3.BarTime, p3.Low, p3.High).AppendLine();
                                                    }
                                                    break;
                                            }
                                        }
                                    }

                                    if (pattern != null)
                                    {
                                        sb.AppendFormat("{0}MFE: {1}", space, pattern.MaximumFavorableExcursion).AppendLine();
                                        sb.AppendFormat("{0}MAE: {1}", space, pattern.MaximumAdverseExcursion).AppendLine();

                                        if (pattern.ScoreDot != null && pattern.ScoreDot.IsSet())
                                        {
                                            sb.AppendFormat("{1}Dot Violation:{0}{1}{1}{2:F2}{0}{1}{1}@ {3}", Environment.NewLine, space, pattern.ScoreDot.Dot, pattern.ScoreDot.TimeDot).AppendLine();
                                        }

                                        if (pattern.ScoreAtr != null && pattern.ScoreAtr.IsSet())
                                        {
                                            sb.AppendFormat("{1}Hit {2} {3}:{0}{1}{1}{4:F2}{0}{1}{1}@ {5}", Environment.NewLine, space, "Atr", pattern.ScoreAtr.IsSuccess() ? "Target" : "Stop", pattern.ScoreAtr.IsSuccess() ? pattern.ScoreAtr.Target : pattern.ScoreAtr.Stop, pattern.ScoreAtr.IsSuccess() ? pattern.ScoreAtr.TimeTarget : pattern.ScoreAtr.TimeStop).AppendLine();
                                        }

                                        if (pattern.ScoreTick != null && pattern.ScoreTick.IsSet())
                                        {
                                            sb.AppendFormat("{1}Hit {2} {3}:{0}{1}{1}{4:F2}{0}{1}{1}@ {5}", Environment.NewLine, space, "Tick", pattern.ScoreTick.IsSuccess() ? "Target" : "Stop", pattern.ScoreTick.IsSuccess() ? pattern.ScoreTick.Target : pattern.ScoreTick.Stop, pattern.ScoreTick.IsSuccess() ? pattern.ScoreTick.TimeTarget : pattern.ScoreTick.TimeStop).AppendLine();
                                        }
                                    }

                                    sb.AppendLine();

                                    if (__debugging)
                                    {
                                        sb.AppendLine().AppendLine("DEBUG").AppendLine().Append("BarIndex: ").Append(line.BarIndex).AppendLine();

                                        sb.AppendFormat("Child Number: {0}", line.ChildNumber).AppendLine();

                                        {
                                            var patternDebug = line.BuyPattern;
                                            if (patternDebug != null)
                                            {
                                                sb.AppendLine().AppendFormat("Buy Pattern: {0}", patternDebug.GetPatternType()).AppendLine();
                                                sb.Append("MFE: ").Append(patternDebug.MaximumFavorableExcursion).AppendLine();
                                                sb.Append("MAE: ").Append(patternDebug.MaximumAdverseExcursion).AppendLine();
                                                if (patternDebug.ScoreDot != null)
                                                {
                                                    sb.Append("Dot: ").Append(patternDebug.ScoreDot.Dot).AppendLine();
                                                }
                                                if (patternDebug.ScoreAtr != null)
                                                {
                                                    sb.AppendFormat("Atr: Target = {0}, Stop = {1}", patternDebug.ScoreAtr.Target, patternDebug.ScoreAtr.Stop).AppendLine();
                                                }
                                                if (patternDebug.ScoreTick != null)
                                                {
                                                    sb.AppendFormat("Tick: Target = {0}, Stop = {1}", patternDebug.ScoreTick.Target, patternDebug.ScoreTick.Stop).AppendLine();
                                                }
                                            }
                                        }

                                        {
                                            var patternDebug = line.SellPattern;
                                            if (patternDebug != null)
                                            {
                                                sb.AppendLine().AppendFormat("Sell Pattern: {0}",
                                                    patternDebug.GetPatternType()).AppendLine();
                                                sb.Append("MFE: ").Append(patternDebug.MaximumFavorableExcursion)
                                                    .AppendLine();
                                                sb.Append("MAE: ").Append(patternDebug.MaximumAdverseExcursion)
                                                    .AppendLine();
                                                if (patternDebug.ScoreDot != null)
                                                {
                                                    sb.Append("Dot: ").Append(patternDebug.ScoreDot.Dot).AppendLine();
                                                }

                                                if (patternDebug.ScoreAtr != null)
                                                {
                                                    sb.AppendFormat("Atr: Target = {0}, Stop = {1}",
                                                            patternDebug.ScoreAtr.Target, patternDebug.ScoreAtr.Stop)
                                                        .AppendLine();
                                                }

                                                if (patternDebug.ScoreTick != null)
                                                {
                                                    sb.AppendFormat("Tick: Target = {0}, Stop = {1}",
                                                            patternDebug.ScoreTick.Target, patternDebug.ScoreTick.Stop)
                                                        .AppendLine();
                                                }
                                            }
                                        }
                                    }

                                    sb.AppendLine();
                                };

                                var txt = new DtwUiTextBlock(textBlock,
                                    item =>
                                    {
                                        item.Text = "";

                                        if (__processor == null) return;

                                        if (__processor.InfoSignal != null)
                                        {
                                            var sb = new StringBuilder();

                                            {
                                                var line = __processor.InfoSignal;
                                                infoText(line, sb, "");
                                            }

                                            item.Text = sb.ToString();
                                        }
                                    });
                                _uiItems.Add(txt);

                                groupBox = new GroupBox
                                {
                                    Header = "Export"
                                };
                                gridTab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                groupBox.SetValue(Grid.RowProperty, gridTab.RowDefinitions.Count - 1);
                                gridTab.Children.Add(groupBox);

                                var stackPanel = new StackPanel
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalAlignment = HorizontalAlignment.Center
                                };
                                groupBox.Content = stackPanel;

                                var btnExport = new Button
                                {
                                    Content = "Signals"
                                };
                                stackPanel.Children.Add(btnExport);
                                var btn = new DtwUiButton(btnExport,
                                    item =>
                                    {
                                        item.IsEnabled = __entitySignal.Anchor.IsSet;
                                    },
                                    (o, e) =>
                                    {
                                        if (__processor == null) return;
                                        __processor.ExportSignals();
                                    });

                                _uiItems.Add(btn);
                            }
                        }
                    }
                    #endregion

                    #region future tab
                    /*
                    {
                        var panelTab = new TabItem
                        {
                            Header = "Future"
                        };
                        tabControl.Items.Add(panelTab);
                        {
                            var gridTab = new Grid();
                            
                            panelTab.Content = gridTab;

                            var groupBox = new GroupBox
                            {
                                Header = "Future"
                            };
                            gridTab.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                            groupBox.SetValue(Grid.RowProperty, gridTab.RowDefinitions.Count - 1);
                            gridTab.Children.Add(groupBox);

                            var gridGroup = new Grid();
                            groupBox.Content = gridGroup;

                            #region from/to dates
                            var stackPanel = new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                HorizontalAlignment = HorizontalAlignment.Center
                            };
                            gridGroup.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                            stackPanel.SetValue(Grid.RowProperty, gridGroup.RowDefinitions.Count - 1);
                            gridGroup.Children.Add(stackPanel);

                            var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                            stackPanel.Children.Add(grid);

                            var label = new Label
                            {
                                Content = "From:",
                                HorizontalContentAlignment = HorizontalAlignment.Right
                            };
                            label.SetValue(Grid.RowProperty, 0);
                            label.SetValue(Grid.ColumnProperty, 0);
                            grid.Children.Add(label);

                            label = new Label
                            {
                                Content = "To:",
                                HorizontalContentAlignment = HorizontalAlignment.Right
                            };
                            label.SetValue(Grid.RowProperty, 1);
                            label.SetValue(Grid.ColumnProperty, 0);
                            grid.Children.Add(label);

                            var futureStart = new DatePicker
                            {
                                HorizontalContentAlignment = HorizontalAlignment.Center
                            };
                            futureStart.SetValue(Grid.RowProperty, 0);
                            futureStart.SetValue(Grid.ColumnProperty, 1);
                            grid.Children.Add(futureStart);

                            var futureEnd = new DatePicker
                            {
                                HorizontalContentAlignment = HorizontalAlignment.Center
                            };
                            futureEnd.SetValue(Grid.RowProperty, 1);
                            futureEnd.SetValue(Grid.ColumnProperty, 1);
                            grid.Children.Add(futureEnd);

                            var date = new ForceUIDatePicker(futureStart,
                                o =>
                                {
                                },
                                (o, e) =>
                                {
                                    if (!futureEnd.SelectedDate.HasValue)
                                    {
                                        futureEnd.SelectedDate = futureStart.SelectedDate;
                                    }
                                });
                            _uiItems.Add(date);

                            date = new ForceUIDatePicker(futureEnd,
                                o =>
                                {
                                },
                                (o, e) =>
                                {
                                //Log("changed: " + futureEnd.SelectedDate);
                            });
                            _uiItems.Add(date);
                            #endregion

                            #region functions
                            var futureList = new ObservableCollection<DateTime>();
                            var lastCalc = "";

                            stackPanel = new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                HorizontalAlignment = HorizontalAlignment.Center
                            };
                            gridGroup.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                            stackPanel.SetValue(Grid.RowProperty, gridGroup.RowDefinitions.Count - 1);
                            gridGroup.Children.Add(stackPanel);

                            var btnCalc = new Button
                            {
                                Content = "Calculate"
                            };
                            stackPanel.Children.Add(btnCalc);
                            var btn = new ForceUIButton(btnCalc,
                                item =>
                                {
                                    if (__entitySignal.Anchor.IsSet)
                                    {
                                        if (!__entitySignal.ToSerialString().Equals(lastCalc))
                                        {
                                            futureList.Clear();
                                        }
                                        item.IsEnabled = true;
                                        return;
                                    }

                                    item.IsEnabled = false;
                                },
                                (o, e) =>
                                {
                                    var start = futureStart.SelectedDate;
                                    if (!start.HasValue) return;
                                    var end = futureEnd.SelectedDate;
                                    if (!end.HasValue) return;

                                    //var list = CalculateFutureLines(chartControl, __primaryCompute, (DateTime)start, ((DateTime)end).AddDays(1), true);
                                    var list = CalculateFutureSignals(chartControl, __primaryAnalysis, (DateTime)start, ((DateTime)end).AddDays(1), true);
                                    futureList.Clear();
                                    foreach (var l in list)
                                    {
                                        futureList.Add(l.BarTime);
                                    }

                                    lastCalc = __entitySignal.ToSerialString();
                                    RefreshUI();
                                });

                            _uiItems.Add(btn);

                            var btnExport = new Button
                            {
                                Content = "Export"
                            };
                            stackPanel.Children.Add(btnExport);
                            btn = new ForceUIButton(btnExport,
                                item =>
                                {
                                    item.IsEnabled = futureList.Count > 0;
                                },
                                (o, e) =>
                                {
                                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                                    saveFileDialog.Filter = "Comma delimited file (*.csv)|*.csv|Text file (*.txt)|*.txt";
                                    saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                                    saveFileDialog.FileName = string.Format(@"FutureForceSignals_{0}_{1}_{2}", Instrument.FullName, BarsArray[0].BarsPeriod.BarsPeriodType, BarsArray[0].BarsPeriod.Value);
                                    if (saveFileDialog.ShowDialog() == true)
                                    {
                                        StringBuilder sb = new StringBuilder();
                                        foreach (var l in futureList)
                                        {
                                            sb.AppendLine(l.ToString());
                                        }
                                        File.WriteAllText(saveFileDialog.FileName, sb.ToString());
                                    }
                                });

                            _uiItems.Add(btn);
                            #endregion

                            #region list
                            var scroll = new ScrollViewer { Background = Brushes.Transparent };
                            gridGroup.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                            scroll.SetValue(Grid.RowProperty, gridGroup.RowDefinitions.Count - 1);
                            gridGroup.Children.Add(scroll);

                            scroll.Content = new ListBox
                            {
                                ItemsSource = futureList
                            };
                            #endregion

                        }
                    }
                    */
                    #endregion

                    #region debug
                    {
                        //if (__debugging)
                        //{
                        //    var panelTab = new TabItem
                        //    {
                        //        Header = "Debug"
                        //    };
                        //    tabControl.Items.Add(panelTab);
                        //}
                    }
                    #endregion
                }

                #endregion

                #region chart button to toggle panel
                var chartToggle = new DtwUiChartToolbarButton(
                    chartWindow,
                    sortOrder++,
                    o =>
                    {
                        var toolbar = o as DtwUiChartToolbarButton;
                        if (toolbar == null) return;
                        var item = toolbar.Button;
                        item.Content = "Force";
                        item.IsEnabled = true;
                    },
                    (o, e) =>
                    {
                        _panelVisible = !_panelVisible;
                        uiChartPanel.MakeVisible(_panelVisible);
                    });
                _chartItems.Add(chartToggle);

                #endregion

                #region chart button for debug action
                if (__debugging)
                {
                    var debug = new DtwUiChartToolbarButton(
                    chartWindow,
                    sortOrder++,
                    o =>
                    {
                        var toolbar = o as DtwUiChartToolbarButton;
                        if (toolbar == null) return;
                        var item = toolbar.Button;
                        item.Content = @"Debug";
                        item.IsEnabled = true;
                    },
                    (o, e) =>
                    {
                        DebugAction();
                    });
                    _chartItems.Add(debug);
                }
                #endregion

                RefreshUI();

                if (TabSelected())
                    AddChartItems();
            }
            catch (Exception err)
            {
                throw new ForceException(this, string.Format("CreateUI: {0}", err));
            }
        }

        private void DisposeUI(ChartControl chartControl)
        {
            try
            {
                chartControl.MouseLeftButtonDown -= Chart_MouseDown;
                //chartControl.MouseLeftButtonUp -= chart_MouseUp;
                chartControl.MouseMove -= Chart_MouseMove;
                //chartControl.PreviewKeyDown -= Chart_PreviewKeyDown;
                if (__infoEnabled)
                {
                    chartControl.MouseDown -= Chart_MouseDownInfo;
                }

                var chartWindow = Window.GetWindow(chartControl.Parent) as Chart;
                if (chartWindow == null) return;

                //chartWindow.PreviewKeyDown -= Chart_PreviewKeyDown;

                chartWindow.MainTabControl.SelectionChanged -= TabSelectionChangedHandler;

                foreach (var item in _uiItems)
                {
                    item.Dispose();
                }

                foreach (var item in _chartItems)
                {
                    item.Dispose();
                }
            }
            catch (Exception err)
            {
                throw new ForceException(string.Format("DisposeUI: {0}", err.Message));
            }
        }

        private void RefreshUI()
        {
            foreach (var item in _uiItems)
                item.Refresh();
            foreach (var item in _chartItems)
                item.Refresh();
        }

        private void AddChartItems()
        {
            foreach (var item in _chartItems)
                item.Add();
        }

        private void RemoveChartItems()
        {
            foreach (var item in _chartItems)
                item.Remove();
        }

        private void TabSelectionChangedHandler(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0) return;

            var tabItem = e.AddedItems[0] as System.Windows.Controls.TabItem;
            if (tabItem == null) return;

            var chartTab = tabItem.Content as ChartTab;
            if (chartTab == null) return;

            if (TabSelected())
                AddChartItems();
            else
                RemoveChartItems();
        }

        private bool TabSelected()
        {
            if (ChartControl == null) return false;
            var chartWindow = Window.GetWindow(ChartControl.Parent) as Chart;
            if (chartWindow == null) return false;

            foreach (TabItem tab in chartWindow.MainTabControl.Items)
            {
                var chartTab = tab.Content as ChartTab;
                if (chartTab == null) continue;
                if (chartTab.ChartControl == null) continue;
                if (chartTab.ChartControl.Equals(ChartControl) && tab.Equals(chartWindow.MainTabControl.SelectedItem))
                    return true;
            }
            return false;
        }

        #endregion UI create/dispose/etc

        #region Chart MouseControl / KeyControl

        private bool Anchor_MouseDown(ChartControl chartControl, ForceAnchor anchor, System.Windows.Point point, bool snap, bool isPulse, Action onCompleteDown = null)
        {
            //TODO:  is this really necessary??
            var stateChange = false;

            if (anchor.State == ForceAnchor.DrawingState.Normal)
            {
                if (!anchor.IsSet || !anchor.IsDrawn) return stateChange;

                anchor.State = anchor.WhichHandle(point);
                if (anchor.State == ForceAnchor.DrawingState.Moving)
                {
                    anchor.CenterHandleFrozen = anchor.CenterHandle;
                    anchor.StartHandleFrozen = anchor.StartHandle;
                    anchor.EndHandleFrozen = anchor.EndHandle;
                    stateChange = true;
                }
                else if (anchor.State != ForceAnchor.DrawingState.Normal)
                {
                    stateChange = true;
                }
                RefreshUI();
                ForceRefresh();
            }
            else
            {
                var bars = BarsArray[__settings.BarsIndex];
                if (bars == null) return stateChange;

                if (bars.Count == 0) return stateChange;
                var lastBarTime = IsIntraday(bars) ? bars.GetTime(bars.Count - 1) : bars.GetSessionEndTime(bars.Count - 1);
                var selectedTime = snap ? chartControl.GetTimeBySlotIndex((int)chartControl.GetSlotIndexByX((int)point.X)) : chartControl.GetTimeBySlotIndex(chartControl.GetSlotIndexByX((int)point.X));
                if (selectedTime.CompareTo(lastBarTime) > 0) return false;

                var selectedPrice = snap ? __chartScale.GetValueByY((int)point.Y) : __chartScale.GetValueByY((float)point.Y);

                if (!anchor.IsSet)
                {
                    if (isPulse && __entitySignal.CalculationMethod == eForceLineCalcMethod.BarCount)
                    {
                        var startBarIndex = bars.GetBar(selectedTime);
                        if (startBarIndex < bars.Count - 1 - __entitySignal.BarCount)
                        {
                            var endTime = IsIntraday(bars) ? bars.GetTime(startBarIndex + __entitySignal.BarCount) : bars.GetSessionEndTime(startBarIndex + __entitySignal.BarCount);
                            anchor.Set(selectedTime, selectedPrice, endTime, selectedPrice);

                            anchor.State = ForceAnchor.DrawingState.Normal;
                            if (onCompleteDown != null)
                                onCompleteDown();
                            stateChange = false;
                        }
                    }
                    else
                    {
                        anchor.Set(selectedTime, selectedPrice, selectedTime, selectedPrice);
                    }
                }
                else
                {
                    if (anchor.State == ForceAnchor.DrawingState.Moving)
                    {

                    }
                    else if (anchor.State == ForceAnchor.DrawingState.EditingStart)
                    {
                        anchor.SetStart(selectedTime, selectedPrice);
                    }
                    else
                    {
                        anchor.SetEnd(selectedTime, selectedPrice);
                    }

                    anchor.State = ForceAnchor.DrawingState.Normal;
                    if (onCompleteDown != null)
                        onCompleteDown();
                    stateChange = true;
                }
                RefreshUI();
                ForceRefresh();
            }
            return stateChange;
        }

        private void Chart_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ChartControl == null) return;
            var bars = BarsArray[__settings.BarsIndex];
            if (bars == null) return;

            var x = ChartingExtensions.ConvertToHorizontalPixels(e.GetPosition(ChartPanel).X, ChartControl.PresentationSource);
            var y = ChartingExtensions.ConvertToVerticalPixels(e.GetPosition(ChartPanel).Y, ChartControl.PresentationSource);
            var point = new System.Windows.Point(x, y);

            if (__entityAngle != null)
            {
                if (Anchor_MouseDown(ChartControl, __entityAngle.Anchor, point, _snapToBarAngle, false,
                    () =>
                    {
                        __entityAngle.Anchor.Normalize();
                        var startBarIndex = bars.GetBar(__entityAngle.Anchor.StartTime);
                        var endBarIndex = bars.GetBar(__entityAngle.Anchor.EndTime);

                        __entityAngle.BarDelta = Math.Abs(startBarIndex - endBarIndex);
                        __entityAngle.TickDelta = (Math.Abs(__entityAngle.Anchor.EndPrice - __entityAngle.Anchor.StartPrice)) / TickSize;

                    }))
                    return;
            }

            if (__entitySignal != null)
            {
                Anchor_MouseDown(ChartControl, __entitySignal.Anchor, point, _snapToBarLine, true);
            }
        }

        private void Chart_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ChartControl == null) return;

            var x = ChartingExtensions.ConvertToHorizontalPixels(e.GetPosition(ChartPanel).X, ChartControl.PresentationSource);
            var y = ChartingExtensions.ConvertToVerticalPixels(e.GetPosition(ChartPanel).Y, ChartControl.PresentationSource);
        }

        private void Anchor_MouseMove(ChartControl chartControl, ForceAnchor anchor, Vector2 point, bool snap, bool isPulse, Action onCompleteMove = null)
        {
            if (anchor.State == ForceAnchor.DrawingState.Normal) return;
            if (!anchor.IsSet) return;
            var bars = BarsArray[__settings.BarsIndex];
            if (bars == null) return;
            if (bars.Count == 0) return;
            var lastBarTime = IsIntraday(bars) ? bars.GetTime(bars.Count - 1) : bars.GetSessionEndTime(bars.Count - 1);

            if (anchor.State == ForceAnchor.DrawingState.Moving)
            {
                var change = point - anchor.CenterHandleFrozen.Point;
                var start = anchor.StartHandleFrozen.Point + change;
                var end = anchor.EndHandleFrozen.Point + change;

                var selectedStart = snap ? chartControl.GetTimeBySlotIndex((int)chartControl.GetSlotIndexByX((int)start.X)) : chartControl.GetTimeBySlotIndex(chartControl.GetSlotIndexByX((int)start.X));
                if (selectedStart.CompareTo(lastBarTime) > 0) return;

                var selectedEnd = snap ? chartControl.GetTimeBySlotIndex((int)chartControl.GetSlotIndexByX((int)end.X)) : chartControl.GetTimeBySlotIndex(chartControl.GetSlotIndexByX((int)end.X));
                if (selectedEnd.CompareTo(lastBarTime) > 0) return;

                anchor.SetStart(selectedStart, snap ? __chartScale.GetValueByY((int)start.Y) : __chartScale.GetValueByY(start.Y));
                anchor.SetEnd(selectedEnd, snap ? __chartScale.GetValueByY((int)end.Y) : __chartScale.GetValueByY(end.Y));
            }
            else
            {
                var selectedTime = snap ? chartControl.GetTimeBySlotIndex((int)chartControl.GetSlotIndexByX((int)point.X)) : chartControl.GetTimeBySlotIndex(chartControl.GetSlotIndexByX((int)point.X));
                if (selectedTime.CompareTo(lastBarTime) > 0) return;

                var selectedPrice = snap ? __chartScale.GetValueByY((int)point.Y) : __chartScale.GetValueByY(point.Y);

                if (anchor.State == ForceAnchor.DrawingState.EditingStart)
                {
                    anchor.SetStart(selectedTime, selectedPrice);
                }
                else
                {
                    anchor.SetEnd(selectedTime, selectedPrice);
                }

                if (isPulse && __entitySignal.CalculationMethod == eForceLineCalcMethod.BarCount)
                {
                    var startBarIndex = bars.GetBar(__entitySignal.Anchor.StartTime);
                    var endBarIndex = bars.GetBar(__entitySignal.Anchor.EndTime);
                    var size = Math.Abs(startBarIndex - endBarIndex);
                    if (size != __entitySignal.BarCount)
                    {
                        __entitySignal.BarCount = size;
                        RefreshUI();
                    }
                }
            }
            if (onCompleteMove != null)
                onCompleteMove();
            ForceRefresh();
        }

        private void Chart_MouseMove(object sender, MouseEventArgs e)
        {
            if (ChartControl == null) return;
            if (__entitySignal.Anchor.State == ForceAnchor.DrawingState.Normal && __entityAngle.Anchor.State == ForceAnchor.DrawingState.Normal) return;
            var bars = BarsArray[__settings.BarsIndex];
            if (bars == null) return;

            var x = ChartingExtensions.ConvertToHorizontalPixels(e.GetPosition(ChartPanel).X, ChartControl.PresentationSource);
            var y = ChartingExtensions.ConvertToVerticalPixels(e.GetPosition(ChartPanel).Y, ChartControl.PresentationSource);
            var point = new Vector2(x, y);

            if (__entitySignal != null)
            {
                Anchor_MouseMove(ChartControl, __entitySignal.Anchor, point, _snapToBarLine, true);
            }

            if (__entityAngle != null)
            {
                Anchor_MouseMove(ChartControl, __entityAngle.Anchor, point, _snapToBarAngle, false,
                    () =>
                    {
                        var startBarIndex = bars.GetBar(__entityAngle.Anchor.StartTime);
                        var endBarIndex = bars.GetBar(__entityAngle.Anchor.EndTime);

                        __entityAngle.BarDelta = Math.Abs(startBarIndex - endBarIndex);
                        __entityAngle.TickDelta = (Math.Abs(__entityAngle.Anchor.EndPrice - __entityAngle.Anchor.StartPrice)) / TickSize;
                    });
            }
        }

        private void Anchor_PreviewKeyDown(ChartControl chartControl, ForceAnchor anchor, KeyEventArgs e)
        {
            if (!anchor.IsSet) return;
            if (anchor.State == ForceAnchor.DrawingState.Normal) return;
            Log(e.Key);
            if (!Regex.IsMatch(e.Key.ToString(), "^(Right|Left|Up|Down)$")) return;
            Log(e.Key);
            e.Handled = true;
        }

        private void Chart_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Anchor_PreviewKeyDown(ChartControl, __entitySignal.Anchor, e);
            Anchor_PreviewKeyDown(ChartControl, __entityAngle.Anchor, e);
        }

        private void Chart_MouseDownInfo(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (__processor == null) return;
                if (__processor.CurrentSignal == null) return;

                var x = ChartingExtensions.ConvertToHorizontalPixels(e.GetPosition(ChartPanel).X, ChartControl.PresentationSource);
                var selectedTime = ChartControl.GetTimeBySlotIndex(ChartControl.GetSlotIndexByX(x));

                __processor.SetInfoSignal(selectedTime);
                RefreshUI();
                ForceRefresh();
            }
        }

        #endregion Chart MouseControl / KeyControl

        #region class ForceSave

        /// <summary>
        /// class for temporary storage of Force data
        /// this keeps F5 refreshes and data-connection events from losing anchor data
        /// the Dictionary is static so it exists across instantiations - and across the workspace/NT session
        /// </summary>
        private class ForceSave
        {
            public string Pulse = "";
            public string Angle = "";
        }

        private static Dictionary<int, ForceSave> ForceSaves = new Dictionary<int, ForceSave>();

        #endregion class ForceSave

        #region NT8 On_Functions

        /// <summary>
        /// _canProcess is only used in OnStateChange
        /// if the charts's timeframe is not time based, it will exit out of certain sections thus disabling the indicator
        /// </summary>
        private bool __canProcess;

        /// <summary>
        /// _chartScale is used for chart coordinates, set in OnStateChange to make sure to have it
        /// </summary>
        private ChartScale __chartScale;

        protected override void OnStateChange()
        {
            try
            {
                if (State == State.SetDefaults)
                {
                    Description = "May the Force be with you.";
                    Name = "TachEonForce";
                    IsOverlay = true;
                    DrawOnPricePanel = true;
                    DrawHorizontalGridLines = false;
                    DrawVerticalGridLines = false;
                    PaintPriceMarkers = false;
                    ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                    //Disable this property if your indicator requires custom values that cumulate with each new market data event. 
                    //See Help Guide for additional information.
                    IsSuspendedWhileInactive = true;

                    Calculate = Calculate.OnEachTick;
                    MaximumBarsLookBack = MaximumBarsLookBack.Infinite;

                    ShowInDatabox = true;
                    DisplayInDataBox = ShowTransparentPlotsInDataBox = ShowInDatabox;

                    // UI Panel Defaults
                    _panelVisible = true;
                    _panelSize = 200;

                    // Force Method Defaults
                    _calcMethod = eForceLineCalcMethod.Fibs;
                    _defaultBarCount = 9;
                    _resetsToAnchorTime = false;

                    // SnapToBar is hidden from the user
                    // Left in the code for possible future changes
                    // drawing signals between bars doesn't make sense when time is the main calculation
                    _snapToBarLine = true;
                    _snapToBarAngle = true;

                    // Swing defaults for pattern computation
                    __settings.SetSwing(1, 5);
                    __settings.SetSwing(2, 5);
                    __settings.SetSwing(3, 5);

                    // Slope defaults (SMA/EMA, etc)
                    __settings.SlopeMethod = eTachEonMultiSlope.EMA;
                    __settings.SlopePeriod = 120;
                    SlopePlot = PlotStyle.Line;
                    SlopeWidth = 2;
                    SlopeLongColor = Brushes.Green;
                    SlopeShortColor = Brushes.Red;

                    #region Signal Defaults

                    ForceBuyStyle = SharpDX.Direct2D1.DashStyle.Solid;
                    ForceBuyWidth = 1;
                    ForceBuyColor = Brushes.Green;

                    ForceSellStyle = SharpDX.Direct2D1.DashStyle.Solid;
                    ForceSellWidth = 1;
                    ForceSellColor = Brushes.Red;

                    // "the Zone" is the Force Anchor placement
                    _zoneOpacity = 0.15f;

                    _renderLineDates = true;
                    _renderLineDatesFontSize = 12;

                    #endregion

                    #region Resonant Angle Defaults

                    // Draw angle from Bar Close (false) or Bar Open (true)
                    _angleStart = false;

                    // Anchor
                    ResAngleAnchorStyle = SharpDX.Direct2D1.DashStyle.Solid;
                    ResAngleAnchorWidth = 2;
                    ResAngleAnchorColor = Brushes.Aqua;

                    // Forward printing Angle
                    _angleRenderFoward = true;

                    ResAngleBuyStyle = SharpDX.Direct2D1.DashStyle.Dash;
                    ResAngleBuyWidth = 1;
                    ResAngleBuyColor = Brushes.Green;

                    ResAngleSellStyle = SharpDX.Direct2D1.DashStyle.Dash;
                    ResAngleSellWidth = 1;
                    ResAngleSellColor = Brushes.Red;

                    // Backward printing Angle
                    _angleRenderReverse = false;

                    ResAngleReverseBuyStyle = SharpDX.Direct2D1.DashStyle.Dash;
                    ResAngleReverseBuyWidth = 1;
                    ResAngleReverseBuyColor = Brushes.Red;

                    ResAngleReverseSellStyle = SharpDX.Direct2D1.DashStyle.Dash;
                    ResAngleReverseSellWidth = 1;
                    ResAngleReverseSellColor = Brushes.Green;

                    __angleExtension = new ForceExtensionLine()
                    {
                        Method = eTachEonExtensionMethods.Infinite,
                        Count = 5
                    };

                    #endregion

                    #region Dot Defaults

                    __settings.DotOffset = 3;
                    _dotWidth = 5;

                    __dotExtension = new ForceExtensionLine()
                    {
                        Method = eTachEonExtensionMethods.NumberOfSignals,
                        Count = 5
                    };

                    DotBuyStyle = SharpDX.Direct2D1.DashStyle.Dash;
                    DotBuyWidth = 1;
                    DotBuyColor = Brushes.Green;

                    DotSellStyle = SharpDX.Direct2D1.DashStyle.Dash;
                    DotSellWidth = 1;
                    DotSellColor = Brushes.Red;

                    #endregion

                    #region Open Price Line Defaults

                    __oplExtension = new ForceExtensionLine()
                    {
                        Method = eTachEonExtensionMethods.NumberOfSignals,
                        Count = 5
                    };

                    OplBuyStyle = SharpDX.Direct2D1.DashStyle.Dash;
                    OplBuyWidth = 1;
                    OplBuyColor = Brushes.Green;

                    OplSellStyle = SharpDX.Direct2D1.DashStyle.Dash;
                    OplSellWidth = 1;
                    OplSellColor = Brushes.Red;

                    #endregion

                    #region Pattern Defaults

                    _patternOffset = 2; // this setting is relative to _dotOffset
                    _patternDrawGhostDot = true;
                    _patternFontSize = 12;
                    _patternPrint12 = _patternPrint45 = _patternPrint67 = true;
                    _patternPrint = true;

                    PatternStyle = SharpDX.Direct2D1.DashStyle.Solid;
                    PatternWidth = 2;
                    PatternColor = Brushes.AntiqueWhite;

                    _patternGhostDotMethod = eTachEonGhostDotMethods.PrintDot;

                    #endregion

                    #region Score Defaults

                    _scoreRender = false;
                    __settings.IsTimeRestricted = false;
                    __settings.SetScoreStart("000000");
                    __settings.SetScoreEnd("000000");

                    __settings.AtrValue = 7;

                    for (var i = 0; i <= 3; i++)
                    {
                        var setting = __settings.ScoreSettings[i];
                        setting.Method = eTachEonScoreMethod.Dot;
                        setting.AtrValue = 7;
                        setting.AtrTarget = 3;
                        setting.AtrStop = 1;
                        setting.TickTarget = 10;
                        setting.TickStop = 5;
                        setting.DotOffset = __settings.DotOffset;
                    }

                    ScoreSuccessStyle = SharpDX.Direct2D1.DashStyle.Solid;
                    ScoreSuccessWidth = 2;
                    ScoreSuccessColor = Brushes.DodgerBlue;

                    ScoreFailureStyle = SharpDX.Direct2D1.DashStyle.Solid;
                    ScoreFailureWidth = 2;
                    ScoreFailureColor = Brushes.Orange;

                    #endregion

                    ExternalSignal = ExternalAngle = "";

                    _userMessagePlacement = eForceMessages.ChartCenter;

                    _futureSignals = 5;

                    _maxDraw = 100;

                    __onRender = __onBarUpdate = false;
                }

                else if (State == State.Configure)
                {
                    Calculate = Calculate.OnEachTick;
                    MaximumBarsLookBack = MaximumBarsLookBack.Infinite;
                    __canProcess = false;

                    // Values index 0
                    if (__settings.SlopeMethod == eTachEonMultiSlope.None)
                    {
                        AddPlot(Brushes.Transparent, "TrendPlot");
                    }
                    else
                    {
                        AddPlot(SlopeLongColor, "TrendPlot");
                    }
                    Plots[0].Width = SlopeWidth;

                    // Values index 1
                    AddPlot(Brushes.Transparent, "BarsToNextSignal");
                    // Values index 2
                    AddPlot(Brushes.Transparent, "BarsFromPreviousSignal");
                    // Values index 3
                    AddPlot(Brushes.Transparent, "SignalPattern");

                    //__checkedLicense = true;
                }

                else if (State == State.DataLoaded)
                {
                    //if (__debugging) ClearOutputWindow();
                }

                else if (State == State.Historical)
                {
                    Calculate = Calculate.OnEachTick;
                    var bars = BarsArray[__settings.BarsIndex];
                    if (bars == null) return;

                    __log = new ForceLog(string.Format("{0}_{1}_{2}", Instrument.FullName, bars.BarsPeriod.BarsPeriodType, bars.BarsPeriod.Value), __internalId);

                    switch (bars.BarsPeriod.BarsPeriodType)
                    {
                        case BarsPeriodType.Second:
                            break;
                        case BarsPeriodType.Minute:
                            break;
                        case BarsPeriodType.Day:
                            break;
                        case BarsPeriodType.Week:
                            break;
                        case BarsPeriodType.Month:
                            break;
                        case BarsPeriodType.Year:
                            break;
                        default:
                            UserMessage(string.Format("Cannot calculate PeriodType: {0}", bars.BarsPeriod.BarsPeriodType));
                            return;
                    }

                    __canProcess = true;

                    if (ForceEntitySignal.IsValidSerialString(ExternalSignal))
                    {
                        var entity = new ForceEntitySignal();
                        entity.FromSerialString(ExternalSignal);
                        if (entity.From.Instrument.Equals(Instrument.FullName) && entity.IsCompatible(this))
                        {
                            __entitySignal = new ForceEntitySignal(Instrument.FullName, bars.BarsPeriod.BarsPeriodType, bars.BarsPeriod.Value);
                            __entitySignal.BarCount = entity.BarCount;
                            __entitySignal.CalculationMethod = entity.CalculationMethod;
                            __entitySignal.Polarity = entity.Polarity;
                            __entitySignal.ResetsToAnchorTime = entity.ResetsToAnchorTime;
                            if (entity.Anchor.IsSet)
                            {
                                __entitySignal.Anchor.Set(entity.Anchor.StartTime, entity.Anchor.StartPrice, entity.Anchor.EndTime, entity.Anchor.EndPrice);
                            }
                        }
                    }

                    if (__entitySignal == null)
                    {
                        __entitySignal = new ForceEntitySignal(Instrument.FullName, bars.BarsPeriod.BarsPeriodType, bars.BarsPeriod.Value)
                        {
                            CalculationMethod = _calcMethod,
                            BarCount = _defaultBarCount,
                            ResetsToAnchorTime = _resetsToAnchorTime
                        };
                    }
                    else
                    {
                        __entitySignal.Set(Instrument.FullName, bars.BarsPeriod.BarsPeriodType, bars.BarsPeriod.Value);
                    }

                    if (ForceEntityAngle.IsValidSerialString(ExternalAngle))
                    {
                        var entity = new ForceEntityAngle();
                        entity.FromSerialString(ExternalAngle);
                        if (entity.From.Instrument.Equals(Instrument.FullName) && entity.IsCompatible(this))
                        {
                            __entityAngle = new ForceEntityAngle(Instrument.FullName, bars.BarsPeriod.BarsPeriodType, bars.BarsPeriod.Value);
                            __entityAngle.BarDelta = entity.BarDelta;
                            __entityAngle.TickDelta = entity.TickDelta;
                            if (entity.Anchor.IsSet)
                            {
                                __entityAngle.Anchor.Set(entity.Anchor.StartTime, entity.Anchor.StartPrice, entity.Anchor.EndTime, entity.Anchor.EndPrice);
                            }
                        }
                    }

                    if (__entityAngle == null)
                    {
                        __entityAngle = new ForceEntityAngle(Instrument.FullName, bars.BarsPeriod.BarsPeriodType, bars.BarsPeriod.Value);
                    }
                    else
                    {
                        __entityAngle.Set(Instrument.FullName, bars.BarsPeriod.BarsPeriodType, bars.BarsPeriod.Value);
                    }

                    if (ForceSaves != null)
                    {
                        if (ForceSaves.ContainsKey(IndicatorId))
                        {
                            var entityStr = ForceSaves[IndicatorId].Pulse;
                            if (entityStr != null)
                            {
                                if (entityStr.Length > 0)
                                {
                                    var entity = new ForceEntitySignal();
                                    entity.FromSerialString(entityStr);
                                    if (entity.From.Instrument.Equals(Instrument.FullName) && entity.IsCompatible(this))
                                    {
                                        __entitySignal.BarCount = entity.BarCount;
                                        __entitySignal.CalculationMethod = entity.CalculationMethod;
                                        __entitySignal.Polarity = entity.Polarity;
                                        __entitySignal.ResetsToAnchorTime = entity.ResetsToAnchorTime;
                                        if (entity.Anchor.IsSet)
                                        {
                                            __entitySignal.Anchor.Set(entity.Anchor.StartTime, entity.Anchor.StartPrice, entity.Anchor.EndTime, entity.Anchor.EndPrice);
                                        }
                                    }
                                }
                            }

                            entityStr = ForceSaves[IndicatorId].Angle;
                            if (entityStr != null)
                            {
                                if (entityStr.Length > 0)
                                {
                                    var entity = new ForceEntityAngle();
                                    entity.FromSerialString(entityStr);
                                    if (entity.From.Instrument.Equals(Instrument.FullName) && entity.IsCompatible(this))
                                    {
                                        __entityAngle.BarDelta = entity.BarDelta;
                                        __entityAngle.TickDelta = entity.TickDelta;
                                        if (entity.Anchor.IsSet)
                                        {
                                            __entityAngle.Anchor.Set(entity.Anchor.StartTime, entity.Anchor.StartPrice, entity.Anchor.EndTime, entity.Anchor.EndPrice);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (ChartControl == null) return;

                    __drawTools.AddBrush(NamingChart + "Background", ChartControl.Properties.ChartBackground, SharpDX.Direct2D1.DashStyle.Solid, 1);

                    __brushes.Add(NamingForceLine + NamingBuy, new DtwSolidColorBrushDX(ForceBuyColor, ForceBuyStyle, ForceBuyWidth));
                    __brushes.Add(NamingResAngle + NamingBuy, new DtwSolidColorBrushDX(ResAngleBuyColor, ResAngleBuyStyle, ResAngleBuyWidth));
                    __brushes.Add(NamingResAngleReverse + NamingBuy, new DtwSolidColorBrushDX(ResAngleReverseBuyColor, ResAngleReverseBuyStyle, ResAngleReverseBuyWidth));

                    __brushes.Add(NamingForceLine + NamingSell, new DtwSolidColorBrushDX(ForceSellColor, ForceSellStyle, ForceSellWidth));
                    __brushes.Add(NamingResAngle + NamingSell, new DtwSolidColorBrushDX(ResAngleSellColor, ResAngleSellStyle, ResAngleSellWidth));
                    __brushes.Add(NamingResAngleReverse + NamingSell, new DtwSolidColorBrushDX(ResAngleReverseSellColor, ResAngleReverseSellStyle, ResAngleReverseSellWidth));

                    __brushes.Add(NamingResAngle, new DtwSolidColorBrushDX(ResAngleAnchorColor, ResAngleAnchorStyle, ResAngleAnchorWidth));

                    __brushes.Add(NamingDot + NamingBuy, new DtwSolidColorBrushDX(DotBuyColor, DotBuyStyle, DotBuyWidth));
                    __brushes.Add(NamingDot + NamingSell, new DtwSolidColorBrushDX(DotSellColor, DotSellStyle, DotSellWidth));

                    __brushes.Add(NamingOpl + NamingBuy, new DtwSolidColorBrushDX(OplBuyColor, OplBuyStyle, OplBuyWidth));
                    __brushes.Add(NamingOpl + NamingSell, new DtwSolidColorBrushDX(OplSellColor, OplSellStyle, OplSellWidth));

                    __brushes.Add(NamingScoreSuccess, new DtwSolidColorBrushDX(ScoreSuccessColor, ScoreSuccessStyle, ScoreSuccessWidth));
                    __brushes.Add(NamingScoreFailure, new DtwSolidColorBrushDX(ScoreFailureColor, ScoreFailureStyle, ScoreFailureWidth));

                    __brushes.Add(NamingHandle, new DtwSolidColorBrushDX(ChartControl.Properties.ChartText, SharpDX.Direct2D1.DashStyle.Solid, 0.5f));
                    __brushes.Add(NamingPattern, new DtwSolidColorBrushDX(PatternColor, PatternStyle, PatternWidth));

                    foreach (var scale in ChartPanel.Scales)
                    {
                        if (scale.ScaleJustification != ScaleJustification) continue;
                        __chartScale = scale;
                    }

                    var fontFace = "Arial";
                    if (ChartControl.Properties != null)
                    {
                        if (ChartControl.Properties.LabelFont != null)
                        {
                            if (ChartControl.Properties.LabelFont.Family != null)
                            {
                                fontFace = ChartControl.Properties.LabelFont.Family.ToString();
                            }
                        }
                    }
                    __drawTools.AddTextFormat(NamingForceLine, fontFace, _renderLineDatesFontSize);
                    __drawTools.AddTextFormat(NamingPattern, fontFace, _patternFontSize);

                    if (Dispatcher.CheckAccess())
                    {
                        CreateUI(ChartControl);
                    }
                    else
                    {
                        Dispatcher.InvokeAsync(() =>
                        {
                            CreateUI(ChartControl);
                        });
                    }
                }

                else if (State == State.Transition)
                {
                    if (!__canProcess) return;

                    // when in State.Historical, the patterns don't print on loading after F5
                    __processor = new ForceProcessor(this, __settings, __entitySignal, _futureSignals);

                    __onRender = __onBarUpdate = true;
                }

                else if (State == State.Realtime)
                {
                    if (!__canProcess) return;
                }

                else if (State == State.Terminated)
                {
                    __onRender = __onBarUpdate = false;

                    if (ChartControl == null) return;

                    if (!__canProcess) return;

                    if (Dispatcher.CheckAccess())
                    {
                        DisposeUI(ChartControl);
                    }
                    else
                    {
                        Dispatcher.InvokeAsync(() =>
                        {
                            DisposeUI(ChartControl);
                        });
                    }

                    if (ForceSaves != null)
                    {
                        if (!ForceSaves.ContainsKey(IndicatorId))
                        {
                            ForceSaves.Add(IndicatorId, new ForceSave());
                        }
                        var save = ForceSaves[IndicatorId];
                        if (__entitySignal != null)
                        {
                            save.Pulse = __entitySignal.Anchor.IsSet ? __entitySignal.ToSerialString() : "";
                        }
                        if (__entityAngle != null)
                        {
                            save.Angle = __entityAngle.Anchor.IsSet ? __entityAngle.ToSerialString() : "";
                        }
                    }
                }

            }
            catch (Exception err)
            {
                Log(string.Format("Error in State: {0}", State));
                Log(err);
                throw new ForceException(string.Format("Error in State: {0}", State));
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar == 0)
            {
                var atr = ATR(10);
                var swing = Swing(10).SwingHigh[0];
            }
            if (BarsInProgress == __settings.BarsIndex)
            {
                if (IsFirstTickOfBar)
                {
                    #region slope

                    double val = 0;

                    switch (__settings.SlopeMethod)
                    {
                        case eTachEonMultiSlope.SMA:
                            val = SMA(__settings.SlopePeriod)[0];
                            break;
                        case eTachEonMultiSlope.EMA:
                            val = EMA(__settings.SlopePeriod)[0];
                            break;
                        case eTachEonMultiSlope.HMA:
                            val = HMA(__settings.SlopePeriod)[0];
                            break;
                        case eTachEonMultiSlope.WMA:
                            val = WMA(__settings.SlopePeriod)[0];
                            break;
                    }

                    Values[__settings.BarsIndex][__settings.TrendValuesIndex] = val;
                    if (CurrentBar > 0 && __settings.SlopeMethod != eTachEonMultiSlope.None)
                    {
                        var isUp = Values[__settings.BarsIndex][0] > Values[__settings.BarsIndex][1];
                        PlotBrushes[__settings.BarsIndex][0] = isUp ? SlopeLongColor : SlopeShortColor;
                    }

                    #endregion
                }

                if (!__onBarUpdate) return;
                if (__processor == null) return;
                __processor.OnBarUpdate();
            }
        }

        #endregion NT8 On_Functions

        #region onrender

        #region drawing Tools

        private readonly DtwDrawTools __drawTools = new DtwDrawTools();

        private readonly Dictionary<string, DtwSolidColorBrushDX> __brushes = new Dictionary<string, DtwSolidColorBrushDX>();
        //private readonly Dictionary<string, DtwTextDX> __textFormats = new Dictionary<string, DtwTextDX>();

        private int __handleRadius = 5;
        private int __dotRadius = 5;
        private const float __textRotation = (float)(Math.PI / 180 * -90);

        private void DrawingToolsCreate(RenderTarget renderTarget)
        {
            try
            {
                //foreach (var brush in __brushes.Values.ToList())
                //{
                //    brush.Refresh(renderTarget);
                //}
                //foreach (var text in __textFormats.Values.ToList())
                //{
                //    text.Refresh();
                //}

                __handleRadius = ChartControl == null ? 5 : ChartingExtensions.ConvertToVerticalPixels(5, ChartControl.PresentationSource);
                __dotRadius = ChartControl == null ? 5 : ChartingExtensions.ConvertToVerticalPixels(_dotWidth, ChartControl.PresentationSource);
            }
            catch (Exception err)
            {
                Log(string.Format("DrawingToolsCreate: {0}", err.Message));
                //throw new ForceException(string.Format("DrawingToolsCreate: {0}", err.Message));
            }
        }

        private void DrawingToolsDispose()
        {
            try
            {
                __drawTools.Dispose();
                foreach (var brush in __brushes.Values.ToList())
                {
                    brush.Dispose();
                }
                //foreach (var text in __textFormats.Values.ToList())
                //{
                //    text.Dispose();
                //}
            }
            catch (Exception err)
            {
                Log(string.Format("DrawingToolsDispose: {0}", err.Message));
                //throw new ForceException(string.Format("DrawingToolsDispose: {0}", err.Message));
            }
        }

        #endregion

        #region render basics

        /// <summary>
        /// Arrow head drawing
        /// </summary>
        /// <param name="renderTarget"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="brush"></param>
        private void DrawArrowHead(RenderTarget renderTarget, Vector2 start, Vector2 end, DtwSolidColorBrushDX brush)
        {
            var diffX = end.X - start.X;
            var diffY = end.Y - start.Y;
            var length = Math.Sqrt(Math.Pow(diffX, 2) + Math.Pow(diffY, 2));

            var nX = diffX / length;
            var nY = diffY / length;

            var size = brush.Width * 2;
            var aX = size * (-nY - nX);
            var aY = size * (nX - nY);

            var pathGeom = new SharpDX.Direct2D1.PathGeometry(NinjaTrader.Core.Globals.D2DFactory);
            var geomSink = pathGeom.Open();
            geomSink.BeginFigure(end, FigureBegin.Filled);
            geomSink.AddLine(new Vector2((int)(end.X + aX), (int)(end.Y + aY)));
            geomSink.AddLine(new Vector2((int)(end.X - aY), (int)(end.Y + aX)));
            geomSink.EndFigure(FigureEnd.Closed);
            geomSink.Close();
            renderTarget.FillGeometry(pathGeom, brush.GetBrush(renderTarget));
            geomSink.Dispose();
            pathGeom.Dispose();
        }

        /// <summary>
        /// Draws Anchor handles (start/center/end)
        /// these are the anchor elements for user editing
        /// </summary>
        /// <param name="renderTarget"></param>
        /// <param name="anchor"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private void DrawAnchorHandle(RenderTarget renderTarget, ForceAnchor anchor, Vector2 start, Vector2 end)
        {
            var brush = __brushes[NamingHandle];
            var background = __drawTools.GetBrush(NamingChart + "Background");

            renderTarget.DrawLine(start, end, brush.GetBrush(renderTarget), brush.Width, brush.StrokeStyle);

            var center = (start + end) / 2;
            var handleCenter = new SharpDX.Direct2D1.Ellipse(center, __handleRadius, __handleRadius);
            anchor.CenterHandle = handleCenter;
            renderTarget.FillEllipse(handleCenter, anchor.State == ForceAnchor.DrawingState.Moving ? brush.GetBrush(renderTarget) : background.GetBrush(renderTarget));
            renderTarget.DrawEllipse(handleCenter, brush.GetBrush(renderTarget), brush.Width * 2);

            var handleStart = new SharpDX.Direct2D1.Ellipse(start, __handleRadius, __handleRadius);
            anchor.StartHandle = handleStart;
            renderTarget.FillEllipse(handleStart, anchor.State == ForceAnchor.DrawingState.EditingStart ? brush.GetBrush(renderTarget) : background.GetBrush(renderTarget));
            renderTarget.DrawEllipse(handleStart, brush.GetBrush(renderTarget), brush.Width * 2);

            var handleEnd = new SharpDX.Direct2D1.Ellipse(end, __handleRadius, __handleRadius);
            anchor.EndHandle = handleEnd;
            renderTarget.FillEllipse(handleEnd, (anchor.State == ForceAnchor.DrawingState.Building || anchor.State == ForceAnchor.DrawingState.EditingEnd) ? brush.GetBrush(renderTarget) : background.GetBrush(renderTarget));
            renderTarget.DrawEllipse(handleEnd, brush.GetBrush(renderTarget), brush.Width * 2);

            anchor.IsDrawn = true;
        }

        /// <summary>
        /// Draws Force Signal with optional datetime
        /// </summary>
        /// <param name="renderTarget"></param>
        /// <param name="chartControl"></param>
        /// <param name="chartPanel"></param>
        /// <param name="x"></param>
        /// <param name="time"></param>
        /// <param name="brush"></param>
        /// <param name="isFuture"></param>
        /// <param name="emphasize"></param>
        /// 
        private void DrawForceSignal(RenderTarget renderTarget, ChartControl chartControl, ChartPanel chartPanel, float x, DateTime time, DtwSolidColorBrushDX brush, bool isFuture = false, int emphasize = 0)
        {
            var lineStart = new Vector2(x, chartPanel.Y);
            var lineEnd = new Vector2(x, chartPanel.Y + chartPanel.H);
            renderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(renderTarget), brush.Width + emphasize, brush.StrokeStyle);

            if (_renderLineDates)
            {
                var currentTransform = renderTarget.Transform;
                var rect = new RectangleF(x + 2, chartPanel.Y + chartPanel.H - 25, 1500, 3);
                renderTarget.Transform = Matrix3x2.Rotation(__textRotation, new Vector2(rect.X, rect.Y));

                var text = string.Format("{0} {1}", time.ToString(CultureInfo.CurrentCulture), isFuture ? "~" : "");// (isFuture ? "~ " : "") + time.ToString(CultureInfo.CurrentCulture);
                renderTarget.DrawText(text, __drawTools.GetTextFormat(NamingForceLine).TextFormat, rect, brush.GetBrush(renderTarget));

                renderTarget.Transform = currentTransform;
            }
        }

        #endregion

        #region pattern render

        private void RenderPattern(RenderTarget renderTarget, ChartControl chartControl, ChartScale chartScale, ChartPanel chartPanel,
            Signal signal, int pulseDivision,
            int barX, float chartX, int lastX, int distanceX,
            double dotDistance, double patternDistance,
            int scoreWidth, DtwSolidColorBrushDX dotBrushBuy, DtwSolidColorBrushDX dotBrushSell)
        {
            var patternbrush = __brushes[NamingPattern];
            var backgroundbrush = __drawTools.GetBrush(NamingChart + "Background");
            var drawGhostDot = false;

            // for score rendering
            Pattern patternScore = null;
            // since scores can be turned off, need a way to know if this is just a default pattern 1/2
            //bool isDefaultPattern = true;
            bool scoreRender = false;

            IndicatorSettings.ScoreSetting scoreSetting = __settings.ScoreSettings[0];

            if (_patternPrintNeutral)
            {
                scoreSetting = __settings.ScoreSettings[0];

                //if at GREEN FORCE LINE
                if (signal.IsBuySignal(__entitySignal.IsReversePolarity))
                {
                    scoreRender = _scoreRender;
                    patternScore = signal.BuyPatternNeutral;
                }
                //else at RED FORCE LINE
                else
                {
                    scoreRender = _scoreRender;
                    patternScore = signal.SellPatternNeutral;
                }
            }
            else
            {
                //if at GREEN FORCE LINE
                if (signal.IsBuySignal(__entitySignal.IsReversePolarity))
                {
                    patternScore = signal.BuyPattern;//(__entitySignal.IsReversePolarity);

                    if (patternScore != null)
                    {
                        scoreSetting = __settings.ScoreSettings[patternScore.GetPatternType()];

                        switch (patternScore.GetPatternType())
                        {
                            case 1:
                                if (_patternPrint12)
                                {
                                    DrawPatternText(renderTarget, chartControl, chartScale, barX, signal.Low, patternDistance, "1", patternbrush);
                                    scoreRender = _scoreRender;
                                }
                                break;
                            case 2: // patttern 5
                                drawGhostDot = true;
                                if (_patternPrint45)
                                {
                                    var pattern = patternScore as PatternTwo;

                                    //if the signal.hasData then use the signal, otherwise the pattern value (for future signal)
                                    DrawPatternText(renderTarget, chartControl, chartScale, barX,
                                        signal.HasData ? signal.High : pattern.GetValue(0),
                                        -patternDistance, "5", patternbrush);
                                    DrawPattern45(renderTarget, chartControl, chartScale, pattern, patternbrush);

                                    scoreRender = _scoreRender;
                                }
                                break;
                            case 3: // pattern 7
                                drawGhostDot = true;
                                if (_patternPrint67)
                                {
                                    var pattern = patternScore as PatternThree;

                                    DrawPatternText(renderTarget, chartControl, chartScale, barX, signal.High, -patternDistance, "7", patternbrush);
                                    DrawPattern67(renderTarget, chartControl, chartScale, signal, barX, pattern, patternbrush);

                                    scoreRender = _scoreRender;
                                }
                                break;
                        }
                    }
                }
                //else at RED FORCE LINE
                else
                {
                    patternScore = signal.SellPattern;//(__entitySignal.IsReversePolarity);

                    if (patternScore != null)
                    {
                        scoreSetting = __settings.ScoreSettings[patternScore.GetPatternType()];

                        switch (patternScore.GetPatternType())
                        {
                            case 1:
                                if (_patternPrint12)//if (line.SellPatternOld != null)// && line.SellPatternOld.GetPatternType() == 1)
                                {
                                    DrawPatternText(renderTarget, chartControl, chartScale, barX, signal.High, -patternDistance, "2", patternbrush);
                                    scoreRender = _scoreRender;
                                }
                                break;
                            case 2: // pattern 4
                                drawGhostDot = true;
                                if (_patternPrint45)
                                {
                                    var pattern = patternScore as PatternTwo;

                                    //if the signal.hasData then use the signal, otherwise the pattern value (for future signal)
                                    DrawPatternText(renderTarget, chartControl, chartScale, barX,
                                        signal.HasData ? signal.Low : pattern.GetValue(0),
                                        patternDistance, "4", patternbrush);
                                    DrawPattern45(renderTarget, chartControl, chartScale, pattern, patternbrush);

                                    scoreRender = _scoreRender;
                                }
                                break;
                            case 3: // pattern 6
                                drawGhostDot = true;
                                if (_patternPrint67)
                                {
                                    var pattern = patternScore as PatternThree;

                                    DrawPatternText(renderTarget, chartControl, chartScale, barX, signal.Low, patternDistance, "6", patternbrush);
                                    DrawPattern67(renderTarget, chartControl, chartScale, signal, barX, pattern, patternbrush);

                                    scoreRender = _scoreRender;
                                }
                                break;
                        }
                    }
                }
            }

            if (scoreRender && patternScore != null)
            {
                DrawScore(renderTarget, chartControl, chartScale, signal, patternScore, barX, (int)chartX, lastX, scoreWidth, __settings, scoreSetting);
            }

            if (_patternGhostDotMethod != eTachEonGhostDotMethods.None && __dotExtension.Method != eTachEonExtensionMethods.None && drawGhostDot)
            {
                int y;
                DtwSolidColorBrushDX brush;
                if (signal.IsEven)
                {
                    y = __entitySignal.IsReversePolarity ? chartScale.GetYByValue(signal.Low - dotDistance) : chartScale.GetYByValue(signal.High + dotDistance);
                    brush = dotBrushSell;
                }
                else
                {
                    y = __entitySignal.IsReversePolarity ? chartScale.GetYByValue(signal.High + dotDistance) : chartScale.GetYByValue(signal.Low - dotDistance);
                    brush = dotBrushBuy;
                }

                if (y >= 0 && y <= chartPanel.H)
                {
                    var lineStart = new Vector2(barX, y);

                    var dot = new SharpDX.Direct2D1.Ellipse(lineStart, __dotRadius, __dotRadius);
                    renderTarget.FillEllipse(dot, brush.GetBrush(renderTarget));
                    var tmpRadius = Math.Max(Math.Min(__dotRadius - 2, __dotRadius), 1);
                    dot = new SharpDX.Direct2D1.Ellipse(lineStart, tmpRadius, tmpRadius);
                    renderTarget.FillEllipse(dot, backgroundbrush.GetBrush(renderTarget));

                    if (_patternGhostDotMethod == eTachEonGhostDotMethods.PrintDotAndPriceLine && __dotExtension.Count > 0)
                    {
                        var myX = GetExtensionX(chartControl, chartPanel, signal, pulseDivision, distanceX, __dotExtension.Method, __dotExtension.Count);
                        if (myX > 0)
                        {
                            var lineEnd = new Vector2(myX, y);
                            renderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(renderTarget), brush.Width, brush.StrokeStyle);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draws pattern text
        /// </summary>
        /// <param name="renderTarget"></param>
        /// <param name="chartControl"></param>
        /// <param name="chartScale"></param>
        /// <param name="timeX"></param>
        /// <param name="priceY"></param>
        /// <param name="distance"></param>
        /// <param name="txt"></param>
        /// <param name="brush"></param>
        private void DrawPatternText(RenderTarget renderTarget, ChartControl chartControl, ChartScale chartScale, int timeX, double priceY, double distance, string txt, DtwSolidColorBrushDX brush)
        {
            var barSize = chartControl.Properties.BarDistance;
            var width = barSize * 2;
            var height = 20;
            var y = chartScale.GetYByValue(priceY - distance);

            var format = __drawTools.GetTextFormat(NamingPattern).TextFormat;
            format.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
            if (distance < 0)
            {
                y -= height;
                format.ParagraphAlignment = ParagraphAlignment.Far;
            }
            else
                format.ParagraphAlignment = ParagraphAlignment.Near;

            var rect = new SharpDX.Rectangle((int)(timeX - width / 2), (int)(y), (int)width, height);
            renderTarget.DrawText(txt, format, rect, brush.GetBrush(renderTarget));
        }

        /// <summary>
        /// Draws lines to indicate the prices that form pattern 4/5
        /// </summary>
        /// <param name="renderTarget"></param>
        /// <param name="chartControl"></param>
        /// <param name="chartScale"></param>
        /// <param name="pattern"></param>
        /// <param name="brush"></param>
        private void DrawPattern45(RenderTarget renderTarget, ChartControl chartControl, ChartScale chartScale, PatternTwo pattern, DtwSolidColorBrushDX brush)
        {
            for (var i = 0; i < 3; i++)
            {
                if (pattern.IsSet(i))
                {
                    var startX = chartControl.GetXByTime(pattern.GetTime(i));
                    var endX = chartControl.GetXByTime(pattern.GetTargetTime(i));
                    var y = chartScale.GetYByValue(pattern.GetValue(i));
                    var lineStart = new Vector2(startX, y);
                    var lineEnd = new Vector2(endX, y);
                    renderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(renderTarget), brush.Width, brush.StrokeStyle);
                    DrawArrowHead(renderTarget, lineStart, lineEnd, brush);
                }
            }
        }

        /// <summary>
        /// Draws lines to indicate the prices that form pattern 6/7
        /// </summary>
        /// <param name="renderTarget"></param>
        /// <param name="chartControl"></param>
        /// <param name="chartScale"></param>
        /// <param name="signal"></param>
        /// <param name="timeX"></param>
        /// <param name="pattern"></param>
        /// <param name="brush"></param>
        private void DrawPattern67(RenderTarget renderTarget, ChartControl chartControl, ChartScale chartScale, Signal signal, int timeX, PatternThree pattern, DtwSolidColorBrushDX brush)
        {
            var endX = chartControl.GetXByTime(pattern.BarTime);
            //high
            {
                var lineStart = new Vector2(timeX, chartScale.GetYByValue(signal.High));
                var lineEnd = new Vector2(endX, chartScale.GetYByValue(pattern.High));
                renderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(renderTarget), brush.Width, brush.StrokeStyle);
                DrawArrowHead(renderTarget, lineStart, lineEnd, brush);
            }
            //low
            {
                var lineStart = new Vector2(timeX, chartScale.GetYByValue(signal.Low));
                var lineEnd = new Vector2(endX, chartScale.GetYByValue(pattern.Low));
                renderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(renderTarget), brush.Width, brush.StrokeStyle);
                DrawArrowHead(renderTarget, lineStart, lineEnd, brush);
            }
        }

        #endregion

        #region score render

        /// <summary>
        /// Draws box at the top to indicate success/failure
        /// Draws lines for price action that creates the score
        /// </summary>
        /// <param name="renderTarget"></param>
        /// <param name="chartControl"></param>
        /// <param name="chartScale"></param>
        /// <param name="line"></param>
        /// <param name="pattern"></param>
        /// <param name="timeX"></param>
        /// <param name="lineX"></param>
        /// <param name="endX"></param>
        /// <param name="offsetWidth"></param>
        /// <param name="fScore"></param>
        private void DrawScore(RenderTarget renderTarget, ChartControl chartControl, ChartScale chartScale, Signal line, Pattern pattern, int timeX, int lineX, int endX, int offsetWidth, IndicatorSettings fScore, IndicatorSettings.ScoreSetting scoreSetting)
        {
            if (fScore.IsTimeRestricted && !line.IsWithinScoreTime) return;
            if (scoreSetting.Method == eTachEonScoreMethod.Off) return;

            var brush = __brushes[NamingScoreSuccess];

            var rect = new SharpDX.Rectangle(lineX + offsetWidth, 0, endX - lineX - (offsetWidth * 2), 10);

            var draw = false;

            if (scoreSetting.Method == eTachEonScoreMethod.Dot)
            {
                if (pattern.ScoreDot != null && pattern.ScoreDot.IsSet())
                {
                    brush = __brushes[NamingScoreFailure];
                    var targetX = chartControl.GetXByTime(pattern.ScoreDot.TimeDot);
                    var targetY = chartScale.GetYByValue(pattern.ScoreDot.Dot);
                    var lineStart = new Vector2(timeX, targetY);
                    var lineEnd = new Vector2(targetX, targetY);
                    renderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(renderTarget), brush.Width, brush.StrokeStyle);
                    DrawArrowHead(renderTarget, lineStart, lineEnd, brush);
                }
                draw = true;
            }
            else if (scoreSetting.Method == eTachEonScoreMethod.ATR || scoreSetting.Method == eTachEonScoreMethod.Tick)
            {
                ScoreTargetStop score;
                switch (scoreSetting.Method)
                {
                    case eTachEonScoreMethod.ATR:
                        score = pattern.ScoreAtr;
                        break;
                    default://defaults to Tick
                        score = pattern.ScoreTick;
                        break;
                }
                if (score != null)
                {
                    if (score.IsSet())
                    {
                        int targetX, targetY;
                        if (score.IsSuccess())
                        {
                            targetX = chartControl.GetXByTime(score.TimeTarget);
                            targetY = chartScale.GetYByValue(score.Target);
                        }
                        else
                        {
                            targetX = chartControl.GetXByTime(score.TimeStop);
                            targetY = chartScale.GetYByValue(score.Stop);
                            brush = __brushes[NamingScoreFailure];
                        }
                        var lineStart = new Vector2(timeX, chartScale.GetYByValue(line.Close));
                        var lineEnd = new Vector2(targetX, targetY);
                        renderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(renderTarget), brush.Width, brush.StrokeStyle);
                        DrawArrowHead(renderTarget, lineStart, lineEnd, brush);
                    }
                    else if (__debugging)
                    {
                        var chartPanel = chartControl.ChartPanels[0];
                        int targetX, targetY;
                        {
                            targetX = timeX + (int)chartControl.Properties.BarDistance;
                            targetY = chartScale.GetYByValue(score.Target);
                            var lineStart = new Vector2(timeX, chartScale.GetYByValue(line.Close));
                            var lineEnd = new Vector2(targetX, targetY);
                            renderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(renderTarget), 1, brush.StrokeStyle);
                            renderTarget.DrawLine(lineEnd, new Vector2(chartPanel.W, targetY), brush.GetBrush(renderTarget), 1, brush.StrokeStyle);
                            //DrawArrowHead(renderTarget, lineStart, lineEnd, brush);
                        }
                        {
                            targetX = timeX + (int)chartControl.Properties.BarDistance;
                            targetY = chartScale.GetYByValue(score.Stop);
                            brush = __brushes[NamingScoreFailure];
                            var lineStart = new Vector2(timeX, chartScale.GetYByValue(line.Close));
                            var lineEnd = new Vector2(targetX, targetY);
                            renderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(renderTarget), 1, brush.StrokeStyle);
                            renderTarget.DrawLine(lineEnd, new Vector2(chartPanel.W, targetY), brush.GetBrush(renderTarget), 1, brush.StrokeStyle);
                            //DrawArrowHead(renderTarget, lineStart, lineEnd, brush);
                        }
                    }
                    draw = score.IsSet();
                }
            }
            if (draw)
                renderTarget.FillRectangle(rect, brush.GetBrush(renderTarget));
        }

        #endregion

        #region render functions

        /// <summary>
        /// Centralized function to compute the "endX" of Dot or OPL
        /// </summary>
        /// <param name="chartControl"></param>
        /// <param name="chartPanel"></param>
        /// <param name="currentLink"></param>
        /// <param name="pulseDivision"></param>
        /// <param name="distanceX"></param>
        /// <param name="method"></param>
        /// <param name="methodCount"></param>
        /// <returns></returns>
        private int GetExtensionX(ChartControl chartControl, ChartPanel chartPanel, Signal currentLink, int pulseDivision, int distanceX, eTachEonExtensionMethods method, int methodCount)
        {
            if (method == eTachEonExtensionMethods.NumberOfSignals)
            {
                var next = currentLink;
                while (methodCount-- > 0)
                {
                    if (next.Next == null) break;
                    next = next.Next;
                }
                var nextLine = next;
                if (nextLine.IsPrimary)
                    return chartControl.GetXByTime(nextLine.BarTime);
                else
                {
                    var timeX = chartControl.GetXByTime(nextLine.Parent.BarTime);
                    var count = 0;
                    while (!next.IsPrimary)
                    {
                        count++;
                        next = next.Previous;
                    }
                    return (int)(timeX + (distanceX * ((float)count / pulseDivision)));
                }
            }
            return chartPanel.W;
        }

        #endregion

        private double __avgOnRender = 0.0;

        public override void OnRenderTargetChanged()
        {
            base.OnRenderTargetChanged();

            DrawingToolsDispose();

            if (!__onRender) return;

            if (RenderTarget == null) return;

            DrawingToolsCreate(RenderTarget);
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (!__onRender) return;

            base.OnRender(chartControl, chartScale);
            if (IsInHitTest) return;

            if (RenderTarget == null) return;
            if (chartControl.ChartPanels.Count == 0) return;
            var bars = BarsArray[__settings.BarsIndex];
            if (bars == null) return;
            if (bars.Count == 0) return;

            if (__brushes == null) return;
            if (__entitySignal == null) return;
            if (__entityAngle == null) return;
            if (__processor == null) return;

            if (chartControl.BarSpacingType == BarSpacingType.TimeBased)
            {
                UserMessage(string.Format("Requires Equidistant bar spacing."));
                return;
            }

            var timer = DateTime.Now;

            var oldAntialiasMode = RenderTarget.AntialiasMode;
            RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;

            var chartPanel = chartControl.ChartPanels[0];

            DateTime chartMinTime, chartMaxTime;
            double chartMinPrice, chartMaxPrice;
            {
                var chartStartPoint = new Vector2(chartPanel.X, chartPanel.Y);
                var chartEndPoint = new Vector2(chartPanel.X + chartPanel.W, chartPanel.Y + chartPanel.H);

                var startIndex = chartControl.GetSlotIndexByX((int)chartStartPoint.X);
                chartMinTime = chartControl.GetTimeBySlotIndex(startIndex);

                var endIndex = chartControl.GetSlotIndexByX((int)chartEndPoint.X);
                chartMaxTime = chartControl.GetTimeBySlotIndex(endIndex);

                chartMaxPrice = chartScale.MaxValue;
                chartMinPrice = chartScale.MinValue;
            }

            var forceAnchor = __entitySignal.Anchor;
            var angleAnchor = __entityAngle.Anchor;
            var handlebrush = __brushes[NamingHandle];
            var patternbrush = __brushes[NamingPattern];
            var backgroundbrush = __drawTools.GetBrush(NamingChart + "Background");

            #region Force Signal

            if (forceAnchor.IsSet)
            {
                forceAnchor.IsDrawn = false;

                DtwSolidColorBrushDX buyBrush, sellBrush;
                DtwSolidColorBrushDX dotBrushBuy, dotBrushSell;
                DtwSolidColorBrushDX oplBrushBuy, oplBrushSell;
                if (__entitySignal.IsReversePolarity)
                {
                    buyBrush = __brushes[NamingForceLine + NamingSell];
                    sellBrush = __brushes[NamingForceLine + NamingBuy];

                    dotBrushBuy = __brushes[NamingDot + NamingSell];
                    dotBrushSell = __brushes[NamingDot + NamingBuy];

                    oplBrushBuy = __brushes[NamingOpl + NamingSell];
                    oplBrushSell = __brushes[NamingOpl + NamingBuy];
                }
                else
                {
                    buyBrush = __brushes[NamingForceLine + NamingBuy];
                    sellBrush = __brushes[NamingForceLine + NamingSell];

                    dotBrushBuy = __brushes[NamingDot + NamingBuy];
                    dotBrushSell = __brushes[NamingDot + NamingSell];

                    oplBrushBuy = __brushes[NamingOpl + NamingBuy];
                    oplBrushSell = __brushes[NamingOpl + NamingSell];
                }

                //in "editing" modes, fill the chart with force signals
                #region signal editing

                if (forceAnchor.State != ForceAnchor.DrawingState.Normal)
                {
                    var startX = (float)chartControl.GetXByTime(forceAnchor.StartTime);
                    var endX = (float)chartControl.GetXByTime(forceAnchor.EndTime);

                    var distanceX = Math.Abs(endX - startX);
                    var pulseDivision = __entitySignal.CalculationMethod == eForceLineCalcMethod.BarCount ? 1 : __entitySignal.PulseDivision;

                    if (distanceX / pulseDivision >= chartControl.Properties.BarDistance)// - 0.05)
                    {
                        var barSize = chartControl.Properties.BarDistance * 0.85;//this adds a fudge-factor to push the distance out enough to ensure we are getting the right "slot" in the chart
                        var polarityIndex = startX >= endX ? 1 : 0;
                        var moveX = startX >= endX ? endX : startX;

                        var moveY = chartPanel.Y;
                        var moveYY = chartPanel.Y + chartPanel.H;

                        var leftMostX = Math.Max(0 - distanceX, chartControl.GetXByTime(IsIntraday(bars) ? bars.GetTime(0) : bars.GetSessionEndTime(0)));

                        #region left
                        while (moveX >= leftMostX)
                        {
                            var lineStart = new Vector2(moveX, moveY);
                            var lineEnd = new Vector2(moveX, moveYY);
                            var brush = polarityIndex++ % 2 == 0 ? buyBrush : sellBrush;
                            RenderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(RenderTarget), brush.Width, brush.StrokeStyle);

                            if (moveX - distanceX - barSize < leftMostX) break;
                            var nextTime = chartControl.GetTimeBySlotIndex((int)chartControl.GetSlotIndexByX((int)(moveX - distanceX + barSize)));
                            var nextX = chartControl.GetXByTime(nextTime);

                            if (pulseDivision > 1)
                            {
                                var tmpDistance = moveX - nextX;
                                for (var i = 1; i < pulseDivision; i++)
                                {
                                    var tmpX = moveX - (tmpDistance * ((float)i / pulseDivision));
                                    lineStart = new Vector2(tmpX, moveY);
                                    lineEnd = new Vector2(tmpX, moveYY);
                                    brush = polarityIndex++ % 2 == 0 ? buyBrush : sellBrush;
                                    RenderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(RenderTarget), brush.Width, brush.StrokeStyle);
                                }
                            }

                            moveX = nextX;
                        }
                        #endregion

                        polarityIndex = startX >= endX ? 0 : 1;
                        moveX = startX >= endX ? endX : startX;
                        #region right
                        while (moveX <= chartPanel.W)
                        {

                            var nextTime = chartControl.GetTimeBySlotIndex((int)chartControl.GetSlotIndexByX((int)(moveX + distanceX + barSize)));
                            var nextX = chartControl.GetXByTime(nextTime);

                            var lineStart = new Vector2(nextX, moveY);
                            var lineEnd = new Vector2(nextX, moveYY);
                            var brush = polarityIndex++ % 2 == 0 ? buyBrush : sellBrush;
                            RenderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(RenderTarget), brush.Width, brush.StrokeStyle);

                            if (pulseDivision > 1)
                            {
                                var tmpDistance = nextX - moveX;
                                for (var i = 1; i < pulseDivision; i++)
                                {
                                    var tmpX = moveX + (tmpDistance * ((float)i / pulseDivision));
                                    lineStart = new Vector2(tmpX, moveY);
                                    lineEnd = new Vector2(tmpX, moveYY);
                                    brush = polarityIndex++ % 2 == 1 ? buyBrush : sellBrush;
                                    RenderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(RenderTarget), brush.Width, brush.StrokeStyle);
                                }
                            }

                            moveX = nextX;
                        }
                        #endregion
                    }
                }
                #endregion

                //otherwise draw data
                #region signal drawing
                else
                {
                    var acquiredLock = false;
                    try
                    {
                        Monitor.TryEnter(__settings, ref acquiredLock);
                        if (acquiredLock)
                        {
                            #region draw signals

                            var currentSignal = __processor.CurrentSignal;
                            if (currentSignal != null)
                            {
                                var mostRecentBarTime = IsIntraday(bars) ? bars.GetTime(bars.Count - 1) : bars.GetSessionEndTime(bars.Count - 1);
                                var pulseDivision = __entitySignal.CalculationMethod == eForceLineCalcMethod.BarCount ? 1 : __entitySignal.PulseDivision;
                                var total = 0;

                                #region Angle setup
                                var doAngleRight = __angleExtension.Method != eTachEonExtensionMethods.None && _angleRenderFoward && __entityAngle.HasAngle && angleAnchor.State == ForceAnchor.DrawingState.Normal;
                                var doAngleLeft = __angleExtension.Method != eTachEonExtensionMethods.None && _angleRenderReverse && __entityAngle.HasAngle && angleAnchor.State == ForceAnchor.DrawingState.Normal;

                                var buyAngle = __brushes[NamingResAngle + NamingBuy];
                                var sellAngle = __brushes[NamingResAngle + NamingSell];
                                var buyAngleReverse = __brushes[NamingResAngleReverse + NamingBuy];
                                var sellAngleReverse = __brushes[NamingResAngleReverse + NamingSell];
                                #endregion

                                #region Dot setup
                                var doDot = __dotExtension.Method != eTachEonExtensionMethods.None;
                                #endregion

                                #region OPL setup
                                var doOpenPriceLine = __oplExtension.Method != eTachEonExtensionMethods.None;
                                #endregion

                                var patternDistance = (TickSize * (_patternOffset + __settings.DotOffset));

                                var dotDistance = (TickSize * __settings.DotOffset);
                                var theAngle = __entityAngle.Angle * TickSize;
                                var angleDrawn = 0;
                                var angleReverseDrawn = 0;

                                var resetStartSecs = __entitySignal.ResetsToAnchorTime
                                    ? __entitySignal.Anchor.StartTime.Second + (__entitySignal.Anchor.StartTime.Minute * 60) + (__entitySignal.Anchor.StartTime.Hour * 3600)
                                    : 0;

                                var baseSignal = currentSignal.IsPrimary ? currentSignal : currentSignal.Parent;

                                var calc = baseSignal;
                                while (calc.IsEstimated && calc.PreviousPrimary != null)
                                    calc = calc.PreviousPrimary;

                                var calcStart = chartControl.GetXByTime(calc.BarTime);
                                var calcEnd = chartControl.GetXByTime(calc.EndTime);

                                var distanceX = Math.Abs(calcStart - calcEnd);
                                var lastX = chartPanel.W;

                                var node = currentSignal;
                                var signal = node;

                                var future = signal.Next;
                                if (future != null)
                                {
                                    var baseX = chartControl.GetXByTime(baseSignal.BarTime);
                                    while (future != null)
                                    {
                                        if (future.BarTime.CompareTo(chartMaxTime) > 0)
                                            break;

                                        var isResetSignal = false;
                                        if (__entitySignal.ResetsToAnchorTime)
                                        {
                                            var secsStart = future.BarTime.Second + (future.BarTime.Minute * 60) + (future.BarTime.Hour * 3600);
                                            isResetSignal = resetStartSecs == secsStart;
                                        }

                                        if (isResetSignal)
                                            baseX = chartControl.GetXByTime(future.BarTime);
                                        else if (future.IsPrimary)
                                            baseX += distanceX;

                                        var chartX = baseX + (distanceX * ((float)future.ChildNumber / pulseDivision));

                                        lastX = (int)Math.Min(chartX, lastX);

                                        var emphasize = future.IsPrimary ? (__debugging ? 1 : 0) : 0;
                                        emphasize += isResetSignal ? (__debugging ? 4 : 2) : 0;
                                        DrawForceSignal(RenderTarget, chartControl, chartPanel, chartX, future.BarTime, future.IsEven ? buyBrush : sellBrush, true, emphasize);

                                        RenderPattern(RenderTarget, chartControl, chartScale, chartPanel, future,
                                            pulseDivision, baseX, chartX, lastX, distanceX, dotDistance, patternDistance,
                                            (int)(Math.Min(buyBrush.Width, sellBrush.Width)), dotBrushBuy, dotBrushSell);

                                        future = future.Next;
                                    }
                                }

                                Signal primary = null;
                                var divCount = 0;
                                var calcX = 0;

                                while (node != null)
                                {
                                    // Just in case (State == State.Terminated) gets called while going through the list
                                    if (!__onRender) return;

                                    signal = node;

                                    var currentNode = node;//store to be used with DOTS/OPL before iteration to Previous
                                    node = node.Previous;

                                    var isResetSignal = false;
                                    if (__entitySignal.ResetsToAnchorTime)
                                    {
                                        var secsStart = signal.BarTime.Second + (signal.BarTime.Minute * 60) + (signal.BarTime.Hour * 3600);
                                        isResetSignal = resetStartSecs == secsStart;
                                    }

                                    //used throughout the loop - this is the key X coordinate
                                    var barX = chartControl.GetXByTime(signal.BarTime);

                                    var toTheRightOfChart = signal.BarTime.CompareTo(chartMaxTime) > 0;

                                    #region doAngleLeft
                                    if (doAngleLeft)// && signal.HasData)//<- hasData meaning it is NOT in the future
                                    {
                                        if (__angleExtension.Method == eTachEonExtensionMethods.Infinite
                                            || (__angleExtension.Method == eTachEonExtensionMethods.NumberOfSignals && !toTheRightOfChart && angleReverseDrawn++ < __angleExtension.Count))
                                        {
                                            var toTheLeft = theAngle * (barX / chartControl.Properties.BarDistance);

                                            var startPrice = _angleStart ? signal.Open : signal.Close;
                                            var endPrice = startPrice - toTheLeft;

                                            if (startPrice >= chartMinPrice
                                            || startPrice <= chartMaxPrice
                                            || endPrice >= chartMinPrice
                                            || endPrice <= chartMaxPrice)
                                            {
                                                var startY = chartScale.GetYByValue(startPrice);
                                                var endY = chartScale.GetYByValue(endPrice);

                                                var lineStart = new Vector2(barX, startY);
                                                var lineEnd = new Vector2(0, endY);

                                                RenderTarget.DrawLine(lineStart, lineEnd, buyAngleReverse.GetBrush(RenderTarget), buyAngleReverse.Width, buyAngleReverse.StrokeStyle);
                                            }

                                            endPrice = startPrice + toTheLeft;
                                            if (startPrice >= chartMinPrice
                                            || startPrice <= chartMaxPrice
                                            || endPrice >= chartMinPrice
                                            || endPrice <= chartMaxPrice)
                                            {
                                                var startY = chartScale.GetYByValue(startPrice);
                                                var endY = chartScale.GetYByValue(endPrice);

                                                var lineStart = new Vector2(barX, startY);
                                                var lineEnd = new Vector2(0, endY);

                                                RenderTarget.DrawLine(lineStart, lineEnd, sellAngleReverse.GetBrush(RenderTarget), sellAngleReverse.Width, sellAngleReverse.StrokeStyle);
                                            }
                                        }
                                    }
                                    #endregion

                                    //in fib calculation, need to adjust "line distance" so drawings look jail-cell-like
                                    if (!signal.IsPrimary)
                                    {
                                        if (!signal.Parent.Equals(primary))
                                        {
                                            if (primary == null) // first one
                                            {
                                                primary = signal.Parent;
                                                calcX = chartControl.GetXByTime(primary.BarTime);
                                                var tmp = primary;
                                                divCount = 1;
                                                while (tmp.Next != null && !tmp.Next.IsPrimary && !tmp.Next.IsEstimated)
                                                {
                                                    divCount++;
                                                    tmp = tmp.Next;
                                                }
                                            }
                                            else
                                            {
                                                primary = signal.Parent;
                                                calcX = chartControl.GetXByTime(primary.BarTime);
                                                divCount = Math.Min(primary.ChildCount + 1, pulseDivision);
                                            }
                                        }

                                        divCount--;
                                    }

                                    //no need to calculate any line to the right of the chart
                                    if (toTheRightOfChart) continue;

                                    //chartX is basically barX (see above) but takes into account the "in between bar" signals
                                    //barX is used for the X coord if you want it on the bar, chartX is used if you want it for the line itself
                                    var chartX = signal.IsPrimary ? barX : (calcX + (distanceX * ((float)divCount / pulseDivision)));

                                    var emphasize = signal.IsPrimary ? (__debugging ? 1 : 0) : 0;
                                    emphasize += isResetSignal ? (__debugging ? 4 : 2) : 0;

                                    DrawForceSignal(RenderTarget, chartControl, chartPanel, chartX, signal.BarTime, signal.IsEven ? buyBrush : sellBrush, false, emphasize);

                                    RenderPattern(RenderTarget, chartControl, chartScale, chartPanel, signal,
                                        pulseDivision, barX, chartX, lastX, distanceX, dotDistance, patternDistance,
                                        (int)(Math.Min(buyBrush.Width, sellBrush.Width)), dotBrushBuy, dotBrushSell);

                                    #region Info Signal Draw
                                    if (__infoEnabled
                                        && __processor.InfoSignal != null
                                        && __processor.InfoSignal.Equals(signal))
                                    {
                                        var rect = new SharpDX.Rectangle((int)(chartX + ForceBuyWidth), chartPanel.H - 10, (int)(lastX - chartX - (ForceBuyWidth * 2)), 10);
                                        RenderTarget.FillRectangle(rect, handlebrush.GetBrush(RenderTarget));

                                        //if (__debugging && __primaryBarsArray.Data.ContainsKey(signal.BarTime))
                                        //{
                                        //    var data = __primaryBarsArray.Data[signal.BarTime];

                                        //    var strokeStyleProperties = new StrokeStyleProperties();
                                        //    strokeStyleProperties.DashStyle = SharpDX.Direct2D1.DashStyle.Solid;

                                        //    var style = new StrokeStyle(Core.Globals.D2DFactory, strokeStyleProperties);

                                        //    {
                                        //        var color = Brushes.Blue.Color;
                                        //        var brush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(color.R, color.G, color.B, color.A));
                                        //        var highY = chartScale.GetYByValue(signal.High);
                                        //        for (var i = 1; i <= 3; i++)
                                        //        {
                                        //            var diff = signal.BarIndex - data.GetHighest(i);
                                        //            if (diff >= 0)
                                        //            {
                                        //                var timeS = bars.GetTime(diff);
                                        //                var sS = bars.GetHigh(diff);

                                        //                var lineStart = new Vector2(barX, chartScale.GetYByValue(signal.High));
                                        //                var lineEnd = new Vector2(chartControl.GetXByTime(timeS), chartScale.GetYByValue(sS));

                                        //                RenderTarget.DrawLine(lineStart, lineEnd, brush, i, style);
                                        //                //DrawArrowHead(RenderTarget, lineStart, lineEnd, brush);

                                        //                diff = signal.BarIndex - data.GetSwingHigh(i);
                                        //                if (diff >= 0)
                                        //                {
                                        //                    timeS = bars.GetTime(diff);
                                        //                    sS = bars.GetHigh(diff);
                                        //                    var lineNext = new Vector2(chartControl.GetXByTime(timeS), chartScale.GetYByValue(sS));

                                        //                    RenderTarget.DrawLine(lineEnd, lineNext, brush, i, style);
                                        //                    //DrawArrowHead(RenderTarget, lineEnd, lineNext, brush);
                                        //                }
                                        //            }
                                        //        }
                                        //        brush.Dispose();
                                        //    }
                                        //    {
                                        //        var color = Brushes.Violet.Color;
                                        //        var brush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(color.R, color.G, color.B, color.A));
                                        //        var lowY = chartScale.GetYByValue(signal.Low);
                                        //        for (var i = 1; i <= 3; i++)
                                        //        {
                                        //            var diff = signal.BarIndex - data.GetLowest(i);
                                        //            if (diff >= 0)
                                        //            {
                                        //                var timeS = bars.GetTime(diff);
                                        //                var sS = bars.GetLow(diff);

                                        //                var lineStart = new Vector2(barX, chartScale.GetYByValue(signal.Low));
                                        //                var lineEnd = new Vector2(chartControl.GetXByTime(timeS), chartScale.GetYByValue(sS));

                                        //                RenderTarget.DrawLine(lineStart, lineEnd, brush, i, style);
                                        //                //DrawArrowHead(RenderTarget, lineStart, lineEnd, brush);

                                        //                diff = signal.BarIndex - data.GetSwingLow(i);
                                        //                if (diff >= 0)
                                        //                {
                                        //                    timeS = bars.GetTime(diff);
                                        //                    sS = bars.GetLow(diff);
                                        //                    var lineNext = new Vector2(chartControl.GetXByTime(timeS), chartScale.GetYByValue(sS));

                                        //                    RenderTarget.DrawLine(lineEnd, lineNext, brush, i, style);
                                        //                    //DrawArrowHead(RenderTarget, lineEnd, lineNext, brush);
                                        //                }
                                        //            }
                                        //        }
                                        //        brush.Dispose();
                                        //    }
                                        //}
                                    }
                                    #endregion

                                    //trap lastX for drawing patterns
                                    lastX = (int)chartX;

                                    // Just in case (State == State.Terminated) gets called while going through the list
                                    if (!__onRender) return;

                                    //exit for signals to the left
                                    if (signal.BarTime.CompareTo(chartMinTime) < 0) break;

                                    //if (!signal.HasData) continue;// future signal, skip the rest

                                    #region doDot
                                    if (doDot)
                                    {
                                        int y;
                                        DtwSolidColorBrushDX brush;
                                        if (signal.IsEven)
                                        {
                                            y = __entitySignal.IsReversePolarity ? chartScale.GetYByValue(signal.High + dotDistance) : chartScale.GetYByValue(signal.Low - dotDistance);
                                            brush = dotBrushBuy;
                                        }
                                        else
                                        {
                                            y = __entitySignal.IsReversePolarity ? chartScale.GetYByValue(signal.Low - dotDistance) : chartScale.GetYByValue(signal.High + dotDistance);
                                            brush = dotBrushSell;
                                        }

                                        if (y >= 0 && y <= chartPanel.H)
                                        {
                                            var lineStart = new Vector2(barX, y);

                                            var dot = new SharpDX.Direct2D1.Ellipse(lineStart, __dotRadius, __dotRadius);
                                            RenderTarget.FillEllipse(dot, brush.GetBrush(RenderTarget));

                                            if (__dotExtension.Count > 0)
                                            {
                                                var myX = GetExtensionX(chartControl, chartPanel, currentNode, pulseDivision, distanceX, __dotExtension.Method, __dotExtension.Count);
                                                if (myX > 0)
                                                {
                                                    var lineEnd = new Vector2(myX, y);
                                                    RenderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(RenderTarget), brush.Width, brush.StrokeStyle);
                                                }
                                            }
                                        }
                                    }
                                    #endregion

                                    #region doOpl
                                    if (doOpenPriceLine)
                                    {
                                        var myX = GetExtensionX(chartControl, chartPanel, currentNode, pulseDivision, distanceX, __oplExtension.Method, __oplExtension.Count);
                                        if (myX > 0)
                                        {
                                            if (signal.Close >= chartMinPrice && signal.Close <= chartMaxPrice)
                                            {
                                                var y = chartScale.GetYByValue(signal.Close);
                                                var lineStart = new Vector2(barX, y);
                                                var lineEnd = new Vector2(myX, y);
                                                var brush = signal.IsEven ? oplBrushBuy : oplBrushSell;

                                                RenderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(RenderTarget), brush.Width, brush.StrokeStyle);
                                            }
                                        }
                                    }
                                    #endregion

                                    #region doAngleRight
                                    if (doAngleRight)
                                    {
                                        if (__angleExtension.Method == eTachEonExtensionMethods.Infinite
                                            || (__angleExtension.Method == eTachEonExtensionMethods.NumberOfSignals && angleDrawn++ < __angleExtension.Count))
                                        {
                                            var toTheRight = theAngle * ((chartPanel.W - barX) / chartControl.Properties.BarDistance);

                                            var startPrice = _angleStart ? signal.Open : signal.Close;
                                            var endPrice = startPrice + toTheRight;

                                            if (startPrice >= chartMinPrice
                                            || startPrice <= chartMaxPrice
                                            || endPrice >= chartMinPrice
                                            || endPrice <= chartMaxPrice)
                                            {
                                                var startY = chartScale.GetYByValue(startPrice);
                                                var endY = chartScale.GetYByValue(endPrice);

                                                var lineStart = new Vector2(barX, startY);
                                                var lineEnd = new Vector2(chartPanel.W, endY);

                                                RenderTarget.DrawLine(lineStart, lineEnd, sellAngle.GetBrush(RenderTarget), sellAngle.Width, sellAngle.StrokeStyle);
                                            }

                                            endPrice = startPrice - toTheRight;
                                            if (startPrice >= chartMinPrice
                                            || startPrice <= chartMaxPrice
                                            || endPrice >= chartMinPrice
                                            || endPrice <= chartMaxPrice)
                                            {
                                                var startY = chartScale.GetYByValue(startPrice);
                                                var endY = chartScale.GetYByValue(endPrice);

                                                var lineStart = new Vector2(barX, startY);
                                                var lineEnd = new Vector2(chartPanel.W, endY);

                                                RenderTarget.DrawLine(lineStart, lineEnd, buyAngle.GetBrush(RenderTarget), buyAngle.Width, buyAngle.StrokeStyle);
                                            }
                                        }
                                    }
                                    #endregion

                                    total++;

                                    //exit on _maxDraw
                                    if (total >= _maxDraw) break;
                                }

                                // Just in case (State == State.Terminated) gets called while going through the list
                                if (!__onRender) return;

                                //to the left of the chart
                                if (total < _maxDraw && node != null)
                                {
                                    //pop back one bc the above WHILE loop
                                    node = node.Next;

                                    var leftSignals = new List<Signal>();
                                    if (__dotExtension.Method == eTachEonExtensionMethods.Infinite
                                        || __oplExtension.Method == eTachEonExtensionMethods.Infinite
                                        || __angleExtension.Method == eTachEonExtensionMethods.Infinite)
                                    {
                                        var leftNode = node;
                                        var leftLimit = 50;
                                        while (leftNode != null)
                                        {
                                            leftSignals.Add(leftNode);
                                            leftNode = leftNode.Previous;
                                            if (--leftLimit <= 0) break;
                                        }
                                    }

                                    #region left doDot
                                    if (doDot)
                                    {
                                        if (__dotExtension.Method == eTachEonExtensionMethods.Infinite)
                                        {
                                            //var leftSignals = __primaryCompute.LinkedLines.Where(x => x.BarTime.CompareTo(chartMinTime) < 0);
                                            var all = leftSignals.Where(x =>
                                            {
                                                var price = x.IsEven ? (__entitySignal.IsReversePolarity ? x.High + dotDistance : x.Low - dotDistance) : (__entitySignal.IsReversePolarity ? x.Low - dotDistance : x.High + dotDistance);
                                                return price >= chartMinPrice && price <= chartMaxPrice;
                                            });
                                            foreach (var left in all)
                                            {
                                                int y;
                                                DtwSolidColorBrushDX brush;
                                                if (left.IsEven)
                                                {
                                                    y = __entitySignal.IsReversePolarity ? chartScale.GetYByValue(left.High + dotDistance) : chartScale.GetYByValue(left.Low - dotDistance);
                                                    brush = dotBrushBuy;
                                                }
                                                else
                                                {
                                                    y = __entitySignal.IsReversePolarity ? chartScale.GetYByValue(left.Low - dotDistance) : chartScale.GetYByValue(left.High + dotDistance);
                                                    brush = dotBrushSell;
                                                }
                                                var lineStart = new Vector2(0, y);
                                                var lineEnd = new Vector2(chartPanel.W, y);

                                                RenderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(RenderTarget), brush.Width, brush.StrokeStyle);
                                            }
                                        }
                                        else
                                        {
                                            var leftNode = node;
                                            var rightNode = node;
                                            var count = 0;
                                            while (count < __dotExtension.Count)
                                            {
                                                if (rightNode.Next == null) break;
                                                rightNode = rightNode.Next;
                                                count++;
                                            }

                                            var leftCount = __dotExtension.Count;
                                            while (leftNode != null && leftCount-- > 0)
                                            {
                                                var left = leftNode;
                                                int y;

                                                DtwSolidColorBrushDX brush;
                                                if (left.IsEven)
                                                {
                                                    y = __entitySignal.IsReversePolarity ? chartScale.GetYByValue(left.High + dotDistance) : chartScale.GetYByValue(left.Low - dotDistance);
                                                    brush = dotBrushBuy;
                                                }
                                                else
                                                {
                                                    y = __entitySignal.IsReversePolarity ? chartScale.GetYByValue(left.Low - dotDistance) : chartScale.GetYByValue(left.High + dotDistance);
                                                    brush = dotBrushSell;
                                                }

                                                if (y >= 0 && y <= chartPanel.H)
                                                {
                                                    int x;
                                                    var nextLine = rightNode;
                                                    if (nextLine.IsPrimary)
                                                    {
                                                        x = chartControl.GetXByTime(nextLine.BarTime);
                                                    }
                                                    else
                                                    {
                                                        var timeX = chartControl.GetXByTime(nextLine.Parent.BarTime);
                                                        var tmpCount = 0;
                                                        var next = rightNode;
                                                        while (!next.IsPrimary)
                                                        {
                                                            tmpCount++;
                                                            next = next.Previous;
                                                        }
                                                        x = (int)(timeX + (distanceX * ((float)tmpCount / pulseDivision)));
                                                    }

                                                    var lineStart = new Vector2(0, y);
                                                    var lineEnd = new Vector2(x, y);

                                                    RenderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(RenderTarget), brush.Width, brush.StrokeStyle);
                                                }

                                                leftNode = leftNode.Previous;
                                                rightNode = rightNode.Previous;
                                            }
                                        }
                                    }
                                    #endregion

                                    #region left doOpl
                                    if (doOpenPriceLine)
                                    {
                                        if (__oplExtension.Method == eTachEonExtensionMethods.Infinite)
                                        {
                                            //var leftSignals = __primaryCompute.LinkedLines.Where(x => x.BarTime.CompareTo(chartMinTime) < 0);
                                            var all = leftSignals.Where(x =>
                                            {
                                                return x.Close >= chartMinPrice && x.Close <= chartMaxPrice;
                                            });
                                            foreach (var left in all)
                                            {
                                                var y = chartScale.GetYByValue(left.Close);
                                                var lineStart = new Vector2(0, y);
                                                var lineEnd = new Vector2(chartPanel.W, y);
                                                var brush = signal.IsEven ? oplBrushBuy : oplBrushSell;

                                                RenderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(RenderTarget), brush.Width, brush.StrokeStyle);
                                            }
                                        }
                                        else
                                        {
                                            var leftNode = node;
                                            var rightNode = node;
                                            var count = 0;
                                            while (count < __oplExtension.Count)
                                            {
                                                if (rightNode.Next == null) break;
                                                rightNode = rightNode.Next;
                                                count++;
                                            }
                                            var leftCount = __oplExtension.Count;
                                            while (leftNode != null && leftCount-- > 0)
                                            {
                                                var left = leftNode;

                                                if (left.Close >= chartMinPrice && left.Close <= chartMaxPrice)
                                                {
                                                    int x;
                                                    var nextLine = rightNode;
                                                    if (nextLine.IsPrimary)
                                                        x = chartControl.GetXByTime(nextLine.BarTime);
                                                    else
                                                    {
                                                        var timeX = chartControl.GetXByTime(nextLine.Parent.BarTime);
                                                        var tmpCount = 0;
                                                        var next = rightNode;
                                                        while (!next.IsPrimary)
                                                        {
                                                            tmpCount++;
                                                            next = next.Previous;
                                                        }
                                                        x = (int)(timeX + (distanceX * ((float)tmpCount / pulseDivision)));
                                                    }

                                                    var y = chartScale.GetYByValue(left.Close);
                                                    var lineStart = new Vector2(0, y);
                                                    var lineEnd = new Vector2(x, y);
                                                    var brush = left.IsEven ? oplBrushBuy : oplBrushSell;

                                                    RenderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(RenderTarget), brush.Width, brush.StrokeStyle);
                                                }

                                                leftNode = leftNode.Previous;
                                                rightNode = rightNode.Previous;
                                            }
                                        }
                                    }
                                    #endregion

                                    #region left doAngleRight
                                    if (doAngleRight)
                                    {
                                        if (__angleExtension.Method == eTachEonExtensionMethods.Infinite || (__angleExtension.Method == eTachEonExtensionMethods.NumberOfSignals && angleDrawn < __angleExtension.Count))
                                        {
                                            //var leftLines = __primaryCompute.LinkedLines.Where(x => x.BarTime.CompareTo(chartMinTime) < 0).OrderByDescending(x => x.BarTime);
                                            var count = 10;
                                            var leftE = leftSignals.OrderByDescending(x => x.BarTime).GetEnumerator();
                                            while (leftE.MoveNext())
                                            {
                                                var left = leftE.Current;
                                                var barX = chartControl.GetXByTime(left.BarTime);
                                                var angleX = chartPanel.W;
                                                var toTheRight = theAngle * ((angleX - barX) / chartControl.Properties.BarDistance);

                                                var startPrice = _angleStart ? left.Open : left.Close;
                                                var endPrice = startPrice + toTheRight;

                                                if (startPrice >= chartMinPrice ||
                                                startPrice <= chartMaxPrice ||
                                                endPrice >= chartMinPrice ||
                                                endPrice <= chartMaxPrice)
                                                {
                                                    var startY = chartScale.GetYByValue(startPrice);
                                                    var endY = chartScale.GetYByValue(endPrice);

                                                    var lineStart = new Vector2(barX, startY);
                                                    var lineEnd = new Vector2(angleX, endY);

                                                    RenderTarget.DrawLine(lineStart, lineEnd, sellAngle.GetBrush(RenderTarget), sellAngle.Width, sellAngle.StrokeStyle);
                                                }

                                                endPrice = startPrice - toTheRight;

                                                if (startPrice >= chartMinPrice ||
                                                startPrice <= chartMaxPrice ||
                                                endPrice >= chartMinPrice ||
                                                endPrice <= chartMaxPrice)
                                                {
                                                    var startY = chartScale.GetYByValue(startPrice);
                                                    var endY = chartScale.GetYByValue(endPrice);

                                                    var lineStart = new Vector2(barX, startY);
                                                    var lineEnd = new Vector2(angleX, endY);

                                                    RenderTarget.DrawLine(lineStart, lineEnd, buyAngle.GetBrush(RenderTarget), buyAngle.Width, buyAngle.StrokeStyle);
                                                }

                                                if (__angleExtension.Method == eTachEonExtensionMethods.NumberOfSignals && ++angleDrawn >= __angleExtension.Count) break;
                                                if (--count <= 0) break;
                                            }
                                        }
                                    }
                                    #endregion
                                }
                            }

                            #endregion
                        }
                    }
                    catch (Exception err)
                    {
                        throw new ForceException(string.Format("OnRender: {0}", err.Message));
                    }
                    finally
                    {
                        if (acquiredLock)
                            Monitor.Exit(__settings);
                    }
                }
                #endregion

                //only draw "the zone" when in view
                #region zone draw

                if ((forceAnchor.StartTime.CompareTo(chartMinTime) >= 0 && forceAnchor.StartTime.CompareTo(chartMaxTime) <= 0) //start time is within chart viewing area
                     || (forceAnchor.EndTime.CompareTo(chartMinTime) >= 0 && forceAnchor.EndTime.CompareTo(chartMaxTime) <= 0) //end time is within chart viewing area
                     || (forceAnchor.StartTime.CompareTo(chartMinTime) <= 0 && forceAnchor.EndTime.CompareTo(chartMaxTime) >= 0)) //the anchor is so large, it spans the entire chart
                {
                    var startX = (float)chartControl.GetXByTime(forceAnchor.StartTime);
                    var startY = (float)chartScale.GetYByValueWpf(forceAnchor.StartPrice);

                    var endX = (float)chartControl.GetXByTime(forceAnchor.EndTime);
                    var endY = (float)chartScale.GetYByValueWpf(forceAnchor.EndPrice);

                    var gradientStops = new SharpDX.Direct2D1.GradientStop[2];
                    gradientStops[0].Color = buyBrush.Color.BrushColor;
                    gradientStops[0].Color.Alpha = _zoneOpacity;
                    gradientStops[0].Position = 0.0f;

                    gradientStops[1].Color = sellBrush.Color.BrushColor;
                    gradientStops[1].Color.Alpha = _zoneOpacity;
                    gradientStops[1].Position = 1.0f;

                    var gradientStopCollection = new SharpDX.Direct2D1.GradientStopCollection(RenderTarget, gradientStops);

                    var pulseZoneProperties = new SharpDX.Direct2D1.LinearGradientBrushProperties();
                    pulseZoneProperties.StartPoint = new Vector2(startX, chartPanel.Y + chartPanel.H);
                    pulseZoneProperties.EndPoint = new Vector2(endX, chartPanel.Y + chartPanel.H);

                    var pulseZoneBrush = new SharpDX.Direct2D1.LinearGradientBrush(RenderTarget, pulseZoneProperties, gradientStopCollection);

                    var rect = new RectangleF(startX, chartPanel.Y, endX - startX, chartPanel.Y + chartPanel.H);
                    RenderTarget.FillRectangle(rect, pulseZoneBrush);

                    pulseZoneBrush.Dispose();
                    gradientStopCollection.Dispose();

                    DrawAnchorHandle(RenderTarget, forceAnchor, new Vector2(startX, startY), new Vector2(endX, endY));
                }

                #endregion
            }

            #endregion

            #region angle

            if (angleAnchor.IsSet)
            {
                angleAnchor.IsDrawn = false;

                var brush = __brushes[NamingResAngle];

                var barX = chartControl.GetXByTime(angleAnchor.StartTime);

                //need to keep angle handles forward looking to keep calculations consistent
                var flip = angleAnchor.StartPrice <= angleAnchor.EndPrice ? 1 : -1;
                var flop = flip * (angleAnchor.StartTime.CompareTo(angleAnchor.EndTime) <= 0 ? 1 : -1);

                var angleCalc = __entityAngle.Angle * TickSize * flop;

                var startPrice = angleAnchor.StartPrice - (angleCalc * (barX / chartControl.Properties.BarDistance));

                var endPrice = angleAnchor.StartPrice + (angleCalc * ((chartPanel.W - barX) / chartControl.Properties.BarDistance));

                if (startPrice >= chartMinPrice || startPrice <= chartMaxPrice || endPrice >= chartMinPrice || endPrice <= chartMaxPrice)
                {
                    var startY = chartScale.GetYByValue(startPrice);
                    var endY = chartScale.GetYByValue(endPrice);
                    var lineStart = new Vector2(0, startY);
                    var lineEnd = new Vector2(chartPanel.W, endY);

                    RenderTarget.DrawLine(lineStart, lineEnd, brush.GetBrush(RenderTarget), brush.Width, brush.StrokeStyle);

                    if ((angleAnchor.StartTime.CompareTo(chartMinTime) >= 0 && angleAnchor.StartTime.CompareTo(chartMaxTime) <= 0)
                        || (angleAnchor.EndTime.CompareTo(chartMinTime) >= 0 && angleAnchor.EndTime.CompareTo(chartMaxTime) <= 0))
                    {
                        DrawAnchorHandle(RenderTarget, angleAnchor,
                            new Vector2(chartControl.GetXByTime(angleAnchor.StartTime),
                                (float)chartScale.GetYByValueWpf(angleAnchor.StartPrice)),
                            new Vector2(chartControl.GetXByTime(angleAnchor.EndTime),
                                (float)chartScale.GetYByValueWpf(angleAnchor.EndPrice)));
                    }
                }
            }

            #endregion

            RenderTarget.AntialiasMode = oldAntialiasMode;

            var time = (DateTime.Now - timer).TotalMilliseconds;
            __avgOnRender = ((__avgOnRender * 10) - __avgOnRender + time) / 10;
            //Log(string.Format("OnRender: {0} ({1})", __avgOnRender, time));
        }

        #endregion onrender

        #region override DisplayName

        public override string DisplayName
        {
            get { return string.Format("{0}{1}", __debugging ? "DEBUGGING " : "", Name); }
        }

        #endregion override DisplayName

        #region exported data

        [Browsable(false)]
        [XmlIgnore()]
        public Series<double> TrendValue
        {
            get
            {
                Update();
                return Values[0];
            }
        }

        [Browsable(false)]
        [XmlIgnore()]
        public Series<double> BarsToNextSignal
        {
            get
            {
                Update();
                return Values[1];
            }
        }

        [Browsable(false)]
        [XmlIgnore()]
        public Series<double> BarsFromPreviousSignal
        {
            get
            {
                Update();
                return Values[2];
            }
        }

        [Browsable(false)]
        [XmlIgnore()]
        [Display(Name = "SignalPattern")]
        public Series<double> SignalPattern
        {
            get
            {
                Update();
                return Values[3];
            }
        }

        #endregion

        #region Indicator Properties

        #region Basics

        [ReadOnly(true)]
        [Display(Name = "Version", Order = 10, GroupName = NamingCategoryBasics)]
        public string Indicator_Version
        {
            get { return "2018.08.27"; }
        }

        private bool _panelVisible;

        [Display(Name = "Panel Visible", Order = 1000, GroupName = NamingCategoryBasics)]
        public bool PanelVisible
        {
            get { return _panelVisible; }
            set { _panelVisible = value; }
        }

        private int _panelSize;

        [Display(Name = "Panel Size", Order = 1100, GroupName = NamingCategoryBasics)]
        [Range(0, int.MaxValue)]
        public int PanelSize
        {
            get { return _panelSize; }
            set { _panelSize = value; }
        }

        private NinjaTrader.NinjaScript.Indicators.TachEon.TachEonForce.eForceLineCalcMethod _calcMethod = NinjaTrader.NinjaScript.Indicators.TachEon.TachEonForce.eForceLineCalcMethod.Fibs;

        [Display(Name = "Calculation Method", Order = 2000, GroupName = NamingCategoryBasics)]
        public NinjaTrader.NinjaScript.Indicators.TachEon.TachEonForce.eForceLineCalcMethod ForceLineCalcMethod
        {
            get { return _calcMethod; }
            set { _calcMethod = value; }
        }

        private int _defaultBarCount;

        [Display(Name = "Bar Count", Order = 3000, GroupName = NamingCategoryBasics, Description = "Bar Count")]
        [Range(1, 500)]
        public int BarCountSize
        {
            get { return _defaultBarCount; }
            set { _defaultBarCount = value; }
        }

        private bool _resetsToAnchorTime;

        [Display(Name = "Resets to Anchor Time", Order = 4000, GroupName = NamingCategoryBasics)]
        [Browsable(__betaStuff)]
        public bool ResetsToAnchorTime
        {
            get { return _resetsToAnchorTime; }
            set { _resetsToAnchorTime = value; }
        }

        #endregion

        #region Force Signals

        private float _zoneOpacity;

        [Display(Name = "Zone Opacity", Order = 1010, GroupName = NamingCategoryForceSignals)]
        [Range(1, 100)]
        public int ZoneOpacity
        {
            get { return (int)(_zoneOpacity * 100); }
            set { _zoneOpacity = value / (float)100; }
        }

        private bool _snapToBarLine;

        [Display(Name = "Snap To Bar", Order = 1050, GroupName = NamingCategoryForceSignals)]
        [Browsable(false)]
        public bool SnapToBarLine
        {
            get { return _snapToBarLine; }
            set { _snapToBarLine = value; }
        }

        private bool _renderLineDates;

        [Display(Name = "Render Dates", Order = 1060, GroupName = NamingCategoryForceSignals)]
        public bool RenderLineDates
        {
            get { return _renderLineDates; }
            set { _renderLineDates = value; }
        }

        private int _renderLineDatesFontSize;

        [Display(Name = "Date Font Size", Order = 1070, GroupName = NamingCategoryForceSignals)]
        [Range(1, 100)]
        public int RenderLineDatesFontSize
        {
            get { return _renderLineDatesFontSize; }
            set { _renderLineDatesFontSize = value; }
        }

        #region buy line

        [XmlIgnore]
        [Display(Name = NamingBuy + NamingColor, Order = 1100, GroupName = NamingCategoryForceSignals)]
        public System.Windows.Media.Brush ForceBuyColor { get; set; }

        [Browsable(false)]
        public string PulseBuyLineColorSerialize
        {
            get { return Serialize.BrushToString(ForceBuyColor); }
            set { ForceBuyColor = Serialize.StringToBrush(value); }
        }

        [Display(Name = NamingBuy + NamingStyle, Order = 1110, GroupName = NamingCategoryForceSignals)]
        public SharpDX.Direct2D1.DashStyle ForceBuyStyle { get; set; }

        [Display(Name = NamingBuy + NamingWidth, Order = 1120, GroupName = NamingCategoryForceSignals)]
        [Range(1, 10)]
        public int ForceBuyWidth { get; set; }

        #endregion

        #region sell line

        [XmlIgnore]
        [Display(Name = NamingSell + NamingColor, Order = 5200, GroupName = NamingCategoryForceSignals)]
        public System.Windows.Media.Brush ForceSellColor { get; set; }

        [Browsable(false)]
        public string PulseSellLineColorSerialize
        {
            get { return Serialize.BrushToString(ForceSellColor); }
            set { ForceSellColor = Serialize.StringToBrush(value); }
        }

        [Display(Name = NamingSell + NamingStyle, Order = 5210, GroupName = NamingCategoryForceSignals)]
        public SharpDX.Direct2D1.DashStyle ForceSellStyle { get; set; }

        [Display(Name = NamingSell + NamingWidth, Order = 5220, GroupName = NamingCategoryForceSignals)]
        [Range(1, 10)]
        public int ForceSellWidth { get; set; }

        #endregion

        #endregion

        #region Angles

        private bool _snapToBarAngle;

        [Display(Name = "Snap To Bar", Order = 100, GroupName = NamingCategoryAngles)]
        [Browsable(false)]
        public bool SnapToBarAngle
        {
            get { return _snapToBarAngle; }
            set { _snapToBarAngle = value; }
        }

        private bool _angleStart;

        [TypeConverter(typeof(AngleStartOnBoolConverter))]
        [PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
        [Display(Name = "Start On Open", Order = 120, GroupName = NamingCategoryAngles)]
        public bool ResAngleStart
        {
            get { return _angleStart; }
            set { _angleStart = value; }
        }

        #region extension methods
        [Display(Name = "Method", Order = 130, GroupName = NamingCategoryAngles)]
        public NinjaTrader.NinjaScript.Indicators.TachEon.eTachEonExtensionMethods AngleMethod
        {
            get { return __angleExtension.Method; }
            set { __angleExtension.Method = value; }
        }

        [Display(Name = "Count", Order = 150, GroupName = NamingCategoryAngles)]
        [Range(0, int.MaxValue)]
        public int AngleCount
        {
            get { return __angleExtension.Count; }
            set { __angleExtension.Count = value; }
        }
        #endregion

        #region anchor line

        [XmlIgnore]
        [Display(Name = NamingResAngleRay + NamingColor, Order = 500, GroupName = NamingCategoryAngles)]
        public System.Windows.Media.Brush ResAngleAnchorColor { get; set; }

        [Browsable(false)]
        public string ResAngleAnchorLineColorSerialize
        {
            get { return Serialize.BrushToString(ResAngleAnchorColor); }
            set { ResAngleAnchorColor = Serialize.StringToBrush(value); }
        }

        [Display(Name = NamingResAngleRay + NamingStyle, Order = 510, GroupName = NamingCategoryAngles)]
        public SharpDX.Direct2D1.DashStyle ResAngleAnchorStyle { get; set; }

        [Display(Name = NamingResAngleRay + NamingWidth, Order = 520, GroupName = NamingCategoryAngles)]
        [Range(1, 10)]
        public int ResAngleAnchorWidth { get; set; }

        #endregion

        #region forward 

        private bool _angleRenderFoward;

        [Display(Name = "Render Forward", Order = 1000, GroupName = NamingCategoryAngles)]
        public bool RenderAngle
        {
            get { return _angleRenderFoward; }
            set { _angleRenderFoward = value; }
        }

        [XmlIgnore]
        [Display(Name = NamingBuy + NamingColor, Order = 1100, GroupName = NamingCategoryAngles)]
        public System.Windows.Media.Brush ResAngleBuyColor { get; set; }

        [Browsable(false)]
        public string ResAngleBuyLineColorSerialize
        {
            get { return Serialize.BrushToString(ResAngleBuyColor); }
            set { ResAngleBuyColor = Serialize.StringToBrush(value); }
        }

        [Display(Name = NamingBuy + NamingStyle, Order = 1110, GroupName = NamingCategoryAngles)]
        public SharpDX.Direct2D1.DashStyle ResAngleBuyStyle { get; set; }

        [Display(Name = NamingBuy + NamingWidth, Order = 1120, GroupName = NamingCategoryAngles)]
        [Range(1, 10)]
        public int ResAngleBuyWidth { get; set; }

        [XmlIgnore]
        [Display(Name = NamingSell + NamingColor, Order = 1200, GroupName = NamingCategoryAngles)]
        public System.Windows.Media.Brush ResAngleSellColor { get; set; }

        [Browsable(false)]
        public string ResAngleSellLineColorSerialize
        {
            get { return Serialize.BrushToString(ResAngleSellColor); }
            set { ResAngleSellColor = Serialize.StringToBrush(value); }
        }

        [Display(Name = NamingSell + NamingStyle, Order = 1210, GroupName = NamingCategoryAngles)]
        public SharpDX.Direct2D1.DashStyle ResAngleSellStyle { get; set; }

        [Display(Name = NamingSell + NamingWidth, Order = 1220, GroupName = NamingCategoryAngles)]
        [Range(1, 10)]
        public int ResAngleSellWidth { get; set; }

        #endregion

        #region reverse 

        private bool _angleRenderReverse;

        [Display(Name = "Render Reverse", Order = 2000, GroupName = NamingCategoryAngles)]
        public bool RenderReverseAngle
        {
            get { return _angleRenderReverse; }
            set { _angleRenderReverse = value; }
        }

        [XmlIgnore]
        [Display(Name = NamingResAngleReverse + NamingBuy + NamingColor, Order = 2130, GroupName = NamingCategoryAngles)]
        public System.Windows.Media.Brush ResAngleReverseBuyColor { get; set; }

        [Browsable(false)]
        public string ResAngleReverseBuyLineColorSerialize
        {
            get { return Serialize.BrushToString(ResAngleReverseBuyColor); }
            set { ResAngleReverseBuyColor = Serialize.StringToBrush(value); }
        }

        [Display(Name = NamingResAngleReverse + NamingBuy + NamingStyle, Order = 2140, GroupName = NamingCategoryAngles)]
        public SharpDX.Direct2D1.DashStyle ResAngleReverseBuyStyle { get; set; }

        [Display(Name = NamingResAngleReverse + NamingBuy + NamingWidth, Order = 2150, GroupName = NamingCategoryAngles)]
        [Range(1, 10)]
        public int ResAngleReverseBuyWidth { get; set; }

        [XmlIgnore]
        [Display(Name = NamingResAngleReverse + NamingSell + NamingColor, Order = 2230, GroupName = NamingCategoryAngles)]
        public System.Windows.Media.Brush ResAngleReverseSellColor { get; set; }

        [Browsable(false)]
        public string ResAngleReverseSellLineColorSerialize
        {
            get { return Serialize.BrushToString(ResAngleReverseSellColor); }
            set { ResAngleReverseSellColor = Serialize.StringToBrush(value); }
        }

        [Display(Name = NamingResAngleReverse + NamingSell + NamingStyle, Order = 2240, GroupName = NamingCategoryAngles)]
        public SharpDX.Direct2D1.DashStyle ResAngleReverseSellStyle { get; set; }

        [Display(Name = NamingResAngleReverse + NamingSell + NamingWidth, Order = 2250, GroupName = NamingCategoryAngles)]
        [Range(1, 10)]
        public int ResAngleReverseSellWidth { get; set; }

        #endregion

        #endregion

        #region Dots

        [Display(Name = NamingOffset, Order = 150, GroupName = NamingCategoryDots)]
        [Range(0, int.MaxValue)]
        public int DotOffset
        {
            get { return __settings.DotOffset; }
            set { __settings.DotOffset = value; }
        }

        [Display(Name = "Method", Order = 1000, GroupName = NamingCategoryDots)]
        public NinjaTrader.NinjaScript.Indicators.TachEon.eTachEonExtensionMethods DotMethod
        {
            get { return __dotExtension.Method; }
            set { __dotExtension.Method = value; }
        }

        [Display(Name = "Count", Order = 1040, GroupName = NamingCategoryDots)]
        [Range(0, int.MaxValue)]
        public int DotCount
        {
            get { return __dotExtension.Count; }
            set { __dotExtension.Count = value; }
        }

        private int _dotWidth;

        [Display(Name = "Width", Order = 1050, GroupName = NamingCategoryDots)]
        [Range(1, int.MaxValue)]
        public int DotWidth
        {
            get { return _dotWidth; }
            set { _dotWidth = value; }
        }

        [XmlIgnore]
        [Display(Name = NamingBuy + NamingColor, Order = 6130, GroupName = NamingCategoryDots)]
        public System.Windows.Media.Brush DotBuyColor { get; set; }

        [Browsable(false)]
        public string DotBuyLineColorSerialize
        {
            get { return Serialize.BrushToString(DotBuyColor); }
            set { DotBuyColor = Serialize.StringToBrush(value); }
        }

        [Display(Name = NamingBuy + NamingStyle, Order = 6140, GroupName = NamingCategoryDots)]
        public SharpDX.Direct2D1.DashStyle DotBuyStyle { get; set; }

        [Display(Name = NamingBuy + NamingWidth, Order = 6150, GroupName = NamingCategoryDots)]
        [Range(1, 10)]
        public int DotBuyWidth { get; set; }

        [XmlIgnore]
        [Display(Name = NamingSell + NamingColor, Order = 6230, GroupName = NamingCategoryDots)]
        public System.Windows.Media.Brush DotSellColor { get; set; }

        [Browsable(false)]
        public string DotSellLineColorSerialize
        {
            get { return Serialize.BrushToString(DotSellColor); }
            set { DotSellColor = Serialize.StringToBrush(value); }
        }

        [Display(Name = NamingSell + NamingStyle, Order = 6240, GroupName = NamingCategoryDots)]
        public SharpDX.Direct2D1.DashStyle DotSellStyle { get; set; }

        [Display(Name = NamingSell + NamingWidth, Order = 6250, GroupName = NamingCategoryDots)]
        [Range(1, 10)]
        public int DotSellWidth { get; set; }

        #endregion

        #region Open Price Lines

        [Display(Name = "Method", Order = 1000, GroupName = NamingCategoryOpenPriceLines)]
        public NinjaTrader.NinjaScript.Indicators.TachEon.eTachEonExtensionMethods OplMethod
        {
            get { return __oplExtension.Method; }
            set { __oplExtension.Method = value; }
        }

        [Display(Name = "Count", Order = 1050, GroupName = NamingCategoryOpenPriceLines)]
        [Range(0, int.MaxValue)]
        public int OplCount
        {
            get { return __oplExtension.Count; }
            set { __oplExtension.Count = value; }
        }

        [XmlIgnore]
        [Display(Name = NamingBuy + NamingColor, Order = 6130, GroupName = NamingCategoryOpenPriceLines)]
        public System.Windows.Media.Brush OplBuyColor { get; set; }

        [Browsable(false)]
        public string OplBuyLineColorSerialize
        {
            get { return Serialize.BrushToString(OplBuyColor); }
            set { OplBuyColor = Serialize.StringToBrush(value); }
        }

        [Display(Name = NamingBuy + NamingStyle, Order = 6140, GroupName = NamingCategoryOpenPriceLines)]
        public SharpDX.Direct2D1.DashStyle OplBuyStyle { get; set; }

        [Display(Name = NamingBuy + NamingWidth, Order = 6150, GroupName = NamingCategoryOpenPriceLines)]
        [Range(1, 10)]
        public int OplBuyWidth { get; set; }

        [XmlIgnore]
        [Display(Name = NamingSell + NamingColor, Order = 6230, GroupName = NamingCategoryOpenPriceLines)]
        public System.Windows.Media.Brush OplSellColor { get; set; }

        [Browsable(false)]
        public string OplSellLineColorSerialize
        {
            get { return Serialize.BrushToString(OplSellColor); }
            set { OplSellColor = Serialize.StringToBrush(value); }
        }

        [Display(Name = NamingSell + NamingStyle, Order = 6240, GroupName = NamingCategoryOpenPriceLines)]
        public SharpDX.Direct2D1.DashStyle OplSellStyle { get; set; }

        [Display(Name = NamingSell + NamingWidth, Order = 6250, GroupName = NamingCategoryOpenPriceLines)]
        [Range(1, 10)]
        public int OplSellWidth { get; set; }

        #endregion

        #region Patterns

        private bool _patternPrint;

        [Display(Name = "Print Patterns", Order = 100, GroupName = NamingCategoryPatterns)]
        [Browsable(false)]
        public bool PatternPrint
        {
            get { return _patternPrint; }
            set { _patternPrint = value; }
        }

        private int _patternOffset;

        [Display(Name = NamingOffset + " (from Dot)", Order = 150, GroupName = NamingCategoryPatterns)]
        [Range(1, int.MaxValue)]
        public int PatternOffset
        {
            get { return _patternOffset; }
            set { _patternOffset = value; }
        }

        [XmlIgnore]
        [Display(Name = NamingColor, Order = 800, GroupName = NamingCategoryPatterns)]
        public System.Windows.Media.Brush PatternColor { get; set; }

        [Browsable(false)]
        public string PatternColorColorSerialize
        {
            get { return Serialize.BrushToString(PatternColor); }
            set { PatternColor = Serialize.StringToBrush(value); }
        }

        [Display(Name = NamingStyle, Order = 810, GroupName = NamingCategoryPatterns)]
        public SharpDX.Direct2D1.DashStyle PatternStyle { get; set; }

        [Display(Name = NamingWidth, Order = 820, GroupName = NamingCategoryPatterns)]
        [Range(1, 10)]
        public int PatternWidth { get; set; }

        private int _patternFontSize;

        [Display(Name = "Font Size", Order = 1000, GroupName = NamingCategoryPatterns)]
        [Range(1, 100)]
        public int PatternFontSize
        {
            get { return _patternFontSize; }
            set { _patternFontSize = value; }
        }

        private bool _patternPrint12;

        [Display(Name = "Print Pattern 1/2", Order = 2500, GroupName = NamingCategoryPatterns)]
        public bool Pattern12
        {
            get { return _patternPrint12; }
            set { _patternPrint12 = value; }
        }

        private bool _patternPrint45;

        [Display(Name = "Print Pattern 4/5", Order = 2600, GroupName = NamingCategoryPatterns)]
        public bool Pattern45
        {
            get { return _patternPrint45; }
            set { _patternPrint45 = value; }
        }

        private bool _patternPrint67;

        [Display(Name = "Print Pattern 6/7", Order = 2700, GroupName = NamingCategoryPatterns)]
        public bool Pattern67
        {
            get { return _patternPrint67; }
            set { _patternPrint67 = value; }
        }

        private bool _patternPrintNeutral;

        [Display(Name = "Print Pattern Neutral", Order = 2710, GroupName = NamingCategoryPatterns)]
        [RefreshProperties(RefreshProperties.All)]
        public bool PatternNeutral
        {
            get { return _patternPrintNeutral; }
            set { _patternPrintNeutral = value; }
        }

        //no longer used - just hide for now
        private bool _patternDrawGhostDot;

        [Display(Name = "Print Ghost Dot", Order = 2750, GroupName = NamingCategoryPatterns)]
        //[RefreshProperties(RefreshProperties.All)]
        [Browsable(false)]
        public bool PatternDrawGhost
        {
            get { return _patternDrawGhostDot; }
            set { _patternDrawGhostDot = value; }
        }

        private eTachEonGhostDotMethods _patternGhostDotMethod;

        [Display(Name = "Print Ghost Dot", Order = 2760, GroupName = NamingCategoryPatterns)]
        public eTachEonGhostDotMethods PatternGhostDotMethod
        {
            get { return _patternGhostDotMethod; }
            set { _patternGhostDotMethod = value; }
        }

        #endregion

        #region Scores

        private bool _scoreRender;

        [Display(Name = "Render", Order = 10, GroupName = NamingCategoryScores, Description = "Render on Chart")]
        public bool ScoreRender
        {
            get { return _scoreRender; }
            set { _scoreRender = value; }
        }

        [Display(Name = "ATR Value", Order = 130, GroupName = NamingCategoryScores)]
        [Range(1, int.MaxValue)]
        public int ScoreAtrValue
        {
            get { return __settings.AtrValue; }
            set { __settings.AtrValue = value; }
        }

        [Display(Name = "Scores Require Close", Order = 150, GroupName = NamingCategoryScores)]
        public bool ScoresRequireClose
        {
            get { return __settings.ScoresRequireClose; }
            set { __settings.ScoresRequireClose = value; }
        }

        [Display(Name = "Trading Times Enabled", Order = 200, GroupName = NamingCategoryScores, Description = "Limit Times for Scoring")]
        [RefreshProperties(RefreshProperties.All)]
        public bool ScoreTimes
        {
            get { return __settings.IsTimeRestricted; }
            set { __settings.IsTimeRestricted = value; }
        }

        [Display(Name = "Start", Order = 210, GroupName = NamingCategoryScores, Description = "24 Hour Format: HH:MM:SS")]
        public string ScoreTimesStart
        {
            get { return __settings.GetScoreStart(); }
            set
            {
                if (IndicatorSettings.IsValidString(value))
                    __settings.SetScoreStart(value);
            }
        }

        [Display(Name = "End", Order = 220, GroupName = NamingCategoryScores, Description = "24 Hour Format: HH:MM:SS")]
        public string ScoreTimesEnd
        {
            get { return __settings.GetScoreEnd(); }
            set
            {
                if (IndicatorSettings.IsValidString(value))
                    __settings.SetScoreEnd(value);
            }
        }

        #region pattern 1/2

        [Display(Name = "1/2 Method", Order = 1100, GroupName = NamingCategoryScores)]
        [RefreshProperties(RefreshProperties.All)]
        public NinjaTrader.NinjaScript.Indicators.TachEon.eTachEonScoreMethod TachEonScoreMethod12
        {
            get { return __settings.ScoreSettings[1].Method; }
            set { __settings.ScoreSettings[1].Method = value; }
        }

        [Display(Name = "1/2 ATR Target", Order = 1200, GroupName = NamingCategoryScores)]
        [Range(0, double.MaxValue)]
        public double ScoreAtrTarget12
        {
            get { return __settings.ScoreSettings[1].AtrTarget; }
            set { __settings.ScoreSettings[1].AtrTarget = value; }
        }

        [Display(Name = "1/2 ATR Stop", Order = 1300, GroupName = NamingCategoryScores)]
        [Range(0, double.MaxValue)]
        public double ScoreAtrStop12
        {
            get { return __settings.ScoreSettings[1].AtrStop; }
            set { __settings.ScoreSettings[1].AtrStop = value; }
        }

        [Display(Name = "1/2 Tick Target", Order = 1400, GroupName = NamingCategoryScores)]
        [Range(0, int.MaxValue)]
        public int ScoreTickTarget12
        {
            get { return __settings.ScoreSettings[1].TickTarget; }
            set { __settings.ScoreSettings[1].TickTarget = value; }
        }

        [Display(Name = "1/2 Tick Stop", Order = 1500, GroupName = NamingCategoryScores)]
        [Range(0, int.MaxValue)]
        public int ScoreTickStop12
        {
            get { return __settings.ScoreSettings[1].TickStop; }
            set { __settings.ScoreSettings[1].TickStop = value; }
        }

        #endregion

        #region pattern 4/5

        [Display(Name = "4/5 Method", Order = 2100, GroupName = NamingCategoryScores)]
        [RefreshProperties(RefreshProperties.All)]
        public NinjaTrader.NinjaScript.Indicators.TachEon.eTachEonScoreMethod TachEonScoreMethod45
        {
            get { return __settings.ScoreSettings[2].Method; }
            set { __settings.ScoreSettings[2].Method = value; }
        }

        [Display(Name = "4/5 ATR Target", Order = 2200, GroupName = NamingCategoryScores)]
        [Range(0, double.MaxValue)]
        public double ScoreAtrTarget45
        {
            get { return __settings.ScoreSettings[2].AtrTarget; }
            set { __settings.ScoreSettings[2].AtrTarget = value; }
        }

        [Display(Name = "4/5 ATR Stop", Order = 2300, GroupName = NamingCategoryScores)]
        [Range(0, double.MaxValue)]
        public double ScoreAtrStop45
        {
            get { return __settings.ScoreSettings[2].AtrStop; }
            set { __settings.ScoreSettings[2].AtrStop = value; }
        }

        [Display(Name = "4/5 Tick Target", Order = 2400, GroupName = NamingCategoryScores)]
        [Range(0, int.MaxValue)]
        public int ScoreTickTarget45
        {
            get { return __settings.ScoreSettings[2].TickTarget; }
            set { __settings.ScoreSettings[2].TickTarget = value; }
        }

        [Display(Name = "4/5 Tick Stop", Order = 2500, GroupName = NamingCategoryScores)]
        [Range(0, int.MaxValue)]
        public int ScoreTickStop45
        {
            get { return __settings.ScoreSettings[2].TickStop; }
            set { __settings.ScoreSettings[2].TickStop = value; }
        }

        #endregion

        #region pattern 6/7

        [Display(Name = "6/7 Method", Order = 3100, GroupName = NamingCategoryScores)]
        [RefreshProperties(RefreshProperties.All)]
        public NinjaTrader.NinjaScript.Indicators.TachEon.eTachEonScoreMethod TachEonScoreMethod67
        {
            get { return __settings.ScoreSettings[3].Method; }
            set { __settings.ScoreSettings[3].Method = value; }
        }

        [Display(Name = "6/7 ATR Target", Order = 3200, GroupName = NamingCategoryScores)]
        [Range(0, double.MaxValue)]
        public double ScoreAtrTarget67
        {
            get { return __settings.ScoreSettings[3].AtrTarget; }
            set { __settings.ScoreSettings[3].AtrTarget = value; }
        }

        [Display(Name = "6/7 ATR Stop", Order = 3300, GroupName = NamingCategoryScores)]
        [Range(0, double.MaxValue)]
        public double ScoreAtrStop67
        {
            get { return __settings.ScoreSettings[3].AtrStop; }
            set { __settings.ScoreSettings[3].AtrStop = value; }
        }

        [Display(Name = "6/7 Tick Target", Order = 3400, GroupName = NamingCategoryScores)]
        [Range(0, int.MaxValue)]
        public int ScoreTickTarget67
        {
            get { return __settings.ScoreSettings[3].TickTarget; }
            set { __settings.ScoreSettings[3].TickTarget = value; }
        }

        [Display(Name = "6/7 Tick Stop", Order = 3500, GroupName = NamingCategoryScores)]
        [Range(0, int.MaxValue)]
        public int ScoreTickStop67
        {
            get { return __settings.ScoreSettings[3].TickStop; }
            set { __settings.ScoreSettings[3].TickStop = value; }
        }

        #endregion

        #region pattern all compute

        [Display(Name = "Neutral Pattern Method", Order = 4100, GroupName = NamingCategoryScores)]
        [RefreshProperties(RefreshProperties.All)]
        public NinjaTrader.NinjaScript.Indicators.TachEon.eTachEonScoreMethod TachEonScoreMethodAll
        {
            get { return __settings.ScoreSettings[0].Method; }
            set { __settings.ScoreSettings[0].Method = value; }
        }

        [Display(Name = "Neutral Pattern ATR Target", Order = 4200, GroupName = NamingCategoryScores)]
        [Range(0, double.MaxValue)]
        public double ScoreAtrTargetAll
        {
            get { return __settings.ScoreSettings[0].AtrTarget; }
            set { __settings.ScoreSettings[0].AtrTarget = value; }
        }

        [Display(Name = "Neutral Pattern ATR Stop", Order = 4300, GroupName = NamingCategoryScores)]
        [Range(0, double.MaxValue)]
        public double ScoreAtrStopAll
        {
            get { return __settings.ScoreSettings[0].AtrStop; }
            set { __settings.ScoreSettings[0].AtrStop = value; }
        }

        [Display(Name = "Neutral Pattern Tick Target", Order = 4400, GroupName = NamingCategoryScores)]
        [Range(0, int.MaxValue)]
        public int ScoreTickTargetAll
        {
            get { return __settings.ScoreSettings[0].TickTarget; }
            set { __settings.ScoreSettings[0].TickTarget = value; }
        }

        [Display(Name = "Neutral Pattern Tick Stop", Order = 4500, GroupName = NamingCategoryScores)]
        [Range(0, int.MaxValue)]
        public int ScoreTickStopAll
        {
            get { return __settings.ScoreSettings[0].TickStop; }
            set { __settings.ScoreSettings[0].TickStop = value; }
        }

        #endregion

        #region pens
        [XmlIgnore]
        [Display(Name = NamingScoreSuccess + NamingColor, Order = 8100, GroupName = NamingCategoryScores)]
        public System.Windows.Media.Brush ScoreSuccessColor { get; set; }

        [Browsable(false)]
        public string ScoreSuccessColorSerialize
        {
            get { return Serialize.BrushToString(ScoreSuccessColor); }
            set { ScoreSuccessColor = Serialize.StringToBrush(value); }
        }

        [Display(Name = NamingScoreSuccess + NamingStyle, Order = 8200, GroupName = NamingCategoryScores)]
        public SharpDX.Direct2D1.DashStyle ScoreSuccessStyle { get; set; }

        [Display(Name = NamingScoreSuccess + NamingWidth, Order = 8300, GroupName = NamingCategoryScores)]
        [Range(1, 10)]
        public int ScoreSuccessWidth { get; set; }

        [XmlIgnore]
        [Display(Name = NamingScoreFailure + NamingColor, Order = 9100, GroupName = NamingCategoryScores)]
        public System.Windows.Media.Brush ScoreFailureColor { get; set; }

        [Browsable(false)]
        public string ScoreFailureColorSerialize
        {
            get { return Serialize.BrushToString(ScoreFailureColor); }
            set { ScoreFailureColor = Serialize.StringToBrush(value); }
        }

        [Display(Name = NamingScoreFailure + NamingStyle, Order = 9200, GroupName = NamingCategoryScores)]
        public SharpDX.Direct2D1.DashStyle ScoreFailureStyle { get; set; }

        [Display(Name = NamingScoreFailure + NamingWidth, Order = 9300, GroupName = NamingCategoryScores)]
        [Range(1, 10)]
        public int ScoreFailureWidth { get; set; }
        #endregion

        #endregion

        #region swings

        [Display(Name = "Strength for Pattern 1/2", Order = 1000, GroupName = NamingCategorySwings)]
        [Browsable(false)]
        public int SwingStrength12
        {
            get { return __settings.GetSwing(1); }
            set { __settings.SetSwing(1, value); }
        }

        [Display(Name = "Strength for Pattern 4/5", Order = 1100, GroupName = NamingCategorySwings)]
        public int SwingStrength45
        {
            get { return __settings.GetSwing(2); }
            set { __settings.SetSwing(2, value); }
        }

        [Display(Name = "Strength for Pattern 6/7", Order = 1200, GroupName = NamingCategorySwings)]
        public int SwingStrength67
        {
            get { return __settings.GetSwing(3); }
            set { __settings.SetSwing(3, value); }
        }

        #endregion

        #region slope

        [Display(Name = "Method", Order = 1000, GroupName = NamingCategorySlope, Description = "Slope Line Method")]
        public NinjaTrader.NinjaScript.Indicators.TachEon.eTachEonMultiSlope SlopeMethod
        {
            get { return __settings.SlopeMethod; }
            set { __settings.SlopeMethod = value; }
        }

        [Display(Name = "Period", Order = 1050, GroupName = NamingCategorySlope, Description = "Slope Line Period")]
        [Range(2, int.MaxValue)]
        public int SlopePeriod
        {
            get { return __settings.SlopePeriod; }
            set { __settings.SlopePeriod = value; }
        }

        [Display(Name = NamingStyle, Order = 3010, GroupName = NamingCategorySlope)]
        public PlotStyle SlopePlot { get; set; }

        [Display(Name = NamingWidth, Order = 3020, GroupName = NamingCategorySlope)]
        [Range(1, 10)]
        public int SlopeWidth { get; set; }

        [XmlIgnore]
        [Display(Name = NamingColor + "for Long", Order = 3031, GroupName = NamingCategorySlope)]
        public System.Windows.Media.Brush SlopeLongColor { get; set; }

        [Browsable(false)]
        public string SlopeLineLongColorSerialize
        {
            get { return Serialize.BrushToString(SlopeLongColor); }
            set { SlopeLongColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = NamingColor + "for Short", Order = 3032, GroupName = NamingCategorySlope)]
        public System.Windows.Media.Brush SlopeShortColor { get; set; }

        [Browsable(false)]
        public string SlopeLineShortColorSerialize
        {
            get { return Serialize.BrushToString(SlopeShortColor); }
            set { SlopeShortColor = Serialize.StringToBrush(value); }
        }

        #endregion

        #region other

        private NinjaTrader.NinjaScript.Indicators.TachEon.TachEonForce.eForceMessages _userMessagePlacement;

        [Display(Name = "User Messages", Order = 2000, GroupName = NamingCategoryOther, Description = "Location for User Messages")]
        public NinjaTrader.NinjaScript.Indicators.TachEon.TachEonForce.eForceMessages UserMessagePlacement
        {
            get { return _userMessagePlacement; }
            set { _userMessagePlacement = value; }
        }

        private int _futureSignals;

        [Display(Name = "Future Force Signals Displayed", Order = 5000, GroupName = NamingCategoryOther, Description = "Recommended count is 5.")]
        [Range(1, int.MaxValue)]
        public int FutureSignals
        {
            get { return _futureSignals; }
            set { _futureSignals = value; }
        }

        private int _maxDraw;

        [Display(Name = "Force Signal Draw Maximum", Order = 5000, GroupName = NamingCategoryOther, Description = "Used for performance purposes only.")]
        [Range(1, int.MaxValue)]
        public int MaxDrawPeriod
        {
            get { return _maxDraw; }
            set { _maxDraw = value; }
        }

        [Display(Name = "Display In Data Box", Order = 6000, GroupName = NamingCategoryOther)]
        public bool ShowInDatabox { get; set; }

        #endregion

        #region programmatic

        [NinjaScriptProperty]
        [Display(Name = "Signal Data", Order = 1000, GroupName = NamingCategoryProg, Description = "For instantiation inside other indicators.")]
        public string ExternalSignal { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Angle Data", Order = 5000, GroupName = NamingCategoryProg, Description = "For instantiation inside other indicators.")]
        public string ExternalAngle { get; set; }

        /// <summary>
        /// static so the check is done only once across an entire NT session
        /// </summary>
        private static bool __checkedLicense = File.Exists("c:\\_$license.dtw");

        #endregion

        #endregion Indicator Properties

        public TachEonForce()
        {
            Licensing();
        }

        private void Licensing(bool save = false)
        {
            const string mod = "TachEonForce";
            const string vend = "BackToTheFutureTrading";
            const string email = "ron@backtothefuturetrading.com";
            const string url = "www.backtothefuturetrading.com";

            if (!__checkedLicense && !__debugging)
            {
                VendorLicense(vend, mod, url, email);   //verify vendor licence
                __checkedLicense = save;
            }
        }
    }

    public class ForceConverter : IndicatorBaseConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            return base.CreateInstance(context, propertyValues);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
        {
            return base.GetCreateInstanceSupported(context);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object component, Attribute[] attrs)
        {
            var indicator = component as TachEonForce;

            var propertyDescriptorCollection = base.GetPropertiesSupported(context)
                                                ? base.GetProperties(context, component, attrs)
                                                : TypeDescriptor.GetProperties(component, attrs);

            if (indicator == null || propertyDescriptorCollection == null)
                return propertyDescriptorCollection;

            // score times
            {
                var var1 = propertyDescriptorCollection["ScoreTimesStart"];
                var var2 = propertyDescriptorCollection["ScoreTimesEnd"];

                propertyDescriptorCollection.Remove(var1);
                propertyDescriptorCollection.Remove(var2);

                if (indicator.ScoreTimes)
                {
                    propertyDescriptorCollection.Add(var1);
                    propertyDescriptorCollection.Add(var2);
                }
            }

            // pattern methods
            {
                foreach (var item in "12~45~67~All".Split("~".ToCharArray()))
                {
                    var var1 = propertyDescriptorCollection["ScoreAtrTarget" + item];
                    var var2 = propertyDescriptorCollection["ScoreAtrStop" + item];
                    var var3 = propertyDescriptorCollection["ScoreTickTarget" + item];
                    var var4 = propertyDescriptorCollection["ScoreTickStop" + item];

                    propertyDescriptorCollection.Remove(var1);
                    propertyDescriptorCollection.Remove(var2);
                    propertyDescriptorCollection.Remove(var3);
                    propertyDescriptorCollection.Remove(var4);

                    var method = item == "All" ? indicator.TachEonScoreMethodAll : (item == "12" ? indicator.TachEonScoreMethod12 : (item == "45" ? indicator.TachEonScoreMethod45 : indicator.TachEonScoreMethod67));
                    switch (method)
                    {
                        case eTachEonScoreMethod.ATR:
                            propertyDescriptorCollection.Add(var1);
                            propertyDescriptorCollection.Add(var2);
                            break;
                        case eTachEonScoreMethod.Dot:
                            break;
                        case eTachEonScoreMethod.Tick:
                            propertyDescriptorCollection.Add(var3);
                            propertyDescriptorCollection.Add(var4);
                            break;
                        case eTachEonScoreMethod.Off:
                            break;
                    }
                }
            }

            // pattern printing
            {
                if (indicator.PatternNeutral)
                {
                    propertyDescriptorCollection.Remove(propertyDescriptorCollection["Pattern12"]);
                    propertyDescriptorCollection.Remove(propertyDescriptorCollection["Pattern45"]);
                    propertyDescriptorCollection.Remove(propertyDescriptorCollection["Pattern67"]);
                    propertyDescriptorCollection.Remove(propertyDescriptorCollection["PatternGhostDotMethod"]);
                }
            }

            return propertyDescriptorCollection;
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return base.GetStandardValues(context);
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return base.GetStandardValuesExclusive(context);
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return base.GetStandardValuesSupported(context);
        }

        public override bool IsValid(ITypeDescriptorContext context, object value)
        {
            return base.IsValid(context, value);
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    public class AngleStartOnBoolConverter : TypeConverter
    {
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> values = new List<string>() { "On Open Value", "On Close Value" };
            return new StandardValuesCollection(values);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return value.ToString() == "On Open Value" ? true : false;
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return (bool)value ? "On Open Value" : "On Close Value";
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        { return true; }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        { return true; }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        { return true; }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        { return true; }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TachEon.TachEonForce[] cacheTachEonForce;
		public TachEon.TachEonForce TachEonForce(string externalSignal, string externalAngle)
		{
			return TachEonForce(Input, externalSignal, externalAngle);
		}

		public TachEon.TachEonForce TachEonForce(ISeries<double> input, string externalSignal, string externalAngle)
		{
			if (cacheTachEonForce != null)
				for (int idx = 0; idx < cacheTachEonForce.Length; idx++)
					if (cacheTachEonForce[idx] != null && cacheTachEonForce[idx].ExternalSignal == externalSignal && cacheTachEonForce[idx].ExternalAngle == externalAngle && cacheTachEonForce[idx].EqualsInput(input))
						return cacheTachEonForce[idx];
			return CacheIndicator<TachEon.TachEonForce>(new TachEon.TachEonForce(){ ExternalSignal = externalSignal, ExternalAngle = externalAngle }, input, ref cacheTachEonForce);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TachEon.TachEonForce TachEonForce(string externalSignal, string externalAngle)
		{
			return indicator.TachEonForce(Input, externalSignal, externalAngle);
		}

		public Indicators.TachEon.TachEonForce TachEonForce(ISeries<double> input , string externalSignal, string externalAngle)
		{
			return indicator.TachEonForce(input, externalSignal, externalAngle);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TachEon.TachEonForce TachEonForce(string externalSignal, string externalAngle)
		{
			return indicator.TachEonForce(Input, externalSignal, externalAngle);
		}

		public Indicators.TachEon.TachEonForce TachEonForce(ISeries<double> input , string externalSignal, string externalAngle)
		{
			return indicator.TachEonForce(input, externalSignal, externalAngle);
		}
	}
}

#endregion
