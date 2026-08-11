using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace jlinkdev.UnityUtilities.WorldScanning.Timeline
{
    public sealed class ScanTimelineClip : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField] private ExposedReference<ScanEmitter> emitter;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            ScriptPlayable<ScanTimelineBehaviour> playable = ScriptPlayable<ScanTimelineBehaviour>.Create(graph);
            ScanTimelineBehaviour behaviour = playable.GetBehaviour();
            behaviour.Emitter = emitter.Resolve(graph.GetResolver());
            return playable;
        }
    }

    public sealed class ScanTimelineBehaviour : PlayableBehaviour
    {
        private bool emitted;

        internal ScanEmitter Emitter { get; set; }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (emitted || Emitter == null || !Application.isPlaying)
                return;
            Emitter.Emit();
            emitted = true;
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (playable.GetTime() <= 0.0 || playable.IsDone())
                emitted = false;
        }

        public override void OnGraphStop(Playable playable)
        {
            emitted = false;
        }
    }
}
