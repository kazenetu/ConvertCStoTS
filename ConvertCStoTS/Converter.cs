using System;
using System.IO;

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
      }
    }
  }
}
