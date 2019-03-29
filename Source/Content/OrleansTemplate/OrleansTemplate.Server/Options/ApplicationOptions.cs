namespace OrleansTemplate.Server.Options
{
    using Orleans.Configuration;

    public class ApplicationOptions
    {
#if (ApplicationInsights)
        public ApplicationInsightsOptions ApplicationInsights { get; set; }

#endif
        public ClusterOptions Cluster { get; set; }

        public StorageOptions Storage { get; set; }
    }
}
