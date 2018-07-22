using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace ConvertCStoTS
{
  public class CodeAnalyzer
  {
    private List<TextLine> textList = null;

    public string Analyze(string targetCode)
    {
      var tree = CSharpSyntaxTree.ParseText(targetCode) as CSharpSyntaxTree;

      // 構文エラーチェック
      foreach (var item in tree.GetDiagnostics())
      {
        return string.Empty;
      }

      var root = tree.GetRoot();

      // ソース変換
      var result = new StringBuilder();
      foreach (CSharpSyntaxNode item in root.DescendantNodes())
      {
        switch (item.Kind())
        {
          case SyntaxKind.NamespaceDeclaration:
            var nsItem = item as NamespaceDeclarationSyntax;
            Console.Write("namespace ");
            Console.Write(nsItem.Name.ToString());
            Console.WriteLine();

            foreach (var childItem in nsItem.Members)
            {
              if (childItem is ClassDeclarationSyntax ci)
              {
                result.Append(GetItemText(ci));
              }

            }
            break;
        }
      }
      return result.ToString();
    }

    private string GetItemText(ClassDeclarationSyntax item,int index = 0)
    {
      var result = new StringBuilder();

      var lineIndex = string.Empty;
      var spaceCount = index;
      while (spaceCount > 0)
      {
        lineIndex += " ";
        spaceCount--;
      }

      var className = item.Identifier.ValueText;
      if (item.BaseList != null)
      {
        className += $" extends {item.BaseList.Types.ToString()}";
      }
      var exportKeyword = "export ";
      if(item.Parent is ClassDeclarationSyntax)
      {
        exportKeyword = string.Empty;
      }
      result.AppendLine($"{lineIndex}{exportKeyword}class {className} {item.OpenBraceToken.ValueText}");

      // 子要素を設定
      foreach(var childItem in item.Members)
      {
        if (childItem is ClassDeclarationSyntax ci)
        {
          result.Append(GetItemText(ci, index + 2));
        }
        
      }

      result.AppendLine($"{lineIndex}{item.CloseBraceToken.ValueText}");
      return result.ToString();
    }

  }
}
