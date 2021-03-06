﻿using System;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp.AspNetCore.Mvc.ApplicationConfigurations;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Http.Client.DynamicProxying;
using Volo.Abp.Users;

namespace Volo.Abp.AspNetCore.Mvc.Client
{
    public class CachedApplicationConfigurationClient : ICachedApplicationConfigurationClient, ITransientDependency
    {
        public IHttpContextAccessor HttpContextAccessor { get; set; }

        protected IHttpClientProxy<IAbpApplicationConfigurationAppService> Proxy { get; }
        protected ICurrentUser CurrentUser { get; }
        protected IDistributedCache<ApplicationConfigurationDto> Cache { get; }

        public CachedApplicationConfigurationClient(
            IDistributedCache<ApplicationConfigurationDto> cache,
            IHttpClientProxy<IAbpApplicationConfigurationAppService> proxy,
            ICurrentUser currentUser,
            IHttpContextAccessor httpContextAccessor)
        {
            Proxy = proxy;
            CurrentUser = currentUser;
            HttpContextAccessor = httpContextAccessor;
            Cache = cache;
        }

        public async Task<ApplicationConfigurationDto> GetAsync()
        {
            var cacheKey = CreateCacheKey();
            var httpContext = HttpContextAccessor?.HttpContext;

            if (httpContext != null && httpContext.Items[cacheKey] is ApplicationConfigurationDto configuration)
            {
                return configuration;
            }

            configuration = await Cache.GetOrAddAsync(
                CreateCacheKey(),
                async () => await Proxy.Service.GetAsync(),
                () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
                }
            );

            if (httpContext != null)
            {
                httpContext.Items[cacheKey] = configuration;
            }

            return configuration;
        }

        protected virtual string CreateCacheKey()
        {
            return $"ApplicationConfiguration_{CurrentUser.Id?.ToString("N") ?? "Anonymous"}";
        }
    }
}