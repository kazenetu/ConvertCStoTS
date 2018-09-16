using System;
using System.IO;
using Xunit;

namespace ConvertCStoTSTest
{
  public class ConverterTest
  {
    [Fact]
    public void TestLogicTest()
    {
    }

    /// <summary>
    /// ファイル読み込み
    /// </summary>
    /// <param name="filePath">対象ファイル</param>
    /// <returns>読み込み結果</returns>
    private string ReadFile(string filePath)
    {
      if (!File.Exists(filePath))
      {
        return string.Empty;
      }

      using (var sr = new StreamReader(filePath))
      {
        return sr.ReadToEnd();
      }
    }
  }
}
