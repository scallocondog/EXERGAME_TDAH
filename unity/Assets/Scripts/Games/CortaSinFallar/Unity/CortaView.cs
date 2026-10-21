// RF-13: escena de Corta sin fallar; dibuja lo que exponen las reglas
using System.Collections.Generic;
using MoviMente.Gestures;
using UnityEngine;

namespace MoviMente.Games.CortaSinFallar
{
    // Solo presentación: posiciones, la zona de corte y un destello al cortar.
    // Qué es acierto, error u omisión lo deciden las reglas.
    public sealed class CortaView : MonoBehaviour
    {
        // Mismos modelos de frutas que Atrapa lo correcto; si falta alguno, una primitiva.
        [SerializeField] private GameObject[] fruitModels;
        [SerializeField] private GameObject forbiddenModel;
        [SerializeField] private Difficulty difficulty = Difficulty.Easy;
        [SerializeField] private float fieldHalfWidth = 6f;
        [SerializeField] private float fieldHeight = 7f;
        [SerializeField] private float itemSize = 0.9f;
        [SerializeField] private float spinDegreesPerSecond = 90f;
        // Tokens de docs/ux/sistema-visual.md.
        [SerializeField] private Color bandColor = new Color32(0x24, 0x59, 0x6b, 0xff);
        [SerializeField] private Color floorColor = new Color32(0x1b, 0x41, 0x50, 0xff);
        [SerializeField] private Color fruitColor = new Color32(0xf2, 0xa6, 0x5a, 0xff);
        // El prohibido es una piedra gris: se distingue por forma y color, sin parecer una amenaza (RNF-05).
        [SerializeField] private Color forbiddenColor = new Color32(0x8a, 0x9b, 0xa3, 0xff);
        [SerializeField] private Color hitColor = new Color32(0x58, 0xcf, 0xa6, 0xff);
        [SerializeField] private Color errorColor = new Color32(0xe7, 0x9a, 0x86, 0xff);
        [SerializeField] private float hitFlashSeconds = 0.2f;
        [SerializeField] private float errorFlashSeconds = 0.1f;

        private readonly Dictionary<int, GameObject> views = new Dictionary<int, GameObject>();
        private readonly List<Material> materials = new List<Material>();
        private Material bandMaterial;
        private Material fruitMaterial;
        private Material forbiddenMaterial;
        private CortaEffects effects;
        private Vector3 slashCenter;
        private float slashWidth;
        private CortaSinFallarRules rules;
        private float flashUntil;
        private Color flashColor;

        public CortaSinFallarRules Rules => rules;

        private void Start()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            bandMaterial = NewMaterial(lit, bandColor);
            fruitMaterial = NewMaterial(lit, fruitColor);
            forbiddenMaterial = NewMaterial(lit, forbiddenColor);

            CreateBlock("Piso", NewMaterial(lit, floorColor),
                new Vector3(0f, -0.2f, 0f), new Vector3(fieldHalfWidth * 2f, 0.1f, 1.4f));
            float bandBottom = CortaSinFallarRules.BandLow * fieldHeight;
            float bandTop = CortaSinFallarRules.BandHigh * fieldHeight;
            CreateBlock("Zona de corte", bandMaterial,
                new Vector3(0f, (bandBottom + bandTop) / 2f, 1f), new Vector3(fieldHalfWidth * 2.3f, bandTop - bandBottom, 0.1f));

            // El tajo pasa justo delante de las frutas y a lo ancho del campo: más cerca
            // de la cámara que la franja, se vería más ancho que ella.
            effects = GetComponent<CortaEffects>();
            if (effects == null) effects = gameObject.AddComponent<CortaEffects>();
            slashCenter = new Vector3(0f, (bandBottom + bandTop) / 2f, -0.05f);
            slashWidth = fieldHalfWidth * 2f;
            CoreSystems.Instance.PadInput.GestureDetected += OnGesture;

            CoreSystems.Instance.Runner.Begin(CortaSinFallarGame.CreateDefinition(onRulesCreated: Bind), difficulty,
                CortaSinFallarRules.PlayerSlot);
        }

        private void OnDestroy()
        {
            Unbind();
            if (CoreSystems.Instance != null)
            {
                CoreSystems.Instance.PadInput.GestureDetected -= OnGesture;
                CoreSystems.Instance.Runner.End();
            }
            foreach (Material material in materials) Destroy(material);
        }

