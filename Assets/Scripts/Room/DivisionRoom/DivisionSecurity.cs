using UnityEngine;

/// <summary>
/// Room Divisi Security -- tempat kerja & spawn point employee ber-Division = EmployeeDivision.Security.
/// Spesialisasi : Sterilisasi Ruangan (Lockdown/Poisoned) dan Pembasmian Hama.
/// </summary>
public class DivisionSecurity : DivisionRoom
{
    protected override EmployeeDivision EmployeeDivisionType => EmployeeDivision.Security;
}
