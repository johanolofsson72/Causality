using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using BlazorOnlineState;
using Causality.Client.Services;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TG.Blazor.IndexedDB;

namespace Causality.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.RootComponents.Add<App>("#app");

            // Add the Telerik components
            builder.Services.AddTelerikBlazor();

            // Add the Grpc channels
            builder.Services.AddSingleton(services =>
            {
                var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var backendUrl = services.GetRequiredService<NavigationManager>().BaseUri;
                var channel = GrpcChannel.ForAddress(backendUrl, new GrpcChannelOptions { HttpClient = httpClient });
                return new Causality.Shared.Models.EventService.EventServiceClient(channel);
            });
            builder.Services.AddSingleton(services =>
            {
                var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var backendUrl = services.GetRequiredService<NavigationManager>().BaseUri;
                var channel = GrpcChannel.ForAddress(backendUrl, new GrpcChannelOptions { HttpClient = httpClient });
                return new Causality.Shared.Models.CauseService.CauseServiceClient(channel);
            });
            builder.Services.AddSingleton(services =>
            {
                var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var backendUrl = services.GetRequiredService<NavigationManager>().BaseUri;
                var channel = GrpcChannel.ForAddress(backendUrl, new GrpcChannelOptions { HttpClient = httpClient });
                return new Causality.Shared.Models.ClassService.ClassServiceClient(channel);
            });
            builder.Services.AddSingleton(services =>
            {
                var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var backendUrl = services.GetRequiredService<NavigationManager>().BaseUri;
                var channel = GrpcChannel.ForAddress(backendUrl, new GrpcChannelOptions { HttpClient = httpClient });
                return new Causality.Shared.Models.ExcludeService.ExcludeServiceClient(channel);
            });
            builder.Services.AddSingleton(services =>
            {
                var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var backendUrl = services.GetRequiredService<NavigationManager>().BaseUri;
                var channel = GrpcChannel.ForAddress(backendUrl, new GrpcChannelOptions { HttpClient = httpClient });
                return new Causality.Shared.Models.EffectService.EffectServiceClient(channel);
            });
            builder.Services.AddSingleton(services =>
            {
                var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var backendUrl = services.GetRequiredService<NavigationManager>().BaseUri;
                var channel = GrpcChannel.ForAddress(backendUrl, new GrpcChannelOptions { HttpClient = httpClient });
                return new Causality.Shared.Models.UserService.UserServiceClient(channel);
            });
            builder.Services.AddSingleton(services =>
            {
                var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var backendUrl = services.GetRequiredService<NavigationManager>().BaseUri;
                var channel = GrpcChannel.ForAddress(backendUrl, new GrpcChannelOptions { HttpClient = httpClient });
                return new Causality.Shared.Models.MetaService.MetaServiceClient(channel);
            });

            // Add http client
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            // Add localstorage nuget
            builder.Services.AddBlazoredLocalStorage();

            // Add onlinestate nuget
            builder.Services.AddTransient<OnlineStateService>();

            // Add and Init indexedDB nuget
            builder.Services.AddIndexedDB(dbStore =>
            {
                dbStore.DbName = "Causality";
                dbStore.Version = 1;

                dbStore.Stores.Add(new StoreSchema
                {
                    Name = "Blobs",
                    PrimaryKey = new IndexSpec { Name = "id", KeyPath = "id", Auto = true },
                    Indexes = new List<IndexSpec>
                    {
                        new IndexSpec{Name="key", KeyPath = "key", Auto = false},
                        new IndexSpec{Name="value", KeyPath = "value", Auto = false}
                    }
                });

                dbStore.Stores.Add(new StoreSchema
                {
                    Name = "Outbox",
                    PrimaryKey = new IndexSpec { Auto = true }
                });
            });

            // Add the stateobject
            builder.Services.AddScoped<ApplicationState>();

            // Add the data services
            builder.Services.AddTransient<EventService>();
            builder.Services.AddTransient<CauseService>();
            builder.Services.AddTransient<ClassService>();
            builder.Services.AddTransient<ExcludeService>();
            builder.Services.AddTransient<EffectService>();
            builder.Services.AddTransient<UserService>();
            builder.Services.AddTransient<MetaService>();

            await builder.Build().RunAsync();
        }
    }
}
