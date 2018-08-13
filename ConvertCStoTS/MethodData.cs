using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace ConvertCStoTS
{
  /// <summary>
  /// 変換中のメソッド情報
  /// </summary>
  public class MethodData
  {
    public int IndexSpaceCount { get; }

    /// <summary>
    /// スコープ
    /// </summary>
    public string Scope { get; }

    /// <summary>
    /// パラメータリスト情報
    /// </summary>
    public List<ParameterData> ParamList { get; }

    /// <summary>
    /// 戻り値の型
    /// </summary>
    public string ReturnValue { get; }

    /// <summary>
    /// TypeScriptに変換したソースコード
    /// </summary>
    public string SourceCode { get; }

    /// <summary>
    /// パラメータ数
    /// </summary>
    public int PramCount
    {
      get
      {
        return ParamList.Count;
      }
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="paramList">パラメータリスト情報</param>
    /// <param name="sourceCode">TypeScriptに変換したソースコード</param>
    public MethodData(int indexSpaceCount, string scope, List<ParameterData> paramList, string sourceCode,string returnValue)
    {
      IndexSpaceCount = indexSpaceCount;
      Scope = scope;
      ParamList = paramList;
      SourceCode = sourceCode;
      ReturnValue = returnValue;
    }

    /// <summary>
    /// パラメータ情報
    /// </summary>
    public class ParameterData
    {
      /// <summary>
      /// パラメータ名
      /// </summary>
      public string Name { get; }

      /// <summary>
      /// パラメータの型
      /// </summary>
      public string LocalDeclaration { get; }

      /// <summary>
      /// コンストラクタ
      /// </summary>
      /// <param name="name">パラメータ名</param>
      /// <param name="localDeclaration">パラメータの型</param>
      public ParameterData(string name, string localDeclaration)
      {
        Name = name;
        LocalDeclaration = localDeclaration;
      }
    }
  }
}
