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

using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System.Windows.Media;

#endregion

//This namespace holds Add ons in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.AddOns.DtwAddOns
{
	 public interface IDtwDX : IDisposable
    {

    }

    public class DtwDrawTools : IDisposable
    {
        private readonly Dictionary<string, DtwBrushDXColor> _colors = new Dictionary<string, DtwBrushDXColor>();
        private readonly Dictionary<string, DtwBrushDXStyle> _styles = new Dictionary<string, DtwBrushDXStyle>();
        private readonly Dictionary<string, DtwTextDX> _textFormats = new Dictionary<string, DtwTextDX>();
        private readonly Dictionary<string, DtwSolidColorBrushDX> _brushes = new Dictionary<string, DtwSolidColorBrushDX>();

        public DtwDrawTools()
        {
            _colors.Add("__default", new DtwBrushDXColor(Brushes.AntiqueWhite));
            _styles.Add("__default", new DtwBrushDXStyle(SharpDX.Direct2D1.DashStyle.Solid));
            _textFormats.Add("__default", new DtwTextDX("Arial", 12));
            _brushes.Add("__default", new DtwSolidColorBrushDX(Brushes.AntiqueWhite, SharpDX.Direct2D1.DashStyle.Solid, 1));
        }

        public DtwSolidColorBrushDX GetBrush(string name)
        {
            if (_brushes.ContainsKey(name))
                return _brushes[name];
            return _brushes["__default"];
        }

        public void AddBrush(string name, System.Windows.Media.Brush brush, SharpDX.Direct2D1.DashStyle dashStyle, float width)
        {
            _brushes.Add(name, new DtwSolidColorBrushDX(brush, dashStyle, width));
        }

        public void AddBrush(string name, DtwSolidColorBrushDX item)
        {
            _brushes.Add(name, item);
        }

        public DtwBrushDXColor GetColor(string name)
        {
            if (_colors.ContainsKey(name))
                return _colors[name];
            return _colors["__default"];
        }

        public void AddColor(string name, System.Windows.Media.Brush brush)
        {
            _colors.Add(name, new DtwBrushDXColor(brush));
        }

        public void AddColor(string name, DtwBrushDXColor item)
        {
            _colors.Add(name, item);
        }

        public DtwBrushDXStyle GetStyle(string name)
        {
            if (_styles.ContainsKey(name))
                return _styles[name];
            return _styles["__default"];
        }

        public void AddStyle(string name, SharpDX.Direct2D1.DashStyle dashStyle)
        {
            _styles.Add(name, new DtwBrushDXStyle(dashStyle));
        }

        public void AddStyle(string name, DtwBrushDXStyle item)
        {
            _styles.Add(name, item);
        }

        public DtwTextDX GetTextFormat(string name)
        {
            if (_textFormats.ContainsKey(name))
                return _textFormats[name];
            return _textFormats["__default"];
        }

        public void AddTextFormat(string name, DtwTextDX item)
        {
            _textFormats.Add(name, item);
        }

        public void AddTextFormat(string name, string fontFamily, float fontSize)
        {
            _textFormats.Add(name, new DtwTextDX(fontFamily, fontSize));
        }

        public virtual void Dispose()
        {
            foreach (var item in _colors.Values.ToList())
            {
                item.Dispose();
            }
            foreach (var item in _styles.Values.ToList())
            {
                item.Dispose();
            }
            foreach (var item in _textFormats.Values.ToList())
            {
                item.Dispose();
            }
            foreach (var item in _brushes.Values.ToList())
            {
                item.Dispose();
            }
        }
    }

    public class DtwBrush : IDtwDX
    {
        public System.Windows.Media.Brush Brush;
        public SharpDX.Direct2D1.DashStyle DashStyle;
        public float Width;

        public DtwBrush(System.Windows.Media.Brush brush, SharpDX.Direct2D1.DashStyle dashStyle, float width)
        {
            Brush = brush;
            DashStyle = dashStyle;
            Width = width;
        }

        public virtual void Dispose()
        {

        }
    }

    public class DtwBrushDXColor : IDtwDX
    {
        public readonly SharpDX.Color BrushColor;

        private SharpDX.Direct2D1.SolidColorBrush _dxBrush;
        public SharpDX.Direct2D1.SolidColorBrush Brush(RenderTarget rt)
        {
            if (_dxBrush != null)
                Dispose();
            _dxBrush = new SharpDX.Direct2D1.SolidColorBrush(rt, BrushColor);
            return _dxBrush;
        }

        public virtual void Dispose()
        {
            if (_dxBrush != null)
            {
                _dxBrush.Dispose();
                _dxBrush = null;
            }
        }

        public DtwBrushDXColor(System.Windows.Media.Brush brush)
        {
            var color = ((System.Windows.Media.SolidColorBrush)brush).Color;
            BrushColor = new SharpDX.Color(color.R, color.G, color.B, color.A);
        }
    }

    public class DtwBrushDXStyle : IDtwDX
    {
        private readonly StrokeStyleProperties _properties;

        private StrokeStyle _style;

        public StrokeStyle StrokeStyle
        {
            get
            {
                if (_style == null)
                    _style = new StrokeStyle(Core.Globals.D2DFactory, _properties);
                return _style;
            }
        }

        public DtwBrushDXStyle(SharpDX.Direct2D1.DashStyle dashStyle)
        {
            _properties = new StrokeStyleProperties();
            _properties.DashStyle = dashStyle;
        }

        public virtual void Dispose()
        {
            if (_style != null)
            {
                _style.Dispose();
                _style = null;
            }
        }
    }

    public class DtwSolidColorBrushDX : IDtwDX
    {
        private DtwBrushDXColor _color;

        public DtwBrushDXColor Color
        {
            get { return _color; }
        }

        private DtwBrushDXStyle _style;

        public DtwBrushDXStyle Style
        {
            get { return _style; }
        }

        public StrokeStyle StrokeStyle
        {
            get { return _style.StrokeStyle; }
        }

        public readonly float Width;

        public SharpDX.Direct2D1.SolidColorBrush GetBrush(RenderTarget renderTarget)
        {
            return Color.Brush(renderTarget);
        }

        public DtwSolidColorBrushDX(System.Windows.Media.Brush brush, SharpDX.Direct2D1.DashStyle dashStyle, float width)
        {
            _color = new DtwBrushDXColor(brush);
            _style = new DtwBrushDXStyle(dashStyle);
            Width = width;
        }

        public virtual void Dispose()
        {
            _color.Dispose();
            _style.Dispose();
        }
    }

    public class DtwTextDX : IDtwDX
    {
        private readonly string _family;
        private readonly float _size;
        private readonly SharpDX.DirectWrite.FontWeight _weight;
        private readonly SharpDX.DirectWrite.FontStyle _style;
        private readonly SharpDX.DirectWrite.FontStretch _stretch;

        private TextFormat _font;

        public TextFormat TextFormat
        {
            get
            {
                if (_font == null)
                    _font = new TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, _family, _weight, _style, _stretch, _size);
                return _font;
            }
        }

        public DtwTextDX(string fontFamily, SharpDX.DirectWrite.FontWeight fontWeight, SharpDX.DirectWrite.FontStyle fontStyle, SharpDX.DirectWrite.FontStretch fontStretch, float fontSize)
        {
            _family = fontFamily;
            _weight = fontWeight;
            _style = fontStyle;
            _stretch = fontStretch;
            _size = fontSize;
        }

        public DtwTextDX(string fontFamily, SharpDX.DirectWrite.FontWeight fontWeight, SharpDX.DirectWrite.FontStyle fontStyle, float fontSize)
            : this(fontFamily, fontWeight, fontStyle, SharpDX.DirectWrite.FontStretch.Normal, fontSize)
        {
        }

        public DtwTextDX(string fontFamily, SharpDX.DirectWrite.FontWeight fontWeight, float fontSize)
            : this(fontFamily, fontWeight, SharpDX.DirectWrite.FontStyle.Normal, SharpDX.DirectWrite.FontStretch.Normal, fontSize)
        {
        }

        public DtwTextDX(string fontFamily, float fontSize)
            : this(fontFamily, SharpDX.DirectWrite.FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Normal, SharpDX.DirectWrite.FontStretch.Normal, fontSize)
        {
        }

        public virtual void Dispose()
        {
            if (_font != null)
            {
                _font.Dispose();
                _font = null;
            }
        }
    }

    public static class DtwDraw
	{
        #region DrawLine/DrawArrow

        private static void DrawLineArrow(RenderTarget renderTarget, DtwBrushDXColor color, DtwBrushDXStyle style, float width, Vector2 start, Vector2 end, bool withArrow)
        {
            renderTarget.DrawLine(start, end, color.Brush(renderTarget), width, style.StrokeStyle);
            if (!withArrow) return;
            var diffX = end.X - start.X;
            var diffY = end.Y - start.Y;
            var length = Math.Sqrt(Math.Pow(diffX, 2) + Math.Pow(diffY, 2));

            var nX = diffX / length;
            var nY = diffY / length;

            var size = width * 2;
            var aX = size * (-nY - nX);
            var aY = size * (nX - nY);

            var pathGeom = new SharpDX.Direct2D1.PathGeometry(NinjaTrader.Core.Globals.D2DFactory);
            var geomSink = pathGeom.Open();
            geomSink.BeginFigure(end, FigureBegin.Filled);
            geomSink.AddLine(new Vector2((int)(end.X + aX), (int)(end.Y + aY)));
            geomSink.AddLine(new Vector2((int)(end.X - aY), (int)(end.Y + aX)));
            geomSink.EndFigure(FigureEnd.Closed);
            geomSink.Close();
            renderTarget.FillGeometry(pathGeom, color.Brush(renderTarget));
            geomSink.Dispose();
            pathGeom.Dispose();
        }

        #region DrawLine

        public static void DrawLine(RenderTarget renderTarget, DtwSolidColorBrushDX brush, Vector2 start, Vector2 end)
        {
            DrawLineArrow(renderTarget, brush.Color, brush.Style, brush.Width, start, end, false);
        }

        public static void DrawLine(RenderTarget renderTarget, DtwSolidColorBrushDX brush, Vector2 start, float endX, float endY)
        {
            DrawLine(renderTarget, brush, start, new Vector2(endX, endY));
        }

        public static void DrawLine(RenderTarget renderTarget, DtwSolidColorBrushDX brush, float startX, float startY, Vector2 end)
        {
            DrawLine(renderTarget, brush, new Vector2(startX, startY), end);
        }

        public static void DrawLine(RenderTarget renderTarget, DtwSolidColorBrushDX brush, float startX, float startY, float endX, float endY)
        {
            DrawLine(renderTarget, brush, new Vector2(startX, startY), new Vector2(endX, endY));
        }

        public static void DrawLine(RenderTarget renderTarget, ChartControl chartControl, ChartScale chartScale, DtwSolidColorBrushDX brush,
            DateTime startTime, double startPrice, DateTime endTime, double endPrice)
        {
            var startX = chartControl.GetXByTime(startTime);
            var startY = chartScale.GetYByValue(startPrice);
            var endX = chartControl.GetXByTime(endTime);
            var endY = chartScale.GetYByValue(endPrice);
            DrawLine(renderTarget, brush, startX, startY, endX, endY);
        }

        #endregion

        #region DrawArrow

        public static void DrawArrow(RenderTarget renderTarget, DtwSolidColorBrushDX brush, Vector2 start, Vector2 end)
        {
            DrawLineArrow(renderTarget, brush.Color, brush.Style, brush.Width, start, end, true);
        }

        public static void DrawArrow(RenderTarget renderTarget, DtwSolidColorBrushDX brush, Vector2 start, float endX, float endY)
        {
            DrawArrow(renderTarget, brush, start, new Vector2(endX, endY));
        }

        public static void DrawArrow(RenderTarget renderTarget, DtwSolidColorBrushDX brush, float startX, float startY, Vector2 end)
        {
            DrawArrow(renderTarget, brush, new Vector2(startX, startY), end);
        }

        public static void DrawArrow(RenderTarget renderTarget, DtwSolidColorBrushDX brush, float startX, float startY, float endX, float endY)
        {
            DrawArrow(renderTarget, brush, new Vector2(startX, startY), new Vector2(endX, endY));
        }

        public static void DrawArrow(RenderTarget renderTarget, ChartControl chartControl, ChartScale chartScale, DtwSolidColorBrushDX brush,
            DateTime startTime, double startPrice, DateTime endTime, double endPrice)
        {
            var startX = chartControl.GetXByTime(startTime);
            var startY = chartScale.GetYByValue(startPrice);
            var endX = chartControl.GetXByTime(endTime);
            var endY = chartScale.GetYByValue(endPrice);
            DrawArrow(renderTarget, brush, startX, startY, endX, endY);
        }

        #endregion

        #endregion
	}
}
