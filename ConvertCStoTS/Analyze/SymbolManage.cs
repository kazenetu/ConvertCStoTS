using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ConvertCStoTS.Analyze
{
  /// <summary>
  /// メンバ型管理クラス
  /// </summary>
  /// <remarks>
  /// シングルトン
  /// </remarks>
  public class SymbolManage
  {
    #region 列挙型

    /// <summary>
    /// 変数タイプ
    /// </summary>
    public enum SymbolType
    {
      Fail,
      InstanceMember,
      Literal,
      Local,
      ClassMember,
      PredefinedType,
    }

    #endregion

    #region クラスフィールド
    /// <summary>
    /// インスタンス
    /// </summary>
    private static readonly SymbolManage Instance = new SymbolManage();
    #endregion

    #region インスタンスフィールド
    /// <summary>
    /// 編集クラスインスタンス
    /// </summary>
    private CSharpCompilation Compilation = null;

    /// <summary>
    /// セマンティックモデル
    /// </summary>
    private SemanticModel Model = null;
    #endregion

    #region コンストラクタ

    /// <summary>
    /// コンストラクタ
    /// </summary>
    private SymbolManage()
    {
    }

    #endregion

    #region クラスメソッド

    /// <summary>
    /// 編集クラスインスタンス設定
    /// </summary>
    /// <param name="csInfos">C#ファイル情報リスト</param>
    public static void SetCompilation(List<CSFileInfo> csInfos)
    {
      var mscorlib = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);
      var treeList = csInfos.Select(info => CSharpSyntaxTree.ParseText(info.SourceCode)).ToList();
      Instance.Compilation = CSharpCompilation.Create("", treeList, new[] { mscorlib });
    }

    /// <summary>
    /// 構文ツリーを返す
    /// </summary>
    /// <param name="targetSource">取得したいソースコード</param>
    /// <returns>構文ツリー</returns>
    public static SyntaxTree GetSyntaxTree(string targetSource)
    {
      return Instance.Compilation?.SyntaxTrees.Where(tree => tree.GetText().ToString() == targetSource).FirstOrDefault();
    }

    /// <summary>
    /// セマンティックモデル設定
    /// </summary>
    /// <param name="info">対象C#ファイルのソース</param>
    public static void SetSemanticModel(CSFileInfo info)
    {
      Instance.Model = Instance.Compilation.GetSemanticModel(GetSyntaxTree(info.SourceCode));
    }

    /// <summary>
    /// 変数タイプを返す
    /// </summary>
    /// <param name="condition">対象</param>
    /// <returns>変数タイプ</returns>
    public static SymbolType GetSymbolType(SyntaxNode condition)
    {
      var result = SymbolType.Fail;

      // Modelが存在しない場合は終了
      if(Instance.Model == null)
      {
        return result;
      }

      var contidionResult = Instance.GetSymbol(condition);
      var identifierNameSyntaxResult = Instance.GetSymbol(Instance.GetIdentifierNameSyntax(condition));

      if(contidionResult < identifierNameSyntaxResult)
      {
        result = identifierNameSyntaxResult;
      }
      else
      {
        result = contidionResult;
      }

      return result;
    }

    #endregion

    #region インスタンスメソッド

    /// <summary>
    /// 変数タイプを取得
    /// </summary>
    /// <param name="condition">対象</param>
    /// <returns>変数タイプ</returns>
    private SymbolType GetSymbol(SyntaxNode targetCondition)
    {
      var result = SymbolType.Fail;

      // 型を取得
      var symbolInfo = Instance.Model.GetSymbolInfo(targetCondition);

      // 型の判定
      if (symbolInfo.Symbol == null)
      {
        result = SymbolType.Literal;
      }

      if (symbolInfo.Symbol is ILocalSymbol || symbolInfo.Symbol is IParameterSymbol)
      {
        result = SymbolType.Local;
      }
      
      if (symbolInfo.Symbol is IMethodSymbol || symbolInfo.Symbol is IPropertySymbol || symbolInfo.Symbol is IFieldSymbol)
      {
        result = SymbolType.InstanceMember;
        if (symbolInfo.Symbol.IsStatic)
        {
          result = SymbolType.ClassMember;
        }
      }

      // 定義済みの場合は強制的に変更
      if(targetCondition is PredefinedTypeSyntax)
      {
        result = SymbolType.PredefinedType;
      }

      return result;
    }

    /// <summary>
    /// IdentifierNameSyntaxを返す
    /// </summary>
    /// <param name="condition">対象</param>
    /// <returns>IdentifierNameSyntax</returns>
    private SyntaxNode GetIdentifierNameSyntax(SyntaxNode condition)
    {
      var result = condition;

      if (condition is InvocationExpressionSyntax ies)
      {
        if (ies.Expression is ThisExpressionSyntax == false)
        {
          result = GetIdentifierNameSyntax(ies.Expression);
        }
      }

      if (condition is MemberAccessExpressionSyntax maes)
      {
        if (maes.Expression is ThisExpressionSyntax == false)
        {
          result = GetIdentifierNameSyntax(maes.Expression);
        }
      }

      if (condition is ElementAccessExpressionSyntax eaes)
      {
        if (eaes.Expression is ThisExpressionSyntax == false)
        {
          result = GetIdentifierNameSyntax(eaes.Expression);
        }
      }

      return result;
    }

    #endregion
  }
}
