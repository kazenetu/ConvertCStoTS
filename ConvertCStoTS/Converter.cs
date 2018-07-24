using System;
using System.IO;
using System.Linq;

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
  }
}
