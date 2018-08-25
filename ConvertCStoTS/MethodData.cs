using System.Collections.Generic;

namespace ConvertCStoTS
{
  /// <summary>
  /// 変換中のメソッド情報
  /// </summary>
  public class MethodData
  {
    /// <summary>
    /// インデント数
    /// </summary>
    public int IndentSpaceCount { get; }

    /// <summary>
    /// スコープ
    /// </summary>
    public string Scope { get; set; }

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
    /// コンストラクタで設定されたスーパークラスのパラメータ数
    /// </summary>
    public int BaseArgCount { get; } 

    /// <summary>
    /// ヘッダーコメント
    /// </summary>
    public string HeaderComments { get; }

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
    /// <param name="indentSpaceCount">インデント数</param>
    /// <param name="scope">スコープ</param>
    /// <param name="paramList">パラメータリスト情報</param>
    /// <param name="sourceCode">TypeScriptに変換したソースコード</param>
    /// <param name="returnValue">戻り値の型</param>
    /// <param name="baseArgCount">コンストラクタで設定されたスーパークラスのパラメータ数</param>
    /// <param name="headerComment">ヘッダーコマンド</param>
    public MethodData(int indentSpaceCount, string scope, List<ParameterData> paramList, string sourceCode, string returnValue, int baseArgCount = -1,string headerComment="")
    {
      IndentSpaceCount = indentSpaceCount;
      Scope = scope;
      ParamList = paramList;
      SourceCode = sourceCode;
      ReturnValue = returnValue;
      BaseArgCount = baseArgCount;
      HeaderComments = headerComment;
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
