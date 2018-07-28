using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ConvertCStoTS
{
  /// <summary>
  /// C＃ファイルをTSファイルに変換
  /// </summary>
  public class Converter
  {
    /// <summary>
    /// 変換対象C＃ファイルの入力ディレクトリ
    /// </summary>
    private readonly string SrcPath;
    /// <summary>
    /// 変換結果TSファイルの出力ディレクトリ
    /// </summary>
    private readonly string DestPath;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="srcPath">C＃ファイルの入力ディレクトリ</param>
    /// <param name="destPath">TSファイルの出力ディレクトリ</param>
    public Converter(string srcPath,string destPath)
    {
      SrcPath = srcPath;
      DestPath = destPath;
    }

    /// <summary>
    /// C＃ファイルをTSファイルに変換
    /// </summary>
    /// <param name="targetFileName">対象C＃ファイル</param>
    public void ConvertTS(string targetFileName)
    {
      var codeAnalyzer = new CodeAnalyzer();

      using(var sr = new StreamReader($"{SrcPath}/{targetFileName}"))
      {
        var analyzeResult = codeAnalyzer.Analyze(sr.ReadToEnd());
        Console.Write(analyzeResult.SourceCode);

        Console.WriteLine("-- 未知の参照 --");
        analyzeResult.UnknownReferences.ToList().ForEach(item=> Console.WriteLine(item.Key));
      }
    }

    /// <summary>
    /// C＃ファイル一括変換
    /// </summary>
    /// <param name="otherReferencesPath">未参照クラスが格納されたTSファイル</param>
    public void ConvertAll(string otherReferencesPath="base")
    {
      // 対象ファイルを取得
      var targetFilePaths = GetTargetCSFilePaths();

      // 参照結果リスト作成
      var analyzeResults = new List<AnalyzeResult>();

      // 解析
      Analyze(targetFilePaths, ref analyzeResults);

      // 未解決の参照を修正
      FixUnknownReferences(ref analyzeResults);

      // 参照を追加
      AddReferences(otherReferencesPath, ref analyzeResults);

      // ファイル作成
      CreateTSFiles(analyzeResults);
    }

    /// <summary>
    /// 対象C#ファイルパスを取得する
    /// </summary>
    /// <returns>参照ファイルパスリスト</returns>
    private List<string> GetTargetCSFilePaths()
    {
      // 除外フォルダ
      var exclusionKeywords = new List<string>() {
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"
      };

      // ディレクトリ内のファイルリストを作成
      var csFilePaths = Directory.GetFiles(SrcPath, "*.cs", SearchOption.AllDirectories);

      // 対象ファイルリストを作成
      return csFilePaths.Where(filePath => !exclusionKeywords.Any(keyword => filePath.Contains(keyword))).ToList();
    }

    /// <summary>
    /// C#ソースを解析し、TypeScript情報を取得する
    /// </summary>
    /// <param name="targetFiles">変換対象のC#ファイルリスト</param>
    /// <param name="analyzeResults">対象ファイルの解析結果リスト</param>
    private void Analyze(List<string> targetFiles, ref List<AnalyzeResult> analyzeResults)
    {
      var codeAnalyzer = new CodeAnalyzer();

      // ファイル単位でソース解析
      foreach (var filePath in targetFiles)
      {
        // TS用importパスを作成
        var importPath = Path.GetRelativePath(SrcPath, filePath).Replace(".cs", string.Empty, StringComparison.CurrentCulture).ToLower(CultureInfo.CurrentCulture);
        importPath = importPath.Replace(Path.DirectorySeparatorChar, '/');

        // C＃ファイル読み込み
        using (var sr = new StreamReader(filePath))
        {
          var tsInfo = codeAnalyzer.Analyze(sr.ReadToEnd());
          tsInfo.ImportPath = importPath;
          analyzeResults.Add(tsInfo);
        }
      }
    }

    /// <summary>
    /// 未解決の参照を修正する
    /// </summary>
    /// <param name="analyzeResults">対象ファイルの解析結果リスト</param>
    private void FixUnknownReferences(ref List<AnalyzeResult> analyzeResults)
    {
      var results = analyzeResults;
      foreach (var analyzeResult in analyzeResults)
      {
        if (!analyzeResult.UnknownReferences.Any())
        {
          continue;
        }

        // 未解決の参照の設定を行う
        SetReference(analyzeResult);
      }

      // 変換済のTSファイル情報リストから検索・設定する
      void SetReference(AnalyzeResult target)
      {
        foreach (var searchName in target.UnknownReferences.Keys.ToList())
        {
          var refrences = results.Where(item => item.ClassNames.Contains(searchName));
          if (refrences.Any())
          {
            // 変換済のTSファイル情報に未解決の参照がある場合は設定する
            target.UnknownReferences[searchName] = refrences.First().ImportPath;
          }
        }
      }
    }

    /// <summary>
    /// 参照を追加する
    /// </summary>
    /// <param name="otherReferencesPath">問題解決しない場合の参照パス</param>
    /// <param name="analyzeResults">対象ファイルの解析結果リスト</param>
    private void AddReferences(string otherReferencesPath, ref List<AnalyzeResult> analyzeResults)
    {
      foreach (var analyzeResult in analyzeResults)
      {
        if (!analyzeResult.UnknownReferences.Any())
        {
          continue;
        }

        // ソースに参照を追加する
        AddReference(analyzeResult);
      }

      // ソースに参照の追加
      void AddReference(AnalyzeResult target)
      {
        var sb = new StringBuilder();

        // 参照ごとにimportを追加する
        foreach (var referenceName in target.UnknownReferences.Keys.ToList())
        {
          // ディレクトリの深さを取得
          var directoryLevel = target.ImportPath.Where(c => c == '/').Count();

          // 「未解決の参照」の解決結果を取得
          var impoertPath = target.UnknownReferences[referenceName];
          if (impoertPath == null)
          {
            // 解決していない場合は「未参照クラスが格納されたTSファイル」に投げる
            sb.AppendLine($"import {{{referenceName}}} from '{setImportPath(directoryLevel, otherReferencesPath)}'");
          }
          else
          {
            // 解決した場合はその結果を設定する
            sb.AppendLine($"import {{{referenceName}}} from '{setImportPath(directoryLevel, impoertPath)}'");
          }
        }

        // ソースファイルの冒頭に参照情報を追記する
        target.SourceCode = sb.ToString() + Environment.NewLine + target.SourceCode;

        // ディレクトリの深さに合わせたパスを作成する
        string setImportPath(int directoryLevel, string importPath)
        {
          var result = string.Empty;
          while (directoryLevel > 0)
          {
            result += "../";
            directoryLevel--;
          }
          result += importPath;

          return result;
        }
      }
    }

    /// <summary>
    /// TSファイル作成
    /// </summary>
    /// <param name="analyzeResults">対象ファイルの解析結果リスト</param>
    private void CreateTSFiles(List<AnalyzeResult> analyzeResults)
    {
      var descPath = DestPath;

      // 出力先が設定されていない場合は暫定ディレクトリを設定する
      if (string.IsNullOrEmpty(DestPath))
      {
        descPath = Path.Combine(SrcPath, "dist");
      }

      // C#のディレクトリ構成でTSファイルを作成する
      foreach (var analyzeResult in analyzeResults)
      {
        var filePath = $"{descPath}/{analyzeResult.ImportPath}.ts";
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

        using (var sw = File.CreateText(filePath))
        {
          sw.Write(analyzeResult.SourceCode);
        }
      }
    }

  }
}
