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

namespace Baml.Runtime
{
    /// <summary>
    /// Base exception for BAML-related errors.
    /// </summary>
    public class BamlException : Exception
    {
        public BamlException() : base() { }
        public BamlException(string message) : base(message) { }
        public BamlException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when a BAML function call fails.
    /// </summary>
    public class BamlFunctionException : BamlException
    {
        public string FunctionName { get; }
        public object Parameters { get; }

        public BamlFunctionException(string functionName, object parameters) : base($"BAML function '{functionName}' failed.")
        {
            FunctionName = functionName;
            Parameters = parameters;
        }

        public BamlFunctionException(string functionName, object parameters, string message) : base(message)
        {
            FunctionName = functionName;
            Parameters = parameters;
        }

        public BamlFunctionException(string functionName, object parameters, string message, Exception innerException) : base(message, innerException)
        {
            FunctionName = functionName;
            Parameters = parameters;
        }
    }

    /// <summary>
    /// Exception thrown when BAML configuration is invalid.
    /// </summary>
    public class BamlConfigurationException : BamlException
    {
        public BamlConfigurationException() : base() { }
        public BamlConfigurationException(string message) : base(message) { }
        public BamlConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when BAML schema parsing fails.
    /// </summary>
    public class BamlSchemaException : BamlException
    {
        public string SchemaFile { get; }

        public BamlSchemaException(string schemaFile) : base($"Failed to parse BAML schema file '{schemaFile}'.")
        {
            SchemaFile = schemaFile;
        }

        public BamlSchemaException(string schemaFile, string message) : base(message)
        {
            SchemaFile = schemaFile;
        }

        public BamlSchemaException(string schemaFile, string message, Exception innerException) : base(message, innerException)
        {
            SchemaFile = schemaFile;
        }
    }
}

