using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

                var methods=ProcessLocalMethod(nodes);
                methods.AddRange(ProcessFunction(nodes));

                foreach(var method in methods)
                {
                    GenerateCachedVariant(method);
                }
            }
        }
        private void GenerateCachedVariant(SyntaxNode function)
        {
            int size=GetSizeOfCache(function);
        }

        private int GetSizeOfCache(SyntaxNode function)
        {
            int size = LruSize;
            if (function is LocalDeclarationStatementSyntax localfunction)
            {
                var attrbutes = localfunction.AttributeLists;
                size = GetAttributeSize(attrbutes);
            }
            else if (function is MethodDeclarationSyntax method)
            {
                var attrbutes = method.AttributeLists;
                size = GetAttributeSize(attrbutes);
            }
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

        private List<SyntaxNode> ProcessLocalMethod(List<SyntaxNode> nodes)
        {
            IEnumerable<SyntaxNode> v= nodes.OfType<LocalFunctionStatementSyntax>()
                .Where(f=>f.AttributeLists.Count(a=>a.GetText().ToString().Contains("[LruCache"))>0);
            return v.ToList();

        }
        private IEnumerable<SyntaxNode> ProcessFunction(List<SyntaxNode> nodes)
        {
            IEnumerable<SyntaxNode> lf = nodes.OfType<MethodDeclarationSyntax>()
                .Where(f => f.AttributeLists.Count(a => a.GetText().ToString().Contains("[LruCache")) > 0);
            return lf;
        }



        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
        }
    }
}