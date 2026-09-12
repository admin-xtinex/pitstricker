using System;

namespace PitStrikerServer
{
    public class ServerConfiguration
    {
        public int Port { get; set; } = 7777;
        public string Host { get; set; } = "0.0.0.0";
        public string Environment { get; set; } = "Production";
        public string LogLevel { get; set; } = "Info";
        public int TickRateHz { get; set; } = 20;
        public int RollingSnapshotRateHz { get; set; } = 15;
        public int IdleSnapshotRateHz { get; set; } = 2;

        public static ServerConfiguration LoadFromEnvironmentAndArgs(string[] args)
        {
            var config = new ServerConfiguration();

            // Check Environment variables (Google Cloud Run / Compute Engine / Docker standard)
            string? envPort = System.Environment.GetEnvironmentVariable("PORT");
            if (!string.IsNullOrEmpty(envPort) && int.TryParse(envPort, out int p))
            {
                config.Port = p;
            }

            string? envHost = System.Environment.GetEnvironmentVariable("HOST");
            if (!string.IsNullOrEmpty(envHost))
            {
                config.Host = envHost;
            }

            string? envLog = System.Environment.GetEnvironmentVariable("LOG_LEVEL");
            if (!string.IsNullOrEmpty(envLog))
            {
                config.LogLevel = envLog;
            }

            string? envEnv = System.Environment.GetEnvironmentVariable("ENVIRONMENT");
            if (!string.IsNullOrEmpty(envEnv))
            {
                config.Environment = envEnv;
            }

            // Command line arguments override
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--port" && i + 1 < args.Length && int.TryParse(args[i + 1], out int argPort))
                {
                    config.Port = argPort;
                }
                else if (args[i] == "--host" && i + 1 < args.Length)
                {
                    config.Host = args[i + 1];
                }
                else if (args[i] == "--log-level" && i + 1 < args.Length)
                {
                    config.LogLevel = args[i + 1];
                }
            }

            return config;
        }
    }
}
