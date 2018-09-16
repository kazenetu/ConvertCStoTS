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
