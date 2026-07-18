using UnityEngine;

/// <summary>
/// Room Divisi Medic -- tempat kerja & spawn point employee ber-Division = EmployeeDivision.Medic.
/// Perilaku & mekanik khusus akan ditambahkan nanti.
/// </summary>
public class DivisionMedic : DivisionRoom
{
    protected override EmployeeDivision EmployeeDivisionType => EmployeeDivision.Medic;
}
