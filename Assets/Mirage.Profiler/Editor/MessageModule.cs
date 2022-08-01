using System;
using System.Collections.Generic;
using System.IO;
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
        private static readonly ProfilerCounterDescriptor[] counters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor(Names.SENT_COUNT, Counters.Category),
            new ProfilerCounterDescriptor(Names.SENT_BYTES, Counters.Category),
            new ProfilerCounterDescriptor(Names.SENT_PER_SECOND, Counters.Category),
        };

        public SentModule() : base(counters) { }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            var names = new MessageViewController.CounterNames(
                Names.SENT_COUNT,
                Names.SENT_BYTES,
                Names.SENT_PER_SECOND
            );

            return new MessageViewController(ProfilerWindow, names, "Sent", this);
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
        private static readonly ProfilerCounterDescriptor[] counters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor(Names.RECEIVED_COUNT, Counters.Category),
            new ProfilerCounterDescriptor(Names.RECEIVED_BYTES, Counters.Category),
            new ProfilerCounterDescriptor(Names.RECEIVED_PER_SECOND, Counters.Category),
        };

        public ReceivedModule() : base(counters) { }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            var names = new MessageViewController.CounterNames(
                Names.RECEIVED_COUNT,
                Names.RECEIVED_BYTES,
                Names.RECEIVED_PER_SECOND
            );

            return new MessageViewController(ProfilerWindow, names, "Received", this);
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

    internal class SaveDataLoader
    {
        public static void Save(string path, SavedData data)
        {
            CheckDir(path);

            var text = JsonUtility.ToJson(data);
            File.WriteAllText(path, text);
        }

        public static SavedData Load(string path)
        {
            CheckDir(path);

            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
                return JsonUtility.FromJson<SavedData>(text);
            }
            else
            {
                return new SavedData();
            }
        }

        private static void CheckDir(string path)
        {
            // check dir exists
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }
    [Serializable]
    internal class SavedData
    {
        /// <summary>
        /// Message from each frame so they can survive domain reload
        /// </summary>
        public Frame[] Frames;

        /// <summary>
        /// Active sort header
        /// </summary>
        public string SortHeader;

        public SortMode SortMode;

        /// <summary>
        /// Which Message groups are expanded
        /// </summary>
        public List<string> Expanded;

        public (ColumnInfo, SortMode) GetSortHeader(MessageViewController.Columns columns)
        {
            foreach (var c in columns)
            {
                if (SortHeader == c.Header)
                {
                    return (c, SortMode);
                }
            }

            return (null, SortMode.None);
        }
        public void SetSortHeader(SortHeader header)
        {
            if (header == null)
                SortHeader = "";
            else
            {
                SortHeader = header.Info.Header;
                SortMode = header.SortMode;
            }
        }
    }
    internal sealed class MessageViewController : ProfilerModuleViewController
    {
        private readonly string _saveDataPath;
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
        private SavedData _savedData;

        public MessageViewController(ProfilerWindow profilerWindow, CounterNames names, string name, ICountRecorderProvider counterProvider) : base(profilerWindow)
        {
            _names = names;
            _counterProvider = counterProvider;

            var userSettingsFolder = Path.GetFullPath("UserSettings");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            _saveDataPath = Path.Join(userSettingsFolder, "Mirage.Profiler", $"{name}.json");
            Debug.Log($"Load from {_saveDataPath}");
            _savedData = SaveDataLoader.Load(_saveDataPath);
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

            // todo allow selection of multiple frames
            //var frameSlider = new MinMaxSlider();
            //frameSlider.highLimit = 300;
            //frameSlider.lowLimit = 1;
            //frameSlider.value = Vector2.one;
            //frameSlider.RegisterValueChangedCallback(_ => Debug.Log(frameSlider.value));
            //_toggleBox.Add(frameSlider);

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

            _table = new Table(_columns, new TableSorter(this));
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

            Debug.Log($"Save to {_saveDataPath}");
            SaveDataLoader.Save(_saveDataPath, _savedData);

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
            var asGroups = _groupMsgToggle.value;
            foreach (var message in messages)
            {
                string name;
                if (asGroups)
                    name = message.Name;
                else
                    name = "all_messages";

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
            public readonly List<DrawnMessage> Messages = new List<DrawnMessage>();
            public Row Head;

            private readonly MessageViewController _view;

            public int TotalBytes { get; private set; }
            public int TotalCount { get; private set; }
            public int Order { get; private set; }

            public bool Expanded { get; private set; }

            public Group(string name, MessageViewController view)
            {
                Name = name;
                _view = view;
                // start at max, then take min each time message is added
                Order = int.MaxValue;
            }

            public void AddMessage(MessageInfo msg)
            {
                Messages.Add(new DrawnMessage { Info = msg });
                TotalBytes += msg.TotalBytes;
                TotalCount += msg.Count;
                Order = Math.Min(Order, msg.Order);
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

                DrawMessages();
            }


            /// <param name="messages">Messages to add to table</param>
            /// <param name="createdRows">list to add rows to once created, Can be null</param>
            private void DrawMessages()
            {
                var table = _view._table;
                var columns = _view._columns;

                var previous = Head;
                var backgroundColor = GetBackgroundColor();

                foreach (var drawn in Messages)
                {
                    var row = table.AddRow(previous);
                    Rows.Add(row);

                    // set previous to be new row, so that message are added in order after previous
                    previous = row;

                    drawn.Row = row;
                    var info = drawn.Info;

                    row.SetText(columns.FullName, info.Name);
                    row.SetText(columns.TotalBytes, info.TotalBytes);
                    row.SetText(columns.Count, info.Count);
                    row.SetText(columns.BytesPerMessage, info.Bytes);
                    row.SetText(columns.NetId, info.NetId.HasValue ? info.NetId.ToString() : "");

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

        public class DrawnMessage
        {
            public MessageInfo Info;
            public Row Row;
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
            private readonly MessageViewController _view;
            private readonly Columns _columns;

            private ColumnInfo _sortHeader;
            private SortMode _sortMode;


            public TableSorter(MessageViewController view)
            {
                _view = view;
                _columns = _view._columns;
            }

            public void Sort(Table table, SortHeader header)
            {
                Debug.Assert(table == _view._table);

                _view._savedData.SetSortHeader(header);
                Sort();
            }
            public void Sort()
            {
                (_sortHeader, _sortMode) = _view._savedData.GetSortHeader(_view._columns);

                if (_view._table.ContainsEmptyRows)
                {
                    Debug.LogWarning("Can't sort when there are empty rows");
                    return;
                }

                var messages = _view._messages;
                var groups = new List<Group>(messages.Values);

                // sort all groups and their messages
                groups.Sort(ApplyMode<Group>(CompareGroup));
                foreach (var group in groups)
                {
                    group.Messages.Sort(ApplyMode<DrawnMessage>(CompareDrawn));
                }


                // apply sort to table
                foreach (var group in groups)
                {
                    // use BringToFront so that each new element is placed after the last one, bring them all to their correct position

                    // head might be null if messages are ungrouped
                    group.Head?.VisualElement.BringToFront();
                    foreach (var msg in group.Messages)
                    {
                        // row might be null before it is drawn for first time
                        msg.Row?.VisualElement.BringToFront();
                    }
                }
            }

            private Comparison<T> ApplyMode<T>(Comparison<T> comparison)
            {
                return (x, y) =>
                {
                    var sort = comparison.Invoke(x, y);

                    // flip order if Descending
                    if (_sortMode == SortMode.Descending)
                        return -sort;

                    return sort;
                };
            }

            private int CompareGroup(Group x, Group y)
            {
                // find matching coloum and sort using it
                if (IsHeader(_columns.FullName))
                    return Compare(x, y, m => m.Name);

                if (IsHeader(_columns.TotalBytes))
                    return Compare(x, y, m => m.TotalBytes);

                if (IsHeader(_columns.Count))
                    return Compare(x, y, m => m.TotalCount);

                // else just use order
                // for example if someone is sorting by netid
                return x.Order.CompareTo(y.Order);
            }

            private int CompareDrawn(DrawnMessage x, DrawnMessage y)
            {
                return CompareMessage(x.Info, y.Info);
            }
            private int CompareMessage(MessageInfo x, MessageInfo y)
            {
                // find matching coloum and sort using it
                if (IsHeader(_columns.FullName))
                    return Compare(x, y, m => m.Name);

                if (IsHeader(_columns.TotalBytes))
                    return Compare(x, y, m => m.TotalBytes);

                if (IsHeader(_columns.Count))
                    return Compare(x, y, m => m.Count);

                if (IsHeader(_columns.BytesPerMessage))
                    return Compare(x, y, m => m.Bytes);

                if (IsHeader(_columns.NetId))
                    return Compare(x, y, m => m.NetId.GetValueOrDefault());

                // else, header not found just use order
                return x.Order.CompareTo(y.Order);
            }

            private bool IsHeader(ColumnInfo info)
            {
                return _sortHeader != null && _sortHeader == info;
            }

            private int Compare<T>(Group x, Group y, Func<Group, T> func) where T : IComparable<T>
            {
                var xValue = func.Invoke(x);
                var yValue = func.Invoke(y);
                return xValue.CompareTo(yValue);
            }
            private int Compare<T>(MessageInfo x, MessageInfo y, Func<MessageInfo, T> func) where T : IComparable<T>
            {
                var xValue = func.Invoke(x);
                var yValue = func.Invoke(y);
                return xValue.CompareTo(yValue);
            }
        }
    }
}
