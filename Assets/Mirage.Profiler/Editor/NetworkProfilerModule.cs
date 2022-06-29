using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.UIElements;

namespace Mirage.NetworkProfiler.ModuleGUI
{
    [System.Serializable]
    [ProfilerModuleMetadata("Network Profiler Sent")]
    public class NetworkProfilerSentModule : ProfilerModule
    {
        static readonly ProfilerCounterDescriptor[] k_Counters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor(Names.MESSAGES_SENT_COUNT, Counters.Category),
            new ProfilerCounterDescriptor(Names.MESSAGES_SENT_BYTES, Counters.Category),
            new ProfilerCounterDescriptor(Names.MESSAGES_SENT_PER_SECOND, Counters.Category),
        };

        public NetworkProfilerSentModule() : base(k_Counters) { }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new NetworkProfilerModuleViewController(ProfilerWindow);
        }
    }

    [System.Serializable]
    [ProfilerModuleMetadata("Network Profiler Received")]
    public class NetworkProfilerReceivedModule : ProfilerModule
    {
        static readonly ProfilerCounterDescriptor[] k_Counters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor(Names.MESSAGES_RECEIVED_COUNT, Counters.Category),
            new ProfilerCounterDescriptor(Names.MESSAGES_RECEIVED_BYTES, Counters.Category),
            new ProfilerCounterDescriptor(Names.MESSAGES_RECEIVED_PER_SECOND, Counters.Category),
        };

        public NetworkProfilerReceivedModule() : base(k_Counters) { }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new NetworkProfilerModuleViewController(ProfilerWindow);
        }
    }

    public class NetworkProfilerModuleViewController : ProfilerModuleViewController
    {
        // Define a label, which will display the total particle count for tank trails in the selected frame.
        Label PlayerCount;
        Label CountLabel;
        Label BytesLabel;
        Label MessagesReceivedCount;
        Label MessagesReceivedBytes;
        private VisualElement messageView;

        // Define a constructor for the view controller, which calls the base constructor with the Profiler Window passed from the module.
        public NetworkProfilerModuleViewController(ProfilerWindow profilerWindow) : base(profilerWindow) { }

        // Override CreateView to build the custom module details panel.
        protected override VisualElement CreateView()
        {
            var root = new VisualElement();
            VisualElement dataView = CreateDataView();

            messageView = AddMessageView();
            root.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            root.Add(dataView);
            root.Add(messageView);

            // Populate the label with the current data for the selected frame. 
            ReloadData();

            // Be notified when the selected frame index in the Profiler Window changes, so we can update the label.
            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            return root;
        }

        void OnSelectedFrameIndexChanged(long selectedFrameIndex)
        {
            // Update the label with the current data for the newly selected frame.
            ReloadData();
        }

        private VisualElement CreateDataView()
        {
            var dataView = new VisualElement();
            PlayerCount = AddLabelWithPadding(dataView);
            MessagesSentCount = AddLabelWithPadding(dataView);
            MessagesSentBytes = AddLabelWithPadding(dataView);
            MessagesReceivedCount = AddLabelWithPadding(dataView);
            MessagesReceivedBytes = AddLabelWithPadding(dataView);
            return dataView;
        }

        private static VisualElement AddMessageView()
        {
            return new VisualElement();
        }

        static Label AddLabelWithPadding(VisualElement view)
        {
            var label = new Label() { style = { paddingTop = 8, paddingLeft = 8 } };
            view.Add(label);
            return label;
        }

        // Override Dispose to do any cleanup of the view when it is destroyed. This is a standard C# Dispose pattern.
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
            SetText(PlayerCount, Names.PLAYER_COUNT);
            SetText(MessagesSentCount, Names.MESSAGES_SENT_COUNT);
            SetText(MessagesSentBytes, Names.MESSAGES_SENT_BYTES);
            SetText(MessagesReceivedCount, Names.MESSAGES_RECEIVED_COUNT);
            SetText(MessagesReceivedBytes, Names.MESSAGES_RECEIVED_BYTES);

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
            messageView.Clear();

            int count = 0;
            Frame frame = default;
            if (NetworkProfilerBehaviour.sentCounter != null)
            {
                string frameIndexStr = ProfilerDriver.GetFormattedCounterValue((int)ProfilerWindow.selectedFrameIndex, ProfilerCategory.Network.Name, Names.INTERNAL_FRAME_COUNTER);
                int frameIndex = 0;
                if (!string.IsNullOrEmpty(frameIndexStr))
                    frameIndex = int.Parse(frameIndexStr);

                frame = NetworkProfilerBehaviour.sentCounter.frames[frameIndex];
                count = frame.Messages.Count;


                // var label = new Label() { style = { paddingTop = 8, paddingLeft = 8 } };
                // messageView.Add(label);
                // label.text = $"DebugIndex: {frameIndex}";
            }

            if (count == 0)
            {
                var label = new Label() { style = { paddingTop = 8, paddingLeft = 8 } };
                messageView.Add(label);
                label.text = $"No messages";
                return;
            }

            foreach (NetworkDiagnostics.MessageInfo message in frame.Messages)
            {
                var label = new Label() { style = { paddingTop = 8, paddingLeft = 8 } };
                messageView.Add(label);
                string text = CreateTextForMessageInfo(message);
                label.text = text;
            }
        }

        public static string CreateTextForMessageInfo(NetworkDiagnostics.MessageInfo message)
        {
            string fullName = message.message.GetType().FullName;
            int bytes = message.bytes;
            int count = message.count;
            int totalBytes = bytes * count;

            uint? netid = default;
            int? compIndex = default;
            if (message.message is RpcMessage rpc1)
            {
                netid = rpc1.netId;
                compIndex = rpc1.componentIndex;
            }
            if (message.message is ServerRpcMessage rpc2)
            {
                netid = rpc2.netId;
                compIndex = rpc2.componentIndex;
            }
            if (message.message is ServerRpcWithReplyMessage rpc3)
            {
                netid = rpc3.netId;
                compIndex = rpc3.componentIndex;
            }
            if (message.message is UpdateVarsMessage vars)
            {
                netid = vars.netId;
            }

            string netidText = netid.HasValue ? $"netid={netid.Value}" : string.Empty;
            string compIdText = compIndex.HasValue ? $"compId={compIndex.Value}" : string.Empty;
            string text = $"{fullName} [{bytes}*{count}={totalBytes}] {netidText} {compIdText}";
            return text;
        }
    }
}