        private void Update()
        {
            if (rules == null) return;

            float spin = spinDegreesPerSecond * Time.deltaTime;
            foreach (FlyingObject target in rules.Objects)
            {
                if (!views.TryGetValue(target.Id, out GameObject view)) continue;
                view.transform.localPosition = ToWorld(target.Position);
                view.transform.Rotate(0f, spin, spin * 0.5f, Space.Self);
            }
            bandMaterial.color = Time.time < flashUntil ? flashColor : bandColor;
        }

        // Las reglas cambian en cada partida y al reintentar.
        private void Bind(CortaSinFallarRules next)
        {
            Unbind();
            rules = next;
            rules.ObjectSpawned += OnSpawned;
            rules.ObjectCut += OnCut;
            rules.ObjectLeft += OnLeft;
        }

        private void Unbind()
        {
            if (rules != null)
            {
                rules.ObjectSpawned -= OnSpawned;
                rules.ObjectCut -= OnCut;
                rules.ObjectLeft -= OnLeft;
            }
            foreach (GameObject view in views.Values) Destroy(view);
            views.Clear();
            rules = null;
        }

        private void OnSpawned(FlyingObject target)
        {
            GameObject view = target.Kind == FlyingKind.Valid
                ? CreateItem(Model(fruitModels, target.Category), PrimitiveType.Sphere, fruitMaterial, $"Fruta {target.Id}")
                : CreateItem(forbiddenModel, PrimitiveType.Cube, forbiddenMaterial, $"Prohibido {target.Id}");
            view.transform.localScale = Vector3.one * itemSize;
            view.transform.localPosition = ToWorld(target.Position);
            views.Add(target.Id, view);
        }

        // Cada latigazo deja un tajo, corte o no algo: el niño ve que su gesto llegó.
        private void OnGesture(GestureEvent gesture)
        {
            if (gesture.Slot != CortaSinFallarRules.PlayerSlot || gesture.Type != GestureType.Swing) return;
            if (CoreSystems.Instance.Runner.Session?.State != GameState.Playing) return;
            effects.Slash(slashCenter, slashWidth);
        }

        // La fruta se parte en dos; lo prohibido se desvanece, sin explotar (RNF-05).
        private void OnCut(FlyingObject target)
        {
            bool valid = target.Kind == FlyingKind.Valid;
            if (views.Remove(target.Id, out GameObject view))
            {
                if (valid) effects.Slice(view, target.Category);
                else effects.Fade(view);
            }
            flashColor = valid ? hitColor : Color.Lerp(bandColor, errorColor, 0.5f);
            flashUntil = Time.time + (valid ? hitFlashSeconds : errorFlashSeconds);
        }

        private void OnLeft(FlyingObject target) => RemoveView(target);

        private void RemoveView(FlyingObject target)
        {
            if (views.Remove(target.Id, out GameObject view)) Destroy(view);
        }

        private Vector3 ToWorld(FieldPoint point) => new Vector3(point.X * fieldHalfWidth, point.Y * fieldHeight, 0f);

        private static GameObject Model(GameObject[] models, int index) =>
            models != null && index >= 0 && index < models.Length ? models[index] : null;

        private GameObject CreateItem(GameObject model, PrimitiveType fallback, Material fallbackMaterial, string objectName)
        {
            if (model == null)
            {
                GameObject shape = GameObject.CreatePrimitive(fallback);
                shape.name = objectName;
                Destroy(shape.GetComponent<Collider>());
                shape.transform.SetParent(transform, false);
                shape.GetComponent<Renderer>().sharedMaterial = fallbackMaterial;
                return shape;
            }

            // Envuelve el modelo centrado y de lado 1, igual que las primitivas.
            var holder = new GameObject(objectName);
            holder.transform.SetParent(transform, false);
            GameObject view = Instantiate(model, holder.transform, false);
            Bounds bounds = LocalBounds(holder.transform, view);
            float largestSide = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float scale = largestSide > 0f ? 1f / largestSide : 1f;
            view.transform.localScale *= scale;
            view.transform.localPosition -= bounds.center * scale;
            return holder;
        }

        private static Bounds LocalBounds(Transform space, GameObject view)
        {
            Renderer[] renderers = view.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);

            Bounds world = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) world.Encapsulate(renderers[i].bounds);
            Vector3 scale = space.lossyScale;
            return new Bounds(space.InverseTransformPoint(world.center),
                new Vector3(world.size.x / scale.x, world.size.y / scale.y, world.size.z / scale.z));
        }

        private void CreateBlock(string objectName, Material material, Vector3 position, Vector3 scale)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            Destroy(cube.GetComponent<Collider>());
            cube.transform.SetParent(transform, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
        }

        private Material NewMaterial(Shader shader, Color color)
        {
            var material = new Material(shader) { color = color };
            materials.Add(material);
            return material;
        }
    }
}
