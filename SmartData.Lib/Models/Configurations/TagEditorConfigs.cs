using System.Text.Json.Serialization;

namespace Models.Configurations
{
    public class TagEditorConfigs
    {
        [JsonPropertyName("inputFolder")]
        public string InputFolder { get; set; } = string.Empty;

        [JsonPropertyName("exactMatchesFiltering")]
        public bool ExactMatchesFiltering { get; set; } = false;

        [JsonPropertyName("tagsInputFile")]
        public string TagsInputFile { get; set; } = string.Empty;

        [JsonPropertyName("tagHighlightColor1")]
        public string TagHighlightColor1 { get; set; } = "Orange";

        [JsonPropertyName("tagHighlightColor2")]
        public string TagHighlightColor2 { get; set; } = "Blue";

        [JsonPropertyName("tagHighlightColor3")]
        public string TagHighlightColor3 { get; set; } = "Green";

        [JsonPropertyName("tagHighlightColor4")]
        public string TagHighlightColor4 { get; set; } = "Cyan";

        [JsonPropertyName("tagHighlightColor5")]
        public string TagHighlightColor5 { get; set; } = "Red";
    }
}
