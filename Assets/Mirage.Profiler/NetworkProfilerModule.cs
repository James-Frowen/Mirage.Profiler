using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mirage.Profiler
{
    [DefaultExecutionOrder(10000)] // last
    public class NetworkProfilerBehaviour : MonoBehaviour
    {
        public NetworkServer Server;

        CountRecorder sentCounter;
        CountRecorder receivedCounter;

        private void Start()
        {
            sentCounter = new CountRecorder(NetworkProfilerCounters.MessageSentCount);
            receivedCounter = new CountRecorder(NetworkProfilerCounters.MessageReceivedCount);

            receivedCounter.Debug = true;

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

            NetworkProfilerCounters.PlayerCount.Sample(Server.Players.Count);
            sentCounter.Flush();
            receivedCounter.Flush();
        }
    }

    internal class NetworkProfilerCounters
    {
        public const string PLAYER_COUNT = "Player Count";
        public const string MESSAGES_SENT = "Messages Sent";
        public const string MESSAGES_RECEIVED = "Messages Received";

        public static readonly ProfilerCategory Category = ProfilerCategory.Network;

        public static readonly ProfilerCounter<int> PlayerCount;
        public static readonly ProfilerCounter<int> MessageSentCount;
        public static readonly ProfilerCounter<int> MessageReceivedCount;

        static NetworkProfilerCounters()
        {
            ProfilerMarkerDataUnit unit = ProfilerMarkerDataUnit.Count;

            PlayerCount = new ProfilerCounter<int>(Category, PLAYER_COUNT, unit);
            MessageSentCount = new ProfilerCounter<int>(Category, MESSAGES_SENT, unit);
            MessageReceivedCount = new ProfilerCounter<int>(Category, MESSAGES_RECEIVED, unit);
        }
    }

    class CountRecorder
    {
        readonly ProfilerCounter<int> profiler;
        int count;
        public bool Debug;

        public CountRecorder(ProfilerCounter<int> profiler)
        {
            this.profiler = profiler;
        }

        public void OnMessage(NetworkDiagnostics.MessageInfo obj)
        {
            count += obj.count;
            if (Debug) UnityEngine.Debug.Log($"Message: {obj.count}");
        }

        public void Flush()
        {
            profiler.Sample(count);
            if (Debug) UnityEngine.Debug.Log($"Flush: {count}");
            count = 0;
        }
    }
}
namespace Mirage.Profiler.ModuleGUI
{
    [System.Serializable]
    [ProfilerModuleMetadata("Network Profiler")]
    public class NetworkProfilerModule : ProfilerModule
    {
        static readonly ProfilerCounterDescriptor[] k_Counters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor(NetworkProfilerCounters.PLAYER_COUNT, NetworkProfilerCounters.Category),
            new ProfilerCounterDescriptor(NetworkProfilerCounters.MESSAGES_SENT, NetworkProfilerCounters.Category),
            new ProfilerCounterDescriptor(NetworkProfilerCounters.MESSAGES_RECEIVED,  NetworkProfilerCounters.Category),
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
        Label MessagesSent;
        Label MessagesReceived;

        // Define a constructor for the view controller, which calls the base constructor with the Profiler Window passed from the module.
        public NetworkProfilerModuleViewController(ProfilerWindow profilerWindow) : base(profilerWindow) { }

        // Override CreateView to build the custom module details panel.
        protected override VisualElement CreateView()
        {
            var view = new VisualElement();

            // Create the label and add it to the view.
            PlayerCount = new Label() { style = { paddingTop = 8, paddingLeft = 8 } };
            MessagesSent = new Label() { style = { paddingTop = 8, paddingLeft = 8 } };
            MessagesReceived = new Label() { style = { paddingTop = 8, paddingLeft = 8 } };
            view.Add(PlayerCount);
            view.Add(MessagesSent);
            view.Add(MessagesReceived);

            // Populate the label with the current data for the selected frame. 
            ReloadData();

            // Be notified when the selected frame index in the Profiler Window changes, so we can update the label.
            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            return view;
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
            // Retrieve the TankTrailParticleCount counter value from the Profiler as a formatted string.
            int frame = (int)ProfilerWindow.selectedFrameIndex;
            string category = ProfilerCategory.Network.Name;
            string playerValue = ProfilerDriver.GetFormattedCounterValue(frame, category, NetworkProfilerCounters.PLAYER_COUNT);
            string sentValue = ProfilerDriver.GetFormattedCounterValue(frame, category, NetworkProfilerCounters.MESSAGES_SENT);
            string receivedValue = ProfilerDriver.GetFormattedCounterValue(frame, category, NetworkProfilerCounters.MESSAGES_RECEIVED);

            // Update the label's text with the value.
            PlayerCount.text = $"Player Count: {playerValue}";
            MessagesSent.text = $"Messages Sent: {sentValue}";
            MessagesReceived.text = $"Messages Received: {receivedValue}";
        }

        void OnSelectedFrameIndexChanged(long selectedFrameIndex)
        {
            // Update the label with the current data for the newly selected frame.
            ReloadData();
        }
    }
}
