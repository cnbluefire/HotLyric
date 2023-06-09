using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Windows.Storage;
using Microsoft.UI;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HotLyric.Win32.Models
{
    public static class LyricThemeManager
    {
        static LyricThemeManager()
        {
            var defaultThemeJsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "Assets", "defaultTheme.json");

            List<LyricThemeView>? list = null;

            try
            {
                var allText = System.IO.File.ReadAllText(defaultThemeJsonPath);
                var jsonModel = JsonConvert.DeserializeObject<LyricThemeJsonModel[]>(allText);
                list = jsonModel?.Select(c => CreateView(c)).ToList();
            }
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }

            if (list != null)
            {
                LyricThemes = list;
            }
            else
            {
                LyricThemes = new List<LyricThemeView>()
                {
                    new LyricThemeView(
                        name: "default",
                        borderColor: ParseColor("#548F8F8F"),
                        backgroundColor: ParseColor("#FF2C2C2C"),
                        lyricColor: ParseColor("#FFFFFFFF"),
                        karaokeColor: ParseColor("#FFFFA04D"),
                        lyricStrokeColor: ParseColor("#FF000000"),
                        karaokeStrokeColor: ParseColor("#FF000000"))
                };
            }


        }


        public static IReadOnlyList<LyricThemeView> LyricThemes { get; }

        public static LyricThemeView? CurrentThemeView
        {
            get
            {
                if (ApplicationData.Current.LocalSettings.Values.TryGetValue("Theme_Current", out var _json)
                    && _json is string json
                    && !string.IsNullOrEmpty(json))
                {
                    try
                    {
                        var model = JsonConvert.DeserializeObject<LyricThemeJsonModel>(json);

                        if (model != null)
                        {
                            return CreateView(model);
                        }
                    }
                    catch (Exception ex)
                    {
                        HotLyric.Win32.Utils.LogHelper.LogError(ex);
                    }
                }

                return null;
            }
            set
            {
                if (value == null)
                {
                    try
                    {
                        ApplicationData.Current.LocalSettings.Values.Remove("Theme_Current");
                    }
                    catch (Exception ex)
                    {
                        HotLyric.Win32.Utils.LogHelper.LogError(ex);
                    }
                }
                else
                {
                    var jsonModel = CreateJsonModel(value);
                    ApplicationData.Current.LocalSettings.Values["Theme_Current"] = JsonConvert.SerializeObject(jsonModel);
                }
            }
        }

        private static LyricThemeView CreateView(LyricThemeJsonModel? jsonModel)
        {
            var name = jsonModel?.Name;
            if (string.IsNullOrEmpty(name))
            {
                name = "customize";
            }

            var borderBrush = ParseColor(jsonModel?.BorderBrush);
            var backgroundBrush = ParseColor(jsonModel?.BackgroundBrush);
            var lyricBrush = ParseColor(jsonModel?.LyricBrush);
            var karaokeBrush = ParseColor(jsonModel?.KaraokeBrush);
            var lyricStrokeBrush = ParseColor(jsonModel?.LyricStrokeBrush);
            var karaokeStrokeBrush = ParseColor(jsonModel?.KaraokeStrokeBrush);

            return new LyricThemeView(name, borderBrush, backgroundBrush, lyricBrush, karaokeBrush, lyricStrokeBrush, karaokeStrokeBrush);
        }

        private static LyricThemeJsonModel CreateJsonModel(LyricThemeView? view)
        {
            var defaultColor = Color.FromArgb(0, 255, 255, 255);

            var jsonModel = new LyricThemeJsonModel()
            {
                Name = view?.Name,
                BorderBrush = GetColorJson(view?.BorderColor ?? defaultColor),
                BackgroundBrush = GetColorJson(view?.BackgroundColor ?? defaultColor),
                LyricBrush = GetColorJson(view?.LyricColor ?? defaultColor),
                KaraokeBrush = GetColorJson(view?.KaraokeColor ?? defaultColor),
                LyricStrokeBrush = GetColorJson(view?.LyricStrokeColor ?? defaultColor),
                KaraokeStrokeBrush = GetColorJson(view?.KaraokeStrokeColor ?? defaultColor),
            };

            return jsonModel;
        }

        private static Color ParseColor(string? colorJson)
        {
            if (!string.IsNullOrEmpty(colorJson) && colorJson[0] == '#' && (colorJson.Length == 4 || colorJson.Length == 7 || colorJson.Length == 9))
            {
                try
                {
                    if (XamlBindingHelper.ConvertValue(typeof(Color), colorJson) is Color color)
                    {
                        return color;
                    }
                }
                catch (Exception ex)
                {
                    HotLyric.Win32.Utils.LogHelper.LogError(ex);
                }
            }
            return Color.FromArgb(0, 255, 255, 255);
        }

        private static string GetColorJson(Color color)
        {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private class LyricThemeJsonModel
        {
            [JsonProperty("name")]
            public string? Name { get; set; }

            [JsonProperty("borderBrush")]
            public string? BorderBrush { get; set; }

            [JsonProperty("backgroundBrush")]
            public string? BackgroundBrush { get; set; }

            [JsonProperty("lyricBrush")]
            public string? LyricBrush { get; set; }

            [JsonProperty("karaokeBrush")]
            public string? KaraokeBrush { get; set; }

            [JsonProperty("lyricStrokeBrush")]
            public string? LyricStrokeBrush { get; set; }

            [JsonProperty("karaokeStrokeBrush")]
            public string? KaraokeStrokeBrush { get; set; }
        }
    }
}
