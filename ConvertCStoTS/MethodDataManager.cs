using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConvertCStoTS
{
  /// <summary>
  /// TSメソッド管理クラス
  /// </summary>
  public class MethodDataManager
  {
    /// <summary>
    /// メソッド名重複対応
    /// </summary>
    private readonly Dictionary<string, List<MethodData>> MethodLists = new Dictionary<string, List<MethodData>>();

    /// <summary>
    /// メソッドデータの追加
    /// </summary>
    /// <param name="methodName">メソッド名</param>
    /// <param name="item">対象のメソッドデータ</param>
    public void AddMethodData(string methodName,MethodData item)
    {
      if (!MethodLists.ContainsKey(methodName))
      {
        MethodLists.Add(methodName, new List<MethodData>());
      }

      MethodLists[methodName].Add(item);
    }

    /// <summary>
    /// メソッドTSコードの取得
    /// </summary>
    /// <returns>メソッドTSコード</returns>
    public string GetMethodText()
    {
      var result = new StringBuilder();

      foreach (var methodName in MethodLists.Keys)
      {
        result.Append(CreateMethodText(methodName,MethodLists[methodName]));
      }

      return result.ToString();
    }

    /// <summary>
    /// メソッド名単位のTSコード作成
    /// </summary>
    /// <param name="methodName">メソッド名</param>
    /// <param name="methodDataList">メソッド名が同じメソッドコードリスト</param>
    /// <returns></returns>
    private string CreateMethodText(string methodName, List<MethodData> methodDataList)
    {
      var result = new StringBuilder();

      var paramList = new List<HashSet<string>>();

      var spaceIndex = "  ";
      if (methodDataList.Count <= 1)
      {
        // メソッドが重複しない場合はそのまま出力
        var item = methodDataList.FirstOrDefault();
        if(item == null)
        {
          return string.Empty;
        }

        result.Append($"{spaceIndex}{item.Scope} {methodName}(");

        var isFirst = true;
        foreach(var param in item.ParamList)
        {
          if (!isFirst)
          {
            result.Append(", ");
          }

          result.Append(GetParam(param.Name, new List<string>() { param.LocalDeclaration }));

          isFirst = false;
        }

        result.Append(")");
        result.AppendLine(" {");

        // メソッド内処理を変換
        result.Append(item.SourceCode);

        result.AppendLine(spaceIndex + "}");
      }
      else
      {
        // TODO 複数メソッド名が重複している場合
       

      }

      return result.ToString();
    }

    /// <summary>
    /// パラメータ設定
    /// </summary>
    /// <param name="name">パラメータ名</param>
    /// <param name="localDeclarations">パラメータリスト</param>
    /// <returns>パラメータ設定のTypeScriptソースコード</returns>
    private string GetParam(string name,List<string> localDeclarations)
    {
      var result = new StringBuilder();

      result.Append($"{name}");

      var isFirst = true;
      foreach(var localDeclaration in localDeclarations.Distinct())
      {
        if (isFirst)
        {
          result.Append(": ");
          isFirst = false;
        }
        else
        {
          result.Append(" | ");
        }

        result.Append(localDeclaration);
      }

      return result.ToString();
    }
  }
}
