using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;

namespace DatasetProcessor.Views
{
    public partial class ImagePopupOverlay : Window
    {
        private double currentZoom = 1.0;
        private const double minZoom = 1.0;
        private const double maxZoom = 10.0; // 1000%
        private const double zoomStep = 0.1;
        
        private bool isDragging = false;
        private Point lastDragPosition;
        private Point imageOffset = new Point(0, 0);
        
        // Make the flag static so it persists across instances
        private static bool helpOverlayDismissed = false;

        public ImagePopupOverlay()
        {
            InitializeComponent();
            
            // Set focus so that key events (Escape) are received
            this.Focus();
            
            // Apply the help overlay state immediately based on previous instances
            if (helpOverlayDismissed)
            {
                HelpOverlay.IsVisible = false;
            }
            else
            {
                // Hide help overlay after 5 seconds if not already dismissed
                var timer = new Avalonia.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(5)
                };
                timer.Tick += (s, e) => 
                {
                    HelpOverlay.IsVisible = false;
                    helpOverlayDismissed = true;
                    timer.Stop();
                };
                timer.Start();
            }
        }

        private void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            else if (e.Key == Key.H)
            {
                // Toggle help overlay visibility
                if (HelpOverlay.IsVisible)
                {
                    HelpOverlay.IsVisible = false;
                    helpOverlayDismissed = true;
                }
                else if (!helpOverlayDismissed)
                {
                    HelpOverlay.IsVisible = true;
                }
            }
        }

        private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(this);
            
            // Right click to dismiss the overlay
            if (pointerPoint.Properties.IsRightButtonPressed)
            {
                this.Close();
                return;
            }
            
            // Left click to start dragging
            if (pointerPoint.Properties.IsLeftButtonPressed && currentZoom > minZoom)
            {
                isDragging = true;
                lastDragPosition = e.GetPosition(this);
                this.Cursor = new Cursor(StandardCursorType.Hand);
            }
        }

        private void Window_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (isDragging)
            {
                Point currentPosition = e.GetPosition(this);
                Vector delta = currentPosition - lastDragPosition;
                
                // Update the offset
                imageOffset = new Point(imageOffset.X + delta.X, imageOffset.Y + delta.Y);
                lastDragPosition = currentPosition;
                
                // Apply the transform
                UpdateImageTransform();
            }
        }

        private void Window_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                this.Cursor = new Cursor(StandardCursorType.Arrow);
            }
        }
        
        private void Window_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            // Get the pointer position relative to the image
            Point pointerPosition = e.GetPosition(FullScreenImage);
            
            // Calculate zoom change based on wheel delta
            double zoomDelta = e.Delta.Y * zoomStep;
            double newZoom = Math.Clamp(currentZoom + zoomDelta, minZoom, maxZoom);
            
            if (newZoom != currentZoom)
            {
                // If zooming out to 100%, reset to center
                if (currentZoom > minZoom && newZoom == minZoom)
                {
                    currentZoom = minZoom;
                    imageOffset = new Point(0, 0);
                    UpdateImageTransform();
                    return;
                }
                
                // Calculate the point in the image where the pointer is before zooming
                double beforeX = (pointerPosition.X - imageOffset.X) / currentZoom;
                double beforeY = (pointerPosition.Y - imageOffset.Y) / currentZoom;
                
                // Update the zoom level
                currentZoom = newZoom;
                
                // Calculate where the same point will be after zooming
                double afterX = beforeX * currentZoom;
                double afterY = beforeY * currentZoom;
                
                // Adjust the offset to keep the pointer over the same point in the image
                imageOffset = new Point(
                    imageOffset.X + (pointerPosition.X - (afterX + imageOffset.X)),
                    imageOffset.Y + (pointerPosition.Y - (afterY + imageOffset.Y))
                );
                
                // Apply the transform
                UpdateImageTransform();
            }
        }
        
        private void UpdateImageTransform()
        {
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(currentZoom, currentZoom));
            transformGroup.Children.Add(new TranslateTransform(imageOffset.X, imageOffset.Y));
            FullScreenImage.RenderTransform = transformGroup;
            
            // Update the zoom level text
            int zoomPercentage = (int)(currentZoom * 100);
            ZoomLevelText.Text = $"Zoom: {zoomPercentage}%";
        }
    }
}