using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Revisa todas las mejoras de la escena abierta y dice cuáles están mal.
///
/// Existe porque un mejorable sin UpgradeData, o con uno del subtipo
/// equivocado, no se nota hasta que abres su panel en pleno juego — y entonces
/// el fallo sale lejísimos de su causa. Aquí se ve la escena entera de un
/// vistazo y sin entrar en Play.
/// </summary>
public static class UpgradeAudit
{
    [MenuItem("Taller/Revisar mejoras de la escena")]
    public static void Run()
    {
        UpgradeableBase[] upgradeables =
            Object.FindObjectsByType<UpgradeableBase>(FindObjectsInactive.Include);

        if (upgradeables.Length == 0)
        {
            EditorUtility.DisplayDialog("Revisar mejoras",
                "No hay ningún mejorable en la escena abierta.", "Vale");
            return;
        }

        List<string> broken = new();
        StringBuilder report = new();
        report.AppendLine($"Mejoras encontradas: {upgradeables.Length}");
        report.AppendLine();

        foreach (UpgradeableBase upgradeable in upgradeables)
        {
            string who = $"{upgradeable.GetType().Name} en '{upgradeable.name}'";
            UpgradeData data = upgradeable.UpgradeData;
            System.Type expected = upgradeable.ExpectedDataType;

            if (data == null)
            {
                report.AppendLine($"  SIN DATOS  {who} — necesita un {expected.Name}");
                broken.Add(who);
                Debug.LogError($"[Revisar mejoras] {who} no tiene UpgradeData asignado.", upgradeable);
            }
            else if (!expected.IsInstanceOfType(data))
            {
                report.AppendLine($"  TIPO MAL   {who} — tiene '{data.name}' " +
                                  $"({data.GetType().Name}) y necesita {expected.Name}");
                broken.Add(who);
                Debug.LogError($"[Revisar mejoras] {who}: '{data.name}' es " +
                               $"{data.GetType().Name}, hace falta {expected.Name}.", upgradeable);
            }
            else
            {
                report.AppendLine($"  ok         {who} — '{data.name}' " +
                                  $"(nivel máx {data.maxLevel}, coste base {data.baseCost})");
            }
        }

        string summary = broken.Count == 0
            ? "Todas las mejoras están bien cableadas."
            : $"{broken.Count} mejora(s) con problemas. El panel fallará al abrirlas.\n\n" +
              "Suele arreglarse reconstruyendo: Taller > Construir layout Taller 1.";

        Debug.Log($"[Revisar mejoras]\n{report}");
        EditorUtility.DisplayDialog("Revisar mejoras", $"{summary}\n\n{report}", "Vale");
    }
}
