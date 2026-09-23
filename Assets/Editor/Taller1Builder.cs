using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Construye los talleres en la escena abierta, calcados de la hoja de diseño:
/// las dos colas del hall, los dos mostradores de recepción, los dos
/// transportistas con carrito, las tres mesas de limpieza con su worker, las
/// decoraciones, el almacén y la habitación; y deja todas las referencias
/// cableadas.
///
/// Construye varios talleres iguales, uno al lado del otro. El primero es el de
/// partida; los demás quedan apagados hasta que el jugador los compra. Por
/// ahora el segundo es una copia del primero: cuando exista su diseño propio,
/// se construirá con el suyo.
///
/// Es idempotente: vuelve a ejecutarlo y reconstruye los talleres desde cero,
/// conservando dos cosas que no se pueden perder al reconstruir:
///   - Dónde está cada taller. El primero está movido a mano para cuadrar con
///     los suelos, y antes cada reconstrucción lo devolvía al origen.
///   - Los ids de guardado. Cada elemento nuevo recibía un id nuevo, y la
///     partida del jugador se quedaba con los viejos: reconstruir borraba en
///     silencio todo lo comprado en salas, decoraciones y carritos.
///
/// Las coordenadas son locales a la raíz de cada taller y salen de medir la
/// imagen de diseño sobre los suelos ya colocados:
///   Taller limpieza  X[-1.84, 5.03]  Y[1.75, 6.77]
///   Recepción        X[-0.21, 5.03]  Y[-1.69, 1.75]
///   Hall             X[-3.43, 5.03]  Y[-6.77, -1.69]
/// </summary>
public static class Taller1Builder
{
    private const string WorkDeskPrefabPath = "Assets/Prefabs/WorkDesk/WorkDesk.prefab";

    // ── Talleres ──────────────────────────────────────────────────────

    /// <summary>Cuántos talleres se construyen. Todos iguales por ahora.</summary>
    private const int WorkshopCount = 2;

    /// <summary>
    /// Cuánto se aparta cada taller del anterior. Un taller ocupa unas diez
    /// unidades de ancho, así que con catorce quedan cuatro de hueco entre
    /// ellos: se distinguen bien y ninguno asoma en la pantalla del otro.
    /// </summary>
    private static readonly Vector2 WorkshopSpacing = new(14f, 0f);

    /// <summary>
    /// Lo que cuesta el segundo taller si no hay nada mejor que usar. Es el
    /// valor que ya tenías en WorkStation2.asset; si ese asset existe, se lee
    /// de ahí.
    /// </summary>
    private const double DefaultNextWorkshopCost = 10000;
    private const string LegacyWorkStation2Path = "Assets/ScriptableObjects/WorkStation/WorkStation2.asset";

    /// <summary>
    /// La suma de niveles que tiene que tener un taller para poder comprar el
    /// siguiente, si no hay nada puesto a mano. Un taller recién abierto suma
    /// 5 (la primera mesa, los dos carritos y los dos mostradores) y al
    /// máximo, 588.
    /// </summary>
    private const int DefaultRequiredPreviousLevel = 100;

    /// <summary>Nombre de la raíz del taller número <paramref name="index"/> (desde 0).</summary>
    private static string RootNameFor(int index) => $"Taller{index + 1}";

    /// <summary>
    /// Lo que ocupa un taller, en coordenadas locales: la unión de todas sus
    /// salas, del almacén a la izquierda al hall por abajo.
    /// </summary>
    private static readonly Vector2 WorkshopBoundsCenter = new(-0.005f, 0f);
    private static readonly Vector2 WorkshopBoundsSize = new(10.08f, 13.54f);

    /// <summary>
    /// A dónde va la cámara si en la escena no hay cámara de la que sacarlo:
    /// entre la recepción y el taller, que es donde pasa casi todo.
    /// </summary>
    private static readonly Vector2 DefaultHomePoint = new(1.60f, 0.40f);

    // De aquí se saca el círculo de progreso: el mismo que usan los workers de
    // mesa, clonado tal cual, para que no haya dos círculos distintos.
    private const string WorkerPrefabPath = "Assets/Prefabs/Worker/Worker.prefab";

    /// <summary>Escala a la que se ven los trabajadores, y con ellos su círculo.</summary>
    private const float ActorScale = 0.5f;

    // ── Geometría del layout ─────────────────────────────────────────

    // Hall: carriles de las dos colas
    private const float RightLaneX = 3.62f;   // cola de entrega ("dar")
    private const float LeftLaneX = 0.88f;    // cola de recogida
    private const float QueueHeadY = -2.35f;  // primer cliente, frente al mostrador
    private const float QueueSpacingY = -0.85f;
    private const int QueueLength = 5;

    private static readonly Vector2 EntranceXY = new(0.21f, -6.35f);
    private static readonly Vector2 OffMapXY = new(0.21f, -7.40f);

    // Recepción. Los dos mostradores son idénticos y solo cambian de X: mismo
    // mostrador delante, mismo recepcionista, y la misma mesa detrás.
    private const float CounterY = -1.80f;
    private const float ReceptionistIdleY = -1.25f;
    private const float BackDeskY = -0.50f;
    private const float PickupStackY = BackDeskY;
    private static readonly Vector2 CounterSize = new(1.00f, 0.45f);
    private static readonly Vector2 BackDeskSize = new(1.40f, 0.50f);
    private static readonly Vector2 PickupTableSize = BackDeskSize;

    // Mesas del taller. Cada una tiene la zona de espera (Box) en un extremo y
    // la parte contraria (Out) en el otro, donde quedan los terminados cuando
    // esa mesa es la última desbloqueada.
    private static readonly Vector2 Desk1Pos = new(3.79f, 4.33f);
    private static readonly Vector2 Desk1Box = new(3.98f, 3.30f);
    private static readonly Vector2 Desk1Out = new(3.98f, 5.35f);
    private static readonly Vector2 Desk1Item = new(3.98f, 4.46f);
    private static readonly Vector2 Desk1Player = new(4.47f, 4.42f);

    private static readonly Vector2 Desk2Pos = new(1.64f, 5.80f);
    private static readonly Vector2 Desk2Box = new(2.20f, 5.80f);
    private static readonly Vector2 Desk2Out = new(1.10f, 5.80f);
    private static readonly Vector2 Desk2Item = new(1.50f, 5.78f);
    private static readonly Vector2 Desk2Player = new(1.50f, 6.20f);

    private static readonly Vector2 Desk3Pos = new(-0.58f, 4.33f);
    private static readonly Vector2 Desk3Box = new(-0.80f, 5.20f);
    private static readonly Vector2 Desk3Out = new(-0.80f, 3.30f);
    private static readonly Vector2 Desk3Item = new(-0.80f, 4.46f);
    private static readonly Vector2 Desk3Player = new(-1.39f, 4.42f);

    private static readonly Vector2 DeskVerticalSize = new(1.05f, 2.36f);
    private static readonly Vector2 DeskHorizontalSize = new(1.32f, 0.50f);

    // ── Pasillos por los que circulan los carritos ────────────────────
    // Las mesas ocupan de y=3.15 a y=6.05, y en horizontal dejan un hueco
    // libre entre la mesa 2 (acaba en x=2.30) y la mesa 1 (empieza en x=3.27).
    // Moviéndose solo por estas líneas, un carrito no puede atravesar nada.
    private const float BottomCorridorY = 2.45f;   // por debajo de todas las mesas
    private const float VerticalCorridorX = 2.80f; // entre la mesa 2 y la mesa 1
    private const float TopCorridorY = 6.00f;      // por encima de la mesa 1

    // Sitios de reposo de cada carrito. Todo trayecto sale de aquí y vuelve
    // aquí, por eso los caminos de aproximación se miden desde este punto.
    // Los dos descansan en la sala amarilla, que es la zona desde la que se
    // mejoran: si uno reposara en la naranja, tocarlo abriría el panel de mesas.
    private static readonly Vector2 InCartIdle = new(4.50f, 0.80f);
    private static readonly Vector2 OutCartIdle = new(VerticalCorridorX, 1.00f);

    // Composición del carrito: el trabajador va detrás y el carro delante, sin
    // solaparse. El conjunto ocupa ~0.75 x 0.95 centrado en el punto de atraque,
    // y de ahí salen las distancias de seguridad de los atraques.
    private static readonly Vector2 CartWorkerBodyOffset = new(0f, 0.24f);
    private static readonly Vector2 CartBodyOffset = new(0f, -0.22f);
    private static readonly Vector2 CartBoxSize = new(0.62f, 0.40f);

    // El único tope del circuito es el del carrito: dice cuántos encargos se
    // lleva por viaje, y por tanto cuántos tienen que juntarse para que salga.
    // Las bolsas fijas no tienen tope, así que van acumulando mientras el
    // carrito está de viaje y la cola de clientes nunca se queda atascada.
    private const int CartCapacity = 5;

    // Los sacos no cambian de tamaño ni de color con la carga: solo dan el
    // golpecito. Un tamaño y un color, iguales para todos.
    private static readonly Vector2 SackSize = new(0.44f, 0.38f);
    private static readonly Color SackColor = new(0.28f, 0.62f, 0.30f);

    // Puntos de atraque: el conjunto trabajador+carrito queda entero fuera del
    // mueble, y el objeto salva lo que falte dando un saltito.
    private static readonly Vector2 Desk1BoxDock = new(3.98f, 2.60f);
    private static readonly Vector2 Desk1OutDock = new(3.98f, 6.20f);
    private static readonly Vector2 Desk2OutDock = new(1.10f, 4.95f);
    private static readonly Vector2 Desk3OutDock = new(-0.80f, 2.60f);
    private const float ReceptionDockY = 0.40f;
    private static readonly Vector2 BackDeskDock = new(RightLaneX, ReceptionDockY);
    private static readonly Vector2 PickupDock = new(LeftLaneX, ReceptionDockY);

    // El círculo del mostrador sale al lado del cliente que está siendo
    // atendido, no sobre el recepcionista: es el cliente quien tarda.
    private static readonly Vector3 DropOffProgressOffset = new(0.62f, -0.40f, 0f);
    private static readonly Vector3 PickupProgressOffset = new(0f, -0.15f, 0f);

    // ── Zonas mejorables ──────────────────────────────────────────────
    // La sala naranja mejora sus mesas; la amarilla, los carritos; y todo el
    // hall azul, las recepciones. Las cajas no se solapan entre ellas.
    private static readonly Vector2 CartZoneCenter = new(2.41f, 0.40f);
    private static readonly Vector2 CartZoneSize = new(5.24f, 2.70f);
    private static readonly Vector2 CartZoneFocus = new(2.41f, 0.03f);

    // Toda la zona azul: se toca en cualquier punto del hall.
    private static readonly Vector2 HallCenter = new(0.80f, -4.23f);
    private static readonly Vector2 HallSize = new(8.46f, 5.08f);

    // ── Sala de espera ────────────────────────────────────────────────
    // En el hueco vacío de la izquierda del hall. Ahí esperan sentados los
    // clientes que ya entregaron; quien no encuentra sitio se va y vuelve.
    // 3 columnas x 4 filas = los 12 asientos. Van de fuera hacia recepción y
    // de arriba abajo; el orden en que aparecen lo decide BuildWaitingArea.
    private static readonly float[] SeatColumns = { -3.00f, -2.10f, -1.20f };
    private static readonly float[] SeatRows = { -2.60f, -3.50f, -4.40f, -5.30f };

    // Los asientos se compran; hasta entonces no se ven y nadie puede sentarse.
    // Las monedas que suma cada nivel viven en SeatsUpgrade.asset, no aquí.
    private const double SeatsUnlockCost = 500;

    /// <summary>
    /// Nivel al que aparece cada asiento. Al comprarlos sale el primero (nivel
    /// 1) y los demás van saliendo con saltos de 2, 3, 6, 9... hasta 30.
    /// Un umbral por asiento: son los mismos tramos que mueven la barra de
    /// progreso del panel, así que lo que se ve y lo que marca la barra van
    /// siempre a la par.
    /// </summary>
    private static readonly int[] SeatLevelThresholds =
        { 1, 3, 6, 12, 21, 33, 48, 66, 87, 111, 138, 168 };

    // ── Decoraciones ──────────────────────────────────────────────────
    // Cada una suma monedas a cada cobro del taller y va apareciendo a
    // trozos. Los umbrales son iguales para las cuatro decoraciones de 4
    // piezas: la primera al comprarla y las demás a los niveles 5, 15 y 30.
    private static readonly int[] DecorationThresholds = { 1, 5, 15, 30 };

    // Taller (sala naranja). Las estanterías van pegadas al borde de arriba,
    // a la izquierda de la mesa 2; las plantas, en la columna de la derecha.
    // Ninguna se cruza con los atraques de los carritos.
    private static readonly Vector2[] ShelfPieces =
    {
        new(-1.40f, 6.45f), new(-0.75f, 6.45f), new(-0.10f, 6.45f), new(0.55f, 6.45f),
    };

    private static readonly Vector2[] WorkshopPlantPieces =
    {
        new(4.72f, 3.05f), new(4.72f, 3.95f), new(4.72f, 4.85f), new(4.72f, 5.75f),
    };

    // Hall (zona azul). Las plantas a la derecha del carril de entrega; las
    // lámparas en el hueco entre los asientos y el carril de recogida.
    // Nada a y = -6.35: por ahí es por donde entran y salen los clientes.
    private static readonly Vector2[] HallPlantPieces =
    {
        new(4.55f, -2.35f), new(4.55f, -3.35f), new(4.55f, -4.35f), new(4.55f, -5.35f),
    };

    private static readonly Vector2[] LampPieces =
    {
        new(-0.10f, -2.60f), new(-0.10f, -3.50f), new(-0.10f, -4.40f), new(-0.10f, -5.30f),
    };


    // Zona naranja: la sala que se toca para abrir el panel de mejoras, y a
    // cuyo centro se lleva la cámara. Coincide con el suelo SueloTallerLimpieza.
    private static readonly Vector2 CleaningRoomCenter = new(1.60f, 4.26f);
    private static readonly Vector2 CleaningRoomSize = new(6.87f, 5.02f);

    // ── Salas de la izquierda ─────────────────────────────────────────
    // Coinciden con SueloZonaMateriales (morada) y SueloHabitacion (rosa).
    // Ahí no entra ningún carrito: sus trabajadores no mueven objetos, así que
    // los bloqueadores de navegación que las pisan siguen sin estorbar.
    private static readonly Vector2 StorageCenter = new(-3.45f, 4.68f);
    private static readonly Vector2 StorageSize = new(3.19f, 2.81f);

    private static readonly Vector2 BedroomCenter = new(-3.45f, 2.00f);
    private static readonly Vector2 BedroomSize = new(3.19f, 2.54f);

