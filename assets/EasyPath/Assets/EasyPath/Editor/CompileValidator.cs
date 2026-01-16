using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace EasyPath.Editor
{
    /// <summary>
    /// Compile validation script for Codebuff CLI integration.
    /// Validates C# compilation and reports errors in a parseable format.
    /// </summary>
    public static class CompileValidator
    {
        private static List<CompilerMessage> _errors = new List<CompilerMessage>();
        private static List<CompilerMessage> _warnings = new List<CompilerMessage>();

        /// <summary>
        /// Validate compilation from CLI.
        /// Called via: -executeMethod EasyPath.Editor.CompileValidator.ValidateFromCLI
        /// </summary>
        public static void ValidateFromCLI()
        {
            Debug.Log("[CompileValidator] Starting compilation validation...");
            
            _errors.Clear();
            _warnings.Clear();
            
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            CompilationPipeline.RequestScriptCompilation();
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            foreach (var message in messages)
            {
                if (message.type == CompilerMessageType.Error)
                {
                    _errors.Add(message);
                    Debug.LogError($"[ERROR] {message.file}({message.line},{message.column}): {message.message}");
                }
                else if (message.type == CompilerMessageType.Warning)
                {
                    _warnings.Add(message);
                    Debug.LogWarning($"[WARN] {message.file}({message.line},{message.column}): {message.message}");
                }
            }
        }

        private static void OnCompilationFinished(object obj)
        {
            
            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            
            if (_errors.Count > 0)
            {
                Debug.LogError($"[CompileValidator] Compilation FAILED with {_errors.Count} errors");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log($"[CompileValidator] Compilation SUCCEEDED ({_warnings.Count} warnings)");
                EditorApplication.Exit(0);
            }
        }

        [MenuItem("Tools/EasyPath/Check Compilation")]
        public static void CheckCompilation()
        {
            AssetDatabase.Refresh();
            
            var assemblies = CompilationPipeline.GetAssemblies();
            Debug.Log($"[CompileValidator] Found {assemblies.Length} assemblies");
            
            bool hasErrors = EditorUtility.scriptCompilationFailed;
            
            if (hasErrors)
            {
                Debug.LogError("[CompileValidator] Compilation has errors!");
            }
            else
            {
                Debug.Log("[CompileValidator] Compilation successful - no errors");
            }
        }

        public static void GenerateReport(string outputPath)
        {
            using (var writer = new StreamWriter(outputPath))
            {
                writer.WriteLine("# Compilation Report");
                writer.WriteLine($"Generated: {DateTime.Now}");
                writer.WriteLine();
                
                writer.WriteLine($"## Summary");
                writer.WriteLine($"- Errors: {_errors.Count}");
                writer.WriteLine($"- Warnings: {_warnings.Count}");
                writer.WriteLine();
                
                if (_errors.Count > 0)
                {
                    writer.WriteLine("## Errors");
                    foreach (var error in _errors)
                    {
                        writer.WriteLine($"- `{error.file}:{error.line}` - {error.message}");
                    }
                    writer.WriteLine();
                }
                
                if (_warnings.Count > 0)
                {
                    writer.WriteLine("## Warnings");
                    foreach (var warning in _warnings)
                    {
                        writer.WriteLine($"- `{warning.file}:{warning.line}` - {warning.message}");
                    }
                }
            }
            
            Debug.Log($"[CompileValidator] Report saved to: {outputPath}");
        }
    }
}
