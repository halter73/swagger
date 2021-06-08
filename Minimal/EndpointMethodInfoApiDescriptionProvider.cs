// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    internal class EndpointMethodInfoApiDescriptionProvider : IApiDescriptionProvider
    {
        private readonly EndpointDataSource _endpointDataSource;

        // Executes before MVC's DefaultApiDescriptionProvider and GrpcHttpApiDescriptionProvider
        public int Order => -1100;

        public EndpointMethodInfoApiDescriptionProvider(EndpointDataSource endpointDataSource)
        {
            _endpointDataSource = endpointDataSource;
        }

        public void OnProvidersExecuting(ApiDescriptionProviderContext context)
        {
            foreach (var endpoint in _endpointDataSource.Endpoints)
            {
                if (endpoint is RouteEndpoint routeEndpoint
                    && routeEndpoint.Metadata.GetMetadata<MethodInfo>() is { } methodInfo
                    && routeEndpoint.Metadata.GetMetadata<IHttpMethodMetadata>() is { } httpMethodMetadata)
                {
                    foreach (var httpMethod in httpMethodMetadata.HttpMethods)
                    {
                        context.Results.Add(CreateApiDescription(routeEndpoint.RoutePattern, httpMethod, methodInfo));
                    }
                }
            }
        }

        public void OnProvidersExecuted(ApiDescriptionProviderContext context)
        {
        }

        private static ApiDescription CreateApiDescription(RoutePattern pattern, string httpMethod, MethodInfo actionMethodInfo)
        {
            var apiDescription = new ApiDescription
            {
                HttpMethod = httpMethod,
                RelativePath = pattern.RawText?.TrimStart('/'),
                ActionDescriptor = new ActionDescriptor
                {
                    RouteValues =
                    {
                        // Swagger uses this to group endpoints together.
                        // For now, put all endpoints configured with Map(Delegate) together.
                        // TODO: Use some other metadata for this.
                        ["controller"] = "Map"
                    },
                },
            };

            var responseType = actionMethodInfo.ReturnType;

            if (AwaitableInfo.IsTypeAwaitable(responseType, out var awaitableInfo))
            {
                responseType = awaitableInfo.ResultType;
            }

            if (CreateApiResponseType(responseType) is { } apiResponseType)
            {
                apiDescription.SupportedResponseTypes.Add(apiResponseType);
            }

            return apiDescription;
        }

        private static ApiResponseType? CreateApiResponseType(Type responseType)
        {
            if (typeof(IResult).IsAssignableFrom(responseType))
            {
                // Can't determine anything about IResults yet. IResult<T> could help here.
                // REVIEW: Is there any value in returning an ApiResponseType with StatusCode = 200 and that's it?
                return null;
            }

            if (responseType == typeof(void))
            {
                return new ApiResponseType
                {
                    ModelMetadata = new EndpointMethodInfoModelMetadata(ModelMetadataIdentity.ForType(typeof(void))),
                    StatusCode = 200,
                };
            }

            if (responseType == typeof(string))
            {
                // This uses HttpResponse.WriteAsync(string) method which doesn't set a content type. It could be anything,
                // but I think "text/plain" is a reasonable assumption.

                return new ApiResponseType
                {
                    ApiResponseFormats = { new ApiResponseFormat { MediaType = "text/plain" } },
                    ModelMetadata = new EndpointMethodInfoModelMetadata(ModelMetadataIdentity.ForType(typeof(string))),
                    StatusCode = 200,
                };
            }

            // Everything else is written using HttpResponse.WriteAsJsonAsync<TValue>(T).
            return new ApiResponseType
            {
                ApiResponseFormats = { new ApiResponseFormat { MediaType = "application/json" } },
                ModelMetadata = new EndpointMethodInfoModelMetadata(ModelMetadataIdentity.ForType(responseType)),
                StatusCode = 200,
            };
        }
    }
}
