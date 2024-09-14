// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LibObjectFile.Utils;

/// <summary>
/// Defines a stream as a slice of another existing stream.
/// </summary>
public class SliceStream : Stream
{
    private Stream _baseStream;
    private readonly long _length;
    private readonly long _basePosition;
    private long _localPosition;

    public SliceStream(Stream baseStream, long position, long length)
    {
        if (baseStream == null) throw new ArgumentNullException(nameof(baseStream));
        if (!baseStream.CanSeek) throw new ArgumentException("Invalid base stream that can't be seek.");
        if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (position + length > baseStream.Length) throw new ArgumentOutOfRangeException(nameof(position), $"The position {position} + length {length} > baseStream.Length {baseStream.Length}");

        _baseStream = baseStream;
        _length = length;
        _basePosition = position;
    }
    public override int Read(byte[] buffer, int offset, int count) => Read(new Span<byte>(buffer, offset, count));

    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();

        long remaining = _length - _localPosition;
        if (remaining <= 0) return 0;
        if (remaining < buffer.Length) buffer = buffer.Slice(0, (int)remaining);

        _baseStream.Position = _basePosition + _localPosition;
        int read = _baseStream.Read(buffer);
        _localPosition += read;

        return read;
    }

    public override int ReadByte()
    {
        ThrowIfDisposed();

        if (_localPosition >= _length) return -1;

        _baseStream.Position = _basePosition + _localPosition;
        int read = _baseStream.ReadByte();
        _localPosition++;

        return read;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        long remaining = _length - _localPosition;
        if (remaining <= 0) return 0;
        if (remaining < buffer.Length) buffer = buffer.Slice(0, (int)remaining);

        _baseStream.Position = _basePosition + _localPosition;
        var read = await _baseStream.ReadAsync(buffer, cancellationToken);

        _localPosition += read;
        return read;
    }
        
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_baseStream == Stream.Null, this);
    }

    public override long Length
    {
        get { ThrowIfDisposed(); return _length; }
    }
    public override bool CanRead
    {
        get { ThrowIfDisposed(); return _baseStream.CanRead; }
    }
    public override bool CanWrite
    {
        get { ThrowIfDisposed(); return _baseStream.CanWrite; }
    }
    public override bool CanSeek
    {
        get { ThrowIfDisposed(); return _baseStream.CanSeek; }
    }
    public override long Position
    {
        get
        {
            ThrowIfDisposed();
            return _localPosition;
        }
        set => Seek(value, SeekOrigin.Begin);
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
        long newPosition = _localPosition;
        switch (origin)
        {
            case SeekOrigin.Begin:
                newPosition = offset;
                break;
            case SeekOrigin.Current:
                newPosition += offset;
                break;
            case SeekOrigin.End:
                newPosition = _length - offset;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
        }

        if (newPosition < 0) throw new ArgumentOutOfRangeException(nameof(offset), $"New resulting position {newPosition} is < 0");
        if (newPosition > _length) throw new ArgumentOutOfRangeException(nameof(offset), $"New resulting position {newPosition} is > Length {_length}");

        // Check that we can seek on the origin stream
        _baseStream.Position = _basePosition + newPosition;
        _localPosition = newPosition;

        return newPosition;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    {
        ThrowIfDisposed(); _baseStream.Flush();
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            if (_baseStream != Stream.Null)
            {
                try
                {
                    _baseStream.Dispose();
                }
                catch
                {
                    // ignored
                }
                _baseStream = Stream.Null;
            }
        }
    }

    public override void Write(byte[] buffer, int offset, int count)
        => Write(new ReadOnlySpan<byte>(buffer, offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        if (buffer.Length == 0) return;

        long length = Length;
        var isOverLength = _localPosition + buffer.Length > length;
        var maxLength = isOverLength ? (int)(length - _localPosition) : buffer.Length;
        _baseStream.Position = _basePosition + _localPosition;
        _baseStream.Write(buffer.Slice(0, maxLength));
        _localPosition += maxLength;
        if (isOverLength)
        {
            ThrowCannotWriteOutside();
        }
    }
        
    public override void WriteByte(byte value)
    {
        ThrowIfDisposed();
        if (_localPosition >= _length) ThrowCannotWriteOutside();

        _baseStream.Position = _basePosition + _localPosition;
        _baseStream.WriteByte(value);
        _localPosition++;
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (buffer.Length == 0) return;

        long length = Length;
        var isOverLength = _localPosition + buffer.Length > length;
        var maxLength = isOverLength ? (int)(length - _localPosition) : buffer.Length;
        _baseStream.Position = _basePosition + _localPosition;
        await _baseStream.WriteAsync(buffer.Slice(0, maxLength), cancellationToken);
        _localPosition += maxLength;
        if (isOverLength)
        {
            ThrowCannotWriteOutside();
        }
    }

    [DoesNotReturn]
    private void ThrowCannotWriteOutside()
    {
        throw new InvalidOperationException("Cannot write outside of this stream slice");
    }
}