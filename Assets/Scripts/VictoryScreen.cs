using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Écran de victoire en VR : un panneau qui se place devant le casque, fait face au joueur
// et le suit doucement (comme le menu). Joue aussi le son de victoire (2 s maximum).
//
// Rien à mettre dans la scène : il est créé automatiquement au premier trou réussi.
// Pour changer ses réglages, tu peux quand même créer un objet vide avec ce composant.
public class VictoryScreen : MonoBehaviour
{
    private static VictoryScreen instance;

    // Déclenché quand tous les niveaux sont terminés (écouté par MascotCelebration)
    public static event System.Action AllLevelsCompleted;

    public static VictoryScreen Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<VictoryScreen>();
                if (instance == null)
                    instance = new GameObject("VictoryScreen").AddComponent<VictoryScreen>();
            }
            return instance;
        }
    }

    [Header("Position du panneau")]
    [Tooltip("Distance devant le casque (m)")]
    [SerializeField] private float distance = 1.1f;
    [Tooltip("Décalage vertical par rapport aux yeux (m)")]
    [SerializeField] private float heightOffset = -0.05f;
    [Tooltip("Vitesse à laquelle le panneau suit le regard")]
    [SerializeField] private float followSpeed = 3f;

    [Header("Durées (s)")]
    [SerializeField] private float levelMessageDuration = 4f;
    [Tooltip("Durée du message final (0 = reste affiché)")]
    [SerializeField] private float finalMessageDuration = 4f;
    [SerializeField] private float maxSoundDuration = 2f;

    [Header("Couleurs")]
    [SerializeField] private Color backgroundColor = new Color(0.04f, 0.07f, 0.16f, 0.88f);
    [SerializeField] private Color titleColor = new Color(1f, 0.84f, 0.25f);
    [SerializeField] private Color bodyColor = Color.white;

    private struct Message
    {
        public string title;
        public string body;
        public float duration;
    }

    private readonly Queue<Message> queue = new Queue<Message>();
    private readonly HashSet<int> completedLevels = new HashSet<int>();
    private int totalStrokes;
    private int totalLevels;

    private GameObject panel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI bodyText;
    private AudioSource audioSource;
    private Coroutine queueRoutine;
    private Coroutine soundRoutine;
    private bool snapNextFrame;
    private AudioClip defaultJingle;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // son "dans la tête", pas spatialisé

        BuildUI();
        panel.SetActive(false);
    }

    // ---------- Appelé par HoleTrigger ----------

    public void LevelCompleted(int level, AudioClip clip, float volume)
    {
        var score = FindObjectOfType<Score>(true);
        int strokes = score != null ? score.HitCount : 0;

        PlaySound(clip, volume);

        bool firstTime = completedLevels.Add(level);
        if (firstTime)
            totalStrokes += strokes;

        Enqueue("Level " + level + " completed!",
                "Strokes: " + strokes,
                levelMessageDuration);

        if (score != null)
            score.ResetScore();

        if (totalLevels == 0)
            totalLevels = Mathf.Max(1, FindObjectsOfType<HoleTrigger>(true).Length);

        if (firstTime && completedLevels.Count >= totalLevels)
        {
            Enqueue("Congratulations!",
                    "You completed all " + totalLevels + " levels!\nTotal score: " + totalStrokes +
                    (totalStrokes == 1 ? " stroke" : " strokes"),
                    finalMessageDuration);

            AllLevelsCompleted?.Invoke();
        }
    }

    // ---------- Messages ----------

    private void Enqueue(string title, string body, float duration)
    {
        queue.Enqueue(new Message { title = title, body = body, duration = duration });
        if (queueRoutine == null)
            queueRoutine = StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        while (queue.Count > 0)
        {
            Message m = queue.Dequeue();
            titleText.text = m.title;
            bodyText.text = m.body;

            if (!panel.activeSelf)
            {
                panel.SetActive(true);
                snapNextFrame = true;
            }

            if (m.duration <= 0f)
            {
                queueRoutine = null;
                yield break; // reste affiché
            }

            yield return new WaitForSeconds(m.duration);
        }

        panel.SetActive(false);
        queueRoutine = null;
    }

    // ---------- Position face au casque ----------

    private void LateUpdate()
    {
        if (panel == null || !panel.activeSelf || Camera.main == null)
            return;

        Transform head = Camera.main.transform;
        Vector3 forward = head.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;

        Vector3 target = head.position + forward * distance + Vector3.up * heightOffset;
        Transform t = panel.transform;

        if (snapNextFrame)
        {
            t.position = target;
            snapNextFrame = false;
        }
        else
        {
            t.position = Vector3.Lerp(t.position, target, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        }

        // Le texte d'un canvas se lit quand son axe Z s'éloigne du regard
        t.rotation = Quaternion.LookRotation(t.position - head.position);
    }

    // ---------- Son (2 s max) ----------

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null)
            clip = DefaultJingle();

        if (soundRoutine != null)
            StopCoroutine(soundRoutine);

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
        soundRoutine = StartCoroutine(StopSoundAfter(maxSoundDuration, volume));
    }

    private IEnumerator StopSoundAfter(float duration, float volume)
    {
        const float fade = 0.25f;
        yield return new WaitForSeconds(Mathf.Max(0f, duration - fade));

        float t = 0f;
        while (t < fade && audioSource.isPlaying)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(volume, 0f, t / fade);
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = volume;
        soundRoutine = null;
    }

    // Petit jingle généré (do-mi-sol-do) si aucun son n'est assigné
    private AudioClip DefaultJingle()
    {
        if (defaultJingle != null)
            return defaultJingle;

        const int rate = 44100;
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        float[] lengths = { 0.13f, 0.13f, 0.13f, 0.7f };

        int total = 0;
        foreach (float l in lengths) total += (int)(l * rate);
        var data = new float[total];

        int offset = 0;
        for (int n = 0; n < notes.Length; n++)
        {
            int count = (int)(lengths[n] * rate);
            for (int i = 0; i < count; i++)
            {
                float time = i / (float)rate;
                float attack = Mathf.Clamp01(time / 0.01f);
                float release = Mathf.Clamp01((lengths[n] - time) / (n == notes.Length - 1 ? 0.5f : 0.03f));
                float w = 2f * Mathf.PI * notes[n] * time;
                data[offset + i] = 0.35f * attack * release * (Mathf.Sin(w) + 0.3f * Mathf.Sin(2f * w));
            }
            offset += count;
        }

        defaultJingle = AudioClip.Create("JingleVictoire", total, 1, rate, false);
        defaultJingle.SetData(data, 0);
        return defaultJingle;
    }

    // ---------- Construction du panneau ----------

    private void BuildUI()
    {
        panel = new GameObject("Panneau", typeof(RectTransform));
        panel.transform.SetParent(transform, false);

        var canvas = panel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        var rt = (RectTransform)panel.transform;
        rt.sizeDelta = new Vector2(900f, 420f);
        panel.transform.localScale = Vector3.one * 0.001f; // 0,9 m x 0,42 m

        var background = new GameObject("Fond", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(panel.transform, false);
        Stretch((RectTransform)background.transform, Vector2.zero, Vector2.one);
        background.GetComponent<Image>().color = backgroundColor;

        titleText = CreateText("Titre", 90f, FontStyles.Bold, titleColor, new Vector2(0f, 0.52f), new Vector2(1f, 0.95f));
        bodyText = CreateText("Texte", 58f, FontStyles.Normal, bodyColor, new Vector2(0f, 0.05f), new Vector2(1f, 0.55f));
    }

    private TextMeshProUGUI CreateText(string objectName, float size, FontStyles style, Color color, Vector2 min, Vector2 max)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(panel.transform, false);
        Stretch((RectTransform)go.transform, min, max);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.margin = new Vector4(40f, 0f, 40f, 0f);
        return text;
    }

    private static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
