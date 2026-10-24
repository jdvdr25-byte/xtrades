using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using TradingHub.Data;
using TradingHub.Rendering.Context;
using TradingHub.Rendering.Tools;

namespace TradingHub.Indicators.Native;

/// <summary>
/// Perfil de volumen por sesión con el conjunto completo de opciones de estilo Bidstrata:
/// tipos de barra, área de valor, POC developing, niveles naked, nodos HVN/LVN, alineación y más.
/// </summary>
[DisplayName("Perfil de volumen por sesión")]
[Category(IndicatorCategories.VolumeOrderFlow)]
public class SessionVolumeProfile : Indicator
{
    // ── Enums ────────────────────────────────────────────────────────────────

    public enum ProfilePeriod
    {
        [Display(Name = "Sesión (límite del feed)")] Session,
        [Display(Name = "Semana")]                   Week,
        [Display(Name = "Toda la historia visible")] Visible,
        [Display(Name = "Sesión personalizada")]     Custom,
    }

    public enum SessionTimezone
    {
        [Display(Name = "UTC")]              Utc,
        [Display(Name = "CT (Chicago, CME)")] Central,
        [Display(Name = "ET (New York, NYSE)")] Eastern,
    }

    public enum ProfileKind
    {
        [Display(Name = "Volumen total")]      Volume,
        [Display(Name = "Delta")]              Delta,
        [Display(Name = "Bid / Ask")]          BidAsk,
        [Display(Name = "Bid / Ask + total")]  BidAskTotal,
        [Display(Name = "Delta + total")]      DeltaTotal,
        [Display(Name = "Delta (%)")]          DeltaPct,
    }

    public enum BarStyle
    {
        [Display(Name = "Shades — tonos del DOM")] Shades,
        [Display(Name = "Voltage — look anterior")] Voltage,
    }

    public enum BarBorderMode
    {
        [Display(Name = "Auto")]    Auto,
        [Display(Name = "Siempre")] On,
        [Display(Name = "Nunca")]   Off,
    }

    public enum BarAlignment
    {
        [Display(Name = "Borde izquierdo")] Left,
        [Display(Name = "Borde derecho")]   Right,
    }

    public enum ExtendMode
    {
        [Display(Name = "No extender")]  None,
        [Display(Name = "Izquierda")]    Left,
        [Display(Name = "Derecha")]      Right,
        [Display(Name = "Ambos lados")]  Both,
    }

    public enum VolumeNodeMethod
    {
        [Display(Name = "Percentil")]        Percentile,
        [Display(Name = "% del máximo")]     PercentOfMax,
        [Display(Name = "Desviación estándar")] StdDev,
    }

    public enum LineStyle
    {
        [Display(Name = "Sólida")]  Solid,
        [Display(Name = "Guiones")] Dashed,
        [Display(Name = "Puntos")]  Dotted,
    }

    // ── Profile inner class ───────────────────────────────────────────────────

    private sealed class Profile
    {
        public int StartBar;
        public int EndBar;
        public int CommittedEnd = -1;
        public readonly Dictionary<decimal, (decimal Bid, decimal Ask)> Committed = new();
        public SortedDictionary<decimal, (decimal Bid, decimal Ask)> Levels = new();
        public decimal Poc;
        public decimal Vah;
        public decimal Val;
        public decimal MaxVolume;
        public decimal Total;
        // (bar index, POC price at that bar) — for developing POC trace
        public readonly List<(int Bar, decimal Poc)> PocTrace = new();
    }

    // ── Private fields ───────────────────────────────────────────────────────

    private readonly List<Profile> _profiles = new();
    private ProfilePeriod _period = ProfilePeriod.Session;
    private int _valueArea = 70;
    private int _customHour = 8;
    private int _customMinute = 30;
    private SessionTimezone _customTimezone = SessionTimezone.Central;
    private SessionSettings? _customSettings;

    // ── Constructor ───────────────────────────────────────────────────────────

    public SessionVolumeProfile() : base(true)
    {
        DataSeries[0].IsHidden = true;
        ((ValueDataSeries)DataSeries[0]).VisualType = VisualMode.Hide;
        EnableCustomDrawing = true;
        SubscribeToDrawingEvents(DrawingLayouts.Final);
        DrawAbovePrice = false;
    }

    // ── Cálculo ───────────────────────────────────────────────────────────────

    [Display(Name = "Período", GroupName = "Cálculo", Order = 10)]
    public ProfilePeriod Period
    {
        get => _period;
        set { _period = value; _customSettings = null; RecalculateValues(); }
    }

    [Display(Name = "Área de valor (%)", GroupName = "Cálculo", Order = 20)]
    [Range(10, 100)]
    public int ValueAreaPercent
    {
        get => _valueArea;
        set { _valueArea = Math.Clamp(value, 10, 100); RecalculateValues(); }
    }

    // ── Sesión personalizada ──────────────────────────────────────────────────

