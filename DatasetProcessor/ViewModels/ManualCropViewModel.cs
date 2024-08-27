using Avalonia.Media.Imaging;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SmartData.Lib.Enums;
using SmartData.Lib.Interfaces;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class ManualCropViewModel : BaseViewModel
    {
        private readonly IImageProcessorService _imageProcessor;
        private readonly IFileManipulatorService _fileManipulator;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _outputFolderPath;
        [ObservableProperty]
        private List<string> _imageFiles;
        [ObservableProperty]
        private string _totalImageFiles;
        [ObservableProperty]
        private int _selectedItemIndex;
        [ObservableProperty]
        private Bitmap? _selectedImage;
        [ObservableProperty]
        private string _selectedImageFilename;
        [ObservableProperty]
        private string _currentAndTotal;
        [ObservableProperty]
        private Point _imageSize;
        private bool _imageWasDownscaled;

        [ObservableProperty]
        private bool _buttonEnabled;
        [ObservableProperty]
        private bool _isUiEnabled;

        [ObservableProperty]
        private Point _startingPosition;
        [ObservableProperty]
        private Point _endingPosition;

        [ObservableProperty]
        private AspectRatios _aspectRatio;

        [ObservableProperty]
        private bool _enableMultipleCrops;

        public ManualCropViewModel(IImageProcessorService imageProcessor, IFileManipulatorService fileManipulator,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _imageProcessor = imageProcessor;
            _fileManipulator = fileManipulator;

            ButtonEnabled = true;
            IsUiEnabled = true;

            SelectedItemIndex = 0;

            StartingPosition = Point.Empty;
            EndingPosition = Point.Empty;

            InputFolderPath = _configs.Configurations.ManualCropConfigs.InputFolder;
            OutputFolderPath = _configs.Configurations.ManualCropConfigs.OutputFolder;
            ImageFiles = new List<string>();
            CurrentAndTotal = string.Empty;
            SelectedImageFilename = string.Empty;
            TotalImageFiles = string.Empty;
        }

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

        [RelayCommand]
        private async Task SelectOutputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                OutputFolderPath = result;
            }
        }

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

        [RelayCommand]
        private void CopyCurrentImage()
        {
            if (!string.IsNullOrEmpty(SelectedImageFilename) && !string.IsNullOrEmpty(OutputFolderPath))
            {
                string currentImage = ImageFiles[SelectedItemIndex];
                string outputPath = Path.Combine(OutputFolderPath, Path.GetFileName(currentImage));
                File.Copy(currentImage, outputPath);
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
                    ImageFiles = ImageFiles.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x)))
                        .ToList();
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
                    SelectedImage = new Bitmap(ImageFiles[SelectedItemIndex]);
                }
            }
        }

        /// <summary>
        /// Handles changes in the SelectedItemIndex property to ensure it stays within the valid range.
        /// </summary>
        partial void OnSelectedItemIndexChanged(int value)
        {
            if (ImageFiles?.Count > 0)
            {
                SelectedItemIndex = Math.Clamp(value, 0, ImageFiles.Count - 1);
                SelectedImage = new Bitmap((ImageFiles[SelectedItemIndex]));
            }
            else
            {
                SelectedItemIndex = 0;
            }
        }

        /// <summary>
        /// Handles changes in the SelectedImage property to update the current selected image tags.
        /// </summary>
        partial void OnSelectedImageChanged(Bitmap? value)
        {
            CurrentAndTotal = $"Currently viewing: {SelectedItemIndex + 1}/{ImageFiles?.Count}.";
            SelectedImageFilename = $"Current file: {Path.GetFileName(ImageFiles?[SelectedItemIndex])}.";
            if (value == null)
            {
                return;
            }

            double widthScale = 768.0 / value.Size.Width;
            double heightScale = 768.0 / value.Size.Height;
            double scaleFactor = Math.Min(widthScale, heightScale);

            if (scaleFactor < 1.0)
            {
                ImageSize = new Point((int)(value.Size.Width * scaleFactor), (int)(value.Size.Height * scaleFactor));
                _imageWasDownscaled = true;
            }
            else
            {
                ImageSize = new Point((int)value.Size.Width, (int)value.Size.Height);
                _imageWasDownscaled = false;
            }
        }

        /// <summary>
        /// Handles changes in the ImageFiles property to update the total image files count.
        /// </summary>
        partial void OnImageFilesChanged(List<string> value)
        {
            TotalImageFiles = $"Total files found: {ImageFiles.Count.ToString()}.";
        }

        /// <summary>
        /// Handles a button down event and navigates to the item with the specified index.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        /// <param name="index">The index of the item to navigate to.</param>
        private void OnNavigationButtonDown(string index)
        {
            if (IsActive)
            {
                Dispatcher.UIThread.InvokeAsync(() => GoToItem(index));
            }
        }

        /// <summary>
        /// Handles the change event of the ending position of the crop region.
        /// </summary>
        /// <param name="value">The new ending position of the crop region.</param>
        /// <remarks>
        /// This method is triggered when the ending position of the crop region changes.
        /// It checks if an image is selected and if an output folder path is specified.
        /// If both conditions are met, it asynchronously crops the image based on the specified crop region
        /// (defined by the starting and ending positions) and saves the cropped image to the output folder.
        /// If no output folder is specified, it logs a message indicating that an output folder needs to be selected.
        /// If an error occurs during the cropping process, it throws an exception.
        /// </remarks>
        partial void OnEndingPositionChanged(Point value)
        {
            if (SelectedImage == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(OutputFolderPath))
            {
                Logger.SetLatestLogMessage("You need to first select a folder for the output files! Image won't be saved.",
                    LogMessageColor.Warning);
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    Point startingPosition = StartingPosition;
                    Point endingPosition;

                    if (AspectRatio != AspectRatios.AspectRatioFree)
                    {
                        double aspectRatio = GetAspectRatio(AspectRatio);
                        double rectWidth = Math.Abs(EndingPosition.X - startingPosition.X);
                        double rectHeight = Math.Abs(EndingPosition.Y - startingPosition.Y);

                        if (rectWidth / rectHeight > aspectRatio)
                        {
                            rectHeight = rectWidth / aspectRatio;
                        }
                        else
                        {
                            rectWidth = rectHeight * aspectRatio;
                        }

                        if (EndingPosition.X < startingPosition.X)
                        {
                            if (EndingPosition.Y < startingPosition.Y)
                            {
                                endingPosition = new Point(startingPosition.X - (int)rectWidth, startingPosition.Y - (int)rectHeight);
                            }
                            else
                            {
                                endingPosition = new Point(startingPosition.X - (int)rectWidth, startingPosition.Y + (int)rectHeight);
                            }
                        }
                        else
                        {
                            if (EndingPosition.Y < startingPosition.Y)
                            {
                                endingPosition = new Point(startingPosition.X + (int)rectWidth, startingPosition.Y - (int)rectHeight);
                            }
                            else
                            {
                                endingPosition = new Point(startingPosition.X + (int)rectWidth, startingPosition.Y + (int)rectHeight);
                            }
                        }
                    }
                    else
                    {
                        endingPosition = EndingPosition;
                    }

                    if (_imageWasDownscaled)
                    {
                        startingPosition = new Point((int)(startingPosition.X / (ImageSize.X / (float)SelectedImage.PixelSize.Width)),
                            (int)(startingPosition.Y / (ImageSize.Y / (float)SelectedImage.PixelSize.Height)));
                        endingPosition = new Point((int)(endingPosition.X / (ImageSize.X / (float)SelectedImage.PixelSize.Width)),
                            (int)(endingPosition.Y / (ImageSize.Y / (float)SelectedImage.PixelSize.Height)));
                    }

                    // Check if the ending position is within the image display
                    if (endingPosition.X < 0 || endingPosition.X > SelectedImage.PixelSize.Width ||
                        endingPosition.Y < 0 || endingPosition.Y > SelectedImage.PixelSize.Height)
                    {
                        Logger.SetLatestLogMessage("The crop area extends outside the image display. Please adjust the crop area.",
                            LogMessageColor.Warning);
                        return;
                    }

                    // Ensure the starting position is always the top-left corner and the ending position is the bottom-right corner
                    int cropX = Math.Min(startingPosition.X, endingPosition.X);
                    int cropY = Math.Min(startingPosition.Y, endingPosition.Y);
                    int cropWidth = Math.Abs(endingPosition.X - startingPosition.X);
                    int cropHeight = Math.Abs(endingPosition.Y - startingPosition.Y);

                    await _imageProcessor.CropImageAsync(ImageFiles[SelectedItemIndex], OutputFolderPath, new Point(cropX, cropY), new Point(cropX + cropWidth, cropY + cropHeight), EnableMultipleCrops);
                }
                catch (ArgumentOutOfRangeException)
                {
                    Logger.SetLatestLogMessage("An error occurred while trying to crop the image. Be sure the crop area is bigger than 0 pixels in both Width and Height!",
                        LogMessageColor.Warning);
                }
            });
        }

        private double GetAspectRatio(AspectRatios ratio)
        {
            return ratio switch
            {
                AspectRatios.AspectRatio1x1 => 1.0,
                AspectRatios.AspectRatio4x3 => 4.0 / 3.0,
                AspectRatios.AspectRatio3x4 => 3.0 / 4.0,
                AspectRatios.AspectRatio3x2 => 3.0 / 2.0,
                AspectRatios.AspectRatio2x3 => 2.0 / 3.0,
                AspectRatios.AspectRatio16x9 => 16.0 / 9.0,
                AspectRatios.AspectRatio9x16 => 9.0 / 16.0,
                AspectRatios.AspectRatio13x19 => 13.0 / 19.0,
                AspectRatios.AspectRatio19x13 => 19.0 / 13.0,
                _ => throw new ArgumentOutOfRangeException(nameof(ratio)),
            };
        }
        private string GenerateFileName(string originalFileName, int width, int height)
        {
            if (!EnableMultipleCrops)
            {
                return originalFileName;
            }

            string baseFileName = $"{originalFileName}-{width}x{height}";
            string fullPath = Path.Combine(OutputFolderPath, $"{baseFileName}{Path.GetExtension(ImageFiles[SelectedItemIndex])}");
            int counter = 1;

            while (File.Exists(fullPath))
            {
                string newFileName = $"{baseFileName}-{counter}";
                fullPath = Path.Combine(OutputFolderPath, $"{newFileName}{Path.GetExtension(ImageFiles[SelectedItemIndex])}");
                counter++;
            }

            return Path.GetFileNameWithoutExtension(fullPath);
        }
    }
}
