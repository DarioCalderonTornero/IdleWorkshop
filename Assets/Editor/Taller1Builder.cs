using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Construye el layout completo del Taller 1 en la escena abierta, calcado de
/// la hoja de diseño: las dos colas del hall, los dos mostradores de recepción,
/// los dos transportistas con carrito y las tres mesas de limpieza con su
/// worker, y deja todas las referencias cableadas.
///
/// Es idempotente: vuelve a ejecutarlo y reconstruye desde cero el objeto
/// "Taller1". No toca el mapa (los suelos) ni los managers, salvo para
/// re-cablear CustomerManager y WorkStationUnlocker al taller nuevo.
///
/// Las coordenadas están en unidades de mundo y salen de medir la imagen de
/// diseño sobre los suelos ya colocados:
///   Taller limpieza  X[-1.84, 5.03]  Y[1.75, 6.77]
///   Recepción        X[-0.21, 5.03]  Y[-1.69, 1.75]
///   Hall             X[-3.43, 5.03]  Y[-6.77, -1.69]
/// </summary>
public static class Taller1Builder
{
    private const string RootName = "Taller1";
    private const string WorkDeskPrefabPath = "Assets/Prefabs/WorkDesk/WorkDesk.prefab";

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

    // Los sacos: todos del mismo tamaño y capacidad, para que el lote que sale
    // de recepción quepa entero en el carrito y luego en la mesa.
    private const int BagCapacity = 5;
    private static readonly Vector2 SackEmptySize = new(0.34f, 0.28f);
    private static readonly Vector2 SackFullSize = new(0.52f, 0.46f);
    private static readonly Color SackEmptyColor = new(0.42f, 0.72f, 0.42f);
    private static readonly Color SackFullColor = new(0.15f, 0.52f, 0.18f);

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
    private static readonly float[] SeatColumns = { -2.85f, -1.95f };
    private static readonly float[] SeatRows = { -2.60f, -3.50f, -4.40f, -5.30f };


    // Zona naranja: la sala que se toca para abrir el panel de mejoras, y a
    // cuyo centro se lleva la cámara. Coincide con el suelo SueloTallerLimpieza.
    private static readonly Vector2 CleaningRoomCenter = new(1.60f, 4.26f);
    private static readonly Vector2 CleaningRoomSize = new(6.87f, 5.02f);

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

    [MenuItem("Taller/Construir layout Taller 1")]
    public static void Build()
    {
        GameObject deskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorkDeskPrefabPath);

