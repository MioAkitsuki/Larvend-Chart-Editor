using System.Collections.Generic;
using QFramework;

namespace Larvend
{
    public class EventTrackModel : AbstractModel
    {
        public List<EventGroupData> EventGroups;
        public EventGroupData CurrentEventGroup;

        public List<EventGroup> DisplayedEventGroups;
        public List<EventGroupData> SelectedGroups;

        public EventButtonData HoldStartButton;
        public EventButtonData HoldEndButton;
        protected override void OnInit()
        {
            EventGroups = new List<EventGroupData>();
            CurrentEventGroup = new EventGroupData();

            DisplayedEventGroups = new List<EventGroup>();
            SelectedGroups = new List<EventGroupData>();
        }

        public void Reset()
        {
            EventGroups = new List<EventGroupData>();
            CurrentEventGroup = new EventGroupData();

            DisplayedEventGroups = new List<EventGroup>();
            SelectedGroups = new List<EventGroupData>();
        }

    }
    
}