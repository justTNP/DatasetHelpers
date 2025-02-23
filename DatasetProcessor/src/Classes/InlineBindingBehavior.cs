using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using DatasetProcessor.ViewModels;
using System.Reactive.Linq;

namespace DatasetProcessor.src.Classes
{
    public class InlineBindingBehavior
    {
        public static readonly AttachedProperty<IEnumerable<TextPart>> InlineSourceProperty =
            AvaloniaProperty.RegisterAttached<InlineBindingBehavior, TextBlock, IEnumerable<TextPart>>("InlineSource");

        public static IEnumerable<TextPart> GetInlineSource(TextBlock element)
        {
            return element.GetValue(InlineSourceProperty);
        }

        public static void SetInlineSource(TextBlock element, IEnumerable<TextPart> value)
        {
            element.SetValue(InlineSourceProperty, value);
        }

        static InlineBindingBehavior()
        {
            InlineSourceProperty.Changed.Subscribe(args =>
            {
                if (args.Sender is TextBlock textBlock)
                {
                    textBlock.Inlines.Clear();
                    
                    // Get the formatted parts from the binding value.
                    var parts = args.NewValue.GetValueOrDefault();
                    if (parts != null)
                    {
                        foreach (var part in parts)
                        {
                            textBlock.Inlines.Add(new Run
                            {
                                Text = part.Text,
                                FontWeight = part.IsBold ? FontWeight.Bold : FontWeight.Normal
                            });
                        }
                    }
                    
                    // Append the formatted count if available.
                    if (textBlock.DataContext is TagSuggestion suggestion)
                    {
                        textBlock.Inlines.Add(new Run { Text = " (" });
                        textBlock.Inlines.Add(new Run { Text = suggestion.FormattedCount });
                        textBlock.Inlines.Add(new Run { Text = ")" });
                    }
                }
            });
        }
    }
}
