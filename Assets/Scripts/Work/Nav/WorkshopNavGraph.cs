using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Red de navegación del taller. Los carritos le piden el camino de un punto a
/// otro y ella devuelve una ruta que rodea los muebles.
///
/// Funciona como un grafo de visibilidad: se unen entre sí los nodos que se
/// ven sin obstáculo por medio, y se busca el camino más corto con Dijkstra.
/// El origen y el destino se meten en el grafo en cada consulta, así que no hay
/// rutas precalculadas que se rompan al mover algo — que era justo el problema
/// de los waypoints fijos.
///
/// Hay una por taller. Antes era única, y con dos talleres los carritos del
/// segundo habrían pedido ruta a la red del primero: sus nodos están a catorce
/// unidades, así que el carrito se habría ido a cruzar el otro taller para
/// llegar a la mesa de al lado. Cada carrito pide la suya con <see cref="For"/>.
/// </summary>
public class WorkshopNavGraph : MonoBehaviour
{
    /// <summary>
    /// La primera red que despertó. Solo queda para lo que no está dentro de
    /// ningún taller; lo que sí lo está debe usar <see cref="For"/>.
    /// </summary>
    public static WorkshopNavGraph Instance { get; private set; }

    /// <summary>
    /// La red del taller en el que está <paramref name="component"/>.
    ///
    /// Si no está dentro de ningún taller —una escena de pruebas— devuelve la
    /// global, que es lo que hacía todo antes.
    /// </summary>
    public static WorkshopNavGraph For(Component component)
    {
        Workshop workshop = Workshop.Of(component);

        if (workshop != null)
        {
            WorkshopNavGraph own = workshop.GetComponentInChildren<WorkshopNavGraph>(includeInactive: true);
            if (own != null) return own;
        }

        return Instance;
    }

    [Tooltip("Puntos de paso: esquinas y cruces del espacio libre. No hace falta " +
             "que estén conectados a mano, se calcula qué ve cada uno")]
    [SerializeField] private Transform[] nodes;

    [Tooltip("Cuánto ocupa el que se mueve. Los obstáculos se inflan por este " +
             "valor, así que el carrito nunca pasa rozando")]
    [SerializeField] private float agentRadius = 0.4f;

    [Tooltip("Recoger todos los NavObstacle de la escena al arrancar")]
    [SerializeField] private bool autoCollectObstacles = true;

    [SerializeField] private List<NavObstacle> obstacles = new();

    private void Awake()
    {
        // Ya no es un error que haya varias: hay una por taller.
        if (Instance == null) Instance = this;

        CollectObstacles();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Los muebles que rodear, solo los de su propio taller. Los del otro están
    /// lejos y no estorban a ninguna ruta, pero meterlos haría cada consulta
    /// más lenta para nada, y un día que los talleres estuvieran más juntos
    /// empezarían a desviar carritos por muebles que no están en su taller.
    /// </summary>
    private void CollectObstacles()
    {
        if (!autoCollectObstacles) return;

        obstacles.Clear();
        obstacles.AddRange(OwnObstacles());
    }

    private IEnumerable<NavObstacle> OwnObstacles()
    {
        Workshop workshop = Workshop.Of(this);

        return workshop != null
            ? workshop.GetComponentsInChildren<NavObstacle>(includeInactive: false)
            : FindObjectsByType<NavObstacle>(FindObjectsInactive.Exclude);
    }

    // ── Consulta ─────────────────────────────────────────────────────

    /// <summary>
    /// Camino de <paramref name="from"/> a <paramref name="to"/>, sin incluir
    /// el origen. Si se ven directamente, devuelve solo el destino.
    /// </summary>
    public List<Vector3> FindPath(Vector3 from, Vector3 to)
    {
        var path = new List<Vector3>();

        if (IsClear(from, to))
        {
            path.Add(to);
            return path;
        }

        // Grafo de la consulta: 0 = origen, 1..n = nodos, n+1 = destino.
        List<Vector3> points = BuildQueryPoints(from, to);
        int count = points.Count;
        int target = count - 1;

        var distance = new float[count];
        var previous = new int[count];
        var visited = new bool[count];

        for (int i = 0; i < count; i++)
        {
            distance[i] = float.PositiveInfinity;
            previous[i] = -1;
        }
        distance[0] = 0f;

        for (int step = 0; step < count; step++)
        {
            int current = NearestUnvisited(distance, visited);
            if (current == -1 || current == target) break;

            visited[current] = true;

            for (int next = 0; next < count; next++)
            {
                if (visited[next] || next == current) continue;
                if (!IsClear(points[current], points[next])) continue;

                float candidate = distance[current] + Vector3.Distance(points[current], points[next]);
                if (candidate >= distance[next]) continue;

                distance[next] = candidate;
                previous[next] = current;
            }
        }

        if (float.IsPositiveInfinity(distance[target]))
        {
            // Mejor ir en línea recta que quedarse parado: se ve el problema
            // en pantalla en vez de que el carrito se congele en silencio.
            Debug.LogWarning(
                $"[WorkshopNavGraph] No hay camino de {from} a {to}. " +
                "¿Faltan nodos, o el origen/destino está pegado a un mueble? Se va en línea recta.", this);

            path.Add(to);
            return path;
        }

        for (int at = target; at > 0; at = previous[at])
            path.Add(points[at]);

        path.Reverse();
        return path;
    }

    private List<Vector3> BuildQueryPoints(Vector3 from, Vector3 to)
    {
        var points = new List<Vector3> { from };

        if (nodes != null)
            foreach (Transform node in nodes)
                if (node != null) points.Add(node.position);

        points.Add(to);
        return points;
    }

    private static int NearestUnvisited(float[] distance, bool[] visited)
    {
        int best = -1;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < distance.Length; i++)
        {
            if (visited[i] || distance[i] >= bestDistance) continue;
            best = i;
            bestDistance = distance[i];
        }

        return best;
    }

    /// <summary>¿Se puede ir de a a b en línea recta sin chocar?</summary>
    public bool IsClear(Vector3 a, Vector3 b)
    {
        foreach (NavObstacle obstacle in obstacles)
        {
            if (obstacle == null) continue;
            if (obstacle.BlocksSegment(a, b, agentRadius)) return false;
        }

        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (nodes == null) return;

        // En editor no ha pasado por Awake: se recogen al vuelo para poder
        // ver qué conexiones existen sin darle a play.
        if (obstacles.Count == 0)
            obstacles.AddRange(OwnObstacles());

        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] == null) continue;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(nodes[i].position, 0.12f);

            for (int j = i + 1; j < nodes.Length; j++)
            {
                if (nodes[j] == null) continue;

                bool clear = IsClear(nodes[i].position, nodes[j].position);
                Gizmos.color = clear
                    ? new Color(0f, 1f, 1f, 0.45f)
                    : new Color(1f, 0f, 0f, 0.12f);

                Gizmos.DrawLine(nodes[i].position, nodes[j].position);
            }
        }
    }
#endif
}
