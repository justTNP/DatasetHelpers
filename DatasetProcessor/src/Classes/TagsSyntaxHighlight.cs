using Avalonia.Media;

using AvaloniaEdit.Highlighting;

using SmartData.Lib.Helpers;

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DatasetProcessor.src.Classes
{
    /// <summary>
    /// Defines a custom syntax highlighting for highlighting specific tags in text using regular expressions.
    /// </summary>
    public class TagsSyntaxHighlight : IHighlightingDefinition
    {
        /// <summary>
        /// Gets the name of the custom syntax highlighting definition.
        /// </summary>
        public string Name => "TagsSyntaxHighlight";

        /// <summary>
        /// Gets the main rule set for syntax highlighting.
        /// </summary>
        public HighlightingRuleSet MainRuleSet { get; }

        /// <summary>
        /// Initializes a new instance of the TagsSyntaxHighlight class with specific tags and foreground color.
        /// </summary>
        /// <param name="foregroundColor">The color used to highlight the specified tags.</param>
        /// <param name="tags">An array of tags to be highlighted.</param>
        public TagsSyntaxHighlight(List<Color> highlightColors, string[] tags)
        {
            MainRuleSet = new HighlightingRuleSet();

            string[] linesOfTags = string.Join(",", tags).Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < linesOfTags.Length; i++)
            {
                string[] tagsInLine = linesOfTags[i].Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);

                Color lineColor = highlightColors[i % highlightColors.Count];

                foreach (string tag in tagsInLine)
                {
                    string trimmedTag = tag.Trim();

                    if (!string.IsNullOrEmpty(trimmedTag))
                    {
                        HighlightingRule customWordRule = new HighlightingRule()
                        {
                            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(lineColor) },
                            Regex = new Regex(@$"\b({Regex.Escape(trimmedTag)})\b", RegexOptions.IgnoreCase, Utilities.RegexTimeout)
                        };

                        MainRuleSet.Rules.Add(customWordRule);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a collection of named highlighting colors, which is not used in this custom syntax highlighting.
        /// </summary>
        public IEnumerable<HighlightingColor> NamedHighlightingColors => null;

        /// <summary>
        /// Gets a collection of properties, which is not used in this custom syntax highlighting.
        /// </summary>
        public IDictionary<string, string> Properties => null;

        /// <summary>
        /// Gets a named highlighting color by name, which is not used in this custom syntax highlighting.
        /// </summary>
        /// <param name="name">The name of the highlighting color.</param>
        /// <returns>The named highlighting color, or null if not found.</returns>
        public HighlightingColor GetNamedColor(string name) => null;

        /// <summary>
        /// Gets a named rule set by name, which is not used in this custom syntax highlighting.
        /// </summary>
        /// <param name="name">The name of the rule set.</param>
        /// <returns>The named rule set, or null if not found.</returns>
        public HighlightingRuleSet GetNamedRuleSet(string name) => null;
    }
}
