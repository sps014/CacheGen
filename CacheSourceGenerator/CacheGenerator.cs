using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Drawing;

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
            
            ProcessTrees(trees);
        }
        private void ProcessTrees(IEnumerable<SyntaxTree> trees)
        {
            foreach(var t in trees)
            {
                var nodes= t.GetRoot().DescendantNodes().ToList();

                var methods=ProcessFunction(nodes);

                foreach(var method in methods)
                {
                    GenerateCachedVariant(method);
                }
            }
        }
        private void GenerateCachedVariant(LocalFunctionStatementSyntax function)
        {
            int size=GetSizeOfCache(function);   
            var newDef=GetFunctionCachedDefinition(function);
        }
        private string GetFunctionCachedDefinition(LocalFunctionStatementSyntax method)
        {
            string result = "";
            //add modifiers
            result += method.Modifiers.ToFullString();
            // add return type
            result += $"{method.ReturnType.GetText()}";
            //add function name 
            result += $"{method.Identifier.ValueText}Cached";
            //add brackets and parameters
            result += $"{method.ParameterList.ToFullString()}{{\r\n";

            string paramName = string.Join(",", method.ParameterList.Parameters.Select(x => x.Identifier));
            //add cacherefer
            result += $"\tvar contains = cache.Refer({paramName});\r\n";
            result += $"\tif(contains)";

            bool isVoid=false;

            if(method.ReturnType.GetText().ToString().Contains("void"))
            {
                isVoid=true;
                result += "return;\r\n";
            }
            else
            {
                result += $"return cache.Get({paramName});\r\n";
            }

            foreach (var st in method.Body.Statements)
            {
                var stmt=st.GetText().ToString();
                
                if (stmt.Contains("return") && !isVoid)
                {
                    var r = stmt.Replace("return ", $"return cache.AddResult({paramName},").TrimEnd(new char[] {'\r','\n',';'});
                    result += r+");\r\n";
                }
                else
                    result += stmt;
            }
            result += "}";
            Console.WriteLine(result);
            return result;
        }

        private int GetSizeOfCache(SyntaxNode function)
        {
            int size = LruSize;
            var attrbutes = (function as LocalFunctionStatementSyntax).AttributeLists;
            size = GetAttributeSize(attrbutes);
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