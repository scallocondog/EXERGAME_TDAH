// RF-11: técnica de movimiento Wandering (Reynolds) para los objetos que caen en difícil
using System;

namespace MoviMente.Games.AtrapaLoCorrecto
{
    // Wandering: el objeto proyecta un círculo por delante, en la dirección en
    // que ya se mueve, y apunta a un punto del borde cuyo ángulo se corre un
    // poco al azar en cada cuadro. Girar hacia ese punto de a poco da un rumbo
    // que cambia suave, sin saltos: parece que el objeto "pasea" mientras cae.
    //
    // Solo se aplica al movimiento lateral. La caída sigue a velocidad fija para
    // que el tiempo que tiene el jugador sea el mismo que sin wandering y los
    // tiempos de reacción se puedan comparar entre partidas (RF-15).
    internal static class Wandering
    {
        // Círculo en unidades del rumbo (vector de largo 1).
        private const float CircleDistance = 1f;
        private const float CircleRadius = 0.8f;
        // Cuánto se corre el punto sobre el círculo por segundo: más alto, más nervioso.
        // La caída dura 2,4 s en difícil; con menos jitter el rumbo casi no cambia y el
        // objeto baja en diagonal en vez de pasear.
        private const float JitterRadiansPerSecond = 40f;
        // Qué tan rápido la velocidad lateral alcanza la deseada: el giro es suave.
        private const float SteeringRate = 3f;

        public static void Step(FallingItem item, float fallSpeed, float maxLateralSpeed, float margin,
            float deltaSeconds, Random random)
        {
            // 1. El punto objetivo se desplaza un poco sobre el borde del círculo.
            item.WanderAngle += (float)(random.NextDouble() * 2.0 - 1.0) * JitterRadiansPerSecond * deltaSeconds;

            // 2. Centro del círculo: por delante, en el rumbo actual (lateral + caída).
            float speed = (float)Math.Sqrt(item.VelocityX * item.VelocityX + fallSpeed * fallSpeed);
            float headingX = item.VelocityX / speed;

            // 3. Objetivo = centro + punto del borde; interesa su componente lateral.
            float targetX = headingX * CircleDistance + (float)Math.Cos(item.WanderAngle) * CircleRadius;
            float desiredVelocityX = Math.Max(-1f, Math.Min(1f, targetX)) * maxLateralSpeed;

            // 4. Steering: la velocidad se acerca a la deseada sin girar de golpe.
            item.VelocityX += (desiredVelocityX - item.VelocityX) * Math.Min(1f, SteeringRate * deltaSeconds);

            // 5. En el borde del campo rebota, para que siempre se pueda atrapar.
            float x = item.X + item.VelocityX * deltaSeconds;
            if (Math.Abs(x) > margin)
            {
                x = Math.Sign(x) * margin;
                item.VelocityX = -item.VelocityX;
                item.WanderAngle = (float)Math.PI - item.WanderAngle;
            }
            item.X = x;
        }
    }
}
