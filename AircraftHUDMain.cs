using Brutal.ImGuiApi;
using Brutal.Logging;
using Brutal.Numerics;
using HarmonyLib;
using KSA;
using ModMenu;
using ShaderExtensions;
using StarMap.API;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;

namespace AircraftHUD
{
    [StarMapMod]
    public class HUD
    {
        public static HUD Instance;

        private Harmony harmony;

        enum DistanceUnits
        {
            Metric,
            Imperial
        }

        enum SpeedUnits
        {
            Metric,
            Imperial,
            Knots
        }
        enum TextAlignHoriz
        {
            Left,
            Right,
            Center
        }

        enum TextAlignVert
        {
            Top,
            Bottom,
            Center
        }

        static bool debugAreas = false;
        static ImColor8 debugColor = new ImColor8(255, 0, 255, 255);

        static bool enabled = true;
        static bool symbology = false;
        static bool settingsPageOn = false;
        static bool orbitMode = true;
        static bool effectsOutsideIVA = true;

        static DistanceUnits CurrentDistUnits { get; set; } = DistanceUnits.Metric;
        static SpeedUnits CurrentSpeedUnits { get; set; } = SpeedUnits.Metric;

        static float4 hudColorPicker = new float4(0.3f, 1f, 0f, 1f);
        static float4 hudColor1Picker = new float4(0.3f, 1f, 0f, 1f);
        static float4 hudColor2Picker = new float4(0.3f, 1f, 0f, 1f);
        static float4 shadowColorPicker = new float4(0f, 1f, 0f, 1f);
        static float textScale = 1f;
        static float lineScale = 1f;

        private float2 center;
        private float2 window_size;
        private float2 localCenter;

        private float verticalScale;

        static ImColor8 color = new ImColor8(78, 255, 0, 255);
        static ImColor8 color1 = new ImColor8(78, 255, 0, 255);
        static ImColor8 color2 = new ImColor8(78, 255, 0, 255);
        static ImColor8 shadowColor = new ImColor8(0, 0, 0, 150);
        static float2 shadowOffset = new float2(0f, 2f);
        static float line_thickness = 2.0f;

        private float DegToPx;

        private float2 mainClipMin;
        private float2 mainClipMax;

        // TEXT
        private bool fontLoaded = false;
        private ImFontPtr hudFont;
        static float fontSizeSm = 20f;
        static float fontSizeMed = 26f;
        static float fontSizeLrg = 32f;
        private string fontFile = "Fonts/HornetDisplay-Bold.ttf";

        private float textPosX;
        private float textPosX2;
        private float textPosX3;
        private float textBoxSizeX;
        private float textBoxSizeY;

        // FLIGHT VECTOR
        private float flightVecRadius1;
        private float flightVecRadius2;

        // AIM POINT
        private float aimpointRadius1;
        private float aimpointRadius2;

        // HORIZON
        private float horizonLen1;
        private float horizonLen2;

        // VERTICAL SCALES
        private float verticalScaleLenY;
        private float verticalScaleTextLen;
        private float verticalScaleMajorLen;
        private float verticalScaleMinorLen;

        private float altitudeScaleUnitsToPx;
        private float velocityScaleUnitsToPx;

        // PITCH LADDER
        private float pitchLadderLenX1;
        private float pitchLadderLenX2;
        private float pitchLadderLenX3;
        private float pitchLadderLenY1;

        // HEADING SCALE
        private float headingScalePosY;
        private float headingScaleMinX;
        private float headingScaleMaxX;
        private float headingScaleTextLen;
        private float headingScaleMajorLen;
        private float headingScaleMinorLen;
        private float headingScaleDegToPx;

        private float2 headingScaleClipMin;
        private float2 headingScaleClipMax;

        [ModMenuEntry("Aircraft HUD")]
        public static void CreateModMenu()
        {
            if (ImGui.MenuItem(HUD.enabled ? "Disable HUD" : "Enable HUD"))
            {
                HUD.enabled = !HUD.enabled;
            }

            if (ImGui.MenuItem("Settings"))
            {
                HUD.settingsPageOn = !HUD.settingsPageOn;
            }
        }

        public static float ConvertToDistanceUnits(float dist)
        {
            return CurrentDistUnits switch
            {
                DistanceUnits.Metric => dist,
                DistanceUnits.Imperial => dist * 3.28084f,
                _ => dist,
            };
        }
        public static string GetDistanceUnitsString()
        {
            return CurrentDistUnits switch
            {
                DistanceUnits.Metric => "M",
                DistanceUnits.Imperial => "FT",
                _ => "M",
            };
        }

        // speed in units per hour
        public static float ConvertToSpeedUnits(float speed)
        {
            return CurrentSpeedUnits switch
            {
                SpeedUnits.Metric => speed * 25.2f,
                SpeedUnits.Imperial => speed * 15.6586f,
                SpeedUnits.Knots => speed * 13.6f,
                _ => speed,
            };
        }

        public static string GetSpeedUnitsString()
        {
            return CurrentSpeedUnits switch
            {
                SpeedUnits.Metric => "KM", // km/hr
                SpeedUnits.Imperial => "MH", // miles/hr
                SpeedUnits.Knots => "KT", // knots
                _ => "KM",
            };
        }

        public static string FormatVerticalVelocityString(float speed)
        {
            return CurrentSpeedUnits switch
            {
                SpeedUnits.Metric => FormatStringPadded(speed, 3) + "M/S",
                SpeedUnits.Imperial => FormatStringPadded(speed * 196.85f, 4) + "FPM",
                SpeedUnits.Knots => FormatStringPadded(speed * 1.94384f, 4) + "KTS",
                _ => FormatStringPadded(speed, 3) + "M/S",
            };
        }

        public static double GetVerticalVelocity(Vehicle vehicle)
        {
            double3 positionCci = vehicle.GetPositionCci();
            double3 vector = positionCci.Normalized();
            return double3.Dot(vehicle.GetVelocityCci(), vector);
        }

        public static double2 GetAlphaBeta(Vehicle vehicle)
        {
            var state = vehicle.Orbit.StateVectors;
            var cci2body = vehicle.GetBody2Cci().Inverse();
            var velBody = (cci2body * state.VelocityCci).Normalized();

            var alpha = Math.Atan2(-velBody.X, -velBody.Z);
            var beta = Math.Atan2(-velBody.X, velBody.Y);
            return new double2(alpha, beta); // radians
        }

        public static double3 GetSurfaceAttitude(Vehicle vehicle)
        {
            double3 Body2Cci = vehicle.GetBody2Cci().ToRollYawPitchRadians();

            doubleQuat enuBody2Cci = VehicleReferenceFrameEx.GetEnuBody2Cci(vehicle.GetPositionCci()) ?? doubleQuat.Identity;
            doubleQuat attitudeQuat = doubleQuat.Concatenate(vehicle.GetBody2Cci(), enuBody2Cci.Inverse());
            return attitudeQuat.ToRollPitchYawRadians();
        }

