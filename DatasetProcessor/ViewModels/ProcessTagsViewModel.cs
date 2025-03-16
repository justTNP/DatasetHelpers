using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace DatasetProcessor.ViewModels
{
    public partial class ProcessTagsViewModel : BaseViewModel
    {
        private readonly ITagProcessorService _tagProcessor;
        private readonly IFileManipulatorService _fileManipulator;

        private const string _invalidMinSharpenNumberMessage = "File names must be a number between 1 and 2147483647.";

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _tagsToAdd;
        [ObservableProperty]
        private string _tagsToEmphasize;
        [ObservableProperty]
        private string _tagsToRemove;
        [ObservableProperty]
        private string _tagsToReplace;
        [ObservableProperty]
        private string _tagsToBeReplaced;
        [ObservableProperty]
        private Progress _tagProcessingProgress;
        [ObservableProperty]
        private bool _randomizeTags;
        [ObservableProperty]
        private bool _renameFilesToCrescent;
        [ObservableProperty]
        private bool _applyRedundancyRemoval;
        [ObservableProperty]
        private bool _applyConsolidateTags;
        [ObservableProperty]
        private ObservableCollection<string> _sortedByFrequencyList;
        [ObservableProperty]
        private string _existingTagToFind;
        [ObservableProperty]
        private string _tagsToAppend;

        private int? _startingNumberForFileNames;
        public string StartingNumberForFileNames
        {
            get => _startingNumberForFileNames.ToString();
            set
            {
                try
                {
                    int parsedValue = int.Parse(value);
                    if (parsedValue <= 0 || parsedValue > int.MaxValue)
                    {
                        Logger.SetLatestLogMessage($"{_invalidMinSharpenNumberMessage}{Environment.NewLine}This value will be clampled to a valid number before processing!",
                            LogMessageColor.Warning);
                    }
                    else
                    {
                        Logger.SetLatestLogMessage(string.Empty, LogMessageColor.Error);
                    }

                    _startingNumberForFileNames = parsedValue;
                    OnPropertyChanged(nameof(StartingNumberForFileNames));
                }
                catch
                {
                    _startingNumberForFileNames = null;
                    Logger.SetLatestLogMessage($"{_invalidMinSharpenNumberMessage}{Environment.NewLine}This value cannot be empty! Use at least 1 as its minimum valid number.",
                        LogMessageColor.Warning);
                }
            }
        }

        private readonly Stopwatch _timer;
        public TimeSpan ElapsedTime => _timer.Elapsed;

        [ObservableProperty]
        private bool _isUiEnabled;
        [ObservableProperty]
        private bool _isCancelEnabled;

        public ProcessTagsViewModel(ITagProcessorService tagProcessor, IFileManipulatorService fileManipulator,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _tagProcessor = tagProcessor;
            _fileManipulator = fileManipulator;

            (_tagProcessor as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                TagProcessingProgress = ResetProgress(TagProcessingProgress);
                TagProcessingProgress.TotalFiles = args;
            };
            (_tagProcessor as INotifyProgress).ProgressUpdated += (sender, args) => TagProcessingProgress.UpdateProgress();

            InputFolderPath = _configs.Configurations.ProcessTagsConfigs.InputFolder;
            _fileManipulator.CreateFolderIfNotExist(InputFolderPath);
            RandomizeTags = _configs.Configurations.ProcessTagsConfigs.RandomizeTags;
            RenameFilesToCrescent = _configs.Configurations.ProcessTagsConfigs.RenameFiles;
            ApplyRedundancyRemoval = _configs.Configurations.ProcessTagsConfigs.ApplyRedudancyRemoval;
            ApplyConsolidateTags = _configs.Configurations.ProcessTagsConfigs.ConsolidateTags;

            // Initialize new properties
            ExistingTagToFind = string.Empty;
            TagsToAppend = string.Empty;
            
            SortedByFrequencyList = new ObservableCollection<string>();
            _timer = new Stopwatch();

            IsUiEnabled = true;
        }

        [RelayCommand]
        private async Task SelectInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
            }
        }

        [RelayCommand]
        private async Task ProcessTagsAsync()
        {
            IsUiEnabled = false;

            _timer.Reset();
            _timer.Start();
            DispatcherTimer timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            timer.Tick += (sender, eventArgs) => OnPropertyChanged(nameof(ElapsedTime));
            timer.Start();

            TaskStatus = ProcessingStatus.Running;

            try
            {
                // When append tags are provided, use ExistingTagToFind as the target left input.
                // Otherwise, fall back to the original TagsToAdd.
                string targetTag = string.IsNullOrWhiteSpace(TagsToAppend) ? TagsToAdd : ExistingTagToFind;
                await _tagProcessor.ProcessAllTagFiles(InputFolderPath, targetTag, TagsToEmphasize, TagsToRemove, TagsToAppend);
                if (RandomizeTags)
                {
                    TagProcessingProgress.Reset();
                    await _tagProcessor.RandomizeTagsOfFiles(InputFolderPath);
                }
                if (RenameFilesToCrescent)
                {
                    Logger.SetLatestLogMessage("Renaming files... Renaming operation can't be cancelled!", LogMessageColor.Informational);
                    IsCancelEnabled = false;
                    TagProcessingProgress.Reset();
                    if (_startingNumberForFileNames == null)
                    {
                        _startingNumberForFileNames = 1;
                    }
                    await _fileManipulator.RenameAllToCrescentAsync(InputFolderPath, (int)_startingNumberForFileNames);
                    IsCancelEnabled = true;
                }
                if (ApplyConsolidateTags)
                {
                    TagProcessingProgress.Reset();
                    await _tagProcessor.ConsolidateTagsAndLogEdgeCases(InputFolderPath, Logger);
                }
                if (ApplyRedundancyRemoval)
                {
                    TagProcessingProgress.Reset();
                    await _tagProcessor.ApplyRedundancyRemovalToFiles(InputFolderPath);
                }
            }
            catch (ArgumentException ex)
            {
                Logger.SetLatestLogMessage($"Invalid input: {ex.Message}", LogMessageColor.Error);
            }
            catch (OperationCanceledException)
            {
                IsCancelEnabled = false;
                Logger.SetLatestLogMessage($"Cancelled the current operation!", LogMessageColor.Informational);
            }
            catch (IOException)
            {
                Logger.SetLatestLogMessage($"Images and Tag files are named in crescent order already!",
                        LogMessageColor.Warning);
            }
            catch (Exception exception)
            {
                Logger.SetLatestLogMessage($"Something went wrong! Error log will be saved inside the logs folder.",
                        LogMessageColor.Error);
                await Logger.SaveExceptionStackTrace(exception);
            }
            finally
            {
                IsUiEnabled = true;
                TaskStatus = ProcessingStatus.Finished;
            }

            if (!string.IsNullOrEmpty(TagsToReplace))
            {
                TaskStatus = ProcessingStatus.Running;
                TagProcessingProgress.Reset();

                try
                {
                    await _tagProcessor.ProcessTagsReplacement(InputFolderPath, TagsToBeReplaced, TagsToReplace);
                }
                catch (OperationCanceledException)
                {
                    IsCancelEnabled = false;
                    Logger.SetLatestLogMessage($"Cancelled the current operation!", LogMessageColor.Informational);
                }
                catch (Exception exception)
                {
                    Logger.SetLatestLogMessage($"Something went wrong! Error log will be saved inside the logs folder.",
                            LogMessageColor.Error);
                    await Logger.SaveExceptionStackTrace(exception);
                }
                finally
                {
                    IsUiEnabled = true;
                    TaskStatus = ProcessingStatus.Finished;
                }
            }

            _timer.Stop();
            timer.Stop();
        }

        [RelayCommand]
        private async Task CalculateByFrequencyAsync()
        {
            try
            {
                // Get the frequency analysis result as a single string.
                string result = _tagProcessor.CalculateListOfMostFrequentTags(InputFolderPath);
                
                // Split the result into lines (assuming each frequency is on a new line).
                var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                
                // Option 1: Replace the collection entirely:
                SortedByFrequencyList = new ObservableCollection<string>(lines);
                
                // Option 2: Alternatively, update the existing collection:
                // SortedByFrequencyList.Clear();
                // foreach(var line in lines)
                // {
                //     SortedByFrequencyList.Add(line);
                // }
            }
            catch (Exception exception)
            {
                Logger.SetLatestLogMessage($"Something went wrong! Error log will be saved inside the logs folder.",
                    LogMessageColor.Error);
                await Logger.SaveExceptionStackTrace(exception);
            }
        }

        [RelayCommand]
        private void CancelTask()
        {
            (_fileManipulator as ICancellableService)?.CancelCurrentTask();
            (_tagProcessor as ICancellableService)?.CancelCurrentTask();
        }

        partial void OnIsUiEnabledChanged(bool value)
        {
            if (value == true)
            {
                IsCancelEnabled = false;
            }
            else
            {
                IsCancelEnabled = true;
            }
        }
    }
}