    [Display(Name = "Hora de inicio (0–23)", GroupName = "Sesión personalizada", Order = 21)]
    [Range(0, 23)]
    public int CustomHour
    {
        get => _customHour;
        set { _customHour = Math.Clamp(value, 0, 23); _customSettings = null; RecalculateValues(); }
    }

    [Display(Name = "Minuto de inicio (0–59)", GroupName = "Sesión personalizada", Order = 22)]
    [Range(0, 59)]
    public int CustomMinute
    {
        get => _customMinute;
        set { _customMinute = Math.Clamp(value, 0, 59); _customSettings = null; RecalculateValues(); }
    }

    [Display(Name = "Zona horaria", GroupName = "Sesión personalizada", Order = 23)]
    public SessionTimezone CustomTimezone
    {
        get => _customTimezone;
        set { _customTimezone = value; _customSettings = null; RecalculateValues(); }
    }

    // ── Barras ─────────────────────────────────────────────────────────────────

    [Display(Name = "Contenido de barras", GroupName = "Barras", Order = 100)]
    public ProfileKind Kind { get; set; } = ProfileKind.Volume;

    [Display(Name = "Estilo de barra (Bid/Ask)", GroupName = "Barras", Order = 101)]
    public BarStyle BarStyleMode { get; set; } = BarStyle.Shades;

    [Display(Name = "Alineación de barras", GroupName = "Barras", Order = 102)]
    public BarAlignment Alignment { get; set; } = BarAlignment.Left;

    [Display(Name = "Ancho máximo (% de la sesión)", GroupName = "Barras", Order = 103)]
    [Range(5, 100)]
    public int WidthPercent { get; set; } = 70;

    [Display(Name = "Opacidad de barras (%)", GroupName = "Barras", Order = 104)]
    [Range(10, 100)]
    public int BarOpacityPercent { get; set; } = 45;

    [Display(Name = "Atenuar fuera del área de valor", GroupName = "Barras", Order = 105)]
    public bool DimOutsideValueArea { get; set; } = true;

    [Display(Name = "Opacidad fuera del VA (%)", GroupName = "Barras", Order = 106)]
    [Range(0, 100)]
    public int OutsideVaOpacityPercent { get; set; } = 30;

    [Display(Name = "Borde de barra", GroupName = "Barras", Order = 107)]
    public BarBorderMode BarBorder { get; set; } = BarBorderMode.Auto;

    [Display(Name = "Mostrar valores en filas", GroupName = "Barras", Order = 108)]
    public bool ShowRowValues { get; set; } = false;

    [Display(Name = "Mostrar borde del perfil", GroupName = "Barras", Order = 109)]
    public bool ShowProfileBorder { get; set; } = false;

    // ── Colores ────────────────────────────────────────────────────────────────

    [Display(Name = "Color del perfil (volumen)", GroupName = "Colores", Order = 200)]
    public Color ProfileColor { get; set; } = Color.FromArgb(255, 110, 140, 190);

    [Display(Name = "Color del área de valor", GroupName = "Colores", Order = 201)]
    public Color ValueAreaColor { get; set; } = Color.FromArgb(255, 120, 160, 220);

    [Display(Name = "Color bid", GroupName = "Colores", Order = 202)]
    public Color BidColor { get; set; } = Color.FromArgb(255, 232, 69, 72);

    [Display(Name = "Color ask", GroupName = "Colores", Order = 203)]
    public Color AskColor { get; set; } = Color.FromArgb(255, 73, 209, 73);

    [Display(Name = "Delta positivo", GroupName = "Colores", Order = 204)]
    public Color DeltaPositiveColor { get; set; } = Color.FromArgb(255, 73, 209, 73);

    [Display(Name = "Delta negativo", GroupName = "Colores", Order = 205)]
    public Color DeltaNegativeColor { get; set; } = Color.FromArgb(255, 232, 69, 72);

    [Display(Name = "Color del borde del perfil", GroupName = "Colores", Order = 206)]
    public Color ProfileBorderColor { get; set; } = Color.FromArgb(255, 60, 80, 120);

    // ── POC ────────────────────────────────────────────────────────────────────

    [Display(Name = "Mostrar línea del POC", GroupName = "POC", Order = 300)]
    public bool ShowPocLine { get; set; } = true;

    [Display(Name = "Color del POC", GroupName = "POC", Order = 301)]
    public Color PocColor { get; set; } = Color.FromArgb(255, 255, 196, 0);

    [Display(Name = "Grosor del POC (px)", GroupName = "POC", Order = 302)]
    public float PocLineWeight { get; set; } = 1.5f;

    [Display(Name = "Estilo del POC", GroupName = "POC", Order = 303)]
    public LineStyle PocLineStyle { get; set; } = LineStyle.Solid;

    [Display(Name = "Extender línea del POC", GroupName = "POC", Order = 304)]
    public ExtendMode PocExtend { get; set; } = ExtendMode.Right;

