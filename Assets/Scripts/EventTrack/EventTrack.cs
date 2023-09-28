using QFramework;

namespace Larvend
{
    public class EventTrack : Architecture<EventTrack>
    {
        protected override void Init()
        {
            this.RegisterModel(new EventTrackModel());
        }
    }
}