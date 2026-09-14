using UnityEngine;

/// <summary>
/// Una bolsa donde los encargos esperan turno: el escritorio de recepción, la
/// zona de espera de una mesa, la parte contraria de la última mesa o la mesa
/// de entrega al cliente.
///
/// Existe para que el CartWorker pueda transportar entre dos bolsas
/// cualesquiera sin saber qué son.
/// </summary>
public interface IItemContainer
{
    /// <summary>Encargos que hay ahora mismo.</summary>
    int Count { get; }

    /// <summary>Cuántos caben.</summary>
    int Capacity { get; }

    /// <summary>Si admite al menos uno más.</summary>
    bool HasSpace { get; }

    /// <summary>Si ya no cabe nada: es cuando el carrito viene a vaciarla.</summary>
    bool IsFull { get; }

    /// <summary>Punto en mundo al que se acerca quien carga o descarga.</summary>
    Vector3 AccessPointPos { get; }

    /// <summary>
    /// Punto en mundo donde se ve la bolsa. No es lo mismo que
    /// <see cref="AccessPointPos"/>: ahí se pone quien viene a por ella, aquí
    /// es donde aterrizan los encargos que llegan dando un saltito.
    /// </summary>
    Vector3 ContentsPos { get; }

    /// <summary>Mete un encargo. Devuelve false si estaba llena.</summary>
    bool TryEnqueue(ItemOrder order);

    /// <summary>Saca el encargo más antiguo. Devuelve false si estaba vacía.</summary>
    bool TryDequeue(out ItemOrder order);
}
