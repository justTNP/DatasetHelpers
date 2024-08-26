using Models.Configurations;

using System.Text.Json.Serialization;

namespace SmartData.Lib.Models.Configurations
{
    public class Config
    {
        [JsonPropertyName("galleryConfigs")]
        public GalleryConfigs GalleryConfigs { get; set; }

        [JsonPropertyName("sortImagesConfigs")]
        public SortImagesConfigs SortImagesConfigs { get; set; }

        [JsonPropertyName("contentAwareCropConfigs")]
        public ContentAwareCropConfigs ContentAwareCropConfigs { get; set; }

        [JsonPropertyName("manualCropConfigs")]
        public ManualCropConfigs ManualCropConfigs { get; set; }

        [JsonPropertyName("resizeImagesConfigs")]
        public ResizeImagesConfigs ResizeImagesConfigs { get; set; }

        [JsonPropertyName("upscaleImagesConfigs")]
        public UpscaleImagesConfigs UpscaleImagesConfigs { get; set; }

        [JsonPropertyName("generateTagsConfigs")]
        public GenerateTagsConfigs GenerateTagsConfigs { get; set; }

        [JsonPropertyName("processCaptionsConfigs")]
        public ProcessCaptionsConfigs ProcessCaptionsConfigs { get; set; }

        [JsonPropertyName("processTagsConfigs")]
        public ProcessTagsConfigs ProcessTagsConfigs { get; set; }

        [JsonPropertyName("tagEditorConfigs")]
        public TagEditorConfigs TagEditorConfigs { get; set; }

        [JsonPropertyName("extractSubsetConfigs")]
        public ExtractSubsetConfigs ExtractSubsetConfigs { get; set; }

        [JsonPropertyName("promptGeneratorConfigs")]
        public PromptGeneratorConfigs PromptGeneratorConfigs { get; set; }

        [JsonPropertyName("metadataViewerConfigs")]
        public MetadataViewerConfigs MetadataViewerConfigs { get; set; }

        // New properties for hiding tabs
        public bool? HideGalleryPage { get; set; }
        public bool? HideSortImages { get; set; }
        public bool? HideContentAwareCrop { get; set; }
        public bool? HideManualCrop { get; set; }
        public bool? HideInpaintImages { get; set; }
        public bool? HideResizeImages { get; set; }
        public bool? HideUpscaleImages { get; set; }
        public bool? HideGenerateTags { get; set; }
        public bool? HideProcessCaptions { get; set; }
        public bool? HideProcessTags { get; set; }
        public bool? HideTagEditor { get; set; }
        public bool? HideExtractSubset { get; set; }
        public bool? HidePromptGenerator { get; set; }
        public bool? HideMetadataViewer { get; set; }
    }
}
