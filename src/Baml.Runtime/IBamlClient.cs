using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Baml.Runtime
{
    /// <summary>
    /// Interface for BAML client implementations.
    /// This interface will be implemented by generated client code.
    /// </summary>
    public interface IBamlClient
    {
        /// <summary>
        /// Gets the underlying BAML runtime instance.
        /// </summary>
        IBamlRuntime Runtime { get; }
    }

    /// <summary>
    /// Interface for the BAML runtime that handles communication with the BAML execution engine.
    /// </summary>
    public interface IBamlRuntime
    {
        /// <summary>
        /// Calls a BAML function asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The expected return type of the function.</typeparam>
        /// <param name="functionName">The name of the BAML function to call.</param>
        /// <param name="parameters">The parameters to pass to the function.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The result of the function call.</returns>
        Task<TResult> CallFunctionAsync<TResult>(string functionName, object parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calls a BAML function with streaming response.
        /// </summary>
        /// <typeparam name="TResult">The expected return type of the function.</typeparam>
        /// <param name="functionName">The name of the BAML function to call.</param>
        /// <param name="parameters">The parameters to pass to the function.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>An async enumerable of partial results.</returns>
        IAsyncEnumerable<TResult> StreamFunctionAsync<TResult>(string functionName, object parameters, CancellationToken cancellationToken = default);
    }
}

