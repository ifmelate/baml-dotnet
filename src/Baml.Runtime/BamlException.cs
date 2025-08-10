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

