using Player.Skills;
using UnityEngine;

namespace Player
{
    public class SkillBufferDebugger : MonoBehaviour
    {
        [SerializeField] private PlayerSkillModule skillModule;

        private PlayerAttackCombo _comboSkill;
        private Texture2D _texFill;
        private Texture2D _texEmpty;
        private Texture2D _texBg;

        private void Start()
        {
            _comboSkill = skillModule.GetSkill<PlayerAttackCombo>();
            _texFill  = MakeTex(new Color(1f, 0.75f, 0f));
            _texEmpty = MakeTex(new Color(0.2f, 0.2f, 0.2f));
            _texBg    = MakeTex(new Color(0f, 0f, 0f, 0.7f));
        }

        private void OnGUI()
        {
            if (skillModule == null) return;

            const float px = 20f;
            const float py = 20f;
            const float w  = 270f;
            const float lh = 22f;

            float totalH = lh * (9 + (_comboSkill != null ? 2 : 0));
            var bg = new Rect(px - 8, py - 8, w + 16, totalH + 16);
            GUI.DrawTexture(bg, _texBg);

            GUILayout.BeginArea(new Rect(px, py, w, totalH));

            Row("── SKILL BUFFER DEBUG ──", Color.white);
            Divider(w);

            bool active = skillModule.IsSkillActive;
            Row($"Current  :  {skillModule.CurrentSkillName}", active ? Color.green : Color.gray);
            Row($"Active   :  {(active ? "● USING" : "○ idle")}", active ? Color.green : Color.gray);

            Divider(w);

            bool hasBuffer = skillModule.BufferedSkillIndex.HasValue;
            float norm     = skillModule.BufferRemainingNormalized;
            float secs     = skillModule.BufferRemainingSeconds;

            Color bufCol = !hasBuffer ? Color.gray : (norm < 0.35f ? Color.red : Color.yellow);
            Row($"Buffer   :  {(hasBuffer ? skillModule.BufferedSkillName : "(없음)")}", bufCol);

            // 게이지 바
            Rect barArea = GUILayoutUtility.GetRect(w, 14f);
            barArea.x = 0; barArea.width = w;
            GUI.DrawTexture(barArea, _texEmpty);
            if (hasBuffer && norm > 0f)
            {
                Rect fill = barArea;
                fill.width = barArea.width * norm;
                GUI.DrawTexture(fill, _texFill);
            }

            GUI.color = bufCol;
            GUILayout.Label(hasBuffer ? $"         {secs:F2}s  /  {PlayerSkillModule.BufferDuration:F2}s" : "");
            GUI.color = Color.white;

            if (_comboSkill != null)
            {
                Divider(w);
                Row($"Combo    :  {_comboSkill.ComboCount}  /  {_comboSkill.ComboMax}", Color.cyan);
            }

            GUILayout.EndArea();
        }

        private static void Row(string text, Color color)
        {
            GUI.color = color;
            GUILayout.Label(text);
            GUI.color = Color.white;
        }

        private static void Divider(float w)
        {
            GUILayout.Label(new string('─', (int)(w / 7.2f)));
        }

        private static Texture2D MakeTex(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
