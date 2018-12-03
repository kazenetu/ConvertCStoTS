using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpAnalyze.Domain.Model.Analyze.Items
{
  /// <summary>
  /// アイテム：フィールド
  /// </summary>
  public class ItemField : AbstractItem, ISemanticModelAnalyzeItem
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
    /// フィールドの型リスト
    /// </summary>
    public List<Expression> FieldTypes { get; } = new List<Expression>();

    /// <summary>
    /// デフォルト設定リスト
    /// </summary>
    public List<Expression> DefaultValues { get; } = new List<Expression>();

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="node">対象Node</param>
    /// <param name="target">対象ソースのsemanticModel</param>
    public ItemField(FieldDeclarationSyntax node, SemanticModel semanticModel)
    {
      ItemType = ItemTypes.Field;

      var declaredSymbol = semanticModel.GetDeclaredSymbol(node.Declaration.Variables.First());

      // 名前設定
      Name = declaredSymbol.Name;

      // 識別子リスト設定
      Modifiers.AddRange(node.Modifiers.Select(item => item.Text));

      // コメント設定
      var targerComments = node.GetLeadingTrivia().ToString().Split(Environment.NewLine).
                            Select(item => item.TrimStart().Replace(Environment.NewLine, string.Empty, StringComparison.CurrentCulture)).
                            Where(item => !string.IsNullOrEmpty(item));
      Comments.AddRange(targerComments);

      // フィールドの型設定
      var parts = ((IFieldSymbol)declaredSymbol).Type.ToDisplayParts(SymbolDisplayFormat.MinimallyQualifiedFormat);
      foreach (var part in parts)
      {
        var name = $"{part}";
        var type = GetSymbolTypeName(part.Symbol);
        if (part.Kind == SymbolDisplayPartKind.ClassName)
        {
          // 外部ファイル参照イベント発行
          RaiseOtherFileReferenced(node, part.Symbol);
        }

        FieldTypes.Add(new Expression(name, type));
      }

      // デフォルト設定
      var constantValue = node.Declaration.Variables.FirstOrDefault();
      if (constantValue?.Initializer == null)
      {
        return;
      }
      var initializer = semanticModel.GetOperation(constantValue.Initializer.Value);
      if (initializer.ConstantValue.HasValue)
      {
        // 値型
        var targetValue = initializer.ConstantValue;
        var name = targetValue.Value.ToString();
        if (targetValue.Value is string)
        {
          name = $"\"{name}\"";
        }
        DefaultValues.Add(new Expression(name, targetValue.Value.GetType().Name));
      }
      else
      {
        // クラスインスタンスなど
        var tokens = initializer.Syntax.DescendantTokens();
        foreach (var token in tokens)
        {
          var symbol = semanticModel.GetSymbolInfo(token.Parent);
          DefaultValues.Add(new Expression(token.Value.ToString(), GetSymbolTypeName(symbol.Symbol)));

          if (symbol.Symbol != null && symbol.Symbol is INamedTypeSymbol)
          {
            // 外部ファイル参照イベント発行
            RaiseOtherFileReferenced(node, symbol.Symbol);
          }
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

      // プロパティの型
      FieldTypes.ForEach(type => result.Append(type.Name));

      // プロパティ名
      result.Append($" {Name}");

      if (DefaultValues.Any())
      {
        // デフォルト値
        result.Append(" = ");

        foreach (var value in DefaultValues)
        {
          result.Append($"{value.Name}");
          if (value.Name == "new")
          {
            result.Append(" ");
          }
        }
        result.Append(";");
      }

      return result.ToString();
    }

    #endregion

  }
}
