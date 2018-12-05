using CSharpAnalyze.Domain.Model.Analyze.Items;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpAnalyze.Domain.Model.Analyze
{
  /// <summary>
  /// ISemanticModelAnalyzeItemインスタンス作成クラス
  /// </summary>
  public static class ItemFactory
  {

    /// <summary>
    /// エントリメソッド
    /// </summary>
    /// <param name="node">対象Node</param>
    /// <param name="target">対象ソースのsemanticModel</param>
    /// <returns>ISemanticModelAnalyzeItemインスタンス</returns>
    public static ISemanticModelAnalyzeItem Create(SyntaxNode node, SemanticModel semanticModel)
    {
      ISemanticModelAnalyzeItem result = null;

      // nodeの種類によって取得メソッドを実行
      switch (node)
      {
        case ClassDeclarationSyntax classDeclarationSyntax:
          result = Create(classDeclarationSyntax, semanticModel);
          break;
        case PropertyDeclarationSyntax propertyDeclarationSyntax:
          result = Create(propertyDeclarationSyntax, semanticModel);
          break;
        case FieldDeclarationSyntax fieldDeclarationSyntax:
          result = Create(fieldDeclarationSyntax, semanticModel);
          break;
        case MethodDeclarationSyntax methodDeclarationSyntax:
          result = Create(methodDeclarationSyntax, semanticModel);
          break;
      }

      return result;
    }

    #region クラスアイテム

    /// <summary>
    /// クラスアイテム作成:class
    /// </summary>
    /// <param name="node">対象Node</param>
    /// <param name="target">対象ソースのsemanticModel</param>
    /// <returns>ISemanticModelAnalyzeItemインスタンス</returns>
    private static ISemanticModelAnalyzeItem Create(ClassDeclarationSyntax node, SemanticModel semanticModel)
    {
      return new ItemClass(node, semanticModel);
    }

    /// <summary>
    /// クラスアイテム作成:property
    /// </summary>
    /// <param name="node">対象Node</param>
    /// <param name="target">対象ソースのsemanticModel</param>
    /// <returns>ISemanticModelAnalyzeItemインスタンス</returns>
    private static ISemanticModelAnalyzeItem Create(PropertyDeclarationSyntax node, SemanticModel semanticModel)
    {
      return new ItemProperty(node, semanticModel);
    }

    /// <summary>
    /// クラスアイテム作成:field
    /// </summary>
    /// <param name="node">対象Node</param>
    /// <param name="target">対象ソースのsemanticModel</param>
    /// <returns>ISemanticModelAnalyzeItemインスタンス</returns>
    private static ISemanticModelAnalyzeItem Create(FieldDeclarationSyntax node, SemanticModel semanticModel)
    {
      return new ItemField(node, semanticModel);
    }

    /// <summary>
    /// クラスアイテム作成:Method
    /// </summary>
    /// <param name="node">対象Node</param>
    /// <param name="target">対象ソースのsemanticModel</param>
    /// <returns>ISemanticModelAnalyzeItemインスタンス</returns>
    private static ISemanticModelAnalyzeItem Create(MethodDeclarationSyntax node, SemanticModel semanticModel)
    {
      return new ItemMethod(node, semanticModel);
    }

    #endregion
  }
}
