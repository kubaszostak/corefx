﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal static partial class JsonWriterHelper
    {
        // Only allow ASCII characters between ' ' (0x20) and '~' (0x7E), inclusively,
        // but exclude characters that need to be escaped as hex: '"', '\'', '&', '+', '<', '>', '`'
        // and exclude characters that need to be escaped by adding a backslash: '\n', '\r', '\t', '\\', '/', '\b', '\f'
        //
        // non-zero = allowed, 0 = disallowed
        private static ReadOnlySpan<byte> AllowList => new byte[byte.MaxValue + 1] {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1,
            0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        };

        private static char[] s_hexFormat = { 'x', '4' };
        private static StandardFormat s_hexStandardFormat = new StandardFormat('x', 4);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NeedsEscaping(byte value) => AllowList[value] == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NeedsEscaping(char value) => value > byte.MaxValue || AllowList[value] == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NeedsEscaping(ReadOnlySpan<byte> value)
        {
            int idx;
            for (idx = 0; idx < value.Length; idx++)
            {
                if (NeedsEscaping(value[idx]))
                {
                    goto Return;
                }
            }

            idx = -1; // all characters allowed

        Return:
            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NeedsEscaping(ReadOnlySpan<char> value)
        {
            int idx;
            for (idx = 0; idx < value.Length; idx++)
            {
                if (NeedsEscaping(value[idx]))
                {
                    goto Return;
                }
            }

            idx = -1; // all characters allowed

        Return:
            return idx;
        }

        public static void EscapeString(ref ReadOnlySpan<byte> value, ref Span<byte> destination, int indexOfFirstByteToEscape, out int written)
        {
            Debug.Assert(indexOfFirstByteToEscape >= 0 && indexOfFirstByteToEscape < value.Length);

            value.Slice(0, indexOfFirstByteToEscape).CopyTo(destination);
            written = indexOfFirstByteToEscape;
            int consumed = indexOfFirstByteToEscape;

            while (consumed < value.Length)
            {
                byte val = value[consumed];
                if (NeedsEscaping(val))
                {
                    consumed += EscapeNextBytes(value.Slice(consumed), ref destination, ref written);
                }
                else
                {
                    destination[written] = val;
                    written++;
                    consumed++;
                }
            }
        }

        private static int EscapeNextBytes(ReadOnlySpan<byte> value, ref Span<byte> destination, ref int written)
        {
            SequenceValidity status = PeekFirstSequence(value, out int numBytesConsumed, out Rune rune);
            if (status != SequenceValidity.WellFormed)
                ThrowHelper.ThrowArgumentException_InvalidUTF8(value);

            destination[written++] = (byte)'\\';
            int scalar = rune.Value;
            switch (scalar)
            {
                case JsonConstants.LineFeed:
                    destination[written++] = (byte)'n';
                    break;
                case JsonConstants.CarriageReturn:
                    destination[written++] = (byte)'r';
                    break;
                case JsonConstants.Tab:
                    destination[written++] = (byte)'t';
                    break;
                case JsonConstants.BackSlash:
                    destination[written++] = (byte)'\\';
                    break;
                case JsonConstants.Slash:
                    destination[written++] = (byte)'/';
                    break;
                case JsonConstants.BackSpace:
                    destination[written++] = (byte)'b';
                    break;
                case JsonConstants.FormFeed:
                    destination[written++] = (byte)'f';
                    break;
                default:
                    destination[written++] = (byte)'u';
                    if (scalar < JsonConstants.UnicodePlane01StartValue)
                    {
                        Utf8Formatter.TryFormat(scalar, destination.Slice(written), out int bytesWritten, format: s_hexStandardFormat);
                        Debug.Assert(bytesWritten == 4);
                        written += bytesWritten;
                    }
                    else
                    {
                        // Divide by 0x400 to shift right by 10 in order to find the surrogate pairs from the scalar
                        // High surrogate = ((scalar -  0x10000) / 0x400) + D800
                        // Low surrogate = ((scalar -  0x10000) % 0x400) + DC00
                        int quotient = Math.DivRem(scalar - JsonConstants.UnicodePlane01StartValue, JsonConstants.ShiftRightBy10, out int remainder);
                        int firstChar = quotient + JsonConstants.HighSurrogateStartValue;
                        int nextChar = remainder + JsonConstants.LowSurrogateStartValue;
                        Utf8Formatter.TryFormat(firstChar, destination.Slice(written), out int bytesWritten, format: s_hexStandardFormat);
                        Debug.Assert(bytesWritten == 4);
                        written += bytesWritten;
                        destination[written++] = (byte)'\\';
                        destination[written++] = (byte)'u';
                        Utf8Formatter.TryFormat(nextChar, destination.Slice(written), out bytesWritten, format: s_hexStandardFormat);
                        Debug.Assert(bytesWritten == 4);
                        written += bytesWritten;
                    }
                    break;
            }
            return numBytesConsumed;
        }

        private static bool IsAsciiValue(byte value) => value < 0x80;

        /// <summary>
        /// Returns <see langword="true"/> iff <paramref name="value"/> is a UTF-8 continuation byte.
        /// A UTF-8 continuation byte is a byte whose value is in the range 0x80-0xBF, inclusive.
        /// </summary>
        private static bool IsUtf8ContinuationByte(byte value) => (value & 0xC0) == 0x80;

        /// <summary>
        /// Returns <see langword="true"/> iff <paramref name="value"/> is between
        /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsInRangeInclusive(byte value, byte lowerBound, byte upperBound)
            => ((byte)(value - lowerBound) <= (byte)(upperBound - lowerBound));

        /// <summary>
        /// Returns <see langword="true"/> iff <paramref name="value"/> is between
        /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsInRangeInclusive(uint value, uint lowerBound, uint upperBound)
            => (value - lowerBound) <= (upperBound - lowerBound);

        /// <summary>
        /// Returns <see langword="true"/> iff the low word of <paramref name="char"/> is a UTF-16 surrogate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLowWordSurrogate(uint @char)
            => (@char & 0xF800U) == 0xD800U;

        public static SequenceValidity PeekFirstSequence(ReadOnlySpan<byte> data, out int numBytesConsumed, out Rune rune)
        {
            // This method is implemented to match the behavior of System.Text.Encoding.UTF8 in terms of
            // how many bytes it consumes when reporting invalid sequences. The behavior is as follows:
            //
            // - Some bytes are *always* invalid (ranges [ C0..C1 ] and [ F5..FF ]), and when these
            //   are encountered it's an invalid sequence of length 1.
            //
            // - Multi-byte sequences which are overlong are reported as an invalid sequence of length 2,
            //   since per the Unicode Standard Table 3-7 it's always possible to tell these by the second byte.
            //   Exception: Sequences which begin with [ C0..C1 ] are covered by the above case, thus length 1.
            //
            // - Multi-byte sequences which are improperly terminated (no continuation byte when one is
            //   expected) are reported as invalid sequences up to and including the last seen continuation byte.

            rune = Rune.ReplacementChar;

            if (data.IsEmpty)
            {
                // No data to peek at
                numBytesConsumed = 0;
                return SequenceValidity.Empty;
            }

            byte firstByte = data[0];

            if (IsAsciiValue(firstByte))
            {
                // ASCII byte = well-formed one-byte sequence.
                rune = new Rune(firstByte);
                numBytesConsumed = 1;
                return SequenceValidity.WellFormed;
            }

            if (!IsInRangeInclusive(firstByte, (byte)0xC2U, (byte)0xF4U))
            {
                // Standalone continuation byte or "always invalid" byte = ill-formed one-byte sequence.
                goto InvalidOneByteSequence;
            }

            // At this point, we know we're working with a multi-byte sequence,
            // and we know that at least the first byte is potentially valid.

            if (data.Length < 2)
            {
                // One byte of an incomplete multi-byte sequence.
                goto OneByteOfIncompleteMultiByteSequence;
            }

            byte secondByte = data[1];

            if (!IsUtf8ContinuationByte(secondByte))
            {
                // One byte of an improperly terminated multi-byte sequence.
                goto InvalidOneByteSequence;
            }

            if (firstByte < (byte)0xE0U)
            {
                // Well-formed two-byte sequence.
                rune = new Rune((((uint)firstByte & 0x1FU) << 6) | ((uint)secondByte & 0x3FU));
                numBytesConsumed = 2;
                return SequenceValidity.WellFormed;
            }

            if (firstByte < (byte)0xF0U)
            {
                // Start of a three-byte sequence.
                // Need to check for overlong or surrogate sequences.

                uint scalar = (((uint)firstByte & 0x0FU) << 12) | (((uint)secondByte & 0x3FU) << 6);
                if (scalar < 0x800U || IsLowWordSurrogate(scalar))
                {
                    goto OverlongOutOfRangeOrSurrogateSequence;
                }

                // At this point, we have a valid two-byte start of a three-byte sequence.

                if (data.Length < 3)
                {
                    // Two bytes of an incomplete three-byte sequence.
                    goto TwoBytesOfIncompleteMultiByteSequence;
                }
                else
                {
                    byte thirdByte = data[2];
                    if (IsUtf8ContinuationByte(thirdByte))
                    {
                        // Well-formed three-byte sequence.
                        scalar |= (uint)thirdByte & 0x3FU;
                        rune = new Rune(scalar);
                        numBytesConsumed = 3;
                        return SequenceValidity.WellFormed;
                    }
                    else
                    {
                        // Two bytes of improperly terminated multi-byte sequence.
                        goto InvalidTwoByteSequence;
                    }
                }
            }

            {
                // Start of four-byte sequence.
                // Need to check for overlong or out-of-range sequences.

                uint scalar = (((uint)firstByte & 0x07U) << 18) | (((uint)secondByte & 0x3FU) << 12);
                if (!IsInRangeInclusive(scalar, 0x10000U, 0x10FFFFU))
                {
                    goto OverlongOutOfRangeOrSurrogateSequence;
                }

                // At this point, we have a valid two-byte start of a four-byte sequence.

                if (data.Length < 3)
                {
                    // Two bytes of an incomplete four-byte sequence.
                    goto TwoBytesOfIncompleteMultiByteSequence;
                }
                else
                {
                    byte thirdByte = data[2];
                    if (IsUtf8ContinuationByte(thirdByte))
                    {
                        // Valid three-byte start of a four-byte sequence.

                        if (data.Length < 4)
                        {
                            // Three bytes of an incomplete four-byte sequence.
                            goto ThreeBytesOfIncompleteMultiByteSequence;
                        }
                        else
                        {
                            byte fourthByte = data[3];
                            if (IsUtf8ContinuationByte(fourthByte))
                            {
                                // Well-formed four-byte sequence.
                                scalar |= (((uint)thirdByte & 0x3FU) << 6) | ((uint)fourthByte & 0x3FU);
                                rune = new Rune(scalar);
                                numBytesConsumed = 4;
                                return SequenceValidity.WellFormed;
                            }
                            else
                            {
                                // Three bytes of an improperly terminated multi-byte sequence.
                                goto InvalidThreeByteSequence;
                            }
                        }
                    }
                    else
                    {
                        // Two bytes of improperly terminated multi-byte sequence.
                        goto InvalidTwoByteSequence;
                    }
                }
            }

        // Everything below here is error handling.

        InvalidOneByteSequence:
            numBytesConsumed = 1;
            return SequenceValidity.Invalid;

        InvalidTwoByteSequence:
        OverlongOutOfRangeOrSurrogateSequence:
            numBytesConsumed = 2;
            return SequenceValidity.Invalid;

        InvalidThreeByteSequence:
            numBytesConsumed = 3;
            return SequenceValidity.Invalid;

        OneByteOfIncompleteMultiByteSequence:
            numBytesConsumed = 1;
            return SequenceValidity.Incomplete;

        TwoBytesOfIncompleteMultiByteSequence:
            numBytesConsumed = 2;
            return SequenceValidity.Incomplete;

        ThreeBytesOfIncompleteMultiByteSequence:
            numBytesConsumed = 3;
            return SequenceValidity.Incomplete;
        }

        public static void EscapeString(ref ReadOnlySpan<char> value, ref Span<char> destination, int indexOfFirstByteToEscape, out int written)
        {
            Debug.Assert(indexOfFirstByteToEscape >= 0 && indexOfFirstByteToEscape < value.Length);

            value.Slice(0, indexOfFirstByteToEscape).CopyTo(destination);
            written = indexOfFirstByteToEscape;
            int consumed = indexOfFirstByteToEscape;

            while (consumed < value.Length)
            {
                char val = value[consumed];
                if (NeedsEscaping(val))
                {
                    EscapeNextChars(ref value, val, ref destination, ref consumed, ref written);
                }
                else
                {
                    destination[written++] = val;
                }
                consumed++;
            }
        }

        private static void EscapeNextChars(ref ReadOnlySpan<char> value, int firstChar, ref Span<char> destination, ref int consumed, ref int written)
        {
            int nextChar = -1;
            if (IsInRangeInclusive(firstChar, JsonConstants.HighSurrogateStartValue, JsonConstants.LowSurrogateEndValue))
            {
                consumed++;
                if (value.Length <= consumed || firstChar >= JsonConstants.LowSurrogateStartValue)
                {
                    ThrowHelper.ThrowArgumentException_InvalidUTF16(firstChar);
                }

                nextChar = value[consumed];
                if (!IsInRangeInclusive(nextChar, JsonConstants.LowSurrogateStartValue, JsonConstants.LowSurrogateEndValue))
                {
                    ThrowHelper.ThrowArgumentException_InvalidUTF16(nextChar);
                }
            }

            destination[written++] = '\\';
            switch (firstChar)
            {
                case JsonConstants.LineFeed:
                    destination[written++] = 'n';
                    break;
                case JsonConstants.CarriageReturn:
                    destination[written++] = 'r';
                    break;
                case JsonConstants.Tab:
                    destination[written++] = 't';
                    break;
                case JsonConstants.BackSlash:
                    destination[written++] = '\\';
                    break;
                case JsonConstants.Slash:
                    destination[written++] = '/';
                    break;
                case JsonConstants.BackSpace:
                    destination[written++] = 'b';
                    break;
                case JsonConstants.FormFeed:
                    destination[written++] = 'f';
                    break;
                default:
                    destination[written++] = 'u';
                    firstChar.TryFormat(destination.Slice(written), out int charsWritten, s_hexFormat);
                    Debug.Assert(charsWritten == 4);
                    written += charsWritten;
                    if (nextChar != -1)
                    {
                        destination[written++] = '\\';
                        destination[written++] = 'u';
                        nextChar.TryFormat(destination.Slice(written), out charsWritten, s_hexFormat);
                        Debug.Assert(charsWritten == 4);
                        written += charsWritten;
                    }
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsInRangeInclusive(int ch, int start, int end)
        {
            return (uint)(ch - start) <= (uint)(end - start);
        }
    }
}
