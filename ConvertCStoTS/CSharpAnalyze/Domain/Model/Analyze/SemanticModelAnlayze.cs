using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpAnalyze.Domain.Model.Analyze
{
  /// <summary>
  /// セマンティックモデル解析クラス(実装中)
  /// </summary>
  public class SemanticModelAnlayze
  {
    public SemanticModelAnlayze(SemanticModel target)
    {
      var rootNode = target.SyntaxTree.GetRoot().ChildNodes().Where(syntax=> syntax.IsKind(SyntaxKind.NamespaceDeclaration)).First();
      foreach (var item in (rootNode as NamespaceDeclarationSyntax).Members)
      {
        GetMember(item,target);
      }
    }

    private void GetMember(SyntaxNode node, SemanticModel target)
    {
      var nodeType = node.Kind();
      switch (nodeType)
      {
        case SyntaxKind.ClassDeclaration:
          Console.WriteLine(target.GetDeclaredSymbol(node));
          foreach(var childSyntax in node.ChildNodes())
          {
            GetMember(childSyntax, target);
          }
          break;
        default:
          Console.WriteLine(node);
          break;
      }

    }
  }
}
