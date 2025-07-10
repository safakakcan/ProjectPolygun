using System;
using Microsoft.CSharp;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using System.Text;

namespace Netpack
{
    public static class Generator
    {
        private const string GeneratedClassName = "Serializer";

        public static void Generate(params Type[] types)
        {
            var codeNamespace = new CodeNamespace("Netpack");
            codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Text"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Runtime.InteropServices"));
            var generatedClass = new CodeTypeDeclaration(GeneratedClassName);
            codeNamespace.Types.Add(generatedClass);
            generatedClass.TypeAttributes = TypeAttributes.Public;
            generatedClass.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            generatedClass.IsClass = true;

            foreach (var type in types)
            {
                var fields = type.GetFields();
                var typeName = type.Name;

                {   // Serialization
                    CodeMemberMethod serializationMethod = new CodeMemberMethod()
                    {
                        Name = "Serialize",
                        Attributes = MemberAttributes.Public | MemberAttributes.Static
                    };
                    serializationMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference($"this {type.Name}"), typeName));
                    serializationMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(Span<byte>)), "Data"));
                    serializationMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "Index") { Direction = FieldDirection.Ref });

                    serializationMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(ushort), "ArraySize"));
                    serializationMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(int), "ByteCount"));

                    GenerateFieldSerializer(serializationMethod.Statements, fields, typeName);

                    generatedClass.Members.Add(serializationMethod);
                }

                {   // Deserialization
                    CodeMemberMethod deserializationMethod = new CodeMemberMethod()
                    {
                        Name = "Deserialize",
                        Attributes = MemberAttributes.Public | MemberAttributes.Static
                    };
                    deserializationMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference($"this Span<byte>"), "Data"));
                    deserializationMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "Index") { Direction = FieldDirection.Ref });
                    deserializationMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(type), typeName) { Direction = FieldDirection.Out });

                    deserializationMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(ushort), "ArraySize"));
                    deserializationMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(int), "ByteCount"));
                    GenerateFieldDeserializer(deserializationMethod.Statements, fields, type, typeName);

                    generatedClass.Members.Add(deserializationMethod);
                }
            }

            CodeDomProvider provider = new CSharpCodeProvider();
            var generatedFile = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Generateds.cs");
            var stringBuilder = new StringBuilder();

            using (StringWriter writer = new StringWriter(stringBuilder))
            {
                provider.GenerateCodeFromNamespace(codeNamespace, writer, new CodeGeneratorOptions());
                var newCode = stringBuilder.ToString().Replace($"public class {GeneratedClassName}", $"public static class {GeneratedClassName}");
                File.WriteAllText(generatedFile, newCode);
            }
        }

        private static void GenerateFieldSerializer(CodeStatementCollection statements, FieldInfo[] fields, string parentName)
        {
            foreach (var field in fields)
            {
                var fieldName = string.IsNullOrEmpty(parentName) ? field.Name : $"{parentName}.{field.Name}";

                if (field.FieldType == typeof(string))
                {
                    WriteArraySize(statements, fieldName);

                    var byteCountVarExpression = new CodeAssignStatement(new CodeVariableReferenceExpression("ByteCount"), new CodeBinaryOperatorExpression(new CodeSnippetExpression("sizeof(char)"), CodeBinaryOperatorType.Multiply, new CodeSnippetExpression($"{fieldName}.Length")));
                    var targetSpan = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("Data"), "Slice"), new CodeVariableReferenceExpression("Index"), new CodeVariableReferenceExpression("ByteCount"));
                    statements.Add(byteCountVarExpression);
                    statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeSnippetExpression("Encoding.UTF8"), "GetBytes", new CodeVariableReferenceExpression(fieldName), targetSpan)));
                    statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("Index"), new CodeSnippetExpression($"Index + ByteCount")));
                    continue;
                }

                if (field.FieldType.IsPrimitive)
                {
                    var methodRefExpression = new CodeMethodReferenceExpression(new CodeSnippetExpression("MemoryMarshal"), "Write", new CodeTypeReference(field.FieldType));
                    var spanSliceExpression = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("Data"), "Slice");
                    statements.Add(new CodeCommentStatement($"Write value of {fieldName}"));
                    statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(methodRefExpression, new CodeMethodInvokeExpression(spanSliceExpression, new CodeVariableReferenceExpression("Index")), new CodeSnippetExpression($"ref {fieldName}"))));
                    statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("Index"), new CodeSnippetExpression($"Index + sizeof({field.FieldType.Name})")));
                    continue;
                }

                if (field.FieldType.IsArray)
                {
                    var elementType = field.FieldType.GetElementType();

                    var iterationIndexName = string.Empty;
                    var iterationDepth = fieldName.Split('.').Length - 1;
                    for (int i = 0; i < iterationDepth; i++)
                    {
                        iterationIndexName += "i";
                    }

                    WriteArraySize(statements, fieldName);

                    var iterationStatements = new CodeStatementCollection();

                    if (elementType == typeof(string))
                    {
                        WriteArraySize(iterationStatements, $"{fieldName}[{iterationIndexName}]");

                        var byteCountVarExpression = new CodeAssignStatement(new CodeVariableReferenceExpression("ByteCount"), new CodeBinaryOperatorExpression(new CodeSnippetExpression("sizeof(char)"), CodeBinaryOperatorType.Multiply, new CodeSnippetExpression($"{fieldName}[{iterationIndexName}].Length")));
                        var targetSpan = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("Data"), "Slice"), new CodeVariableReferenceExpression("Index"), new CodeVariableReferenceExpression("ByteCount"));
                        iterationStatements.Add(byteCountVarExpression);
                        iterationStatements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeSnippetExpression("Encoding.UTF8"), "GetBytes", new CodeSnippetExpression($"{fieldName}[{iterationIndexName}]"), targetSpan)));
                        iterationStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("Index"), new CodeSnippetExpression($"Index + ByteCount")));
                    }
                    else if (elementType.IsPrimitive)
                    {
                        var methodRefExpression = new CodeMethodReferenceExpression(new CodeSnippetExpression("MemoryMarshal"), "Write", new CodeTypeReference(elementType));
                        var spanSliceExpression = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("Data"), "Slice");

                        iterationStatements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(methodRefExpression, new CodeMethodInvokeExpression(spanSliceExpression, new CodeVariableReferenceExpression("Index")), new CodeSnippetExpression($"ref {fieldName}[{iterationIndexName}]"))));
                        iterationStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("Index"), new CodeSnippetExpression($"Index + sizeof({elementType.Name})")));
                    }
                    else
                    {
                        GenerateFieldSerializer(iterationStatements, elementType.GetFields(), $"{fieldName}[{iterationIndexName}]");
                    }

                    var iterationStatementArray = new CodeStatement[iterationStatements.Count];
                    for (int i = 0; i < iterationStatements.Count; i++)
                    {
                        iterationStatementArray[i] = iterationStatements[i];
                    }

                    statements.Add(new CodeCommentStatement($"Iterate {fieldName} array"));
                    statements.Add(new CodeIterationStatement(new CodeVariableDeclarationStatement(typeof(int), iterationIndexName, new CodeSnippetExpression("0")), new CodeSnippetExpression($"{iterationIndexName} < {fieldName}.Length"), new CodeSnippetStatement($"{iterationIndexName}++"), iterationStatementArray));
                    continue;
                }
                
                {
                    GenerateFieldSerializer(statements, field.FieldType.GetFields(), fieldName);
                    continue;
                }
            }
        }

        private static void GenerateFieldDeserializer(CodeStatementCollection statements, FieldInfo[] fields, Type parentType, string parentName)
        {
            statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(parentName), new CodeSnippetExpression($"new()")));

            foreach (var field in fields)
            {
                var fieldName = string.IsNullOrEmpty(parentName) ? field.Name : $"{parentName}.{field.Name}";

                if (field.FieldType == typeof(string))
                {
                    ReadArraySize(statements, fieldName);

                    var byteCountVarExpression = new CodeAssignStatement(new CodeVariableReferenceExpression("ByteCount"), new CodeBinaryOperatorExpression(new CodeSnippetExpression("sizeof(char)"), CodeBinaryOperatorType.Multiply, new CodeVariableReferenceExpression("ArraySize")));
                    var targetSpan = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("Data"), "Slice"), new CodeVariableReferenceExpression("Index"), new CodeVariableReferenceExpression("ByteCount"));
                    statements.Add(byteCountVarExpression);
                    statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(fieldName), new CodeMethodInvokeExpression(new CodeSnippetExpression("Encoding.UTF8"), "GetString", targetSpan)));
                    statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("Index"), new CodeSnippetExpression($"Index + ByteCount")));
                    continue;
                }

                if (field.FieldType.IsPrimitive)
                {
                    var methodRefExpression = new CodeMethodReferenceExpression(new CodeSnippetExpression("MemoryMarshal"), "Read", new CodeTypeReference(field.FieldType));
                    var spanSliceExpression = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("Data"), "Slice");
                    statements.Add(new CodeCommentStatement($"Read value of {fieldName}"));
                    statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(fieldName), new CodeMethodInvokeExpression(methodRefExpression, new CodeMethodInvokeExpression(spanSliceExpression, new CodeVariableReferenceExpression("Index"), new CodeSnippetExpression($"sizeof({field.FieldType.Name})")))));
                    statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("Index"), new CodeSnippetExpression($"Index + sizeof({field.FieldType.Name})")));
                    continue;
                }

                if (field.FieldType.IsArray)
                {
                    var elementType = field.FieldType.GetElementType();

                    var iterationIndexName = string.Empty;
                    var iterationDepth = fieldName.Split('.').Length - 1;
                    for (int i = 0; i < iterationDepth; i++)
                    {
                        iterationIndexName += "i";
                    }

                    ReadArraySize(statements, fieldName);
                    statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(fieldName), new CodeSnippetExpression($"new {elementType.Name}[ArraySize]")));

                    var iterationStatements = new CodeStatementCollection();

                    if (elementType == typeof(string))
                    {
                        ReadArraySize(iterationStatements, fieldName);

                        var byteCountVarExpression = new CodeAssignStatement(new CodeVariableReferenceExpression("ByteCount"), new CodeBinaryOperatorExpression(new CodeSnippetExpression("sizeof(char)"), CodeBinaryOperatorType.Multiply, new CodeVariableReferenceExpression("ArraySize")));
                        var targetSpan = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("Data"), "Slice"), new CodeVariableReferenceExpression("Index"), new CodeVariableReferenceExpression("ByteCount"));
                        iterationStatements.Add(byteCountVarExpression);
                        iterationStatements.Add(new CodeAssignStatement(new CodeSnippetExpression($"{fieldName}[{iterationIndexName}]"), new CodeMethodInvokeExpression(new CodeSnippetExpression("Encoding.UTF8"), "GetString", targetSpan)));
                        iterationStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("Index"), new CodeSnippetExpression($"Index + ByteCount")));
                    }
                    else if (elementType.IsPrimitive)
                    {
                        var methodRefExpression = new CodeMethodReferenceExpression(new CodeSnippetExpression("MemoryMarshal"), "Read", new CodeTypeReference(elementType));
                        var spanSliceExpression = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("Data"), "Slice");

                        iterationStatements.Add(new CodeAssignStatement(new CodeIndexerExpression(new CodeVariableReferenceExpression(fieldName), new CodeSnippetExpression(iterationIndexName)), new CodeMethodInvokeExpression(methodRefExpression, new CodeMethodInvokeExpression(spanSliceExpression, new CodeVariableReferenceExpression("Index")))));
                        iterationStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("Index"), new CodeSnippetExpression($"Index + sizeof({elementType.Name})")));
                    }
                    else
                    {
                        GenerateFieldDeserializer(iterationStatements, elementType.GetFields(), elementType, $"{fieldName}[{iterationIndexName}]");
                    }

                    var iterationStatementArray = new CodeStatement[iterationStatements.Count];
                    for (int i = 0; i < iterationStatements.Count; i++)
                    {
                        iterationStatementArray[i] = iterationStatements[i];
                    }

                    statements.Add(new CodeCommentStatement($"Iterate {fieldName} array"));
                    statements.Add(new CodeIterationStatement(new CodeVariableDeclarationStatement(typeof(int), iterationIndexName, new CodeSnippetExpression("0")), new CodeSnippetExpression($"{iterationIndexName} < {fieldName}.Length"), new CodeSnippetStatement($"{iterationIndexName}++"), iterationStatementArray));
                    continue;
                }

                {
                    GenerateFieldDeserializer(statements, field.FieldType.GetFields(), field.FieldType, fieldName);
                    continue;
                }
            }
        }

        private static void WriteArraySize(CodeStatementCollection statements, string fieldName)
        {
            var sizeExpression = new CodeMethodReferenceExpression(new CodeSnippetExpression("MemoryMarshal"), "Write", new CodeTypeReference(typeof(ushort)));
            var sizeSliceExpression = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("Data"), "Slice");
            statements.Add(new CodeCommentStatement($"Write array size of {fieldName}"));
            statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("ArraySize"), new CodeSnippetExpression($"(ushort){fieldName}.Length")));
            statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(sizeExpression, new CodeMethodInvokeExpression(sizeSliceExpression, new CodeVariableReferenceExpression("Index")), new CodeSnippetExpression($"ref ArraySize"))));
            statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("Index"), new CodeSnippetExpression($"Index + sizeof(ushort)")));
        }

        private static void ReadArraySize(CodeStatementCollection statements, string fieldName)
        {
            var sizeExpression = new CodeMethodReferenceExpression(new CodeSnippetExpression("MemoryMarshal"), "Read", new CodeTypeReference(typeof(ushort)));
            var sizeSliceExpression = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("Data"), "Slice");
            var readSizeExpression = new CodeMethodInvokeExpression(sizeExpression, new CodeMethodInvokeExpression(sizeSliceExpression, new CodeVariableReferenceExpression("Index"), new CodeSnippetExpression($"sizeof(ushort)")));
            statements.Add(new CodeCommentStatement($"Write array size of {fieldName}"));
            statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("ArraySize"), readSizeExpression));
            statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("Index"), new CodeSnippetExpression($"Index + sizeof(ushort)")));
        }
    }
}