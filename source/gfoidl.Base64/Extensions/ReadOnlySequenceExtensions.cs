using System;
using System.Buffers;

namespace gfoidl.Base64
{
    public static class ReadOnlySequenceExtensions
    {
        public static OperationStatus Encode(this IBase64 encoder, in ReadOnlySequence<byte> data, IBufferWriter<byte> base64, out long consumed, out long written)
        {
            if (data.IsEmpty)
            {
                consumed = 0;
                written = 0;
                return OperationStatus.Done;
            }

            if (base64 is null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.writer);

            throw new NotImplementedException();
        }
        //---------------------------------------------------------------------
        public static OperationStatus Decode(this IBase64 encoder, in ReadOnlySequence<byte> base64, IBufferWriter<byte> data, out long consumed, out long written)
        {
            if (base64.IsEmpty)
            {
                consumed = 0;
                written = 0;
                return OperationStatus.Done;
            }

            if (data is null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.writer);

            throw new NotImplementedException();
        }
    }
}