        static readonly double G = 6.67430e-11; // m^3 / (kg s^2)
        static double GetGravity(Vehicle vehicle)
        {
            if (vehicle.Orbit.Parent is Celestial celestial)
            {
                double r = vehicle.DistanceTo(celestial);

                if (r < 1.0)
                    return 0; // safety

                return (G * celestial.Mass) / (r * r);
            }
            else
            {
                return 9.81;
            }
        }

        public static string FormatStringDecimal(float value, int decimals = 2)
        {
            return $"{value.ToString("F" + decimals)}";
        }

        public static string FormatStringPadded(float value, int padding = 2)
        {
            int intVal = (int)Math.Round(value);
            int len = Math.Abs(intVal).ToString().Length;

            // if absolute char length less than/equal padding, zero-pad normally
            if (len <= padding)
                return intVal.ToString(new string('0', padding));

            // otherwise convert to thousand format: divide by 1000 and add 1 decimal place
            float kVal = intVal / 1000f;
            return kVal.ToString("0.0") + "K";
        }

        public static float RadToDeg(float angle)
        {
            return (angle + (float)Math.PI / 2.0f) * (180.0f / (float)Math.PI);
        }

        public static float NormalizeDeg(float d)
        {
            d %= 360f;
            if (d < 0) d += 360f;
            return d;
        }

        public static float AngleRadToDegClamped(float angle)
        {
            float deg = RadToDeg(angle);
            return NormalizeDeg(deg);
        }

        public static float AngleRadToDegHalfClamped(float angle)
        {
            float deg = RadToDeg(angle);
            deg %= 360.0f;
            deg -= 90.0f;
            if (deg > 90) deg -= 360.0f;
            return deg;
        }
        public static float AngleRadToDegHalf(float angle)
        {
            float deg = RadToDeg(angle);
            if (deg > 180f) deg -= 360.0f;
            else if (deg < -180f) deg += 360.0f;
            return deg;
        }
        unsafe void DrawTextAligned(ImDrawList* target, float2 center, uint color, string text, TextAlignHoriz alignHorizontal = TextAlignHoriz.Center, TextAlignVert alignVertical = TextAlignVert.Center)
        {
            float2 textSize = ImGui.CalcTextSize(text);

            float offsetH = alignHorizontal switch
            {
                TextAlignHoriz.Left => 0f,
                TextAlignHoriz.Right => textSize.X,
                TextAlignHoriz.Center => textSize.X * 0.5f,
                _ => 0f,
            };

            float offsetV = alignVertical switch
            {
                TextAlignVert.Top => 0f,
                TextAlignVert.Bottom => textSize.Y,
                TextAlignVert.Center => textSize.Y * 0.5f,
                _ => 0f,
            };

            center -= new float2(offsetH, offsetV);

            ImDrawListExtensions.AddText(target, center + shadowOffset, shadowColor, text);
            ImDrawListExtensions.AddText(target, center, color, text);
        }

        unsafe void DrawCenteredRect(ImDrawList* target, float2 size, float2 center, uint color, float thickness)
        {
            size = size * 0.5f;
            float2 halfSize = new float2(size.X, size.Y);
            float2 pMin = center - halfSize;
            float2 pMax = center + halfSize;
            ImDrawListExtensions.AddRect(target, pMin + shadowOffset, pMax + shadowOffset, shadowColor, 0, 0, thickness);
            ImDrawListExtensions.AddRect(target, pMin, pMax, color, 0, 0, thickness);
        }

        unsafe void DrawRotatedText(ImDrawList* target, float2 p1, float2 center, uint color, string text, float sinA, float cosA)
        {
            float2 rp1 = RotateAroundPointFast(p1, center, sinA, cosA);

            DrawTextAligned(target, rp1, color, text);
        }

        unsafe void DrawLine(ImDrawList* target, float2 p1, float2 p2, uint color, float thickness)
        {
            ImDrawListExtensions.AddLine(target, p1 + shadowOffset, p2 + shadowOffset, shadowColor, thickness);
            ImDrawListExtensions.AddLine(target, p1, p2, color, thickness);
        }

        unsafe void DrawLineDashed(ImDrawList* target, float2 p1, float2 p2, uint color, float thickness, float dashLength, float gapLength)
        {
            float2 delta = p2 - p1;
            float totalLength = delta.Length();
            if (totalLength <= 0.0001f)
                return;

            float2 dir = delta / totalLength;

            float patternLength = dashLength + gapLength;
            float dist = 0f;

            while (dist < totalLength)
            {
                float startDist = dist;
                float endDist = Math.Min(dist + dashLength, totalLength);

                float2 segStart = p1 + dir * startDist;
                float2 segEnd = p1 + dir * endDist;

                DrawLine(target, segStart, segEnd, color, thickness);

                dist += patternLength;
            }
        }

        unsafe void DrawRotatedLine(ImDrawList* target,
            float2 p1, float2 p2, float2 center,
            uint color, float thickness,
            float sinA, float cosA, bool Dashed = false)
        {
            float2 rp1 = RotateAroundPointFast(p1, center, sinA, cosA);
            float2 rp2 = RotateAroundPointFast(p2, center, sinA, cosA);

            if (Dashed)
            {
                DrawLineDashed(target, rp1, rp2, color, thickness, 15f, 7.5f);
            }
            else
            {
                DrawLine(target, rp1, rp2, color, thickness);
            }
        }

        float2 RotateAroundPointFast(float2 point, float2 pivot, float s, float c)
        {
            float px = point.X - pivot.X;
            float py = point.Y - pivot.Y;

            float rx = px * c - py * s;
            float ry = px * s + py * c;

            return new float2(rx + pivot.X, ry + pivot.Y);
        }

