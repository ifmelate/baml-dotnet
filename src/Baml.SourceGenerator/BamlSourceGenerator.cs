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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace Baml.SourceGenerator
{
    /// <summary>
    /// Source generator that generates C# client code from BAML schema files.
    /// </summary>
    [Generator]
    public class BamlSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Get BAML files with change tracking
            var bamlFiles = context.AdditionalTextsProvider
                .Where(static file => Path.GetExtension(file.Path).Equals(".baml", StringComparison.OrdinalIgnoreCase))
                .Select(static (file, cancellationToken) => new BamlFileInfo
                {
                    Path = file.Path,
                    Content = file.GetText(cancellationToken)?.ToString() ?? string.Empty
                });

            // Get configuration properties
            var configProperties = context.AnalyzerConfigOptionsProvider
                .Select(static (provider, _) =>
                {
                    var enabled = provider.GlobalOptions.TryGetValue("build_property.BamlGeneratorEnabled", out var enabledStr)
                        ? bool.Parse(enabledStr) : true;

                    string namespaceName;
                    if (provider.GlobalOptions.TryGetValue("build_property.BamlGeneratorNamespace", out var ns))
                    {
                        namespaceName = ns;
                    }
                    else
                    {
                        namespaceName = "Baml.Generated";
                    }

                    return new GeneratorConfig
                    {
                        Enabled = enabled,
                        Namespace = namespaceName
                    };
                });

            // Combine files and config
            var combined = bamlFiles.Collect()
                .Combine(configProperties);

            // Register source output
            context.RegisterSourceOutput(combined, Execute);
        }

        private static void Execute(SourceProductionContext context, (ImmutableArray<BamlFileInfo> files, GeneratorConfig config) input)
        {
            var (files, config) = input;

            if (!config.Enabled)
                return;

            try
            {
                if (files.IsEmpty)
                {
                    // No BAML files found, nothing to generate
                    return;
                }

                var parser = new BamlParser();

                // Process each BAML file independently
                foreach (var file in files)
                {
                    if (string.IsNullOrEmpty(file.Content))
                        continue;

                    try
                    {
                        var schema = parser.Parse(file.Content, file.Path);

                        // Skip empty schemas
                        if (!schema.Classes.Any() && !schema.Enums.Any() && !schema.Functions.Any())
                            continue;

                        // Generate code for this individual file
                        var generator = new BamlCodeGenerator(config.Namespace);
                        var generatedCode = generator.Generate(new List<BamlSchema> { schema });

                        // Create file prefix from the BAML filename
                        var fileName = Path.GetFileNameWithoutExtension(file.Path);
                        var filePrefix = $"{fileName}_baml";

                        foreach (var kvp in generatedCode)
                        {
                            var uniqueFileName = $"{filePrefix}_{kvp.Key}";
                            context.AddSource(uniqueFileName, SourceText.From(kvp.Value, Encoding.UTF8));
                        }
                    }
                    catch (Exception ex)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.ParsingError,
                            Location.None,
                            file.Path,
                            ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.GeneratorError,
                    Location.None,
                    ex.Message));
            }
        }

    }

    /// <summary>
    /// Information about a BAML file.
    /// </summary>
    internal class BamlFileInfo
    {
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configuration for the BAML source generator.
    /// </summary>
    internal class GeneratorConfig
    {
        public bool Enabled { get; set; } = true;
        public string Namespace { get; set; } = "Baml.Generated";
    }

    /// <summary>
    /// Diagnostic descriptors for BAML source generator.
    /// </summary>
    internal static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor ParsingError = new(
            "BAML001",
            "BAML parsing error",
            "Failed to parse BAML file '{0}': {1}",
            "BAML",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor GeneratorError = new(
            "BAML000",
            "BAML generator error",
            "Unexpected error in BAML source generator: {0}",
            "BAML",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}

