using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace CacheSourceGenerator
{
    public class FunctionInfo
    {
        public SyntaxToken Identifier { get; set; }
        public ParameterListSyntax ParameterList { get; set; }
        public TypeSyntax ReturnType { get; set; }
        public BlockSyntax Body {get;set;}
        public SyntaxList<AttributeListSyntax> AttributeLists { get; set;}
    }
}