        unsafe void DrawHeadingScaleTicks(ImDrawListPtr draw_list, float2 localCenter, uint color, float line_thickness,
            float HeadingDeg,
            float headingScaleDegToPx,
            float headingScaleMinX,
            float headingScaleMaxX,
            float headingScalePosY,
            float headingScaleTextLen,
            float headingScaleMinorLen,
            float headingScaleMajorLen,
            bool flipDirection = false,
            float phaseOffsetDeg = 0f
        )
        {
            const float minorStep = 5f;
            const float majorStep = 15f;

            float dirSign = flipDirection ? -1f : 1f;

            float headingRef = HeadingDeg + phaseOffsetDeg;

            float degAtMin = headingRef + ((headingScaleMinX - localCenter.X) / headingScaleDegToPx) * (1f / dirSign);
            float degAtMax = headingRef + ((headingScaleMaxX - localCenter.X) / headingScaleDegToPx) * (1f / dirSign);

            if (degAtMax < degAtMin)
            {
                float tmp = degAtMin; degAtMin = degAtMax; degAtMax = tmp;
            }

            float firstTick = (float)MathF.Floor(degAtMin / minorStep) * minorStep;

            firstTick -= minorStep;

            float rangeDeg = degAtMax - firstTick;
            int maxTicks = (int)(rangeDeg / minorStep) + 8;

            for (int i = 0; i < maxTicks; i++)
            {
                float H = firstTick + i * minorStep;
                if (H < degAtMin - 0.001f) continue;
                if (H > degAtMax + 0.001f) break;

                float degDelta = H - headingRef;
                float x = localCenter.X + (degDelta * headingScaleDegToPx * dirSign);

                if (x < headingScaleMinX - 1f || x > headingScaleMaxX + 1f)
                    continue;

                float normH = NormalizeDeg(H);
                bool isMajor = (MathF.Abs((normH % majorStep)) < 0.001f);

                float len = isMajor ? headingScaleMajorLen : headingScaleMinorLen;

                ImDrawListExtensions.AddLine(draw_list, new float2(x, headingScalePosY), new float2(x, headingScalePosY - len), color, line_thickness);

                if (isMajor)
                {
                    int labelVal = (int)MathF.Round(normH) % 360;
                    if (labelVal < 0) labelVal += 360;
                    string label = labelVal.ToString("000");

                    DrawTextAligned(draw_list, new float2(x, headingScalePosY - headingScaleTextLen), color, label);
                }
            }
        }

        unsafe void DrawVerticalScale(
            ImDrawList* draw_list,
            float centerValue,
            float valueScaleUnitsToPx,
            float scaleMinY, float scaleMaxY,
            float centerX,
            uint color,
            float minorStep, float majorStep,
            float minorLen, float majorLen,
            float fontOffset, bool flipX = false)
        {
            float2 clipMin = new float2(centerX - 1000f, scaleMinY);
            float2 clipMax = new float2(centerX + 1000f, scaleMaxY);

            float scaleRangePx = scaleMaxY - scaleMinY;

            float valueAtTop = centerValue - (scaleRangePx * 0.5f / valueScaleUnitsToPx);
            float valueAtBottom = centerValue + (scaleRangePx * 0.5f / valueScaleUnitsToPx);

            float firstTick = MathF.Floor(valueAtTop / minorStep) * minorStep;

            for (float v = firstTick; v <= valueAtBottom; v += minorStep)
            {
                float y = (centerValue - v) * valueScaleUnitsToPx + (scaleMinY + scaleRangePx * 0.5f);

                if (y < scaleMinY || y > scaleMaxY)
                    continue;

                bool isMajor = MathF.Abs((v % majorStep)) < 0.0001f;

                float len = isMajor ? majorLen : minorLen;

                DrawLine(
                    draw_list,
                    new float2(centerX, y),
                    new float2(flipX ? centerX - len : centerX + len, y),
                    color,
                    1.5f
                );

                if (isMajor)
                {
                    string label = FormatStringPadded(v, 4);
                    DrawTextAligned(
                        draw_list,
                        new float2(flipX ? centerX - len - fontOffset : centerX + len + fontOffset, y),
                        color,
                        label
                    );
                }
            }
        }

        static float4 ParseColor(string s)
        {
            var p = s.Split(',');
            return new float4(
                float.Parse(p[0], CultureInfo.InvariantCulture),
                float.Parse(p[1], CultureInfo.InvariantCulture),
                float.Parse(p[2], CultureInfo.InvariantCulture),
                float.Parse(p[3], CultureInfo.InvariantCulture)
            );
        }

        static string FormatColor(float4 c)
        {
            return $"{c.X.ToString(CultureInfo.InvariantCulture)}," +
                   $"{c.Y.ToString(CultureInfo.InvariantCulture)}," +
                   $"{c.Z.ToString(CultureInfo.InvariantCulture)}," +
                   $"{c.W.ToString(CultureInfo.InvariantCulture)}";
        }

        static void LoadHudIni(string path)
        {
            if (!File.Exists(path))
            {  
                Console.WriteLine("AircraftHUD: file does not exist " + path);
                return;
            }
            Console.WriteLine("AircraftHUD: Load Settings from " + path);

            string[] lines = File.ReadAllLines(path);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                    continue;

                var parts = line.Split('=');
                if (parts.Length != 2)
                    continue;

                string key = parts[0].Trim();
                string value = parts[1].Trim();

                switch (key)
                {
                    case "Enabled":
                        HUD.enabled = value == "1";
                        break;
                    case "OrbitMode":
                        HUD.orbitMode = value == "1";
                        break;
                    case "EffectsOutsideIVA":
                        HUD.effectsOutsideIVA = value == "1";
                        break;
                    case "DistanceUnits":
                        if (Enum.TryParse(value, out DistanceUnits distUnits))
                            HUD.CurrentDistUnits = distUnits;
                        break;
                    case "SpeedUnits":
                        if (Enum.TryParse(value, out SpeedUnits speedUnits))
                            HUD.CurrentSpeedUnits = speedUnits;
                        break;
                    case "HUDColor":
                        HUD.hudColorPicker = ParseColor(value);
                        break;
                    case "HUDColor1":
                        HUD.hudColor1Picker = ParseColor(value);
                        break;
                    case "HUDColor2":
                        HUD.hudColor2Picker = ParseColor(value);
                        break;
                    case "ShadowColor":
                        HUD.shadowColorPicker = ParseColor(value);
                        break;
                    case "TextScale":
                        HUD.textScale = float.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "LineScale":
                        HUD.lineScale = float.Parse(value, CultureInfo.InvariantCulture);
                        break;
                }
            }
        }

        static void SaveHudIni(string path)
        {
            using var w = new StreamWriter(path);

            w.WriteLine("[HUD]");
            w.WriteLine($"Enabled={(HUD.enabled ? 1 : 0)}");
            w.WriteLine($"OrbitMode={(HUD.orbitMode ? 1 : 0)}");
            w.WriteLine($"EffectsOutsideIVA={(HUD.effectsOutsideIVA ? 1 : 0)}");
            w.WriteLine($"DistanceUnits={HUD.CurrentDistUnits.ToString()}");
            w.WriteLine($"SpeedUnits={HUD.CurrentSpeedUnits.ToString()}");
            w.WriteLine($"HUDColor={FormatColor(HUD.hudColorPicker)}");
            w.WriteLine($"HUDColor1={FormatColor(HUD.hudColor1Picker)}");
            w.WriteLine($"HUDColor2={FormatColor(HUD.hudColor2Picker)}");
            w.WriteLine($"ShadowColor={FormatColor(HUD.shadowColorPicker)}");
            w.WriteLine($"TextScale={HUD.textScale.ToString(CultureInfo.InvariantCulture)}");
            w.WriteLine($"LineScale={HUD.lineScale.ToString(CultureInfo.InvariantCulture)}");

            Console.WriteLine("AircraftHUD: Save Settings to " + path);
        }

