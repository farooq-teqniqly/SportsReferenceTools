namespace Teqniqly.SportsReferenceClient.Common
{
    /// <summary>
    /// A read-only stream that forwards to an <see cref="HttpResponseMessage"/>'s content stream
    /// and disposes that response when the stream is disposed. Lets a caller stream a large
    /// response body without buffering it while still owning the response's lifetime.
    /// </summary>
    internal sealed class ResponseOwningStream : Stream
    {
        private readonly HttpResponseMessage _response;
        private readonly Stream _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseOwningStream"/> class.
        /// </summary>
        /// <param name="response">The response whose lifetime this stream owns.</param>
        /// <param name="inner">The response's content stream to forward reads to.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="response"/> or <paramref name="inner"/> is null.
        /// </exception>
        public ResponseOwningStream(HttpResponseMessage response, Stream inner)
        {
            ArgumentNullException.ThrowIfNull(response);
            ArgumentNullException.ThrowIfNull(inner);

            _response = response;
            _inner = inner;
        }

        /// <inheritdoc />
        public override bool CanRead => _inner.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => _inner.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => _inner.Length;

        /// <inheritdoc />
        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        /// <inheritdoc />
        public override void Flush() => _inner.Flush();

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) =>
            _inner.Read(buffer, offset, count);

        /// <inheritdoc />
        public override int Read(Span<byte> buffer) => _inner.Read(buffer);

        /// <inheritdoc />
        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        ) => _inner.ReadAsync(buffer, offset, count, cancellationToken);

        /// <inheritdoc />
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default
        ) => _inner.ReadAsync(buffer, cancellationToken);

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                _response.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        public override async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync().ConfigureAwait(false);
            _response.Dispose();
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}
