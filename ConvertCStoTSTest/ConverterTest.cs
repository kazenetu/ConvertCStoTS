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

      // TypeScript�t�@�C���o��
      var converter = new Converter(srcPath, destPath, isOutputMethod);
      converter.ConvertTS($"{filePath}.cs", otherReferencesPath);

      // �t�@�C���ǂݍ���
      var expectedFilePath = "./ExpectedTypeScriptFiles/TestLogicFull.ts";
      var expectedResult = ReadFile(expectedFilePath);
      var outputFlieResult = ReadFile($"{destPath}/{filePath}.ts");

      // �t�@�C����r
      Assert.True(expectedResult == outputFlieResult, "�ϊ��G���[");
    }

    /// <summary>
    /// �t�@�C���ǂݍ���
    /// </summary>
    /// <param name="filePath">�Ώۃt�@�C��</param>
    /// <returns>�ǂݍ��݌���</returns>
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
