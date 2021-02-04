using System.Linq;
using Causality.Server.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Causality.Shared.Models;
using System.Net.Http;
using Microsoft.AspNetCore.Components;
using System;

namespace Causality.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // Add compression this is the best so far
            services.AddResponseCompression();   // 506b/9ms  481b/2ms

            // Add dbcontext connectionstring
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));

            // Add dbcontect
            services.AddTransient<ApplicationDbContext, ApplicationDbContext>();

            // Add all db services to dbcontext
            services.AddTransient<Repository<Event, ApplicationDbContext>>();
            services.AddTransient<Repository<Cause, ApplicationDbContext>>();
            services.AddTransient<Repository<Class, ApplicationDbContext>>();
            services.AddTransient<Repository<Exclude, ApplicationDbContext>>();
            services.AddTransient<Repository<Effect, ApplicationDbContext>>();
            services.AddTransient<Repository<User, ApplicationDbContext>>();
            services.AddTransient<Repository<Meta, ApplicationDbContext>>();

            // Use memeorycache
            services.AddMemoryCache();

            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddGrpc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Add support for compression
            app.UseResponseCompression();

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors();

            // Add Grpc with default options
            app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

            app.UseEndpoints(endpoints =>
            {
                // add all the Grpc services
                endpoints.MapGrpcService<Services.EventService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<Services.CauseService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<Services.ClassService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<Services.ExcludeService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<Services.EffectService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<Services.UserService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<Services.MetaService>().EnableGrpcWeb().RequireCors("AllowAll");

                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
