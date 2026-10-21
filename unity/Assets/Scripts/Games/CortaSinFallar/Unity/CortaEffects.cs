// RF-13, RNF-05: lo que se ve al cortar, al estilo Fruit Ninja pero sin castigo
using System.Collections.Generic;
using UnityEngine;

namespace MoviMente.Games.CortaSinFallar
{
    // Solo presentación. La fruta cortada se parte en dos mitades que salen volando
    // y salpica jugo; cada latigazo deja un tajo en la zona de corte, corte o no
    // algo, para que el niño vea que su gesto se registró. Lo prohibido no explota:
    // se desvanece, igual que el error en el resto del juego.
    public sealed class CortaEffects : MonoBehaviour
    {
        private const float Gravity = 14f;

        // Mismo orden que las categorías de fruta de CortaView.
        [SerializeField] private Color[] juiceColors =
        {
            new Color32(0xf5, 0x9a, 0x3e, 0xff),
            new Color32(0x9c, 0xc7, 0xe0, 0xff),
            new Color32(0x9b, 0x6a, 0xd6, 0xff),
        };
        [SerializeField] private Color slashColor = new Color32(0xf3, 0xf8, 0xf9, 0xff);
        [SerializeField] private float halfSeconds = 0.7f;
        [SerializeField] private float halfSpeed = 3f;
        [SerializeField] private float halfSpinDegrees = 420f;
        // Gotas grandes y pocas: a 3 m de la TV, las chicas no se ven.
        [SerializeField] private int juiceDrops = 22;
        [SerializeField] private float fadeSeconds = 0.15f;
        [SerializeField] private float slashSeconds = 0.22f;
        [SerializeField] private float slashThickness = 0.14f;

        private readonly List<Piece> pieces = new List<Piece>();
        private readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        private Mesh dropMesh;
        private Transform slash;
        private float slashStartedAt = float.NegativeInfinity;
        private float slashWidth;
        private float slashDirection = 1f;

        private sealed class Piece
        {
            public Transform Transform;
            public Vector3 Velocity;
            public float Spin;
            public float BornAt;
            public float Seconds;
            public Vector3 Scale;
            public bool Falls;
        }

        // La vista entrega la fruta ya fuera de juego: estas mitades ya no son "Fruta N".
        public void Slice(GameObject fruit, int category)
        {
            Transform first = fruit.transform;
            first.name = "Mitad";
            Transform second = Instantiate(fruit, first.parent).transform;
            second.name = "Mitad";

            Vector3 scale = first.localScale;
            var half = new Vector3(scale.x * 0.55f, scale.y, scale.z);
            AddHalf(first, half, -1f);
            AddHalf(second, half, 1f);
            SplashJuice(first.position, JuiceColor(category), scale.x);
        }

        public void Fade(GameObject forbidden)
        {
            forbidden.name = "Desvaneciendo";
            pieces.Add(new Piece
            {
                Transform = forbidden.transform,
                BornAt = Time.time,
                Seconds = fadeSeconds,
                Scale = forbidden.transform.localScale,
            });
        }

        // El tajo cruza la franja de un lado al otro, alternando el sentido.
        public void Slash(Vector3 center, float width)
        {
            if (slash == null) slash = CreateSlash();
            slashDirection = -slashDirection;
            slashWidth = width;
            // El pivote va en el borde de donde sale; la hoja crece hacia el otro.
            slash.localPosition = center - new Vector3(slashDirection * width / 2f, 0f, 0f);
            slash.localRotation = Quaternion.Euler(0f, 0f, -6f * slashDirection);
            slash.GetChild(0).localPosition = new Vector3(slashDirection * 0.5f, 0f, 0f);
            slashStartedAt = Time.time;
            slash.gameObject.SetActive(true);
            // Sin esto, el primer cuadro se dibuja con la escala de un cubo entero.
            PaintSlash(slashStartedAt);
        }

        private void Update()
        {
            float now = Time.time;
            for (int i = pieces.Count - 1; i >= 0; i--)
            {
                Piece piece = pieces[i];
                float t = (now - piece.BornAt) / piece.Seconds;
                if (t >= 1f)
                {
                    Destroy(piece.Transform.gameObject);
                    pieces.RemoveAt(i);
                    continue;
                }
                if (piece.Falls)
                {
                    piece.Velocity.y -= Gravity * Time.deltaTime;
                    piece.Transform.localPosition += piece.Velocity * Time.deltaTime;
                    piece.Transform.Rotate(0f, 0f, piece.Spin * Time.deltaTime, Space.World);
                }
                // Las mitades se achican al final; lo prohibido se apaga de una vez.
                float shrink = piece.Falls ? Mathf.InverseLerp(1f, 0.6f, t) : 1f - t;
                piece.Transform.localScale = piece.Scale * shrink;
            }
            PaintSlash(now);
        }

        private void AddHalf(Transform half, Vector3 scale, float side)
        {
            half.localScale = scale;
            half.localPosition += new Vector3(side * scale.x * 0.35f, 0f, 0f);
            pieces.Add(new Piece
            {
                Transform = half,
                Velocity = new Vector3(side * halfSpeed, halfSpeed * 0.8f, 0f),
                Spin = side * halfSpinDegrees,
                BornAt = Time.time,
                Seconds = halfSeconds,
                Scale = scale,
                Falls = true,
            });
        }

        private void SplashJuice(Vector3 position, Color color, float size)
        {
            var juice = new GameObject("Jugo");
            juice.SetActive(false);
            juice.transform.position = position;
            ParticleSystem system = juice.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = system.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f * size, 0.32f * size);
            main.gravityModifier = 1.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)juiceDrops) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f * size;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var renderer = juice.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = DropMesh();
            renderer.sharedMaterial = MaterialFor(color);

            juice.SetActive(true);
            Destroy(juice, 1f);
        }

        // El tajo crece de un lado al otro y después se afina hasta desaparecer.
        private void PaintSlash(float now)
        {
            if (slash == null || !slash.gameObject.activeSelf) return;
            float t = (now - slashStartedAt) / slashSeconds;
            if (t >= 1f)
            {
                slash.gameObject.SetActive(false);
                return;
            }
            float grow = Mathf.Clamp01(t / 0.4f);
            float thin = 1f - Mathf.Clamp01((t - 0.4f) / 0.6f);
            slash.localScale = new Vector3(slashWidth * grow, slashThickness * thin, 0.05f);
        }

        private Transform CreateSlash()
        {
            // Un pivote en un extremo, así el tajo crece desde un lado.
            var pivot = new GameObject("Tajo").transform;
            pivot.SetParent(transform, false);
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "Hoja";
            Destroy(bar.GetComponent<Collider>());
            bar.transform.SetParent(pivot, false);
            bar.GetComponent<Renderer>().sharedMaterial = MaterialFor(slashColor);
            pivot.gameObject.SetActive(false);
            return pivot;
        }

        private Color JuiceColor(int category) =>
            juiceColors != null && category >= 0 && category < juiceColors.Length ? juiceColors[category] : slashColor;

        private Mesh DropMesh()
        {
            if (dropMesh != null) return dropMesh;
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dropMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            Destroy(sphere);
            return dropMesh;
        }

        private Material MaterialFor(Color color)
        {
            if (materials.TryGetValue(color, out Material material)) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            materials.Add(color, material);
            return material;
        }

        private void OnDestroy()
        {
            foreach (Material material in materials.Values) Destroy(material);
        }
    }
}
