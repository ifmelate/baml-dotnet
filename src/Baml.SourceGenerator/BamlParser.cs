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
using System.Linq;
using System.Text.RegularExpressions;

namespace Baml.SourceGenerator
{
    /// <summary>
    /// Parser for BAML schema files.
    /// </summary>
    public class BamlParser
    {
        private static readonly Regex FunctionRegex = new Regex(
            @"function\s+(\w+)\s*\(([^)]*)\)\s*->\s*([^{]+)\s*\{([^}]*)\}",
            RegexOptions.Multiline | RegexOptions.Singleline);

        private static readonly Regex ClassRegex = new Regex(
            @"class\s+(\w+)\s*\{([^}]*)\}",
            RegexOptions.Multiline | RegexOptions.Singleline);

        private static readonly Regex EnumRegex = new Regex(
            @"enum\s+(\w+)\s*\{([^}]*)\}",
            RegexOptions.Multiline | RegexOptions.Singleline);

        private static readonly Regex ClientRegex = new Regex(
            @"client\s+""([^""]+)""",
            RegexOptions.Multiline);

        private static readonly Regex PropertyRegex = new Regex(
            @"^\s*(\w+)\s+(?:(\w+)|""([^""]+)"")(?:\s*@description\(([^)]+)\))?\s*$",
            RegexOptions.Multiline);

        public BamlSchema Parse(string content, string filePath)
        {
            var schema = new BamlSchema { FilePath = filePath };

            // Parse classes
            var classMatches = ClassRegex.Matches(content);
            foreach (Match match in classMatches)
            {
                var className = match.Groups[1].Value;
                var classBody = match.Groups[2].Value;
                var bamlClass = ParseClass(className, classBody);
                schema.Classes.Add(bamlClass);
            }

            // Parse enums
            var enumMatches = EnumRegex.Matches(content);
            foreach (Match match in enumMatches)
            {
                var enumName = match.Groups[1].Value;
                var enumBody = match.Groups[2].Value;
                var bamlEnum = ParseEnum(enumName, enumBody);
                schema.Enums.Add(bamlEnum);
            }

            // Parse functions
            var functionMatches = FunctionRegex.Matches(content);
            foreach (Match match in functionMatches)
            {
                var functionName = match.Groups[1].Value;
                var parameters = match.Groups[2].Value;
                var returnType = match.Groups[3].Value.Trim();
                var functionBody = match.Groups[4].Value;

                var bamlFunction = ParseFunction(functionName, parameters, returnType, functionBody);
                schema.Functions.Add(bamlFunction);
            }

            return schema;
        }

        private BamlClass ParseClass(string name, string body)
        {
            var bamlClass = new BamlClass { Name = name };

            // Split body into lines and process each line
            var lines = body.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                var propertyMatch = PropertyRegex.Match(trimmedLine);
                if (propertyMatch.Success)
                {
                    var propertyName = propertyMatch.Groups[1].Value;
                    var propertyType = propertyMatch.Groups[2].Success ? propertyMatch.Groups[2].Value : "string"; // literal values are strings
                    var literalValue = propertyMatch.Groups[3].Success ? propertyMatch.Groups[3].Value : null;
                    var description = propertyMatch.Groups[4].Success ? propertyMatch.Groups[4].Value.Trim('"') : null;

                    bamlClass.Properties.Add(new BamlProperty
                    {
                        Name = propertyName,
                        Type = propertyType ?? "string",
                        Description = description,
                        LiteralValue = literalValue
                    });
                }
            }

            return bamlClass;
        }

        private BamlEnum ParseEnum(string name, string body)
        {
            var bamlEnum = new BamlEnum { Name = name };

            var values = body.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim().Trim('"'))
                .Where(v => !string.IsNullOrEmpty(v))
                .ToList();

            bamlEnum.Values.AddRange(values);
            return bamlEnum;
        }

        private BamlFunction ParseFunction(string name, string parameters, string returnType, string body)
        {
            var bamlFunction = new BamlFunction
            {
                Name = name,
                ReturnType = returnType
            };

            // Parse parameters
            if (!string.IsNullOrWhiteSpace(parameters))
            {
                var paramPairs = parameters.Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p));

                foreach (var param in paramPairs)
                {
                    var parts = param.Split(':');
                    if (parts.Length == 2)
                    {
                        bamlFunction.Parameters.Add(new BamlParameter
                        {
                            Name = parts[0].Trim(),
                            Type = parts[1].Trim()
                        });
                    }
                }
            }

            // Parse client from function body
            var clientMatch = ClientRegex.Match(body);
            if (clientMatch.Success)
            {
                bamlFunction.Client = clientMatch.Groups[1].Value;
            }

            return bamlFunction;
        }
    }

    /// <summary>
    /// Represents a parsed BAML schema.
    /// </summary>
    public class BamlSchema
    {
        public string FilePath { get; set; } = string.Empty;
        public List<BamlClass> Classes { get; set; } = new List<BamlClass>();
        public List<BamlEnum> Enums { get; set; } = new List<BamlEnum>();
        public List<BamlFunction> Functions { get; set; } = new List<BamlFunction>();
    }

    /// <summary>
    /// Represents a BAML class definition.
    /// </summary>
    public class BamlClass
    {
        public string Name { get; set; } = string.Empty;
        public List<BamlProperty> Properties { get; set; } = new List<BamlProperty>();
    }

    /// <summary>
    /// Represents a property in a BAML class.
    /// </summary>
    public class BamlProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LiteralValue { get; set; }
    }

    /// <summary>
    /// Represents a BAML enum definition.
    /// </summary>
    public class BamlEnum
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Values { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a BAML function definition.
    /// </summary>
    public class BamlFunction
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public string? Client { get; set; }
        public List<BamlParameter> Parameters { get; set; } = new List<BamlParameter>();
    }

    /// <summary>
    /// Represents a parameter in a BAML function.
    /// </summary>
    public class BamlParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}

