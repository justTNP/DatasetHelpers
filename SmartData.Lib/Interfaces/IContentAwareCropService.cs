﻿using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;

namespace SmartData.Lib.Interfaces
{
    public interface IContentAwareCropService
    {
        public int LanczosSamplerRadius { get; set; }

        public bool IsModelLoaded { get; }
        public string ModelPath { get; set; }
        public float ScoreThreshold { get; set; }
        public float IouThreshold { get; set; }
        public float ExpansionPercentage { get; set; }

        public Task ProcessCroppedImage(string inputPath, string outputPath, SupportedDimensions dimension);
        public Task ProcessCroppedImage(string inputPath, string outputPath, Progress progress, SupportedDimensions dimension);
    }
}
