using Config;

namespace SkillEditor
{
    public class AudioTrackItem : TrackItemBase<AudioTrack>
    {
        private SkillMultiLineTrackStyle.ChildTrack childTrack;
        private SkillAudioTrackItemStyle style;
        public void Init(float frameUnitWidth, SkillAudioEvent audioEvent, SkillMultiLineTrackStyle.ChildTrack childTrack)
        {
            this.childTrack = childTrack;
            style = new SkillAudioTrackItemStyle();
            ItemStyle = style;
            style.Init(frameUnitWidth, audioEvent, childTrack);
        }

        public void Destroy()
        {
            childTrack.Destroy();
        }

        public void SetTrackName(string trackName)
        {
            childTrack.SetTrackName(trackName);
        }
    }
}