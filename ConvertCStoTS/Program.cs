using System;

namespace ConvertCStoTS
{
  class Program
  {
    static void Main(string[] args)
    {
      var targetDirectory = AppDomain.CurrentDomain.BaseDirectory;
      targetDirectory = targetDirectory.Substring(0, targetDirectory.LastIndexOf("ConvertCStoTS"));
      var srcPath = $"{targetDirectory}TargetSources";
      var descPath = "";

      var converter = new Converter(srcPath, descPath);

      converter.ConvertTS("Response/OrderList/SearchResponse.cs");

      Console.ReadKey();
    }
  }
}
