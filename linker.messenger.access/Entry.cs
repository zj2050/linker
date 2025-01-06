﻿using linker.messenger.access;
using linker.messenger.decenter;
using linker.messenger.signin;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.api
{
    public static class Entry
    {
        public static ServiceCollection AddAccessClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<AccessApiController>();
            serviceCollection.AddSingleton<AccessDecenter>();
            serviceCollection.AddSingleton<AccessClientMessenger>();
            return serviceCollection;
        }
        public static ServiceProvider UseAccessClient(this ServiceProvider serviceProvider)
        {
            IApiServer apiServer = serviceProvider.GetService<IApiServer>();
            apiServer.AddPlugins(new List<libs.api.IApiController> { serviceProvider.GetService<AccessApiController>() });

            DecenterClientTransfer decenterClientTransfer = serviceProvider.GetService<DecenterClientTransfer>();
            decenterClientTransfer.AddDecenters(new List<IDecenter> { serviceProvider.GetService<AccessDecenter>() });


            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<AccessClientMessenger>() });

            return serviceProvider;
        }

        public static ServiceCollection AddAccessServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<AccessServerMessenger>();
            return serviceCollection;
        }
        public static ServiceProvider UseAccessServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<AccessServerMessenger>() });

            return serviceProvider;
        }
    }
}
