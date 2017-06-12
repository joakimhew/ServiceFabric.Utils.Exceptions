using Microsoft.ServiceFabric.Services.Communication.Client;
using ServiceFabric.Utils.Logging;
using ServiceFabric.Utils.Shared;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using IExceptionHandler = Microsoft.ServiceFabric.Services.Communication.Client.IExceptionHandler;

namespace ServiceFabric.Utils.Exceptions
{
    public class ApiHttpExceptionHandler : ExceptionHandler, IExceptionHandler
    {
        private readonly IErrorHandler _errorHandler;

        public ApiHttpExceptionHandler()
        {
            
        }

        public ApiHttpExceptionHandler(IErrorHandler errorHandler)
        {
            _errorHandler = errorHandler;
        }

        public override async Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
        {
            var owinContext = context.Request.GetOwinContext();
            var errorId = await _errorHandler.LogErrorAsync(owinContext, HttpStatusCode.InternalServerError, context.Exception);

            context.Result = new ApiHttpActionResult(
                context.Request,
                HttpStatusCode.InternalServerError,
                "Internal server error",
                errorId == Guid.Empty ? null : errorId.ToString());
        }

        public override bool ShouldHandle(ExceptionHandlerContext context)
        {
            return true;
        }

        public bool TryHandleException(ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings,
            out ExceptionHandlingResult result)
        {
            if (exceptionInformation.Exception is TimeoutException)
            {
                result =
                    new ExceptionHandlingRetryResult(
                        exceptionInformation.Exception,
                        false,
                        retrySettings,
                        retrySettings.DefaultMaxRetryCount);

                return true;
            }

            if (exceptionInformation.Exception is SocketException)
            {
                result =
                    new ExceptionHandlingRetryResult(
                        exceptionInformation.Exception,
                        false,
                        retrySettings,
                        retrySettings.DefaultMaxRetryCount);

                return true;
            }

            if (exceptionInformation.Exception is ProtocolViolationException)
            {
                result = new ExceptionHandlingThrowResult();
                return true;
            }

            var we = exceptionInformation.Exception.InnerException as WebException ??
                              exceptionInformation.Exception.InnerException as WebException;

            if (we != null)
            {
                var errorResponse = we.Response as HttpWebResponse;

                if (we.Status == WebExceptionStatus.ProtocolError)
                {
                    if (errorResponse != null && errorResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        result =
                            new ExceptionHandlingRetryResult(
                                exceptionInformation.Exception,
                                false,
                                retrySettings,
                                retrySettings.DefaultMaxRetryCount);

                        return true;
                    }

                    if (errorResponse != null && errorResponse.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        result =
                            new ExceptionHandlingRetryResult(
                                exceptionInformation.Exception,
                                true,
                                retrySettings,
                                retrySettings.DefaultMaxRetryCount);

                        return true;
                    }
                }

                if (we.Status == WebExceptionStatus.Timeout ||
                    we.Status == WebExceptionStatus.RequestCanceled ||
                    we.Status == WebExceptionStatus.ConnectionClosed ||
                    we.Status == WebExceptionStatus.ConnectFailure)
                {
                    result =
                        new ExceptionHandlingRetryResult(
                            exceptionInformation.Exception,
                            false,
                            retrySettings,
                            retrySettings.DefaultMaxRetryCount);

                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}