    [Display(Name = "Etiqueta de precio del POC", GroupName = "POC", Order = 305)]
    public bool ShowPocPriceLabel { get; set; } = false;

    [Display(Name = "Niveles naked (POC/VAH/VAL sin tocar)", GroupName = "POC", Order = 306)]
    public bool ShowNakedLevels { get; set; } = false;

    [Display(Name = "Mostrar POC en desarrollo", GroupName = "POC", Order = 307)]
    public bool ShowDevelopingPoc { get; set; } = false;

    [Display(Name = "Color del POC en desarrollo", GroupName = "POC", Order = 308)]
    public Color DevelopingPocColor { get; set; } = Color.FromArgb(255, 255, 160, 64);

    [Display(Name = "Grosor del POC en desarrollo (px)", GroupName = "POC", Order = 309)]
    public float DevelopingPocWeight { get; set; } = 1f;

    [Display(Name = "Estilo del POC en desarrollo", GroupName = "POC", Order = 310)]
    public LineStyle DevelopingPocStyle { get; set; } = LineStyle.Dashed;

    // ── Área de valor ──────────────────────────────────────────────────────────

    [Display(Name = "Sombrear área de valor", GroupName = "Área de valor", Order = 400)]
    public bool ShadeValueArea { get; set; } = false;

    [Display(Name = "Color del relleno del VA", GroupName = "Área de valor", Order = 401)]
    public Color ValueAreaFillColor { get; set; } = Color.FromArgb(255, 90, 143, 220);

    [Display(Name = "Opacidad del relleno del VA (%)", GroupName = "Área de valor", Order = 402)]
    [Range(1, 50)]
    public int ValueAreaFillOpacityPercent { get; set; } = 12;

    [Display(Name = "Mostrar VAH / VAL", GroupName = "Área de valor", Order = 403)]
    public bool ShowValueAreaLines { get; set; } = true;

    [Display(Name = "Color de líneas VAH / VAL", GroupName = "Área de valor", Order = 404)]
    public Color ValueAreaLineColor { get; set; } = Color.FromArgb(255, 106, 150, 220);

    [Display(Name = "Grosor de líneas VA (px)", GroupName = "Área de valor", Order = 405)]
    public float VahValLineWeight { get; set; } = 1f;

    [Display(Name = "Estilo de líneas VA", GroupName = "Área de valor", Order = 406)]
    public LineStyle VahValLineStyle { get; set; } = LineStyle.Dashed;

    [Display(Name = "Extender VAH / VAL", GroupName = "Área de valor", Order = 407)]
    public ExtendMode VahValExtend { get; set; } = ExtendMode.None;

    [Display(Name = "Etiquetas de precio VAH / VAL", GroupName = "Área de valor", Order = 408)]
    public bool ShowVahValPriceLabels { get; set; } = false;

    // ── Nodos de volumen ───────────────────────────────────────────────────────

    [Display(Name = "Método HVN", GroupName = "Nodos de volumen", Order = 500)]
    public VolumeNodeMethod HvnMethod { get; set; } = VolumeNodeMethod.Percentile;

    [Display(Name = "Umbral HVN (%)", GroupName = "Nodos de volumen", Order = 501)]
    [Range(50, 99)]
    public int HvnCutoffPercent { get; set; } = 85;

    [Display(Name = "Mostrar zonas HVN", GroupName = "Nodos de volumen", Order = 502)]
    public bool ShowHvnZones { get; set; } = false;

    [Display(Name = "Color de zona HVN", GroupName = "Nodos de volumen", Order = 503)]
    public Color HvnZoneColor { get; set; } = Color.FromArgb(255, 42, 120, 214);

    [Display(Name = "Opacidad zona HVN (%)", GroupName = "Nodos de volumen", Order = 504)]
    [Range(2, 30)]
    public int HvnZoneOpacityPercent { get; set; } = 8;

    [Display(Name = "Mostrar línea pico HVN", GroupName = "Nodos de volumen", Order = 505)]
    public bool ShowHvnPeakLine { get; set; } = false;

    [Display(Name = "Color línea pico HVN", GroupName = "Nodos de volumen", Order = 506)]
    public Color HvnPeakColor { get; set; } = Color.FromArgb(255, 73, 209, 73);

    [Display(Name = "Grosor línea pico HVN (px)", GroupName = "Nodos de volumen", Order = 507)]
    public float HvnPeakWeight { get; set; } = 1.5f;

    [Display(Name = "Estilo línea pico HVN", GroupName = "Nodos de volumen", Order = 508)]
    public LineStyle HvnPeakStyle { get; set; } = LineStyle.Solid;

    [Display(Name = "Extender pico HVN", GroupName = "Nodos de volumen", Order = 509)]
    public ExtendMode HvnExtend { get; set; } = ExtendMode.None;

    [Display(Name = "Método LVN", GroupName = "Nodos de volumen", Order = 510)]
    public VolumeNodeMethod LvnMethod { get; set; } = VolumeNodeMethod.Percentile;

