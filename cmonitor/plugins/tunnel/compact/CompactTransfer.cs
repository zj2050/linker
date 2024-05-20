﻿using cmonitor.client.tunnel;
using cmonitor.config;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace cmonitor.plugins.tunnel.compact
{
    public sealed class CompactTransfer
    {
        private List<ICompact> compacts;

        private readonly Config config;
        private readonly ServiceProvider serviceProvider;
        public CompactTransfer(Config config, ServiceProvider serviceProvider)
        {
            this.config = config;
            this.serviceProvider = serviceProvider;
        }

        public void Load(Assembly[] assembs)
        {
            IEnumerable<Type> types = ReflectionHelper.GetInterfaceSchieves(assembs, typeof(ICompact));
            compacts = types.Select(c => (ICompact)serviceProvider.GetService(c)).Where(c => c != null).Where(c => string.IsNullOrWhiteSpace(c.Name) == false).ToList();

            Logger.Instance.Warning($"load tunnel compacts:{string.Join(",", compacts.Select(c => c.Name))}");
        }

        public List<TunnelCompactTypeInfo> GetTypes()
        {
            return compacts.Select(c => new TunnelCompactTypeInfo { Value = c.Type, Name = c.Type.ToString() }).Distinct(new TunnelCompactTypeInfoEqualityComparer()).ToList();
        }


        public async Task<TunnelCompactIPEndPoint[]> GetExternalIPAsync(TunnelProtocolType protocolType)
        {
            TunnelCompactIPEndPoint[] endpoints = new TunnelCompactIPEndPoint[config.Data.Client.Tunnel.Servers.Length];

            for (int i = 0; i < config.Data.Client.Tunnel.Servers.Length; i++)
            {
                TunnelCompactInfo item = config.Data.Client.Tunnel.Servers[i];
                if (item.Disabled || string.IsNullOrWhiteSpace(item.Host)) continue;
                ICompact compact = compacts.FirstOrDefault(c => c.Type == item.Type);
                if (compact == null) continue;

                try
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    IPEndPoint server = NetworkHelper.GetEndPoint(item.Host, 3478);
                    sw.Stop();
                    if (sw.ElapsedMilliseconds > 1000)
                    {
                        Logger.Instance.Warning($"get domain ip time:{sw.ElapsedMilliseconds}ms");
                    }
                    if (protocolType == TunnelProtocolType.Tcp)
                    {
                        TunnelCompactIPEndPoint externalIP = await compact.GetTcpExternalIPAsync(server);
                        endpoints[i] = externalIP;
                    }
                    else if (protocolType == TunnelProtocolType.Udp)
                    {
                        TunnelCompactIPEndPoint externalIP = await compact.GetUdpExternalIPAsync(server);
                        endpoints[i] = externalIP;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
            }

            return endpoints.Where(c => c != null && c.Remote != null).ToArray();
        }
    }
}