    // Las dos salas se compran enteras antes de dar nada.
    private const double StorageUnlockCost = 5000;
    private const double BedroomUnlockCost = 12000;

    // El segundo del almacén se compra aparte, ya dentro de la sala.
    private const double StorageSecondWorkerCost = 8000;

    private static readonly Vector2[] StorageWorkerSpots =
    {
        new(-4.15f, 4.68f), new(-2.75f, 4.68f),
    };

    // Uno cálido y otro frío: el suelo del almacén es morado, así que un
    // trabajador morado se perdía contra él, y los dos iguales no había forma
    // de saber cuál estabas mejorando.
    private static readonly Color[] StorageWorkerColors =
    {
        new(0.91f, 0.64f, 0.24f),   // ámbar
        new(0.18f, 0.66f, 0.63f),   // turquesa
    };

    private static readonly Vector2 IdleWorkerSize = new(0.34f, 0.62f);

    // ── Habitación: el jefe y sus mejoras ─────────────────────────────
    // No produce nada por sí misma: sus mejoras tiran de los mostradores del
    // hall y del bonus de monedas. El personaje está por ambiente.
    private static readonly Vector2 BedroomHome = new(-3.45f, 2.00f);

    /// <summary>
    /// La ronda del jefe, de ida. Sale de la habitación por el hueco que la
    /// une al taller (y entre 1,75 y 3,27), baja al pasillo de abajo y sube por
    /// el corredor central. La vuelta es la misma al revés.
    /// </summary>
    private static readonly Vector2[] BedroomPatrol =
    {
        new(-2.30f, 2.45f),   // hacia la puerta, aún dentro de la habitación
        new(-1.40f, 2.45f),   // ya en el taller
        new(-0.80f, 2.45f),   // al lado de la mesa 3
        new(1.60f, 2.45f),    // pasillo de abajo
        new(2.80f, 2.45f),    // corredor central
        new(2.80f, 4.95f),    // entre las mesas
        new(2.80f, 6.00f),    // fondo del taller
    };

    private static readonly Color BossColor = new(0.86f, 0.36f, 0.42f);

    // De aquí no sale pase lo que pase: la habitación más el taller. Es una
    // red de seguridad, no el camino; la ronda ya va por donde debe.
    private static readonly Vector2 BossAreaCenter = new(-0.005f, 3.75f);
    private static readonly Vector2 BossAreaSize = new(10.08f, 6.04f);

    // Nodos de la red de navegación: cruces y esquinas del espacio libre. No se
    // conectan a mano — WorkshopNavGraph calcula qué nodos se ven entre sí y
    // busca el camino más corto en cada consulta.
    private static readonly Vector2[] NavNodes =
    {
        new(VerticalCorridorX, BottomCorridorY),  // cruce pasillo inferior / vertical
        new(VerticalCorridorX, 4.95f),            // pasillo vertical, altura mesa 2
        new(VerticalCorridorX, TopCorridorY),     // arriba del pasillo vertical
        new(-0.80f, BottomCorridorY),             // pasillo inferior, lado mesa 3
        new(3.98f, BottomCorridorY),              // pasillo inferior, lado mesa 1
        new(VerticalCorridorX, 1.30f),            // bajada del taller a recepción
        new(LeftLaneX, 1.10f),                    // recepción, sobre la mesa de recogida
        new(4.50f, 1.60f),                        // recepción, lado derecho
    };

    private const float AgentRadius = 0.40f;

    private const int OrderFurniture = SortingOrders.Furniture;
    private const int OrderStack = SortingOrders.Container;
    private const int OrderActor = SortingOrders.Actor;

    // ── Menú ─────────────────────────────────────────────────────────

    [MenuItem("Taller/Construir talleres")]
    public static void Build()
    {
        GameObject deskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorkDeskPrefabPath);

