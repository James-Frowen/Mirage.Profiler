using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.UIElements;

namespace Mirage.NetworkProfiler.ModuleGUI
{
    [System.Serializable]
    [ProfilerModuleMetadata(ModuleNames.SERVER)]
    public class ServerModule : ProfilerModule
    {
        static readonly ProfilerCounterDescriptor[] s_Counters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor(Names.PLAYER_COUNT, Counters.Category),
            new ProfilerCounterDescriptor(Names.OBJECT_COUNT, Counters.Category),
        };

        public ServerModule() : base(s_Counters) { }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new ServerViewController(ProfilerWindow);
        }
    }

    public abstract class BaseViewController : ProfilerModuleViewController
    {
        public BaseViewController(ProfilerWindow profilerWindow) : base(profilerWindow) { }

        protected static Label AddLabelWithPadding(VisualElement parent)
        {
            var label = new Label() { style = { paddingTop = 8, paddingLeft = 8 } };
            parent.Add(label);
            return label;
        }


        protected void SetText(Label label, string name)
        {
            int frame = (int)ProfilerWindow.selectedFrameIndex;
            string category = ProfilerCategory.Network.Name;
            string value = ProfilerDriver.GetFormattedCounterValue(frame, category, name);

            label.text = $"{name}: {value}";
        }
    }

    public class ServerViewController : BaseViewController
    {
        // Define a label, which will display the total particle count for tank trails in the selected frame.
        Label PlayerLabel;
        Label ObjectLabel;

        // Define a constructor for the view controller, which calls the base constructor with the Profiler Window passed from the module.
        public ServerViewController(ProfilerWindow profilerWindow) : base(profilerWindow) { }

        // Override CreateView to build the custom module details panel.
        protected override VisualElement CreateView()
        {
            var root = new VisualElement();

            PlayerLabel = AddLabelWithPadding(root);
            ObjectLabel = AddLabelWithPadding(root);
            NetIdLabel = AddLabelWithPadding(root);

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
            SetText(PlayerLabel, Names.PLAYER_COUNT);
            SetText(ObjectLabel, Names.OBJECT_COUNT);
        }
    }
}