        //[StarMapImmediateLoad]
        //public void Load(KSA.Mod mod)
        //{
        //    Instance = this;

        //    harmony = new Harmony("AircraftHUD");

        //    harmony.PatchAll(typeof(HUD).Assembly);

        //    DefaultCategory.Log.Warning(
        //        "AircraftHUD: Harmony patches loaded"
        //    );
        //}

        [StarMapImmediateLoad]
        unsafe public void Init(Mod definingMod)
        {
            Console.WriteLine("AircraftHUD: Init");

            Instance = this;

            harmony = new Harmony("AircraftHUD");

            harmony.PatchAll(typeof(HUD).Assembly);

            //DefaultCategory.Log.Warning(
            //    "AircraftHUD: Harmony patches loaded"
            //);

            ImGuiViewport * viewport = ImGui.GetMainViewport();

            string dllDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            LoadHudIni(Path.Combine(dllDir, "hud.ini"));

            // most params are scaled by center.Y
            center = new float2(viewport->Pos.X + viewport->Size.X * 0.5f, viewport->Pos.Y + viewport->Size.Y * 0.5f);
            window_size = viewport->Size;
            localCenter = new float2(center.X, center.Y);

            // CALCULATE SCREEN SCALING
            // responsive scaling based on vertical resolution
            verticalScale = viewport->Size.Y / 1440f;

            fontSizeSm = (float)Math.Round(20f * textScale * verticalScale);
            fontSizeMed = (float)Math.Round(26f * textScale * verticalScale);
            fontSizeLrg = (float)Math.Round(32f * textScale * verticalScale);
            line_thickness = Math.Max((float)Math.Round(line_thickness * lineScale * verticalScale), 1f);
            shadowOffset = new float2(0f, line_thickness);

            color = ImGui.ColorConvertFloat4ToU32(hudColorPicker);
            color1 = ImGui.ColorConvertFloat4ToU32(hudColor1Picker);
            color2 = ImGui.ColorConvertFloat4ToU32(hudColor2Picker);
            shadowColor = ImGui.ColorConvertFloat4ToU32(shadowColorPicker);

            if (!fontLoaded)
            {
                ImGuiIO* io = ImGui.GetIO();
                ImFontAtlasPtr atlas = io->Fonts;

                string fontPathStr = Path.Combine(dllDir, fontFile);
                if (File.Exists(fontPathStr))
                {
                    ImString fontPath = new ImString(fontPathStr);
                    hudFont = atlas.AddFontFromFileTTF(fontPath, fontSizeLrg);
                    fontLoaded = true;
                }
            }

            DegToPx = localCenter.Y * 0.05f;

            float clipSize = localCenter.Y * 0.7f;
            mainClipMin = new float2(center.X - clipSize, center.Y - clipSize);
            mainClipMax = new float2(center.X + clipSize, center.Y + clipSize);

            // TEXT
            textPosX = localCenter.Y * 0.85f;
            textPosX2 = textPosX * 1.07f;
            textPosX3 = textPosX * 1.25f;
            textBoxSizeX = localCenter.Y * 0.17f;
            textBoxSizeY = localCenter.Y * 0.08f;

            // FLIGHT VECTOR
            flightVecRadius1 = localCenter.Y * 0.025f;
            flightVecRadius2 = localCenter.Y * 0.05f;

            // AIM POINT
            aimpointRadius1 = localCenter.Y * 0.055f;
            aimpointRadius2 = localCenter.Y * 0.09f;

            // HORIZON
            horizonLen1 = localCenter.Y * 0.2f;
            horizonLen2 = localCenter.Y * 0.95f;

            // VERTICAL SCALES
            verticalScaleLenY = localCenter.Y * 0.45f;
            verticalScaleTextLen = localCenter.Y * 0.075f;
            verticalScaleMajorLen = localCenter.Y * 0.035f;
            verticalScaleMinorLen = localCenter.Y * 0.025f;

            altitudeScaleUnitsToPx = localCenter.Y * 0.0035f;
            velocityScaleUnitsToPx = localCenter.Y * 0.02f;

            // PITCH LADDER
            pitchLadderLenX1 = localCenter.Y * 0.3f;
            pitchLadderLenX2 = localCenter.Y * 0.5f;
            pitchLadderLenX3 = localCenter.Y * 0.55f;
            pitchLadderLenY1 = localCenter.Y * 0.02f;

            // HEADING SCALE
            headingScalePosY = localCenter.Y - (localCenter.Y * 0.8f);
            headingScaleMinX = localCenter.X - (localCenter.Y * 0.55f);
            headingScaleMaxX = localCenter.X + (localCenter.Y * 0.55f);
            headingScaleTextLen = localCenter.Y * 0.045f;
            headingScaleMajorLen = localCenter.Y * 0.025f;
            headingScaleMinorLen = localCenter.Y * 0.015f;
            headingScaleDegToPx = localCenter.Y * 0.015f;

            headingScaleClipMin = new float2(headingScaleMinX, headingScalePosY - (localCenter.Y * 0.1f));
            headingScaleClipMax = new float2(headingScaleMaxX, headingScalePosY + (localCenter.Y * 0.1f));
        }

