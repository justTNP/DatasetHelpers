using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using DatasetProcessor.ViewModels;

using System;
using System.ComponentModel;
using System.Drawing;

using SmartData.Lib.Enums;

namespace DatasetProcessor.Views
{
    public partial class ManualCropView : UserControl
    {
        private ManualCropViewModel? _viewModel;

        private bool _isDragging = false;
        private bool _shouldSaveCroppedImage = true;
        private Point _startingPosition = Point.Empty;

        Line[] _lines;

        public ManualCropView()
        {
            InitializeComponent();
            _lines = new Line[4];
            SolidColorBrush brush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(255, 255, 179, 71), 0.5f);
            for (int i = 0; i < _lines.Length; i++)
            {
                _lines[i] = new Line()
                {
                    StrokeThickness = 3,
                    Stroke = brush
                };
                CanvasPanel.Children.Add(_lines[i]);
            }

            // Add KeyDown event handler
            KeyDown += ManualCropView_KeyDown;
            PointerPressed += ManualCropView_PointerPressed;
        }

        /// <summary>
        /// Clears the lines representing the crop area by setting their stroke thickness to 0.
        /// </summary>
        /// <param name="sender">The object that triggered the PropertyChanged event.</param>
        /// <param name="e">The PropertyChangedEventArgs object containing information about the property change.</param>
        private void ClearLines(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("SelectedItemIndex"))
            {
                for (int i = 0; i < _lines.Length; i++)
                {
                    _lines[i].StrokeThickness = 0;
                }
            }
        }

        /// <summary>
        /// Handles the pointer press event on the canvas.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void CanvasPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender != null && e != null && _viewModel != null)
            {
                Avalonia.Point clickPosition = e.GetPosition(sender as Button);
                _startingPosition = new Point((int)clickPosition.X, (int)clickPosition.Y);
                _viewModel.StartingPosition = new Point((int)clickPosition.X, (int)clickPosition.Y);
                _isDragging = true;

                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the pointer movement event on the canvas.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void PointerMoving(object? sender, PointerEventArgs e)
        {
            if (!_isDragging)
            {
                return;
            }

            if (e.GetCurrentPoint(sender as Button).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                _isDragging = false;
                _shouldSaveCroppedImage = false;
                e.Handled = true;
                for (int i = 0; i < _lines.Length; i++)
                {
                    _lines[i].StrokeThickness = 0;
                }

                return;
            }

            Avalonia.Point cursorPosition = e.GetPosition(sender as Button);
            DrawCropAreaRectangle(cursorPosition);
            _shouldSaveCroppedImage = true;
        }

        /// <summary>
        /// Handles the pointer release event on the canvas.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void CanvasReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_shouldSaveCroppedImage)
            {
                e.Handled = true;
                return;
            }

            if (sender != null && e != null && _viewModel != null)
            {
                Avalonia.Point clickPosition = e.GetPosition(sender as Button);
                _viewModel.EndingPosition = new Point((int)clickPosition.X, (int)clickPosition.Y);
                _isDragging = false;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Draws the rectangle representing the crop area.
        /// </summary>
        /// <param name="cursorPosition">The current cursor position.</param>
        private void DrawCropAreaRectangle(Avalonia.Point cursorPosition)
        {
            for (int i = 0; i < _lines.Length; i++)
            {
                _lines[i].StrokeThickness = 3;
            }

            if (_viewModel.AspectRatio != AspectRatios.AspectRatioFree)
            {
                double aspectRatio = GetAspectRatio(_viewModel.AspectRatio);
                double width = Math.Abs(cursorPosition.X - _startingPosition.X);
                double height = Math.Abs(cursorPosition.Y - _startingPosition.Y);

                if (width / height > aspectRatio)
                {
                    height = width / aspectRatio;
                }
                else
                {
                    width = height * aspectRatio;
                }

                Avalonia.Point endPosition;
                if (cursorPosition.X < _startingPosition.X)
                {
                    if (cursorPosition.Y < _startingPosition.Y)
                    {
                        endPosition = new Avalonia.Point(_startingPosition.X - width, _startingPosition.Y - height);
                    }
                    else
                    {
                        endPosition = new Avalonia.Point(_startingPosition.X - width, _startingPosition.Y + height);
                    }
                }
                else
                {
                    if (cursorPosition.Y < _startingPosition.Y)
                    {
                        endPosition = new Avalonia.Point(_startingPosition.X + width, _startingPosition.Y - height);
                    }
                    else
                    {
                        endPosition = new Avalonia.Point(_startingPosition.X + width, _startingPosition.Y + height);
                    }
                }

                // TOP LINE
                _lines[0].StartPoint = new Avalonia.Point(_startingPosition.X, _startingPosition.Y);
                _lines[0].EndPoint = new Avalonia.Point(endPosition.X, _startingPosition.Y);

                // LEFT LINE
                _lines[1].StartPoint = new Avalonia.Point(_startingPosition.X, _startingPosition.Y);
                _lines[1].EndPoint = new Avalonia.Point(_startingPosition.X, endPosition.Y);

                // BOTTOM LINE
                _lines[2].StartPoint = new Avalonia.Point(_startingPosition.X, endPosition.Y);
                _lines[2].EndPoint = new Avalonia.Point(endPosition.X, endPosition.Y);

                // RIGHT LINE
                _lines[3].StartPoint = new Avalonia.Point(endPosition.X, _startingPosition.Y);
                _lines[3].EndPoint = new Avalonia.Point(endPosition.X, endPosition.Y);
            }
            else
            {
                // TOP LINE
                _lines[0].StartPoint = new Avalonia.Point(_startingPosition.X, _startingPosition.Y);
                _lines[0].EndPoint = new Avalonia.Point(cursorPosition.X, _startingPosition.Y);

                // LEFT LINE
                _lines[1].StartPoint = new Avalonia.Point(_startingPosition.X, _startingPosition.Y);
                _lines[1].EndPoint = new Avalonia.Point(_startingPosition.X, cursorPosition.Y);

                // BOTTOM LINE
                _lines[2].StartPoint = new Avalonia.Point(_startingPosition.X, cursorPosition.Y);
                _lines[2].EndPoint = new Avalonia.Point(cursorPosition.X, cursorPosition.Y);

                // RIGHT LINE
                _lines[3].StartPoint = new Avalonia.Point(cursorPosition.X, _startingPosition.Y);
                _lines[3].EndPoint = new Avalonia.Point(cursorPosition.X, cursorPosition.Y);
            }
        }

        /// <summary>
        /// Overrides the DataContextChanged method to update the associated view model.
        /// </summary>
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            _viewModel = DataContext as ManualCropViewModel;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ManualCropViewModel.InputFolderPath) ||
                e.PropertyName == nameof(ManualCropViewModel.OutputFolderPath))
            {
                Focus();
            }
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

        private void ManualCropView_KeyDown(object? sender, KeyEventArgs e)
        {
            if (_viewModel == null) return;

            switch (e.Key)
            {
                case Key.F1:
                    _viewModel.GoToItemCommand.Execute("-1");
                    break;
                case Key.F2:
                    _viewModel.GoToItemCommand.Execute("1");
                    break;
                case Key.F3:
                    _viewModel.GoToItemCommand.Execute("-10");
                    break;
                case Key.F4:
                    _viewModel.GoToItemCommand.Execute("10");
                    break;
                case Key.F5:
                    _viewModel.GoToItemCommand.Execute("-100");
                    break;
                case Key.F6:
                    _viewModel.GoToItemCommand.Execute("100");
                    break;
            }
        }

        private void ManualCropView_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_viewModel == null) return;

            var properties = e.GetCurrentPoint(this).Properties;

            if (properties.IsXButton1Pressed) // Mouse Button 4 (Back)
            {
                _viewModel.GoToItemCommand.Execute("-1");
                e.Handled = true;
            }
            else if (properties.IsXButton2Pressed) // Mouse Button 5 (Forward)
            {
                _viewModel.GoToItemCommand.Execute("1");
                e.Handled = true;
            }
        }
    }
}
