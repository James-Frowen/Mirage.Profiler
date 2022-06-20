using System.Collections.Generic;
using Mirage.NetworkProfiler.ModuleGUI;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mirage.NetworkProfiler
{
    [DefaultExecutionOrder(10000)] // last
    public class NetworkProfilerBehaviour : MonoBehaviour
    {
        public NetworkServer Server;

        internal static CountRecorder sentCounter;
        internal static CountRecorder receivedCounter;

        
        const int frameCount = 300; // todo find a way to get real frame count
        private void Start()
        {

            sentCounter = new CountRecorder(frameCount, Server, Counters.SentMessagesCount, Counters.SentMessagesBytes);
            receivedCounter = new CountRecorder(frameCount, Server, Counters.ReceiveMessagesCount, Counters.ReceiveMessagesBytes);

            NetworkDiagnostics.InMessageEvent += receivedCounter.OnMessage;
            NetworkDiagnostics.OutMessageEvent += sentCounter.OnMessage;
        }
        private void OnDestroy()
        {
            if (receivedCounter != null)
                NetworkDiagnostics.InMessageEvent -= receivedCounter.OnMessage;
            if (sentCounter != null)
                NetworkDiagnostics.OutMessageEvent -= sentCounter.OnMessage;
        }

        private void LateUpdate()
        {
            if (Server == null || !Server.Active)
                return;

            Counters.PlayerCount.Sample(Server.Players.Count);
            sentCounter.EndFrame();
            receivedCounter.EndFrame();
            Counters.InternalFrameCounter.Sample(Time.frameCount % frameCount);
        }
    }

    public class Names
    {
        internal const string INTERNAL_FRAME_COUNTER = "INTERNAL_FRAME_COUNTER";

        public const string PLAYER_COUNT = "Player Count";
        public const string MESSAGES_SENT_COUNT = "Sent Messages";
        public const string MESSAGES_SENT_BYTES = "Sent Bytes";
        public const string MESSAGES_RECEIVED_COUNT = "Received Messages";
        public const string MESSAGES_RECEIVED_BYTES = "Received Bytes";
    }
    internal class Counters
    {
        public static readonly ProfilerCategory Category = ProfilerCategory.Network;

        internal static readonly ProfilerCounter<int> InternalFrameCounter;
        public static readonly ProfilerCounter<int> PlayerCount;
        public static readonly ProfilerCounter<int> SentMessagesCount;
        public static readonly ProfilerCounter<int> SentMessagesBytes;
        public static readonly ProfilerCounter<int> ReceiveMessagesCount;
        public static readonly ProfilerCounter<int> ReceiveMessagesBytes;

        static Counters()
        {
            ProfilerMarkerDataUnit count = ProfilerMarkerDataUnit.Count;
            ProfilerMarkerDataUnit bytes = ProfilerMarkerDataUnit.Bytes;

            InternalFrameCounter = new ProfilerCounter<int>(Category, Names.INTERNAL_FRAME_COUNTER, count);
            PlayerCount = new ProfilerCounter<int>(Category, Names.PLAYER_COUNT, count);
            SentMessagesCount = new ProfilerCounter<int>(Category, Names.MESSAGES_SENT_COUNT, count);
            SentMessagesBytes = new ProfilerCounter<int>(Category, Names.MESSAGES_SENT_BYTES, bytes);
            ReceiveMessagesCount = new ProfilerCounter<int>(Category, Names.MESSAGES_RECEIVED_COUNT, count);
            ReceiveMessagesBytes = new ProfilerCounter<int>(Category, Names.MESSAGES_RECEIVED_BYTES, bytes);
        }
    }
    internal class Frame
    {
        public readonly List<NetworkDiagnostics.MessageInfo> Messages = new List<NetworkDiagnostics.MessageInfo>();
        public int Bytes;
    }
    class CountRecorder
    {
        readonly ProfilerCounter<int> profilerCount;
        readonly ProfilerCounter<int> profilerBytes;
        readonly object instance;
        internal readonly Frame[] frames;

        int count;
        int bytes;


        public CountRecorder(int bufferSize, object instance, ProfilerCounter<int> profilerCount, ProfilerCounter<int> profilerBytes)
        {
            this.instance = instance;
            this.profilerCount = profilerCount;
            this.profilerBytes = profilerBytes;
            frames = new Frame[bufferSize];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = new Frame();
        }



        public void OnMessage(NetworkDiagnostics.MessageInfo obj)
        {
            // using the profiler-window branch of mirage to allow NetworkDiagnostics to say which server/client is sent the event
#if MIRAGE_DIAGNOSTIC_INSTANCE
            if (obj.instance != instance)
                return;
#endif

            // Debug.Log($"{Time.frameCount % frames.Length} {NetworkProfilerModuleViewController.CreateTextForMessageInfo(obj)}");

            count += obj.count;
            bytes += obj.bytes * obj.count;
            Frame frame = frames[Time.frameCount % frames.Length];
            frame.Messages.Add(obj);
            frame.Bytes++;
        }

        public void EndFrame()
        {
            profilerCount.Sample(count);
            profilerBytes.Sample(bytes);
            count = 0;
            bytes = 0;
            Frame frame = frames[(Time.frameCount + 1) % frames.Length];
            frame.Messages.Clear();
        }
    }
}
namespace Mirage.NetworkProfiler.ModuleGUI
{
    [System.Serializable]
    [ProfilerModuleMetadata("Network Profiler")]
    public class NetworkProfilerModule : ProfilerModule
    {
        static readonly ProfilerCounterDescriptor[] k_Counters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor(Names.PLAYER_COUNT, Counters.Category),
            new ProfilerCounterDescriptor(Names.MESSAGES_SENT_COUNT, Counters.Category),
            new ProfilerCounterDescriptor(Names.MESSAGES_SENT_BYTES, Counters.Category),
            new ProfilerCounterDescriptor(Names.MESSAGES_RECEIVED_COUNT, Counters.Category),
            new ProfilerCounterDescriptor(Names.MESSAGES_RECEIVED_BYTES, Counters.Category),
        };

        public NetworkProfilerModule() : base(k_Counters) { }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new NetworkProfilerModuleViewController(ProfilerWindow);
        }
    }

    public class NetworkProfilerModuleViewController : ProfilerModuleViewController
    {
        // Define a label, which will display the total particle count for tank trails in the selected frame.
        Label PlayerCount;
        Label MessagesSentCount;
        Label MessagesSentBytes;
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
                var frameIndexStr = ProfilerDriver.GetFormattedCounterValue((int)ProfilerWindow.selectedFrameIndex, ProfilerCategory.Network.Name, Names.INTERNAL_FRAME_COUNTER);
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
