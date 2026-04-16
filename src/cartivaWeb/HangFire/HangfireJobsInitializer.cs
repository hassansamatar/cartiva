using Cartiva.Application.Abstractions;
using Hangfire;

namespace cartivaWeb.HangFire
{
    public static class HangfireJobsInitializer
    {
        public static void RegisterRecurringJobs()

        {

       
            // Approve shipments for active companies (every 2 min in dev, hourly in prod)
            RecurringJob.AddOrUpdate<ICompanyShipmentApprovalService>(
                "approve-company-shipments",
                service => service.ApprovePendingCompanyShipmentsAsync(CancellationToken.None),
                Cron.MinuteInterval(2)); // Or use Cron.MinuteInterval(2) for testing

            // Process approved shipments (every 2 min in dev, hourly in prod)
            RecurringJob.AddOrUpdate<ICompanyShipmentProcessingService>(
                "process-approved-shipments",
                service => service.ProcessApprovedShipmentsAsync(CancellationToken.None),
                Cron.MinuteInterval(2)); // Or use Cron.MinuteInterval(2) for testing

        }
    }
}
