using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using NUnit.Framework;

namespace gfoidl.Base64.Tests.Extensions.ReadOnlySequenceExtensionsTests
{
    [TestFixture]
    public class Encode
    {
        [Test]
        public void BufferWriter_is_null___throws_ArgumentNull()
        {
            var sequence               = new ReadOnlySequence<byte>(new byte[100]);
            IBufferWriter<byte> writer = null;

            Assert.Throws<ArgumentNullException>(() => Base64.Default.Encode(sequence, writer, out long consumed, out long written));
        }
        //---------------------------------------------------------------------
        [Test]
        public void Empty_sequence___OK_and_nothing_read_and_written()
        {
            var sequence               = new ReadOnlySequence<byte>();
            IBufferWriter<byte> writer = null;

            OperationStatus status = Base64.Default.Encode(sequence, writer, out long consumed, out long written);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(OperationStatus.Done, status);
                Assert.AreEqual(0, consumed);
                Assert.AreEqual(0, written);
            });
        }
        //---------------------------------------------------------------------
        [Test]
        public async Task SingleSegment_encode___OK()
        {
            byte[] data = new byte[300];
            var rnd     = new Random();
            rnd.NextBytes(data);

            var sequence = new ReadOnlySequence<byte>(data);
            var pipe     = new Pipe();

            OperationStatus status  = Base64.Default.Encode(sequence, pipe.Writer, out long consumed, out long written);
            FlushResult flushResult = await pipe.Writer.FlushAsync();
            pipe.Writer.Complete();

            Assert.Multiple(() =>
            {
                Assert.AreEqual(OperationStatus.Done, status);
                Assert.AreEqual(300, consumed);
                Assert.AreEqual(400, written);
            });

            Assert.Multiple(async () =>
            {
                ReadResult readResult = await pipe.Reader.ReadAsync();
                Assert.IsTrue(readResult.IsCompleted);
                Assert.IsTrue(readResult.Buffer.IsSingleSegment);
                Assert.AreEqual(written, readResult.Buffer.First.Length);
            });
        }
        //---------------------------------------------------------------------
        [Test]
        public async Task MultiSegment_encode___OK()
        {
            var pipeOptions = PipeOptions.Default;
            var pipe        = new Pipe(pipeOptions);

            var data = new byte[2 * pipeOptions.MinimumSegmentSize];
            var rnd  = new Random();

            rnd.NextBytes(data);
            pipe.Writer.Write(data);

            rnd.NextBytes(data);
            pipe.Writer.Write(data.AsSpan(0, pipeOptions.MinimumSegmentSize / 2));

            pipe.Writer.Complete();

            ReadResult readResult = await pipe.Reader.ReadAsync();

            var resultPipe          = new Pipe();
            OperationStatus status  = Base64.Default.Encode(readResult.Buffer, resultPipe.Writer, out long consumed, out long written);
            FlushResult flushResult = await resultPipe.Writer.FlushAsync();
            resultPipe.Writer.Complete();

            Assert.Multiple(() =>
            {
                int consumedExpected = 2 * pipeOptions.MinimumSegmentSize + pipeOptions.MinimumSegmentSize / 2;
                int writtenExpected  = consumedExpected / 3 * 4;

                Assert.AreEqual(OperationStatus.Done, status);
                Assert.AreEqual(consumedExpected, consumed);
                Assert.AreEqual(writtenExpected, written);
            });

            Assert.Multiple(async () =>
            {
                readResult = await resultPipe.Reader.ReadAsync();
                Assert.IsTrue(readResult.IsCompleted);
                Assert.IsTrue(readResult.Buffer.IsSingleSegment);
                Assert.AreEqual(written, readResult.Buffer.Length);
            });
        }
    }
}
