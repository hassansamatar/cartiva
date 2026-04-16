namespace cartivaWeb.HangFire
{
    public class TestJobService
    {
        public Task RunJob()
        {
            Console.WriteLine("🔥 Hangfire is working!");
            return Task.CompletedTask;
        }
    }
}
