namespace SignalGenerator.Web.Services
{
    public class AppState
    {
        public event Func<Task>? OnLoadingStart;
        public event Func<Task>? OnLoadingEnd;

        public async Task StartLoading() => await InvokeAsync(OnLoadingStart);
        public async Task EndLoading() => await InvokeAsync(OnLoadingEnd);

        private async Task InvokeAsync(Func<Task>? eventHandler)
        {
            if (eventHandler != null)
            {
                foreach (var handler in eventHandler.GetInvocationList().Cast<Func<Task>>())
                {
                    await handler.Invoke();
                }
            }
        }
    }
}
