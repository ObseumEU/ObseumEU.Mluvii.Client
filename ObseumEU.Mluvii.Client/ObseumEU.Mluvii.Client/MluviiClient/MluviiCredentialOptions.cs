namespace ObseumEU.Mluvii.Client
{
    public class MluviiCredentialOptions
    {
        public string BaseApiEndpoint { get; set; }
        public string TokenEndpoint { get; set; }
        public string Name { get; set; }
        public string Secret { get; set; }
        public string WebhookSecret { get; set; }
        public string WebhookEndpoint { get; set; }
        public int Company { get; set; }
        public int Tenant { get; set; }
        public bool AutoRetry { get; set; }
    }
}