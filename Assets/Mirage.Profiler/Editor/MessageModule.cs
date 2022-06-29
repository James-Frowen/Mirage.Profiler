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
        static readonly ProfilerCounterDescriptor[] k_Counters = new ProfilerCounterDescriptor[]
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
            return NetworkProfilerBehaviour.sentCounter;
        }
    }

    [System.Serializable]
    [ProfilerModuleMetadata(ModuleNames.RECEIVED)]
    public class ReceivedModule : ProfilerModule, ICountRecorderProvider
    {
        static readonly ProfilerCounterDescriptor[] k_Counters = new ProfilerCounterDescriptor[]
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
            return NetworkProfilerBehaviour.receivedCounter;
        }
    }

    internal interface ICountRecorderProvider
    {
        CountRecorder GetCountRecorder();
    }

    internal sealed class MessageViewController : ProfilerModuleViewController
    {
        readonly CounterNames _names;
        readonly ICountRecorderProvider _counterProvider;
        readonly Columns columns = new Columns();

        Label _countLabel;
        Label _bytesLabel;
        Label _perSecondLabel;

        Table table;
        Toggle debugToggle;

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

            VisualElement labels = CreateLabels();
            root.Add(labels);
            labels.style.height = Length.Percent(100);
            labels.style.width = 180;
            labels.style.minWidth = 180;
            labels.style.maxWidth = 180;
            labels.style.borderRightColor = Color.white * .4f;//dark grey
            labels.style.borderRightWidth = 3;

            debugToggle = new Toggle();
            debugToggle.text = "Show Debug Messages";
            debugToggle.value = false;
            debugToggle.style.position = Position.Absolute;
            debugToggle.style.bottom = 5;
            debugToggle.style.left = 5;
            debugToggle.style.unityTextAlign = TextAnchor.LowerLeft;
            debugToggle.RegisterValueChangedCallback(DebugToggleChanged);
            labels.Add(debugToggle);


            table = new Table(columns);
            root.Add(table.VisualElement);

            // Populate the label with the current data for the selected frame. 
            ReloadData();

            // Be notified when the selected frame index in the Profiler Window changes, so we can update the label.
            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            root.style.overflow = Overflow.Hidden;
            return root;
        }
        void DebugToggleChanged(ChangeEvent<bool> _) => ReloadData();

        private VisualElement CreateLabels()
        {
            var dataView = new VisualElement();
            _countLabel = AddLabelWithPadding(dataView);
            _bytesLabel = AddLabelWithPadding(dataView);
            _perSecondLabel = AddLabelWithPadding(dataView);
            _perSecondLabel.tooltip = Names.PER_SECOND_TOOLTIP;
            return dataView;
        }

        static Label AddLabelWithPadding(VisualElement view)
        {
            var label = new Label() { style = { paddingTop = 8, paddingLeft = 8 } };
            view.Add(label);
            return label;
        }

        void OnSelectedFrameIndexChanged(long selectedFrameIndex)
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

        void ReloadData()
        {
            SetText(_countLabel, _names.Count);
            SetText(_bytesLabel, _names.Bytes);
            SetText(_perSecondLabel, _names.PerSecond);

            reloadMessages();
        }

        void SetText(Label label, string name)
        {
            int frame = (int)ProfilerWindow.selectedFrameIndex;
            string category = ProfilerCategory.Network.Name;
            string value = ProfilerDriver.GetFormattedCounterValue(frame, category, name);

            label.text = $"{name}: {value}";
        }


        private void reloadMessages()
        {
            table.Clear();

            if (!TryGetMessages(out List<NetworkDiagnostics.MessageInfo> messages))
            {
                AddCantLoadLabel();
                return;
            }

            if (messages.Count == 0)
            {
                AddNoMessagesLabel();
                return;
            }

            foreach (NetworkDiagnostics.MessageInfo info in messages)
            {
                Row row = table.AddRow();
                row.AddElement(columns.FullName, info.message.GetType().FullName);
                row.AddElement(columns.TotalBytes, info.bytes * info.count);
                row.AddElement(columns.Count, info.count);
                row.AddElement(columns.BytesPerMessage, info.bytes);
                uint? netid = GetNetId(info.message);
                string netidStr = netid.HasValue ? netid.ToString() : "";
                row.AddElement(columns.NetId, netidStr);
            }
        }

        private void AddCantLoadLabel()
        {
            Row row = table.AddRow();
            Label ele = AddLabelWithPadding(row.VisualElement);
            ele.style.color = Color.red;
            ele.text = "Can not load messages! (Message list only visible in play mode)";
        }

        private void AddNoMessagesLabel()
        {
            Row row = table.AddRow();
            Label ele = AddLabelWithPadding(row.VisualElement);
            ele.text = "No Messages";
        }

        private bool TryGetMessages(out List<NetworkDiagnostics.MessageInfo> messages)
        {
            if (debugToggle.value)
            {
                messages = new List<NetworkDiagnostics.MessageInfo>();

                for (int i = 0; i < 5; i++)
                {
                    messages.Add(new NetworkDiagnostics.MessageInfo(null, new RpcMessage { netId = (uint)i }, 20, 5));
                    messages.Add(new NetworkDiagnostics.MessageInfo(null, new SpawnMessage { netId = (uint)i }, 80, 1));
                    messages.Add(new NetworkDiagnostics.MessageInfo(null, new SpawnMessage { netId = (uint)i }, 60, 4));
                    messages.Add(new NetworkDiagnostics.MessageInfo(null, new NetworkPingMessage { }, 4, 1));
                }

                return true;
            }


            messages = null;
            CountRecorder counter = _counterProvider.GetCountRecorder();
            if (counter == null)
                return false;

            string frameIndexStr = ProfilerDriver.GetFormattedCounterValue((int)ProfilerWindow.selectedFrameIndex, ProfilerCategory.Network.Name, Names.INTERNAL_FRAME_COUNTER);
            int frameIndex = 0;
            if (!string.IsNullOrEmpty(frameIndexStr))
                frameIndex = int.Parse(frameIndexStr);

            Frame frame = counter.frames[frameIndex];
            int count = frame.Messages.Count;

            messages = frame.Messages;
            return true;
        }

        static uint? GetNetId(object message)
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
    }

    sealed class Columns : IEnumerable<ColumnInfo>
    {
        const int NAME_WIDTH = 300;
        const int OTHER_WIDTH = 100;

        public ColumnInfo FullName = new ColumnInfo("Message", NAME_WIDTH);
        public ColumnInfo TotalBytes = new ColumnInfo("Total Bytes", OTHER_WIDTH);
        public ColumnInfo Count = new ColumnInfo("Count", OTHER_WIDTH);
        public ColumnInfo BytesPerMessage = new ColumnInfo("Bytes", OTHER_WIDTH);
        public ColumnInfo NetId = new ColumnInfo("Net id", OTHER_WIDTH);

        public IEnumerator<ColumnInfo> GetEnumerator()
        {
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
