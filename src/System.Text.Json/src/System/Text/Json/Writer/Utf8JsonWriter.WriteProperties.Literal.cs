﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;

namespace System.Text.Json
{
    public ref partial struct Utf8JsonWriter
    {
        /// <summary>
        /// Writes the property name and the JSON literal "null" as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="suppressEscaping">If this is set, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteNull(string propertyName, bool suppressEscaping = false)
            => WriteNull(propertyName.AsSpan(), suppressEscaping);

        /// <summary>
        /// Writes the property name and the JSON literal "null" as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="suppressEscaping">If this is set, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteNull(ReadOnlySpan<char> propertyName, bool suppressEscaping = false)
        {
            JsonWriterHelper.ValidateProperty(ref propertyName);

            ReadOnlySpan<byte> span = JsonConstants.NullValue;

            if (!suppressEscaping)
            {
                WriteLiteralSuppressFalse(ref propertyName, ref span);
            }
            else
            {
                WriteLiteralByOptions(ref propertyName, ref span);
            }

            _currentDepth |= 1 << 31;
            _tokenType = JsonTokenType.Null;
        }

        /// <summary>
        /// Writes the property name and the JSON literal "null" as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <param name="suppressEscaping">If this is set, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteNull(ReadOnlySpan<byte> propertyName, bool suppressEscaping = false)
        {
            JsonWriterHelper.ValidateProperty(ref propertyName);

            ReadOnlySpan<byte> span = JsonConstants.NullValue;

            if (!suppressEscaping)
            {
                WriteLiteralSuppressFalse(ref propertyName, ref span);
            }
            else
            {
                WriteLiteralByOptions(ref propertyName, ref span);
            }

            _currentDepth |= 1 << 31;
            _tokenType = JsonTokenType.Null;
        }

        /// <summary>
        /// Writes the property name and <see cref="bool"/> value (as a JSON literal "true" or "false") as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a JSON literal "true" or "false" as part of the name/value pair.</param>
        /// <param name="suppressEscaping">If this is set, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteBoolean(string propertyName, bool value, bool suppressEscaping = false)
            => WriteBoolean(propertyName.AsSpan(), value, suppressEscaping);

        /// <summary>
        /// Writes the property name and <see cref="bool"/> value (as a JSON literal "true" or "false") as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a JSON literal "true" or "false" as part of the name/value pair.</param>
        /// <param name="suppressEscaping">If this is set, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteBoolean(ReadOnlySpan<char> propertyName, bool value, bool suppressEscaping = false)
        {
            JsonWriterHelper.ValidateProperty(ref propertyName);

            ReadOnlySpan<byte> span = value ? JsonConstants.TrueValue : JsonConstants.FalseValue;

            if (!suppressEscaping)
            {
                WriteLiteralSuppressFalse(ref propertyName, ref span);
            }
            else
            {
                WriteLiteralByOptions(ref propertyName, ref span);
            }

            _currentDepth |= 1 << 31;
            _tokenType = value ? JsonTokenType.True : JsonTokenType.False;
        }

        /// <summary>
        /// Writes the property name and <see cref="bool"/> value (as a JSON literal "true" or "false") as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <param name="value">The value to be written as a JSON literal "true" or "false" as part of the name/value pair.</param>
        /// <param name="suppressEscaping">If this is set, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteBoolean(ReadOnlySpan<byte> propertyName, bool value, bool suppressEscaping = false)
        {
            JsonWriterHelper.ValidateProperty(ref propertyName);

            ReadOnlySpan<byte> span = value ? JsonConstants.TrueValue : JsonConstants.FalseValue;

            if (!suppressEscaping)
            {
                WriteLiteralSuppressFalse(ref propertyName, ref span);
            }
            else
            {
                WriteLiteralByOptions(ref propertyName, ref span);
            }

            _currentDepth |= 1 << 31;
            _tokenType = value ? JsonTokenType.True : JsonTokenType.False;
        }

        private void WriteLiteralSuppressFalse(ref ReadOnlySpan<char> propertyName, ref ReadOnlySpan<byte> value)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < int.MaxValue / 2);