    [Display(Name = "Umbral LVN (%)", GroupName = "Nodos de volumen", Order = 511)]
    [Range(2, 40)]
    public int LvnCutoffPercent { get; set; } = 15;

    [Display(Name = "Mostrar zonas LVN", GroupName = "Nodos de volumen", Order = 512)]
    public bool ShowLvnZones { get; set; } = false;

    [Display(Name = "Color de zona LVN", GroupName = "Nodos de volumen", Order = 513)]
    public Color LvnZoneColor { get; set; } = Color.FromArgb(255, 232, 69, 72);

    [Display(Name = "Opacidad zona LVN (%)", GroupName = "Nodos de volumen", Order = 514)]
    [Range(2, 30)]
    public int LvnZoneOpacityPercent { get; set; } = 8;

    [Display(Name = "Mostrar línea valle LVN", GroupName = "Nodos de volumen", Order = 515)]
    public bool ShowLvnValleyLine { get; set; } = false;

    [Display(Name = "Color línea valle LVN", GroupName = "Nodos de volumen", Order = 516)]
    public Color LvnValleyColor { get; set; } = Color.FromArgb(255, 232, 69, 72);

    [Display(Name = "Grosor línea valle LVN (px)", GroupName = "Nodos de volumen", Order = 517)]
    public float LvnValleyWeight { get; set; } = 1.5f;

    [Display(Name = "Estilo línea valle LVN", GroupName = "Nodos de volumen", Order = 518)]
    public LineStyle LvnValleyStyle { get; set; } = LineStyle.Solid;

    [Display(Name = "Extender valle LVN", GroupName = "Nodos de volumen", Order = 519)]
    public ExtendMode LvnExtend { get; set; } = ExtendMode.None;

    // ── Calculation ───────────────────────────────────────────────────────────

    protected override void OnRecalculate()
    {
        _profiles.Clear();
        _customSettings = null;
    }

    protected override void OnCalculate(int bar, decimal value)
    {
        bool newProfile = bar == 0 || _profiles.Count == 0 || _period switch
        {
            ProfilePeriod.Session => IsNewSession(bar),
            ProfilePeriod.Week    => IsNewWeek(bar),
            ProfilePeriod.Custom  => IsNewCustomSession(bar),
            _                     => false,
        };

        if (newProfile && (_profiles.Count == 0 || _profiles[^1].StartBar != bar))
            _profiles.Add(new Profile { StartBar = bar, EndBar = bar, CommittedEnd = bar - 1 });

        var p = _profiles[^1];
        while (p.CommittedEnd < bar - 1)
        {
            p.CommittedEnd++;
            AddTo(p.Committed, p.CommittedEnd);
        }

        var levels = new Dictionary<decimal, (decimal Bid, decimal Ask)>(p.Committed);
        AddTo(levels, bar);
        p.Levels = new SortedDictionary<decimal, (decimal Bid, decimal Ask)>(levels);
        p.EndBar = bar;
        Finish(p);

        // Developing POC trace: keep last entry for this bar up to date
        if (p.PocTrace.Count > 0 && p.PocTrace[^1].Bar == bar)
        {
            p.PocTrace.RemoveAt(p.PocTrace.Count - 1);
        }
        p.PocTrace.Add((bar, p.Poc));
    }

