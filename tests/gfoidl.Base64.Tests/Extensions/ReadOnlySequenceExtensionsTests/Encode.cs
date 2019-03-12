using System;
using System.Buffers;
using NUnit.Framework;

namespace gfoidl.Base64.Tests.Extensions.ReadOnlySequenceExtensionsTests
{
    [TestFixture]
    public class Encode
    {
        [Test]
        public void BufferWriter_is_null___throws_ArgumentNull()
        {
            var sequence = new ReadOnlySequence<byte>(new byte[100]);
            IBufferWriter<byte> writer = null;

            Assert.Throws<ArgumentNullException>(() => Base64.Default.Encode(sequence, writer, out long consumed, out long written));
        }
        //---------------------------------------------------------------------
        [Test]
        public void Empty_sequence___OK_and_nothing_read_and_written()
        {
            var sequence = new ReadOnlySequence<byte>();
            IBufferWriter<byte> writer = null;

            var status = Base64.Default.Encode(sequence, writer, out long consumed, out long written);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(OperationStatus.Done, status);
                Assert.AreEqual(0, consumed);
                Assert.AreEqual(0, written);
            });
        }
    }
}
