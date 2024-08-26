using SmartData.Lib.Models.Configurations;

namespace SmartData.Lib.Interfaces
{
    public interface IConfigsService
    {
        public Config Configurations { get; }
        public Task LoadConfigurationsAsync();
        public Task SaveConfigurationsAsync();

        bool HideGalleryPage { get; set; }
        bool HideSortImages { get; set; }
        bool HideContentAwareCrop { get; set; }
        bool HideManualCrop { get; set; }
        bool HideInpaintImages { get; set; }
        bool HideResizeImages { get; set; }
        bool HideUpscaleImages { get; set; }
        bool HideGenerateTags { get; set; }
        bool HideProcessCaptions { get; set; }
        bool HideProcessTags { get; set; }
        bool HideTagEditor { get; set; }
        bool HideExtractSubset { get; set; }
        bool HidePromptGenerator { get; set; }
        bool HideMetadataViewer { get; set; }
    }
}
