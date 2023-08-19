namespace DBracket.Net.TCP.DataSync
{
    /// TODO: This should be move to my common projects and seperated in single files


    public class SpanSplitter
    {
        #region "----------------------------- Private Fields ------------------------------"
        private char[] _seperator;
        private int _currentPosition = 0;

        private int _spanLength;
        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public SpanSplitter(ReadOnlySpan<char> span, char[] seperator)
        {
            if (span == null)
                throw new ArgumentNullException();

            if (seperator == null)
                throw new ArgumentNullException();

            if (span.Length < seperator.Length)
                throw new ArgumentOutOfRangeException();


            _seperator = seperator;
            _spanLength = span.Length;
        }
        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"
        public ReadOnlySpan<char> GetNextSplit(ReadOnlySpan<char> span)
        {
            // End reached, always return null
            if (_currentPosition < 0)
                return null;

            if (span == null)
                throw new ArgumentException();

            if (span.Length != _spanLength)
                throw new ArgumentException();

            var startPosition = _currentPosition;
            ReadOnlySpan<char> result = null;

            if (_seperator.Length == 1)
            {
                for (int i = _currentPosition; i < span.Length; i++)
                {
                    // If last position, save split and finish
                    if (i == span.Length - 1)
                    {
                        var length = _currentPosition - 1 - startPosition;
                        result = span.Slice(startPosition, length);
                        _currentPosition = -1;
                        break;
                    }

                    if (span[i] == _seperator[0])
                    {
                        var length = _currentPosition - 1 - startPosition;
                        result = span.Slice(startPosition, length);
                        _currentPosition = i + 1;
                    }
                }
            }
            else
            {
                for (int i = _currentPosition; i < span.Length; i++)
                {
                    // Check if end reached
                    if (i >= span.Length - 1 - _seperator.Length + 1)
                    {
                        if (i == span.Length - 1)
                        {
                            // End reached
                            var length = i + 1 - startPosition;
                            result = span.Slice(startPosition, length);
                            _currentPosition = -1;
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var isEqual = true;
                    for (int j = 0; j < _seperator.Length; j++)
                    {
                        if (span[i + j] != _seperator[j])
                        {
                            isEqual = false;
                            break;
                        }
                    }

                    if (isEqual)
                    {
                        var length = i - startPosition;
                        result = span.Slice(startPosition, length);
                        _currentPosition = i + _seperator.Length;
                        break;
                    }
                }
            }

            return result;
        }

        public static List<SplitPosition>? GetSplitIndices(ReadOnlySpan<char> span, char[] seperator)
        {
            if (span == null)
                throw new ArgumentNullException();

            if (seperator == null)
                throw new ArgumentNullException();

            if (span.Length < seperator.Length)
                throw new ArgumentOutOfRangeException();

            var result = new List<SplitPosition>();
            var startPosition = 0;

            if (seperator.Length == 1)
            {
                for (int i = 0; i < span.Length; i++)
                {
                    // If last position, save split and finish
                    if (i == span.Length - 1)
                    {
                        result.Add(new SplitPosition(startPosition, i));
                        break;
                    }

                    // Check if seperator found
                    if (span[i] == seperator[0])
                    {
                        result.Add(new SplitPosition(startPosition, i));
                        i++;
                        startPosition = i;
                    }
                }
            }
            else
            {
                for (int i = 0; i < span.Length; i++)
                {
                    // Check if end reached
                    if (i >= span.Length - 1 - seperator.Length + 1)
                    {
                        if (i == span.Length - 1)
                        {
                            // End reached
                            result.Add(new SplitPosition(startPosition, i));
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    // Check if seperator found
                    var isEqual = true;
                    for (int j = 0; j < seperator.Length; j++)
                    {
                        if (span[i + j] != seperator[j])
                        {
                            isEqual = false;
                            break;
                        }
                    }

                    // Add SplitPositions to result
                    if (isEqual)
                    {
                        // i == first seperator
                        result.Add(new SplitPosition(startPosition, i - 1));
                        i += seperator.Length - 1;
                        startPosition = i + 1;
                    }
                }
            }

            return result;
        }
        #endregion

        #region "----------------------------- Private Methods -----------------------------"

        #endregion

        #region "------------------------------ Event Handling -----------------------------"

        #endregion
        #endregion


        #region "--------------------------- Public Propterties ----------------------------"
        #region "------------------------------- Properties --------------------------------"

        #endregion

        #region "--------------------------------- Events ----------------------------------"

        #endregion
        #endregion
    }

    public class SplitPosition
    {
        public SplitPosition(int startIndex, int endIndex)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }


        public int StartIndex { get; }

        public int EndIndex { get; }

        public int Length { get => StartIndex == EndIndex ? 1 : EndIndex - StartIndex + 1; }
        //public int Length { get => StartIndex == 0 ? EndIndex - StartIndex : EndIndex - StartIndex + 1; }
    }
}