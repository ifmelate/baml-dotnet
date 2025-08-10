/*
Copyright (c) 2025 ifmelate

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

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

