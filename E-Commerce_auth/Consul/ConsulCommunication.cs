using Consul;
using System.Text;

namespace E_Commerce_auth.Consul
{
    public class ConsulCommunication : IHostedService
    {
        private readonly IConsulClient _consul;
        private readonly IConfiguration _cfg;
        private readonly ILogger<ConsulCommunication> _log;

        private string ServiceId => _cfg["Consul:ServiceId"]!;
        private string ServiceName => _cfg["Consul:ServiceName"]!;
        private string ServiceHost => _cfg["Consul:ServiceHost"]!;
        private int ServicePort => int.Parse(_cfg["Consul:ServicePort"]!);
        private string HealthPath => _cfg["Consul:HealthPath"] ?? "/health";
        private int HealthIntervalSeconds => int.Parse(_cfg["Consul:HealthIntervalSeconds"] ?? "10");

        public ConsulCommunication(IConsulClient consul, IConfiguration cfg, ILogger<ConsulCommunication> log)
        {
            _consul = consul;
            _cfg = cfg;
            _log = log;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Consul chạy trong Docker nhưng service chạy host:
            // health check URL phải là host.docker.internal để Consul container gọi được vào host
            var healthUrl = $"http://host.docker.internal:{ServicePort}{HealthPath}";

            var reg = new AgentServiceRegistration
            {
                ID = ServiceId,
                Name = ServiceName,
                Address = ServiceHost,   // address mà client sẽ gọi tới (LAN IP)
                Port = ServicePort,
                Check = new AgentServiceCheck
                {
                    HTTP = healthUrl,
                    Interval = TimeSpan.FromSeconds(HealthIntervalSeconds),
                    Timeout = TimeSpan.FromSeconds(2),
                    DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
                }
            };

            await _consul.Agent.ServiceRegister(reg, cancellationToken);
            _log.LogInformation("Registered {ServiceName} ({ServiceId}) to Consul. Health={Health}", ServiceName, ServiceId, healthUrl);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _consul.Agent.ServiceDeregister(ServiceId, cancellationToken);
                _log.LogInformation("Deregistered {ServiceName} ({ServiceId}) from Consul.", ServiceName, ServiceId);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to deregister {ServiceId}", ServiceId);
            }
        }
    }
}
