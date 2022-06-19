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

        CountRecorder sentCounter;
        CountRecorder receivedCounter;

        private void Start()
        {
            sentCounter = new CountRecorder(Server, Counters.SentMessagesCount, Counters.SentMessagesBytes);
            receivedCounter = new CountRecorder(Server, Counters.ReceiveMessagesCount, Counters.ReceiveMessagesBytes);

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
            sentCounter.Flush();
            receivedCounter.Flush();
        }
    }

    public class Names
    {
        public const string PLAYER_COUNT = "Player Count";
        public const string MESSAGES_SENT_COUNT = "Sent Messages";
        public const string MESSAGES_SENT_BYTES = "Sent Bytes";
        public const string MESSAGES_RECEIVED_COUNT = "Received Messages";
        public const string MESSAGES_RECEIVED_BYTES = "Received Bytes";
    }
    internal class Counters
    {
        public static readonly ProfilerCategory Category = ProfilerCategory.Network;

        public static readonly ProfilerCounter<int> PlayerCount;
        public static readonly ProfilerCounter<int> SentMessagesCount;
        public static readonly ProfilerCounter<int> SentMessagesBytes;
        public static readonly ProfilerCounter<int> ReceiveMessagesCount;
        public static readonly ProfilerCounter<int> ReceiveMessagesBytes;

        static Counters()
        {
            ProfilerMarkerDataUnit count = ProfilerMarkerDataUnit.Count;
            ProfilerMarkerDataUnit bytes = ProfilerMarkerDataUnit.Bytes;

            PlayerCount = new ProfilerCounter<int>(Category, Names.PLAYER_COUNT, count);
            SentMessagesCount = new ProfilerCounter<int>(Category, Names.MESSAGES_SENT_COUNT, count);
            SentMessagesBytes = new ProfilerCounter<int>(Category, Names.MESSAGES_SENT_BYTES, bytes);
            ReceiveMessagesCount = new ProfilerCounter<int>(Category, Names.MESSAGES_RECEIVED_COUNT, count);
            ReceiveMessagesBytes = new ProfilerCounter<int>(Category, Names.MESSAGES_RECEIVED_BYTES, bytes);
        }
    }

    class CountRecorder
    {
        readonly ProfilerCounter<int> profilerCount;
        readonly ProfilerCounter<int> profilerBytes;
        readonly object instance;

        int count;
        int bytes;

        public CountRecorder(object instance, ProfilerCounter<int> profilerCount, ProfilerCounter<int> profilerBytes)
        {
            this.instance = instance;
            this.profilerCount = profilerCount;
            this.profilerBytes = profilerBytes;
        }

        public void OnMessage(NetworkDiagnostics.MessageInfo obj)
        {
            // using the profiler-window branch of mirage to allow NetworkDiagnostics to say which server/client is sent the event
#if MIRAGE_DIAGNOSTIC_INSTANCE
            if (obj.instance != instance)
                return;
#endif

            count += obj.count;
            bytes += obj.bytes;
        }

        public void Flush()
        {
            profilerCount.Sample(count);
            profilerBytes.Sample(bytes);
            count = 0;
            bytes = 0;
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

        // Define a constructor for the view controller, which calls the base constructor with the Profiler Window passed from the module.
        public NetworkProfilerModuleViewController(ProfilerWindow profilerWindow) : base(profilerWindow) { }

        // Override CreateView to build the custom module details panel.
        protected override VisualElement CreateView()
        {
            var view = new VisualElement();
            PlayerCount = AddLabelWithPadding(view);
            MessagesSentCount = AddLabelWithPadding(view);
            MessagesSentBytes = AddLabelWithPadding(view);
            MessagesReceivedCount = AddLabelWithPadding(view);
            MessagesReceivedBytes = AddLabelWithPadding(view);

            // Populate the label with the current data for the selected frame. 
            ReloadData();

            // Be notified when the selected frame index in the Profiler Window changes, so we can update the label.
            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            return view;

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
        }
        void SetText(Label label, string name)
        {
            int frame = (int)ProfilerWindow.selectedFrameIndex;
            string category = ProfilerCategory.Network.Name;
            string value = ProfilerDriver.GetFormattedCounterValue(frame, category, name);

            label.text = $"{name}: {value}";
        }

        void OnSelectedFrameIndexChanged(long selectedFrameIndex)
        {
            // Update the label with the current data for the newly selected frame.
            ReloadData();
        }
    }
}
