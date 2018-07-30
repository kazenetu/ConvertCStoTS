using Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace ConvertCStoTS
{
  class Program
  {
    static void Main(string[] args)
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
        Console.WriteLine("-h, --help  view this page");
        return;
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

      Console.WriteLine("---Convert Start---");
      var converter = new Converter(srcPath, destPath);
      try
      {
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
      }

#if DEBUG
      Console.ReadKey();
#endif
    }
  }
}
