using Common;
using CSharpAnalyze.ApplicationService;
using System;
using System.Collections.Generic;
using System.IO;

namespace ConvertCStoTS
{
  class Program
  {
    static int Main(string[] args)
    {
      // パラメータ取得
      var argManager = new ArgManagers(args);

      // ヘルプモードの確認
      var isShowHelp = false;
      if(argManager.GetRequiredArgCount() <= 0)
      {
        // パラメータが不正の場合はヘルプモード
        isShowHelp = true;
      }
      if (argManager.ExistsOptionArg(new List<string>() { "--help","-h" }))
      {
        // ヘルプオプションはヘルプモード
        isShowHelp = true;
      }

      // ヘルプ画面を表示
      if (isShowHelp)
      {
        Console.WriteLine("how to use: ConvertCStoTS <SourcePath> [options]");
        Console.WriteLine("");
        Console.WriteLine("<SourcePath> Input C# Path");
        Console.WriteLine("");
        Console.WriteLine("options:");
        Console.WriteLine("-f, --file  <FilePath>       Input C# Path");
        Console.WriteLine("-o, --out   <OutputPath>     Output TypeScript Path");
        Console.WriteLine("-r, --ref   <ReferencesPath> References TypeScript Path");
        Console.WriteLine("--no_method_output           No Method Output");
        Console.WriteLine("-h, --help  view this page");
        return 0;
      }

      var srcPath = Path.GetFullPath(argManager.GetRequiredArg(0));
      var destPath = argManager.GetOptionArg(new List<string>() { "--out", " -o" });
      if (string.IsNullOrEmpty(destPath))
      {
        destPath = Path.Combine(srcPath, "dest");
      }
      else
      {
        destPath = Path.GetFullPath(destPath);
      }

      // 参照TSファイルを取得
      var otherReferencesPath = argManager.GetOptionArg(new List<string>() { "--ref", " -r" });
      if (string.IsNullOrEmpty(otherReferencesPath))
      {
        otherReferencesPath = "base";
      }

      // FilePath
      var filePath = argManager.GetOptionArg(new List<string>() { "--file", " -f" });

      // Output Method
      var isOutputMethod = !argManager.ExistsOptionArg("--no_method_output");

      // コレクション用クラスをコピー
      var tsFileNames = new List<string>() { "Dictionary.ts", "List.ts" };
      foreach(var tsFileName in tsFileNames)
      {
        File.Copy($"TypeScripts/{ tsFileName}", Path.Combine(destPath, tsFileName), true);
      }

      // C#ファイルの変換とファイル出力
      Console.WriteLine("---Convert Start---");
      var converter = new Converter(srcPath, destPath, isOutputMethod);
      try
      {
        // HACK 設計し直し版のテスト実行
        var csAnalyze = new AnalyzeApplication();
        csAnalyze.GetAnalyzeResult(srcPath);

        if (string.IsNullOrEmpty(filePath))
        {
          converter.ConvertAll(otherReferencesPath);
        }
        else
        {
          converter.ConvertTS(filePath, otherReferencesPath);
        }
        Console.WriteLine("---Convert End---");
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        Console.WriteLine($"ErrorMethod[{converter.RunMethodName}]");

        Console.WriteLine("---Convert Fail---");

        return 1;
      }

#if DEBUG
      Console.ReadKey();
#endif

      return 0;
    }
  }
}
