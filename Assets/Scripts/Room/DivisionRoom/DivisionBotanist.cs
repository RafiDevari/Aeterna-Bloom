/// <summary>
/// Room Divisi Botanist -- tempat kerja & spawn point employee ber-Division = EmployeeDivision.Botanist.
/// Spesialisasi : Feed & Harvest.
/// 
/// Memiliki 5 objek dekorasi (addObjects / "Adds (1)" - "Adds (5)") yang sekarang dikelola di parent class DivisionRoom.
/// </summary>
public class DivisionBotanist : DivisionRoom
{
    protected override EmployeeDivision EmployeeDivisionType => EmployeeDivision.Botanist;

    /// <summary>
    /// Alias method untuk kompatibilitas. Menggunakan implementasi UpdateAddVisuals() dari DivisionRoom.
    /// </summary>
    public void UpdateBotanistAddVisuals()
    {
        UpdateAddVisuals();
    }
}