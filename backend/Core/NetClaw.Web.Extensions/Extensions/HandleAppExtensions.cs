using NetClaw.AspNetCore.Extensions.ErrorHandlers;
using NetClaw.AspNetCore.Extensions.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Refit;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace NetClaw.AspNetCore.Extensions.Extensions
{
    public static class HandleAppExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(
          this IApplicationBuilder app,
          JsonSerializerOptions jsonSerializerOptions = null,
          bool enableDiagnostics = false,
          Func<Exception, ErrorHandlers.ProblemDetails> exceptionConverter = null)
        {
            if (jsonSerializerOptions != null)
                GlobalExceptionHandling.JsonSerializerOptions = jsonSerializerOptions;
            return app.UseGlobalExceptionHandler(enableDiagnostics, exceptionConverter);
        }

        public static IApplicationBuilder UseGlobalExceptionHandler(
          this IApplicationBuilder app,
          bool enableDiagnostics = false,
          Func<Exception, ErrorHandlers.ProblemDetails> exceptionConverter = null)
        {
            //if (namingStrategy != null)
            //    GlobalExceptionHandling.NamingPolicy = namingStrategy;
            app.UseExceptionHandler((Action<IApplicationBuilder>)(errorApp => errorApp.Run((RequestDelegate)(async context =>
            {
                //ILogger logger = LoggerFactory.Create((Action<ILoggingBuilder>)(builder => builder.AddApplicationInsights())).CreateLogger("GlobalExceptionHandling");
                ILogger logger = LoggerFactory.Create((Action<ILoggingBuilder>)(builder => builder.AddConsole())).CreateLogger("GlobalExceptionHandling");
                IExceptionHandlerPathFeature handlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                Exception error1 = handlerPathFeature?.Error;
                string message = handlerPathFeature?.Error?.Message;
                object[] objArray = Array.Empty<object>();
                //logger.LogError(error1, message, objArray);
                if (enableDiagnostics)
                {
                    //Trace.TraceError(handlerPathFeature?.Error?.Message);
                    logger.LogError(handlerPathFeature?.Error?.Message);
                }
                ErrorHandlers.ProblemDetails problemDetails1 = (ErrorHandlers.ProblemDetails)null;
                if (exceptionConverter != null && handlerPathFeature != null)
                    problemDetails1 = exceptionConverter(handlerPathFeature.Error);
                if (problemDetails1 == null)
                {
                    Exception error2 = handlerPathFeature?.Error;
                    ErrorHandlers.ProblemDetails problemDetails2;
                    switch (error2)
                    {
                        case ArgumentNullException argumentNullException4:
                            problemDetails2 = GlobalExceptionHandling.GetProblemDetails(argumentNullException4);
                            break;
                        case ArgumentException argumentException4:
                            problemDetails2 = argumentException4.GetProblemDetails();
                            break;
                        case ValidationException validationException7:
                            problemDetails2 = validationException7.GetProblemDetails();
                            break;
                        case ApplicationException validationException8:
                            problemDetails2 = validationException8.GetProblemDetails();
                            break;
                        case InvalidOperationException validationException9:
                            problemDetails2 = validationException9.GetProblemDetails();
                            break;
                        case AggregateException aggregateException4:
                            problemDetails2 = aggregateException4.GetProblemDetails();
                            break;
                        case ApiException apiException4:
                            problemDetails2 = apiException4.GetProblemDetails();
                            break;
                        case BadRequestException badRequestException:
                            problemDetails2 = badRequestException.GetProblemDetails();
                            break;
                        case NotFoundException notFoundException:
                            problemDetails2 = notFoundException.GetProblemDetails();
                            break;
                        case null:
                            throw new SwitchExpressionException((object)error2);
                        default:
                            problemDetails2 = new ErrorHandlers.ProblemDetails()
                            {
                                Status = HttpStatusCode.InternalServerError,
                                ErrorMessage = "An error occurred. Please try again later."
                            };
                            break;
                    }
                    problemDetails1 = problemDetails2;
                }
                if (string.IsNullOrEmpty(problemDetails1.TraceId))
                    problemDetails1.TraceId = Activity.Current?.RootId ?? Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
                if (problemDetails1 != null && problemDetails1.Status == HttpStatusCode.InternalServerError)
                    context.Response.StatusCode = 500;
                context.Response.StatusCode = (int)problemDetails1.Status;
                context.Response.ContentType = "application/json";
                if (enableDiagnostics)
                {
                    //Trace.WriteLine(problemDetails1 != null ? problemDetails1.ToJson() : (string)null);
                    logger.LogError(problemDetails1 != null ? problemDetails1.ToJson() : (string)null);
                }
                await context.Response.WriteAsync(problemDetails1 != null ? problemDetails1.ToJson() : (string)null).ConfigureAwait(false);

                //if (context.Request.Path.ToString().StartsWith("/api"))
                //    await context.Response.WriteAsync(problemDetails1 != null ? problemDetails1.ToJson() : (string)null).ConfigureAwait(false);
                //else
                //    context.Response.Redirect($"/error?traceId={problemDetails1.TraceId}");
            }))));
            return app;
        }
    }
}
