using System.Collections.Generic;
using MoviMente.Gestures;
using MoviMente.Net;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class SeekingWanderingDemo : MonoBehaviour
{
    private const float WorldWidth = 8f;
    private const float WorldHeight = 4.5f;
    private const float PlayerSpeed = 5f;
    private const float SeekSpeed = 2.2f;
    private const float WanderSpeed = 1.45f;
    private const int SeekerCount = 4;
    private const int WandererCount = 6;

    private readonly List<Agent> agents = new List<Agent>();
    private Transform player;
    [SerializeField] private PadInputHub input;
    [SerializeField] private ServerConnection connection;
    [SerializeField] private RoomQrLoader qr;
    private Texture2D circleTexture;
    private GUIStyle labelStyle;

    private void Start()
    {
        if (input == null) input = FindFirstObjectByType<PadInputHub>();
        if (connection == null) connection = FindFirstObjectByType<ServerConnection>();
        if (qr == null) qr = FindFirstObjectByType<RoomQrLoader>();
        CreateCamera();
        circleTexture = CreateCircleTexture(64);
        CreateBackground();
        player = CreateCircle("Player", Color.cyan, new Vector2(0f, -2.6f), 0.28f).transform;

        for (int i = 0; i < SeekerCount; i++)
        {
            Vector2 position = new Vector2(-5.8f + i * 1.4f, 1.9f);
            agents.Add(new Agent(CreateCircle("Seeker_" + i, new Color(1f, 0.25f, 0.25f), position, 0.22f).transform, false, i));
        }

        for (int i = 0; i < WandererCount; i++)
        {
            float angle = i * Mathf.PI * 2f / WandererCount;
            Vector2 position = new Vector2(Mathf.Cos(angle) * 3.2f, Mathf.Sin(angle) * 1.7f);
            agents.Add(new Agent(CreateCircle("Wanderer_" + i, new Color(1f, 0.8f, 0.15f), position, 0.18f).transform, true, i));
        }
    }

    private void Update()
    {
        if (player == null) return;
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) ResetDemo();

        Vector2 movement = ReadMovement();
        player.position = ClampPosition(player.position + (Vector3)(movement * PlayerSpeed * Time.deltaTime), 0.28f);

        foreach (Agent agent in agents)
        {
            if (agent.IsWanderer) UpdateWanderer(agent);
            else UpdateSeeker(agent);
            agent.Transform.position = ClampPosition(agent.Transform.position, agent.Radius);
        }
    }

    private Vector2 ReadMovement()
    {
        if (input != null && input.IsCalibrated(1))
        {
            TiltAxes tilt = input.GetTilt(1);
            return new Vector2(tilt.X, tilt.Y).normalized;
        }

        if (Keyboard.current == null) return Vector2.zero;

        float horizontal = (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed ? 1f : 0f)
            - (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed ? 1f : 0f);
        float vertical = (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed ? 1f : 0f)
            - (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed ? 1f : 0f);
        return new Vector2(horizontal, vertical).normalized;
    }

    private void UpdateSeeker(Agent agent)
    {
        Vector2 toPlayer = player.position - agent.Transform.position;
        if (toPlayer.sqrMagnitude > 0.001f)
        {
            agent.Transform.position += (Vector3)(toPlayer.normalized * SeekSpeed * Time.deltaTime);
        }
    }

    private void UpdateWanderer(Agent agent)
    {
        agent.WanderAngle += (Mathf.PerlinNoise(agent.Seed, Time.time * 0.35f) - 0.5f) * 2.4f * Time.deltaTime;
        Vector2 forward = agent.Velocity.normalized;
        Vector2 circleCenter = forward * 0.7f;
        float wanderAngle = agent.WanderAngle;
        Vector2 target = circleCenter + new Vector2(Mathf.Cos(wanderAngle), Mathf.Sin(wanderAngle)) * 0.75f;
        agent.Velocity = Vector2.Lerp(agent.Velocity, target.normalized, 1.8f * Time.deltaTime).normalized;
        agent.Transform.position += (Vector3)(agent.Velocity * WanderSpeed * Time.deltaTime);
    }

    private void ResetDemo()
    {
        player.position = new Vector3(0f, -2.6f, 0f);
        for (int i = 0; i < agents.Count; i++)
        {
            Agent agent = agents[i];
            if (agent.IsWanderer)
            {
                float angle = agent.Index * Mathf.PI * 2f / WandererCount;
                agent.Transform.position = new Vector2(Mathf.Cos(angle) * 3.2f, Mathf.Sin(angle) * 1.7f);
            }
            else
            {
                agent.Transform.position = new Vector2(-5.8f + agent.Index * 1.4f, 1.9f);
            }
        }
    }

    private void OnGUI()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, normal = { textColor = Color.white } };
        }

        GUI.Label(new Rect(24f, 18f, 700f, 32f), "SEEKING + WANDERING", labelStyle);
        GUI.Label(new Rect(24f, 48f, 900f, 28f), "Azul: jugador (WASD/flechas)   Rojo: seeking   Amarillo: wandering   R: reiniciar", labelStyle);
        if (connection != null)
        {
            string room = connection.Link.RoomCode ?? "conectando...";
            GUI.Label(new Rect(24f, 82f, 800f, 28f), "Escanea el QR con el celular | Sala: " + room, labelStyle);
            if (qr != null && qr.Qr != null)
            {
                GUI.DrawTexture(new Rect(Screen.width - 220f, 24f, 196f, 196f), qr.Qr, ScaleMode.ScaleToFit);
            }
        }
    }

    private void CreateCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
        }

        camera.orthographic = true;
        camera.orthographicSize = 5.2f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.backgroundColor = new Color(0.035f, 0.055f, 0.09f);
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    private void CreateBackground()
    {
        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "PlayArea";
        background.transform.position = new Vector3(0f, 0f, 1f);
        background.transform.localScale = new Vector3(WorldWidth * 2f, WorldHeight * 2f, 1f);
        background.GetComponent<Renderer>().material.color = new Color(0.07f, 0.11f, 0.18f);
    }

    private GameObject CreateCircle(string name, Color color, Vector2 position, float radius)
    {
        GameObject circle = new GameObject(name);
        circle.transform.position = position;
        circle.transform.localScale = Vector3.one * radius * 2f;
        SpriteRenderer renderer = circle.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(circleTexture, new Rect(0, 0, circleTexture.width, circleTexture.height), new Vector2(0.5f, 0.5f), circleTexture.width);
        renderer.color = color;
        return circle;
    }

    private static Texture2D CreateCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        float center = (size - 1) * 0.5f;
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                pixels[y * size + x] = distance <= radius ? Color.white : Color.clear;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Vector3 ClampPosition(Vector3 position, float radius)
    {
        return new Vector3(
            Mathf.Clamp(position.x, -WorldWidth + radius, WorldWidth - radius),
            Mathf.Clamp(position.y, -WorldHeight + radius, WorldHeight - radius),
            0f);
    }

    private sealed class Agent
    {
        public readonly Transform Transform;
        public readonly bool IsWanderer;
        public readonly int Index;
        public readonly float Radius;
        public readonly float Seed;
        public Vector2 Velocity = Vector2.right;
        public float WanderAngle;

        public Agent(Transform transform, bool isWanderer, int index)
        {
            Transform = transform;
            IsWanderer = isWanderer;
            Index = index;
            Radius = isWanderer ? 0.18f : 0.22f;
            Seed = index * 1.73f + 0.5f;
            WanderAngle = index;
        }
    }
}
