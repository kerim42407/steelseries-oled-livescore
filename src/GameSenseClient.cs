using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace OledLiveScore
{
    // Talks to the SteelSeries GameSense local API: register a screen event and push frames.
    internal sealed class GameSenseClient
    {
        private string _base;

        public bool Connected { get { return _base != null; } }

        public void Connect()
        {
            var core = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "SteelSeries", "SteelSeries Engine 3", "coreProps.json");
            if (!File.Exists(core))
                throw new InvalidOperationException("SteelSeries GG is not running.");
            var addr = Json.Str(Json.Get(Json.Parse(File.ReadAllText(core)), "address"));
            if (string.IsNullOrEmpty(addr))
                throw new InvalidOperationException("Could not read the GameSense address.");
            _base = "http://" + addr;
        }

        private void Post(string endpoint, object body)
        {
            var json = Json.Write(body);
            using (var wc = new TimedWebClient { TimeoutMs = 4000 })
            {
                wc.Encoding = Encoding.UTF8;
                wc.Headers[HttpRequestHeader.ContentType] = "application/json";
                wc.UploadString(_base + endpoint, "POST", json);
            }
        }

        public void RegisterGame()
        {
            Post("/game_metadata", new Dictionary<string, object>
            {
                { "game", Config.GameId },
                { "game_display_name", Config.GameName },
                { "developer", "LiveScore" }
            });

            Post("/bind_game_event", new Dictionary<string, object>
            {
                { "game", Config.GameId },
                { "event", Config.EventName },
                { "value_optional", true },
                { "handlers", new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "device-type", Config.Screen },
                            { "mode", "screen" },
                            { "datas", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        { "lines", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    { "has-text", true },
                                                    { "context-frame-key", "top" },
                                                    { "bold", true }
                                                },
                                                new Dictionary<string, object>
                                                {
                                                    { "has-text", true },
                                                    { "context-frame-key", "bottom" }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }

        public void UpdateScreen(string top, string bottom)
        {
            Post("/game_event", new Dictionary<string, object>
            {
                { "game", Config.GameId },
                { "event", Config.EventName },
                { "data", new Dictionary<string, object>
                    {
                        { "frame", new Dictionary<string, object>
                            {
                                { "top", TextUtils.Trim21(top) },
                                { "bottom", TextUtils.Trim21(bottom) }
                            }
                        }
                    }
                }
            });
        }
    }
}
