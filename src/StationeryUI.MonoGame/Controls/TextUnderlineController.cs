namespace StationeryUI.MonoGame.Controls;

using StationeryUI.Platform;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Globalization;

public sealed class TextBoxController
{
    private const double CaretKeyRepeatInitialDelaySeconds = 0.42d;
    private const double CaretKeyRepeatIntervalSeconds = 0.055d;
    private const double MouseDoubleClickSeconds = 0.36d;
    private const int MaximumHistoryCount = 100;

    private readonly int _maxLength;
    private char? _pendingHighSurrogate;
    private readonly Func<double> _timestampProvider;
    private double _leftKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
    private double _rightKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
    private double _backKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
    private double _deleteKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
    private double _undoKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
    private double _redoKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
    private double _lastMouseSelectionStartedAt = double.NegativeInfinity;
    private readonly Stack<TextEditSnapshot> _undoHistory = new();
    private readonly Stack<TextEditSnapshot> _redoHistory = new();

    public TextBoxController(int maxLength, Func<double>? timestampProvider = null)
    {
        _maxLength = maxLength;
        _timestampProvider = timestampProvider ??
            (() => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency);
    }

    public string Text { get; private set; } = "";

    public int CaretIndex { get; private set; }

    public int SelectionStart
    {
        get
        {
            var caret = Math.Clamp(CaretIndex, 0, Text.Length);
            var anchor = Math.Clamp(SelectionAnchor ?? caret, 0, Text.Length);
            return Math.Min(anchor, caret);
        }
    }

    public int SelectionLength
    {
        get
        {
            var caret = Math.Clamp(CaretIndex, 0, Text.Length);
            var anchor = Math.Clamp(SelectionAnchor ?? caret, 0, Text.Length);
            return Math.Abs(anchor - caret);
        }
    }

    public bool HasSelection => SelectionLength > 0;

    private int? SelectionAnchor { get; set; }

    public bool IsMouseSelecting { get; private set; }

    public bool IsCaretNavigationKeyHeld { get; private set; }

    public void Begin(string text)
    {
        Begin(text, text.Length);
    }

    public void Begin(string text, int caretIndex)
    {
        _pendingHighSurrogate = null;
        Text = text;
        SetCaretIndex(caretIndex);
        ClearSelection();
        ClearHistory();
        IsCaretNavigationKeyHeld = false;
        ResetCaretKeyRepeat();
        ResetMouseDoubleClick();
    }

    public void SetCaretIndex(int caretIndex, bool extendSelection = false)
    {
        if (extendSelection && SelectionAnchor is null)
            SelectionAnchor = CaretIndex;
        else if (!extendSelection)
            SelectionAnchor = null;
        CaretIndex = StringInfo.ParseCombiningCharacters(Text).Append(Text.Length).Last(i => i <= Math.Clamp(caretIndex, 0, Text.Length));
    }

    public void BeginMouseSelection(int caretIndex, bool extendSelection)
    {
        var timestampSeconds = _timestampProvider();
        var isDoubleClick =
            !extendSelection &&
            timestampSeconds >= _lastMouseSelectionStartedAt &&
            timestampSeconds - _lastMouseSelectionStartedAt <= MouseDoubleClickSeconds;
        _lastMouseSelectionStartedAt = isDoubleClick
            ? double.NegativeInfinity
            : timestampSeconds;

        if (isDoubleClick)
        {
            SelectionAnchor = 0;
            CaretIndex = Text.Length;
            IsMouseSelecting = false;
            return;
        }

        SetCaretIndex(caretIndex, extendSelection);
        if (!extendSelection)
            SelectionAnchor = CaretIndex;
        IsMouseSelecting = true;
    }

    public void UpdateMouseSelection(int caretIndex)
    {
        if (IsMouseSelecting)
            SetCaretIndex(caretIndex, extendSelection: true);
    }

    public void EndMouseSelection() => IsMouseSelecting = false;

    public void Clear()
    {
        _pendingHighSurrogate = null;
        Text = "";
        CaretIndex = 0;
        ClearSelection();
        IsMouseSelecting = false;
        ClearHistory();
        IsCaretNavigationKeyHeld = false;
        ResetCaretKeyRepeat();
        ResetMouseDoubleClick();
    }

