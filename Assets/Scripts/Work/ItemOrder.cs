/// <summary>
/// Un encargo concreto: qué objeto es y de quién.
///
/// El dueño se guarda como un número, no como una referencia al cliente,
/// porque el cliente puede irse del mapa mientras su objeto se restaura: se
/// destruye su GameObject y más tarde vuelve otro nuevo. El ticket sobrevive a
/// eso y es lo que permite devolverle SU objeto y no uno cualquiera.
/// </summary>
public class ItemOrder
{
    public ItemDefinition Definition { get; }

    /// <summary>Identidad del cliente que lo trajo. La lleva el CustomerManager.</summary>
    public int TicketId { get; }

    public ItemOrder(ItemDefinition definition, int ticketId)
    {
        Definition = definition;
        TicketId = ticketId;
    }
}
