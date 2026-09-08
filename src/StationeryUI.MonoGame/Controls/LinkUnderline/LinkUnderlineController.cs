namespace StationeryUI.MonoGame.Controls.LinkUnderline;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>非同期アクションへ接続する Link Underline の実行状態を管理します。</summary>
public sealed class LinkUnderlineController : IDisposable
{
    public const double SpinnerDelaySeconds = 0.1d;
    public const string InterruptedMessage = "中断しました。状態が完全に更新されていない可能性があります。";

    private CancellationTokenSource? _cancellation;
    private Task? _actionTask;
    private bool _cancellationRequested;
    private double _startedAtSeconds = double.NegativeInfinity;

    public LinkUnderlineState State { get; private set; } = LinkUnderlineState.Idle;

    public string Message { get; private set; } = "";

    public bool IsExecuting => _actionTask is { IsCompleted: false };

    public bool CanActivate => !IsExecuting;

    public bool IsSpinnerVisible(double nowSeconds) =>
        IsExecuting && nowSeconds - _startedAtSeconds >= SpinnerDelaySeconds;

    public bool TryStart(Func<CancellationToken, Task> action, double nowSeconds)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!CanActivate)
            return false;

        DisposeCompletedCancellation();
        _cancellation = new CancellationTokenSource();
        _cancellationRequested = false;
        _startedAtSeconds = nowSeconds;
        State = LinkUnderlineState.Executing;
        Message = "";
        var cancellationToken = _cancellation.Token;
        _actionTask = Task.Run(async () => await action(cancellationToken), cancellationToken);
        return true;
    }

    /// <summary>ゲームの Update から呼び、完了したアクションの結果を UI 状態へ反映します。</summary>
    public void Update()
    {
        if (_actionTask is not { IsCompleted: true } completedTask)
            return;

        if (_cancellationRequested || completedTask.IsCanceled)
        {
            State = LinkUnderlineState.Interrupted;
            Message = InterruptedMessage;
        }
        else if (completedTask.IsFaulted)
        {
            State = LinkUnderlineState.Failed;
            Message = completedTask.Exception?.GetBaseException().Message ?? "処理に失敗しました。";
        }
        else
        {
            State = LinkUnderlineState.Succeeded;
            Message = "";
        }

        _actionTask = null;
        DisposeCompletedCancellation();
    }

    /// <summary>
    /// 実行中のアクションへ中断を要求します。アクションが停止するまでは新しい実行を受け付けません。
    /// </summary>
    public bool Cancel()
    {
        if (!IsExecuting || _cancellation is null)
            return false;

        _cancellationRequested = true;
        _cancellation.Cancel();
        return true;
    }

    public void Reset()
    {
        if (IsExecuting)
            throw new InvalidOperationException("実行中の Link Underline はリセットできません。");

        State = LinkUnderlineState.Idle;
        Message = "";
        _cancellationRequested = false;
        _startedAtSeconds = double.NegativeInfinity;
    }

    public void Dispose()
    {
        _cancellationRequested = true;
        _cancellation?.Cancel();
        _cancellation?.Dispose();
    }

    private void DisposeCompletedCancellation()
    {
        _cancellation?.Dispose();
        _cancellation = null;
    }
}

public enum LinkUnderlineState
{
    Idle,
    Executing,
    Succeeded,
    Failed,
    Interrupted,
}
