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

        // Antes de tocar la escena: si algún asset de mejora no es del tipo que
        // espera su componente, el cableado saldría a null y el panel de
        // mejoras fallaría al abrirlo. Mejor no construir nada.
        if (!UpgradeAssetsAreValid()) return;

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

        // Decoraciones: suman monedas a cada cobro y van saliendo a trozos.
        List<DecorationReveal> workshopDecorations = BuildWorkshopDecorations(root);
        List<DecorationReveal> hallDecorations = BuildHallDecorations(root);

        WireWorkStation(station, dropOff, pickup, desks,
            new List<CartWorker> { inCart, outCart }, root.transform, workshopDecorations);

        // Las otras dos zonas mejorables: la sala amarilla mejora los carritos
        // y el hall mejora las recepciones y sus decoraciones.
        BuildCartZone(root, inCart, outCart);
        BuildReceptionZone(root,
            dropOff.GetComponent<ServiceSpeed>(),
            pickup.GetComponent<ServiceSpeed>(),
            waitingArea,
            hallDecorations);

        WireCustomerManager(dropOff, pickup, waitingArea, hall);
        WireUnlocker(station);

        VerifyUpgradeablesWired(root);

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
    private static WaitingArea BuildWaitingArea(GameObject root)
    {
        // Los asientos son una decoración más: se compran, suman monedas y van
        // apareciendo a trozos. Lo único suyo es que además reparten sitios.
        DecorationReveal reveal = BuildDecoration(root, new DecorationSpec
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
        List<DecorationReveal> decorations)
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
        List<DecorationReveal> decorations)
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
    private static List<ZoneElement> DecorationElements(List<DecorationReveal> decorations)
    {
        var elements = new List<ZoneElement>();
        if (decorations == null) return elements;

        Sprite icon = DecorationIcon();

        foreach (DecorationReveal decoration in decorations)
        {
            if (decoration == null) continue;

            elements.Add(new ZoneElement
            {
                Unlockable = decoration.GetComponent<Unlockable>(),
                Upgradeable = decoration.GetComponent<CoinBonusUpgradeable>(),
                Icon = icon,
            });
        }

        return elements;
    }

    /// <summary>
    /// Las dos decoraciones de la sala naranja: estanterías arriba y plantas
    /// en la columna de la derecha. Ninguna se cruza con los atraques de los
    /// carritos ni con las mesas.
    /// </summary>
    private static List<DecorationReveal> BuildWorkshopDecorations(GameObject root)
    {
        return new List<DecorationReveal>
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
    private static List<DecorationReveal> BuildHallDecorations(GameObject root)
    {
        return new List<DecorationReveal>
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
        GameObject root, ServiceSpeed dropOffSpeed, ServiceSpeed pickupSpeed, WaitingArea seats,
        List<DecorationReveal> decorations)
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
            deskSo.FindProperty("serviceSpeed").objectReferenceValue = desk;
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
    private static DecorationReveal BuildDecoration(GameObject root, DecorationSpec spec)
    {
        GameObject group = NewChild(root.transform, spec.ObjectName, Vector2.zero);

        var pieces = new List<GameObject>();
        var poofs = new List<Poof>();

        for (int i = 0; i < spec.Pieces.Length; i++)
        {
            GameObject piece = NewChild(group.transform, $"{spec.ObjectName}{i + 1:00}", spec.Pieces[i]);
            SpriteRenderer body = AddSquare(piece, spec.PieceSize, spec.PieceColor, OrderFurniture);

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

        return reveal;
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

    /// <summary>Una entrada del panel de mejoras de una zona.</summary>
    private class ZoneElement
    {
        public Object Unlockable;
        public Object Upgradeable;
        public Sprite Icon;
    }

    private static void WireZone(
        GameObject go, Transform focus, string zoneName, List<ZoneElement> elements)
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
            element.FindPropertyRelative("unlockableTarget").objectReferenceValue = elements[i].Unlockable;
            element.FindPropertyRelative("upgradeableTarget").objectReferenceValue = elements[i].Upgradeable;
            element.FindPropertyRelative("icon").objectReferenceValue = elements[i].Icon;
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
        ("Assets/ScriptableObjects/Upgrades/CartUpgrade.asset",      typeof(CartUpgradeData)),
        ("Assets/ScriptableObjects/Upgrades/ReceptionUpgrade.asset", typeof(ReceptionUpgradeData)),
        ("Assets/ScriptableObjects/Upgrades/SeatsUpgrade.asset",     typeof(DecorationUpgradeData)),
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
        string path, string elementName, string description, double baseCost)
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
