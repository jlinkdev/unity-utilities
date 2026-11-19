using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace JLinkDev.HumanoidStudio
{
    public sealed class HumanoidReview : MonoBehaviour
    {
        public Animator[] figures;
        public Camera studioCamera;
        public AnimationClip motion;

        PlayableGraph graph;
        AnimationClipPlayable[] players;
        Transform[][] joints;
        Vector3[][] positions;
        Quaternion[][] rotations;
        int focus = -1;
        float yaw = 7f, pitch = 5f, distance = 5.6f, time;
        bool playing;

        void Awake()
        {
            joints = new Transform[figures.Length][];
            positions = new Vector3[figures.Length][];
            rotations = new Quaternion[figures.Length][];
            players = new AnimationClipPlayable[figures.Length];
            graph = PlayableGraph.Create("Humanoid review");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            for (int i = 0; i < figures.Length; i++)
            {
                joints[i] = figures[i].GetComponentsInChildren<Transform>(true);
                positions[i] = System.Array.ConvertAll(joints[i], t => t.localPosition);
                rotations[i] = System.Array.ConvertAll(joints[i], t => t.localRotation);
                players[i] = AnimationClipPlayable.Create(graph, motion);
                figures[i].cullingMode = AnimatorCullingMode.AlwaysAnimate;
                players[i].SetSpeed(0);
                AnimationPlayableOutput.Create(graph, figures[i].name, figures[i]).SetSourcePlayable(players[i]);
            }
            graph.Play();
            SelectFigure(-1);
            Sample(0);
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }

        void LateUpdate()
        {
            if (playing) Sample((time + Time.deltaTime) % motion.length);
            var pivot = new Vector3(focus < 0 ? 0 : figures[focus].transform.position.x, .90f, 0);
            var offset = Quaternion.Euler(-pitch, yaw, 0) * Vector3.forward;
            studioCamera.transform.position = pivot + offset * distance;
            studioCamera.transform.LookAt(pivot);
        }

        void Sample(float seconds)
        {
            time = seconds;
            for (int i = 0; i < players.Length; i++) players[i].SetTime(time);
            graph.Evaluate(0);
        }

        void Neutral()
        {
            playing = false;
            time = 0;
            for (int i = 0; i < joints.Length; i++)
                for (int j = 0; j < joints[i].Length; j++)
                {
                    joints[i][j].localPosition = positions[i][j];
                    joints[i][j].localRotation = rotations[i][j];
                }
        }

        void SelectFigure(int index)
        {
            focus = index;
            float halfView = Mathf.Tan(studioCamera.fieldOfView * .5f * Mathf.Deg2Rad);
            distance = index < 0 ? Mathf.Max(3.8f, 3.15f / (halfView * studioCamera.aspect)) : 3.4f;
            for (int i = 0; i < figures.Length; i++)
                foreach (var skin in figures[i].GetComponentsInChildren<SkinnedMeshRenderer>())
                    skin.enabled = index < 0 || index == i;
        }

        void OnGUI()
        {
            GUI.matrix = Matrix4x4.Scale(Vector3.one * Mathf.Clamp(Screen.width / 900f, .72f, 1.5f));
            GUILayout.BeginArea(new Rect(12, 12, 670, 110));
            GUILayout.BeginHorizontal(GUI.skin.box);
            if (GUILayout.Toggle(focus == -1, "All figures", GUI.skin.button) && focus != -1) SelectFigure(-1);
            for (int i = 0; i < figures.Length; i++)
                if (GUILayout.Toggle(focus == i, figures[i].name, GUI.skin.button) && focus != i) SelectFigure(i);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(GUI.skin.box);
            if (GUILayout.Button(playing ? "Pause" : "Play motion", GUILayout.Width(100))) playing = !playing;
            var seek = GUILayout.HorizontalSlider(time, 0, motion.length, GUILayout.Width(180));
            if (Mathf.Abs(seek - time) > .001f) { playing = false; Sample(seek); }
            if (GUILayout.Button("Bind pose", GUILayout.Width(86))) Neutral();
            GUILayout.Label("Drag to orbit   ·   Scroll to zoom");
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            var e = Event.current;
            if (e.type == EventType.MouseDrag && e.button == 0 && GUIUtility.hotControl == 0)
            {
                yaw += e.delta.x * .5f;
                pitch = Mathf.Clamp(pitch + e.delta.y * .4f, -20, 75);
                e.Use();
            }
            if (e.type == EventType.ScrollWheel)
            {
                distance = Mathf.Clamp(distance * Mathf.Exp(.055f * e.delta.y), .6f, 12f);
                e.Use();
            }
        }
    }
}
