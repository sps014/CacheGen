using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Text;
using System.Xml.Linq;
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
            sb.AppendLine("namespace CacheGen;");
            sb.AppendLine("public static class Gen{");
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
        private string GenerateCachedVariant(LocalFunctionStatementSyntax function)
        {
            return GetFunctionCachedDefinition(function);

        }
        private string GetFunctionCachedDefinition(LocalFunctionStatementSyntax method)
        {
            using StringWriter stream = new StringWriter();
            using IndentedTextWriter writer = new IndentedTextWriter(stream, "    ");

            string methodName = $"{method.Identifier.ValueText}Cached";
            string cacheName = $"{method.Identifier.ValueText}_cache";
            string paramName = string.Join(",", method.ParameterList.Parameters.Select(x => x.Identifier));
            string returnType = method.ReturnType.GetText().ToString();
            int size = GetSizeOfCache(method);
            string paramList = method.ParameterList.ToFullString();
            bool isVoid = method.ReturnType.GetText().ToString().Equals("void");

            writer.Indent++;
            writer.WriteLine($"    private static LruCache<int,int> {cacheName} = new({size});");
            writer.WriteLine($"public static {returnType} {methodName}{paramList}");
            writer.WriteLine("{");
            writer.Indent++;

            //start body here
            writer.WriteLine($"var contains = {cacheName}.Refer({paramName});");
           
            //if statement on top
            writer.WriteLine("if(contains)");
            writer.Indent++;
            if (isVoid)
                writer.WriteLine("return;");
            else
                writer.WriteLine($"return {cacheName}.Get({paramName});");
            writer.Indent--;
            //end of if




            foreach (var statement in method.Body.Statements)
            {
                if(isVoid)
                {
                    writer.WriteLine(statement.GetText());
                    continue;
                }

                //var returns =statement.DescendantNodes().OfType<ReturnStatementSyntax>().ToList();
                if(statement is ReturnStatementSyntax @return)
                {
                    var expr = @return.Expression;
                    writer.WriteLine($"return {cacheName}.AddResult({paramName},{expr});"); ;
                }
                else
                    GenerateBody(statement.ChildNodesAndTokens(),ref paramName,ref cacheName,writer);
                
            }


            writer.Indent--;
            //end of body

            writer.WriteLine("}");
            var str=stream.ToString();
            return str;
        }
        private void GenerateBody(ChildSyntaxList children,ref string paramName,ref string cacheName,IndentedTextWriter writer)
        {
            foreach(var c in children)
            {
                if(c.IsNode)
                {
                    if(c.AsNode() is ReturnStatementSyntax @return)
                    {
                        var expr=@return.Expression;
                        writer.WriteLine($"\treturn {cacheName}.AddResult({paramName},{expr});\r\n");
                    }
                    else
                    GenerateBody(c.AsNode().ChildNodesAndTokens(),ref paramName,ref cacheName,writer);
                }
                else
                {

                    writer.Write(c.ToFullString());
                }
                
            }
        }

        private int GetSizeOfCache(SyntaxNode function)
        {
            var attrbutes = (function as LocalFunctionStatementSyntax).AttributeLists;
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

 
        private IEnumerable<LocalFunctionStatementSyntax> ProcessFunction(List<SyntaxNode> nodes)
        {
            return nodes.OfType<LocalFunctionStatementSyntax>()
               .Where(f => f.AttributeLists.Count(a => a.GetText().ToString().Contains("[LruCache")) > 0);
        }



        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
        }
    }
}