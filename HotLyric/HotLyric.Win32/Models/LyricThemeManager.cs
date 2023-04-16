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
            catch { }

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
                        borderBrush: CreateBrush("#548F8F8F"),
                        backgroundBrush: CreateBrush("#FF2C2C2C"),
                        lyricBrush: CreateBrush("#FFFFFFFF"),
                        karaokeBrush: CreateBrush("#FFFFA04D"),
                        lyricStrokeBrush: CreateBrush("#FF000000"),
                        karaokeStrokeBrush: CreateBrush("#FF000000"))
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
                    catch { }
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
                    catch { }
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

            var borderBrush = CreateBrush(jsonModel?.BorderBrush);
            var backgroundBrush = CreateBrush(jsonModel?.BackgroundBrush);
            var lyricBrush = CreateBrush(jsonModel?.LyricBrush);
            var karaokeBrush = CreateBrush(jsonModel?.KaraokeBrush);
            var lyricStrokeBrush = CreateBrush(jsonModel?.LyricStrokeBrush);
            var karaokeStrokeBrush = CreateBrush(jsonModel?.KaraokeStrokeBrush);

            return new LyricThemeView(name, borderBrush, backgroundBrush, lyricBrush, karaokeBrush, lyricStrokeBrush, karaokeStrokeBrush);
        }

        private static LyricThemeJsonModel CreateJsonModel(LyricThemeView? view)
        {
            var jsonModel = new LyricThemeJsonModel()
            {
                Name = view?.Name,
                BorderBrush = GetBrushJson(view?.BorderBrush),
                BackgroundBrush = GetBrushJson(view?.BackgroundBrush),
                LyricBrush = GetBrushJson(view?.LyricBrush),
                KaraokeBrush = GetBrushJson(view?.KaraokeBrush),
                LyricStrokeBrush = GetBrushJson(view?.LyricStrokeBrush),
                KaraokeStrokeBrush = GetBrushJson(view?.KaraokeStrokeBrush),
            };

            return jsonModel;
        }

        private static Brush? CreateBrush(string? brushJson)
        {
            if (brushJson == null) return null;

            if (brushJson.Length > 0 && brushJson[0] == '#' && (brushJson.Length == 4 || brushJson.Length == 7 || brushJson.Length == 9))
            {
                try
                {
                    if (XamlBindingHelper.ConvertValue(typeof(Color), brushJson) is Color color)
                    {
                        return new SolidColorBrush(color);
                    }
                }
                catch { }
            }

            return null;
        }

        private static string GetBrushJson(Brush? brush)
        {
            if (brush is SolidColorBrush solidColorBrush)
            {
                var c = solidColorBrush.Color;
                return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
            }
            return $"#00FFFFFF";
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