    public bool TryInputCharacter(char character)
    {
        if (char.IsHighSurrogate(character)) { _pendingHighSurrogate = character; return true; }
        if (char.IsLowSurrogate(character))
        {
            if (_pendingHighSurrogate is not { } high) return false;
            _pendingHighSurrogate = null;
            if (_maxLength - (Text.Length - SelectionLength) < 2) return false;
            InsertText(new string(new[] { high, character }), null);
            return true;
        }
        _pendingHighSurrogate = null;
        if (char.IsControl(character))
        {
            return true;
        }

        if (!HasSelection && Text.Length >= _maxLength)
        {
            return false;
        }

        PushUndoSnapshot();
        DeleteSelection();
        Text = Text.Insert(CaretIndex, character.ToString());
        CaretIndex++;
        return true;
    }

    public TextBoxKeyboardAction HandleKeyboard(
        KeyboardState keyboard,
        KeyboardState previousKeyboard,
        GameTime gameTime,
        IClipboardService clipboardService,
        bool allowClipboardExport = true,
        Func<char, bool>? pasteCharacterFilter = null,
        bool multiline = false)
    {
        IsCaretNavigationKeyHeld = keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.Right);
        var control = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        var shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

        if (control &&
            ShouldHandleRepeatedKey(keyboard, previousKeyboard, Keys.Z, ref _undoKeyRepeatCountdown, gameTime))
        {
            Undo();
            return TextBoxKeyboardAction.None;
        }

        if (control &&
            ShouldHandleRepeatedKey(keyboard, previousKeyboard, Keys.Y, ref _redoKeyRepeatCountdown, gameTime))
        {
            Redo();
            return TextBoxKeyboardAction.None;
        }

        if (!control)
        {
            _undoKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
            _redoKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
        }

        if (control && IsNewKeyPress(keyboard, previousKeyboard, Keys.A))
        {
            SelectionAnchor = 0;
            CaretIndex = Text.Length;
        }

        if (control && IsNewKeyPress(keyboard, previousKeyboard, Keys.C) &&
            allowClipboardExport && HasSelection)
        {
            clipboardService.TrySetText(Text.Substring(SelectionStart, SelectionLength));
        }

        if (control && IsNewKeyPress(keyboard, previousKeyboard, Keys.X) &&
            allowClipboardExport && HasSelection)
        {
            if (clipboardService.TrySetText(Text.Substring(SelectionStart, SelectionLength)))
            {
                PushUndoSnapshot();
                DeleteSelection();
            }
        }

        if (control && IsNewKeyPress(keyboard, previousKeyboard, Keys.V) &&
            clipboardService.TryGetText(out var clipboardText))
        {
            InsertText(clipboardText, pasteCharacterFilter, multiline);
        }

        if (IsNewKeyPress(keyboard, previousKeyboard, Keys.Enter))
        {
            if (!multiline || control)
                return TextBoxKeyboardAction.Commit;

            InsertText("\n", null, allowNewLines: true);
        }

        if (IsNewKeyPress(keyboard, previousKeyboard, Keys.Escape))
        {
            return TextBoxKeyboardAction.Cancel;
        }

        if (ShouldHandleRepeatedKey(keyboard, previousKeyboard, Keys.Left, ref _leftKeyRepeatCountdown, gameTime) && CaretIndex > 0)
        {
            SetCaretIndex(PreviousBoundary(), shift);
        }

        if (ShouldHandleRepeatedKey(keyboard, previousKeyboard, Keys.Right, ref _rightKeyRepeatCountdown, gameTime) && CaretIndex < Text.Length)
        {
            SetCaretIndex(NextBoundary(), shift);
        }

        if (IsNewKeyPress(keyboard, previousKeyboard, Keys.Home))
        {
            SetCaretIndex(0, shift);
        }

        if (IsNewKeyPress(keyboard, previousKeyboard, Keys.End))
        {
            SetCaretIndex(Text.Length, shift);
        }

        if (ShouldHandleRepeatedKey(keyboard, previousKeyboard, Keys.Back, ref _backKeyRepeatCountdown, gameTime))
        {
            if (HasSelection)
            {
                PushUndoSnapshot();
                DeleteSelection();
            }
            else if (CaretIndex > 0)
            {
                PushUndoSnapshot();
                var previous = PreviousBoundary();
                Text = Text.Remove(previous, CaretIndex - previous);
                CaretIndex = previous;
                ClearSelection();
            }
        }

        if (ShouldHandleRepeatedKey(keyboard, previousKeyboard, Keys.Delete, ref _deleteKeyRepeatCountdown, gameTime))
        {
            if (HasSelection)
            {
                PushUndoSnapshot();
                DeleteSelection();
            }
            else if (CaretIndex < Text.Length)
            {
                PushUndoSnapshot();
                Text = Text.Remove(CaretIndex, NextBoundary() - CaretIndex);
                ClearSelection();
            }
        }

