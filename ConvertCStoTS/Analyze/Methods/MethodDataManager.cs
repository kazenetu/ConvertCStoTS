using ConvertCStoTS.Common;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ConvertCStoTS.Analyze.Methods
{
  /// <summary>
  /// TypeScript変換済メソッド管理クラス
  /// </summary>
  public class MethodDataManager
  {
    /// <summary>
    /// メソッド名重複対応
    /// </summary>
    private readonly Dictionary<string, List<MethodData>> MethodLists = new Dictionary<string, List<MethodData>>();

    /// <summary>
    /// クリア
    /// </summary>
    public void Clear()
    {
      MethodLists.Clear();
    }

    /// <summary>
    /// メソッドデータの追加
    /// </summary>
    /// <param name="item">対象のメソッドデータ</param>
    public void AddMethodData(MethodData item)
    {
      var methodName = item.MethodName;
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
        result.Append(CreateMethodText(methodName, MethodLists[methodName]));
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
        if (item == null)
        {
          return string.Empty;
        }

        // メソッド作成
        result.Append(CreateMethodText(methodName, spaceIndex, item));
      }
      else
      {
        // 複数メソッド名が重複している場合

        // 初期化
        var paramCount = methodDataList.Max(item => item.PramCount);
        var paramLists = new List<List<string>>();
        for (var index = 0; index < paramCount; index++)
        {
          var dataList = new List<string>();
          for (var dataIndex = 0; dataIndex < methodDataList.Count; dataIndex++)
          {
            dataList.Add("null");
          }
          paramLists.Add(dataList);
        }

        // 仮メソッドを作成
        var retunValue = string.Empty;
        var summaryMethod = new StringBuilder();
        var dataListIndex = 0;
        foreach (var methodData in methodDataList)
        {
          var paramIndex = 0;
          foreach (var paramData in methodData.ParamList)
          {
            paramLists[paramIndex][dataListIndex] = paramData.LocalDeclaration;
            paramIndex++;
          }

          // 仮メソッド作成
          var tempMethodName = $"{methodName}{dataListIndex}";
          methodData.Scope = "private";
          result.Append(CreateMethodText(tempMethodName, spaceIndex, methodData));
          retunValue = methodData.ReturnValue;

          // 集約メソッド用処理を追加
          summaryMethod.Append($"{spaceIndex}{spaceIndex}");
          summaryMethod.Append("if (");
          for (paramIndex = 0; paramIndex < paramCount; paramIndex++)
          {
            var paramType = paramLists[paramIndex][dataListIndex];
            if (paramType == paramType.ToLower(CultureInfo.CurrentCulture))
            {
              if (paramType == "null")
              {
                summaryMethod.Append($"p{paramIndex} === null");
              }
              else
              {
                summaryMethod.Append($"typeof p{paramIndex} === '{paramType}'");
              }
            }
            else
            {
              summaryMethod.Append($"p{paramIndex} instanceof {paramType}");
            }

            if (paramCount > 1 && paramIndex < paramCount - 1)
            {
              summaryMethod.Append(" && ");
            }
          }
          summaryMethod.AppendLine(") {");
          summaryMethod.Append($"{spaceIndex}{spaceIndex}");

          summaryMethod.Append($"{spaceIndex}");
          if (!string.IsNullOrEmpty(methodData.ReturnValue) && methodData.ReturnValue != "void")
          {
            summaryMethod.Append("return ");
          }
          summaryMethod.Append($"this.{tempMethodName}(");
          var tempIndex = 0;
          while (tempIndex < methodData.PramCount)
          {
            if (tempIndex > 0)
            {
              summaryMethod.Append(", ");
            }
            summaryMethod.Append($"p{tempIndex}");
            tempIndex++;
          }

          summaryMethod.AppendLine(");");
          summaryMethod.AppendLine($"{spaceIndex}{spaceIndex}" + "}");

          dataListIndex++;
        }

        // 集約メソッド用パラメータの作成
        var typeScriptComments = new List<string>();
        var summaryParamIndex = 0;
        while (summaryParamIndex < paramCount)
        {
          typeScriptComments.Add(GetParam($"p{summaryParamIndex}", paramLists[summaryParamIndex]));
          summaryParamIndex++;
        }

        // 集約メソッド用コメントの作成
        var summaryComments = new StringBuilder();
        summaryComments.AppendLine($"/// <summary>{methodName} Summary Method</summary>");
        foreach (var summaryComment in typeScriptComments)
        {
          var tsComment = summaryComment.Split(": ");
          var commentBody = tsComment[1].Replace(" | ", " or ", System.StringComparison.CurrentCulture);
          summaryComments.AppendLine($"/// <param name=\"{tsComment[0]}\">{commentBody}</param>");
        }
        result.Append(AnalyzeUtility.GetComments(summaryComments.ToString()));

        // 集約メソッド作成
        result.Append($"{spaceIndex}public {methodName}(");
        result.Append(string.Join(", ", typeScriptComments));
        result.Append(")");
        if (!string.IsNullOrEmpty(retunValue) && retunValue != "void")
        {
          result.Append($": {retunValue}");
        }
        result.AppendLine(" {");

        // スーパークラスのコンストラクタを設定
        var superMethodArgCount = methodDataList.Max(item => item.BaseArgCount);
        if (superMethodArgCount >= 0)
        {
          result.Append($"{spaceIndex}{spaceIndex}super(");

          summaryParamIndex = 0;
          while (summaryParamIndex < superMethodArgCount)
          {
            if (summaryParamIndex > 0)
            {
              result.Append(", ");
            }
            result.Append($"p{summaryParamIndex}");

            summaryParamIndex++;
          }

          result.AppendLine(");");
        }

        result.Append(summaryMethod.ToString());
        result.Append(GetDefaultReturnValue(spaceIndex,retunValue));
        result.AppendLine($"{spaceIndex}" + "}");
      }

      return result.ToString();
    }

    /// <summary>
    /// メソッド生成
    /// </summary>
    /// <param name="methodName">メソッド名</param>
    /// <param name="spaceIndex">スペース</param>
    /// <param name="item">変換中メソッド情報</param>
    /// <returns>変換後のTypeScriptソースコード</returns>
    private string CreateMethodText(string methodName, string spaceIndex, MethodData item)
    {
      var result = new StringBuilder();

      // ヘッダーコメント
      result.Append(item.HeaderComments);

      // メソッド設定
      result.Append($"{spaceIndex}{item.Scope} {methodName}(");

      var isFirst = true;
      foreach (var param in item.ParamList)
      {
        if (!isFirst)
        {
          result.Append(", ");
        }

        result.Append(GetParam(param.Name, new List<string>() { param.LocalDeclaration }));

        isFirst = false;
      }

      result.Append(")");
      if (!string.IsNullOrEmpty(item.ReturnValue))
      {
        result.Append($": {item.ReturnValue }");
      }
      result.AppendLine(" {");

      // メソッド内処理を変換
      result.Append(item.SourceCode);

      result.AppendLine(spaceIndex + "}");

      return result.ToString();
    }

    /// <summary>
    /// パラメータ設定
    /// </summary>
    /// <param name="name">パラメータ名</param>
    /// <param name="localDeclarations">パラメータリスト</param>
    /// <returns>パラメータ設定のTypeScriptソースコード</returns>
    private string GetParam(string name, List<string> localDeclarations)
    {
      var result = new StringBuilder();

      result.Append($"{name}");

      var isFirst = true;
      foreach (var localDeclaration in localDeclarations.Distinct())
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

    /// <summary>
    /// 集約メソッドのデフォルト戻り値の取得
    /// </summary>
    /// <param name="spaceIndex">スペース</param>
    /// <param name="returnValue">TypeScriptの戻り値の型</param>
    /// <returns>TypeScriptの戻り値文字列(改行あり)</returns>
    /// <remarks>戻り値なし・voidの場合はstring.Empty</remarks>
    private string GetDefaultReturnValue(string spaceIndex,string returnValue)
    {
      if (string.IsNullOrEmpty(returnValue) || returnValue == "void")
      {
        return string.Empty;
      }

      var result = $"new {returnValue}()";
      switch (returnValue)
      {
        case "number":
          result = "0";
          break;
        case "string":
          result = "''";
          break;
        case "boolean":
          result = "false";
          break;
      }

      return $"{spaceIndex}{spaceIndex}return {result};" + System.Environment.NewLine;
    }
  }
}
