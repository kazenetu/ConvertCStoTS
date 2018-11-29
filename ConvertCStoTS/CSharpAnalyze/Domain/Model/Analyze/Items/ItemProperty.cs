using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConvertCStoTS.CSharpAnalyze.Domain.Model.Analyze.Items
{
  /// <summary>
  /// アイテム：プロパティ
  /// </summary>
  public class ItemProperty : AbstractItem, ISemanticModelAnalyzeItem
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
    public List<string> Comments { get; } = new List<string>();

    /// <summary>
    /// プロパティの型リスト
    /// </summary>
    public List<Expression> PropertyTypes { get; } = new List<Expression>();

    /// <summary>
    /// デフォルト設定リスト
    /// </summary>
    public List<Expression> DefaultValues { get; } = new List<Expression>();

    /// <summary>
    /// アクセサリスト
    /// </summary>
    public List<string> AccessorList { get; } = new List<string>();

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="node">対象Node</param>
    /// <param name="target">対象ソースのsemanticModel</param>
    public ItemProperty(PropertyDeclarationSyntax node, SemanticModel semanticModel)
    {
      ItemType = ItemTypes.Property;

      var declaredSymbol = semanticModel.GetDeclaredSymbol(node);

      // 名前設定
      Name = declaredSymbol.Name;

      // 識別子リスト設定
      Modifiers.AddRange(node.Modifiers.Select(item => item.Text));

      // コメント設定
      var targerComments = node.GetLeadingTrivia().ToString().Split(Environment.NewLine).
                            Select(item => item.TrimStart().Replace(Environment.NewLine, string.Empty, StringComparison.CurrentCulture)).
                            Where(item => !string.IsNullOrEmpty(item));
      Comments.AddRange(targerComments);

      // プロパティの型設定
      var parts = ((IPropertySymbol)declaredSymbol).Type.ToDisplayParts(SymbolDisplayFormat.MinimallyQualifiedFormat);
      foreach(var part in parts)
      {
        var name = $"{part}";
        var type = GetSymbolTypeName(part.Symbol);
        if (part.Kind == SymbolDisplayPartKind.ClassName)
        {
          // 外部ファイル参照イベント発行
          RaiseOtherFileReferenced(node,part.Symbol);
        }

        PropertyTypes.Add(new Expression(name, type));
      }

      // アクセサ設定
      AccessorList.AddRange(node.AccessorList.Accessors.Select(accessor => $"{accessor.Keyword}"));

      // デフォルト設定
      if (node.Initializer == null)
      {
        return;
      }
      var propertyInitializer = semanticModel.GetOperation(node.Initializer.Value);
      if (propertyInitializer.ConstantValue.HasValue)
      {
        // 値型
        var targetValue = propertyInitializer.ConstantValue;
        DefaultValues.Add(new Expression(targetValue.Value.ToString(), targetValue.Value.GetType().Name));
      }
      else
      {
        // クラスインスタンスなど
        var tokens = propertyInitializer.Syntax.DescendantTokens();
        foreach(var token in tokens)
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

    /// <summary>
    /// 文字列取得
    /// </summary>
    /// <param name="index">前スペース数</param>
    /// <returns>文字列</returns>
    public string ToString(int index = 0)
    {
      return string.Empty;
    }
  }
}
