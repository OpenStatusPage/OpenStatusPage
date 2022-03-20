namespace OpenStatusPage.Shared.Utilities
{
    public class Debouncer
    {
        protected CancellationTokenSource CancellationToken { get; set; }

        public void Debounce(TimeSpan debounceFor, Func<Task> method)
        {
            lock (this)
            {
                //Cancel existing task
                CancellationToken?.Cancel();
                CancellationToken?.Dispose();
                CancellationToken = new();

                Task.Run(async () =>
                {
                    await Task.Delay(debounceFor, CancellationToken.Token);
                    await method();
                });
            }
        }
    }
}
