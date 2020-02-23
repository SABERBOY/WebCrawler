using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ArticleConsole.Translators
{
    public static class TranslatorUtility
    {
        private static readonly int LEN_LINEBREAK = 1;

        public static List<List<string>> Wrap(string[] content, int maxUTF8Bytes, out int[] blockPositions)
        {
            blockPositions = new int[content.Length];

            // split content to smaller blocks
            var splitBlocks = new List<string>();
            int position = 0;
            int previousBreakIndex;
            string block;
            string tempBlock;
            MatchCollection breaks;
            for (var i = 0; i < content.Length; i++)
            {
                blockPositions[i] = position;

                // remove original line breaks as they might be inside a single paragraph or sentence.
                // this might not be necessary for well-formatted article.
                block = Regex.Replace(content[i] ?? string.Empty, "[\r\n]", " ");

                //breaks = Regex.Matches(block, @"(?!\1[ \r\n\t]*)<(div|p|br|h\d|figure|section|aside|header|footer|blockquote|ul)( [^<>]*)?>", RegexOptions.IgnoreCase);
                breaks = Regex.Matches(block, @"<(div|p|br|h\d|figure|section|aside|header|footer|blockquote|ul)([ \r\n][^<>]*)?>", RegexOptions.IgnoreCase);
                //breaks = Regex.Matches(block, @"<(div|p|br|h\d|figure|section|aside|header|footer|blockquote|ul)( [^<>]*)?/?>", RegexOptions.IgnoreCase);
                //breaks = Regex.Matches(block, @"</?(div|p|br|h\d|figure|section|aside|header|footer|blockquote|ul)( [^<>]*)?/?>", RegexOptions.IgnoreCase);
                if (breaks.Count == 0)
                {
                    splitBlocks.Add(block);

                    position++;
                }
                else
                {
                    previousBreakIndex = 0;
                    for (var j = 0; j < breaks.Count; j++)
                    {
                        tempBlock = block.Substring(previousBreakIndex, breaks[j].Index - previousBreakIndex);
                        if (!string.IsNullOrEmpty(tempBlock))
                        {
                            splitBlocks.Add(tempBlock);
                            position++;
                        }

                        previousBreakIndex = breaks[j].Index;
                    }

                    tempBlock = block.Substring(previousBreakIndex);
                    if (!string.IsNullOrEmpty(tempBlock))
                    {
                        splitBlocks.Add(tempBlock);
                        position++;
                    }
                }
            }

            // batch the blocks for API call
            var batches = new List<List<string>>();
            List<string> batch = null;
            int subtotal = 0;
            int bytes;
            foreach (var sb in splitBlocks)
            {
                bytes = GetUTF8Bytes(sb);

                if (bytes > maxUTF8Bytes) // not expected, might need to figure out how to split the content
                {
                    // log error
                    throw new NotImplementedException();
                }
                else if (bytes == maxUTF8Bytes) // add new batch and start new batch
                {
                    batches.Add(new List<string> { sb });

                    subtotal = 0;
                }
                else
                {
                    if (subtotal == 0 || subtotal + bytes + LEN_LINEBREAK > maxUTF8Bytes)
                    {
                        batch = new List<string>();
                        batches.Add(batch);

                        subtotal = 0;
                    }

                    batch.Add(sb);
                    subtotal += bytes + LEN_LINEBREAK;
                }
            }

            return batches;
        }

        public static string[] Unwrap(List<string> apiResults, int[] blockPositions)
        {
            var results = new List<string>();

            StringBuilder builder = new StringBuilder();
            int from;
            int to;
            for (var i = 0; i < blockPositions.Length; i++)
            {
                builder.Clear();
                from = blockPositions[i];
                to = (i < blockPositions.Length - 1 ? blockPositions[i + 1] : apiResults.Count);
                for (var j = from; j < to; j++)
                {
                    builder.Append(apiResults[j]);
                }
                results.Add(builder.ToString());
            }

            return results.ToArray();
        }

        public static int GetUTF8Bytes(string content)
        {
            int bytes = 0;

            foreach (var c in content)
            {
                bytes += c > 127 ? 3 : 1;
            }

            return bytes;
        }
    }
}
