/*
 * CODE OWNERS: Tom Puckett>
 * OBJECTIVE: Utility methods supporting retry semantics for caller provided delegate functions
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MapCommonLib
{
    public static class StaticUtil
    {
        public delegate void RetryOperation<TException>() where TException : Exception;
        public delegate TResult RetryOperationWithReturn<TException, TResult>() where TException : Exception;
        public delegate Task RetryAsyncOperation<TException>() where TException : Exception;
        public delegate Task<TResult> RetryAsyncOperationWithReturn<TException, TResult>() where TException : Exception;

        /// <summary>
        /// Attempts to call an async operation, with retry if a specified exception type is caught
        /// </summary>
        /// <typeparam name="TException"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operation"></param>
        /// <param name="maxAttempts"></param>
        /// <param name="baseIntervalMs"></param>
        /// <returns></returns>
        public static async Task<TResult> DoRetryAsyncOperationWithReturn<TException, TResult>(RetryAsyncOperationWithReturn<TException, TResult> operation, int maxAttempts, int baseIntervalMs, bool logException = false)
            where TException : Exception
        {
            int retryInterval = 0;
            int attemptNo = 0;

            while (true)
            {
                try
                {
                    TResult result = await operation();
                    return result;
                }
                catch (TException ex)
                {
                    attemptNo++;
                    if (attemptNo == maxAttempts)
                    {
                        if (logException)
                        {
                            Log.Error(ex, $"Exception of type {ex.GetType().Name} caught in StaticUtil.DoRetryAsyncOperationWithReturn:{Environment.NewLine}");
                        }
                        throw;
                    }

                    retryInterval += attemptNo;
                    Thread.Sleep(retryInterval * baseIntervalMs);
                }
            }
        }

        /// <summary>
        /// Attempts to call an async operation, with retry if a specified exception type is caught
        /// </summary>
        /// <typeparam name="TException"></typeparam>
        /// <param name="operation"></param>
        /// <param name="maxAttempts"></param>
        /// <param name="baseIntervalMs"></param>
        /// <returns></returns>
        public static async Task DoRetryAsyncOperation<TException>(RetryAsyncOperation<TException> operation, int maxAttempts, int baseIntervalMs, bool logException = false)
            where TException : Exception
        {
            int retryInterval = 0;
            int attemptNo = 0;

            while (true)
            {
                try
                {
                    await operation();
                    return;
                }
                catch (TException ex)
                {
                    attemptNo++;
                    if (attemptNo == maxAttempts)
                    {
                        if (logException)
                        {
                            Log.Error(ex, $"Exception of type {ex.GetType().Name} caught in StaticUtil.DoRetryAsyncOperation:{Environment.NewLine}");
                        }
                        throw;
                    }

                    retryInterval += attemptNo;
                    Thread.Sleep(retryInterval * baseIntervalMs);
                }
            }
        }

        /// <summary>
        /// Attempts to call an operation, with retry if a specified exception type is caught
        /// </summary>
        /// <typeparam name="TException">Type of an exception to cause retry of the specified operation</typeparam>
        /// <typeparam name="TResult">Type of the return value of the specified operation</typeparam>
        /// <param name="operation">The operation to try/retry</param>
        /// <param name="maxAttempts"></param>
        /// <param name="baseIntervalMs"></param>
        /// <returns></returns>
        public static TResult DoRetryOperationWithReturn<TException, TResult>(RetryOperationWithReturn<TException, TResult> operation, int maxAttempts, int baseIntervalMs, bool logException = false)
            where TException : Exception
        {
            int retryInterval = 0;
            int attemptNo = 0;

            while (true)
            {
                try
                {
                    TResult result = operation();
                    return result;
                }
                catch (TException ex)
                {
                    attemptNo++;
                    if (attemptNo == maxAttempts)
                    {
                        if (logException)
                        {
                            Log.Error(ex, $"Exception of type {ex.GetType().Name} caught in StaticUtil.DoRetryOperationWithReturn:{Environment.NewLine}");
                        }
                        throw;
                    }

                    retryInterval += attemptNo;
                    Thread.Sleep(retryInterval * baseIntervalMs);
                }
            }
        }

        /// <summary>
        /// Try to perform a retriable operation until it succeeds.
        /// Time before retry increases after each attempt. Retry intervals form a triangular sequence.
        /// </summary>
        /// <typeparam name="TException">
        /// Exception type to monitor. When operation throws an exception of this type, it will be retried.
        /// </typeparam>
        /// <param name="operation">
        /// Retriable operation to try until success. Only retried when TException is thrown.
        /// </param>
        /// <param name="maxAttempts">Times to try the operation before giving up</param>
        /// <param name="baseIntervalMs">Time to wait after initial attempt</param>
        /// <summary>
        internal static void ApplyRetryOperation<TException>(
            RetryOperation<TException> operation, int maxAttempts, int baseIntervalMs, bool logException = false)
            where TException : Exception
        {
            int retryInterval = 0;
            int attemptNo = 0;

            while (true)
            {
                try
                {
                    operation();
                    break;
                }
                catch (TException ex)
                {
                    attemptNo++;
                    if (attemptNo == maxAttempts)
                    {
                        if (logException)
                        {
                            Log.Error(ex, $"Exception of type {ex.GetType().Name} caught in StaticUtil.ApplyRetryOperation:{Environment.NewLine}");
                        }
                        throw;
                    }

                    retryInterval += attemptNo;
                    Thread.Sleep(retryInterval * baseIntervalMs);
                }
            }
        }

    }
}