        [StarMapBeforeGui]
        unsafe public void DrawHudMenu(double dt)
        {
            ImGuiWindowFlags menuFlags = ImGuiWindowFlags.MenuBar;
            // options page
            if (HUD.settingsPageOn)
            {
                ImGui.Begin("HUD Settings", ref HUD.settingsPageOn, menuFlags);

                if (ImGui.Button(HUD.enabled ? "Disable HUD" : "Enable HUD"))
                {
                    HUD.enabled = !HUD.enabled;
                }

                if (ImGui.Button(HUD.orbitMode ? "Orbit: Surface" : "Orbit: Body"))
                {
                    HUD.orbitMode = !HUD.orbitMode;
                }

                if (ImGui.Button(HUD.effectsOutsideIVA ? "Hide Effects" : "Show Effects"))
                {
                    HUD.effectsOutsideIVA = !HUD.effectsOutsideIVA;
                }

                if (ImGui.Button(HUD.symbology ? "Hide Symbology" : "Show Symbology"))
                {
                    HUD.symbology = !HUD.symbology;
                }

                // UNITS
                if (ImGui.BeginMenu("Distance Units"))
                {
                    if (ImGui.MenuItem("Metric"))
                    {
                        CurrentDistUnits = HUD.DistanceUnits.Metric;
                    }
                    else if (ImGui.MenuItem("Imperial"))
                    {
                        CurrentDistUnits = HUD.DistanceUnits.Imperial;
                    }
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Speed Units"))
                {
                    if (ImGui.MenuItem("Metric"))
                    {
                        CurrentSpeedUnits = HUD.SpeedUnits.Metric;
                    }
                    else if (ImGui.MenuItem("Imperial"))
                    {
                        CurrentSpeedUnits = HUD.SpeedUnits.Imperial;
                    }
                    else if (ImGui.MenuItem("Knots"))
                    {
                        CurrentSpeedUnits = HUD.SpeedUnits.Knots;
                    }
                    ImGui.EndMenu();
                }

                // Color Pickers
                ImGui.Text("Colors");
                ImGui.SameLine();
                if (ImGui.ColorButton("##hudColorPickerBtn", hudColorPicker))
                {
                    ImGui.OpenPopup("hudColorPicker");
                }
                ImGui.SameLine();
                if (ImGui.ColorButton("##hudColorPicker1Btn", hudColor1Picker))
                {
                    ImGui.OpenPopup("hudColor1Picker");
                }
                ImGui.SameLine();
                if (ImGui.ColorButton("##hudColorPicker2Btn", hudColor2Picker))
                {
                    ImGui.OpenPopup("hudColor2Picker");
                }
                ImGui.SameLine();
                if (ImGui.ColorButton("##shadowColorPickerBtn", shadowColorPicker))
                {
                    ImGui.OpenPopup("shadowColorPicker");
                }

                if (ImGui.BeginPopup("hudColorPicker"))
                {
                    ImGui.ColorPicker4("##hudColorPicker", ref hudColorPicker);
                    HUD.color = ImGui.ColorConvertFloat4ToU32(hudColorPicker);
                    ImGui.EndPopup();
                }
                if (ImGui.BeginPopup("hudColor1Picker"))
                {
                    ImGui.ColorPicker4("##hudColor1Picker", ref hudColor1Picker);
                    HUD.color1 = ImGui.ColorConvertFloat4ToU32(hudColor1Picker);
                    ImGui.EndPopup();
                }
                if (ImGui.BeginPopup("hudColor2Picker"))
                {
                    ImGui.ColorPicker4("##hudColor2Picker", ref hudColor2Picker);
                    HUD.color2 = ImGui.ColorConvertFloat4ToU32(hudColor2Picker);
                    ImGui.EndPopup();
                }
                if (ImGui.BeginPopup("shadowColorPicker"))
                {
                    ImGui.ColorPicker4("##shadowColorPicker", ref shadowColorPicker);
                    HUD.shadowColor = ImGui.ColorConvertFloat4ToU32(shadowColorPicker);
                    ImGui.EndPopup();
                }

                if (ImGui.SliderFloat("Text Scale", ref HUD.textScale, 0.8f, 1.2f))
                {
                    fontSizeSm = (float)Math.Round(20f * HUD.textScale * verticalScale);
                    fontSizeMed = (float)Math.Round(26f * HUD.textScale * verticalScale);
                    fontSizeLrg = (float)Math.Round(32f * HUD.textScale * verticalScale);
                }

                if (ImGui.SliderFloat("Line Scale", ref HUD.lineScale, 0.5f, 2.0f))
                {
                    line_thickness = Math.Max((float)Math.Round(2f * HUD.lineScale * verticalScale), 1f);
                    shadowOffset = new float2(0f, line_thickness);
                }

                if (ImGui.Button("Save HUD Settings"))
                {
                    string dllDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    SaveHudIni(Path.Combine(dllDir, "hud.ini"));
                }

                ImGui.End();
            }
        }

        unsafe public void DrawHud(double dt)
        {
            //DefaultCategory.Log.Warning("AircraftHUD: DrawHud");

            KeyHash HudShader = KeyHash.Make("HudShader");

            if (!HUD.enabled) { return; }

            // HUD should only render in orbit mode
            CameraMode CamMode = Program.GetCameraMode();
            if (CamMode.Equals(CameraMode.Map) || CamMode.Equals(CameraMode.Free)) { return; }

            Vehicle controlledVehicle = Program.ControlledVehicle;
            if (controlledVehicle == null) { return; }

            Celestial parentBody = (Celestial)controlledVehicle.Orbit.Parent;
            if (parentBody == null) { return; }

            string DistanceUnits = GetDistanceUnitsString();

            //float parentSOI = (float)controlledVehicle.Parent.SphereOfInfluence;
            float altitude = (float)controlledVehicle.GetBarometricAltitude();
            if (altitude > 999999) { return; }

            // ATMOSPHERE
            PhysicalAtmosphereReference atmosphere;
            double AtmosphereDensity = 0f; // kg / m^3
            double AtmospherePressure = 0f;
            //double AtmosphereQbar = 0f;

            AtmosphereReference atmosphereRef = parentBody.BodyTemplate.AtmosphereReference;
            if (atmosphereRef != null)
            {
                atmosphere = atmosphereRef.Physical;
                if (atmosphere != null)
                {
                    AtmosphereDensity = atmosphere.GetAtmosphericDensityAtAltitude(altitude);
                    AtmospherePressure = atmosphere.GetAtmosphericPressureAtAltitude(altitude);
                }
                else { Console.WriteLine("AircraftHUD: PhysicalAtmosphereReference is invalid!"); }
            }
            else { Console.WriteLine("AircraftHUD: AtmosphereReference is invalid!"); }

            // CREATE WINDOW
            ImGuiViewport* viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewport->Pos);
            ImGui.SetNextWindowSize(window_size);
            ImGui.SetNextWindowViewport(viewport->ID);
            ImGuiWindowFlags flags =
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoBackground |
                ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoInputs |
                ImGuiWindowFlags.NoNavFocus;
            ImGui.Begin("HUDFullscreenWindow", flags);
            ImDrawListPtr draw_list = ImGui.GetWindowDrawList();

            if (CamMode.Equals(CameraMode.IVA) || effectsOutsideIVA) { SxImGui.CustomShader(HudShader); }

            // Workaround: It seems that ImGui window gets clipped, draw almost transparent rect to force full size
            ImDrawListExtensions.AddRectFilled(draw_list, new float2(0f, 0f), window_size, new ImColor8(0, 0, 0, 1), 0);

            if (debugAreas) { ImDrawListExtensions.AddRectFilled(draw_list, new float2(0f, 0f), window_size, new ImColor8(0, 0, 255, 100), 0); }

            // ALTITUDE (MSL / RADAR)
            bool useRadarAlt = false;
            float altitudeRadar = 0f;
            if (altitude < 50000f)
            {
                useRadarAlt = true;
                altitudeRadar = ConvertToDistanceUnits((float)controlledVehicle.GetRadarAltitude());
            }
            altitude = ConvertToDistanceUnits(altitude);

            // ORBIT
            float eccentricity = (float)controlledVehicle.Orbit.Eccentricity;
            float inclination = (float)controlledVehicle.Orbit.Inclination;

            float apoapsis = ConvertToDistanceUnits((float)controlledVehicle.Apoapsis);
            float periapsis = ConvertToDistanceUnits((float)controlledVehicle.Periapsis);

            if (orbitMode)
            {
                apoapsis -= (float)parentBody.MeanRadius;
                periapsis -= (float)parentBody.MeanRadius;
            }

            // SPEED
            double velocity = controlledVehicle.GetSurfaceSpeed();
            float speed = ConvertToSpeedUnits((float)velocity / 100f);
            string SpeedUnits = GetSpeedUnitsString();

            float verticalVelocity = (float)GetVerticalVelocity(controlledVehicle);

            double gravity = GetGravity(controlledVehicle);
            float gForce = (float)(controlledVehicle.AccelerationBody.Length() - gravity) / 9.80665f;

            // ATTITUDE
            double3 EulerRad = GetSurfaceAttitude(controlledVehicle);
            float RollDeg = AngleRadToDegClamped((float)EulerRad.X);
            float PitchDeg = AngleRadToDegHalfClamped((float)EulerRad.Y);
            float HeadingDeg = AngleRadToDegClamped((float)EulerRad.Z);

            // AERO
            double2 alphaBeta = GetAlphaBeta(controlledVehicle);
            float alphaDeg = AngleRadToDegHalf((float)alphaBeta.X);
            float betaDeg = AngleRadToDegHalf((float)alphaBeta.Y);

            // DEBUG
            //DrawTextAligned(draw_list, new float2(localCenter.X, localCenter.Y + textPosX), color, "Gravity: " + FormatStringDecimal((float)gravity, 3));

            // TEXT
            // SWITCH TO LARGE FONT
            if (fontLoaded) { ImGui.PushFont(hudFont, fontSizeLrg); }

            // ALTITUDE BOX
            DrawTextAligned(draw_list, new float2(localCenter.X + textPosX, localCenter.Y), color, FormatStringPadded(useRadarAlt ? altitudeRadar : altitude, 5));
            DrawCenteredRect(draw_list, new float2(textBoxSizeX, textBoxSizeY), new float2(localCenter.X + textPosX, localCenter.Y), color, line_thickness);
            if (useRadarAlt) { DrawTextAligned(draw_list, new float2(localCenter.X + textPosX + (textBoxSizeX * 0.7f), localCenter.Y), color, "R"); }

            // SPEED BOX
            DrawTextAligned(draw_list, new float2(localCenter.X - textPosX, localCenter.Y), color, FormatStringPadded(speed, 4));
            DrawCenteredRect(draw_list, new float2(textBoxSizeX, textBoxSizeY), new float2(localCenter.X - textPosX, localCenter.Y), color, line_thickness);

            // SWITCH TO MEDIUM FONT
            if (fontLoaded)
            {
                ImGui.PopFont();
                ImGui.PushFont(hudFont, fontSizeMed);
            }

            // VERTICAL SCALES
            draw_list.PushClipRect(new float2(0f, localCenter.Y - verticalScaleLenY), new float2(window_size.X, localCenter.Y - (textBoxSizeY * 0.5f)), true);
            DrawVerticalScale(draw_list,
                altitude,
                altitudeScaleUnitsToPx,
                localCenter.Y - verticalScaleLenY, localCenter.Y + verticalScaleLenY,
                localCenter.X + (textPosX * 0.9f),
                color,
                10f, 100f,
                verticalScaleMinorLen, verticalScaleMajorLen,
                verticalScaleTextLen);
            DrawVerticalScale(draw_list,
                speed,
                velocityScaleUnitsToPx,
                localCenter.Y - verticalScaleLenY, localCenter.Y + verticalScaleLenY,
                localCenter.X - (textPosX * 0.9f),
                color,
                1f, 10f,
                verticalScaleMinorLen, verticalScaleMajorLen,
                verticalScaleTextLen, true);

            // draw a second time, clipped
            draw_list.PopClipRect();
            draw_list.PushClipRect(new float2(0f, localCenter.Y + (textBoxSizeY * 0.5f)), new float2(window_size.X, localCenter.Y + verticalScaleLenY), true);
            DrawVerticalScale(draw_list,
                altitude,
                altitudeScaleUnitsToPx,
                localCenter.Y - verticalScaleLenY, localCenter.Y + verticalScaleLenY,
                localCenter.X + (textPosX * 0.9f),
                color,
                10f, 100f,
                verticalScaleMinorLen, verticalScaleMajorLen,
                verticalScaleTextLen);
            DrawVerticalScale(draw_list,
                speed,
                velocityScaleUnitsToPx,
                localCenter.Y - verticalScaleLenY, localCenter.Y + verticalScaleLenY,
                localCenter.X - (textPosX * 0.9f),
                color,
                1f, 10f,
                verticalScaleMinorLen, verticalScaleMajorLen,
                verticalScaleTextLen, true);

            draw_list.PopClipRect();

            // top left
            DrawTextAligned(draw_list, new float2(localCenter.X - textPosX2, localCenter.Y - (verticalScaleLenY * 1.15f)), color2, "E " + FormatStringDecimal(eccentricity, 2), TextAlignHoriz.Left);
            DrawTextAligned(draw_list, new float2(localCenter.X - textPosX2, localCenter.Y - (verticalScaleLenY * 1.25f)), color2, "I " + FormatStringDecimal(inclination, 2), TextAlignHoriz.Left);

            // top right
            DrawTextAligned(draw_list, new float2(localCenter.X + (textPosX2 * 0.85f), localCenter.Y - (verticalScaleLenY * 1.25f)), color2, "Ap " + FormatStringPadded(apoapsis, 4) + DistanceUnits, TextAlignHoriz.Left);
            DrawTextAligned(draw_list, new float2(localCenter.X + (textPosX2 * 0.85f), localCenter.Y - (verticalScaleLenY * 1.15f)), color2, "Pe " + FormatStringPadded(periapsis, 4) + DistanceUnits, TextAlignHoriz.Left);

            // bottom left
            DrawTextAligned(draw_list, new float2(localCenter.X - textPosX2, localCenter.Y + (verticalScaleLenY * 1.35f)), color, "G " + FormatStringDecimal(gForce, 1), TextAlignHoriz.Left);

            // bottom right
            DrawTextAligned(draw_list, new float2(localCenter.X + (textPosX2 * 0.85f), localCenter.Y + (verticalScaleLenY * 1.25f)), color, "V " + FormatVerticalVelocityString(verticalVelocity), TextAlignHoriz.Left);

            if (AtmosphereDensity > 0)
            {
                // MACH
                double speedOfSound = Math.Sqrt(1.4 * AtmospherePressure / AtmosphereDensity);
                double mach = velocity / speedOfSound;

                // bottom left
                DrawTextAligned(draw_list, new float2(localCenter.X - textPosX2, localCenter.Y + (verticalScaleLenY * 1.25f)), color, "M " + FormatStringDecimal((float)mach, 1), TextAlignHoriz.Left);
                DrawTextAligned(draw_list, new float2(localCenter.X - textPosX2, localCenter.Y + (verticalScaleLenY * 1.15f)), color, "a " + FormatStringDecimal(alphaDeg, 1), TextAlignHoriz.Left);

                if (HUD.symbology)
                {
                    DrawTextAligned(draw_list, new float2(localCenter.X - textPosX3, localCenter.Y + (verticalScaleLenY * 1.25f)), debugColor, "mach", TextAlignHoriz.Right);
                    DrawTextAligned(draw_list, new float2(localCenter.X - textPosX3, localCenter.Y + (verticalScaleLenY * 1.15f)), debugColor, "alpha", TextAlignHoriz.Right);
                }

                // ROLL / PITCH
                float RollAngleSin = MathF.Sin((float)-EulerRad.X);
                float RollAngleCos = MathF.Cos((float)-EulerRad.X);

                float PitchOffsetY = localCenter.Y + (DegToPx * (float)PitchDeg);

                draw_list.PushClipRect(mainClipMin, mainClipMax, true);
              
                if (debugAreas) { ImDrawListExtensions.AddRectFilled(draw_list, new float2(0f, 0f), window_size, new ImColor8(255, 0, 0, 100), 0); }

                // FLIGHT VECTORS
                if (Math.Abs(alphaDeg) < 90 && Math.Abs(betaDeg) < 90)
                {
                    // PROGRADE
                    float2 flightVecOffset = new float2(betaDeg * DegToPx, alphaDeg * DegToPx);
                    float2 flightVecCenter = new float2(localCenter.X + flightVecOffset.X, localCenter.Y - flightVecOffset.Y);
                    ImDrawListExtensions.AddCircle(draw_list, flightVecCenter + shadowOffset, flightVecRadius1, shadowColor, 16, line_thickness);
                    ImDrawListExtensions.AddCircle(draw_list, flightVecCenter, flightVecRadius1, color1, 16, line_thickness);
                    DrawLine(draw_list, flightVecCenter + new float2(flightVecRadius1, 0.0f), flightVecCenter + new float2(flightVecRadius2, 0.0f), color1, line_thickness);
                    DrawLine(draw_list, flightVecCenter - new float2(flightVecRadius1, 0.0f), flightVecCenter - new float2(flightVecRadius2, 0.0f), color1, line_thickness);
                    ImDrawListExtensions.AddLine(draw_list, flightVecCenter - new float2(0.0f, flightVecRadius1), flightVecCenter - new float2(0.0f, flightVecRadius2), color1, line_thickness);
                }
                else
                {
                    // RETROGRADE
                    alphaDeg -= 180;
                    betaDeg -= 180;
                    if (alphaDeg < -180) { alphaDeg += 360; }
                    //else if (alphaDeg > 180) { alphaDeg -= 360; }
                    if (betaDeg < -180) { betaDeg += 360; }
                    //else if (betaDeg > 180) { betaDeg -= 360; }

                    float2 flightVecOffset = new float2(betaDeg * DegToPx, alphaDeg * DegToPx);
                    float2 flightVecCenter = new float2(localCenter.X + flightVecOffset.X, localCenter.Y - flightVecOffset.Y);

                    float flightVecR45 = flightVecRadius1 * 0.7071f;
                    DrawLine(draw_list, flightVecCenter + new float2(flightVecR45, flightVecR45), flightVecCenter - new float2(flightVecR45, flightVecR45), color1, line_thickness);
                    DrawLine(draw_list, flightVecCenter + new float2(-flightVecR45, flightVecR45), flightVecCenter - new float2(-flightVecR45, flightVecR45), color1, line_thickness);

                    ImDrawListExtensions.AddCircle(draw_list, flightVecCenter + shadowOffset, flightVecRadius1, shadowColor, 16, line_thickness);
                    ImDrawListExtensions.AddCircle(draw_list, flightVecCenter, flightVecRadius1, color1, 16, line_thickness);
                    DrawLine(draw_list, flightVecCenter + new float2(flightVecRadius1, 0.0f), flightVecCenter + new float2(flightVecRadius2, 0.0f), color1, line_thickness);
                    DrawLine(draw_list, flightVecCenter - new float2(flightVecRadius1, 0.0f), flightVecCenter - new float2(flightVecRadius2, 0.0f), color1, line_thickness);
                    ImDrawListExtensions.AddLine(draw_list, flightVecCenter - new float2(0.0f, flightVecRadius1), flightVecCenter - new float2(0.0f, flightVecRadius2), color1, line_thickness);

                    //DrawTextAligned(draw_list, new float2(localCenter.X, localCenter.Y + textPosX*0.2f), color2, "alpha: " + FormatStringDecimal(alphaDeg, 3));
                    //DrawTextAligned(draw_list, new float2(localCenter.X, localCenter.Y - textPosX * 0.2f), color2, "beta: " + FormatStringDecimal(betaDeg, 3));
                }

                // AIM POINT
                DrawLine(draw_list, localCenter + new float2(aimpointRadius1, 0.0f), localCenter + new float2(aimpointRadius2, 0.0f), color, line_thickness);
                DrawLine(draw_list, localCenter - new float2(aimpointRadius1, 0.0f), localCenter - new float2(aimpointRadius2, 0.0f), color, line_thickness);
                DrawLine(draw_list, localCenter - new float2(0.0f, aimpointRadius1), localCenter - new float2(0.0f, aimpointRadius2), color, line_thickness);

                // HORIZON
                DrawRotatedLine(draw_list, new float2(localCenter.X + horizonLen1, PitchOffsetY), new float2(localCenter.X + horizonLen2, PitchOffsetY), localCenter, color, line_thickness, RollAngleSin, RollAngleCos);
                DrawRotatedLine(draw_list, new float2(localCenter.X - horizonLen1, PitchOffsetY), new float2(localCenter.X - horizonLen2, PitchOffsetY), localCenter, color, line_thickness, RollAngleSin, RollAngleCos);

                // PITCH LADDER
                float pitchOffsetDeg = 30f;
                float pitchLadderOffsetY = (localCenter.Y * 0.05f) * (Math.Clamp(PitchDeg, -pitchOffsetDeg, pitchOffsetDeg) / pitchOffsetDeg);

                for (int i = 1; i < 18; i++)
                {
                    int pitchAngle = i * 5;
                    string pitchString = pitchAngle.ToString();
                    float pitchPosY1 = (float)pitchAngle * DegToPx;
                    float pitchPosY2 = pitchPosY1 - pitchLadderLenY1;

                    DrawRotatedLine(draw_list, new float2(localCenter.X + pitchLadderLenX1, PitchOffsetY + pitchPosY1 + pitchLadderOffsetY), new float2(localCenter.X + pitchLadderLenX2, PitchOffsetY + pitchPosY1), localCenter, color, line_thickness, RollAngleSin, RollAngleCos, true);
                    DrawRotatedLine(draw_list, new float2(localCenter.X + pitchLadderLenX1, PitchOffsetY + pitchPosY2 + pitchLadderOffsetY), new float2(localCenter.X + pitchLadderLenX1, PitchOffsetY + pitchPosY1 + pitchLadderOffsetY), localCenter, color, line_thickness, RollAngleSin, RollAngleCos);

                    DrawRotatedLine(draw_list, new float2(localCenter.X - pitchLadderLenX1, PitchOffsetY + pitchPosY1 + pitchLadderOffsetY), new float2(localCenter.X - pitchLadderLenX2, PitchOffsetY + pitchPosY1), localCenter, color, line_thickness, RollAngleSin, RollAngleCos, true);
                    DrawRotatedLine(draw_list, new float2(localCenter.X - pitchLadderLenX1, PitchOffsetY + pitchPosY2 + pitchLadderOffsetY), new float2(localCenter.X - pitchLadderLenX1, PitchOffsetY + pitchPosY1 + pitchLadderOffsetY), localCenter, color, line_thickness, RollAngleSin, RollAngleCos);

                    DrawRotatedLine(draw_list, new float2(localCenter.X + pitchLadderLenX1, PitchOffsetY - pitchPosY1 + pitchLadderOffsetY), new float2(localCenter.X + pitchLadderLenX2, PitchOffsetY - pitchPosY1), localCenter, color, line_thickness, RollAngleSin, RollAngleCos);
                    DrawRotatedLine(draw_list, new float2(localCenter.X + pitchLadderLenX2, PitchOffsetY - pitchPosY2), new float2(localCenter.X + pitchLadderLenX2, PitchOffsetY - pitchPosY1), localCenter, color, line_thickness, RollAngleSin, RollAngleCos);

                    DrawRotatedLine(draw_list, new float2(localCenter.X - pitchLadderLenX1, PitchOffsetY - pitchPosY1 + pitchLadderOffsetY), new float2(localCenter.X - pitchLadderLenX2, PitchOffsetY - pitchPosY1), localCenter, color, line_thickness, RollAngleSin, RollAngleCos);
                    DrawRotatedLine(draw_list, new float2(localCenter.X - pitchLadderLenX2, PitchOffsetY - pitchPosY2), new float2(localCenter.X - pitchLadderLenX2, PitchOffsetY - pitchPosY1), localCenter, color, line_thickness, RollAngleSin, RollAngleCos);

                    // TO DO: Implement pitch ladder text rotation
                    DrawRotatedText(draw_list, new float2(localCenter.X + pitchLadderLenX3, PitchOffsetY + pitchPosY1), localCenter, color, pitchString, RollAngleSin, RollAngleCos);
                    DrawRotatedText(draw_list, new float2(localCenter.X - pitchLadderLenX3, PitchOffsetY + pitchPosY1), localCenter, color, pitchString, RollAngleSin, RollAngleCos);
                    DrawRotatedText(draw_list, new float2(localCenter.X + pitchLadderLenX3, PitchOffsetY - pitchPosY1), localCenter, color, pitchString, RollAngleSin, RollAngleCos);
                    DrawRotatedText(draw_list, new float2(localCenter.X - pitchLadderLenX3, PitchOffsetY - pitchPosY1), localCenter, color, pitchString, RollAngleSin, RollAngleCos);

                    //ImDrawListExtensions.AddImageQuad?
                }

                draw_list.PopClipRect();

                // HEADING SCALE
                draw_list.PushClipRect(headingScaleClipMin, headingScaleClipMax, true);

                // horizontal line
                DrawLine(draw_list, new float2(headingScaleMinX, headingScalePosY), new float2(headingScaleMaxX, headingScalePosY), color, line_thickness);
                DrawLine(draw_list, new float2(localCenter.X, headingScalePosY + localCenter.Y * 0.004f), new float2(localCenter.X + headingScaleMinorLen, headingScalePosY + headingScaleMajorLen), color, line_thickness);
                DrawLine(draw_list, new float2(localCenter.X, headingScalePosY + localCenter.Y * 0.004f), new float2(localCenter.X - headingScaleMinorLen, headingScalePosY + headingScaleMajorLen), color, line_thickness);

                // heading between 0 to 360
                DrawTextAligned(draw_list, new float2(localCenter.X, headingScalePosY + headingScaleTextLen), color, FormatStringPadded(HeadingDeg, 3));

                DrawHeadingScaleTicks(draw_list, localCenter, color, line_thickness, HeadingDeg,
                    headingScaleDegToPx,
                    localCenter.X - (localCenter.Y * 0.65f),
                    localCenter.X + (localCenter.Y * 0.65f),
                    headingScalePosY,
                    headingScaleTextLen,
                    headingScaleMinorLen,
                    headingScaleMajorLen);

                draw_list.PopClipRect();
                //ImDrawListExtensions.AddRect(draw_list, headingScaleClipMin, headingScaleClipMax, color, 1f); // debug clippin

                if (HUD.symbology)
                {
                    // altitude
                    DrawTextAligned(draw_list, new float2(localCenter.X + textPosX3, localCenter.Y), debugColor, useRadarAlt ? "altitude (radar)" : "altitude (msl)", TextAlignHoriz.Left);

                    // velocity
                    DrawTextAligned(draw_list, new float2(localCenter.X - textPosX3, localCenter.Y), debugColor, "velocity (" + SpeedUnits + ")", TextAlignHoriz.Right);

                    // top left
                    DrawTextAligned(draw_list, new float2(localCenter.X - textPosX3, localCenter.Y - (verticalScaleLenY * 1.15f)), debugColor, "eccentricity", TextAlignHoriz.Right);
                    DrawTextAligned(draw_list, new float2(localCenter.X - textPosX3, localCenter.Y - (verticalScaleLenY * 1.25f)), debugColor, "inclination", TextAlignHoriz.Right);

                    // top right
                    DrawTextAligned(draw_list, new float2(localCenter.X + textPosX3, localCenter.Y - (verticalScaleLenY * 1.25f)), debugColor, "apoapsis", TextAlignHoriz.Left);
                    DrawTextAligned(draw_list, new float2(localCenter.X + textPosX3, localCenter.Y - (verticalScaleLenY * 1.15f)), debugColor, "periapsis", TextAlignHoriz.Left);

                    // bottom left
                    DrawTextAligned(draw_list, new float2(localCenter.X - textPosX3, localCenter.Y + (verticalScaleLenY * 1.35f)), debugColor, "g force", TextAlignHoriz.Right);

                    // bottom right
                    DrawTextAligned(draw_list, new float2(localCenter.X + textPosX3, localCenter.Y + (verticalScaleLenY * 1.25f)), debugColor, "vertical velocity", TextAlignHoriz.Left);
                }
            }

            ImGui.PopFont();
            ImGui.End();
        }
    }

    [HarmonyPatch(typeof(Program))]
    internal static class ProgramPatches
    {
        [HarmonyPatch("OnFrameViewports")]
        [HarmonyPostfix]
        private static void AfterOnFrameViewports(double dtPlayer)
        {
            HUD.Instance?.DrawHud(dtPlayer);
        }
    }
}
