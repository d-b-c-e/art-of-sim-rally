using System;
using System.IO;

namespace ArtOfRally.TripleScreen.Mod;

internal static class AdapterConstants
{
    internal const string AdapterId = "dbce-triple-mod-art-of-rally";
    internal const string AdapterVersion = "0.2.0";
    internal const string GameId = "art-of-rally";

    internal static string GameProtocolDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DBCE",
        "TripleScreen",
        "games",
        GameId);

    internal static string DesiredLayoutPath => Path.Combine(GameProtocolDirectory, "desired-layout.json");
    internal static string StatusPath => Path.Combine(GameProtocolDirectory, "status.json");
}
