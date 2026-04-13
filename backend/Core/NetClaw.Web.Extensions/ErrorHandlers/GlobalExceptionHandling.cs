using NetClaw.AspNetCore.Extensions.Exceptions;
using Refit;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace NetClaw.AspNetCore.Extensions.ErrorHandlers
{
    public static class GlobalExceptionHandling
    {
        public static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        private const string CONCURRENCY_ERROR = "concurrency";
        private const string INVALID_ARGUMENT = "invalid_argument";
        private const string ARGUMENT_ERROR_MESSAGGE = "One or more validation errors occurred.";
        public const string CONCURRENCY_ERROR_MESSAGE = "An concurrent error occur when execute this request. Please retry again";
        public const string INTERNALE_ERROR_MESSAGE = "An error occurred. Please try again later.";

        internal static ProblemDetails GetProblemDetails(
          this ArgumentNullException argumentNullException)
        {
            ArgumentException argumentException = (ArgumentException)argumentNullException;
            if (argumentException != null)
                return argumentException.GetProblemDetails();
            return new ProblemDetails()
            {
                Status = HttpStatusCode.BadRequest,
                TraceId = "",
                ErrorMessage = argumentNullException?.Message
            };
        }

        internal static ProblemDetails GetProblemDetails(
          this ArgumentException argumentException)
        {
            ProblemDetails problemDetails = new ProblemDetails()
            {
                Status = HttpStatusCode.BadRequest,
                TraceId = "",
                ErrorMessage = argumentException?.Message
            };
            if (string.IsNullOrEmpty(argumentException.ParamName))
            {
                if (!string.IsNullOrEmpty(argumentException.Message))
                    problemDetails.ErrorMessage = argumentException.Message;
            }
            else
            {
                problemDetails.ErrorDetails = new ProblemResultCollection()
        {
          new GenericValidationResult("invalid_argument", argumentException.Message, new string[1]
          {
            argumentException.ParamName
          })
        };
                problemDetails.ErrorMessage = "One or more validation errors occurred.";
            }
            return problemDetails;
        }

        private static string ExcludingParameterFor(string argumentExceptionMessage)
        {
            if (string.IsNullOrEmpty(argumentExceptionMessage))
                return string.Empty;
            if (argumentExceptionMessage.IndexOf(' ') <= 0)
                return argumentExceptionMessage;
            return argumentExceptionMessage.Substring(0, argumentExceptionMessage.IndexOf(' ') - 0);
        }

        internal static ProblemDetails GetProblemDetails(
          this ValidationException validationException)
        {
            ProblemDetails problemDetails = new ProblemDetails()
            {
                Status = HttpStatusCode.BadRequest,
                TraceId = "",
                ErrorMessage = validationException.Message
            };
            ProblemDetails problem;
            if (GlobalExceptionHandling.TryParseProblemDetail(validationException.Message, out problem))
            {
                if (problem != null && problem.Status == (HttpStatusCode)0)
                    problem.Status = HttpStatusCode.BadRequest;
                return problem;
            }
            problemDetails.ErrorDetails = new ProblemResultCollection();
            foreach (string memberName in validationException.ValidationResult?.MemberNames)
            {
                problemDetails.ErrorDetails.Add(new GenericValidationResult("invalid_argument", validationException.ValidationResult.ErrorMessage, new string[1]
                {
          memberName
                }));
                problemDetails.ErrorMessage = "One or more validation errors occurred.";
            }
            return problemDetails;
        }

        private static bool TryParseProblemDetail(string message, out ProblemDetails problem)
        {
            try
            {
                problem = JsonSerializer.Deserialize<ProblemDetails>(message);
                return !string.IsNullOrEmpty(problem.ErrorCode);
            }
            catch
            {
                problem = (ProblemDetails)null;
                return false;
            }
        }

        internal static ProblemDetails GetProblemDetails(
          this AggregateException aggregateException)
        {
            ProblemDetails problemDetails = new ProblemDetails()
            {
                Status = HttpStatusCode.BadRequest,
                TraceId = "",
                ErrorMessage = aggregateException.Message
            };
            ReadOnlyCollection<Exception> innerExceptions = aggregateException.InnerExceptions;
            List<ValidationException> source = innerExceptions != null ? innerExceptions.Where<Exception>((Func<Exception, bool>)(ex => ex is ValidationException)).OfType<ValidationException>().ToList<ValidationException>() : (List<ValidationException>)null;
            if (!source.Any<ValidationException>())
                return problemDetails;
            problemDetails.ErrorDetails = new ProblemResultCollection();
            foreach (ValidationException validationException in source)
                problemDetails.ErrorDetails.Add(new GenericValidationResult(validationException.ValidationResult));
            string errorMessage = problemDetails.ErrorMessage;
            if ((errorMessage != null ? (errorMessage.Any<char>() ? 1 : 0) : 0) != 0)
                problemDetails.ErrorMessage = "One or more validation errors occurred.";
            return problemDetails;
        }

        internal static ProblemDetails GetProblemDetails(
          this ApplicationException validationException)
        {
            ProblemDetails problemDetails = new ProblemDetails()
            {
                Status = HttpStatusCode.BadRequest,
                TraceId = "",
                ErrorMessage = validationException.Message
            };
            if (validationException.Message == "concurrency")
            {
                problemDetails.ErrorMessage = "An concurrent error occur when execute this request. Please retry again";
                problemDetails.Status = HttpStatusCode.Conflict;
            }
            return problemDetails;
        }

        internal static ProblemDetails GetProblemDetails(
            this InvalidOperationException validationException)
        {
            ProblemDetails problemDetails = new ProblemDetails()
            {
                Status = HttpStatusCode.BadRequest,
                TraceId = "",
                ErrorMessage = validationException.Message
            };
            if (validationException.Message == "concurrency")
            {
                problemDetails.ErrorMessage = "An concurrent error occur when execute this request. Please retry again";
                problemDetails.Status = HttpStatusCode.Conflict;
            }
            return problemDetails;
        }
        
        internal static ProblemDetails GetProblemDetails(this ApiException apiException)
        {
            if (apiException.StatusCode == HttpStatusCode.BadRequest)
            {
                try
                {
                    ProblemDetails problemDetails1;
                    if (apiException is ValidationApiException validationApiException3)
                    {
                        Refit.ProblemDetails content = validationApiException3.Content;
                        problemDetails1 = content != null ? new ProblemDetails()
                        {
                            Status = HttpStatusCode.BadRequest,
                            ErrorMessage = content.Detail
                        } : throw new ArgumentException("Cannot handle problem detail from ApiException");
                        if (!content.Errors.Any<KeyValuePair<string, string[]>>())
                            return problemDetails1;
                        problemDetails1.ErrorDetails = new ProblemResultCollection();
                        foreach ((string key5, string[] strArray5) in content.Errors)
                        {
                            //strArray5 = strArray5;
                            for (int local_9 = 0; local_9 < strArray5.Length; ++local_9)
                            {
                                string local_10 = strArray5[local_9];
                                problemDetails1.ErrorDetails.Add(new GenericValidationResult(local_10, new string[1]
                                {
                                    key5
                                }));
                            }
                        }
                    }
                    else
                    {
                        ProblemDetails problemDetails2 = JsonSerializer.Deserialize<ProblemDetails>(apiException.Content);
                        if (problemDetails2 == null)
                            problemDetails2 = new ProblemDetails(apiException.Message)
                            {
                                Status = apiException.StatusCode
                            };
                        problemDetails1 = problemDetails2;
                    }
                    return problemDetails1;
                }
                catch
                {
                    Trace.TraceError("Error when trying to convert ValidationProblemDetail from upstream. Content:" + apiException.Content);
                }
            }
            switch (apiException.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    return new ProblemDetails()
                    {
                        Status = HttpStatusCode.NotFound,
                        ErrorMessage = "Resource not found"
                    };
                case HttpStatusCode.TooManyRequests:
                    return new ProblemDetails()
                    {
                        Status = HttpStatusCode.TooManyRequests,
                        ErrorMessage = "You're reached the maximum request threshold. Please retry later."
                    };
                default:
                    ProblemDetails problemDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(apiException.Content, GlobalExceptionHandling.JsonSerializerOptions);
                    if (problemDetails != null && problemDetails.Status == apiException.StatusCode)
                        return problemDetails;
                    return new ProblemDetails()
                    {
                        Status = apiException.StatusCode,
                        ErrorMessage = apiException.Content
                    };
            }
        }

        internal static ProblemDetails GetProblemDetails(
        this BadRequestException badRequestException)
        {
            ProblemDetails problemDetails = new ProblemDetails()
            {
                Status = HttpStatusCode.BadRequest,
                TraceId = "",
                ErrorMessage = "One or more validation errors occurred."
            };
            problemDetails.ErrorDetails = new ProblemResultCollection();
            foreach (var error in badRequestException.Errors)
                problemDetails.ErrorDetails.Add(new GenericValidationResult(error.Code, error.Message, new string[1]
                   {
                        error.Code
                   }));
            return problemDetails;
        }

        internal static ProblemDetails GetProblemDetails(
          this NotFoundException notFoundException)
        {
            ProblemDetails problemDetails = new ProblemDetails()
            {
                Status = HttpStatusCode.NotFound,
                TraceId = "",
                ErrorMessage = "Not found."
            };
            problemDetails.ErrorDetails = new ProblemResultCollection();
            foreach (var error in notFoundException.Errors)
                problemDetails.ErrorDetails.Add(new GenericValidationResult(error.Code, error.Message, new string[1]
                   {
                        error.Code
                   }));
            return problemDetails;
        }

        public static string ToJson(this ProblemDetails problemDetails, bool enableIndented = false)
        {
            if (problemDetails == null)
                return string.Empty;
            Type type = problemDetails.GetType();
            //if (enableIndented)
            //return JsonSerializer.Serialize((object)problemDetails, type, Formatting.Indented, (JsonSerializerSettings)null);
            //return JsonSerializer.Serialize((object)problemDetails);
            //var xxx = JsonSerializer.Serialize((object)problemDetails, type, GlobalExceptionHandling.JsonSerializerOptions);

            //return JsonSerializer.Serialize((object)problemDetails, type, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true });
            return JsonSerializer.Serialize((object)problemDetails, type, GlobalExceptionHandling.JsonSerializerOptions);
            //switch (GlobalExceptionHandling.NamingPolicy)
            //{
            //    case null:
            //    case CamelCaseNamingStrategy _:
            //        return System.Text.Json.JsonSerializer.Serialize((object)problemDetails, type, GlobalExceptionHandling.JsonSerializerOptions);
            //    default:
            //        return JsonConvert.SerializeObject((object)problemDetails, type, Formatting.None, new JsonSerializerSettings()
            //        {
            //            ContractResolver = (IContractResolver)new DefaultContractResolver()
            //            {
            //                NamingStrategy = GlobalExceptionHandling.NamingPolicy
            //            }
            //        });
            //}
        }
    }
}
