// RF-11: escena de Atrapa lo correcto; dibuja lo que exponen las reglas
using System.Collections.Generic;
using UnityEngine;

namespace MoviMente.Games.AtrapaLoCorrecto
{
    // Solo presentación: posiciones, formas y un destello corto al resolver.
    // Todo lo que cuenta (acierto, error, omisión) lo deciden las reglas.
    public sealed class AtrapaView : MonoBehaviour
    {
        // Cada categoría es forma + color, para no depender solo del color (RNF-06).
        // Son el respaldo si falta el modelo de esa categoría.
        private static readonly PrimitiveType[] CategoryShapes =
            { PrimitiveType.Sphere, PrimitiveType.Cube, PrimitiveType.Capsule };

        // docs/ux/sistema-visual.md §8: naranja, pez y uvas, y el cuenco como canasta.
        [SerializeField] private GameObject[] categoryModels;
        [SerializeField] private GameObject basketModel;
        [SerializeField] private Difficulty difficulty = Difficulty.Easy;
        [SerializeField] private float fieldHalfWidth = 6f;
        [SerializeField] private float fieldHeight = 7f;
        [SerializeField] private float itemSize = 0.9f;
        [SerializeField] private Color[] categoryColors =
        {
            new Color32(0xf2, 0xa6, 0x5a, 0xff),
            new Color32(0x6f, 0xa8, 0xdc, 0xff),
            new Color32(0xb4, 0x8e, 0xad, 0xff),
        };
        [SerializeField] private Color basketColor = new Color32(0xf3, 0xf8, 0xf9, 0xff);
        // bg-soft y bg-raised de docs/ux/sistema-visual.md: piso y panel de la instrucción.
        [SerializeField] private Color floorColor = new Color32(0x1b, 0x41, 0x50, 0xff);
        [SerializeField] private Color panelColor = new Color32(0x24, 0x59, 0x6b, 0xff);
        // Tokens de docs/ux/sistema-visual.md: acierto en accent, error en alert a media intensidad.
        [SerializeField] private Color hitColor = new Color32(0x58, 0xcf, 0xa6, 0xff);
        [SerializeField] private Color errorColor = new Color32(0xe7, 0x9a, 0x86, 0xff);
        [SerializeField] private float hitFlashSeconds = 0.2f;
        [SerializeField] private float errorFlashSeconds = 0.1f;

        private readonly Dictionary<int, GameObject> itemViews = new Dictionary<int, GameObject>();
        // Copias de los materiales del modelo de la canasta, para teñirlas en el destello.
        private readonly List<Material> basketModelMaterials = new List<Material>();
        private Material[] categoryMaterials;
        private Material basketMaterial;
        private Material floorMaterial;
        private Material panelMaterial;
        private Transform basket;
        private Transform targetSample;
        private AtrapaLoCorrectoRules rules;
        private float flashUntil;
        private Color flashColor;

        public AtrapaLoCorrectoRules Rules => rules;

        private void Start()
        {
            CreateMaterials();
            CreateFloor();
            CreateTargetPanel();
            basket = CreateBasket();
            GameRunner runner = CoreSystems.Instance.Runner;
            runner.Begin(AtrapaLoCorrectoGame.CreateDefinition(onRulesCreated: Bind), difficulty, AtrapaLoCorrectoRules.PlayerSlot);
        }

        private void OnDestroy()
        {
            Unbind();
            if (CoreSystems.Instance != null) CoreSystems.Instance.Runner.End();
            foreach (Material material in categoryMaterials ?? new Material[0]) Destroy(material);
            foreach (Material material in new[] { basketMaterial, floorMaterial, panelMaterial })
            {
                if (material != null) Destroy(material);
            }
            foreach (Material material in basketModelMaterials) Destroy(material);
        }

        private void Update()
        {
            if (rules == null) return;

            float basketWidth = rules.BasketHalfWidth * 2f * fieldHalfWidth;
            basket.localPosition = new Vector3(rules.BasketX * fieldHalfWidth, 0f, 0f);
            // El cuenco se escala parejo para no deformarse; el bloque de respaldo, solo a lo ancho.
            basket.localScale = basketModel != null ? Vector3.one * basketWidth : new Vector3(basketWidth, 0.4f, 1.2f);
            foreach (FallingItem item in rules.Items)
            {
                if (itemViews.TryGetValue(item.Id, out GameObject view))
                {
                    view.transform.localPosition = new Vector3(item.X * fieldHalfWidth, item.Y * fieldHeight + itemSize, 0f);
                }
            }
            TintBasket(Time.time < flashUntil);
        }

