﻿using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.ML.OnnxRuntime;

using SmartData.Lib.Enums;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    /// <summary>
    /// View model for the Tag Editor, responsible for managing image tags and text editing.
    /// </summary>
    public partial class TagEditorViewModel : BaseViewModel
    {
        private readonly IFileManipulatorService _fileManipulator;
        private readonly IImageProcessorService _imageProcessor;
        private readonly ICLIPTokenizerService _clipTokenizer;
        private Random _random;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private List<string> _imageFiles;
        [ObservableProperty]
        private string _totalImageFiles;
        [ObservableProperty]
        private int _selectedItemIndex;
        [ObservableProperty]
        private Bitmap _selectedImage;
        [ObservableProperty]
        private string _selectedImageFilename;
        [ObservableProperty]
        private string _wordsToHighlight;
        [ObservableProperty]
        private string _wordsToFilter;
        [ObservableProperty]
        private bool _isExactFilter;
        [ObservableProperty]
        private bool _buttonEnabled;
        [ObservableProperty]
        private string _currentAndTotal;
        [ObservableProperty]
        private bool _editingTxt;
        [ObservableProperty]
        private string _currentImageTags;
        [ObservableProperty]
        private string _currentImageTokenCount;

        [ObservableProperty]
        private SolidColorBrush _tokenTextColor;

        [ObservableProperty]
        private List<string> _highlightColors;

        [ObservableProperty]
        private string _tagsFilePath;

        // Collection bound to the ListBox in the XAML.
        public ObservableCollection<TagSuggestion> TagSuggestions { get; } = new ObservableCollection<TagSuggestion>();

        // Backing list for all suggestions (unfiltered).
        private List<TagSuggestion> _allTagSuggestions = new List<TagSuggestion>();

        // New property bound to the filter TextBox.
        [ObservableProperty]
        private string _filterText;

        // Bind this property to the TextEditor's Text.
        [ObservableProperty]
        private string _editorTagsText;

        [ObservableProperty]
        private bool _isImagePopupVisible;

        private Dictionary<string, string> ColorNameToHex { get; } = new Dictionary<string, string>
        {
            {"Orange", "#FFB347"},
            {"Blue", "#6F7DFF"},
            {"Green", "#2CF747"},
            {"Cyan", "#33FFFF"},
            {"Red", "#FF3333"},
            {"Hot Pink", "#FF69B4"},
            {"Medium Purple", "#9370DB"},
            {"Light Sea Green", "#20B2AA"},
            {"Gold", "#FFD700"},
            {"Blue Violet", "#8A2BE2"},
            {"Gray", "#7A7A79"}
        };

        public string GetHexFromColorName(string colorName)
        {
            return ColorNameToHex.TryGetValue(colorName, out string hexColor) ? hexColor : "#FFB347";
        }
        
        [RelayCommand]
        public async Task OpenFileAsync()
        {
            if (ImageFiles != null && SelectedItemIndex >= 0 && SelectedItemIndex < ImageFiles.Count)
            {
                string filePath = ImageFiles[SelectedItemIndex];
                if (File.Exists(filePath))
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            using (Process process = new Process())
                            {
                                process.StartInfo = new ProcessStartInfo(filePath)
                                {
                                    UseShellExecute = true
                                };
                                process.Start();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.SetLatestLogMessage($"Error opening file: {ex.Message}", LogMessageColor.Error);
                        }
                    });
                }
                else
                {
                    Logger.SetLatestLogMessage("File not found.", LogMessageColor.Error);
                }
            }
            else
            {
                Logger.SetLatestLogMessage("No file selected.", LogMessageColor.Warning);
            }
        }

        /// <summary>
        /// Gets the current type of file being edited, either .txt or .caption.
        /// </summary>
        public string CurrentType
        {
            get
            {
                if (EditingTxt)
                {
                    return ".txt";
                }
                else
                {
                    return ".caption";
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the TagEditorViewModel class.
        /// </summary>
        /// <param name="fileManipulator">The file manipulation service for file operations.</param>
        /// <param name="imageProcessor">The image processing service for image-related operations.</param>
        /// <param name="inputHooks">The input hooks service for managing user input.</param>
        /// <param name="clipTokenizer">The clip tokenizer service for token operations.</param>
        /// <param name="logger">The logger service for logging messages.</param>
        /// <param name="configs">The configuration service for application settings.</param>
        public TagEditorViewModel(IFileManipulatorService fileManipulator, IImageProcessorService imageProcessor,
            ICLIPTokenizerService clipTokenizer, ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _fileManipulator = fileManipulator;
            _imageProcessor = imageProcessor;
            _clipTokenizer = clipTokenizer;
            _random = new Random();
            _configs = configs;

            InputFolderPath = _configs.Configurations.TagEditorConfigs.InputFolder;
            TagsFilePath = _configs.Configurations.TagEditorConfigs.TagsInputFile;
            _fileManipulator.CreateFolderIfNotExist(InputFolderPath);
            IsExactFilter = _configs.Configurations.TagEditorConfigs.ExactMatchesFiltering;
            ButtonEnabled = true;
            EditingTxt = true;

            SelectedItemIndex = 0;
            CurrentImageTokenCount = string.Empty;

            TokenTextColor = new SolidColorBrush(Colors.LightGreen);

            HighlightColors = new List<string>
            {
                GetHexFromColorName(configs.Configurations.TagEditorConfigs.TagHighlightColor1),
                GetHexFromColorName(configs.Configurations.TagEditorConfigs.TagHighlightColor2),
                GetHexFromColorName(configs.Configurations.TagEditorConfigs.TagHighlightColor3),
                GetHexFromColorName(configs.Configurations.TagEditorConfigs.TagHighlightColor4),
                GetHexFromColorName(configs.Configurations.TagEditorConfigs.TagHighlightColor5)
            };
        }

        /// <summary>
        /// Updates the current selected image tags based on the selected image.
        /// </summary>
        public void UpdateCurrentSelectedTags()
        {
            if (SelectedImage != null)
            {
                try
                {
                    CurrentImageTags = _fileManipulator.GetTextFromFile(ImageFiles[SelectedItemIndex], CurrentType);
                }
                catch
                {
                    Logger.SetLatestLogMessage($".txt or .caption file for current image not found, just type in the editor and one will be created!",
                        LogMessageColor.Warning);
                    CurrentImageTags = string.Empty;
                }
            }
        }

        /// <summary>
        /// Navigates to a specific item in the image list.
        /// </summary>
        /// <param name="parameter">The navigation parameter indicating the item index.</param>
        [RelayCommand]
        private void GoToItem(string parameter)
        {
            try
            {
                int.TryParse(parameter, out int parameterInt);

                if (ImageFiles?.Count != 0)
                {
                    SelectedItemIndex += parameterInt;
                }
            }
            catch
            {
                Logger.SetLatestLogMessage("Couldn't load the image.", LogMessageColor.Error);
            }
        }

        /// <summary>
        /// Navigates to a random item in the image list.
        /// </summary>
        [RelayCommand]
        private void GoToRandomItem()
        {
            if (ImageFiles?.Count != 0 && ImageFiles != null)
            {
                SelectedItemIndex = _random.Next(0, ImageFiles.Count);
            }
        }

        /// <summary>
        /// Switches between editing .txt and .caption files and updates the view accordingly.
        /// </summary>
        [RelayCommand]
        private void SwitchEditorType()
        {
            EditingTxt = !EditingTxt;
            OnPropertyChanged(nameof(CurrentType));
        }

        /// <summary>
        /// Asynchronously filters and loads image files based on specified filter criteria.
        /// </summary>
        [RelayCommand]
        private async Task FilterFilesAsync()
        {
            try
            {
                ButtonEnabled = false;
                List<string> searchResult = await Task.Run(() => _fileManipulator.GetFilteredImageFiles(InputFolderPath, CurrentType, WordsToFilter, IsExactFilter));
                if (searchResult.Count > 0)
                {
                    SelectedItemIndex = 0;
                    ImageFiles = searchResult;
                    ImageFiles = ImageFiles.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x))).ToList();
                }
                else
                {
                    Logger.SetLatestLogMessage("No images found!", LogMessageColor.Warning);
                }
            }
            catch (FileNotFoundException)
            {
                Logger.SetLatestLogMessage("No image files were found in the directory.", LogMessageColor.Error);
            }
            catch (Exception exception)
            {
                Logger.SetLatestLogMessage($"Something went wrong! Error log will be saved inside the logs folder.",
                        LogMessageColor.Error);
                await Logger.SaveExceptionStackTrace(exception);
            }
            finally
            {
                if (ImageFiles.Count != 0)
                {
                    SelectedImage = SelectBitmapInterpolation();
                }
                ButtonEnabled = true;
            }
        }

        /// <summary>
        /// Clears the applied filter and reloads all images from the original input folder.
        /// </summary>
        [RelayCommand]
        private void ClearFilter()
        {
            if (!string.IsNullOrEmpty(InputFolderPath))
            {
                LoadImagesFromInputFolder();
            }
        }

        /// <summary>
        /// Selects an input folder and loads images from it.
        /// </summary>
        [RelayCommand]
        private async Task SelectInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
                LoadImagesFromInputFolder();
            }
        }

        /// <summary>
        /// Selects a .csv file
        /// </summary>
        [RelayCommand]
        private async Task SelectTagsFileAsync()
        {
            string result = await SelectCsvFileAsync();
            if (!string.IsNullOrEmpty(result))
            {
                TagsFilePath = result;
                await RefreshTagsAsync();
            }
        }

        [RelayCommand]
        public async Task RefreshTagsAsync()
        {
            if (string.IsNullOrWhiteSpace(TagsFilePath))
                return;

            try
            {
                // Process the CSV file on a background thread.
                var newTagSuggestions = await Task.Run(() =>
                {
                    var suggestions = new List<TagSuggestion>();
                    int index = 0;
                    foreach (var line in File.ReadLines(TagsFilePath))
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        // Assuming the CSV columns are comma-separated.
                        var parts = line.Split(',');
                        if (parts.Length < 3)
                            continue;

                        string tag = parts[0].Trim();

                        if (!int.TryParse(parts[1].Trim(), out int colorCode))
                            continue;

                        if (!long.TryParse(parts[2].Trim(), out long count))
                            continue;

                        suggestions.Add(new TagSuggestion
                        {
                            Tag = tag,
                            ColorCode = colorCode,
                            Count = count
                        });

                        index++;
                    }
                    return suggestions;
                });

                // Update the backing list for filtering.
                _allTagSuggestions = newTagSuggestions;

                // Now update the filtered list.
                FilterTagSuggestions();
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                    Logger.SetLatestLogMessage($"Error: {ex.Message}", LogMessageColor.Error));
            }
        }

        partial void OnFilterTextChanged(string value)
        {
            FilterTagSuggestions();
        }

        /// <summary>
        /// Filters the suggestions based on FilterText.
        /// </summary>
        private void FilterTagSuggestions()
        {
            TagSuggestions.Clear();
            IEnumerable<TagSuggestion> filtered = _allTagSuggestions;
            
            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                // Split the filter text into tokens using underscore (or add other delimiters as needed)
                var tokens = FilterText.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                
                filtered = filtered.Where(suggestion =>
                    tokens.All(token => suggestion.Tag.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0));
            }
            
            filtered = filtered.OrderByDescending(suggestion => suggestion.Count);
            
            // Update each suggestion’s formatted parts based on the current filter.
            foreach (var suggestion in filtered)
            {
                suggestion.UpdateFormattedParts(FilterText);
                TagSuggestions.Add(suggestion);
            }
        }

        /// <summary>
        /// Copies the current image tags to the clipboard asynchronously.
        /// </summary>
        [RelayCommand]
        private async Task CopyCurrentImageTagsToClipboard()
        {
            await CopyToClipboard(CurrentImageTags);
        }

        /// <summary>
        /// Counts the tokens for the current image tags asynchronously.
        /// Downloads the necessary onnx extension file if it is not present, and updates the token count and text color.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        [RelayCommand]
        private async Task CountTokensForCurrentImage()
        {
            if (_fileManipulator.FileNeedsToBeDownloaded(AvailableModels.CLIPTokenizer))
            {
                await _fileManipulator.DownloadModelFile(AvailableModels.CLIPTokenizer);
            }

            int count = 0;
            try
            {
                count = await Task.Run(() => _clipTokenizer.CountTokens(CurrentImageTags));
                CurrentImageTokenCount = $"Token count: {count}/225";

                if (count < 225)
                {
                    TokenTextColor = new SolidColorBrush(Colors.LightGreen);
                }
                else
                {
                    TokenTextColor = new SolidColorBrush(Colors.PaleVioletRed);
                }
            }
            catch (OnnxRuntimeException)
            {
                CurrentImageTokenCount = "Failed to load CLIP Tokenizer.";
                TokenTextColor = new SolidColorBrush(Colors.PaleVioletRed);
            }
        }

        /// <summary>
        /// Loads image files from the specified input folder and prepares the view model for editing.
        /// </summary>
        private void LoadImagesFromInputFolder()
        {
            try
            {
                ImageFiles = _fileManipulator.GetImageFiles(InputFolderPath)
                    .Where(x => !x.Contains("_mask")).ToList();
                if (ImageFiles.Count != 0)
                {
                    ImageFiles = ImageFiles.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x))).ToList();
                    SelectedItemIndex = 0;
                }
            }
            catch
            {
                Logger.SetLatestLogMessage("No image files were found in the directory.", LogMessageColor.Error);
            }
            finally
            {
                if (ImageFiles.Count != 0)
                {
                    SelectedImage = SelectBitmapInterpolation();
                }
            }
        }

        /// <summary>
        /// Handles changes in the EditingTxt property to update the selected image tags.
        /// </summary>
        partial void OnEditingTxtChanged(bool value)
        {
            UpdateCurrentSelectedTags();
        }

        /// <summary>
        /// Handles changes in the ImageFiles property to update the total image files count.
        /// </summary>
        partial void OnImageFilesChanged(List<string> value)
        {
            TotalImageFiles = $"Total files found: {ImageFiles.Count.ToString()}.";
        }

        /// <summary>
        /// Handles changes in the SelectedItemIndex property to ensure it stays within the valid range.
        /// </summary>
        partial void OnSelectedItemIndexChanged(int value)
        {
            if (ImageFiles?.Count > 0)
            {
                SelectedItemIndex = Math.Clamp(value, 0, ImageFiles.Count - 1);
                SelectedImage = SelectBitmapInterpolation();
            }
            else
            {
                SelectedItemIndex = 0;
            }
        }

        /// <summary>
        /// Handles changes in the SelectedImage property to update the current selected image tags.
        /// </summary>
        partial void OnSelectedImageChanged(Bitmap value)
        {
            try
            {
                UpdateCurrentSelectedTags();
            }
            catch (Exception exception)
            {
                Logger.SetLatestLogMessage($".txt or .caption file for current image not found, just type in the editor and one will be created!{Environment.NewLine}{exception.StackTrace}",
                    LogMessageColor.Warning);
                CurrentImageTags = string.Empty;
            }
            finally
            {
                CurrentAndTotal = $"Currently viewing: {SelectedItemIndex + 1}/{ImageFiles?.Count}.";
                SelectedImageFilename = $"Current file: {Path.GetFileName(ImageFiles[SelectedItemIndex])}.";
            }
        }

        /// <summary>
        /// Handles changes in the CurrentImageTags property to save the updated tags to the selected image's file.
        /// </summary>
        partial void OnCurrentImageTagsChanged(string value)
        {
            try
            {
                string txtFile = Path.ChangeExtension(ImageFiles[SelectedItemIndex], CurrentType);
                _fileManipulator.SaveTextToFile(txtFile, CurrentImageTags);
            }
            catch (NullReferenceException)
            {
                Logger.SetLatestLogMessage("You need to select a folder with image files!", LogMessageColor.Warning);
            }
        }

        /// <summary>
        /// Selects a bitmap with optional interpolation based on the size of the image.
        /// </summary>
        /// <returns>The selected bitmap.</returns>
        private Bitmap SelectBitmapInterpolation()
        {
            Bitmap imageBitmap = new Bitmap((ImageFiles[SelectedItemIndex]));
            if (imageBitmap.PixelSize.Width < 256 || imageBitmap.PixelSize.Height < 256)
            {
                double aspectRatio = (double)imageBitmap.PixelSize.Width / imageBitmap.PixelSize.Height;
                int targetWidth = 512;
                int targetHeight = 512;

                if (aspectRatio > 1)
                {
                    targetHeight = (int)(targetWidth / aspectRatio);
                }
                else
                {
                    targetWidth = (int)(targetHeight * aspectRatio);
                }

                imageBitmap = imageBitmap.CreateScaledBitmap(new PixelSize(targetWidth, targetHeight), BitmapInterpolationMode.None);
            }

            return imageBitmap;
        }

        // Command to show the full-screen image pop-up
        [RelayCommand]
        private void ShowImageFullScreen()
        {
            var popup = new DatasetProcessor.Views.ImagePopupOverlay
            {
                DataContext = this // Assuming SelectedImage is part of this view model.
            };
            popup.Show();
        }
    }

    /// <summary>
    /// Represents a single tag suggestion parsed from the CSV.
    /// </summary>
    public class TagSuggestion
    {
        public string Tag { get; set; }
        public int ColorCode { get; set; }
        public long Count { get; set; }

        public string FormattedCount => FormatCount(Count);

        // New property: collection of text parts (normal or bold)
        public ObservableCollection<TextPart> FormattedParts { get; private set; } = new ObservableCollection<TextPart>();

        // Returns a brush based on the color code.
        public IBrush ColorBrush => ColorCode switch
        {
            1 => new SolidColorBrush(Color.Parse("#ff8a8b")),
            3 => new SolidColorBrush(Color.Parse("#c797ff")),
            4 => new SolidColorBrush(Color.Parse("#35c64a")),
            5 => new SolidColorBrush(Color.Parse("#ead084")),
            _ => new SolidColorBrush(Color.Parse("#009be6")),
        };

        /// <summary>
        /// Updates FormattedParts by splitting Tag into segments that are bolded if they match any token from filter.
        /// </summary>
        public void UpdateFormattedParts(string filter)
        {
            FormattedParts.Clear();
            if (string.IsNullOrWhiteSpace(filter))
            {
                FormattedParts.Add(new TextPart { Text = Tag, IsBold = false });
                return;
            }
            
            // Split the filter string into tokens (e.g. using '_' as delimiter)
            var tokens = filter.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            var intervals = new List<(int start, int end)>();
            
            // Find all occurrences for each token (case-insensitive)
            foreach (var token in tokens)
            {
                int startIndex = 0;
                while (true)
                {
                    int index = Tag.IndexOf(token, startIndex, StringComparison.OrdinalIgnoreCase);
                    if (index < 0)
                        break;
                    intervals.Add((index, index + token.Length));
                    startIndex = index + token.Length;
                }
            }
            
            if (intervals.Count == 0)
            {
                FormattedParts.Add(new TextPart { Text = Tag, IsBold = false });
                return;
            }
            
            // Merge overlapping intervals
            intervals = intervals.OrderBy(i => i.start).ToList();
            var merged = new List<(int start, int end)>();
            var current = intervals[0];
            foreach (var interval in intervals.Skip(1))
            {
                if (interval.start <= current.end)
                {
                    current = (current.start, Math.Max(current.end, interval.end));
                }
                else
                {
                    merged.Add(current);
                    current = interval;
                }
            }
            merged.Add(current);
            
            // Create parts from the merged intervals.
            int currentIndex = 0;
            foreach (var interval in merged)
            {
                if (interval.start > currentIndex)
                {
                    FormattedParts.Add(new TextPart { Text = Tag.Substring(currentIndex, interval.start - currentIndex), IsBold = false });
                }
                FormattedParts.Add(new TextPart { Text = Tag.Substring(interval.start, interval.end - interval.start), IsBold = true });
                currentIndex = interval.end;
            }
            if (currentIndex < Tag.Length)
            {
                FormattedParts.Add(new TextPart { Text = Tag.Substring(currentIndex), IsBold = false });
            }
        }
        
        // For non-formatted display if needed.
        public string DisplayText => $"{Tag} ({FormatCount(Count)})";

        private string FormatCount(long count)
        {
            if (count >= 1_000_000)
                return (count / 1_000_000.0).ToString("0.#") + "m";
            else if (count >= 1_000)
                return (count / 1_000.0).ToString("0.#") + "k";
            else
                return count.ToString();
        }
    }

    public class TextPart
    {
        public string Text { get; set; }
        public bool IsBold { get; set; }
    }

}
