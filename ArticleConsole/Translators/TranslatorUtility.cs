using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace ArticleConsole.Translators
{
    public static class TranslatorUtility
    {
        private static readonly int LEN_LINEBREAK = 1;

        public static List<List<string>> Wrap(string[] inputs, int maxUTF8Bytes, out int[] blockPositions)
        {
            blockPositions = new int[inputs.Length];

            // split inputs to smaller blocks
            var inputBlocks = new List<string>();
            int position = 0;
            int previousBreakIndex;
            string input;
            string tempBlock;
            MatchCollection breaks;
            for (var i = 0; i < inputs.Length; i++)
            {
                input = inputs[i];

                if (string.IsNullOrEmpty(input))
                {
                    // ignore empty input
                    blockPositions[i] = -1;
                    continue;
                }

                blockPositions[i] = position;

                // remove original line breaks as they might be inside a single paragraph or sentence.
                // this might not be necessary for well-formatted article.
                input = Regex.Replace(input, "[\r\n]", " ");

                //breaks = Regex.Matches(block, @"(?!\1[ \r\n\t]*)<(div|p|br|h\d|figure|section|aside|header|footer|blockquote|ul)( [^<>]*)?>", RegexOptions.IgnoreCase);
                breaks = Regex.Matches(input, @"<(div|p|br|h\d|figure|section|aside|header|footer|blockquote|ul)([ \r\n][^<>]*)?>", RegexOptions.IgnoreCase);
                //breaks = Regex.Matches(block, @"<(div|p|br|h\d|figure|section|aside|header|footer|blockquote|ul)( [^<>]*)?/?>", RegexOptions.IgnoreCase);
                //breaks = Regex.Matches(block, @"</?(div|p|br|h\d|figure|section|aside|header|footer|blockquote|ul)( [^<>]*)?/?>", RegexOptions.IgnoreCase);
                if (breaks.Count == 0)
                {
                    inputBlocks.Add(input);

                    position++;
                }
                else
                {
                    previousBreakIndex = 0;
                    for (var j = 0; j < breaks.Count; j++)
                    {
                        tempBlock = input.Substring(previousBreakIndex, breaks[j].Index - previousBreakIndex);
                        if (!string.IsNullOrEmpty(tempBlock))
                        {
                            inputBlocks.Add(tempBlock);
                            position++;
                        }

                        previousBreakIndex = breaks[j].Index;
                    }

                    tempBlock = input.Substring(previousBreakIndex);
                    if (!string.IsNullOrEmpty(tempBlock))
                    {
                        inputBlocks.Add(tempBlock);
                        position++;
                    }
                }
            }

            // batch the blocks for API call
            var batches = new List<List<string>>();
            List<string> batch = null;
            int subtotal = 0;
            int bytes;
            foreach (var sb in inputBlocks)
            {
                bytes = GetUTF8Bytes(sb);

                if (bytes > maxUTF8Bytes) // not expected, might need to figure out how to split the input
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

        public static string[] Unwrap(string[] outputBlocks, int[] blockPositions)
        {
            var outputs = new List<string>();

            int start = 0;
            int end;
            for (var i = 0; i < blockPositions.Length; i++)
            {
                start = blockPositions[i];

                if (start < 0)
                {
                    // fill ignored input with empty string
                    outputs.Add(string.Empty);
                }
                else
                {
                    // seek for the start position of next output block or end position
                    end = blockPositions
                            .Skip(i + 1)
                            .Select(o => (int?)o)
                            .FirstOrDefault(o => o > 0)
                        ?? outputBlocks.Length;

                    outputs.Add(string.Join(string.Empty, outputBlocks, start, end - start));
                }
            }

            return outputs.ToArray();
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