        return TextBoxKeyboardAction.None;
    }

    private void InsertText(string value, Func<char, bool>? characterFilter, bool allowNewLines = false)
    {
        value = value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        if (!allowNewLines)
            value = value.Replace("\n", "");
        if (characterFilter is not null)
            value = new string(value.Where(characterFilter).ToArray());
        var available = _maxLength - (Text.Length - SelectionLength);
        if (available <= 0 || value.Length == 0) return;
        var end = StringInfo.ParseCombiningCharacters(value).Append(value.Length).Last(i => i <= Math.Min(available, value.Length));
        if (end == 0) return;
        var inserted = value[..end];
        PushUndoSnapshot();
        DeleteSelection();
        Text = Text.Insert(CaretIndex, inserted);
        CaretIndex += inserted.Length;
    }

    private int PreviousBoundary() => StringInfo.ParseCombiningCharacters(Text).LastOrDefault(i => i < CaretIndex);
    private int NextBoundary() => StringInfo.ParseCombiningCharacters(Text).Append(Text.Length).First(i => i > CaretIndex);

    private bool DeleteSelection()
    {
        if (!HasSelection)
        {
            ClearSelection();
            return false;
        }
        var start = SelectionStart;
        var length = Math.Min(SelectionLength, Text.Length - start);
        Text = Text.Remove(start, length);
        CaretIndex = start;
        ClearSelection();
        return true;
    }

    private void ClearSelection() => SelectionAnchor = null;

    private void PushUndoSnapshot()
    {
        _undoHistory.Push(CaptureSnapshot());
        while (_undoHistory.Count > MaximumHistoryCount)
        {
            var snapshots = _undoHistory.Take(MaximumHistoryCount).Reverse().ToArray();
            _undoHistory.Clear();
            foreach (var snapshot in snapshots)
                _undoHistory.Push(snapshot);
        }
        _redoHistory.Clear();
    }

    private void Undo()
    {
        if (_undoHistory.Count == 0) return;
        _redoHistory.Push(CaptureSnapshot());
        RestoreSnapshot(_undoHistory.Pop());
    }

    private void Redo()
    {
        if (_redoHistory.Count == 0) return;
        _undoHistory.Push(CaptureSnapshot());
        RestoreSnapshot(_redoHistory.Pop());
    }

    private TextEditSnapshot CaptureSnapshot() => new(Text, CaretIndex, SelectionAnchor);

    private void RestoreSnapshot(TextEditSnapshot snapshot)
    {
        Text = snapshot.Text;
        CaretIndex = snapshot.CaretIndex;
        SelectionAnchor = snapshot.SelectionAnchor;
        IsMouseSelecting = false;
    }

    private void ClearHistory()
    {
        _undoHistory.Clear();
        _redoHistory.Clear();
    }

    private static bool IsNewKeyPress(KeyboardState keyboard, KeyboardState previousKeyboard, Keys key) =>
        keyboard.IsKeyDown(key) && previousKeyboard.IsKeyUp(key);

    private static bool ShouldHandleRepeatedKey(
        KeyboardState keyboard,
        KeyboardState previousKeyboard,
        Keys key,
        ref double repeatCountdown,
        GameTime gameTime)
    {
        if (keyboard.IsKeyUp(key))
        {
            repeatCountdown = CaretKeyRepeatInitialDelaySeconds;
            return false;
        }

        if (previousKeyboard.IsKeyUp(key))
        {
            repeatCountdown = CaretKeyRepeatInitialDelaySeconds;
            return true;
        }

        repeatCountdown -= gameTime.ElapsedGameTime.TotalSeconds;
        if (repeatCountdown > 0d)
        {
            return false;
        }

        repeatCountdown += CaretKeyRepeatIntervalSeconds;
        if (repeatCountdown <= 0d)
        {
            repeatCountdown = CaretKeyRepeatIntervalSeconds;
        }

        return true;
    }

    private void ResetCaretKeyRepeat()
    {
        _leftKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
        _rightKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
        _backKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
        _deleteKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
        _undoKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
        _redoKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
    }

    private void ResetMouseDoubleClick() =>
        _lastMouseSelectionStartedAt = double.NegativeInfinity;

    private readonly record struct TextEditSnapshot(string Text, int CaretIndex, int? SelectionAnchor);
}

public enum TextBoxKeyboardAction
{
    None,
    Commit,
    Cancel,
}
