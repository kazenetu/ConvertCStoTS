using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharpAnalyze.Domain.Event
{
  /// <summary>
  /// イベントコンテナ
  /// </summary>
  public static class EventContainer
  {
    /// <summary>
    /// イベントハンドラリスト
    /// </summary>
    private static List<(object instance, Delegate callback)> Handles = new List<(object instance, Delegate callback)>();

    /// <summary>
    /// イベントの登録
    /// </summary>
    /// <param name="instance">登録対象のインスタンス</param>
    /// <param name="callback">イベントハンドラ</param>
    public static void Register<T>(object instance, Action<T> callback) where T : IEvent
    {
      Handles.Add((instance, callback));
    }

    /// <summary>
    /// イベントの削除
    /// </summary>
    /// <param name="instance">登録対象のインスタンス</param>
    /// <param name="callback">イベントハンドラ</param>
    public static void Unregister<T>(object instance) where T : IEvent
    {
      var targets = Handles.Where(handle => handle.instance == instance && handle.callback is Action<T>).ToList();
      targets.ForEach(item => Handles.Remove((item.instance, item.callback)));
    }

    /// <summary>
    /// イベント発行
    /// </summary>
    /// <param name="args">発行イベント</param>
    public static void Raise<T>(T args)
    {
      var targets = Handles.Where(handle => handle.callback is Action<T>);
      foreach (var target in targets)
      {
        ((Action<T>)target.callback)(args);
      }
    }
  }
}
