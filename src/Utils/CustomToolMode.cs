using System.Collections.Generic;
using Newtonsoft.Json;

namespace TabletopGames
{
    [JsonObject]
    public class CustomToolMode
    {
        public string Name { get; set; }
        public string IconPath { get; set; }
        public string HexColor { get; set; }
        public string Type { get; set; }
        public ChangeVariant ChangeVariant { get; set; }
        public SetAttributes SetAttributes { get; set; }
        public List<string> PushEvents { get; set; }
        public string TextIcon { get; set; }
        public bool NoColor { get; set; }
        public bool Linebreak { get; set; }
    }

    [JsonObject]
    public class ChangeVariant
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    [JsonObject]
    public class SetAttributes
    {
        public List<SetString> String { get; set; }
        public List<SetInt> Int { get; set; }
        public List<SetBool> Bool { get; set; }
    }

    [JsonObject]
    public class SetString
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public Condition Condition { get; set; }
    }

    [JsonObject]
    public class SetInt
    {
        public string Key { get; set; }
        public int Value { get; set; }
        public Condition Condition { get; set; }
    }

    [JsonObject]
    public class SetBool
    {
        public string Key { get; set; }
        public bool Value { get; set; }
        public Condition Condition { get; set; }
    }

    [JsonObject]
    public class Condition
    {
        public SetString String { get; set; }
        public SetInt Int { get; set; }
        public SetBool Bool { get; set; }
    }
}