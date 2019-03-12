using System;
using System.Buffers;

namespace gfoidl.Base64
{
    public static class ReadOnlySequenceExtensions
    {
        public static OperationStatus Encode(this IBase64 encoder, in ReadOnlySequence<byte> data, IBufferWriter<byte> base64Writer, out long consumed, out long written)
        {
            if (data.IsEmpty)
            {
                consumed = 0;
                written = 0;
                return OperationStatus.Done;
            }

            if (base64Writer is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.writer);

            if (data.IsSingleSegment)
                return EncodeSingleSegment(encoder, data.First, base64Writer, out consumed, out written);

            throw new NotImplementedException();
        }
        //---------------------------------------------------------------------
        public static OperationStatus Decode(this IBase64 encoder, in ReadOnlySequence<byte> base64, IBufferWriter<byte> dataWriter, out long consumed, out long written)
        {
            if (base64.IsEmpty)
            {
                consumed = 0;
                written = 0;
                return OperationStatus.Done;
            }

            if (dataWriter is null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.writer);

            throw new NotImplementedException();
        }
        //---------------------------------------------------------------------
        private static OperationStatus EncodeSingleSegment(IBase64 encoder, ReadOnlyMemory<byte> data, IBufferWriter<byte> base64Writer, out long consumed, out long written)
        {
            int encodedLength = encoder.GetEncodedLength(data.Length);
            Span<byte> encoded = base64Writer.GetSpan(encodedLength);

            OperationStatus status = encoder.Encode(data.Span, encoded, out int consumedBytes, out int writtenBytes);
            base64Writer.Advance(writtenBytes);

            consumed = consumedBytes;
            written = writtenBytes;

            return status;
        }
    }
}