            if (propertyIdx != -1)
            {
                WriteLiteralEscapeProperty(ref propertyName, ref value, propertyIdx);
            }
            else
            {
                WriteLiteralByOptions(ref propertyName, ref value);
            }
        }

        private void WriteLiteralSuppressFalse(ref ReadOnlySpan<byte> propertyName, ref ReadOnlySpan<byte> value)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < int.MaxValue / 2);

            if (propertyIdx != -1)
            {
                WriteLiteralEscapeProperty(ref propertyName, ref value, propertyIdx);
            }
            else
            {
                WriteLiteralByOptions(ref propertyName, ref value);
            }
        }

        private void WriteLiteralEscapeProperty(ref ReadOnlySpan<char> propertyName, ref ReadOnlySpan<byte> value, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= propertyName.Length);

            char[] propertyArray = null;

            int length = firstEscapeIndexProp + JsonConstants.MaxExpansionFactorWhileEscaping * (propertyName.Length - firstEscapeIndexProp);
            Span<char> span;
            if (length > StackallocThreshold)
            {
                propertyArray = ArrayPool<char>.Shared.Rent(length);
                span = propertyArray;
            }
            else
            {
                // Cannot create a span directly since the span gets exposed outside this method.
                unsafe
                {
                    char* ptr = stackalloc char[length];
                    span = new Span<char>(ptr, length);
                }
            }
            JsonWriterHelper.EscapeString(ref propertyName, ref span, firstEscapeIndexProp, out int written);
            propertyName = span.Slice(0, written);

            WriteLiteralByOptions(ref propertyName, ref value);

            if (propertyArray != null)
            {
                ArrayPool<char>.Shared.Return(propertyArray);
            }
        }

        private void WriteLiteralEscapeProperty(ref ReadOnlySpan<byte> propertyName, ref ReadOnlySpan<byte> value, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= propertyName.Length);

            byte[] propertyArray = null;

            int length = firstEscapeIndexProp + JsonConstants.MaxExpansionFactorWhileEscaping * (propertyName.Length - firstEscapeIndexProp);
            Span<byte> span;
            if (length > StackallocThreshold)
            {
                propertyArray = ArrayPool<byte>.Shared.Rent(length);
                span = propertyArray;
            }
            else
            {
                // Cannot create a span directly since the span gets exposed outside this method.
                unsafe
                {
                    byte* ptr = stackalloc byte[length];
                    span = new Span<byte>(ptr, length);
                }
            }
            JsonWriterHelper.EscapeString(ref propertyName, ref span, firstEscapeIndexProp, out int written);
            propertyName = span.Slice(0, written);

            WriteLiteralByOptions(ref propertyName, ref value);

            if (propertyArray != null)
            {
                ArrayPool<byte>.Shared.Return(propertyArray);
            }
        }

        private void WriteLiteralByOptions(ref ReadOnlySpan<char> propertyName, ref ReadOnlySpan<byte> value)
        {
            int idx;
            if (_writerOptions.Indented)
            {
                if (!_writerOptions.SkipValidation)
                {
                    ValidateWritingProperty();
                }
                idx = WritePropertyNameIndented(ref propertyName);
            }
            else
            {
                if (!_writerOptions.SkipValidation)
                {
                    ValidateWritingProperty();
                }
                idx = WritePropertyNameMinimized(ref propertyName);
            }

            if (value.Length > _buffer.Length - idx)
            {
                AdvanceAndGrow(idx, value.Length);
                idx = 0;
            }

            value.CopyTo(_buffer.Slice(idx));
            idx += value.Length;

            Advance(idx);
        }

        private void WriteLiteralByOptions(ref ReadOnlySpan<byte> propertyName, ref ReadOnlySpan<byte> value)
        {
            int idx;
            if (_writerOptions.Indented)
            {
                if (!_writerOptions.SkipValidation)
                {
                    ValidateWritingProperty();
                }
                idx = WritePropertyNameIndented(ref propertyName);
            }
            else
            {
                if (!_writerOptions.SkipValidation)
                {
                    ValidateWritingProperty();
                }
                idx = WritePropertyNameMinimized(ref propertyName);
            }

            if (value.Length > _buffer.Length - idx)
            {
                AdvanceAndGrow(idx, value.Length);
                idx = 0;
            }

            value.CopyTo(_buffer.Slice(idx));
            idx += value.Length;

            Advance(idx);
        }
    }
}
