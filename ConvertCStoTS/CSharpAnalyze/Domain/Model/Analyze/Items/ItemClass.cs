using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpAnalyze.Domain.Model.Analyze.Items
{
  /// <summary>
  /// アイテム：クラス
  /// </summary>
  public class ItemClass : AbstractItem, ISemanticModelAnalyzeItem
  {
    #region 基本インターフェース実装：プロパティ

    /// <summary>
    /// 子メンバ
    /// </summary>
    public List<ISemanticModelAnalyzeItem> Members { get; } = new List<ISemanticModelAnalyzeItem>();

    /// <summary>
    /// アイテム種別
    /// </summary>
    public ItemTypes ItemType { get; }

    /// <summary>
    /// 名前
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 修飾子リスト
    /// </summary>
    public List<string> Modifiers { get; } = new List<string>();

    /// <summary>
    /// コメント
    /// </summary>
    public List<string> Comments { get; } = new List<string>();

    #endregion

    /// <summary>
    /// スーパークラスリスト
    /// </summary>
    public List<Expression> SuperClass { get; } = new List<Expression>();

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="node">対象Node</param>
    /// <param name="target">対象ソースのsemanticModel</param>
    public ItemClass(ClassDeclarationSyntax node, SemanticModel semanticModel)
    {
      ItemType = ItemTypes.Class;

      var declaredClass = semanticModel.GetDeclaredSymbol(node);

      // 名前設定
      Name = declaredClass.Name;

      // 識別子リスト設定
      Modifiers.AddRange(node.Modifiers.Select(item => item.Text));

      // コメント設定
      var targerComments = node.GetLeadingTrivia().ToString().Split(Environment.NewLine).
                            Select(item => item.TrimStart().Replace(Environment.NewLine, string.Empty, StringComparison.CurrentCulture)).
                            Where(item => !string.IsNullOrEmpty(item));
      Comments.AddRange(targerComments);

      // スーパークラス設定
      if (node.BaseList != null)
      {
        var displayParts = declaredClass.BaseType.ToDisplayParts(SymbolDisplayFormat.MinimallyQualifiedFormat);
        foreach (var part in displayParts)
        {
          var name = $"{part}";
          var type = GetSymbolTypeName(part.Symbol);
          if (part.Symbol != null)
          {
            type = part.Symbol.GetType().Name;
            if (!string.IsNullOrEmpty(part.Symbol.ContainingNamespace.Name))
            {
              name = $"{part.Symbol}".Replace($"{part.Symbol.ContainingNamespace}.", string.Empty, StringComparison.CurrentCulture);
            }

            if (part.Kind == SymbolDisplayPartKind.ClassName)
            {
              // 外部ファイル参照イベント発行
              RaiseOtherFileReferenced(node, part.Symbol);
            }
          }

          SuperClass.Add(new Expression(name, type));
        }
      }
    }

    #region 基本インターフェース実装：メソッド

    /// <summary>
    /// 文字列取得
    /// </summary>
    /// <param name="index">前スペース数</param>
    /// <returns>文字列</returns>
    public string ToString(int index = 0)
    {
      var result = new StringBuilder();
      var indexSpace = string.Concat(Enumerable.Repeat("  ", index));

      foreach (var comment in Comments)
      {
        result.Append(indexSpace);
        result.AppendLine($"{comment}");
      }

      foreach (var modifier in Modifiers)
      {
        result.Append(indexSpace);
        result.Append($"{modifier} ");
      }
      result.Append($"class {Name}");
      if (SuperClass.Any())
      {
        result.Append(" : ");
        SuperClass.ForEach(item => result.Append(item.Name));
      }
      result.AppendLine();
      result.Append(indexSpace);
      result.AppendLine("{");

      Members.ForEach(member => result.AppendLine(member.ToString(index + 1)));

      result.Append(indexSpace);
      result.AppendLine("}");

      return result.ToString();
    }

    #endregion

  }
}
