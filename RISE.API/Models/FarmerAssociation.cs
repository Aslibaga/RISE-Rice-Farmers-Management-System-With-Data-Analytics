namespace RISE.API.Models;

public class FarmerAssociation
{
    public int FarmerAssociationId { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Farmer> Farmers { get; set; } = new List<Farmer>();
}
