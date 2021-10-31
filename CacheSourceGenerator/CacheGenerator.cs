using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.CodeDom.Compiler;
using System.IO;

namespace CacheSourceGenerator
{
    [Generator]
    public class MySourceGenerator : ISourceGenerator
    {
        public const int LruSize = 10000;
        public void Execute(GeneratorExecutionContext context)
        {

            //get all tree with our attribute
            var trees = context.Compilation.SyntaxTrees
                .Where(t => t.GetRoot().DescendantNodes().OfType<AttributeSyntax>()
                .Where(a=>a.Name.ToString().Contains("LruCache")).Count()>0);
            StringBuilder sb= new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Runtime.CompilerServices;");
            sb.AppendLine("namespace LibCache;");
            sb.AppendLine("public static partial class Gen\r\n{");
            ProcessTrees(trees,sb);
            sb.AppendLine("}");
            var str = sb.ToString();
            Console.WriteLine(str);
            context.AddSource("cacheGen.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
        private void ProcessTrees(IEnumerable<SyntaxTree> trees,StringBuilder sb)
        {
            foreach(var t in trees)
            {
                var nodes= t.GetRoot().DescendantNodes().ToList();

                var methods=ProcessFunction(nodes);

                foreach(var method in methods)
                {
                    sb.AppendLine(GenerateCachedVariant(method));
                }
            }
        }
        private string GenerateCachedVariant(FunctionInfo function)
        {
            return GetFunctionCachedDefinition(function);

        }
        private string GetFunctionCachedDefinition(FunctionInfo method)
        {
            if (method.ParameterList.Parameters.Count == 0)
                return string.Empty;

            using StringWriter stream = new StringWriter();
            using IndentedTextWriter writer = new IndentedTextWriter(stream, "    ");

            string methodName = $"{method.Identifier.ValueText}Cached";
            string cacheName = $"{method.Identifier.ValueText}_cache";
            string paramName = string.Join(",", method.ParameterList.Parameters.Select(x => x.Identifier));
            string returnType = method.ReturnType.GetText().ToString();
            int size = GetSizeOfCache(method);
            string paramList = method.ParameterList.ToFullString();
            bool isVoid = method.ReturnType.GetText().ToString().Contains("void");


            string argTypes = string.Join(",", method.ParameterList.Parameters.Select(x => x.Type.ToString()));
            bool isMoreThanOne = method.ParameterList.Parameters.Count>1;
            argTypes = isMoreThanOne ? $"({argTypes})":argTypes;
            var tupledParam = isMoreThanOne ? $"({paramName})" : paramName;

            var dictReturnType = isVoid ? "object" : returnType;

            writer.Indent++;
            writer.WriteLine($"    private static LruCache<{argTypes},{dictReturnType}> {cacheName} = new({size});");
            writer.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            writer.Write($"public static {returnType} {methodName}{paramList}");

            writer.WriteLine("    {");
            writer.Indent++;

            //start body here
            writer.WriteLine($"var contains = {cacheName}.Refer({tupledParam});");
           
            //if statement on top
            writer.WriteLine("if(contains)");
            writer.Indent++;

            if (isVoid)
                writer.WriteLine("return;");
            else
                writer.WriteLine($"return {cacheName}.Get({tupledParam});");

            writer.Indent-=3;
            //end of if




            foreach (var statement in method.Body.Statements)
            {
                if(isVoid)
                {
                    writer.WriteLine(statement.GetText());
                    continue;
                }

                if(statement is ReturnStatementSyntax @return)
                {
                    GenerateReturn(writer,ref cacheName,ref paramName, @return, ref isMoreThanOne);
                }
                else
                    GenerateBody(statement.ChildNodesAndTokens(),ref paramName,ref cacheName,writer,ref isMoreThanOne);
                
            }

            //end of body
           
            writer.WriteLine("    }\r\n");
            var str=stream.ToString();
            return str;
        }

        private static void GenerateReturn(IndentedTextWriter writer, ref string cacheName,ref string paramName, ReturnStatementSyntax @return, ref bool moreThanOneArg)
        {
            var expr = @return.Expression;
            var space=string.Join("",@return.ToFullString().TakeWhile(x=>char.IsWhiteSpace(x)));

            var tupledParam = moreThanOneArg ? $"({paramName})" : paramName;
                writer.WriteLine($"{space}return {cacheName}.AddResult({tupledParam},{expr});");
        }

        private void GenerateBody(ChildSyntaxList children,ref string paramName,ref string cacheName,IndentedTextWriter writer, ref bool moreThanOneArg)
        {
            foreach(var c in children)
            {
                if(c.IsNode)
                {
                    if(c.AsNode() is ReturnStatementSyntax @return)
                        GenerateReturn(writer, ref cacheName, ref paramName, @return, ref moreThanOneArg);
                    else
                        GenerateBody(c.AsNode().ChildNodesAndTokens(),ref paramName,ref cacheName,writer, ref moreThanOneArg);
                }
                else
                    writer.Write(c.ToFullString());
                
            }
        }

        private int GetSizeOfCache(FunctionInfo function)
        {
            var attrbutes = function.AttributeLists;
            int size = GetAttributeSize(attrbutes);
            return size;
        }
        private int GetAttributeSize(SyntaxList<AttributeListSyntax> attrbutes)
        {
            foreach(var attribute in attrbutes)
            {
                var text = attribute.GetText().ToString();
                if (text.Contains("[LruCache]"))
                    return LruSize;
                else
                {
                    var m = Regex.Match(text, @"\[LruCache\((\d+)\)]");
                    if (m.Success)
                        return int.Parse(m.Groups[1].Value);
                    m = Regex.Match(text, @"\[LruCache\(maxsize:(\d+)\)]");
                    if (m.Success)
                        return int.Parse(m.Groups[1].Value);
                }
            }

            return LruSize;
        }

 
        private IEnumerable<FunctionInfo> ProcessFunction(List<SyntaxNode> nodes)
        {
            var allfunc= nodes.OfType<MethodDeclarationSyntax>()
               .Where(f => f.AttributeLists.Count(a => a.GetText().ToString().Contains("[LruCache")) > 0)
               .Select(x=>
               {
                   return new FunctionInfo
                   {
                       Body = x.Body,
                       Identifier = x.Identifier,
                       ParameterList = x.ParameterList,
                       ReturnType=x.ReturnType,
                       AttributeLists=x.AttributeLists
                   };
               }
               );

            var local= nodes.OfType<LocalFunctionStatementSyntax>()
               .Where(f => f.AttributeLists.Count(a => a.GetText().ToString().Contains("[LruCache")) > 0)
               .Select(x =>
               {
                   return new FunctionInfo
                   {
                       Body = x.Body,
                       Identifier = x.Identifier,
                       ParameterList = x.ParameterList,
                       ReturnType = x.ReturnType,
                       AttributeLists=x.AttributeLists
                   };
               }
               );

            return allfunc.Union(local);
        }



        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
        }
    }
}