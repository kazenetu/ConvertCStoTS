using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ConvertCStoTS
{
  public class Converter
  {
    private string SrcPath;
    private string DescPath;

    public Converter(string srcPath,string descPath)
    {
      SrcPath = srcPath;
      DescPath = descPath;
    }

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

    public void ConvertAll(string otherReferencesPath="base")
    {
      // 対象ファイルを取得
      var targetFilePaths = GetTargetCSFilePaths();

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
    /// <param name="targetFiles"></param>
    private void Analyze(List<string> targetFiles, ref List<AnalyzeResult> analyzeResults)
    {
      var codeAnalyzer = new CodeAnalyzer();
      foreach (var filePath in targetFiles)
      {
        var importPath = Path.GetRelativePath(SrcPath, filePath).Replace(".cs", string.Empty, StringComparison.CurrentCulture).ToLower(CultureInfo.CurrentCulture);
        importPath = importPath.Replace(Path.DirectorySeparatorChar, '/');

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

        SetReference(analyzeResult);
      }

      void SetReference(AnalyzeResult target)
      {
        foreach (var searchName in target.UnknownReferences.Keys.ToList())
        {
          var refrences = results.Where(item => item.ClassNames.Contains(searchName));
          if (refrences.Any())
          {
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

        AddReference(analyzeResult);
      }

      void AddReference(AnalyzeResult target)
      {
        var sb = new StringBuilder();
        foreach (var referenceName in target.UnknownReferences.Keys.ToList())
        {
          var directoryLevel = target.ImportPath.Where(c => c == '/').Count();

          var impoertPath = target.UnknownReferences[referenceName];
          if (impoertPath == null)
          {
            sb.AppendLine($"import {{{referenceName}}} from '{setImportPath(directoryLevel, otherReferencesPath)}'");
          }
          else
          {
            sb.AppendLine($"import {{{referenceName}}} from '{setImportPath(directoryLevel, impoertPath)}'");
          }
        }

        target.SourceCode = sb.ToString() + Environment.NewLine + target.SourceCode;

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
      if (string.IsNullOrEmpty(DescPath))
      {
        DescPath = Path.Combine(SrcPath, "dist");
      }

      foreach (var analyzeResult in analyzeResults)
      {
        var filePath = $"{DescPath}/{analyzeResult.ImportPath}.ts";
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

        using (var sw = File.CreateText($"{DescPath}/{analyzeResult.ImportPath}.ts"))
        {
          sw.Write(analyzeResult.SourceCode);
        }
      }
    }

  }
}