    private SessionSettings GetCustomSettings()
    {
        if (_customSettings != null) return _customSettings;
        TimeZoneInfo tz;
        try
        {
            tz = _customTimezone switch
            {
                SessionTimezone.Central => TimeZoneInfo.FindSystemTimeZoneById(
                    OperatingSystem.IsWindows() ? "Central Standard Time" : "America/Chicago"),
                SessionTimezone.Eastern => TimeZoneInfo.FindSystemTimeZoneById(
                    OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York"),
                _ => TimeZoneInfo.Utc,
            };
        }
        catch
        {
            tz = TimeZoneInfo.Utc;
        }
        return _customSettings = new SessionSettings { TimeZone = tz, Start = new TimeSpan(_customHour, _customMinute, 0) };
    }

    private bool IsNewCustomSession(int bar)
    {
        if (bar == 0) return true;
        var prev = GetCandle(bar - 1).Time;
        var cur  = GetCandle(bar).Time;
        return GetCustomSettings().IsNewSession(prev, cur);
    }

    private void AddTo(Dictionary<decimal, (decimal Bid, decimal Ask)> dict, int bar)
    {
        foreach (var lvl in GetCandle(bar).GetAllPriceLevels())
        {
            dict.TryGetValue(lvl.Price, out var v);
            dict[lvl.Price] = (v.Bid + lvl.Bid, v.Ask + lvl.Ask);
        }
    }

    private void Finish(Profile p)
    {
        p.MaxVolume = 0;
        p.Total = 0;
        foreach (var (price, v) in p.Levels)
        {
            var vol = v.Bid + v.Ask;
            p.Total += vol;
            if (vol > p.MaxVolume)
            {
                p.MaxVolume = vol;
                p.Poc = price;
            }
        }

        var prices = p.Levels.Keys.ToList();
        if (prices.Count == 0) return;
        int pocIdx = prices.IndexOf(p.Poc);
        int up = pocIdx, dn = pocIdx;
        decimal acc = Vol(p, prices[pocIdx]);
        decimal target = p.Total * _valueArea / 100m;
        while (acc < target && (up < prices.Count - 1 || dn > 0))
        {
            decimal vu = up < prices.Count - 1 ? Vol(p, prices[up + 1]) : -1;
            decimal vd = dn > 0 ? Vol(p, prices[dn - 1]) : -1;
            if (vu >= vd) acc += Vol(p, prices[++up]);
            else acc += Vol(p, prices[--dn]);
        }

        p.Vah = prices[up];
        p.Val = prices[dn];
    }

    private static decimal Vol(Profile p, decimal price) =>
        p.Levels.TryGetValue(price, out var v) ? v.Bid + v.Ask : 0;

    // ── Rendering helpers ─────────────────────────────────────────────────────

    private static Color WithAlpha(Color c, float opacity) =>
        Color.FromArgb((int)(Math.Clamp(opacity, 0f, 1f) * 255), c.R, c.G, c.B);

    private static System.Drawing.Drawing2D.DashStyle ToDash(LineStyle s) => s switch
    {
        LineStyle.Dashed => System.Drawing.Drawing2D.DashStyle.Dash,
        LineStyle.Dotted => System.Drawing.Drawing2D.DashStyle.Dot,
        _                => System.Drawing.Drawing2D.DashStyle.Solid,
    };

    private static int XExtend(ExtendMode ext, int x0, int x1, int xRight) => ext switch
    {
        ExtendMode.Right or ExtendMode.Both => xRight,
        _ => x1,
    };

    private static int XStart(ExtendMode ext, int x0) => ext == ExtendMode.Both || ext == ExtendMode.Left ? 0 : x0;

    // ── OnRender ──────────────────────────────────────────────────────────────

    protected override void OnRender(RenderContext context, DrawingLayouts layout)
    {
        if (ChartInfo == null || _profiles.Count == 0) return;

        int first = FirstVisibleBarNumber, last = LastVisibleBarNumber;
        var container = ChartInfo.PriceChartContainer;
        int rowH = Math.Max(1, (int)Math.Round(container.PriceRowHeight));
        var font = new RenderFont("Arial", 7);

        // opacity scalars
        float baseOp    = BarOpacityPercent / 100f;
        float outsideOp = DimOutsideValueArea ? baseOp * (OutsideVaOpacityPercent / 100f) : baseOp;

        // right edge of visible chart area (for line extensions)
        int xChartRight = ChartInfo.GetXByBar(last, false) + container.BarsWidth;

        foreach (var p in _profiles)
        {
            if (p.EndBar < first || p.StartBar > last || p.MaxVolume <= 0) continue;

            int x0   = ChartInfo.GetXByBar(p.StartBar, true);
            int x1   = ChartInfo.GetXByBar(p.EndBar, true) + container.BarsWidth;
            int maxW = Math.Max(10, (x1 - x0) * WidthPercent / 100);

            // ── Profile border ────────────────────────────────────────────────
            if (ShowProfileBorder && p.Levels.Count > 0)
            {
                var allPrices = p.Levels.Keys.ToList();
                int yTop = ChartInfo.GetYByPrice(allPrices[^1], true);
                int yBot = ChartInfo.GetYByPrice(allPrices[0], true) + rowH;
                var bpen = new RenderPen(ProfileBorderColor, .5f);
                context.DrawLine(bpen, x0,        yTop, x0 + maxW, yTop);
                context.DrawLine(bpen, x0,        yBot, x0 + maxW, yBot);
                context.DrawLine(bpen, x0,        yTop, x0,        yBot);
                context.DrawLine(bpen, x0 + maxW, yTop, x0 + maxW, yBot);
            }

            // ── VA shade fill ─────────────────────────────────────────────────
            if (ShadeValueArea)
            {
                int yVahF = ChartInfo.GetYByPrice(p.Vah, true);
                int yValF = ChartInfo.GetYByPrice(p.Val, true) + rowH;
                context.FillRectangle(
                    WithAlpha(ValueAreaFillColor, ValueAreaFillOpacityPercent / 100f),
                    new Rectangle(x0, yVahF, maxW, Math.Max(1, yValF - yVahF)));
            }

            // ── HVN / LVN preparation ─────────────────────────────────────────
            bool needNodes = ShowHvnZones || ShowHvnPeakLine || ShowLvnZones || ShowLvnValleyLine;
            decimal hvnThresh = decimal.MaxValue, lvnThresh = 0;
            decimal peakVol = -1, peakPrice = 0;
            decimal valleyVol = decimal.MaxValue, valleyPrice = 0;

            if (needNodes && p.Levels.Count > 0)
            {
                var sortedVols = p.Levels.Values
                    .Select(v => v.Bid + v.Ask)
                    .OrderBy(v => v)
                    .ToList();
                decimal maxRowVol = sortedVols[^1];
                decimal avgVol    = sortedVols.Count > 0 ? sortedVols.Average() : 0;
                decimal stdDev    = sortedVols.Count > 1
                    ? (decimal)Math.Sqrt((double)sortedVols.Select(v => (v - avgVol) * (v - avgVol)).Average())
                    : 0;

                hvnThresh = HvnMethod switch
                {
                    VolumeNodeMethod.PercentOfMax => maxRowVol * HvnCutoffPercent / 100m,
                    VolumeNodeMethod.StdDev       => avgVol + stdDev * (HvnCutoffPercent / 100m),
                    _ => sortedVols[Math.Min((int)(HvnCutoffPercent / 100.0 * (sortedVols.Count - 1)), sortedVols.Count - 1)],
                };

                lvnThresh = LvnMethod switch
                {
                    VolumeNodeMethod.PercentOfMax => maxRowVol * LvnCutoffPercent / 100m,
                    VolumeNodeMethod.StdDev       => Math.Max(0, avgVol - stdDev * (LvnCutoffPercent / 100m)),
                    _ => sortedVols[Math.Min((int)(LvnCutoffPercent / 100.0 * (sortedVols.Count - 1)), sortedVols.Count - 1)],
                };

                // zone painting + identify peak / valley
                foreach (var (price, v) in p.Levels)
                {
                    decimal vol = v.Bid + v.Ask;
                    int y = ChartInfo.GetYByPrice(price, true);

                    if (ShowHvnZones && vol >= hvnThresh)
                    {
                        context.FillRectangle(
                            WithAlpha(HvnZoneColor, HvnZoneOpacityPercent / 100f),
                            new Rectangle(x0, y, maxW, Math.Max(1, rowH - 1)));
                        if (vol > peakVol) { peakVol = vol; peakPrice = price; }
                    }
                    else if (ShowHvnPeakLine && vol >= hvnThresh)
                    {
                        if (vol > peakVol) { peakVol = vol; peakPrice = price; }
                    }

                    if ((ShowLvnZones || ShowLvnValleyLine) && vol > 0 && vol <= lvnThresh)
                    {
                        if (ShowLvnZones)
                            context.FillRectangle(
                                WithAlpha(LvnZoneColor, LvnZoneOpacityPercent / 100f),
                                new Rectangle(x0, y, maxW, Math.Max(1, rowH - 1)));
                        if (vol < valleyVol) { valleyVol = vol; valleyPrice = price; }
                    }
                }
            }

            // ── Bars ──────────────────────────────────────────────────────────
            bool isBidAsk = Kind is ProfileKind.BidAsk or ProfileKind.BidAskTotal;
            bool isDelta  = Kind is ProfileKind.Delta or ProfileKind.DeltaTotal or ProfileKind.DeltaPct;
            bool showBorder = BarBorder == BarBorderMode.On
                || (BarBorder == BarBorderMode.Auto && isBidAsk && BarStyleMode == BarStyle.Shades);

            foreach (var (price, v) in p.Levels)
            {
                decimal vol  = v.Bid + v.Ask;
                decimal d    = v.Ask - v.Bid;
                int y        = ChartInfo.GetYByPrice(price, true);
                bool inVa    = price <= p.Vah && price >= p.Val;
                float opFact = inVa ? baseOp : outsideOp;
                float voltMult = BarStyleMode == BarStyle.Voltage ? Math.Min(1f, opFact * 1.4f) : opFact;
                int h = Math.Max(1, rowH - 1);

                switch (Kind)
                {
                    case ProfileKind.BidAsk:
                    case ProfileKind.BidAskTotal:
                    {
                        int wb = (int)(maxW * v.Bid / p.MaxVolume);
                        int wa = (int)(maxW * v.Ask / p.MaxVolume);
                        int tw = Math.Max(1, wb + wa);
                        int bx = Alignment == BarAlignment.Right ? x1 - tw : x0;
                        context.FillRectangle(WithAlpha(BidColor, voltMult), new Rectangle(bx,      y, Math.Max(1, wb), h));
                        context.FillRectangle(WithAlpha(AskColor, voltMult), new Rectangle(bx + wb, y, Math.Max(1, wa), h));
                        if (Kind == ProfileKind.BidAskTotal)
                        {
                            int wt = (int)(maxW * vol / p.MaxVolume);
                            int bx2 = Alignment == BarAlignment.Right ? x1 - wt : x0;
                            context.DrawLine(new RenderPen(Color.FromArgb(50, 255, 255, 255), .5f), bx2, y, bx2 + wt, y);
                            context.DrawLine(new RenderPen(Color.FromArgb(50, 255, 255, 255), .5f), bx2, y + h, bx2 + wt, y + h);
                        }
                        if (showBorder)
                            DrawBorder(context, bx, y, tw, h, Color.FromArgb(20, 255, 255, 255));
                        if (ShowRowValues)
                            DrawRowText(context, font, vol, bx, y, h, Alignment, tw);
                        break;
                    }

                    case ProfileKind.Delta:
                    case ProfileKind.DeltaTotal:
                    {
                        int w  = Math.Max(1, (int)(maxW * Math.Abs(d) / p.MaxVolume));
                        int bx = Alignment == BarAlignment.Right ? x1 - w : x0;
                        var c  = d >= 0 ? DeltaPositiveColor : DeltaNegativeColor;
                        context.FillRectangle(WithAlpha(c, opFact), new Rectangle(bx, y, w, h));
                        if (Kind == ProfileKind.DeltaTotal)
                        {
                            int wt  = Math.Max(1, (int)(maxW * vol / p.MaxVolume));
                            int bx2 = Alignment == BarAlignment.Right ? x1 - wt : x0;
                            context.DrawLine(new RenderPen(Color.FromArgb(50, 255, 255, 255), .5f), bx2, y, bx2 + wt, y);
                            context.DrawLine(new RenderPen(Color.FromArgb(50, 255, 255, 255), .5f), bx2, y + h, bx2 + wt, y + h);
                        }
                        if (showBorder)
                            DrawBorder(context, bx, y, w, h, WithAlpha(c, Math.Min(1f, opFact + .2f)));
                        if (ShowRowValues)
                            DrawRowText(context, font, vol, bx, y, h, Alignment, w);
                        break;
                    }

                    case ProfileKind.DeltaPct:
                    {
                        float pct = vol > 0 ? (float)(Math.Abs(d) / vol) : 0f;
                        int w  = Math.Max(1, (int)(maxW * pct));
                        int bx = Alignment == BarAlignment.Right ? x1 - w : x0;
                        var c  = d >= 0 ? DeltaPositiveColor : DeltaNegativeColor;
                        context.FillRectangle(WithAlpha(c, opFact), new Rectangle(bx, y, w, h));
                        if (showBorder)
                            DrawBorder(context, bx, y, w, h, WithAlpha(c, Math.Min(1f, opFact + .2f)));
                        if (ShowRowValues)
                            DrawRowText(context, font, vol, bx, y, h, Alignment, w);
                        break;
                    }

                    default: // Volume
                    {
                        int w  = Math.Max(1, (int)(maxW * vol / p.MaxVolume));
                        int bx = Alignment == BarAlignment.Right ? x1 - w : x0;
                        var c  = price == p.Poc ? PocColor : inVa ? ValueAreaColor : ProfileColor;
                        context.FillRectangle(WithAlpha(c, opFact), new Rectangle(bx, y, w, h));
                        if (showBorder)
                            DrawBorder(context, bx, y, w, h, WithAlpha(c, Math.Min(1f, opFact + .2f)));
                        if (ShowRowValues)
                            DrawRowText(context, font, vol, bx, y, h, Alignment, w);
                        break;
                    }
                }
            }

            // ── Developing POC trace ──────────────────────────────────────────
            if (ShowDevelopingPoc && p.PocTrace.Count > 1)
            {
                var devPen = new RenderPen(DevelopingPocColor, DevelopingPocWeight, ToDash(DevelopingPocStyle));
                decimal prevPoc = p.PocTrace[0].Poc;
                for (int i = 1; i < p.PocTrace.Count; i++)
                {
                    var (b1t, poc1) = p.PocTrace[i];
                    var (b0t, poc0) = p.PocTrace[i - 1];
                    if (poc0 == poc1) { prevPoc = poc1; continue; }
                    int dx0 = ChartInfo.GetXByBar(b0t, true);
                    int dx1 = ChartInfo.GetXByBar(b1t, true);
                    int dy0 = ChartInfo.GetYByPrice(poc0, false) + rowH / 2;
                    int dy1 = ChartInfo.GetYByPrice(poc1, false) + rowH / 2;
                    context.DrawLine(devPen, dx0, dy0, dx1, dy1);
                    prevPoc = poc1;
                }
            }

            // ── HVN peak line ─────────────────────────────────────────────────
            if (ShowHvnPeakLine && peakVol >= 0 && peakPrice != 0)
            {
                int yPeak = ChartInfo.GetYByPrice(peakPrice, false) + rowH / 2;
                context.DrawLine(
                    new RenderPen(HvnPeakColor, HvnPeakWeight, ToDash(HvnPeakStyle)),
                    XStart(HvnExtend, x0), yPeak,
                    XExtend(HvnExtend, x0, x1, xChartRight), yPeak);
            }

            // ── LVN valley line ───────────────────────────────────────────────
            if (ShowLvnValleyLine && valleyVol < decimal.MaxValue && valleyPrice != 0)
            {
                int yVal2 = ChartInfo.GetYByPrice(valleyPrice, false) + rowH / 2;
                context.DrawLine(
                    new RenderPen(LvnValleyColor, LvnValleyWeight, ToDash(LvnValleyStyle)),
                    XStart(LvnExtend, x0), yVal2,
                    XExtend(LvnExtend, x0, x1, xChartRight), yVal2);
            }

            // ── POC line ──────────────────────────────────────────────────────
            if (ShowPocLine)
            {
                int yPoc    = ChartInfo.GetYByPrice(p.Poc, false) + rowH / 2;
                int xPocEnd = XExtend(PocExtend, x0, x1, xChartRight);

                // find first bar where price touches the POC
                if (PocExtend == ExtendMode.Right || PocExtend == ExtendMode.Both)
                {
                    for (int b = p.EndBar + 1; b < CurrentBar; b++)
                    {
                        var c = GetCandle(b);
                        if (c.Low <= p.Poc && c.High >= p.Poc)
                        {
                            xPocEnd = ChartInfo.GetXByBar(b, false);
                            break;
                        }
                    }
                }

                context.DrawLine(
                    new RenderPen(PocColor, PocLineWeight, ToDash(PocLineStyle)),
                    XStart(PocExtend, x0), yPoc,
                    Math.Max(x1, xPocEnd), yPoc);
            }

            // ── VAH / VAL lines ───────────────────────────────────────────────
            if (ShowValueAreaLines)
            {
                int yVah2   = ChartInfo.GetYByPrice(p.Vah, true);
                int yVal2   = ChartInfo.GetYByPrice(p.Val, true) + rowH;
                int xVaStart = XStart(VahValExtend, x0);
                int xVaEnd   = XExtend(VahValExtend, x0, x1, xChartRight);
                var vaPen    = new RenderPen(ValueAreaLineColor, VahValLineWeight, ToDash(VahValLineStyle));

                context.DrawLine(vaPen, xVaStart, yVah2, xVaEnd, yVah2);
                context.DrawLine(vaPen, xVaStart, yVal2, xVaEnd, yVal2);
            }

            // ── Naked levels (POC / VAH / VAL from prior sessions that haven't been touched) ──
            if (ShowNakedLevels && _profiles.IndexOf(p) < _profiles.Count - 1)
            {
                bool pocTouched = false, vahTouched = false, valTouched = false;
                for (int b = p.EndBar + 1; b < CurrentBar; b++)
                {
                    var c = GetCandle(b);
                    if (!pocTouched && c.Low <= p.Poc && c.High >= p.Poc) pocTouched = true;
                    if (!vahTouched && c.Low <= p.Vah && c.High >= p.Vah) vahTouched = true;
                    if (!valTouched && c.Low <= p.Val && c.High >= p.Val) valTouched = true;
                    if (pocTouched && vahTouched && valTouched) break;
                }

                var nakedPocPen = new RenderPen(WithAlpha(PocColor,           0.4f), 1f, System.Drawing.Drawing2D.DashStyle.Dash);
                var nakedVaPen  = new RenderPen(WithAlpha(ValueAreaLineColor, 0.4f), .5f, System.Drawing.Drawing2D.DashStyle.Dash);

                if (ShowPocLine && !pocTouched)
                    context.DrawLine(nakedPocPen, x1, ChartInfo.GetYByPrice(p.Poc, false) + rowH / 2,
                        xChartRight, ChartInfo.GetYByPrice(p.Poc, false) + rowH / 2);
                if (ShowValueAreaLines && !vahTouched)
                    context.DrawLine(nakedVaPen, x1, ChartInfo.GetYByPrice(p.Vah, true),
                        xChartRight, ChartInfo.GetYByPrice(p.Vah, true));
                if (ShowValueAreaLines && !valTouched)
                    context.DrawLine(nakedVaPen, x1, ChartInfo.GetYByPrice(p.Val, true) + rowH,
                        xChartRight, ChartInfo.GetYByPrice(p.Val, true) + rowH);
            }
        }
    }

    private static void DrawBorder(RenderContext context, int bx, int y, int w, int h, Color c)
    {
        var pen = new RenderPen(c, .5f);
        context.DrawLine(pen, bx,     y,     bx + w, y);
        context.DrawLine(pen, bx,     y + h, bx + w, y + h);
        context.DrawLine(pen, bx,     y,     bx,     y + h);
        context.DrawLine(pen, bx + w, y,     bx + w, y + h);
    }

    private static void DrawRowText(RenderContext context, RenderFont font, decimal vol, int bx, int y, int h, BarAlignment align, int barW)
    {
        if (vol <= 0) return;
        string label = vol >= 1_000_000 ? $"{vol / 1_000_000m:F1}M"
            : vol >= 1_000 ? $"{vol / 1_000m:F1}K"
            : vol.ToString("F0");
        int tx = align == BarAlignment.Right ? bx - 1 : bx + barW + 2;
        context.DrawString(label, font, Color.FromArgb(160, 220, 230, 245), tx, y + (int)(h * 0.35f));
    }
}
