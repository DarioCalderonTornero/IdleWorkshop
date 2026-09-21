using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Revisa la identidad de guardado de todos los elementos de la escena abierta.
///
/// Existe porque un elemento sin id, o con el id de otro, no se nota jugando:
/// simplemente no se guarda, y el jugador descubre la pérdida al volver a abrir
/// el juego. Aquí se ve la escena entera de un vistazo, sin entrar en Play, y
/// se arregla en el sitio.
///
/// Lo que arregla:
///   - Elementos sin id: se les asigna uno.
///   - Ids repetidos: se le da uno nuevo a todos menos al primero. Pasa al
///     duplicar un objeto con Ctrl+D, que copia el id tal cual.
///
/// Las mesas de trabajo no salen en el listado: no se guardan por id, las
/// guarda su taller por posición.
/// </summary>
public static class SaveAudit
{
    [MenuItem("Taller/Revisar guardado")]
    public static void Run()
    {
        List<ISaveableElement> elements = Collect();

        if (elements.Count == 0)
        {
            EditorUtility.DisplayDialog("Revisar guardado",
                "No hay ningún elemento que se guarde por id en la escena abierta.", "Vale");
            return;
        }

        List<ISaveableElement> fromPrefab = FindPrefabInherited(elements);

        int assigned = FixMissing(elements);
        int deduped = FixDuplicates(elements);

        Report(elements, assigned, deduped, fromPrefab);
    }

    // ── Ids heredados de un prefab ───────────────────────────────────

    /// <summary>
    /// Elementos cuyo id no es suyo sino del prefab del que salen, así que
    /// todas las copias de ese prefab lo comparten.
    ///
    /// No se arregla solo a posta. Hay dos salidas y la elección no es de una
    /// herramienta: o el elemento se declara fuera del registro porque lo
    /// guarda su dueño (SavesItself), o cada copia lleva su propio id como
    /// override — y esto segundo solo vale si el prefab no se instancia
    /// también en tiempo de ejecución, donde no hay overrides que valgan.
    ///
    /// Es el caso que se escapó con el trabajador de las mesas: tres copias
    /// del mismo prefab, un solo id entre las tres.
    /// </summary>
    private static List<ISaveableElement> FindPrefabInherited(List<ISaveableElement> elements)
    {
        List<ISaveableElement> found = new();

        foreach (ISaveableElement element in elements)
        {
            if (element is not Component component) continue;
            if (!PrefabUtility.IsPartOfPrefabInstance(component)) continue;

            Object origin = PrefabUtility.GetCorrespondingObjectFromSource(component);
            if (origin == null) continue;

            SerializedProperty originId = new SerializedObject(origin).FindProperty("saveId");
            if (originId == null) continue;

            // Si difieren, esta copia ya tiene su propio override y no hay
            // problema: el id es suyo.
            if (originId.stringValue != element.SaveId) continue;

            found.Add(element);

            Debug.LogError(
                $"[SaveAudit] {Path(element)} usa el id '{element.SaveId}' heredado de " +
                $"'{AssetDatabase.GetAssetPath(origin)}', así que lo comparte con todas las " +
                "copias de ese prefab y no identifica a ninguna.\n" +
                "Decide: o el elemento se guarda por su dueño (SavesItself => false, como " +
                "WorkDeskUpgradeable y WorkerUpgradeable), o le das a esta copia su propio id.",
                component);
        }

        return found;
    }

    // ── Recogida ─────────────────────────────────────────────────────

    private static List<ISaveableElement> Collect()
    {
        List<ISaveableElement> found = new();

        // No se puede buscar por interfaz, así que se barren los MonoBehaviour
        // y se filtra. Include para que cuenten también los que estén apagados:
        // un almacén por comprar tiene su cuerpo desactivado y aun así se guarda.
        foreach (MonoBehaviour behaviour in
                 Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (behaviour is ISaveableElement element && element.SavesItself)
                found.Add(element);
        }

        return found;
    }

    // ── Arreglos ─────────────────────────────────────────────────────

    private static int FixMissing(List<ISaveableElement> elements)
    {
        int assigned = 0;

        foreach (ISaveableElement element in elements)
        {
            if (!string.IsNullOrEmpty(element.SaveId)) continue;

            AssignNewId(element);
            assigned++;
        }

        return assigned;
    }

    private static int FixDuplicates(List<ISaveableElement> elements)
    {
        HashSet<string> seen = new();
        int fixedCount = 0;

        foreach (ISaveableElement element in elements)
        {
            // El primero se queda con el id: así el que ya tenía progreso
            // guardado no lo pierde, y el nuevo es el que cambia.
            if (seen.Add(element.SaveId)) continue;

            Debug.LogWarning(
                $"[SaveAudit] {Path(element)} repetía el id '{element.SaveId}'. Se le ha dado uno nuevo.",
                element as Object);

            AssignNewId(element);
            seen.Add(element.SaveId);
            fixedCount++;
        }

        return fixedCount;
    }

    /// <summary>
    /// Escribe el id por reflexión del SerializedObject.
    ///
    /// El campo es privado a propósito —nadie debería poder cambiar un id desde
    /// código de juego— así que se toca por la vía del Editor, que además
    /// registra el cambio para el Deshacer y marca la escena como sucia sola.
    /// </summary>
    private static void AssignNewId(ISaveableElement element)
    {
        SerializedObject so = new(element as Object);
        SerializedProperty property = so.FindProperty("saveId");

        if (property == null)
        {
            Debug.LogError(
                $"[SaveAudit] {Path(element)} no tiene campo 'saveId' que rellenar.",
                element as Object);
            return;
        }

        property.stringValue = SaveIdentity.NewId();
        so.ApplyModifiedProperties();
    }

    // ── Informe ──────────────────────────────────────────────────────

    private static void Report(List<ISaveableElement> elements, int assigned, int deduped,
                              List<ISaveableElement> fromPrefab)
    {
        StringBuilder report = new();
        report.AppendLine($"[SaveAudit] {elements.Count} elemento(s) se guardan por id:");
        report.AppendLine();

        foreach (ISaveableElement element in elements)
            report.AppendLine($"  {element.SaveId}   {Path(element)}");

        Debug.Log(report.ToString());

        string summary =
            $"{elements.Count} elemento(s) se guardan por id.\n\n" +
            $"Ids asignados que faltaban: {assigned}\n" +
            $"Ids repetidos corregidos: {deduped}\n" +
            $"Ids heredados de un prefab: {fromPrefab.Count}" +
            (fromPrefab.Count > 0 ? "  <-- HAY QUE DECIDIR A MANO" : "") + "\n\n" +
            (fromPrefab.Count > 0
                ? "Los heredados de un prefab NO se arreglan solos: mira la consola, " +
                  "explica cada caso y las dos salidas posibles.\n\n"
                : "") +
            (assigned + deduped > 0
                ? "GUARDA LA ESCENA (Ctrl+S) para que los ids sean permanentes."
                : fromPrefab.Count == 0
                    ? "Todo correcto, no había nada que arreglar."
                    : "") +
            "\n\nEl detalle está en la consola.";

        EditorUtility.DisplayDialog("Revisar guardado", summary, "Vale");
    }

    /// <summary>Ruta en la jerarquía, que es como se identifica algo de un vistazo.</summary>
    private static string Path(ISaveableElement element)
    {
        Component component = element as Component;
        if (component == null) return element.GetType().Name;

        string path = component.gameObject.name;

        Transform parent = component.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return $"{path}  ({element.GetType().Name})";
    }
}
