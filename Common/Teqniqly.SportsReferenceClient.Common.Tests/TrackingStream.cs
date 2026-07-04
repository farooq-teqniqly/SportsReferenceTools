using System.Text;

namespace Teqniqly.SportsReferenceClient.Common.Tests
{
    // A stream that records whether it was disposed, used to prove GetPageAsync transfers
    // ownership of the response (and its content stream) to the returned stream.
    internal sealed class TrackingStream : MemoryStream
    {
        public TrackingStream(byte[] data)
            : base(data) { }

        public TrackingStream(string content)
            : this(Encoding.UTF8.GetBytes(content)) { }

        public bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
