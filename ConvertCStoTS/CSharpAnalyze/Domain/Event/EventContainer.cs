﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConvertCStoTS.CSharpAnalyze.Domain.Event
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
