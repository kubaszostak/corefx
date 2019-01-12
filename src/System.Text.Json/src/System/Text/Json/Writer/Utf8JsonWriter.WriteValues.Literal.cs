﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    public ref partial struct Utf8JsonWriter
    {
        /// <summary>
        /// Writes the JSON literal "null" as an element of a JSON array.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteNullValue()
        {
            WriteLiteralByOptions(JsonConstants.NullValue);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.Null;
        }

        /// <summary>
        /// Writes the <see cref="bool"/> value (as a JSON literal "true" or "false") as an element of a JSON array.
        /// </summary>
        /// <param name="value">The value to be written as a JSON literal "true" or "false" as an element of a JSON array.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteBooleanValue(bool value)
        {
            if (value)
            {
                WriteLiteralByOptions(JsonConstants.TrueValue);
                _tokenType = JsonTokenType.True;
            }
            else
            {
                WriteLiteralByOptions(JsonConstants.FalseValue);
                _tokenType = JsonTokenType.False;
            }

            SetFlagToAddListSeparatorBeforeNextItem();
        }

        private void WriteLiteralByOptions(ReadOnlySpan<byte> value)
        {
            ValidateWritingValue();
            if (_writerOptions.Indented)
            {
                WriteLiteralIndented(value);
            }
            else
            {
                WriteLiteralMinimized(value);
            }
        }

        private void WriteLiteralMinimized(ReadOnlySpan<byte> value)
        {
            int idx = 0;
            WriteListSeparator(ref idx);

            CopyLoop(value, ref idx);

            Advance(idx);
        }

        private void WriteLiteralIndented(ReadOnlySpan<byte> value)
        {
            int idx = WriteCommaAndFormattingPreamble();

            CopyLoop(value, ref idx);

            Advance(idx);
        }
    }
}
