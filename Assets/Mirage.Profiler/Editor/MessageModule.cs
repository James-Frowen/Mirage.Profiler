using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mirage.NetworkProfiler.ModuleGUI
{
    [System.Serializable]
    [ProfilerModuleMetadata(ModuleNames.SENT)]
    public class SentModule : ProfilerModule, ICountRecorderProvider
    {
        private static readonly ProfilerCounterDescriptor[] k_Counters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor(Names.SENT_COUNT, Counters.Category),
            new ProfilerCounterDescriptor(Names.SENT_BYTES, Counters.Category),
            new ProfilerCounterDescriptor(Names.SENT_PER_SECOND, Counters.Category),
        };

        public SentModule() : base(k_Counters) { }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            var names = new MessageViewController.CounterNames(
                Names.SENT_COUNT,
                Names.SENT_BYTES,
                Names.SENT_PER_SECOND
            );

            return new MessageViewController(ProfilerWindow, names, this);
        }

        CountRecorder ICountRecorderProvider.GetCountRecorder()
        {
            return NetworkProfilerBehaviour._sentCounter;
        }
    }

    [System.Serializable]
    [ProfilerModuleMetadata(ModuleNames.RECEIVED)]
    public class ReceivedModule : ProfilerModule, ICountRecorderProvider
    {
        private static readonly ProfilerCounterDescriptor[] k_Counters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor(Names.RECEIVED_COUNT, Counters.Category),
            new ProfilerCounterDescriptor(Names.RECEIVED_BYTES, Counters.Category),
            new ProfilerCounterDescriptor(Names.RECEIVED_PER_SECOND, Counters.Category),
        };

        public ReceivedModule() : base(k_Counters) { }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            var names = new MessageViewController.CounterNames(
                Names.RECEIVED_COUNT,
                Names.RECEIVED_BYTES,
                Names.RECEIVED_PER_SECOND
            );

            return new MessageViewController(ProfilerWindow, names, this);
        }

        CountRecorder ICountRecorderProvider.GetCountRecorder()
        {
            return NetworkProfilerBehaviour._receivedCounter;
        }
    }

    internal interface ICountRecorderProvider
    {
        CountRecorder GetCountRecorder();
    }

    internal sealed class MessageViewController : ProfilerModuleViewController
    {
        private readonly CounterNames _names;
        private readonly ICountRecorderProvider _counterProvider;
        private readonly Columns _columns = new Columns();
        private Label _countLabel;
        private Label _bytesLabel;
        private Label _perSecondLabel;
        private Table _table;
        private VisualElement _toggleBox;
        private Toggle _debugToggle;
        private Toggle _groupMsgToggle;
        private Dictionary<string, Group> _messages;

        public MessageViewController(ProfilerWindow profilerWindow, CounterNames names, ICountRecorderProvider counterProvider) : base(profilerWindow)
        {
            _names = names;
            _counterProvider = counterProvider;
        }

        protected override VisualElement CreateView()
        {
            // unity doesn't catch errors here so we have to wrap in try/catch
            try
            {
                return CreateViewInternal();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        private VisualElement CreateViewInternal()
        {
            var root = new VisualElement();
            root.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            root.style.height = Length.Percent(100);
            root.style.overflow = Overflow.Hidden;

            var summary = new VisualElement();
            _countLabel = AddLabelWithPadding(summary);
            _bytesLabel = AddLabelWithPadding(summary);
            _perSecondLabel = AddLabelWithPadding(summary);
            _perSecondLabel.tooltip = Names.PER_SECOND_TOOLTIP;
            root.Add(summary);
            summary.style.height = Length.Percent(100);
            summary.style.width = 180;
            summary.style.minWidth = 180;
            summary.style.maxWidth = 180;
            summary.style.borderRightColor = Color.white * .4f;//dark grey
            summary.style.borderRightWidth = 3;

            _toggleBox = new VisualElement();
            _toggleBox.style.position = Position.Absolute;
            _toggleBox.style.bottom = 5;
            _toggleBox.style.left = 5;
            _toggleBox.style.unityTextAlign = TextAnchor.LowerLeft;
            summary.Add(_toggleBox);

            _groupMsgToggle = new Toggle();
            _groupMsgToggle.text = "Group Messages";
            _groupMsgToggle.tooltip = "Groups Message by type";
            _groupMsgToggle.value = true;
            _groupMsgToggle.RegisterValueChangedCallback(_ => ReloadData());
            _toggleBox.Add(_groupMsgToggle);

            _debugToggle = new Toggle();
            _debugToggle.text = "Show Fake Messages";
            _debugToggle.tooltip = "Adds fakes message to table to debug layout of table";
            _debugToggle.value = false;
            _debugToggle.RegisterValueChangedCallback(_ => ReloadData());
            _toggleBox.Add(_debugToggle);
#if MIRAGE_PROFILER_DEBUG
            _debugToggle.style.display = DisplayStyle.Flex;
#else
            _debugToggle.style.display = DisplayStyle.None;
#endif

            _table = new Table(_columns, new TableSorter());
            root.Add(_table.VisualElement);

            // Populate the label with the current data for the selected frame. 
            ReloadData();

            // Be notified when the selected frame index in the Profiler Window changes, so we can update the label.
            ProfilerWindow.SelectedFrameIndexChanged += FrameIndexChanged;

            return root;
        }

        private static Label AddLabelWithPadding(VisualElement view)
        {
            var label = new Label() { style = { paddingTop = 8, paddingLeft = 8 } };
            view.Add(label);
            return label;
        }

        private void FrameIndexChanged(long selectedFrameIndex)
        {
            // Update the label with the current data for the newly selected frame.
            ReloadData();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            // Unsubscribe from the Profiler window event that we previously subscribed to.
            ProfilerWindow.SelectedFrameIndexChanged -= FrameIndexChanged;

            base.Dispose(disposing);
        }

        private void ReloadData()
        {
            SetSummary(_countLabel, _names.Count);
            SetSummary(_bytesLabel, _names.Bytes);
            SetSummary(_perSecondLabel, _names.PerSecond);

            ReloadMessages();
        }

        private void SetSummary(Label label, string counterName)
        {
            var frame = (int)ProfilerWindow.selectedFrameIndex;
            var category = ProfilerCategory.Network.Name;
            var value = ProfilerDriver.GetFormattedCounterValue(frame, category, counterName);

            // replace prefix
            var display = counterName.Replace("Received", "").Replace("Sent", "").Trim();
            label.text = $"{display}: {value}";
        }

        private void ReloadMessages()
        {
            _table.Clear();

            if (!TryGetMessages(out var messages))
            {
                AddCantLoadLabel();
                return;
            }

            if (messages.Count == 0)
            {
                AddNoMessagesLabel();
                return;
            }

            _messages = GroupMessages(messages);
            DrawGroups(_messages);

            var expandColumn = _columns.Expand;
            var defaultWidth = expandColumn.Width;
            var width = _groupMsgToggle.value ? defaultWidth : 0;
            _table.ChangeWidth(expandColumn, width, true);
        }

        private bool TryGetMessages(out List<MessageInfo> messages)
        {
            if (_debugToggle.value)
            {
                messages = GenerateDebugMessages();
                return true;
            }

            messages = null;
            var counter = _counterProvider.GetCountRecorder();
            if (counter == null)
                return false;

            var frameIndexStr = ProfilerDriver.GetFormattedCounterValue((int)ProfilerWindow.selectedFrameIndex, ProfilerCategory.Network.Name, Names.INTERNAL_FRAME_COUNTER);
            var frameIndex = 0;
            if (!string.IsNullOrEmpty(frameIndexStr))
                frameIndex = int.Parse(frameIndexStr);

            var frame = counter._frames[frameIndex];
            messages = frame.Messages;

            return true;
        }

        private Dictionary<string, Group> GroupMessages(List<MessageInfo> messages)
        {
            var groups = new Dictionary<string, Group>();
            foreach (var message in messages)
            {
                var name = message.Name;

                if (!groups.TryGetValue(name, out var group))
                {
                    group = new Group(name, this);
                    groups[name] = group;
                }

                group.AddMessage(message);
            }
            return groups;
        }

        private static List<MessageInfo> GenerateDebugMessages()
        {
            var messages = new List<MessageInfo>();
            var order = 0;
            for (var i = 0; i < 5; i++)
            {
                messages.Add(NewInfo(order++, new RpcMessage { netId = (uint)i }, 20 + i, 5));
                messages.Add(NewInfo(order++, new SpawnMessage { netId = (uint)i }, 80 + i, 1));
                messages.Add(NewInfo(order++, new SpawnMessage { netId = (uint)i }, 60 + i, 4));
                messages.Add(NewInfo(order++, new NetworkPingMessage { }, 4, 1));

                static MessageInfo NewInfo(int order, object msg, int bytes, int count)
                {
#if MIRAGE_DIAGNOSTIC_INSTANCE
                        return new MessageInfo(null, msg, bytes, count);
#else
                    return new MessageInfo(new NetworkDiagnostics.MessageInfo(msg, bytes, count), order);
#endif
                }
            }

            return messages;
        }

        private void DrawGroups(Dictionary<string, Group> groups)
        {
            var asGroups = _groupMsgToggle.value;
            foreach (var group in groups.Values)
            {
                if (asGroups)
                {
                    DrawGroupHeader(group);
                }
                else
                {
                    group.Expand(true);
                }
            }
        }

        private void DrawGroupHeader(Group group)
        {
            // draw header
            var head = _table.AddRow();
            head.SetText(_columns.Expand, group.Expanded ? "-" : "+");
            head.SetText(_columns.FullName, group.Name);
            head.SetText(_columns.TotalBytes, group.TotalBytes);
            head.SetText(_columns.Count, group.TotalCount);
            head.SetText(_columns.BytesPerMessage, "");
            head.SetText(_columns.NetId, "");
            group.Head = head;

            var expand = head.GetLabel(_columns.Expand);
            expand.AddManipulator(new Clickable((evt) =>
            {
                group.ToggleExpand();
                group.Head.SetText(_columns.Expand, group.Expanded ? "-" : "+");
            }));

            // will lazy create message if expanded
            group.Expand(group.Expanded);
        }

        /// <param name="messages">Messages to add to table</param>
        /// <param name="createdRows">list to add rows to once created, Can be null</param>
        private void DrawMessages(List<MessageInfo> messages, Row previous = null, List<Row> createdRows = null)
        {
            foreach (var info in messages)
            {
                var row = _table.AddRow(previous);
                // set previous to be new row, so that message are added in order after previous
                previous = row;

                row.SetText(_columns.FullName, info.Name);
                row.SetText(_columns.TotalBytes, info.TotalBytes);
                row.SetText(_columns.Count, info.Count);
                row.SetText(_columns.BytesPerMessage, info.Bytes);
                var netidStr = info.NetId.HasValue ? info.NetId.ToString() : "";
                row.SetText(_columns.NetId, netidStr);

                createdRows?.Add(row);
            }
        }


        private void AddCantLoadLabel()
        {
            var row = _table.AddEmptyRow();
            var ele = AddLabelWithPadding(row.VisualElement);
            ele.style.color = Color.red;
            ele.text = "Can not load messages! (Message list only visible in play mode)";
        }

        private void AddNoMessagesLabel()
        {
            var row = _table.AddEmptyRow();
            var ele = AddLabelWithPadding(row.VisualElement);
            ele.text = "No Messages";
        }

        public class Group
        {
            public readonly List<Row> Rows = new List<Row>();
            public readonly string Name;
            public readonly List<MessageInfo> Messages = new List<MessageInfo>();
            public Row Head;

            private readonly MessageViewController _view;

            public int TotalBytes { get; private set; }
            public int TotalCount { get; private set; }

            public bool Expanded { get; private set; }

            public Group(string name, MessageViewController view)
            {
                Name = name;
                _view = view;
            }

            public void AddMessage(MessageInfo msg)
            {
                Messages.Add(msg);
                TotalBytes += msg.TotalBytes;
                TotalCount += msg.Count;
            }

            public void ToggleExpand()
            {
                Expand(!Expanded);
            }

            public void Expand(bool expanded)
            {
                Expanded = expanded;
                // create rows if needed
                LazyCreateRows();
                foreach (var row in Rows)
                {
                    row.VisualElement.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            public void LazyCreateRows()
            {
                // not visible, do nothing till row is expanded
                if (!Expanded)
                    return;
                // already created
                if (Rows.Count > 0)
                    return;

                _view.DrawMessages(Messages, Head, Rows);

                var backgroundColor = GetBackgroundColor();

                // set the each element of child rows
                foreach (var row in Rows)
                {
                    // set color of labels not whole row, otherwise color will be outside of table as well
                    foreach (var ele in row.GetChildren())
                        ele.style.backgroundColor = backgroundColor;
                }
            }

            private static Color GetBackgroundColor()
            {
                // pick color that is lighter/darker than default editor background
                // todo check if there is a way to get the real color, or do we have to use `isProSkin`?
                return EditorGUIUtility.isProSkin
                    ? (Color)new Color32(56, 56, 56, 255) / 0.8f
                    : (Color)new Color32(194, 194, 194, 255) * .8f;
            }
        }

        public struct CounterNames
        {
            public readonly string Count;
            public readonly string Bytes;
            public readonly string PerSecond;

            public CounterNames(string count, string bytes, string perSecond)
            {
                Count = count;
                Bytes = bytes;
                PerSecond = perSecond;
            }
        }

        internal sealed class Columns : IEnumerable<ColumnInfo>
        {
            private const int EXPAND_WIDTH = 25;
            private const int NAME_WIDTH = 300;
            private const int OTHER_WIDTH = 100;

            public readonly ColumnInfo Expand = new ColumnInfo("+", EXPAND_WIDTH, false);
            public readonly ColumnInfo FullName = new ColumnInfo("Message", NAME_WIDTH, true);
            public readonly ColumnInfo TotalBytes = new ColumnInfo("Total Bytes", OTHER_WIDTH, true);
            public readonly ColumnInfo Count = new ColumnInfo("Count", OTHER_WIDTH, true);
            public readonly ColumnInfo BytesPerMessage = new ColumnInfo("Bytes", OTHER_WIDTH, true);
            public readonly ColumnInfo NetId = new ColumnInfo("Net id", OTHER_WIDTH, true);

            public IEnumerator<ColumnInfo> GetEnumerator()
            {
                yield return Expand;
                yield return FullName;
                yield return TotalBytes;
                yield return Count;
                yield return BytesPerMessage;
                yield return NetId;
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class TableSorter : ITableSorter
        {
            private SortHeader _header;
            private Table _table;

            public void Sort(Table table, SortHeader header)
            {
                _header = header;
                _table = table;

                if (header == null)
                    return;

                if (table.ContainsEmptyRows)
                {
                    Debug.LogWarning("Can't sort when there are empty rows");
                    return;
                }

                table.Rows.Sort(Sort);
                foreach (var row in table.Rows)
                {
                    // put at end of parent list
                    // this causes rows to be moved to the end of the layout as they are in the List
                    row.VisualElement.BringToFront();
                }
            }

            private int Sort(Row x, Row y)
            {
                // make sure header stays at top
                if (x == _table.Header)
                    return -1;
                if (y == _table.Header)
                    return 1;

                var a = x.GetLabel(_header.Info);
                var b = y.GetLabel(_header.Info);

                var aText = a.text;
                var bText = b.text;

                var sort = 0;
                // if both numbers then sort by number instead of string
                if (int.TryParse(aText, out var aNum) && int.TryParse(bText, out var bNum))
                {
                    sort = aNum.CompareTo(bNum);
                }
                else
                {
                    sort = a.text.CompareTo(b.text);
                }


                if (_header.SortMode == SortMode.Descending)
                {
                    return -sort;
                }
                else
                {
                    return sort;
                }
            }
        }
    }
}