        if (deskPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Taller 1",
                $"No encuentro el prefab de mesa:\n{WorkDeskPrefabPath}",
                "Vale");
            return;
        }

        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Taller 1",
                $"Ya existe un objeto \"{RootName}\" en la escena. Se borrará y se reconstruirá desde cero.\n\n¿Continuar?",
                "Reconstruir", "Cancelar");

            if (!replace) return;
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject root = new(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Construir Taller 1");

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
        WaitingArea waitingArea = BuildWaitingArea(root);

        WireWorkStation(station, dropOff, pickup, desks, new List<CartWorker> { inCart, outCart }, root.transform);

        // Las otras dos zonas mejorables: la sala amarilla mejora los carritos
        // y los mostradores mejoran la velocidad de atención.
        BuildCartZone(root, inCart, outCart);
        BuildReceptionZone(root,
            dropOff.GetComponent<ServiceSpeed>(),
            pickup.GetComponent<ServiceSpeed>());

        WireCustomerManager(dropOff, pickup, waitingArea, hall);
        WireUnlocker(station);

        Selection.activeGameObject = root;
        EditorSceneMarkDirty();

        Debug.Log("[Taller1Builder] Layout construido. Revisa la escena y guarda (Ctrl+S).");
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
            new Vector2(RightLaneX, BackDeskY + 0.05f), BackDeskDock, capacity: BagCapacity);

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
            new Vector2(LeftLaneX, PickupStackY + 0.05f), PickupDock, capacity: BagCapacity);

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
        ItemStack inStack = BuildStack(go.transform, "ZonaEspera", inPos, inDock, capacity: BagCapacity);
        ItemStack outStack = BuildStack(go.transform, "ZonaTerminados", outPos, outDock, capacity: BagCapacity);

        SerializedObject so = new(table);
        so.FindProperty("playerSlot").objectReferenceValue = playerPoint;
        so.FindProperty("itemSlot").objectReferenceValue = itemPoint;
        so.FindProperty("boxPoint").objectReferenceValue = boxPoint;
        so.FindProperty("inStack").objectReferenceValue = inStack;
        so.FindProperty("outStack").objectReferenceValue = outStack;
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

    /// <summary>Marca un mueble para que los carritos lo rodeen.</summary>
    private static void MarkAsObstacle(GameObject go)
    {
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
        cartSo.FindProperty("capacity").intValue = BagCapacity;
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
    /// <summary>
    /// La sala de espera: los asientos del hueco de la izquierda del hall,
    /// donde los clientes aguardan a que su objeto esté listo.
    /// </summary>
    private static WaitingArea BuildWaitingArea(GameObject root)
    {
        GameObject group = NewChild(root.transform, "SalaEspera", Vector2.zero);

        var seats = new List<Transform>();
        int index = 0;

        foreach (float x in SeatColumns)
        {
            foreach (float y in SeatRows)
            {
                GameObject seat = NewChild(group.transform, $"Asiento{++index:00}", new Vector2(x, y));
                AddSquare(seat, new Vector2(0.34f, 0.34f), new Color(0.35f, 0.42f, 0.55f), OrderFurniture);

                // El cliente se sienta un poco por delante del asiento para
                // que no lo tape del todo.
                seats.Add(NewChild(seat.transform, "Sitio", new Vector2(x + 0.30f, y)).transform);
            }
        }

        WaitingArea area = group.AddComponent<WaitingArea>();
        SerializedObject so = new(area);
        SetObjectList(so.FindProperty("seats"), seats);
        so.ApplyModifiedPropertiesWithoutUndo();

        return area;
    }

    // ── Cableado ──────────────────────────────────────────────────────

    private static void WireWorkStation(
        WorkStation station, ReceptionDesk dropOff, PickupDesk pickup,
        List<WorkDeskUnlockable> desks, List<CartWorker> carts, Transform root)
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

        BuildWorkshopZone(station.gameObject, focus, desks);
    }

    /// <summary>
    /// Zona de la sala naranja: se toca y salen las mesas, para desbloquearlas
    /// o mejorarlas. El collider ya está en la raíz del taller.
    /// </summary>
    private static void BuildWorkshopZone(
        GameObject root, Transform focus, List<WorkDeskUnlockable> desks)
    {
        UpgradeZone zone = root.AddComponent<UpgradeZone>();

        SerializedObject so = new(zone);
        so.FindProperty("zoneName").stringValue = "Taller de limpieza";
        so.FindProperty("cameraFocusPoint").objectReferenceValue = focus;

        SerializedProperty elements = so.FindProperty("upgradeElements");
        elements.arraySize = desks.Count;

        Sprite icon = DeskIcon();

        for (int i = 0; i < desks.Count; i++)
        {
            SerializedProperty element = elements.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("unlockableTarget").objectReferenceValue = desks[i];
            element.FindPropertyRelative("upgradeableTarget").objectReferenceValue =
                desks[i].GetComponent<WorkDeskUpgradeable>();
            element.FindPropertyRelative("icon").objectReferenceValue = icon;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Icono provisional de los botones de mesa, del set de graybox.</summary>
    private static Sprite DeskIcon() =>
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/GrayBox/Graybox2D/16x16.png");

    private static Sprite CartIcon() =>
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/GrayBox/Graybox2D/32x32.png");

    private static Sprite ReceptionIcon() =>
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/GrayBox/Graybox2D/16x32.png");

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

        var elements = new List<(Object target, Sprite icon)>();

        foreach (CartWorker cart in carts)
        {
            if (cart == null) continue;

            CartUpgradeable upgradeable = cart.gameObject.AddComponent<CartUpgradeable>();

            SerializedObject cartSo = new(upgradeable);
            cartSo.FindProperty("upgradeData").objectReferenceValue = CartUpgradeData();
            cartSo.FindProperty("cartWorker").objectReferenceValue = cart;
            cartSo.ApplyModifiedPropertiesWithoutUndo();

            elements.Add((upgradeable, CartIcon()));
        }

        WireZone(go, focus, "Carritos", elements);
    }

    /// <summary>
    /// Zona de las recepciones: todo el hall azul. Se toca en cualquier punto y
    /// la cámara se centra en él.
    /// </summary>
    private static void BuildReceptionZone(GameObject root, params ServiceSpeed[] desks)
    {
        GameObject go = NewChild(root.transform, "ZonaRecepciones", Vector2.zero);

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.offset = HallCenter;
        box.size = HallSize;

        Transform focus = NewChild(go.transform, "CameraFocusPoint", HallCenter).transform;

        var elements = new List<(Object target, Sprite icon)>();

        foreach (ServiceSpeed desk in desks)
        {
            if (desk == null) continue;

            ReceptionUpgradeable upgradeable = desk.gameObject.AddComponent<ReceptionUpgradeable>();

            SerializedObject deskSo = new(upgradeable);
            deskSo.FindProperty("upgradeData").objectReferenceValue = ReceptionUpgradeData();
            deskSo.FindProperty("serviceSpeed").objectReferenceValue = desk;
            deskSo.ApplyModifiedPropertiesWithoutUndo();

            elements.Add((upgradeable, ReceptionIcon()));
        }

        WireZone(go, focus, "Recepciones", elements);
    }

    private static void WireZone(
        GameObject go, Transform focus, string zoneName, List<(Object target, Sprite icon)> elements)
    {
        UpgradeZone zone = go.AddComponent<UpgradeZone>();

        SerializedObject so = new(zone);
        so.FindProperty("zoneName").stringValue = zoneName;
        so.FindProperty("cameraFocusPoint").objectReferenceValue = focus;

        SerializedProperty list = so.FindProperty("upgradeElements");
        list.arraySize = elements.Count;

        for (int i = 0; i < elements.Count; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("unlockableTarget").objectReferenceValue = null;
            element.FindPropertyRelative("upgradeableTarget").objectReferenceValue = elements[i].target;
            element.FindPropertyRelative("icon").objectReferenceValue = elements[i].icon;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── ScriptableObjects de mejora ───────────────────────────────────
    // Se crean solo si no existen, para no pisar los valores que ajuste el
    // jugador al reconstruir el layout.

    private static UpgradeData CartUpgradeData() => LoadOrCreateUpgradeData(
        "Assets/ScriptableObjects/Upgrades/CartUpgrade.asset",
        "Carrito", "Transporta los objetos más rápido entre recepción y taller.",
        baseCost: 250);

    private static UpgradeData ReceptionUpgradeData() => LoadOrCreateUpgradeData(
        "Assets/ScriptableObjects/Upgrades/ReceptionUpgrade.asset",
        "Recepción", "Atiende a los clientes más rápido.",
        baseCost: 200);

    private static UpgradeData LoadOrCreateUpgradeData(
        string path, string elementName, string description, double baseCost)
    {
        UpgradeData existing = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
        if (existing != null) return existing;

        UpgradeData data = ScriptableObject.CreateInstance<UpgradeData>();
        data.elementName = elementName;
        data.description = description;
        data.baseCost = baseCost;
        data.growthFactor = 1.5f;
        data.maxLevel = 20;

        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Taller1Builder] Creado {path}. Ajusta ahí coste y niveles.");
        return data;
    }

    private static void WireCustomerManager(
        ReceptionDesk dropOff, PickupDesk pickup, WaitingArea waitingArea, HallAnchors hall)
    {
        CustomerManager manager = Object.FindAnyObjectByType<CustomerManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            Debug.LogWarning("[Taller1Builder] No hay CustomerManager en la escena: no se ha podido cablear la cola.");
            return;
        }

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

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireUnlocker(WorkStation station)
    {
        WorkStationUnlocker unlocker = Object.FindAnyObjectByType<WorkStationUnlocker>(FindObjectsInactive.Include);
        if (unlocker == null) return;

        SerializedObject so = new(unlocker);
        so.FindProperty("initialWorkStation").objectReferenceValue = station;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static ItemStack BuildStack(
        Transform parent, string name, Vector2 pos, Vector2 dockPos, int capacity)
    {
        GameObject go = NewChild(parent, name, pos);

        // Bandeja: marca dónde está el saco.
        GameObject trayGO = NewChild(go.transform, "Bandeja", pos);
        AddSquare(trayGO, new Vector2(0.62f, 0.34f), new Color(0.38f, 0.30f, 0.22f), OrderStack);

        Sack sack = BuildSack(go.transform, pos + new Vector2(0f, 0.08f));

        Transform access = NewChild(go.transform, "AccessPoint", dockPos).transform;

        ItemStack stack = go.AddComponent<ItemStack>();
        SerializedObject so = new(stack);
        so.FindProperty("accessPoint").objectReferenceValue = access;
        so.FindProperty("capacity").intValue = capacity;
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
        SpriteRenderer body = AddSquare(bodyGO, SackEmptySize, SackEmptyColor, SortingOrders.Item);

        // La boca, un poco por encima del centro: ahí aterrizan los saltitos.
        Transform mouth = NewChild(go.transform, "Boca", pos + new Vector2(0f, 0.12f)).transform;

        Sack sack = go.AddComponent<Sack>();
        SerializedObject so = new(sack);
        so.FindProperty("body").objectReferenceValue = body;
        so.FindProperty("mouth").objectReferenceValue = mouth;
        so.FindProperty("emptyScale").vector2Value = SackEmptySize;
        so.FindProperty("fullScale").vector2Value = SackFullSize;
        so.FindProperty("emptyColor").colorValue = SackEmptyColor;
        so.FindProperty("fullColor").colorValue = SackFullColor;
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
