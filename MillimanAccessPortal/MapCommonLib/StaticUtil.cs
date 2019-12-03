using System;
using System.Threading;

namespace MapCommonLib
{
    public static class StaticUtil
    {
        /// <summary>
        /// Represents a retriable operation that throws an exception when it should be retried
        /// </summary>
        /// <param name="opArg"></param>
        internal delegate void RetryOperation<TException, TArg>(TArg opArg) where TException : Exception;
        internal delegate void RetryOperation<TException, TArg1, TArg2>(TArg1 opArg1, TArg2 opArg2) where TException : Exception;

        /// <summary>
        /// Try to perform a retriable operation until it succeeds.
        /// Time before retry increases after each attempt. Retry intervals form a triangular sequence.
        /// </summary>
        /// <typeparam name="TException">
        /// Exception type to monitor. When operation throws an exception of this type, it will be retried.
        /// </typeparam>
        /// <typeparam name="TArg"></typeparam>
        /// <param name="operation">
        /// Retriable operation to try until success. Only retried when TException is thrown.
        /// </param>
        /// <param name="attempts">Times to try the operation before giving up</param>
        /// <param name="baseIntervalMs">Time to wait after initial attempt</param>
        /// <param name="opArg">Passed to operation delegate</param>
        /// <summary>
        internal static void ApplyRetryOperation<TException, TArg>(
            RetryOperation<TException, TArg> operation, int attempts, int baseIntervalMs, TArg opArg)
            where TException : Exception
        {
            int retryInterval = 0;
            int attemptNo = 0;

            while (true)
            {
                try
                {
                    operation(opArg);
                    break;
                }
                catch (TException)
                {
                    attemptNo += 1;

                    int attemptsLeft = attempts - attemptNo;
                    if (attemptsLeft == 0)
                    {
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
        /// <typeparam name="TArg1"></typeparam>
        /// <typeparam name="TArg2"></typeparam>
        /// <param name="operation">
        /// Retriable operation to try until success. Only retried when TException is thrown.
        /// </param>
        /// <param name="attempts">Times to try the operation before giving up</param>
        /// <param name="baseIntervalMs">Time to wait after initial attempt</param>
        /// <param name="opArg1">Passed to operation delegate</param>
        /// <param name="opArg2">Passed to operation delegate</param>
        /// <summary>
        internal static void ApplyRetryOperation<TException, TArg1, Tagr2>(
            RetryOperation<TException, TArg1, Tagr2> operation, int attempts, int baseIntervalMs, TArg1 opArg1, Tagr2 opArg2)
            where TException : Exception
        {
            int retryInterval = 0;
            int attemptNo = 0;

            while (true)
            {
                try
                {
                    operation(opArg1, opArg2);
                    break;
                }
                catch (TException)
                {
                    attemptNo += 1;

                    int attemptsLeft = attempts - attemptNo;
                    if (attemptsLeft == 0)
                    {
                        throw;
                    }

                    retryInterval += attemptNo;
                    Thread.Sleep(retryInterval * baseIntervalMs);
                }
            }
        }
    }
}
