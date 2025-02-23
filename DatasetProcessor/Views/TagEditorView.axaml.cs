using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Input;

using DatasetProcessor.src.Classes;
using DatasetProcessor.ViewModels;

using SmartData.Lib.Interfaces;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace DatasetProcessor.Views
{
    /// <summary>
    /// A view for editing tags, with the ability to highlight specific tags by changing their text color.
    /// </summary>
    public partial class TagEditorView : UserControl
    {
        private readonly IInputHooksService _inputHooks;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private TimeSpan _highlightUpdateDelay = TimeSpan.FromSeconds(0.75);

        private TagEditorViewModel? _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="TagEditorView"/> class.
        /// </summary>
        public TagEditorView() : this(null)
        {
            // This constructor is required for design-time preview and runtime XAML loading
        }

        public TagEditorView(IInputHooksService inputHooks)
        {
            _inputHooks = inputHooks;
            InitializeComponent();

            EditorHighlight.TextChanged += async (sender, args) => await DebounceOnTextChangedAsync(() => OnEditorHighlightTextChanged(sender, args));
            EditorTags.TextChanged += async (sender, args) => await OnEditorTextChangedAsync(sender, args);
        }

        /// <summary>
        /// Handles the TextChanged event of the EditorHighlight control to update tag highlighting.
        /// TODO: Allow the user to change highlight colors in Settings.
        /// </summary>
        private void OnEditorHighlightTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(EditorHighlight.Text))
            {
                EditorTags.SyntaxHighlighting = null;
                return;
            }

            if (_viewModel != null)
            {
                string[] tagsToHighlight = EditorHighlight.Text.Replace(", ", ",").Split(",");

                List<Color> highlightColors = [.. _viewModel.HighlightColors.Select(Color.Parse)];

                EditorTags.SyntaxHighlighting = new TagsSyntaxHighlight(highlightColors, tagsToHighlight);
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the EditorTags control to process changes in tags.
        /// </summary>
        private async Task OnEditorTextChangedAsync(object? sender, EventArgs args)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnTagsPropertyChanged;
                _viewModel.CurrentImageTags = EditorTags.Text;
                _viewModel.PropertyChanged += OnTagsPropertyChanged;

                await _viewModel.CountTokensForCurrentImageCommand.ExecuteAsync(null);
            }
        }

        /// <summary>
        /// Overrides the DataContextChanged method to update the associated view model.
        /// </summary>
        protected override void OnDataContextChanged(EventArgs e)
        {
            _viewModel = DataContext as TagEditorViewModel;
            _viewModel!.PropertyChanged += OnTagsPropertyChanged;

            _inputHooks.ButtonF1 += async (sender, args) => await OnNavigationButtonDown("-1");
            _inputHooks.ButtonF2 += async (sender, args) => await OnNavigationButtonDown("1");
            _inputHooks.ButtonF3 += async (sender, args) => await OnNavigationButtonDown("-10");
            _inputHooks.ButtonF4 += async (sender, args) => await OnNavigationButtonDown("10");
            _inputHooks.ButtonF5 += async (sender, args) => await OnNavigationButtonDown("-100");
            _inputHooks.ButtonF6 += async (sender, args) => await OnNavigationButtonDown("100");
            _inputHooks.ButtonF8 += async (sender, args) => await _viewModel.OpenFileAsync();

            _inputHooks.MouseButton3 += async (sender, args) => await _viewModel.OpenFileAsync();
            _inputHooks.MouseButton4 += async (sender, args) => await OnNavigationButtonDown("-1");
            _inputHooks.MouseButton5 += async (sender, args) => await OnNavigationButtonDown("1");

            _inputHooks.AltLeftArrowCombo += async (sender, args) => await OnNavigationButtonDown("-1");
            _inputHooks.AltRightArrowCombo += async (sender, args) => await OnNavigationButtonDown("1");

            base.OnDataContextChanged(e);
        }

        /// <summary>
        /// Asynchronously debounces an action to execute after a specified delay.
        /// </summary>
        private async Task DebounceOnTextChangedAsync(Action action)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Delay(_highlightUpdateDelay, _cancellationTokenSource.Token);
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    action.Invoke();
                }
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("Task canceled.");
            }
        }

        /// <summary>
        /// Handles property changes in the associated view model to update tag text.
        /// </summary>
        private void OnTagsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.CurrentImageTags))
            {
                EditorTags.Text = _viewModel.CurrentImageTags;
            }
        }

        /// <summary>
        /// Add a tag to the current image via double-clicking.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TagDoubleTapped(object? sender, TappedEventArgs e)
        {
            // Ensure the sender is a TextBlock with the proper DataContext.
            if (sender is TextBlock textBlock && textBlock.DataContext is TagSuggestion suggestion)
            {
                // If the Shift key is held down, open a webpage.
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    // Build a URL based on the tag.
                    // Here, we're using the tag directly; you may need to URL encode it.
                    string url = "https://danbooru.donmai.us/wiki_pages/" + Uri.EscapeDataString(suggestion.Tag);
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        // Optionally log the error or notify the user.
                        Debug.WriteLine("Error opening URL: " + ex.Message);
                    }
                    return;
                }

                // Otherwise, proceed to append the tag to the TextEditor.
                string cleanTag = suggestion.Tag.Replace("_", " ");

                // Reference the TextEditor by its name (assumed to be "EditorTags").
                // Append the tag according to the current content.
                if (!string.IsNullOrWhiteSpace(EditorTags.Text))
                {
                    // If there is existing content, prepend with a comma and space.
                    EditorTags.Text += ", " + cleanTag;
                }
                else
                {
                    // If the TextEditor is empty, append the tag followed by a comma and space.
                    EditorTags.Text = cleanTag + ", ";
                }
            }
        }

        /// <summary>
        /// Handles a button down event and navigates to the item with the specified index.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        /// <param name="index">The index of the item to navigate to.</param>
        public async Task OnNavigationButtonDown(string index)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                int caretOffset = EditorTags.CaretOffset;

                if (_viewModel.IsActive)
                {
                    _viewModel.GoToItemCommand.Execute(index);
                }

                EditorTags.CaretOffset = Math.Clamp(caretOffset, 0, EditorTags.Text.Length);
            });
        }
    }
}