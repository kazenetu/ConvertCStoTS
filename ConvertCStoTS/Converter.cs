using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
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
        var sourceCode = codeAnalyzer.Analyze(sr.ReadToEnd());
        Console.Write(sourceCode);
      }
    }
  }
}