        if (deskPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Talleres",
                $"No encuentro el prefab de mesa:\n{WorkDeskPrefabPath}",
                "Vale");
            return;
        }

        // Antes de tocar la escena: si algún asset de mejora no es del tipo que
        // espera su componente, el cableado saldría a null y el panel de
        // mejoras fallaría al abrirlo. Mejor no construir nada.
        if (!UpgradeAssetsAreValid()) return;

        // Buscados entre las raíces de la escena y no con GameObject.Find, que
        // no ve los objetos apagados: el segundo taller lo está, y no
        // encontrarlo haría que cada reconstrucción añadiera uno más.
        GameObject[] existing = FindWorkshopRoots();

        if (System.Array.Exists(existing, go => go != null))
        {
            bool replace = EditorUtility.DisplayDialog(
                "Talleres",
                "Se van a reconstruir los talleres desde cero.\n\n" +
                "Se conservan su posición y los ids de guardado, así que la partida " +
                "guardada sigue valiendo.\n\n¿Continuar?",
                "Reconstruir", "Cancelar");

            if (!replace) return;
        }

        // Todo lo que hay que conservar se lee ANTES de borrar nada.
        Vector3 firstPosition = existing[0] != null ? existing[0].transform.position : Vector3.zero;

        var savedIds = new Dictionary<string, string>[WorkshopCount];
        var savedSettings = new WorkshopSettings[WorkshopCount];

        for (int i = 0; i < WorkshopCount; i++)
        {
            savedIds[i] = existing[i] != null ? CaptureSaveIds(existing[i]) : new Dictionary<string, string>();
            savedSettings[i] = existing[i] != null ? WorkshopSettings.From(existing[i]) : null;
        }

        CustomerManager template = TakeCustomerTemplate(existing[0]);
        if (template == null) return;

        Transform map = FindRoot("Map")?.transform;
        Vector3 homeLocal = HomeLocal(firstPosition);

        foreach (GameObject go in existing)
            if (go != null) Undo.DestroyObjectImmediate(go);

        var workshops = new List<Workshop>();

        for (int i = 0; i < WorkshopCount; i++)
        {
            GameObject root = BuildWorkshop(i, deskPrefab, template, savedSettings[i], homeLocal);

            // Los suelos del primero son los del mapa, puestos a mano. Los
            // demás se copian de esos, así que quedan idénticos.
            if (i > 0 && map != null) CloneFloors(root, map, firstPosition);

            root.transform.position = firstPosition + (Vector3)(WorkshopSpacing * i);

            RestoreSaveIds(root, savedIds[i]);
            VerifyUpgradeablesWired(root);

            workshops.Add(root.GetComponent<Workshop>());
        }

        Object.DestroyImmediate(template.gameObject);

        // Los ids que falten —elementos nuevos, o el segundo taller la primera
        // vez— se asignan ahora, con los talleres ya montados.
        int assigned = SaveAudit.AssignMissingIds();

        // Apagados después de todo lo demás: con el taller encendido mientras
        // se monta, Unity trata cada pieza como si estuviera activa y el
        // cableado es el mismo que el del primero.
        for (int i = 1; i < workshops.Count; i++)
            workshops[i].gameObject.SetActive(false);

        RetireSceneCustomerManager();
        WireUnlocker(workshops);
        WireTapHandler();
        WireCamera(workshops);
        BuildWorkshopUI();

        Selection.activeGameObject = workshops[0].gameObject;
        EditorSceneMarkDirty();

        Debug.Log(
            $"[Taller1Builder] {workshops.Count} talleres construidos. " +
            (assigned > 0 ? $"{assigned} id(s) de guardado nuevos. " : "") +
            "Revisa la escena y guarda (Ctrl+S).");
    }

    /// <summary>
    /// Monta un taller completo con la raíz en el origen. Quien llama lo
    /// coloca después en su sitio.
    ///
    /// Se monta en el origen y no directamente en su sitio porque todas las
    /// coordenadas del layout son locales al taller: así un taller es igual
    /// que otro y solo cambia dónde se deja al final.
    /// </summary>
    private static GameObject BuildWorkshop(
        int index, GameObject deskPrefab, CustomerManager template,
        WorkshopSettings saved, Vector3 homeLocal)
    {
        GameObject root = new(RootNameFor(index));
        Undo.RegisterCreatedObjectUndo(root, "Construir talleres");

        Workshop workshop = root.AddComponent<Workshop>();
        SerializedObject workshopSo = new(workshop);
        workshopSo.FindProperty("displayName").stringValue =
            saved?.DisplayName ?? $"Taller {index + 1}";
        workshopSo.FindProperty("unlockCost").doubleValue =
            saved?.UnlockCost ?? (index == 0 ? 0 : NextWorkshopCost());
        workshopSo.FindProperty("requiredPreviousLevel").intValue =
            saved?.RequiredPreviousLevel ?? (index == 0 ? 0 : DefaultRequiredPreviousLevel);
        workshopSo.FindProperty("homePoint").objectReferenceValue =
            NewChild(root.transform, "Inicio", homeLocal).transform;
        workshopSo.FindProperty("boundsCenter").vector2Value = WorkshopBoundsCenter;
        workshopSo.FindProperty("boundsSize").vector2Value = WorkshopBoundsSize;
        workshopSo.ApplyModifiedPropertiesWithoutUndo();

        WorkStation station = root.AddComponent<WorkStation>();

        // Sin collider el raycast del TapHandler no encuentra nada y el panel
        // de mejoras de la sala no llega a abrirse nunca. Cubre solo la sala
        // naranja: es lo que hay que tocar para abrirlo.
        BoxCollider2D roomCollider = root.AddComponent<BoxCollider2D>();
        roomCollider.offset = CleaningRoomCenter;
        roomCollider.size = CleaningRoomSize;

        ReceptionDesk dropOff = BuildDropOffDesk(root, out ItemStack backStack);
        PickupDesk pickup = BuildPickupDesk(root, out ItemStack pickupStack);

        List<WorkDeskUnlockable> desks = BuildCleaningDesks(root, deskPrefab, out List<WorkTable> tables);
        WorkshopOutput workshopOutput = BuildWorkshopOutput(root, station);

        // Antes de crear los carritos: son quienes la consultan.
        BuildVoidBlockers(root);
        BuildNavGraph(root);

        CartWorker inCart = BuildCart(root, "CarritoEntrada", InCartIdle, backStack, tables[0]);
        CartWorker outCart = BuildCart(root, "CarritoSalida", OutCartIdle, workshopOutput, pickupStack);

        HallAnchors hall = BuildHall(root);
        WaitingArea waitingArea = BuildWaitingArea(root, out Transform seatsFocus);

        // Decoraciones: suman monedas a cada cobro y van saliendo a trozos.
        List<BuiltDecoration> workshopDecorations = BuildWorkshopDecorations(root);
        List<BuiltDecoration> hallDecorations = BuildHallDecorations(root);

        // El almacén y la habitación: se compran enteras. El almacén produce
        // por su cuenta; la habitación mejora lo que ya hay en el hall.
        BuildStorageRoom(root);
        BuildBedroom(root,
            dropOff.GetComponent<ServiceSpeed>(),
            pickup.GetComponent<ServiceSpeed>(),
            new List<CartWorker> { inCart, outCart });

        WireWorkStation(station, dropOff, pickup, desks,
            new List<CartWorker> { inCart, outCart }, root.transform, workshopDecorations);

        // Las otras dos zonas mejorables: la sala amarilla mejora los carritos
        // y el hall mejora las recepciones y sus decoraciones.
        BuildCartZone(root, inCart, outCart);
        BuildReceptionZone(root,
            dropOff.GetComponent<ServiceSpeed>(),
            pickup.GetComponent<ServiceSpeed>(),
            waitingArea,
            seatsFocus,
            hallDecorations);

        // Cada taller lleva su propio gestor de clientes, con su cola, sus
        // mostradores y su sala de espera.
        BuildCustomerManager(root, template, station, dropOff, pickup, waitingArea, hall);

        return root;
    }

    // ── Recepción: mostrador de entrega (pasos 2 y 3) ─────────────────

    private static ReceptionDesk BuildDropOffDesk(GameObject root, out ItemStack backStack)
    {
        GameObject deskGO = NewChild(root.transform, "MostradorEntrega", new Vector2(RightLaneX, CounterY));
        AddSquare(deskGO, CounterSize, new Color(0.18f, 0.18f, 0.22f), OrderFurniture);
        MarkAsObstacle(deskGO);

        ReceptionDesk desk = deskGO.AddComponent<ReceptionDesk>();

        Transform customerPoint = NewChild(deskGO.transform, "CustomerPoint", new Vector2(RightLaneX, QueueHeadY)).transform;
        Transform objectPoint = NewChild(deskGO.transform, "ObjectPoint", new Vector2(RightLaneX, CounterY + 0.05f)).transform;
        Transform playerPoint = NewChild(deskGO.transform, "PlayerPoint", new Vector2(RightLaneX, ReceptionistIdleY)).transform;
        Transform finalBoxPoint = NewChild(deskGO.transform, "FinalBoxPoint", new Vector2(RightLaneX + 0.7f, ReceptionistIdleY)).transform;

        // Escritorio trasero: donde se acumulan los objetos esperando al carrito
        GameObject backDeskGO = NewChild(root.transform, "EscritorioTrasero", new Vector2(RightLaneX, BackDeskY));
        AddSquare(backDeskGO, BackDeskSize, new Color(0.82f, 0.70f, 0.32f), OrderFurniture);
        MarkAsObstacle(backDeskGO);

        // El carrito atraca al lado del escritorio, nunca encima.
        backStack = BuildStack(backDeskGO.transform, "MontonEntrada",
            new Vector2(RightLaneX, BackDeskY + 0.05f), BackDeskDock);

        SerializedObject so = new(desk);
        so.FindProperty("customerPoint").objectReferenceValue = customerPoint;
        so.FindProperty("objectPoint").objectReferenceValue = objectPoint;
        so.FindProperty("playerPoint").objectReferenceValue = playerPoint;
        so.FindProperty("finalBoxPoint").objectReferenceValue = finalBoxPoint;
        so.FindProperty("backStack").objectReferenceValue = backStack;
        so.FindProperty("serviceSpeed").objectReferenceValue = deskGO.AddComponent<ServiceSpeed>();
        so.FindProperty("progressUI").objectReferenceValue =
            CloneProgressUI(deskGO.transform, "ProgresoEntrega", DropOffProgressOffset);
        so.ApplyModifiedPropertiesWithoutUndo();

        // El recepcionista vive dentro de la ReceptionDesk: WorkStation lo
        // busca con GetComponentInChildren. Se construye a mano en vez de
        // reusar el prefab Worker porque no debe llevar el componente Worker.
        GameObject receptionistGO = BuildActor(
            deskGO.transform, "Recepcionista",
            new Vector2(RightLaneX, ReceptionistIdleY),
            new Color(0.95f, 0.55f, 0.15f),
            out Transform headAnchor);

        Receptionist receptionist = receptionistGO.AddComponent<Receptionist>();

        SerializedObject receptionistSo = new(receptionist);
        receptionistSo.FindProperty("headAnchor").objectReferenceValue = headAnchor;
        receptionistSo.ApplyModifiedPropertiesWithoutUndo();

        return desk;
    }

    /// <summary>
    /// Cuerpo mínimo de un trabajador que se mueve por la escena: un cuadrado
    /// y un punto sobre la cabeza donde se engancha el objeto que transporta.
    /// </summary>
    private static GameObject BuildActor(
        Transform parent, string name, Vector2 pos, Color color, out Transform headAnchor)
    {
        GameObject go = NewChild(parent, name, pos);

        GameObject body = NewChild(go.transform, "Cuerpo", pos);
        AddSquare(body, new Vector2(0.32f, 0.55f), color, OrderActor);

        headAnchor = NewChild(go.transform, "HeadAnchor", pos + new Vector2(0f, 0.40f)).transform;

        return go;
    }

    // ── Recepción: mostrador de recogida (pasos 12 y 13) ──────────────

    private static PickupDesk BuildPickupDesk(GameObject root, out ItemStack stack)
    {
        GameObject deskGO = NewChild(root.transform, "MostradorRecogida", new Vector2(LeftLaneX, CounterY));
        AddSquare(deskGO, CounterSize, new Color(0.18f, 0.18f, 0.22f), OrderFurniture);
        MarkAsObstacle(deskGO);

        PickupDesk desk = deskGO.AddComponent<PickupDesk>();

        Transform customerPoint = NewChild(deskGO.transform, "CustomerPoint", new Vector2(LeftLaneX, QueueHeadY)).transform;

        GameObject tableGO = NewChild(root.transform, "MesaRecogida", new Vector2(LeftLaneX, PickupStackY));
        AddSquare(tableGO, PickupTableSize, new Color(0.82f, 0.70f, 0.32f), OrderFurniture);
        MarkAsObstacle(tableGO);

        // El carrito atraca por encima de la mesa, sin pisarla. Cómo llega
        // hasta aquí lo resuelve la red de navegación.
        stack = BuildStack(tableGO.transform, "MontonSalida",
            new Vector2(LeftLaneX, PickupStackY + 0.05f), PickupDock);

        // Recepcionista de devolución: el objeto salta del montón a sus manos
        // y de ahí al cliente. Se coloca igual que el de entrega, entre el
        // mostrador y la mesa de detrás.
        Vector2 receptionistPos = new(LeftLaneX, ReceptionistIdleY);

        BuildActor(deskGO.transform, "RecepcionistaDevolucion", receptionistPos,
            new Color(0.95f, 0.55f, 0.15f), out Transform handoverPoint);

        Transform playerPoint = NewChild(deskGO.transform, "PlayerPoint", receptionistPos).transform;

        SerializedObject so = new(desk);
        so.FindProperty("customerPoint").objectReferenceValue = customerPoint;
        so.FindProperty("playerPoint").objectReferenceValue = playerPoint;
        so.FindProperty("handoverPoint").objectReferenceValue = handoverPoint;
        so.FindProperty("stack").objectReferenceValue = stack;
        so.FindProperty("serviceSpeed").objectReferenceValue = deskGO.AddComponent<ServiceSpeed>();
        so.FindProperty("progressUI").objectReferenceValue =
            CloneProgressUI(deskGO.transform, "ProgresoRecogida", PickupProgressOffset);
        so.ApplyModifiedPropertiesWithoutUndo();

        return desk;
    }

    // ── Taller: las tres mesas de limpieza (pasos 7, 8 y 9) ───────────

    private static List<WorkDeskUnlockable> BuildCleaningDesks(
        GameObject root, GameObject deskPrefab, out List<WorkTable> tables)
    {
        GameObject group = NewChild(root.transform, "MesasLimpieza", Vector2.zero);

        var unlockables = new List<WorkDeskUnlockable>();
        tables = new List<WorkTable>();

        // Solo se dice dónde está cada zona y dónde atraca el carrito; cómo se
        // llega hasta ahí lo resuelve la red de navegación.
        BuildOneDesk(group.transform, deskPrefab, "Mesa1_Derecha",
            Desk1Pos, DeskVerticalSize, Desk1Item, Desk1Player,
            Desk1Box, Desk1BoxDock, Desk1Out, Desk1OutDock,
            unlockedByDefault: true, unlockables, tables);

        BuildOneDesk(group.transform, deskPrefab, "Mesa2_Centro",
            Desk2Pos, DeskHorizontalSize, Desk2Item, Desk2Player,
            Desk2Box, Desk2Box + new Vector2(0f, -0.55f), Desk2Out, Desk2OutDock,
            unlockedByDefault: false, unlockables, tables);

        BuildOneDesk(group.transform, deskPrefab, "Mesa3_Izquierda",
            Desk3Pos, DeskVerticalSize, Desk3Item, Desk3Player,
            Desk3Box, Desk3Box + new Vector2(0f, 0.55f), Desk3Out, Desk3OutDock,
            unlockedByDefault: false, unlockables, tables);

        return unlockables;
    }

    private static void BuildOneDesk(
        Transform parent, GameObject deskPrefab, string name,
        Vector2 pos, Vector2 size, Vector2 itemPos, Vector2 playerPos,
        Vector2 inPos, Vector2 inDock,
        Vector2 outPos, Vector2 outDock,
        bool unlockedByDefault,
        List<WorkDeskUnlockable> unlockables, List<WorkTable> tables)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(deskPrefab, parent);
        go.name = name;
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = Vector3.one;

        if (go.TryGetComponent(out SpriteRenderer sr))
        {
            sr.color = new Color(0.72f, 0.48f, 0.28f);
            sr.sortingOrder = OrderFurniture;
            // El sprite del prefab mide 1x1: la escala es el tamaño en unidades.
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        MarkAsObstacle(go);

        Transform boxPoint = FindOrCreate(go.transform, "BoxPoint");
        Transform itemPoint = FindOrCreate(go.transform, "ObjectPoint");
        Transform playerPoint = FindOrCreate(go.transform, "PlayerPoint");

        boxPoint.position = new Vector3(inPos.x, inPos.y, 0f);
        itemPoint.position = new Vector3(itemPos.x, itemPos.y, 0f);
        playerPoint.position = new Vector3(playerPos.x, playerPos.y, 0f);

        WorkTable table = go.GetComponent<WorkTable>();
        Worker worker = go.GetComponentInChildren<Worker>(includeInactive: true);

        if (worker != null)
        {
            worker.transform.position = new Vector3(playerPos.x, playerPos.y, 0f);
            worker.transform.localScale = NormalizedChildScale(go.transform, 0.5f);
            TintActor(worker.gameObject, new Color(0.95f, 0.62f, 0.20f));
        }

        // Zona de espera y parte contraria: los objetos van solos entre ellas.
        ItemStack inStack = BuildStack(go.transform, "ZonaEspera", inPos, inDock);
        ItemStack outStack = BuildStack(go.transform, "ZonaTerminados", outPos, outDock);

        SerializedObject so = new(table);
        so.FindProperty("playerSlot").objectReferenceValue = playerPoint;
        so.FindProperty("itemSlot").objectReferenceValue = itemPoint;
        so.FindProperty("boxPoint").objectReferenceValue = boxPoint;
        so.FindProperty("inStack").objectReferenceValue = inStack;
        so.FindProperty("outStack").objectReferenceValue = outStack;
        so.FindProperty("outStackPoof").objectReferenceValue = outStack.GetComponent<Poof>();
        so.FindProperty("worker").objectReferenceValue = worker;
        so.ApplyModifiedPropertiesWithoutUndo();

        WorkDeskUnlockable unlockable = go.GetComponent<WorkDeskUnlockable>();
        SerializedObject unlockSo = new(unlockable);
        unlockSo.FindProperty("unlockedByDefault").boolValue = unlockedByDefault;
        unlockSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
        unlockSo.ApplyModifiedPropertiesWithoutUndo();

        unlockables.Add(unlockable);
        tables.Add(table);
    }

    /// <summary>
    /// Redirección a la parte contraria de la última mesa desbloqueada: es lo
    /// que mira el carrito de salida, en vez de un punto fijo.
    /// </summary>
    private static WorkshopOutput BuildWorkshopOutput(GameObject root, WorkStation station)
    {
        GameObject go = NewChild(root.transform, "SalidaTaller", Desk3Out);

        WorkshopOutput output = go.AddComponent<WorkshopOutput>();
        SerializedObject so = new(output);
        so.FindProperty("workStation").objectReferenceValue = station;
        so.ApplyModifiedPropertiesWithoutUndo();

        return output;
    }

    // ── Red de navegación ─────────────────────────────────────────────

    /// <summary>
    /// Los nodos por los que circulan los carritos. Las conexiones no se
    /// guardan: se calculan en cada consulta según qué nodos se ven sin mueble
    /// por medio, de modo que mover una mesa o un carrito no rompe nada.
    /// </summary>
    private static void BuildNavGraph(GameObject root)
    {
        GameObject go = NewChild(root.transform, "NavGraph", Vector2.zero);

        var nodes = new List<Transform>();
        for (int i = 0; i < NavNodes.Length; i++)
            nodes.Add(NewChild(go.transform, $"Nodo{i + 1:00}", NavNodes[i]).transform);

        WorkshopNavGraph graph = go.AddComponent<WorkshopNavGraph>();
        SerializedObject so = new(graph);
        so.FindProperty("agentRadius").floatValue = AgentRadius;
        so.FindProperty("autoCollectObstacles").boolValue = true;
        SetObjectList(so.FindProperty("nodes"), nodes);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// Marca un mueble para que los carritos lo rodeen y para que nadie lo
    /// atraviese andando.
    ///
    /// Son dos cosas distintas a propósito: los carritos usan la red de
    /// navegación (NavObstacle) y quien anda a pie usa colliders de verdad.
    /// </summary>
    private static void MarkAsObstacle(GameObject go)
    {
        MakeSolid(go);

        if (go.GetComponent<NavObstacle>() == null)
            go.AddComponent<NavObstacle>();
    }

    /// <summary>
    /// Las salas no forman un rectángulo: entre el taller y la recepción hay
    /// huecos sin suelo por la izquierda. Se bloquean como obstáculos para que
    /// ninguna ruta corte por encima del vacío.
    /// </summary>
    private static void BuildVoidBlockers(GameObject root)
    {
        GameObject group = NewChild(root.transform, "ZonasSinSuelo", Vector2.zero);

        // A la izquierda de la recepción, por encima del hall. Se estira un poco
        // por arriba del borde real (1.75 -> 1.90) porque el carrito es más alto
        // que ancho y el radio de navegación se queda corto para esa esquina:
        // sin ese margen, una recta hacia la mesa 3 pasaba rozando el vacío.
        AddVoidBlocker(group.transform, "VacioIzquierdaRecepcion",
            new Vector2(-1.82f, 0.105f), new Vector2(3.22f, 3.59f));

        // A la izquierda del taller.
        AddVoidBlocker(group.transform, "VacioIzquierdaTaller",
            new Vector2(-2.64f, 4.26f), new Vector2(1.59f, 5.02f));
    }

    private static void AddVoidBlocker(Transform parent, string name, Vector2 center, Vector2 size)
    {
        GameObject go = NewChild(parent, name, center);

        NavObstacle obstacle = go.AddComponent<NavObstacle>();
        SerializedObject so = new(obstacle);
        so.FindProperty("manualSize").vector2Value = size;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── Transportistas con carrito (pasos 4-6 y 10-12) ────────────────

    private static CartWorker BuildCart(
        GameObject root, string name, Vector2 idlePos,
        MonoBehaviour source, MonoBehaviour target)
    {
        GameObject go = NewChild(root.transform, name, idlePos);

        // El trabajador va detrás y el carrito delante. Antes iban los dos en
        // el mismo punto y el trabajador se dibujaba encima del carro, como si
        // lo atravesara.
        GameObject body = NewChild(go.transform, "Cuerpo", idlePos + CartWorkerBodyOffset);
        AddSquare(body, new Vector2(0.30f, 0.46f), new Color(0.30f, 0.55f, 0.85f), OrderActor);

        Transform headAnchor = NewChild(go.transform, "HeadAnchor",
            idlePos + CartWorkerBodyOffset + new Vector2(0f, 0.32f)).transform;

        Transform idle = NewChild(root.transform, name + "_Idle", idlePos).transform;

        GameObject cartGO = NewChild(go.transform, "Carrito", idlePos + CartBodyOffset);

        // La caja del carro: siempre visible, va vacía o llena.
        GameObject cartBody = NewChild(cartGO.transform, "Caja", idlePos + CartBodyOffset);
        AddSquare(cartBody, CartBoxSize, new Color(0.55f, 0.55f, 0.62f), OrderStack);

        // El saco que lleva encima, siempre a la vista.
        Sack cartSack = BuildSack(cartGO.transform, idlePos + CartBodyOffset + new Vector2(0f, 0.06f));

        Cart cart = cartGO.AddComponent<Cart>();
        SerializedObject cartSo = new(cart);
        cartSo.FindProperty("sack").objectReferenceValue = cartSack;
        cartSo.FindProperty("capacity").intValue = CartCapacity;
        cartSo.ApplyModifiedPropertiesWithoutUndo();

        // El círculo de progreso va colgado del carrito y lo sigue.
        RepairProgressUI progress = CloneProgressUI(go.transform, "ProgresoCarrito", new Vector3(0f, 0.75f, 0f));

        CartWorker worker = go.AddComponent<CartWorker>();
        SerializedObject so = new(worker);
        so.FindProperty("idlePosition").objectReferenceValue = idle;
        so.FindProperty("headAnchor").objectReferenceValue = headAnchor;
        so.FindProperty("sourceContainer").objectReferenceValue = source;
        so.FindProperty("targetContainer").objectReferenceValue = target;
        so.FindProperty("cart").objectReferenceValue = cart;
        so.FindProperty("progressUI").objectReferenceValue = progress;
        so.ApplyModifiedPropertiesWithoutUndo();

        return worker;
    }

    // ── Hall: colas, camino y salida ──────────────────────────────────

    /// <summary>
    /// Todo lo que el CustomerManager necesita del hall. Con nombres y no un
    /// array indexado: eran ya siete y cualquier reordenación los cruzaba en
    /// silencio.
    /// </summary>
    private class HallAnchors
    {
        public Transform DeskSlot;
        public Transform PickupSlot;
        public Transform ItemDropTarget;
        public Transform ExitPoint;
        public List<Transform> EntryPath = new();
        public List<Transform> ReturnPath = new();
    }

    private static HallAnchors BuildHall(GameObject root)
    {
        GameObject group = NewChild(root.transform, "Hall", Vector2.zero);

        var anchors = new HallAnchors
        {
            DeskSlot = NewChild(group.transform, "ColaEntrega_Slot0", new Vector2(RightLaneX, QueueHeadY)).transform,
            PickupSlot = NewChild(group.transform, "ColaRecogida_Slot0", new Vector2(LeftLaneX, QueueHeadY)).transform,
            ItemDropTarget = NewChild(group.transform, "ItemDropTarget", new Vector2(RightLaneX, CounterY + 0.05f)).transform,
            ExitPoint = NewChild(group.transform, "PuntoSalida", OffMapXY).transform,
        };

        // Entrada: del borde del mapa al pie de la cola de entrega (derecha).
        GameObject entry = NewChild(group.transform, "CaminoEntrada", Vector2.zero);
        anchors.EntryPath.Add(NewChild(entry.transform, "WP0_Spawn", OffMapXY).transform);
        anchors.EntryPath.Add(NewChild(entry.transform, "WP1_Entrada", EntranceXY).transform);
        anchors.EntryPath.Add(NewChild(entry.transform, "WP2_PieColaEntrega", new Vector2(RightLaneX, EntranceXY.y)).transform);

        // Vuelta: al pie de la cola de recogida (izquierda), sin cruzar el hall
        // hasta la derecha para luego volver.
        GameObject back = NewChild(group.transform, "CaminoVuelta", Vector2.zero);
        anchors.ReturnPath.Add(NewChild(back.transform, "WP0_Spawn", OffMapXY).transform);
        anchors.ReturnPath.Add(NewChild(back.transform, "WP1_Entrada", EntranceXY).transform);
        anchors.ReturnPath.Add(NewChild(back.transform, "WP2_PieColaRecogida", new Vector2(LeftLaneX, EntranceXY.y)).transform);

        // Los carriles, como referencia visual de por dónde va cada cola.
        BuildLane(group.transform, "CarrilEntrega", RightLaneX, new Color(0.16f, 0.16f, 0.20f));
        BuildLane(group.transform, "CarrilRecogida", LeftLaneX, new Color(0.16f, 0.16f, 0.20f));

        return anchors;
    }

    private static void BuildLane(Transform parent, string name, float x, Color color)
    {
        float top = QueueHeadY + 0.15f;
        float bottom = QueueHeadY + QueueSpacingY * (QueueLength - 1) - 0.15f;

        GameObject go = NewChild(parent, name, new Vector2(x, (top + bottom) * 0.5f));
        AddSquare(go, new Vector2(0.30f, top - bottom), color, OrderFurniture);
    }

    /// <summary>
    /// La sala de espera: los asientos del hueco de la izquierda del hall,
    /// donde los clientes aguardan a que su objeto esté listo.
    ///
    /// El orden en que se crean es el orden en que van apareciendo al subir la
    /// mejora, así que se llena por filas empezando por la de arriba (la que
    /// está pegada a recepción) y de derecha a izquierda dentro de cada fila:
    /// el primer asiento que se compra queda a la vista, junto al mostrador, y
    /// la sala va creciendo hacia el fondo en vez de empezar por la esquina.
    /// </summary>
    private static WaitingArea BuildWaitingArea(GameObject root, out Transform focus)
    {
        // Los asientos son una decoración más: se compran, suman monedas y van
        // apareciendo a trozos. Lo único suyo es que además reparten sitios.
        BuiltDecoration built = BuildDecoration(root, new DecorationSpec
        {
            ObjectName = "Asiento",
            DisplayName = "Asientos",
            Description = "Los clientes esperan sentados y, de paso, cada parte del proceso paga una moneda más por nivel.",
            AssetPath = "Assets/ScriptableObjects/Upgrades/SeatsUpgrade.asset",
            UnlockCost = SeatsUnlockCost,
            Pieces = SeatPositions(),
            PieceSize = new Vector2(0.34f, 0.34f),
            PieceColor = new Color(0.35f, 0.42f, 0.55f),
            Thresholds = SeatLevelThresholds,
        });

        DecorationReveal reveal = built.Reveal;
        focus = built.Focus;

        reveal.gameObject.name = "SalaEspera";

        // Un sitio por asiento, un poco por delante para que el cliente no lo
        // tape del todo. Van colgados del propio asiento, así que se esconden
        // con él.
        var seats = new List<Transform>();
        Vector2[] positions = SeatPositions();

        for (int i = 0; i < positions.Length; i++)
        {
            Transform piece = reveal.transform.GetChild(i);
            seats.Add(NewChild(piece, "Sitio",
                new Vector2(positions[i].x + 0.30f, positions[i].y)).transform);
        }

        WaitingArea area = reveal.gameObject.AddComponent<WaitingArea>();
        SerializedObject so = new(area);
        SetObjectList(so.FindProperty("seats"), seats);
        so.FindProperty("reveal").objectReferenceValue = reveal;
        so.ApplyModifiedPropertiesWithoutUndo();

        return area;
    }

    /// <summary>
    /// Las posiciones de los 12 asientos, en el orden en que van apareciendo:
    /// por filas empezando por la de arriba (la pegada a recepción) y de
    /// derecha a izquierda dentro de cada fila. Así el primero que compras
    /// queda a la vista junto al mostrador y la sala crece hacia el fondo, en
    /// vez de empezar por la esquina.
    /// </summary>
    private static Vector2[] SeatPositions()
    {
        var positions = new List<Vector2>();

        foreach (float y in SeatRows)
            for (int c = SeatColumns.Length - 1; c >= 0; c--)
                positions.Add(new Vector2(SeatColumns[c], y));

        return positions.ToArray();
    }

    // ── Cableado ──────────────────────────────────────────────────────

    private static void WireWorkStation(
        WorkStation station, ReceptionDesk dropOff, PickupDesk pickup,
        List<WorkDeskUnlockable> desks, List<CartWorker> carts, Transform root,
        List<BuiltDecoration> decorations)
    {
        // La cámara se centra en la sala naranja, que es la que se toca.
        Transform focus = NewChild(root, "CameraFocusPoint", CleaningRoomCenter).transform;

        SerializedObject so = new(station);
        so.FindProperty("receptionDesk").objectReferenceValue = dropOff;
        so.FindProperty("pickupDesk").objectReferenceValue = pickup;
        so.FindProperty("cameraFocusPoint").objectReferenceValue = focus;

        SetObjectList(so.FindProperty("workDesks"), desks);
        SetObjectList(so.FindProperty("cartWorkers"), carts);

        // upgradeElements de la WorkStation se deja vacío a propósito: ahí van
        // los elementos decorativos que dan bonus de zona. Lo que sale en el
        // panel lo lleva ahora la UpgradeZone.
        so.FindProperty("upgradeElements").arraySize = 0;
        so.ApplyModifiedPropertiesWithoutUndo();

        BuildWorkshopZone(station.gameObject, focus, desks, decorations);
    }

    /// <summary>
    /// Zona de la sala naranja: se toca y salen las mesas, para desbloquearlas
    /// o mejorarlas, y las decoraciones del taller. El collider ya está en la
    /// raíz del taller.
    /// </summary>
    private static void BuildWorkshopZone(
        GameObject root, Transform focus, List<WorkDeskUnlockable> desks,
        List<BuiltDecoration> decorations)
    {
        var elements = new List<ZoneElement>();
        Sprite deskIcon = DeskIcon();

        foreach (WorkDeskUnlockable desk in desks)
        {
            if (desk == null) continue;

            elements.Add(new ZoneElement
            {
                Unlockable = desk,
                Upgradeable = desk.GetComponent<WorkDeskUpgradeable>(),
                Icon = deskIcon,
            });
        }

        elements.AddRange(DecorationElements(decorations));

        WireZone(root, focus, "Taller de limpieza", elements);
    }

    /// <summary>
    /// Las decoraciones como entradas del panel. Empiezan bloqueadas, así que
    /// el panel las enseña primero como compra y luego como mejora.
    /// </summary>
    private static List<ZoneElement> DecorationElements(List<BuiltDecoration> decorations)
    {
        var elements = new List<ZoneElement>();
        if (decorations == null) return elements;

        Sprite icon = DecorationIcon();

        foreach (BuiltDecoration decoration in decorations)
        {
            if (decoration?.Reveal == null) continue;

            elements.Add(new ZoneElement
            {
                Unlockable = decoration.Reveal.GetComponent<Unlockable>(),
                Upgradeable = decoration.Reveal.GetComponent<CoinBonusUpgradeable>(),
                Icon = icon,
                Focus = decoration.Focus,
            });
        }

        return elements;
    }

    /// <summary>
    /// Las dos decoraciones de la sala naranja: estanterías arriba y plantas
    /// en la columna de la derecha. Ninguna se cruza con los atraques de los
    /// carritos ni con las mesas.
    /// </summary>
    private static List<BuiltDecoration> BuildWorkshopDecorations(GameObject root)
    {
        return new List<BuiltDecoration>
        {
            BuildDecoration(root, new DecorationSpec
            {
                ObjectName = "Estanteria",
                DisplayName = "Estanterías",
                Description = "Material bien colocado. Cada parte del proceso paga una moneda más por nivel.",
                AssetPath = "Assets/ScriptableObjects/Upgrades/ShelvesUpgrade.asset",
                UnlockCost = 750,
                Pieces = ShelfPieces,
                PieceSize = new Vector2(0.40f, 0.52f),
                PieceColor = new Color(0.52f, 0.36f, 0.22f),
                Thresholds = DecorationThresholds,
            }),

            BuildDecoration(root, new DecorationSpec
            {
                ObjectName = "PlantaTaller",
                DisplayName = "Plantas del taller",
                Description = "Un taller más agradable. Cada parte del proceso paga una moneda más por nivel.",
                AssetPath = "Assets/ScriptableObjects/Upgrades/WorkshopPlantsUpgrade.asset",
                UnlockCost = 1200,
                Pieces = WorkshopPlantPieces,
                PieceSize = new Vector2(0.34f, 0.40f),
                PieceColor = new Color(0.30f, 0.60f, 0.32f),
                Thresholds = DecorationThresholds,
            }),
        };
    }

    /// <summary>
    /// Las dos decoraciones del hall que no son los asientos: plantas a la
    /// derecha del carril de entrega y lámparas en el hueco entre los asientos
    /// y el carril de recogida. Nada se pone en el camino de los clientes.
    /// </summary>
    private static List<BuiltDecoration> BuildHallDecorations(GameObject root)
    {
        return new List<BuiltDecoration>
        {
            BuildDecoration(root, new DecorationSpec
            {
                ObjectName = "PlantaHall",
                DisplayName = "Plantas del hall",
                Description = "Verde a la entrada. Cada parte del proceso paga una moneda más por nivel.",
                AssetPath = "Assets/ScriptableObjects/Upgrades/HallPlantsUpgrade.asset",
                UnlockCost = 900,
                Pieces = HallPlantPieces,
                PieceSize = new Vector2(0.34f, 0.40f),
                PieceColor = new Color(0.34f, 0.66f, 0.38f),
                Thresholds = DecorationThresholds,
            }),

            BuildDecoration(root, new DecorationSpec
            {
                ObjectName = "Lampara",
                DisplayName = "Lámparas",
                Description = "El hall mejor iluminado. Cada parte del proceso paga una moneda más por nivel.",
                AssetPath = "Assets/ScriptableObjects/Upgrades/LampsUpgrade.asset",
                UnlockCost = 1500,
                Pieces = LampPieces,
                PieceSize = new Vector2(0.26f, 0.55f),
                PieceColor = new Color(0.90f, 0.80f, 0.42f),
                Thresholds = DecorationThresholds,
            }),
        };
    }

    /// <summary>Icono provisional de los botones de decoración.</summary>
    private static Sprite DecorationIcon() => SeatsIcon();

    /// <summary>Icono provisional de los botones de mesa, del set de graybox.</summary>
    private static Sprite DeskIcon() =>
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/GrayBox/Graybox2D/16x16.png");

    private static Sprite CartIcon() =>
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/GrayBox/Graybox2D/32x32.png");

    private static Sprite ReceptionIcon() =>
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/GrayBox/Graybox2D/16x32.png");

    private static Sprite SeatsIcon() =>
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/GrayBox/Graybox2D/32x64.png");

    // ── Zonas mejorables ──────────────────────────────────────────────

    /// <summary>
    /// Zona de los carritos: la sala amarilla, por encima de los mostradores.
    /// Se toca ahí y salen las dos mejoras de velocidad de carrito.
    /// </summary>
    private static void BuildCartZone(GameObject root, params CartWorker[] carts)
    {
        GameObject go = NewChild(root.transform, "ZonaCarritos", Vector2.zero);

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.offset = CartZoneCenter;
        box.size = CartZoneSize;

        Transform focus = NewChild(go.transform, "CameraFocusPoint", CartZoneFocus).transform;

        var elements = new List<ZoneElement>();

        foreach (CartWorker cart in carts)
        {
            if (cart == null) continue;

            CartUpgradeable upgradeable = cart.gameObject.AddComponent<CartUpgradeable>();

            SerializedObject cartSo = new(upgradeable);
            cartSo.FindProperty("upgradeData").objectReferenceValue = CartData();
            cartSo.FindProperty("cartWorker").objectReferenceValue = cart;
            cartSo.ApplyModifiedPropertiesWithoutUndo();

            elements.Add(new ZoneElement { Upgradeable = upgradeable, Icon = CartIcon() });
        }

        WireZone(go, focus, "Carritos", elements);
    }

    /// <summary>
    /// Zona de las recepciones: todo el hall azul. Se toca en cualquier punto y
    /// la cámara se centra en él.
    /// </summary>
    private static void BuildReceptionZone(
        GameObject root, ServiceSpeed dropOffSpeed, ServiceSpeed pickupSpeed,
        WaitingArea seats, Transform seatsFocus,
        List<BuiltDecoration> decorations)
    {
        GameObject go = NewChild(root.transform, "ZonaRecepciones", Vector2.zero);

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.offset = HallCenter;
        box.size = HallSize;

        Transform focus = NewChild(go.transform, "CameraFocusPoint", HallCenter).transform;

        var elements = new List<ZoneElement>();

        foreach (ServiceSpeed desk in new[] { dropOffSpeed, pickupSpeed })
        {
            if (desk == null) continue;

            ReceptionUpgradeable upgradeable = desk.gameObject.AddComponent<ReceptionUpgradeable>();

            SerializedObject deskSo = new(upgradeable);
            deskSo.FindProperty("upgradeData").objectReferenceValue = ReceptionData();
            SetObjectList(deskSo.FindProperty("serviceSpeeds"), new List<ServiceSpeed> { desk });
            deskSo.ApplyModifiedPropertiesWithoutUndo();

            elements.Add(new ZoneElement { Upgradeable = upgradeable, Icon = ReceptionIcon() });
        }

        // Los asientos: empiezan bloqueados, así que el panel los muestra
        // primero como compra y luego como mejora.
        if (seats != null)
        {
            elements.Add(new ZoneElement
            {
                Unlockable = seats.GetComponent<Unlockable>(),
                Upgradeable = seats.GetComponent<CoinBonusUpgradeable>(),
                Icon = SeatsIcon(),
                Focus = seatsFocus,
            });
        }

        elements.AddRange(DecorationElements(decorations));

        WireZone(go, focus, "Recepción", elements);
    }

    // ── Decoraciones ──────────────────────────────────────────────────

    /// <summary>Todo lo que define una decoración del taller.</summary>
    private class DecorationSpec
    {
        public string ObjectName;      // nombre del grupo en la jerarquía
        public string DisplayName;     // nombre en el panel de mejoras
        public string Description;
        public string AssetPath;
        public double UnlockCost;
        public Vector2[] Pieces;
        public Vector2 PieceSize;
        public Color PieceColor;

        /// <summary>Nivel al que sale cada pieza. Uno por pieza.</summary>
        public int[] Thresholds;
    }

    /// <summary>
    /// Monta una decoración: sus piezas, el desbloqueo, la mejora que suma
    /// monedas y el revelado por trozos.
    ///
    /// Está hecho genérico porque hay cinco y crecerán: si cada una trajera su
    /// propio código copiado, arreglar algo en una dejaría las otras cuatro con
    /// el fallo. Los asientos son una de estas, solo que además reparten sitios.
    /// </summary>
    /// <summary>Una decoración montada, con el punto al que mira la cámara.</summary>
    private class BuiltDecoration
    {
        public DecorationReveal Reveal;
        public Transform Focus;
    }

    private static BuiltDecoration BuildDecoration(GameObject root, DecorationSpec spec)
    {
        GameObject group = NewChild(root.transform, spec.ObjectName, Vector2.zero);

        var pieces = new List<GameObject>();
        var poofs = new List<Poof>();

        for (int i = 0; i < spec.Pieces.Length; i++)
        {
            GameObject piece = NewChild(group.transform, $"{spec.ObjectName}{i + 1:00}", spec.Pieces[i]);
            SpriteRenderer body = AddSquare(piece, spec.PieceSize, spec.PieceColor, OrderFurniture);

            // Las decoraciones también frenan a quien anda. Al estar escondidas
            // su collider se apaga con ellas, que es justo lo que se quiere:
            // lo que no se ve no estorba.
            MakeSolid(piece);

            pieces.Add(piece);
            poofs.Add(AddPoof(piece, body));
        }

        // Hasta comprarla no se ve nada. El revelado lo lleva entero
        // DecorationReveal, así que aquí no se revela nada: si lo hicieran los
        // dos, se pisarían.
        Unlockable unlockable = group.AddComponent<Unlockable>();
        SerializedObject unlockSo = new(unlockable);
        unlockSo.FindProperty("unlockCost").doubleValue = spec.UnlockCost;
        unlockSo.FindProperty("unlockedByDefault").boolValue = false;
        unlockSo.FindProperty("revealOnUnlock").arraySize = 0;
        unlockSo.ApplyModifiedPropertiesWithoutUndo();

        CoinBonusUpgradeable bonus = group.AddComponent<CoinBonusUpgradeable>();
        SerializedObject bonusSo = new(bonus);
        bonusSo.FindProperty("upgradeData").objectReferenceValue = DecorationData(spec, pieces.Count);
        bonusSo.FindProperty("unlockable").objectReferenceValue = unlockable;
        bonusSo.ApplyModifiedPropertiesWithoutUndo();

        DecorationReveal reveal = group.AddComponent<DecorationReveal>();
        SerializedObject revealSo = new(reveal);
        SetObjectList(revealSo.FindProperty("pieces"), pieces);
        SetObjectList(revealSo.FindProperty("piecePoofs"), poofs);
        revealSo.FindProperty("unlockable").objectReferenceValue = unlockable;
        revealSo.FindProperty("upgrade").objectReferenceValue = bonus;
        revealSo.ApplyModifiedPropertiesWithoutUndo();

        // El grupo está en el origen y sus piezas repartidas por la sala, así
        // que sin un punto propio la cámara se iría a (0,0) al tocar la mejora.
        // Se pone en el centro de lo que ocupan las piezas.
        Vector2 sum = Vector2.zero;
        foreach (Vector2 piece in spec.Pieces) sum += piece;

        Transform focus = NewChild(group.transform, "Foco", sum / spec.Pieces.Length).transform;

        return new BuiltDecoration { Reveal = reveal, Focus = focus };
    }

    /// <summary>
    /// El puf con el que algo aparece y desaparece. Las motitas salen del
    /// mismo sprite y color que lo que se esconde, así que no hace falta
    /// ningún asset de partículas.
    /// </summary>
    private static Poof AddPoof(GameObject go, SpriteRenderer body)
    {
        Poof poof = go.AddComponent<Poof>();

        SerializedObject so = new(poof);
        so.FindProperty("particleSprite").objectReferenceValue = body != null ? body.sprite : UnitSquare();
        so.FindProperty("particleColor").colorValue = body != null
            ? Color.Lerp(body.color, Color.white, 0.45f)
            : Color.white;
        so.ApplyModifiedPropertiesWithoutUndo();

        return poof;
    }

    // ── Salas de trabajadores en bucle ────────────────────────────────

    /// <summary>Todo lo que define una de las dos salas de la izquierda.</summary>
    private class SideRoomSpec
    {
        public string ObjectName;
        public string ZoneName;
        public Vector2 Center;
        public Vector2 Size;
        public double UnlockCost;
        public Vector2[] WorkerSpots;
        public Color[] WorkerColors;
        public string AssetPath;
        public string WorkerName;
        public string WorkerDescription;

        /// <summary>
        /// Coste de comprar cada trabajador por separado. El primero entra con
        /// la sala, así que su entrada va a 0.
        /// </summary>
        public double[] WorkerUnlockCosts;

        public int BaseCoins;
        public int CoinsPerLevel;
        public float BaseCycleTime;
        public float TimeReductionPerLevel;
        public float MinCycleTime;
    }

    /// <summary>
    /// Monta una de las salas de la izquierda: el almacén o la habitación.
    ///
    /// La sala entera se compra primero —hasta entonces el panel solo ofrece
    /// desbloquearla— y dentro lleva uno o dos trabajadores que trabajan en
    /// bucle y cobran al terminar cada tanda. Cada trabajador es además una
    /// mejora de la sala.
    /// </summary>
    private static void BuildSideRoom(GameObject root, SideRoomSpec spec)
    {
        GameObject go = NewChild(root.transform, spec.ObjectName, Vector2.zero);

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.offset = spec.Center;
        box.size = spec.Size;

        Transform focus = NewChild(go.transform, "CameraFocusPoint", spec.Center).transform;

        // La compra de la sala. No revela nada por su cuenta: de quién se ve y
        // quién trabaja se encarga cada IdleWorker, que mira este desbloqueo.
        Unlockable roomLock = go.AddComponent<Unlockable>();
        SerializedObject roomSo = new(roomLock);
        roomSo.FindProperty("unlockCost").doubleValue = spec.UnlockCost;
        roomSo.FindProperty("unlockedByDefault").boolValue = false;
        roomSo.FindProperty("revealOnUnlock").arraySize = 0;
        roomSo.ApplyModifiedPropertiesWithoutUndo();

        IdleWorkerUpgradeData data = IdleWorkerData(spec);
        var elements = new List<ZoneElement>();

        for (int i = 0; i < spec.WorkerSpots.Length; i++)
        {
            double cost = spec.WorkerUnlockCosts != null && i < spec.WorkerUnlockCosts.Length
                ? spec.WorkerUnlockCosts[i]
                : 0;

            elements.Add(BuildIdleWorker(go, spec, data, i, roomLock, cost));
        }

        WireZone(go, focus, spec.ZoneName, elements, roomLock, RoomIcon());
    }

    /// <summary>
    /// Un trabajador de sala: su muñeco, su círculo de progreso y su mejora.
    /// El primero de la sala entra con ella; los siguientes se compran aparte.
    /// </summary>
    private static ZoneElement BuildIdleWorker(
        GameObject room, SideRoomSpec spec, IdleWorkerUpgradeData data,
        int index, Unlockable roomLock, double ownCost)
    {
        Vector2 spot = spec.WorkerSpots[index];

        // La raíz se queda siempre activa: es quien escucha el desbloqueo y
        // lleva el bucle. Lo que se enciende y se apaga es el muñeco.
        GameObject go = NewChild(room.transform, $"Trabajador{index + 1}", spot);

        Color color = spec.WorkerColors != null && index < spec.WorkerColors.Length
            ? spec.WorkerColors[index]
            : Color.white;

        GameObject bodyGO = NewChild(go.transform, "Cuerpo", spot);
        SpriteRenderer body = AddSquare(bodyGO, IdleWorkerSize, color, OrderActor);
        Poof poof = AddPoof(bodyGO, body);

        RepairProgressUI progress = CloneProgressUI(
            go.transform, $"Progreso{index + 1}", new Vector3(0f, 0.62f, 0f));

        var gates = new List<Unlockable> { roomLock };

        Unlockable ownLock = null;
        if (ownCost > 0)
        {
            ownLock = go.AddComponent<Unlockable>();
            SerializedObject ownSo = new(ownLock);
            ownSo.FindProperty("unlockCost").doubleValue = ownCost;
            ownSo.FindProperty("unlockedByDefault").boolValue = false;
            ownSo.FindProperty("revealOnUnlock").arraySize = 0;
            ownSo.ApplyModifiedPropertiesWithoutUndo();

            gates.Add(ownLock);
        }

        IdleWorker worker = go.AddComponent<IdleWorker>();
        SerializedObject workerSo = new(worker);
        workerSo.FindProperty("body").objectReferenceValue = bodyGO.transform;
        workerSo.FindProperty("poof").objectReferenceValue = poof;
        workerSo.FindProperty("progressUI").objectReferenceValue = progress;
        SetObjectList(workerSo.FindProperty("requires"), gates);
        workerSo.ApplyModifiedPropertiesWithoutUndo();

        IdleWorkerUpgradeable upgradeable = go.AddComponent<IdleWorkerUpgradeable>();
        SerializedObject upgradeSo = new(upgradeable);
        upgradeSo.FindProperty("upgradeData").objectReferenceValue = data;
        upgradeSo.FindProperty("worker").objectReferenceValue = worker;
        upgradeSo.ApplyModifiedPropertiesWithoutUndo();

        return new ZoneElement
        {
            Unlockable = ownLock,
            Upgradeable = upgradeable,
            Icon = IdleWorkerIcon(),
        };
    }

    private static IdleWorkerUpgradeData IdleWorkerData(SideRoomSpec spec) =>
        LoadOrCreate<IdleWorkerUpgradeData>(
            spec.AssetPath, spec.WorkerName, spec.WorkerDescription, baseCost: 600,
            configureNew: data =>
            {
                data.maxLevel = 50;
                data.baseCoins = spec.BaseCoins;
                data.coinsPerLevel = spec.CoinsPerLevel;
                data.baseCycleTime = spec.BaseCycleTime;
                data.timeReductionPerLevel = spec.TimeReductionPerLevel;
                data.minCycleTime = spec.MinCycleTime;
            });

    /// <summary>
    /// El almacén (sala morada): se compra entera y dentro hay dos
    /// trabajadores que cobran por tanda. El segundo se compra aparte.
    /// </summary>
    private static void BuildStorageRoom(GameObject root)
    {
        BuildSideRoom(root, new SideRoomSpec
        {
            ObjectName = "Almacen",
            ZoneName = "Almacén",
            Center = StorageCenter,
            Size = StorageSize,
            UnlockCost = StorageUnlockCost,
            WorkerSpots = StorageWorkerSpots,
            WorkerUnlockCosts = new[] { 0d, StorageSecondWorkerCost },
            WorkerColors = StorageWorkerColors,
            AssetPath = "Assets/ScriptableObjects/Upgrades/StorageWorkerUpgrade.asset",
            WorkerName = "Encargado de almacén",
            WorkerDescription = "Ordena el material. Cada tanda tarda lo suyo, pero paga bien.",
            BaseCoins = 1200,
            CoinsPerLevel = 320,
            BaseCycleTime = 90f,
            TimeReductionPerLevel = 1.2f,
            MinCycleTime = 30f,
        });
    }

    /// <summary>
    /// La habitación (sala rosa). No produce nada por sí misma: lo que se
    /// mejora aquí son los mostradores del hall y lo que pagan los objetos.
    /// El personaje es el jefe, que sale a dar una vuelta por el taller y
    /// vuelve; está por ambiente y no cobra nada.
    /// </summary>
    private static void BuildBedroom(
        GameObject root, ServiceSpeed dropOffSpeed, ServiceSpeed pickupSpeed,
        List<CartWorker> carts)
    {
        GameObject go = NewChild(root.transform, "Habitacion", Vector2.zero);

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.offset = BedroomCenter;
        box.size = BedroomSize;

        Transform focus = NewChild(go.transform, "CameraFocusPoint", BedroomCenter).transform;

        Unlockable roomLock = go.AddComponent<Unlockable>();
        SerializedObject roomSo = new(roomLock);
        roomSo.FindProperty("unlockCost").doubleValue = BedroomUnlockCost;
        roomSo.FindProperty("unlockedByDefault").boolValue = false;
        roomSo.FindProperty("revealOnUnlock").arraySize = 0;
        roomSo.ApplyModifiedPropertiesWithoutUndo();

        BuildBoss(go, roomLock);

        var elements = new List<ZoneElement>
        {
            // Solo los carritos, y las dos operaciones por igual.
            BedroomHandlingUpgrade(go, carts),

            // Los dos mostradores: dar el objeto y recogerlo en la otra cola.
            BedroomServiceUpgrade(go, new[] { dropOffSpeed, pickupSpeed }),

            BedroomCoinUpgrade(go, roomLock),
        };

        WireZone(go, focus, "Habitación", elements, roomLock, RoomIcon());
    }

    /// <summary>
    /// El jefe y su ronda. Los puntos de la ronda van colgados de él para que
    /// se vean juntos en la jerarquía y se puedan mover a ojo en la escena.
    /// </summary>
    private static void BuildBoss(GameObject room, Unlockable roomLock)
    {
        GameObject go = NewChild(room.transform, "Jefe", BedroomHome);

        GameObject bodyGO = NewChild(go.transform, "Cuerpo", BedroomHome);
        SpriteRenderer body = AddSquare(bodyGO, IdleWorkerSize, BossColor, OrderActor);
        Poof poof = AddPoof(bodyGO, body);

        // Las paradas cuelgan de la SALA, nunca del jefe. Si colgaran de él se
        // moverían con él: perseguiría un objetivo que huye a su misma
        // velocidad y se iría en línea recta para siempre.
        Transform home = NewChild(room.transform, "SitioJefe", BedroomHome).transform;

        GameObject routeGO = NewChild(room.transform, "RondaJefe", Vector2.zero);
        var stops = new List<Transform>();

        for (int i = 0; i < BedroomPatrol.Length; i++)
            stops.Add(NewChild(routeGO.transform, $"Parada{i + 1:00}", BedroomPatrol[i]).transform);

        RoomWanderer wanderer = go.AddComponent<RoomWanderer>();
        SerializedObject so = new(wanderer);

        // El área se mide respecto a la sala, no en coordenadas de mundo: el
        // taller está movido de sitio en la escena y un rectángulo en mundo
        // dejaba fuera media ronda.
        so.FindProperty("areaReference").objectReferenceValue = room.transform;
        so.FindProperty("areaCenter").vector2Value = BossAreaCenter;
        so.FindProperty("areaSize").vector2Value = BossAreaSize;

        int solid = LayerSetup.SolidLayerIndex;
        if (solid >= 0) so.FindProperty("solidLayers").intValue = 1 << solid;

        so.FindProperty("body").objectReferenceValue = bodyGO.transform;
        so.FindProperty("poof").objectReferenceValue = poof;
        so.FindProperty("home").objectReferenceValue = home;
        SetObjectList(so.FindProperty("stops"), stops);
        SetObjectList(so.FindProperty("requires"), new List<Unlockable> { roomLock });
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// Carga y descarga: afecta solo a los carritos, y a las dos operaciones
    /// por igual. No toca la velocidad a la que se mueven ni los mostradores.
    /// </summary>
    private static ZoneElement BedroomHandlingUpgrade(GameObject room, List<CartWorker> carts)
    {
        GameObject go = NewChild(room.transform, "MejoraCargaDescarga", BedroomCenter);

        CartHandlingUpgradeable upgradeable = go.AddComponent<CartHandlingUpgradeable>();
        SerializedObject so = new(upgradeable);
        so.FindProperty("upgradeData").objectReferenceValue =
            LoadOrCreate<CartHandlingUpgradeData>(
                "Assets/ScriptableObjects/Upgrades/BedroomHandlingUpgrade.asset",
                "Decoración",
                "Se mejora este componente para aumentar la velocidad de depósito de los objetos.",
                baseCost: 900);
        SetObjectList(so.FindProperty("carts"), carts);
        so.ApplyModifiedPropertiesWithoutUndo();

        return new ZoneElement { Upgradeable = upgradeable, Icon = CartIcon() };
    }

    /// <summary>
    /// Dar y recibir objetos: afecta a los DOS mostradores, así que mejora
    /// tanto lo que tarda un cliente en entregar como lo que tarda en recoger
    /// en la otra cola.
    ///
    /// Los mostradores no están aquí: se apunta en cada ServiceSpeed como
    /// fuente propia, así que convive con la mejora del propio mostrador del
    /// hall sin pisarla.
    /// </summary>
    private static ZoneElement BedroomServiceUpgrade(GameObject room, ServiceSpeed[] targets)
    {
        GameObject go = NewChild(room.transform, "MejoraAtencion", BedroomCenter);

        ReceptionUpgradeable upgradeable = go.AddComponent<ReceptionUpgradeable>();
        SerializedObject so = new(upgradeable);
        so.FindProperty("upgradeData").objectReferenceValue =
            LoadOrCreate<ReceptionUpgradeData>(
                "Assets/ScriptableObjects/Upgrades/BedroomServiceUpgrade.asset",
                "Escritorio de recogida",
                "Se mejora este componente para aumentar la velocidad de entrega de objetos a los clientes.",
                baseCost: 900);
        SetObjectList(so.FindProperty("serviceSpeeds"), new List<ServiceSpeed>(targets));
        so.ApplyModifiedPropertiesWithoutUndo();

        return new ZoneElement { Upgradeable = upgradeable, Icon = ReceptionIcon() };
    }

    /// <summary>La tercera: monedas extra en cada cobro, como las decoraciones.</summary>
    private static ZoneElement BedroomCoinUpgrade(GameObject room, Unlockable roomLock)
    {
        GameObject go = NewChild(room.transform, "MejoraMonedas", BedroomCenter);

        CoinBonusUpgradeable bonus = go.AddComponent<CoinBonusUpgradeable>();
        SerializedObject so = new(bonus);
        so.FindProperty("upgradeData").objectReferenceValue =
            LoadOrCreate<DecorationUpgradeData>(
                "Assets/ScriptableObjects/Upgrades/BedroomCoinsUpgrade.asset",
                "Decoración",
                "Se mejora este componente para poder conseguir dinero extra de los objetos.",
                baseCost: 1100);

        // Mientras la sala siga sin comprarse no aporta nada.
        so.FindProperty("unlockable").objectReferenceValue = roomLock;
        so.ApplyModifiedPropertiesWithoutUndo();

        return new ZoneElement { Upgradeable = bonus, Icon = DecorationIcon() };
    }

    /// <summary>Icono provisional del botón de comprar una sala.</summary>
    private static Sprite RoomIcon() =>
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/GrayBox/Graybox2D/32x64.png");

    /// <summary>Icono provisional de los trabajadores de sala.</summary>
    private static Sprite IdleWorkerIcon() =>
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/GrayBox/Graybox2D/16x16.png");

    /// <summary>Una entrada del panel de mejoras de una zona.</summary>
    private class ZoneElement
    {
        public Object Unlockable;
        public Object Upgradeable;
        public Sprite Icon;

        /// <summary>
        /// A dónde se acerca la cámara al tocar esta mejora. Si se deja null,
        /// se usa la posición del propio componente.
        /// </summary>
        public Transform Focus;
    }

    private static void WireZone(
        GameObject go, Transform focus, string zoneName, List<ZoneElement> elements,
        Object zoneUnlockable = null, Sprite lockedIcon = null)
    {
        UpgradeZone zone = go.AddComponent<UpgradeZone>();

        SerializedObject so = new(zone);
        so.FindProperty("zoneName").stringValue = zoneName;
        so.FindProperty("cameraFocusPoint").objectReferenceValue = focus;
        so.FindProperty("zoneUnlockableTarget").objectReferenceValue = zoneUnlockable;
        so.FindProperty("lockedIcon").objectReferenceValue = lockedIcon;

        SerializedProperty list = so.FindProperty("upgradeElements");
        list.arraySize = elements.Count;

        for (int i = 0; i < elements.Count; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("unlockableTarget").objectReferenceValue = elements[i].Unlockable;
            element.FindPropertyRelative("upgradeableTarget").objectReferenceValue = elements[i].Upgradeable;
            element.FindPropertyRelative("icon").objectReferenceValue = elements[i].Icon;
            element.FindPropertyRelative("focusPoint").objectReferenceValue = elements[i].Focus;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── ScriptableObjects de mejora ───────────────────────────────────
    // Se crean solo si no existen, para no pisar los valores que ajuste el
    // jugador al reconstruir el layout.

    /// <summary>
    /// Cada asset de mejora que crea el builder, con el tipo que le toca.
    /// </summary>
    private static readonly (string Path, System.Type Type)[] ExpectedUpgradeAssets =
    {
        ("Assets/ScriptableObjects/Upgrades/CartUpgrade.asset",         typeof(CartUpgradeData)),
        ("Assets/ScriptableObjects/Upgrades/ReceptionUpgrade.asset",    typeof(ReceptionUpgradeData)),
        ("Assets/ScriptableObjects/Upgrades/SeatsUpgrade.asset",        typeof(DecorationUpgradeData)),
        ("Assets/ScriptableObjects/Upgrades/ShelvesUpgrade.asset",      typeof(DecorationUpgradeData)),
        ("Assets/ScriptableObjects/Upgrades/WorkshopPlantsUpgrade.asset", typeof(DecorationUpgradeData)),
        ("Assets/ScriptableObjects/Upgrades/HallPlantsUpgrade.asset",   typeof(DecorationUpgradeData)),
        ("Assets/ScriptableObjects/Upgrades/LampsUpgrade.asset",        typeof(DecorationUpgradeData)),
        ("Assets/ScriptableObjects/Upgrades/StorageWorkerUpgrade.asset", typeof(IdleWorkerUpgradeData)),
        ("Assets/ScriptableObjects/Upgrades/BedroomHandlingUpgrade.asset", typeof(CartHandlingUpgradeData)),
        ("Assets/ScriptableObjects/Upgrades/BedroomServiceUpgrade.asset",  typeof(ReceptionUpgradeData)),
        ("Assets/ScriptableObjects/Upgrades/BedroomCoinsUpgrade.asset",    typeof(DecorationUpgradeData)),
    };

    /// <summary>
    /// Comprueba, antes de construir, que los assets de mejora se pueden
    /// cargar y son del tipo correcto.
    ///
    /// Si no lo son, LoadOrCreate devolvería null, los mejorables se quedarían
    /// sin datos y el panel fallaría al abrir esa sala — un fallo que aparece
    /// mucho más tarde y bastante lejos de su causa. Aquí se ve enseguida.
    /// </summary>
    private static bool UpgradeAssetsAreValid()
    {
        List<string> problems = new();

        foreach (var (path, type) in ExpectedUpgradeAssets)
        {
            bool fileExists = System.IO.File.Exists(path);
            UpgradeData asset = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);

            // Que no exista todavía es normal: LoadOrCreate lo creará.
            if (!fileExists && asset == null) continue;

            string file = System.IO.Path.GetFileName(path);

            if (asset == null)
            {
                problems.Add($"- {file}: existe pero no carga (¿script roto o sin reimportar?)");
            }
            else if (!type.IsInstanceOfType(asset))
            {
                problems.Add($"- {file}: es {asset.GetType().Name}, hace falta {type.Name}");
            }
        }

        if (problems.Count == 0) return true;

        string message =
            "No construyo nada: hay assets de mejora que no se pueden usar, y si " +
            "siguiera adelante las mejoras se quedarían sin datos y el panel " +
            "fallaría al abrirse.\n\n" + string.Join("\n", problems) +
            "\n\nSi acabas de cambiar los scripts, haz clic derecho sobre la carpeta " +
            "ScriptableObjects/Upgrades y pulsa Reimport, y vuelve a intentarlo.";

        Debug.LogError($"[Taller1Builder] {message}");
        EditorUtility.DisplayDialog("Taller 1", message, "Vale");
        return false;
    }

    /// <summary>
    /// Repaso final: ningún mejorable puede quedarse sin UpgradeData. Si pasa,
    /// se dice aquí señalando al objeto, en vez de dejar que salte al abrir el
    /// panel en pleno juego.
    /// </summary>
    private static void VerifyUpgradeablesWired(GameObject root)
    {
        int missing = 0;

        foreach (UpgradeableBase upgradeable in root.GetComponentsInChildren<UpgradeableBase>(true))
        {
            if (upgradeable.UpgradeData != null) continue;

            Debug.LogError(
                $"[Taller1Builder] {upgradeable.GetType().Name} en '{upgradeable.name}' " +
                $"se ha quedado sin UpgradeData.", upgradeable);
            missing++;
        }

        if (missing == 0) return;

        EditorUtility.DisplayDialog("Taller 1",
            $"{missing} mejora(s) se han quedado sin datos; mira la consola. " +
            "El panel no podrá abrirlas hasta arreglarlo.", "Vale");
    }

    private static CartUpgradeData CartData() => LoadOrCreate<CartUpgradeData>(
        "Assets/ScriptableObjects/Upgrades/CartUpgrade.asset",
        "Carrito", "Transporta los objetos más rápido entre recepción y taller.",
        baseCost: 250);

    private static ReceptionUpgradeData ReceptionData() => LoadOrCreate<ReceptionUpgradeData>(
        "Assets/ScriptableObjects/Upgrades/ReceptionUpgrade.asset",
        "Recepción", "Atiende a los clientes más rápido.",
        baseCost: 200);

    /// <summary>
    /// Datos de la mejora de una decoración. Los umbrales y el nivel máximo se
    /// reescriben en cada construcción a propósito: tiene que haber
    /// exactamente un tramo por pieza, y si se descuadran dejarían piezas
    /// inalcanzables o la barra apuntando a niveles que no existen. El coste,
    /// los textos y las monedas por nivel sí se respetan si ya los has tocado.
    /// </summary>
    private static DecorationUpgradeData DecorationData(DecorationSpec spec, int pieceCount)
    {
        DecorationUpgradeData data = LoadOrCreate<DecorationUpgradeData>(
            spec.AssetPath, spec.DisplayName, spec.Description, baseCost: 300);

        if (data == null) return null;

        int[] thresholds = spec.Thresholds;
        int stages = Mathf.Min(pieceCount, thresholds.Length);

        data.evolutionStages.Clear();
        for (int i = 0; i < stages; i++)
            data.evolutionStages.Add(new EvolutionStage { levelThreshold = thresholds[i] });

        data.maxLevel = thresholds[stages - 1];

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        return data;
    }

    /// <summary>
    /// Carga el asset de mejora, o lo crea con los valores de partida si no
    /// existe. Nunca pisa uno que ya esté: los números afinados a mano son del
    /// jugador, no del builder.
    /// </summary>
    private static T LoadOrCreate<T>(
        string path, string elementName, string description, double baseCost,
        System.Action<T> configureNew = null)
        where T : UpgradeData
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        // Si hay algo en esa ruta pero no es del tipo que toca, avisar en vez
        // de crear encima y perderlo.
        UpgradeData wrongType = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
        if (wrongType != null)
        {
            Debug.LogError(
                $"[Taller1Builder] {path} es {wrongType.GetType().Name} pero aquí " +
                $"hace falta {typeof(T).Name}. Bórralo o conviértelo a mano; " +
                $"no lo sobrescribo para no perder sus valores.", wrongType);
            return null;
        }

        T data = ScriptableObject.CreateInstance<T>();
        data.elementName = elementName;
        data.description = description;
        data.baseCost = baseCost;
        data.growthFactor = 1.5f;
        data.maxLevel = 20;

        // Los valores propios de cada tipo solo se ponen al crear el asset:
        // si se pusieran siempre, cada reconstrucción pisaría lo que hubieras
        // afinado a mano.
        configureNew?.Invoke(data);

        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Taller1Builder] Creado {path}. Ajusta ahí coste y niveles.");
        return data;
    }

    /// <summary>
    /// Saca la capa de lo sólido del raycast del toque.
    ///
    /// Sin esto, tocar una mesa o una planta devolvería su collider en vez del
    /// de la sala, y el panel que se abriría sería el de la zona que tenga por
    /// encima en la jerarquía — que no tiene por qué ser la que se ha tocado.
    /// </summary>
    private static void WireTapHandler()
    {
        TapHandler handler = Object.FindAnyObjectByType<TapHandler>(FindObjectsInactive.Include);
        if (handler == null)
        {
            Debug.LogWarning("[Taller1Builder] No hay TapHandler en la escena: no puedo excluir " +
                             "la capa de lo sólido del toque.");
            return;
        }

        int solid = LayerSetup.SolidLayerIndex;
        if (solid < 0) return;

        SerializedObject so = new(handler);
        SerializedProperty mask = so.FindProperty("tapLayers");

        mask.intValue &= ~(1 << solid);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── Clientes ──────────────────────────────────────────────────────

    /// <summary>
    /// El gestor de clientes del taller: su cola, sus mostradores, su sala de
    /// espera y sus caminos.
    ///
    /// Lo que no es del layout —el prefab de cliente, la base de objetos, los
    /// tiempos, la probabilidad de sentarse— se copia de la plantilla, que es
    /// el gestor que ya tenías configurado a mano. Así esos ajustes se tocan en
    /// un solo sitio y valen para todos los talleres.
    /// </summary>
    private static void BuildCustomerManager(
        GameObject root, CustomerManager template, WorkStation station,
        ReceptionDesk dropOff, PickupDesk pickup, WaitingArea waitingArea, HallAnchors hall)
    {
        GameObject go = NewChild(root.transform, "Clientes", Vector2.zero);
        CustomerManager manager = go.AddComponent<CustomerManager>();

        EditorUtility.CopySerialized(template, manager);

        // CopySerialized copia también si el componente está activado. La
        // plantilla de la escena se apaga al acabar de construir, así que a
        // partir de la segunda reconstrucción todos los gestores saldrían
        // apagados, y no entraría ni un cliente en ningún taller.
        manager.enabled = true;

        // Y encima, todo lo que es de este taller: lo copiado de la plantilla
        // apunta al taller viejo, que ya no existe.
        SerializedObject so = new(manager);
        so.FindProperty("deskSlot").objectReferenceValue = hall.DeskSlot;
        so.FindProperty("pickupSlot").objectReferenceValue = hall.PickupSlot;
        so.FindProperty("itemDropTarget").objectReferenceValue = hall.ItemDropTarget;
        so.FindProperty("exitPoint").objectReferenceValue = hall.ExitPoint;
        so.FindProperty("dropOffDesk").objectReferenceValue = dropOff;
        so.FindProperty("pickupDesk").objectReferenceValue = pickup;
        so.FindProperty("waitingArea").objectReferenceValue = waitingArea;
        so.FindProperty("slotSpacingY").floatValue = QueueSpacingY;
        so.FindProperty("maxCustomers").intValue = QueueLength;

        SetObjectList(so.FindProperty("pathWaypoints"), hall.EntryPath);
        SetObjectList(so.FindProperty("returnWaypoints"), hall.ReturnPath);
        SetObjectList(so.FindProperty("workStations"), new List<WorkStation> { station });

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// Una copia temporal del gestor de clientes que sirve de plantilla, hecha
    /// antes de borrar nada.
    ///
    /// Se prefiere el de la escena (el que configuraste a mano); si ya no está,
    /// el del primer taller. Se copia porque el del taller se va a borrar al
    /// reconstruir, y sin copia no quedaría de dónde sacar el prefab de cliente.
    /// Quien llama la destruye al acabar.
    /// </summary>
    private static CustomerManager TakeCustomerTemplate(GameObject firstWorkshop)
    {
        CustomerManager source = SceneCustomerManager();

        if (source == null && firstWorkshop != null)
            source = firstWorkshop.GetComponentInChildren<CustomerManager>(includeInactive: true);

        if (source == null)
        {
            EditorUtility.DisplayDialog(
                "Talleres",
                "No encuentro ningún CustomerManager del que sacar el prefab de cliente, " +
                "la base de objetos y los tiempos.\n\nAñade uno a la escena, configúralo y " +
                "vuelve a construir.",
                "Vale");
            return null;
        }

        GameObject copy = Object.Instantiate(source.gameObject);
        copy.hideFlags = HideFlags.HideAndDontSave;
        return copy.GetComponent<CustomerManager>();
    }

    /// <summary>El gestor de clientes que está fuera de cualquier taller, si lo hay.</summary>
    private static CustomerManager SceneCustomerManager()
    {
        foreach (CustomerManager manager in
                 Object.FindObjectsByType<CustomerManager>(FindObjectsInactive.Include))
        {
            if (manager.GetComponentInParent<Workshop>(includeInactive: true) == null)
                return manager;
        }

        return null;
    }

    /// <summary>
    /// Apaga el gestor de clientes de la escena. Ya no gestiona nada —cada
    /// taller lleva el suyo— y si siguiera encendido metería clientes en la
    /// cola de un taller que ya no existe.
    ///
    /// Se apaga el componente, no se borra: se queda como plantilla de ajustes
    /// para las próximas reconstrucciones, y así lo que configuraste a mano no
    /// se pierde.
    /// </summary>
    private static void RetireSceneCustomerManager()
    {
        CustomerManager manager = SceneCustomerManager();
        if (manager == null || !manager.enabled) return;

        Undo.RecordObject(manager, "Construir talleres");
        manager.enabled = false;

        Debug.Log(
            $"[Taller1Builder] '{manager.name}' ya no gestiona clientes: cada taller lleva el " +
            "suyo. Se queda apagado como plantilla de ajustes (prefab de cliente, base de " +
            "objetos, tiempos), así que tócalo ahí y reconstruye.", manager);
    }

    // ── Talleres ──────────────────────────────────────────────────────

    /// <summary>Lo que se conserva de un taller al reconstruirlo.</summary>
    private class WorkshopSettings
    {
        public string DisplayName;
        public double UnlockCost;
        public int? RequiredPreviousLevel;

        public static WorkshopSettings From(GameObject root)
        {
            Workshop workshop = root.GetComponent<Workshop>();
            if (workshop == null) return null;

            // Un taller de la versión anterior no tenía este campo y lo trae a
            // 0: eso sería "sin requisito", así que se trata como no puesto.
            int required = workshop.RequiredPreviousLevel;

            return new WorkshopSettings
            {
                DisplayName = workshop.DisplayName,
                UnlockCost = workshop.UnlockCost,
                RequiredPreviousLevel = required > 0 ? required : null,
            };
        }
    }

    /// <summary>
    /// Las raíces de los talleres que ya haya en la escena, en orden, con null
    /// donde falte alguno.
    /// </summary>
    private static GameObject[] FindWorkshopRoots()
    {
        var found = new GameObject[WorkshopCount];

        for (int i = 0; i < WorkshopCount; i++)
            found[i] = FindRoot(RootNameFor(i));

        return found;
    }

    /// <summary>
    /// Una raíz de la escena por nombre, esté encendida o no. GameObject.Find
    /// no sirve: no ve los objetos apagados.
    /// </summary>
    private static GameObject FindRoot(string name)
    {
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name == name) return go;

        return null;
    }

    /// <summary>
    /// Lo que cuesta el segundo taller: lo que pusiste en WorkStation2.asset si
    /// existe, o el valor por defecto.
    /// </summary>
    private static double NextWorkshopCost()
    {
        WorkStationData legacy = AssetDatabase.LoadAssetAtPath<WorkStationData>(LegacyWorkStation2Path);
        return legacy != null && legacy.cost > 0 ? legacy.cost : DefaultNextWorkshopCost;
    }

    /// <summary>
    /// A dónde va la cámara al visitar un taller, medido desde su raíz.
    ///
    /// Sale de dónde está la cámara en la escena respecto al primer taller: es
    /// la vista con la que arranca el juego, así que volver al taller 1 te deja
    /// donde empezaste, y el taller 2 se ve igual que el 1.
    /// </summary>
    private static Vector3 HomeLocal(Vector3 firstPosition)
    {
        Camera cam = Camera.main;
        if (cam == null) return DefaultHomePoint;

        Vector3 local = cam.transform.position - firstPosition;
        return new Vector3(local.x, local.y, 0f);
    }

    /// <summary>
    /// Copia los suelos del mapa dentro de un taller, en el mismo sitio
    /// relativo. Así el taller trae sus suelos consigo y cuadra con ellos esté
    /// donde esté.
    ///
    /// Los del primer taller no se tocan: son los del mapa, puestos a mano.
    /// </summary>
    private static void CloneFloors(GameObject root, Transform map, Vector3 firstPosition)
    {
        GameObject floors = NewChild(root.transform, "Suelos", Vector2.zero);

        foreach (Transform floor in map)
        {
            if (!floor.TryGetComponent(out SpriteRenderer source)) continue;

            GameObject copy = NewChild(floors.transform, floor.name,
                (Vector2)(floor.position - firstPosition));

            copy.transform.localScale = floor.lossyScale;

            SpriteRenderer sr = copy.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            sr.color = source.color;
            sr.sortingLayerID = source.sortingLayerID;
            sr.sortingOrder = source.sortingOrder;
            sr.drawMode = source.drawMode;
            if (source.drawMode != SpriteDrawMode.Simple) sr.size = source.size;
        }
    }

    private static void WireUnlocker(List<Workshop> workshops)
    {
        WorkStationUnlocker unlocker = Object.FindAnyObjectByType<WorkStationUnlocker>(FindObjectsInactive.Include);
        if (unlocker == null)
        {
            Debug.LogWarning(
                "[Taller1Builder] No hay WorkStationUnlocker en la escena: no se podrá abrir " +
                "ningún taller más allá del primero.");
            return;
        }

        SerializedObject so = new(unlocker);
        SetObjectList(so.FindProperty("workshops"), workshops);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// Amplía los límites de la cámara para que llegue a todos los talleres,
    /// estén abiertos o no. Solo amplía: lo que hubiera puesto a mano se
    /// respeta si ya era más grande.
    /// </summary>
    private static void WireCamera(List<Workshop> workshops)
    {
        CameraController cam = Object.FindAnyObjectByType<CameraController>(FindObjectsInactive.Include);
        if (cam == null) return;

        SerializedObject so = new(cam);
        float margin = so.FindProperty("defaultZoom").floatValue;

        foreach (Workshop workshop in workshops)
        {
            Bounds b = workshop.WorldBounds;

            Expand(so.FindProperty("xMin"), b.min.x - margin, Mathf.Min);
            Expand(so.FindProperty("xMax"), b.max.x + margin, Mathf.Max);
            Expand(so.FindProperty("yMin"), b.min.y - margin, Mathf.Min);
            Expand(so.FindProperty("yMax"), b.max.y + margin, Mathf.Max);
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        static void Expand(SerializedProperty p, float value, System.Func<float, float, float> pick) =>
            p.floatValue = pick(p.floatValue, value);
    }

    // ── Ids de guardado ───────────────────────────────────────────────

    /// <summary>
    /// Los ids de guardado de un taller, por la ruta de cada elemento dentro
    /// de él.
    ///
    /// Reconstruir crea todos los componentes de nuevo, y cada uno recibe un id
    /// nuevo. La partida guardada del jugador se quedaría apuntando a los ids
    /// viejos, que ya no tiene nadie: todo lo comprado en salas, decoraciones y
    /// carritos se perdería en silencio. Como el builder monta siempre el mismo
    /// árbol, la ruta identifica al mismo elemento antes y después.
    /// </summary>
    private static Dictionary<string, string> CaptureSaveIds(GameObject root)
    {
        var ids = new Dictionary<string, string>();

        foreach (var (key, element) in SaveableElements(root))
            if (!string.IsNullOrEmpty(element.SaveId)) ids[key] = element.SaveId;

        return ids;
    }

    private static void RestoreSaveIds(GameObject root, Dictionary<string, string> ids)
    {
        if (ids == null || ids.Count == 0) return;

        foreach (var (key, element) in SaveableElements(root))
        {
            if (!ids.TryGetValue(key, out string id)) continue;

            SerializedObject so = new(element as Object);
            SerializedProperty property = so.FindProperty("saveId");
            if (property == null) continue;

            property.stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    /// <summary>
    /// Cada elemento que se guarda por id, con una clave estable: su ruta
    /// dentro del taller y su tipo. Si dos cosas acabaran con la misma clave
    /// (dos hermanos con el mismo nombre), se numeran por orden de aparición,
    /// que es el mismo al capturar y al restaurar porque el árbol es el mismo.
    /// </summary>
    private static IEnumerable<(string key, ISaveableElement element)> SaveableElements(GameObject root)
    {
        var seen = new Dictionary<string, int>();

        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
        {
            if (behaviour is not ISaveableElement element || !element.SavesItself) continue;

            string key = RelativePath(root.transform, behaviour.transform) + "|" + behaviour.GetType().Name;

            seen.TryGetValue(key, out int count);
            seen[key] = count + 1;

            yield return (count == 0 ? key : $"{key}#{count}", element);
        }
    }

    private static string RelativePath(Transform root, Transform target)
    {
        var parts = new List<string>();

        for (Transform t = target; t != null && t != root; t = t.parent)
            parts.Add(t.name);

        parts.Reverse();
        return string.Join("/", parts);
    }

    // ── Interfaz de talleres ──────────────────────────────────────────

    private const string DotsName = "PuntosTalleres";

    // El selector de flechas de la versión anterior. Se borra si sigue en la
    // escena: su script ya no existe y se quedaría como "Missing Script".
    private const string LegacySwitcherName = "SelectorTalleres";

    // El panel de comprar taller.
    private const string UnlockPanelName = "CompraTaller";

    // El panel de pruebas donde antes vivía el botón de comprar. Se deja como
    // estaba: tiene también el campo de monedas de la tecla Z.
    private const string LegacyTestPanelName = "UnlockTest";
    private const string LegacyUnlockButtonName = "UnlockWorkStationButton";

    /// <summary>
    /// Los puntitos de abajo, uno por taller, y encima el panel de comprar.
    /// </summary>
    private static void BuildWorkshopUI()
    {
        Transform canvas = FindUICanvas();
        if (canvas == null)
        {
            Debug.LogWarning(
                "[Taller1Builder] No encuentro un Canvas de pantalla donde poner los puntitos " +
                "y el panel de comprar taller.");
            return;
        }

        foreach (string name in new[] { DotsName, LegacySwitcherName, UnlockPanelName })
        {
            Transform old = canvas.Find(name);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        }

        RetireLegacyUnlockButton();

        BuildUnlockPanel(canvas);
        BuildDots(canvas);
    }

    /// <summary>
    /// El Canvas de la interfaz: el que ya tenga los puntitos o el botón de
    /// comprar, o si no el primero que se dibuje sobre la pantalla.
    /// </summary>
    private static Transform FindUICanvas()
    {
        foreach (WorkStationUnlockUI ui in Object.FindObjectsByType<WorkStationUnlockUI>(FindObjectsInactive.Include))
        {
            Canvas owner = ui.GetComponentInParent<Canvas>(includeInactive: true);
            if (owner != null) return owner.rootCanvas.transform;
        }

        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
                return canvas.transform;

        return null;
    }

    /// <summary>
    /// El botón de comprar de antes estaba dentro del panel de pruebas. Se le
    /// quita el componente —ahora lo lleva el panel nuevo— y se apaga el botón
    /// viejo, que ya no haría nada.
    ///
    /// Si una reconstrucción anterior lo había centrado, se devuelve a donde
    /// estaba: arriba, con el campo de monedas de pruebas.
    /// </summary>
    private static void RetireLegacyUnlockButton()
    {
        foreach (WorkStationUnlockUI ui in Object.FindObjectsByType<WorkStationUnlockUI>(FindObjectsInactive.Include))
        {
            if (ui.gameObject.name == UnlockPanelName) continue;

            GameObject go = ui.gameObject;
            Undo.DestroyObjectImmediate(ui);

            if (!go.activeSelf)
            {
                Undo.RecordObject(go, "Construir talleres");
                go.SetActive(true);
            }

            Transform oldButton = go.transform.Find(LegacyUnlockButtonName);
            if (oldButton != null && oldButton.gameObject.activeSelf)
            {
                Undo.RecordObject(oldButton.gameObject, "Construir talleres");
                oldButton.gameObject.SetActive(false);
            }

            RestoreLegacyTestPanel(go.GetComponent<RectTransform>());
        }
    }

    private static void RestoreLegacyTestPanel(RectTransform rt)
    {
        if (rt == null || rt.name != LegacyTestPanelName) return;

        // Solo si está como lo dejó la versión anterior del builder (centrado y
        // sin tamaño). Si lo has movido tú, no se toca.
        bool centredByBuilder =
            rt.anchorMin == new Vector2(0.5f, 0.5f) && rt.anchorMax == new Vector2(0.5f, 0.5f) &&
            rt.sizeDelta == Vector2.zero && rt.anchoredPosition == Vector2.zero;

        if (!centredByBuilder) return;

        Undo.RecordObject(rt, "Construir talleres");
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = new Vector2(0f, 1337f);
    }

    /// <summary>
    /// El panel de comprar, abajo en el centro, justo encima de los puntitos:
    /// es a donde mira el pulgar después de pulsar uno.
    ///
    /// La raíz se queda encendida siempre y sin gráfico —no tapa ni bloquea
    /// nada—; lo que se enciende y apaga es "Contenido". Así el componente
    /// sigue mirando aunque el panel no se vea.
    /// </summary>
    private static void BuildUnlockPanel(Transform canvas)
    {
        TMP_FontAsset font = FindUIFont(canvas);

        GameObject root = new(UnlockPanelName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(root, "Construir talleres");
        root.transform.SetParent(canvas, worldPositionStays: false);

        RectTransform rootRt = (RectTransform)root.transform;
        rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0f);
        rootRt.pivot = new Vector2(0.5f, 0f);
        rootRt.anchoredPosition = new Vector2(0f, 170f);
        rootRt.sizeDelta = new Vector2(780f, 380f);

        // Fondo oscuro con esquinas redondeadas.
        GameObject content = new("Contenido", typeof(RectTransform));
        content.transform.SetParent(root.transform, worldPositionStays: false);
        Stretch((RectTransform)content.transform);

        Image background = content.AddComponent<Image>();
        background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        background.type = Image.Type.Sliced;
        background.color = new Color(0.12f, 0.10f, 0.09f, 0.9f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 22, 26);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = AddPanelText(content.transform, "Titulo", font, 56f, FontStyles.Bold, 66f);
        TextMeshProUGUI cost = AddPanelText(content.transform, "Precio", font, 40f, FontStyles.Normal, 50f);
        TextMeshProUGUI level = AddPanelText(content.transform, "Nivel", font, 40f, FontStyles.Normal, 50f);

        // El botón.
        GameObject buttonGo = new("Desbloquear", typeof(RectTransform));
        buttonGo.transform.SetParent(content.transform, worldPositionStays: false);

        LayoutElement buttonLayout = buttonGo.AddComponent<LayoutElement>();
        buttonLayout.preferredWidth = 440f;
        buttonLayout.preferredHeight = 110f;

        Image buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.sprite = background.sprite;
        buttonImage.type = Image.Type.Sliced;

        Button button = buttonGo.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        // El color lo pone el componente (verde o gris); el tinte del botón
        // solo oscurece al pulsar, y desactivado no añade nada encima.
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        colors.selectedColor = Color.white;
        colors.disabledColor = Color.white;
        button.colors = colors;

        GameObject labelGo = new("Texto", typeof(RectTransform));
        labelGo.transform.SetParent(buttonGo.transform, worldPositionStays: false);
        Stretch((RectTransform)labelGo.transform);

        TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = "Desbloquear";
        label.fontSize = 46f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        // Apagado en la escena: el componente lo enciende cuando toca.
        content.SetActive(false);

        WorkStationUnlockUI ui = root.AddComponent<WorkStationUnlockUI>();
        SerializedObject so = new(ui);
        so.FindProperty("content").objectReferenceValue = content;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("costText").objectReferenceValue = cost;
        so.FindProperty("levelText").objectReferenceValue = level;
        so.FindProperty("unlockButton").objectReferenceValue = button;
        so.FindProperty("buttonLabel").objectReferenceValue = label;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TextMeshProUGUI AddPanelText(
        Transform parent, string name, TMP_FontAsset font, float size, FontStyles style, float height)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.transform.SetParent(parent, worldPositionStays: false);

        LayoutElement element = go.AddComponent<LayoutElement>();
        element.preferredWidth = 700f;
        element.preferredHeight = height;

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;

        return text;
    }

    /// <summary>La fuente que ya use la interfaz, para que el panel no desentone.</summary>
    private static TMP_FontAsset FindUIFont(Transform canvas)
    {
        foreach (TextMeshProUGUI text in canvas.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true))
            if (text.font != null) return text.font;

        return TMP_Settings.defaultFontAsset;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// La fila de puntitos, abajo en el centro. Los puntos en sí los crea el
    /// componente al arrancar, porque depende de cuántos talleres haya.
    /// </summary>
    private static void BuildDots(Transform canvas)
    {
        GameObject go = new(DotsName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Construir talleres");
        go.transform.SetParent(canvas, worldPositionStays: false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 60f);
        rt.sizeDelta = new Vector2(400f, 80f);

        HorizontalLayoutGroup layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 8f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        WorkshopDotsUI dots = go.AddComponent<WorkshopDotsUI>();
        SerializedObject so = new(dots);
        so.FindProperty("container").objectReferenceValue = rt;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static ItemStack BuildStack(
        Transform parent, string name, Vector2 pos, Vector2 dockPos)
    {
        GameObject go = NewChild(parent, name, pos);

        // Bandeja: marca dónde está el saco.
        GameObject trayGO = NewChild(go.transform, "Bandeja", pos);
        AddSquare(trayGO, new Vector2(0.62f, 0.34f), new Color(0.38f, 0.30f, 0.22f), OrderStack);

        Sack sack = BuildSack(go.transform, pos + new Vector2(0f, 0.08f));

        Transform access = NewChild(go.transform, "AccessPoint", dockPos).transform;

        // El puf con el que la bolsa aparece y desaparece. Solo lo usan las de
        // terminados, que se esconden al desbloquear una mesa nueva, pero se
        // pone en todas: no estorba y deja la bolsa lista si algún día otra
        // también tiene que esconderse.
        AddPoof(go, sack.GetComponentInChildren<SpriteRenderer>());

        ItemStack stack = go.AddComponent<ItemStack>();
        SerializedObject so = new(stack);
        so.FindProperty("accessPoint").objectReferenceValue = access;
        so.FindProperty("sack").objectReferenceValue = sack;
        so.ApplyModifiedPropertiesWithoutUndo();

        return stack;
    }

    /// <summary>
    /// El saco: el cuadrado verde que está siempre a la vista y en el que
    /// entran y salen los objetos.
    /// </summary>
    private static Sack BuildSack(Transform parent, Vector2 pos)
    {
        GameObject go = NewChild(parent, "Saco", pos);

        GameObject bodyGO = NewChild(go.transform, "Cuerpo", pos);
        SpriteRenderer body = AddSquare(bodyGO, SackSize, SackColor, SortingOrders.Sack);

        // La boca, un poco por encima del centro: ahí aterrizan los saltitos.
        Transform mouth = NewChild(go.transform, "Boca", pos + new Vector2(0f, 0.12f)).transform;

        Sack sack = go.AddComponent<Sack>();
        SerializedObject so = new(sack);
        so.FindProperty("body").objectReferenceValue = body;
        so.FindProperty("mouth").objectReferenceValue = mouth;
        so.FindProperty("size").vector2Value = SackSize;
        so.ApplyModifiedPropertiesWithoutUndo();

        return sack;
    }

    /// <summary>
    /// Clona el círculo de progreso del prefab Worker y lo cuelga de otro
    /// objeto. Se copia de ahí en vez de construir uno nuevo para que sea
    /// literalmente el mismo que el de los trabajadores de mesa: si algún día
    /// se le cambia el aspecto al prefab, cambian todos a la vez.
    ///
    /// RepairProgressUI se recoloca cada frame en la posición de su padre más
    /// el offset, así que vale igual para algo quieto o para un carrito.
    /// </summary>
    private static RepairProgressUI CloneProgressUI(Transform parent, string name, Vector3 offset)
    {
        GameObject workerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorkerPrefabPath);
        RepairProgressUI template = workerPrefab != null
            ? workerPrefab.GetComponentInChildren<RepairProgressUI>(includeInactive: true)
            : null;

        if (template == null)
        {
            Debug.LogWarning(
                $"[Taller1Builder] No encuentro un RepairProgressUI dentro de {WorkerPrefabPath}: " +
                "ese objeto se queda sin círculo de progreso.");
            return null;
        }

        GameObject copy = Object.Instantiate(template.gameObject);
        copy.name = name;
        copy.transform.SetParent(parent, worldPositionStays: false);
        copy.transform.localPosition = Vector3.zero;

        // Mismo tamaño en pantalla que el círculo de los workers de mesa.
        copy.transform.localScale = Vector3.Scale(
            template.transform.localScale, NormalizedChildScale(parent, ActorScale));

        // El prefab lo trae en orden 4, por debajo de los objetos (40).
        foreach (Canvas canvas in copy.GetComponentsInChildren<Canvas>(includeInactive: true))
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrders.Popup;
        }

        RepairProgressUI progress = copy.GetComponent<RepairProgressUI>();

        SerializedObject so = new(progress);
        so.FindProperty("offset").vector3Value = offset;
        so.ApplyModifiedPropertiesWithoutUndo();

        return progress;
    }

    private static GameObject NewChild(Transform parent, string name, Vector2 worldPos)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, worldPositionStays: false);
        go.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
        go.transform.localScale = NormalizedChildScale(parent, 1f);
        return go;
    }

    /// <summary>
    /// Escala local que deja el hijo con escala 1 en mundo, aunque el padre
    /// esté escalado (las mesas lo están, para dar tamaño a su sprite).
    /// </summary>
    private static Vector3 NormalizedChildScale(Transform parent, float target)
    {
        if (parent == null) return Vector3.one * target;

        Vector3 lossy = parent.lossyScale;
        return new Vector3(
            Mathf.Approximately(lossy.x, 0f) ? target : target / lossy.x,
            Mathf.Approximately(lossy.y, 0f) ? target : target / lossy.y,
            1f);
    }

    /// <summary>
    /// Le pone a algo un collider que nadie puede atravesar andando.
    ///
    /// Va en su propia capa para que el raycast del toque lo ignore: si
    /// estuviera en Default, tocar una mesa abriría el panel de la sala que
    /// tenga encima en vez del suyo.
    ///
    /// El tamaño sale del sprite, no de un (1,1) fijo: los muebles que vienen
    /// de un prefab traen su propia escala y un collider unitario no les
    /// cuadraría.
    /// </summary>
    private static void MakeSolid(GameObject go)
    {
        if (go.GetComponent<BoxCollider2D>() != null) return;

        int layer = LayerSetup.SolidLayerIndex;
        if (layer < 0) return;

        go.layer = layer;

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();

        if (go.TryGetComponent(out SpriteRenderer sr) && sr.sprite != null)
            box.size = sr.sprite.bounds.size;
    }

    private static SpriteRenderer AddSquare(GameObject go, Vector2 size, Color color, int sortingOrder)
    {
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = UnitSquare();
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        Vector3 parentScale = NormalizedChildScale(go.transform.parent, 1f);
        go.transform.localScale = new Vector3(size.x * parentScale.x, size.y * parentScale.y, 1f);

        return sr;
    }

    /// <summary>
    /// El sprite cuadrado de 1x1 unidad que ya usa el resto del proyecto
    /// (suelos, mesas, mostradores). Se toma del propio suelo para no depender
    /// de rutas internas de Unity.
    /// </summary>
    private static Sprite UnitSquare()
    {
        GameObject floor = GameObject.Find("SueloTallerLimpieza");
        if (floor != null && floor.TryGetComponent(out SpriteRenderer sr) && sr.sprite != null)
            return sr.sprite;

        Debug.LogWarning("[Taller1Builder] No encuentro 'SueloTallerLimpieza' para tomar el sprite cuadrado. " +
                         "Asigna los sprites a mano o construye primero el mapa.");
        return null;
    }

    private static Transform FindOrCreate(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null) return found;

        GameObject go = new(name);
        go.transform.SetParent(parent, worldPositionStays: false);
        return go.transform;
    }

    private static void SetObjectList<T>(SerializedProperty property, List<T> values) where T : Object
    {
        property.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void TintActor(GameObject go, Color color)
    {
        foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(includeInactive: true))
        {
            sr.color = color;
            sr.sortingOrder = OrderActor;
        }

        // El círculo de progreso del prefab Worker vive en un Canvas en espacio
        // de mundo con orden 4: por debajo del objeto, así que no se veía.
        foreach (Canvas canvas in go.GetComponentsInChildren<Canvas>(includeInactive: true))
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrders.Popup;
        }
    }

    private static void EditorSceneMarkDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
