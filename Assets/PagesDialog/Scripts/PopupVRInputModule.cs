/* Copy and hack StandaloneInputModule.cs from Unity.
 * Differences are marked with REMOVED. */

using UnityEngine;
using UnityEngine.EventSystems;


namespace MyTest
{
    [AddComponentMenu("Event/Popup VR Input Module")]
    public class PopupVRInputModule : PointerInputModule
    {
        protected PopupVRInputModule()
        {
        }

        public override void UpdateModule()
        {
        }

        public override bool IsModuleSupported()
        {
            return true;
        }

        public override void ActivateModule()
        {
            base.ActivateModule();

            var toSelect = eventSystem.currentSelectedGameObject;
            //if (toSelect == null)
            //    toSelect = eventSystem.lastSelectedGameObject;
            if (toSelect == null)
                toSelect = eventSystem.firstSelectedGameObject;

            eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());
        }

        public override void DeactivateModule()
        {
            base.DeactivateModule();
            ClearSelection();
        }

        public override void Process()
        {
            SendUpdateEventToSelectedObject();
        }

        private bool SendUpdateEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return false;

            var data = GetBaseEventData();
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
            return data.used;
        }
    }
}
