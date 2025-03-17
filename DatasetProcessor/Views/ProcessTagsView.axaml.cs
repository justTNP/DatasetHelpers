using Avalonia.Controls;
using Avalonia.Input;

using DatasetProcessor.ViewModels;

using System.Text.RegularExpressions;

namespace DatasetProcessor.Views
{
    public partial class ProcessTagsView : UserControl
    {
        public ProcessTagsView()
        {
            InitializeComponent();
        }

        private void TagDoubleTapped(object? sender, TappedEventArgs e)
        {
            // Get the clicked item from the ListBox
            if (sender is ListBox listBox && listBox.SelectedItem is string tag)
            {
                // Remove trailing "=" followed by digits (and an optional comma)
                string processedTag = Regex.Replace(tag, @"=[0-9]+,?$", "");

                if (DataContext is ProcessTagsViewModel vm)
                {
                    // Check if the Shift key was held during the double-click
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    {
                        vm.TagsToEmphasize = AppendTag(vm.TagsToEmphasize, processedTag);
                    }
                    else
                    {
                        vm.TagsToRemove = AppendTag(vm.TagsToRemove, processedTag);
                    }
                }
            }
        }

        /// <summary>
        /// Appends the new tag to the existing string, adding a comma and space if needed.
        /// </summary>
        private string AppendTag(string current, string newTag)
        {
            if (string.IsNullOrWhiteSpace(current))
            {
                return newTag;
            }
            else if (current.EndsWith(","))
            {
                // If it already ends with a comma (but no trailing space), add a space then the tag.
                return current + " " + newTag;
            }
            else
            {
                // Otherwise, append a comma and space then the new tag.
                return current + ", " + newTag;
            }
        }
    }
}
