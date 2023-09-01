﻿using Dataset_Processor_Desktop.src.Enums;
using Dataset_Processor_Desktop.src.Utilities;

using Microsoft.UI.Xaml;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System.Diagnostics;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class TagGenerationViewModel : BaseViewModel
    {
        private readonly IFileManipulatorService _fileManipulatorService;
        private readonly IAutoTaggerService _autoTaggerService;

        private string _inputFolderPath;
        public string InputFolderPath
        {
            get => _inputFolderPath;
            set
            {
                _inputFolderPath = value;
                OnPropertyChanged(nameof(InputFolderPath));
            }
        }

        private string _outputFolderPath;
        public string OutputFolderPath
        {
            get => _outputFolderPath;
            set
            {
                _outputFolderPath = value;
                OnPropertyChanged(nameof(OutputFolderPath));
            }
        }

        private Progress _predictionProgress;
        public Progress PredictionProgress
        {
            get => _predictionProgress;
            set
            {
                _predictionProgress = value;
                OnPropertyChanged(nameof(PredictionProgress));
            }
        }

        private double _threshold;
        public double Threshold
        {
            get => _threshold;
            set
            {
                try
                {
                    if (Math.Round(value, 2) != _threshold)
                    {
                        _threshold = Math.Round(value, 2);
                        OnPropertyChanged(nameof(Threshold));
                    }
                }
                catch (Exception exception)
                {
                    _loggerService.LatestLogMessage = $"{exception.Message}";
                }
            }
        }

        private readonly Stopwatch _timer;
        public TimeSpan ElapsedTime
        {
            get => _timer.Elapsed;
        }

        private bool _weightedCaptions;
        public bool WeightedCaptions
        {
            get => _weightedCaptions;
            set
            {
                _weightedCaptions = value;
                OnPropertyChanged(nameof(WeightedCaptions));
            }
        }

        private bool _appendCaptionsToFile;
        public bool AppendCaptionsToFile
        {
            get => _appendCaptionsToFile;
            set
            {
                _appendCaptionsToFile = value;
                OnPropertyChanged(nameof(AppendCaptionsToFile));
            }
        }

        public RelayCommand SelectInputFolderCommand { get; private set; }
        public RelayCommand SelectOutputFolderCommand { get; private set; }
        public RelayCommand OpenInputFolderCommand { get; private set; }
        public RelayCommand OpenOutputFolderCommand { get; private set; }
        public RelayCommand MakePredictionsCommand { get; private set; }

        public TagGenerationViewModel(IFileManipulatorService fileManipulatorService, IAutoTaggerService autoTaggerService)
        {
            _fileManipulatorService = fileManipulatorService;
            _autoTaggerService = autoTaggerService;

            InputFolderPath = _configsService.Configurations.ResizedFolder;
            _fileManipulatorService.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configsService.Configurations.CombinedOutputFolder;
            _fileManipulatorService.CreateFolderIfNotExist(OutputFolderPath);

            Threshold = _configsService.Configurations.TaggerThreshold;

            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());
            SelectOutputFolderCommand = new RelayCommand(async () => await SelectOutputFolderAsync());
            OpenInputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(InputFolderPath));
            OpenOutputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(OutputFolderPath));

            MakePredictionsCommand = new RelayCommand(async () => await MakePredictionsAsync());

            WeightedCaptions = false;

            _timer = new Stopwatch();
            TaskStatus = ProcessingStatus.Idle;
        }

        public async Task SelectInputFolderAsync()
        {
            string result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
            }
        }

        public async Task SelectOutputFolderAsync()
        {
            string result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                OutputFolderPath = result;
            }
        }

        public async Task MakePredictionsAsync()
        {
            if (PredictionProgress == null)
            {
                PredictionProgress = new Progress();
            }
            if (PredictionProgress.PercentFloat != 0f)
            {
                PredictionProgress.Reset();
            }

            _timer.Reset();
            TaskStatus = ProcessingStatus.Running;
            _autoTaggerService.Threshold = (float)Threshold;

            try
            {
                _timer.Start();
                DispatcherTimer timer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
                timer.Tick += (s, e) => OnPropertyChanged(nameof(ElapsedTime));
                timer.Start();

                if (AppendCaptionsToFile)
                {
                    await _autoTaggerService.GenerateTagsAndAppendToFile(InputFolderPath, OutputFolderPath, PredictionProgress, WeightedCaptions);
                }
                else
                {
                    await _autoTaggerService.GenerateTags(InputFolderPath, OutputFolderPath, PredictionProgress, WeightedCaptions);
                }
            }
            catch (Exception exception)
            {
                _loggerService.LatestLogMessage = $"Something went wrong! Error log will be saved inside the logs folder.";
                await _loggerService.SaveExceptionStackTrace(exception);
            }
            finally
            {
                TaskStatus = ProcessingStatus.Finished;
                _timer.Stop();
            }
        }
    }
}
