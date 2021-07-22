using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using SignalMonitoring.API.Hubs;
using WebAPI.Models;

namespace CRUD_WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                              .AddJsonOptions(options => {
                                  var resolver = options.SerializerSettings.ContractResolver;
                                  if (resolver != null)
                                      (resolver as DefaultContractResolver).NamingStrategy = null;
                              });

            services.AddDbContext<PaymentDetailContext>(options =>
                    options.UseSqlServer(Configuration.GetConnectionString("DevConnection")));

            services.AddSignalR();
            services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
            {
                builder
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .WithOrigins("http://localhost:4400");
            }));

            //services.AddCors();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // app.UseCors(options =>
            //options.WithOrigins("http://localhost:4200")
            //.AllowCredentials()
            //.AllowAnyMethod()
            //.AllowAnyHeader());

            app.UseCors("CorsPolicy");

            //signalR service configured
            app.UseSignalR(routes =>
            {
                routes.MapHub<SignalHub>("/signalHub");
            });

            app.UseMvc();
        }
    }
}
