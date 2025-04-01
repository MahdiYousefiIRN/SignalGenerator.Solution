namespace SignalGenerator.Web.Services
{
    using System;
    using System.Threading.Tasks;

    public class AppState
    {
        public event Func<Task>? OnLoadingStart;
        public event Func<Task>? OnLoadingEnd;

        public async Task StartLoading()
        {
            if (OnLoadingStart != null)
                await OnLoadingStart.Invoke();
        }

        public async Task EndLoading()
        {
            if (OnLoadingEnd != null)
                await OnLoadingEnd.Invoke();
        }
    }

}
