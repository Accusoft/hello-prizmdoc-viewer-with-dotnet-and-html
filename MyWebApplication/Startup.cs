using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharpReverseProxy;

namespace MyWebApplication
{
    public class Startup
    {
        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            Logger = logger;

            ConfigUtil.ValidateConfiguration(Configuration);
        }

        public IConfiguration Configuration { get; }
        public ILogger<Startup> Logger { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();

            // Setup a proxy route to PAS (PrizmDoc Application Services).
            //
            // The viewer will send all of its requests for document content to this web app
            // using a base route of /pas-proxy. We setup this route here to be a reverse proxy
            // back to the actual PAS host.
            //
            // If you are using PrizmDoc Cloud, this proxy will inject your API
            // key before forwarding the request.
            //
            // In a production application, you would want to setup this reverse proxy outside
            // of your web application, say using IIS or nginx.
            app.UseProxy(new List<ProxyRule> {
                new ProxyRule {
                    Matcher = uri => uri.AbsolutePath.StartsWith("/pas-proxy/"),
                    Modifier = (req, user) =>
                    {
                        // Create a corresponding request to the actual PAS host
                        var match = Regex.Match(req.RequestUri.PathAndQuery, "/pas-proxy/(.+)");
                        var path = match.Groups[1].Value;
                        var pasBaseUri = new Uri(Configuration["PrizmDoc:PasBaseUrl"]);
                        req.RequestUri = new Uri(pasBaseUri, path);

                        // Inject the PrizmDoc Cloud API key if one was defined
                        var apiKey = Configuration["PrizmDoc:CloudApiKey"];
                        if (apiKey != null && apiKey.Trim() != "") {
                            req.Headers.Add("acs-api-key", apiKey);
                        }
                    }
                }
            }, result =>
            {
                Logger.LogDebug($"Proxy: {result.ProxyStatus} Url: {result.OriginalUri} Time: {result.Elapsed}");
                if (result.ProxyStatus == ProxyStatus.Proxied)
                {
                    Logger.LogDebug($"        New Url: {result.ProxiedUri.AbsoluteUri} Status: {result.HttpStatusCode}");
                }
            });

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
