using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConvertCStoTS.CSharpAnalyze.Domain.Model.Analyze.Items
{
  /// <summary>
  /// アイテム：クラス
  /// </summary>
  public class ItemClass: ISemanticModelAnalyzeItem
  {
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
    public List<string> Comments  { get; } = new List<string>();

    /// <summary>
    /// スーパークラス
    /// </summary>
    public string SuperClass { get; } = string.Empty;

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
      Modifiers.AddRange(node.Modifiers.Select(item=>item.Text));

      // コメント設定
      var targerComments = node.GetLeadingTrivia().ToString().Split(Environment.NewLine).
                            Select(item => item.TrimStart().Replace(Environment.NewLine, string.Empty, StringComparison.CurrentCulture)).
                            Where(item => !string.IsNullOrEmpty(item));
      Comments.AddRange(targerComments);

      // スーパークラス設定
      if (node.BaseList != null)
      {
        SuperClass = node.BaseList.Types[0].ToString();
      }
    }

    /// <summary>
    /// 文字列取得
    /// </summary>
    /// <returns>文字列</returns>
    public override string ToString()
    {
      var result = new StringBuilder();

      foreach (var comment in Comments)
      {
        result.AppendLine($"{comment}");
      }

      foreach (var modifier in Modifiers)
      {
        result.Append($"{modifier} ");
      }
      result.Append($"class {Name}");
      if (!string.IsNullOrEmpty(SuperClass))
      {
        result.Append($" : {SuperClass}");
      }
      result.AppendLine();
      result.AppendLine("{");
      result.AppendLine("}");

      return result.ToString();
    }
  }
}