        // El modelo trae su color en la textura: en reposo va sin teñir.
        private void TintBasket(bool flashing)
        {
            basketMaterial.color = flashing ? flashColor : basketColor;
            foreach (Material material in basketModelMaterials) material.color = flashing ? flashColor : Color.white;
        }

        // Las reglas cambian en cada partida y al reintentar.
        private void Bind(AtrapaLoCorrectoRules next)
        {
            Unbind();
            rules = next;
            rules.ItemSpawned += OnItemSpawned;
            rules.ItemResolved += OnItemResolved;
            if (targetSample != null) Destroy(targetSample.gameObject);
            targetSample = CreateTargetSample(rules.TargetCategory);
        }

        private void Unbind()
        {
            if (rules != null)
            {
                rules.ItemSpawned -= OnItemSpawned;
                rules.ItemResolved -= OnItemResolved;
            }
            foreach (GameObject view in itemViews.Values) Destroy(view);
            itemViews.Clear();
            rules = null;
        }

        private void OnItemSpawned(FallingItem item)
        {
            GameObject view = CreateShape(item.Category, $"Objeto {item.Id}");
            view.transform.localScale = Vector3.one * itemSize;
            itemViews.Add(item.Id, view);
        }

        private void OnItemResolved(FallingItem item, bool caught)
        {
            if (itemViews.Remove(item.Id, out GameObject view)) Destroy(view);
            if (!caught) return;
            flashColor = item.IsTarget ? hitColor : Color.Lerp(basketColor, errorColor, 0.5f);
            flashUntil = Time.time + (item.IsTarget ? hitFlashSeconds : errorFlashSeconds);
        }

        private void CreateMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            categoryMaterials = new Material[categoryColors.Length];
            for (int i = 0; i < categoryColors.Length; i++)
            {
                categoryMaterials[i] = new Material(lit) { color = categoryColors[i] };
            }
            basketMaterial = new Material(lit) { color = basketColor };
            floorMaterial = new Material(lit) { color = floorColor };
            panelMaterial = new Material(lit) { color = panelColor };
        }

        private Transform CreateBasket()
        {
            if (basketModel == null) return CreateBlock("Canasta", basketMaterial, Vector3.zero, Vector3.one);

            GameObject view = CreateModel(basketModel, "Canasta");
            foreach (Renderer renderer in view.GetComponentsInChildren<Renderer>())
            {
                basketModelMaterials.AddRange(renderer.materials);
            }
            return view.transform;
        }

        // Marca el ancho por el que se mueve la canasta, sin nada que distraiga (RNF-05).
        private void CreateFloor() =>
            CreateBlock("Piso", floorMaterial, new Vector3(0f, -0.35f, 0f), new Vector3(fieldHalfWidth * 2f, 0.1f, 1.4f));

        // Panel fuera del campo: la muestra no se confunde con lo que cae.
        private void CreateTargetPanel() =>
            CreateBlock("Panel de la categoría pedida", panelMaterial, TargetSamplePosition + new Vector3(0f, 0f, 0.8f),
                new Vector3(itemSize * 2.6f, itemSize * 2.6f, 0.1f));

        private Vector3 TargetSamplePosition => new Vector3(-fieldHalfWidth - 2.2f, fieldHeight - itemSize, 0f);

        // La instrucción "atrapa estos" sin leer (RNF-06).
        private Transform CreateTargetSample(int category)
        {
            GameObject sample = CreateShape(category, "Categoría pedida");
            sample.transform.localPosition = TargetSamplePosition;
            sample.transform.localScale = Vector3.one * itemSize * 1.6f;
            return sample.transform;
        }

        private Transform CreateBlock(string objectName, Material material, Vector3 position, Vector3 scale)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            Destroy(cube.GetComponent<Collider>());
            cube.transform.SetParent(transform, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube.transform;
        }

        private GameObject CreateShape(int category, string objectName)
        {
            GameObject model = categoryModels != null && category < categoryModels.Length ? categoryModels[category] : null;
            if (model != null) return CreateModel(model, objectName);

            GameObject shape = GameObject.CreatePrimitive(CategoryShapes[category % CategoryShapes.Length]);
            shape.name = objectName;
            Destroy(shape.GetComponent<Collider>());
            shape.transform.SetParent(transform, false);
            shape.GetComponent<Renderer>().sharedMaterial = categoryMaterials[category % categoryMaterials.Length];
            return shape;
        }

        // Envuelve el modelo en un objeto vacío, centrado y de lado 1 como las
        // primitivas, así quien lo usa lo mueve y lo escala sin mirar el FBX.
        private GameObject CreateModel(GameObject model, string objectName)
        {
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
    }
}
