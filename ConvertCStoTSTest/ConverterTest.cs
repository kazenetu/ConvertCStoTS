using ConvertCStoTS;
using System.IO;
using Xunit;

namespace ConvertCStoTSTest
{
  public class ConverterTest
  {
    [Fact]
    public void TestLogicTest()
    {
      var srcPath = "../../../../TargetSources";
      var destPath = "./dist";
      var isOutputMethod = true;
      var filePath = "testlogic";
      var otherReferencesPath = "base";

      // TypeScriptファイル出力
      var converter = new Converter(srcPath, destPath, isOutputMethod);
      converter.ConvertTS($"{filePath}.cs", otherReferencesPath);

      // ファイル読み込み
      var expectedFilePath = "./ExpectedTypeScriptFiles/TestLogicFull.ts";
      var expectedResult = ReadFile(expectedFilePath);
      var outputFlieResult = ReadFile($"{destPath}/{filePath}.ts");

      // ファイル比較
      Assert.True(expectedResult == outputFlieResult, "変換エラー");
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
