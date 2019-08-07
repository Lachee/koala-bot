using KoalaBot.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace KoalaBot.Starwatch.Entities
{
    /// <summary>
    /// TaggedText is a formatted version of text received from Starbound. This text will strip the colour tags away from the text and store them. 
    /// </summary>
    public class TaggedText
    {
        /// <summary>
        /// This is the Regex used to seperate the tags from the Starbound text. 
        /// </summary>
        public static readonly Regex TagRegex = new Regex("\\^(.*?)\\;", RegexOptions.Compiled);

        /// <summary>
        /// The text from starbound with the colour tags still.
        /// </summary>
        public string RawContent { get; }

        /// <summary>
        /// The text from starbound with its colour tags removed.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Does this text contain any tags? If this is false, the TaggedContent and Content will be exactly the same.
        /// </summary>
        public bool IsTagged => Tags.Length > 0;

        /// <summary>
        /// The tags from the text.
        /// </summary>
        public Tag[] Tags { get; }
        public struct Tag
        {
            public string color;
            public string text;
        }

        /// <summary>
        /// Creates a new instance of the TaggedText, parsing the stripping the tags.
        /// </summary>
        /// <param name="text">The raw text received from Starbound.</param>
        public TaggedText(string text)
        {
            this.RawContent = text.Trim();
            this.Tags = StripColourTags(this.RawContent, out var clean);
            this.Content = clean;
        }

        /// <summary>
        /// Turns the object into a string without the tags.
        /// </summary>
        /// <returns>Content without tags</returns>
        public override string ToString()
        {
            return this.Content;
        }
        
        /// <summary>
        /// Implicidly casts a TaggedText into a string. This way it will not be required to use ToString() or do a cast.
        /// </summary>
        /// <param name="text"></param>
        public static implicit operator string(TaggedText text)
        {
            return text.ToString();
        }

        public static implicit operator TaggedText(string text)
        {
            return new TaggedText(text);
        }

        /// <summary>
        /// Strips colour tags from the given text
        /// </summary>
        /// <param name="text">Text to strip tags from</param>
        /// <returns>Tagless text</returns>
        private static Tag[] StripColourTags(string text, out string clean)
        {
            //Prepare teh clean
            StringBuilder cleanBuilder = new StringBuilder();

            //Match all the text
            MatchCollection matches = TagRegex.Matches(text);
            Tag[] tags = new Tag[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {
                if (i == matches.Count - 1)
                {
                    tags[i] = new Tag()
                    {
                        color = matches[i].Value.Cut(1, matches[i].Value.Length - 1),
                        text = text.Substring(matches[i].Index + matches[i].Length)
                    };
                }
                else
                {
                    tags[i] = new Tag()
                    {
                        color = matches[i].Value.Cut(1, matches[i].Value.Length - 1),
                        text = text.Cut(matches[i].Index + matches[i].Length, matches[i + 1].Index)
                    };
                }

                //Append the clean text
                cleanBuilder.Append(tags[i].text);
            }

            //Remove the clean
            clean = cleanBuilder.ToString().Trim();
            return tags;
        }
    }
}
