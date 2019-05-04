using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Common.Extensions
{
    public static class PipeWriterExtensions
    {
        //https://blog.marcgravell.com/2018/07/pipe-dreams-part-2.html
        public static ValueTask<bool> Flush(this PipeWriter writer)
        {
            bool GetResult(FlushResult flush)
                // tell the calling code whether any more messages
                // should be written
                => !(flush.IsCanceled || flush.IsCompleted);

            async ValueTask<bool> Awaited(ValueTask<FlushResult> incomplete)
                => GetResult(await incomplete);

            // apply back-pressure etc
            var flushTask = writer.FlushAsync();

            return flushTask.IsCompletedSuccessfully
                ? new ValueTask<bool>(GetResult(flushTask.Result))
                : Awaited(flushTask);
        }
    }
}