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

        public MessageViewController(ProfilerWindow profilerWindow, CounterNames names, ICountRecorderProvider counterProvider) : base(profilerWindow)
        {
            _names = names;
            _counterProvider = counterProvider;
        }

        protected override VisualElement CreateView()
        {
            var root = new VisualElement();
            root.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            root.style.height = Length.Percent(100);

            var labels = CreateLabels();
            root.Add(labels);
            labels.style.height = Length.Percent(100);
            labels.style.width = 180;
            labels.style.minWidth = 180;
            labels.style.maxWidth = 180;
            labels.style.borderRightColor = Color.white * .4f;//dark grey
            labels.style.borderRightWidth = 3;

            _toggleBox = new VisualElement();
            _toggleBox.style.position = Position.Absolute;
            _toggleBox.style.bottom = 5;
            _toggleBox.style.left = 5;
            _toggleBox.style.unityTextAlign = TextAnchor.LowerLeft;
            labels.Add(_toggleBox);


            _groupMsgToggle = new Toggle();
            _groupMsgToggle.text = "Group Messages";
            _groupMsgToggle.tooltip = "Groups Message by type";
            _groupMsgToggle.value = false;
            _groupMsgToggle.RegisterValueChangedCallback(GroupMsgToggleChanged);
            _toggleBox.Add(_groupMsgToggle);


            _debugToggle = new Toggle();
            _debugToggle.text = "Show Fake Messages";
            _debugToggle.tooltip = "Adds fakes message to table to debug layout of table";
            _debugToggle.value = true;
            _debugToggle.RegisterValueChangedCallback(DebugToggleChanged);
            _toggleBox.Add(_debugToggle);
#if MIRAGE_PROFILER_DEBUG
            _debugToggle.style.display = DisplayStyle.Flex;
#else
            _debugToggle.style.display = DisplayStyle.None;
#endif


            _table = new Table(_columns);
            root.Add(_table.VisualElement);

            // Populate the label with the current data for the selected frame. 
            ReloadData();

            // Be notified when the selected frame index in the Profiler Window changes, so we can update the label.
            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            root.style.overflow = Overflow.Hidden;
            return root;
        }

        private void DebugToggleChanged(ChangeEvent<bool> _) => ReloadData();
        private void GroupMsgToggleChanged(ChangeEvent<bool> _) => ReloadData();

        private VisualElement CreateLabels()
        {
            var dataView = new VisualElement();
            _countLabel = AddLabelWithPadding(dataView);
            _bytesLabel = AddLabelWithPadding(dataView);
            _perSecondLabel = AddLabelWithPadding(dataView);
            _perSecondLabel.tooltip = Names.PER_SECOND_TOOLTIP;
            return dataView;
        }

        private static Label AddLabelWithPadding(VisualElement view)
        {
            var label = new Label() { style = { paddingTop = 8, paddingLeft = 8 } };
            view.Add(label);
            return label;
        }

        private void OnSelectedFrameIndexChanged(long selectedFrameIndex)
        {
            // Update the label with the current data for the newly selected frame.
            ReloadData();
        }


        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            // Unsubscribe from the Profiler window event that we previously subscribed to.
            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;

            base.Dispose(disposing);
        }

        private void ReloadData()
        {
            SetText(_countLabel, _names.Count);
            SetText(_bytesLabel, _names.Bytes);
            SetText(_perSecondLabel, _names.PerSecond);

            ReloadMessages();
        }

        private void SetText(Label label, string name)
        {
            var frame = (int)ProfilerWindow.selectedFrameIndex;
            var category = ProfilerCategory.Network.Name;
            var value = ProfilerDriver.GetFormattedCounterValue(frame, category, name);

            label.text = $"{name}: {value}";
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

            if (_groupMsgToggle.value)
            {
                var groups = GroupMessages(messages);
                DrawGroups(groups);
            }
            else
            {
                DrawMessages(messages);
            }

            var expandColumn = _columns.Expand;
            var defaultWidth = expandColumn.Width;
            var width = _groupMsgToggle.value ? defaultWidth : 0;
            _table.ChangeWidth(expandColumn, width, true);
        }

        private void DrawMessages(List<NetworkDiagnostics.MessageInfo> messages)
        {
            foreach (var info in messages)
            {
                var row = _table.AddRow();
                row.SetText(_columns.FullName, info.message.GetType().FullName);
                row.SetText(_columns.TotalBytes, info.bytes * info.count);
                row.SetText(_columns.Count, info.count);
                row.SetText(_columns.BytesPerMessage, info.bytes);
                var netid = GetNetId(info.message);
                var netidStr = netid.HasValue ? netid.ToString() : "";
                row.SetText(_columns.NetId, netidStr);
            }
        }

        private void DrawGroups(Dictionary<Type, List<NetworkDiagnostics.MessageInfo>> groups)
        {
            foreach (var group in groups)
            {
                DrawMessages(group.Value);
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

        private bool TryGetMessages(out List<NetworkDiagnostics.MessageInfo> messages)
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

        private Dictionary<Type, List<NetworkDiagnostics.MessageInfo>> GroupMessages(List<NetworkDiagnostics.MessageInfo> messages)
        {
            var groups = new Dictionary<Type, List<NetworkDiagnostics.MessageInfo>>();
            foreach (var message in messages)
            {
                var type = message.message.GetType();
                if (!groups.TryGetValue(type, out var group))
                {
                    group = new List<NetworkDiagnostics.MessageInfo>();
                    groups[type] = group;
                }

                group.Add(message);
            }
            return groups;
        }

        private static List<NetworkDiagnostics.MessageInfo> GenerateDebugMessages()
        {
            var messages = new List<NetworkDiagnostics.MessageInfo>();
            for (var i = 0; i < 5; i++)
            {
                messages.Add(NewInfo(new RpcMessage { netId = (uint)i }, 20, 5));
                messages.Add(NewInfo(new SpawnMessage { netId = (uint)i }, 80, 1));
                messages.Add(NewInfo(new SpawnMessage { netId = (uint)i }, 60, 4));
                messages.Add(NewInfo(new NetworkPingMessage { }, 4, 1));

                static NetworkDiagnostics.MessageInfo NewInfo(object msg, int bytes, int count)
                {
#if MIRAGE_DIAGNOSTIC_INSTANCE
                        return new NetworkDiagnostics.MessageInfo(null, msg, bytes, count);
#else
                    return new NetworkDiagnostics.MessageInfo(msg, bytes, count);
#endif
                }
            }

            return messages;
        }

        private static uint? GetNetId(object message)
        {
            switch (message)
            {
                case ServerRpcMessage msg: return msg.netId;
                case ServerRpcWithReplyMessage msg: return msg.netId;
                case RpcMessage msg: return msg.netId;
                case SpawnMessage msg: return msg.netId;
                case RemoveAuthorityMessage msg: return msg.netId;
                case ObjectDestroyMessage msg: return msg.netId;
                case ObjectHideMessage msg: return msg.netId;
                case UpdateVarsMessage msg: return msg.netId;
                default: return default;
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
            private const int Expand_WIDTH = 25;
            private const int NAME_WIDTH = 300;
            private const int OTHER_WIDTH = 100;

            public ColumnInfo Expand = new ColumnInfo("+", Expand_WIDTH);
            public ColumnInfo FullName = new ColumnInfo("Message", NAME_WIDTH);
            public ColumnInfo TotalBytes = new ColumnInfo("Total Bytes", OTHER_WIDTH);
            public ColumnInfo Count = new ColumnInfo("Count", OTHER_WIDTH);
            public ColumnInfo BytesPerMessage = new ColumnInfo("Bytes", OTHER_WIDTH);
            public ColumnInfo NetId = new ColumnInfo("Net id", OTHER_WIDTH);

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
    }
}
